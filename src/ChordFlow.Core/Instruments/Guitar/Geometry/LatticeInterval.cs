namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The unfolded, octave-aware description of a signed semitone <see cref="Semitones"/> distance on the
/// fretboard — the "view" peer of the canonical integer. <see cref="Label"/> is the magnitude's name
/// (<c>1, b2 … 8, 9 … 15</c>) from <see cref="IntervalSpeller"/>, <see cref="Octaves"/>
/// is how many full octaves the magnitude spans, and <see cref="Direction"/> is its sign
/// (+1 ascending, −1 descending, 0 unison).
/// </summary>
public readonly record struct LatticeInterval(int Semitones, string Label, int Octaves, int Direction);
