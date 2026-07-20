namespace ChordFlow.Music.Rhythm.Generation;

/// <summary>
/// The base of a rhythm-generation request — the discriminated union over the strategy payloads
/// (<see cref="PatternParams"/> / <see cref="RandomParams"/>). Carries the two shared inputs: the meter
/// (<see cref="Ts"/>, 4/4 in v1) and the <see cref="Seed"/> that makes every generation reproducible — a
/// generation is fully described by <c>{ strategy, params, seed }</c> (req IN6/C7), so the output can be
/// ephemeral now yet saveable later. <see cref="RhythmGenerator.Generate"/> dispatches on the runtime arm.
/// </summary>
public abstract record GenerationParams(TimeSignature Ts, int Seed);
