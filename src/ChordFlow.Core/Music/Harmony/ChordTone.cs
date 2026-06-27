namespace ChordFlow.Music.Harmony;

/// <summary>
/// The harmonic role a <see cref="ChordTone"/> plays within its chord. Classified from the quality's
/// <see cref="QualityFormulas">formula degree</see> (not the raw semitone), so an enharmonically ambiguous
/// pitch resolves by quality — semitone 9 is the <see cref="Seventh"/> (bb7) of a dim7 but the <see cref="Sixth"/>
/// (6) of a 6/m6 chord.
/// </summary>
public enum ChordToneFunction
{
    Root,
    Third,
    Fifth,
    Sixth,
    Seventh,
}

/// <summary>
/// A single tone of a chord, expressed <b>relative to the chord root</b> — distinct from a
/// key-relative <see cref="RomanDegree"/>. <paramref name="Interval"/> is the semitone distance
/// from the root (0 = root, 3/4 = third, 6/7/8 = fifth, 10/11 = seventh). Resolve to a concrete
/// <see cref="PitchClass"/> only when a root is supplied — spelling stays deferred (constraint C4).
/// </summary>
public readonly record struct ChordTone(int Interval, ChordToneFunction Function)
{
    /// <summary>The concrete pitch class of this tone when the chord root is <paramref name="root"/>.</summary>
    public PitchClass PitchClassFor(PitchClass root) =>
        new(((root.Value + Interval) % 12 + 12) % 12);
}
