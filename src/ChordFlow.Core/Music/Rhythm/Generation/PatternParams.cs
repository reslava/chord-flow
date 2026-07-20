namespace ChordFlow.Music.Rhythm.Generation;

/// <summary>
/// The <see cref="PatternStrategy"/> payload: build <see cref="BarCount"/> bars (1–4) by applying
/// <see cref="Behaviour"/> over the base <see cref="Operator"/> and the block <see cref="Family"/> (design
/// §3a). The pedagogical strategy — pick a family + operator + behaviour and the bars teach one metric axis.
/// </summary>
public sealed record PatternParams(
    RhythmFamily Family,
    BarOperator Operator,
    SequenceBehaviour Behaviour,
    int BarCount,
    TimeSignature Ts,
    int Seed) : GenerationParams(Ts, Seed);
