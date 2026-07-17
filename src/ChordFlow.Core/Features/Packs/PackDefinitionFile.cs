using System.Globalization;

namespace ChordFlow.Features.Packs;

/// <summary>
/// Reads a single pack <c>.dsl</c> file into a <see cref="PackDefinition"/> (pure — string in, model out;
/// the I/O is <see cref="PackReader"/>'s). Identity (design §6.4): the <b>filename stem is the id</b>, and an
/// optional leading <c>name:</c> header line is the display name (else the id is title-cased). The <c>name:</c>
/// line is peeled off the stored DSL — it is not part of any entity grammar — while the catalog header
/// (<c>genre</c>/<c>subgenre</c>/<c>tags</c>) is left in place so the importer denormalizes it like seeding.
/// </summary>
public static class PackDefinitionFile
{
    // The recognized leading-header keys. `name` is this layer's concern (identity, peeled out); the rest
    // mirror CatalogHeader's keys so a `name:` line placed after them is still inside the header block and
    // gets found. (These must stay in sync with CatalogHeader's recognized set — a stable v1 set.)
    private static readonly string[] HeaderKeys = { "name", "genre", "subgenre", "tags", "description", "tonality" };

    /// <summary>
    /// Build a <see cref="PackDefinition"/> from a file's <paramref name="fileName"/> (its stem is the id)
    /// and <paramref name="fileText"/>. Throws <see cref="FormatException"/> if the filename has no stem.
    /// </summary>
    public static PackDefinition Read(ContentKind kind, string fileName, string fileText)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(fileText);

        string id = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new FormatException($"pack definition file '{fileName}' has no id (empty filename stem).");
        }

        (string? name, string dsl) = ExtractName(fileText);
        // A `.dsl` file conventionally ends with a trailing newline; trailing whitespace is never meaningful
        // in any entity grammar, so trim it so the stored DSL matches the canonical (newline-free) form.
        return new PackDefinition(kind, id, name ?? TitleCase(id), dsl.TrimEnd());
    }

    // Peel an optional `name:` line from the leading contiguous header block. Returns the captured name
    // (null when absent or empty) and the remaining text with that one line removed (catalog header + body
    // intact). Scanning stops at the first line that is not a recognized header line — that begins the body.
    private static (string? Name, string Dsl) ExtractName(string fileText)
    {
        string[] lines = fileText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        string? name = null;
        int nameLine = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            if (!TryHeaderKey(lines[i].Trim(), out string? key, out string value))
            {
                break;
            }

            if (key == "name")
            {
                name = value.Length == 0 ? null : value;
                nameLine = i;
                break;
            }
        }

        if (nameLine < 0)
        {
            return (name, fileText);
        }

        string dsl = string.Join('\n', lines.Where((_, i) => i != nameLine));
        return (name, dsl);
    }

    private static bool TryHeaderKey(string trimmed, out string? key, out string value)
    {
        key = null;
        value = "";
        if (trimmed.Length == 0)
        {
            return false;
        }

        int colon = trimmed.IndexOf(':');
        if (colon < 0)
        {
            return false;
        }

        string candidate = trimmed[..colon].Trim().ToLowerInvariant();
        if (Array.IndexOf(HeaderKeys, candidate) < 0)
        {
            return false;
        }

        key = candidate;
        value = trimmed[(colon + 1)..].Trim();
        return true;
    }

    // Fallback display name from an id slug: `12bar_blues` -> "12bar Blues".
    private static string TitleCase(string id) =>
        CultureInfo.InvariantCulture.TextInfo.ToTitleCase(id.Replace('_', ' ').Replace('-', ' '));
}
