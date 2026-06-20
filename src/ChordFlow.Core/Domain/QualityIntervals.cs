namespace ChordFlow.Domain;

/// <summary>
/// The single source of truth for what notes a <see cref="Quality"/> contains: each quality
/// maps to its semitone intervals from the chord root (ctx constraint C5). Everything chord-tone
/// derived — voicings, guide tones, lead targets — reads from here instead of hand-authoring
/// pitch content per chord.
/// </summary>
public static class QualityIntervals
{
    // Semitones are a DERIVED projection, not authored here: each quality's formula
    // (QualityFormulas — the single source of truth, in degree+accidental spelling) is parsed
    // to its semitones via IntervalSpeller once at static init. Root-up token order is preserved,
    // so position i still reads as 1st / 3rd / 5th / 7th. Exactly one authored value per quality.
    private static readonly IReadOnlyDictionary<Quality, int[]> Table =
        Enum.GetValues<Quality>().ToDictionary(
            q => q,
            q => IntervalSpeller.ParseSet(QualityFormulas.Formula(q)).ToArray());

    /// <summary>The semitone intervals from the root that make up <paramref name="quality"/>.</summary>
    public static IReadOnlyList<int> Intervals(Quality quality)
    {
        if (!Table.TryGetValue(quality, out int[]? intervals))
        {
            throw new ArgumentOutOfRangeException(
                nameof(quality), quality, "No interval set is defined for this quality.");
        }

        return intervals;
    }

    /// <summary>
    /// The reverse lookup: the v1 <see cref="Quality"/> whose interval set equals
    /// <paramref name="intervals"/> (order-independent). Used by the diatonic generator to name
    /// a stack of scale thirds. Throws if no v1 quality matches.
    /// </summary>
    public static Quality FromIntervals(IReadOnlyCollection<int> intervals)
    {
        ArgumentNullException.ThrowIfNull(intervals);
        var set = intervals.ToHashSet();

        foreach (KeyValuePair<Quality, int[]> entry in Table)
        {
            if (entry.Value.Length == set.Count && set.SetEquals(entry.Value))
            {
                return entry.Key;
            }
        }

        throw new ArgumentException(
            $"No v1 quality matches the interval set {{{string.Join(",", intervals)}}}.", nameof(intervals));
    }
}
