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

        // Function is read from the formula's degree spelling (C6), not the semitone — so the enharmonically
        // ambiguous semitone 9 resolves by quality: degree "bb7" (dim7) → Seventh, degree "6" (6/m6) → Sixth.
        string[] tokens = QualityFormulas.Formula(chord.Quality)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var tones = new ChordTone[tokens.Length];
        for (int i = 0; i < tokens.Length; i++)
        {
            tones[i] = new ChordTone(IntervalSpeller.Parse(tokens[i]), Classify(IntervalSpeller.Degree(tokens[i])));
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

    // Map a formula scale-degree (accidental-stripped: 1/3/5/6/7) to its harmonic function. Degree, not
    // semitone, so the 6 and the bb7 — both at semitone 9 — separate cleanly. The current qualities only use
    // these degrees; an unsupported one (a 9th/11th/13th chord) would need its own function and falls through.
    private static ChordToneFunction Classify(int degree) => degree switch
    {
        1 => ChordToneFunction.Root,
        3 => ChordToneFunction.Third,
        5 => ChordToneFunction.Fifth,
        6 => ChordToneFunction.Sixth,
        7 => ChordToneFunction.Seventh,
        _ => throw new ArgumentOutOfRangeException(
            nameof(degree), degree, "Degree does not map to a chord-tone function."),
    };
}
