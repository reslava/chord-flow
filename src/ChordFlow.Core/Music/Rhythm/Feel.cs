namespace ChordFlow.Music.Rhythm;

/// <summary>
/// Groove feel — a <b>playback-time timing transform</b>, never baked into a <see cref="RhythmPattern"/>
/// (ctx C4 / IN10). Straight is the un-warped grid; the others push the off-beat "and" later to give a
/// long-short swing. Applied by <see cref="FeelTransform"/>; the stored pattern stays straight.
/// </summary>
public enum Feel
{
    /// <summary>Even eighths — no warp.</summary>
    Straight,

    /// <summary>Triplet swing: the off-beat lands at 2/3 of the beat.</summary>
    Swing,

    /// <summary>Hard shuffle: the off-beat lands at 3/4 of the beat (dotted-eighth + sixteenth).</summary>
    Shuffle,

    /// <summary>Full triplet feel: the off-beat lands at 2/3 of the beat (shares swing's ratio for v1).</summary>
    Triplet,
}
