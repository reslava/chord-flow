namespace ChordFlow.Music.Rhythm.Generation;

/// <summary>
/// The <see cref="PatternStrategy"/> payload (design §3a v2): build <see cref="BarCount"/> bars (1–4) by
/// drawing bar patterns from <see cref="Kind"/> via <see cref="Selection"/>, then applying the ordered
/// <see cref="Behaviours"/> overlay (Displace / Sweep / RestBar / CallResponse) to each. The pedagogical
/// strategy — pick a kind, choose how bars are drawn, layer behaviours.
/// </summary>
public sealed record PatternParams(
    RhythmKind Kind,
    PatternSelection Selection,
    IReadOnlyList<SequenceBehaviour> Behaviours,
    int BarCount,
    TimeSignature Ts,
    int Seed) : GenerationParams(Ts, Seed);
