namespace ChordFlow.Music.Harmony;

/// <summary>
/// A letter-pure spelled note: a base letter (<c>A</c>..<c>G</c>) plus a signed accidental count
/// (0 = natural, +1 = <c>#</c>, +2 = <c>##</c>, -1 = <c>b</c>, -2 = <c>bb</c>). Unlike a bare
/// <see cref="PitchClass"/> it commits to a spelling, so <c>Fb</c> and <c>E</c> — same pitch — are
/// distinct values. Produced by the transposer for chromatically-altered degrees and consumed by
/// <see cref="ChordSymbol"/> so the written degree, not the key, names the root.
/// </summary>
public readonly record struct NoteName(char Letter, int Accidental)
{
    /// <summary>The display spelling, e.g. <c>F</c>, <c>F#</c>, <c>Bb</c>, <c>B#</c>, <c>Bbb</c>.</summary>
    public string Symbol =>
        Letter + (Accidental >= 0 ? new string('#', Accidental) : new string('b', -Accidental));
}
