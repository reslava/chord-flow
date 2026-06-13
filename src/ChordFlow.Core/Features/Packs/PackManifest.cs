using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChordFlow.Features.Packs;

/// <summary>
/// A pack's <c>manifest.json</c> (design §3): the pack's stable <see cref="Id"/> (stamped onto every
/// imported definition as its source pack), display <see cref="Name"/>, <see cref="Version"/>, coarse
/// <see cref="Kind"/> (the pack-type discriminator — <c>content</c> today; future <c>soundfont</c>/<c>theme</c>),
/// a free-text <see cref="Provenance"/> label, and <see cref="Requires"/> (other pack ids it depends on —
/// recorded but not yet resolved, per EX2). Parsing is pure (string → model); the directory walk lives in
/// <see cref="PackReader"/>.
/// </summary>
public sealed record PackManifest(
    string Id,
    string Name,
    string Version,
    string Kind,
    string Provenance,
    IReadOnlyList<string> Requires)
{
    /// <summary>The only pack <see cref="Kind"/> supported today.</summary>
    public const string ContentKindLabel = "content";

    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Parse a <c>manifest.json</c> string. <see cref="Id"/> is required (it becomes every imported row's
    /// <c>PackId</c>); a missing <see cref="Kind"/> defaults to <see cref="ContentKindLabel"/>; a missing
    /// <see cref="Requires"/> is an empty list. Throws <see cref="FormatException"/> on malformed JSON or a
    /// missing id.
    /// </summary>
    public static PackManifest Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        Dto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<Dto>(json, Options);
        }
        catch (JsonException ex)
        {
            throw new FormatException($"pack manifest is not valid JSON: {ex.Message}", ex);
        }

        if (dto is null)
        {
            throw new FormatException("pack manifest is empty.");
        }

        if (string.IsNullOrWhiteSpace(dto.Id))
        {
            throw new FormatException("pack manifest is missing required field 'id'.");
        }

        return new PackManifest(
            dto.Id,
            string.IsNullOrWhiteSpace(dto.Name) ? dto.Id : dto.Name,
            string.IsNullOrWhiteSpace(dto.Version) ? "0.0.0" : dto.Version,
            string.IsNullOrWhiteSpace(dto.Kind) ? ContentKindLabel : dto.Kind,
            dto.Provenance ?? "",
            dto.Requires ?? Array.Empty<string>());
    }

    // Mutable shape for System.Text.Json (tolerates missing/extra fields); mapped to the immutable record above.
    private sealed class Dto
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("version")] public string? Version { get; set; }
        [JsonPropertyName("kind")] public string? Kind { get; set; }
        [JsonPropertyName("provenance")] public string? Provenance { get; set; }
        [JsonPropertyName("requires")] public string[]? Requires { get; set; }
    }
}
