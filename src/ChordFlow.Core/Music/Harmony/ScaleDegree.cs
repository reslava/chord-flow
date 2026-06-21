namespace ChordFlow.Music.Harmony;

/// <summary>
/// A pure key-relative scale position (1 = I, 2 = ii, ... 7 = vii) with <b>no quality</b> —
/// the diatonic generator derives the quality from the scale. Distinct from <see cref="RomanDegree"/>,
/// which carries an explicit quality for authored progressions (e.g. all-Dominant7 blues), and from
/// the chord-relative <see cref="ChordTone"/> (see ctx IN4: two distinct degree reference frames).
/// </summary>
public readonly record struct ScaleDegree(int Number);
