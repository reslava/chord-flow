using ChordFlow.Music.Harmony;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The synthetic identity scheme for engine-derived <c>automatic</c> voicing families
/// (shell-voicing-derivation, req IN5): <c>auto:{family}:{qualityToken}:{shape}</c> — e.g.
/// <c>auto:caged:dom7:E</c>, <c>auto:shell:maj7:C</c>, <c>auto:dshell:m7b5:D</c>. Stable, unique,
/// human-readable; shared by the listing source, the comping resolver, and the explicit per-chord voicing
/// reference. The family segment was added when shells joined the engine (the prior 3-segment form is gone).
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
        [Quality.Major6] = "6",
        [Quality.Minor6] = "m6",
    };

    private static readonly IReadOnlyDictionary<string, Quality> ByToken =
        Tokens.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.Ordinal);

    /// <summary>The quality token (e.g. <c>dom7</c>) used in a synthetic id.</summary>
    public static string Token(Quality quality) => Tokens[quality];

    /// <summary>The synthetic id for a family × quality × shape, e.g. <c>auto:shell:dom7:E</c>.</summary>
    public static string For(VoicingFamily family, Quality quality, CagedShape shape) =>
        $"{Prefix}:{family.Token()}:{Token(quality)}:{shape}";

    /// <summary>Parse a synthetic id back to its (family, quality, shape); false if it is not a well-formed <c>auto:…</c> id.</summary>
    public static bool TryParse(string id, out VoicingFamily family, out Quality quality, out CagedShape shape)
    {
        family = default;
        quality = default;
        shape = default;
        if (id is null)
        {
            return false;
        }

        string[] parts = id.Split(':');
        return parts.Length == 4
            && string.Equals(parts[0], Prefix, StringComparison.Ordinal)
            && VoicingFamilies.TryParse(parts[1], out family)
            && ByToken.TryGetValue(parts[2], out quality)
            && Enum.TryParse(parts[3], ignoreCase: false, out shape)
            && Enum.IsDefined(shape);
    }
}
