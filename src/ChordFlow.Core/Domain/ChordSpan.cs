namespace ChordFlow.Domain;

/// <summary>
/// One chord occupying a contiguous span of a <see cref="HarmonicBar"/>, measured on the fixed
/// 48-PPQ <see cref="TickGrid"/> (<see cref="DurationTicks"/>). The harmony stays a pure, key-independent
/// <see cref="RomanDegree"/> (constraint C1) — timing lives only here, in the harmonic-rhythm layer,
/// never on the degree. A single-chord bar is one full-bar span (<c>DurationTicks == BarTicks</c>, C4).
/// </summary>
public readonly record struct ChordSpan(RomanDegree Degree, int DurationTicks);
