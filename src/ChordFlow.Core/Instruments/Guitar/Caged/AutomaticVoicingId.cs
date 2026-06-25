using ChordFlow.Music.Harmony;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The synthetic identity scheme for engine-derived <c>automatic</c> voicing families
/// (engine-derived-as-app-source, req IN3): <c>auto:{qualityToken}:{shape}</c> — e.g. <c>auto:dom7:E</c>,
/// <c>auto:maj7:A</c>, <c>auto:m7b5:D</c>. Stable, unique, human-readable; shared by the listing source, the
/// comping resolver, and (later) the explicit per-chord voicing reference (<c>{a: …}</c>).
/// </summary>
public static class AutomaticVoicingId
{
    /// <summary>The id namespace prefix (<c>auto</c>).</summary>
    public const string Prefix = "auto";

    private static readonly IReadOnlyDictionary<Quality, string> Tokens = new Dictionary<Quality, string>
    {
        [Quality.Major] = "maj",
        [Quality.Minor] = "min",
        [Quality.Dominant7] = "dom7",
        [Quality.Major7] = "maj7",
        [Quality.Minor7] = "min7",
        [Quality.HalfDiminished7] = "m7b5",
        [Quality.Diminished] = "dim",
        [Quality.Diminished7] = "dim7",
        [Quality.Augmented] = "aug",
    };

    private static readonly IReadOnlyDictionary<string, Quality> ByToken =
        Tokens.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.Ordinal);

    /// <summary>The quality token (e.g. <c>dom7</c>) used in a synthetic id.</summary>
    public static string Token(Quality quality) => Tokens[quality];

    /// <summary>The synthetic id for a quality×shape family, e.g. <c>auto:dom7:E</c>.</summary>
    public static string For(Quality quality, CagedShape shape) => $"{Prefix}:{Token(quality)}:{shape}";

    /// <summary>Parse a synthetic id back to its (quality, shape); false if it is not a well-formed <c>auto:…</c> id.</summary>
    public static bool TryParse(string id, out Quality quality, out CagedShape shape)
    {
        quality = default;
        shape = default;
        if (id is null)
        {
            return false;
        }

        string[] parts = id.Split(':');
        return parts.Length == 3
            && string.Equals(parts[0], Prefix, StringComparison.Ordinal)
            && ByToken.TryGetValue(parts[1], out quality)
            && Enum.TryParse(parts[2], ignoreCase: false, out shape)
            && Enum.IsDefined(shape);
    }
}
