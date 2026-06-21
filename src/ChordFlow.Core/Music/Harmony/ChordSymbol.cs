namespace ChordFlow.Music.Harmony;

/// <summary>
/// Conventional chord-symbol spelling for display — e.g. <c>C</c>, <c>Am</c>, <c>G7</c>, <c>Cmaj7</c>.
/// The root is spelled through <see cref="NoteSpeller"/> against the key, so accidentals match the score's
/// key signature. Pure music theory (ctx C1) — the renderer consumes it for the alphaTex <c>{ch "…"}</c>
/// chord label and <c>\chord</c> diagram name.
/// </summary>
public static class ChordSymbol
{
    // Display suffix per quality (distinct from the DSL suffixes in VoicingDslWriter: "" not "maj", "m" not "min").
    private static readonly IReadOnlyDictionary<Quality, string> Suffixes =
        new Dictionary<Quality, string>
        {
            [Quality.Major] = "",
            [Quality.Minor] = "m",
            [Quality.Dominant7] = "7",
            [Quality.Major7] = "maj7",
            [Quality.Minor7] = "m7",
            [Quality.HalfDiminished7] = "m7b5",
            [Quality.Diminished] = "dim",
            [Quality.Diminished7] = "dim7",
            [Quality.Augmented] = "aug",
        };

    /// <summary>The display symbol for <paramref name="chord"/>, root spelled against <paramref name="key"/>.</summary>
    public static string Format(Chord chord, Key key)
    {
        ArgumentNullException.ThrowIfNull(chord);
        if (!Suffixes.TryGetValue(chord.Quality, out string? suffix))
        {
            throw new NotSupportedException($"No display symbol for quality {chord.Quality}.");
        }

        return NoteSpeller.Name(chord.Root, key) + suffix;
    }
}
