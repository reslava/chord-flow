namespace ChordFlow.Music.Harmony;

/// <summary>
/// Generates the chord-relative <see cref="ChordTone"/>s of a <see cref="Chord"/> from the
/// quality interval set (<see cref="QualityIntervals"/>). This is the bridge between the harmony
/// layer and the lead/voicing layers: "the b7 of G7" is computed (root + 10), never stored.
/// </summary>
public static class ChordTones
{
    /// <summary>
    /// The ordered chord tones of <paramref name="chord"/>, each tagged with its harmonic
    /// <see cref="ChordToneFunction"/> (root, third, fifth, seventh).
    /// </summary>
    public static IReadOnlyList<ChordTone> Of(Chord chord)
    {
        ArgumentNullException.ThrowIfNull(chord);

        IReadOnlyList<int> intervals = QualityIntervals.Intervals(chord.Quality);
        var tones = new ChordTone[intervals.Count];
        for (int i = 0; i < intervals.Count; i++)
        {
            tones[i] = new ChordTone(intervals[i], Classify(intervals[i]));
        }

        return tones;
    }

    /// <summary>
    /// The concrete pitch classes of <paramref name="chord"/>'s tones, in root-up order.
    /// </summary>
    public static IReadOnlyList<PitchClass> PitchClassesOf(Chord chord)
    {
        ArgumentNullException.ThrowIfNull(chord);
        return Of(chord).Select(t => t.PitchClassFor(chord.Root)).ToArray();
    }

    // Map a semitone interval to its harmonic function. The v1 qualities (C5) only ever place
    // a tone in one of these bands, so the classification is unambiguous:
    //   0 → root · 3/4 → third · 6/7/8 → fifth (dim/perfect/aug) · 9/10/11 → seventh (bb7/b7/maj7).
    private static ChordToneFunction Classify(int interval) => interval switch
    {
        0 => ChordToneFunction.Root,
        3 or 4 => ChordToneFunction.Third,
        6 or 7 or 8 => ChordToneFunction.Fifth,
        9 or 10 or 11 => ChordToneFunction.Seventh,
        _ => throw new ArgumentOutOfRangeException(
            nameof(interval), interval, "Interval does not map to a v1 chord-tone function."),
    };
}
