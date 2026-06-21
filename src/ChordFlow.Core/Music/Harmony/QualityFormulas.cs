namespace ChordFlow.Music.Harmony;

/// <summary>
/// The authoritative quality → interval-formula table: each <see cref="Quality"/> mapped to its
/// formula in degree+accidental spelling (e.g. dim7 = <c>"1 b3 b5 bb7"</c>). This is the single
/// source of truth for what notes a quality contains — the <b>only</b> authored chord-content data.
/// The semitone projection (<see cref="QualityIntervals"/>) is <i>derived</i> from these formulas
/// via <see cref="IntervalSpeller.ParseSet"/>, never stored alongside them, so the two can never
/// drift. The formula form is the musician's spelling and extends additively to richer qualities.
/// </summary>
public static class QualityFormulas
{
    // The only authored chord-content data. Tokens are root-up so the derived semitones stay
    // root-up — the position property (index i = 1st/3rd/5th/7th) that ChordTones relies on.
    private static readonly IReadOnlyDictionary<Quality, string> Table = new Dictionary<Quality, string>
    {
        [Quality.Major] = "1 3 5",
        [Quality.Minor] = "1 b3 5",
        [Quality.Dominant7] = "1 3 5 b7",
        [Quality.Major7] = "1 3 5 7",
        [Quality.Minor7] = "1 b3 5 b7",
        [Quality.HalfDiminished7] = "1 b3 b5 b7",
        [Quality.Diminished] = "1 b3 b5",
        [Quality.Diminished7] = "1 b3 b5 bb7",
        [Quality.Augmented] = "1 3 #5",
    };

    /// <summary>The interval formula (degree+accidental spelling, root-up) of <paramref name="quality"/>.</summary>
    public static string Formula(Quality quality)
    {
        if (!Table.TryGetValue(quality, out string? formula))
        {
            throw new ArgumentOutOfRangeException(
                nameof(quality), quality, "No interval formula is defined for this quality.");
        }

        return formula;
    }
}
