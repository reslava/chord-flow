namespace ChordFlow.Music.Rhythm.Generation;

/// <summary>
/// The <see cref="RandomStrategy"/> payload: fill <see cref="ContentBars"/> bars by drawing note values from
/// <see cref="ValuePalette"/>, then append <see cref="SilenceBars"/> empty bars (design §3b). Values are
/// alphaTex note-value denominators — <c>4</c> quarter, <c>8</c> eighth, <c>16</c> sixteenth — placed on the
/// v1 sixteenth base grid; each must divide 16 (a whole number of base cells). Triplets are a later phase
/// (req EX3).
/// </summary>
public sealed record RandomParams(
    IReadOnlyList<int> ValuePalette,
    int ContentBars,
    int SilenceBars,
    TimeSignature Ts,
    int Seed) : GenerationParams(Ts, Seed);
