namespace ChordFlow.Domain;

/// <summary>
/// The single source of truth for what notes a <see cref="Quality"/> contains: each quality
/// maps to its semitone intervals from the chord root (ctx constraint C5). Everything chord-tone
/// derived — voicings, guide tones, lead targets — reads from here instead of hand-authoring
/// pitch content per chord.
/// </summary>
public static class QualityIntervals
{
    // Semitone intervals from the root for each v1 quality. Ordered root-up so the position
    // in the set reads as 1st / 3rd / 5th / 7th of the chord.
    private static readonly IReadOnlyDictionary<Quality, int[]> Table = new Dictionary<Quality, int[]>
    {
        [Quality.Major] = new[] { 0, 4, 7 },
        [Quality.Minor] = new[] { 0, 3, 7 },
        [Quality.Dominant7] = new[] { 0, 4, 7, 10 },
        [Quality.Major7] = new[] { 0, 4, 7, 11 },
        [Quality.Minor7] = new[] { 0, 3, 7, 10 },
        [Quality.HalfDiminished7] = new[] { 0, 3, 6, 10 },
        [Quality.Diminished] = new[] { 0, 3, 6 },
        [Quality.Diminished7] = new[] { 0, 3, 6, 9 },
        [Quality.Augmented] = new[] { 0, 4, 8 },
    };

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
