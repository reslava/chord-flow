using ChordFlow.Music.Harmony;
namespace ChordFlow.Music.Progressions;

/// <summary>
/// One chord occupying a contiguous span of a <see cref="HarmonicBar"/>, measured on the fixed
/// 48-PPQ <see cref="TickGrid"/> (<see cref="DurationTicks"/>). The harmony stays a pure, key-independent
/// <see cref="RomanDegree"/> (constraint C1) — timing lives only here, in the harmonic-rhythm layer,
/// never on the degree. A single-chord bar is one full-bar span (<c>DurationTicks == BarTicks</c>, C4).
/// </summary>
/// <param name="Degree">The key-independent chord.</param>
/// <param name="DurationTicks">The span length on the 48-PPQ tick grid.</param>
/// <param name="VoicingAnnotation">The optional per-chord <c>{…}</c> voicing annotation — the <b>raw
/// inner text</b> of the brace (a voicing-spec), kept opaque here so Music stays instrument-agnostic
/// (design D9); the Features layer parses it. Only ever set when the progression is parsed inline in a
/// Song (the purity guard rejects it on a stored progression — req <c>IN7</c>).</param>
public readonly record struct ChordSpan(RomanDegree Degree, int DurationTicks, string? VoicingAnnotation = null);
