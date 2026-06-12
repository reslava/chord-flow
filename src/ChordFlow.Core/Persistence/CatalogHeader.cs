using System.Text;
using System.Text.Json;

namespace ChordFlow.Persistence;

/// <summary>
/// Parses and serializes the self-describing <b>catalog header</b> that may prefix any content
/// definition's canonical <c>.dsl</c> text:
/// <code>
/// genre: Blues
/// subgenre: Shuffle
/// tags: [12-bar, beginner]
/// 17 17 17 17 47 47 17 17 57 47 17 57
/// </code>
/// The header is an optional leading block of <c>key: value</c> lines (recognized keys: <c>genre</c>,
/// <c>subgenre</c>, <c>tags</c>); the first line that is not a recognized header line begins the
/// entity-specific <b>body</b> (e.g. the Nashville bar grammar). This lives on the Entity layer — the pure
/// <c>Domain/</c> parsers (e.g. <c>ProgressionParser</c>) only ever see the body and stay metadata-unaware
/// (constraint C1). The split round-trips 1:1: <c>Parse(Serialize(m, body))</c> yields <c>(m, body)</c>.
/// Shared by all four content entities (design §4: the DSL-header parse is one mechanism, not per-entity).
/// </summary>
public static class CatalogHeader
{
    private const string GenreKey = "genre";
    private const string SubgenreKey = "subgenre";
    private const string TagsKey = "tags";

    /// <summary>
    /// Split <paramref name="dslText"/> into its catalog metadata and the remaining body. A definition
    /// with no header yields (<see cref="CatalogMetadata.Empty"/>, the whole text unchanged). Header lines
    /// must form a contiguous block from the top; parsing stops at the first non-header line, which (with
    /// everything after it) is the body.
    /// </summary>
    public static (CatalogMetadata Metadata, string Body) Parse(string dslText)
    {
        ArgumentNullException.ThrowIfNull(dslText);

        string? genre = null;
        string? subgenre = null;
        IReadOnlyList<string> tags = Array.Empty<string>();

        int pos = 0;
        int bodyStart = 0;
        while (pos < dslText.Length)
        {
            int newline = dslText.IndexOf('\n', pos);
            string rawLine = newline < 0 ? dslText[pos..] : dslText[pos..newline];
            string trimmed = rawLine.Trim();

            if (!TryParseHeaderLine(trimmed, out string? key, out string value))
            {
                break;
            }

            switch (key)
            {
                case GenreKey: genre = NullIfEmpty(value); break;
                case SubgenreKey: subgenre = NullIfEmpty(value); break;
                case TagsKey: tags = ParseTagList(value); break;
            }

            pos = newline < 0 ? dslText.Length : newline + 1;
            bodyStart = pos;
        }

        string body = dslText[bodyStart..];
        return (new CatalogMetadata(genre, subgenre, tags), body);
    }

    /// <summary>
    /// Render <paramref name="metadata"/> as a header block prepended to <paramref name="body"/>. Only
    /// non-empty fields emit a line; an <see cref="CatalogMetadata.IsEmpty"/> metadata returns
    /// <paramref name="body"/> unchanged. Deterministic — the inverse of <see cref="Parse"/>.
    /// </summary>
    public static string Serialize(CatalogMetadata metadata, string body)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(body);

        if (metadata.IsEmpty)
        {
            return body;
        }

        var sb = new StringBuilder();
        if (metadata.Genre is not null)
        {
            sb.Append(GenreKey).Append(": ").Append(metadata.Genre).Append('\n');
        }

        if (metadata.Subgenre is not null)
        {
            sb.Append(SubgenreKey).Append(": ").Append(metadata.Subgenre).Append('\n');
        }

        if (metadata.Tags.Count > 0)
        {
            sb.Append(TagsKey).Append(": [").Append(string.Join(", ", metadata.Tags)).Append("]\n");
        }

        sb.Append(body);
        return sb.ToString();
    }

    /// <summary>
    /// Serialize <paramref name="tags"/> as the JSON array stored in the entity's <c>Tags TEXT</c> column
    /// (constraint C3). Round-trips 1:1 with the canonical header's <c>tags: [...]</c> list.
    /// </summary>
    public static string SerializeTags(IReadOnlyList<string> tags)
    {
        ArgumentNullException.ThrowIfNull(tags);
        return JsonSerializer.Serialize(tags);
    }

    /// <summary>Read the <c>Tags TEXT</c> JSON-array column back into a list (null/blank → empty).</summary>
    public static IReadOnlyList<string> DeserializeTags(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? Array.Empty<string>()
            : JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>();

    private static bool TryParseHeaderLine(string trimmed, out string? key, out string value)
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
        if (candidate is not (GenreKey or SubgenreKey or TagsKey))
        {
            return false;
        }

        key = candidate;
        value = trimmed[(colon + 1)..].Trim();
        return true;
    }

    private static IReadOnlyList<string> ParseTagList(string value)
    {
        string inner = value;
        if (inner.StartsWith('[') && inner.EndsWith(']'))
        {
            inner = inner[1..^1];
        }

        return inner
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }

    private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;
}
