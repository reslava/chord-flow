namespace ChordFlow.Music.Rhythm;

/// <summary>
/// Triplet feel (aka. swing) — a <b>play-time</b> groove choice, never baked into a
/// <see cref="RhythmPattern"/> (ctx C4). Mirrors alphaTab's <c>TripletFeel</c> vocabulary so the engine
/// and the rendered alphaTex <c>\tf</c> directive speak one language. Swing is delegated to alphaTab via
/// <c>\tf</c> (see <see cref="Rendering.AlphaTexRenderer"/>); the stored pattern stays straight.
/// <para>
/// Wired/offered today: <see cref="None"/>, <see cref="Triplet8th"/>, <see cref="Triplet16th"/>. The
/// remaining members are <b>defined but not yet offered</b> in the UI — present so adding them later is a
/// data change, not an enum change.
/// </para>
/// </summary>
public enum TripletFeel
{
    /// <summary>No triplet feel — even, un-warped grid (alphaTex <c>\tf none</c>; emits no directive).</summary>
    None,

    /// <summary>Triplet-8th swing: a straight 8th pair plays as 2/3 + 1/3 of the beat (alphaTex <c>\tf triplet8th</c>).</summary>
    Triplet8th,

    /// <summary>Triplet-16th swing: the same long-short shape at the 16th level (alphaTex <c>\tf triplet16th</c>).</summary>
    Triplet16th,

    /// <summary>Dotted-8th feel (dotted-eighth + sixteenth). Reserved — defined but not yet offered.</summary>
    Dotted8th,

    /// <summary>Dotted-16th feel. Reserved — defined but not yet offered.</summary>
    Dotted16th,

    /// <summary>Scottish-8th (reverse, short-long "snap") feel. Reserved — defined but not yet offered.</summary>
    Scottish8th,

    /// <summary>Scottish-16th feel. Reserved — defined but not yet offered.</summary>
    Scottish16th,
}
