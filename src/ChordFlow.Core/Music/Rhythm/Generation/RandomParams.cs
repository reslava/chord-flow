namespace ChordFlow.Music.Rhythm.Generation;

/// <summary>
/// The <see cref="RandomStrategy"/> payload: fill <see cref="ContentBars"/> bars by drawing note values from
/// <see cref="ValuePalette"/>, then append <see cref="SilenceBars"/> empty bars (design §3b). Values are
/// alphaTex note-value denominators — <c>4</c> quarter, <c>8</c> eighth, <c>16</c> sixteenth — placed on the
/// v1 sixteenth base grid; each must divide 16 (a whole number of base cells). Triplets are a later phase
/// (req EX3).
/// <para>
/// <see cref="RestProbability"/> (0..1, req IN12) interleaves <b>rests</b> among the onsets: at each step of
/// the fill the drawn value becomes a rest with this probability (advancing its duration with no attack), so
/// a rest is exactly the length of the value drawn — a quarter/eighth/16th rest. <c>0</c> = today's solid
/// fill; <c>1</c> = an empty bar. Beat 1 is not special-cased — it may rest like any slot (a downbeat
/// reference is the play-time reference pulse, not a generator onset).
/// </para>
/// </summary>
public sealed record RandomParams(
    IReadOnlyList<int> ValuePalette,
    int ContentBars,
    int SilenceBars,
    TimeSignature Ts,
    int Seed,
    double RestProbability = 0.0) : GenerationParams(Ts, Seed);
