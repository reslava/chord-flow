namespace ChordFlow.Music.Harmony;

/// <summary>
/// The harmonic role a <see cref="ChordTone"/> plays within its chord. Classified from the
/// semitone interval so guide tones (3rd &amp; 7th) and the root/fifth fall out automatically.
/// </summary>
public enum ChordToneFunction
{
    Root,
    Third,
    Fifth,
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
