using ChordFlow.Instruments.Drums;
using ChordFlow.Music.Rhythm;

namespace ChordFlow.Exercises;

/// <summary>
/// One instrument's contribution to an <see cref="Exercise"/> — the typed play-unit part
/// (<c>drums-under-a-song</c> D1/IN1). A part carries its own <b>mix</b> (<see cref="Volume"/>,
/// <see cref="Muted"/>); the shared harmonic + time context (key/tempo/feel/difficulty) stays on the
/// <see cref="Exercise"/>. The union is deliberately <b>typed</b> rather than a stringly bag: each arm's
/// content shape differs — a comped/lead <see cref="RhythmPattern"/> voiced against the harmony vs a
/// harmony-independent <see cref="DrumGroove"/> — and a new instrument is a new arm, never a play-unit
/// remodel (a future <c>BassPart</c> slots in here). The union lives in <c>Exercises/</c>, outside the
/// instrument-agnostic <c>Music/</c> kernel; the renderer is handed the extracted typed pieces, never the union.
/// </summary>
public abstract record InstrumentPart
{
    /// <summary>Linear playback volume for this part's track (1.0 = unattenuated).</summary>
    public double Volume { get; init; } = 1.0;

    /// <summary>Silence this part's audio without hiding its staff — staff visibility is a separate display toggle.</summary>
    public bool Muted { get; init; }
}

/// <summary>
/// The rhythm-guitar comping part — voiced against the harmony at render. Exactly one per exercise
/// (the harmony must be comped; enforced fail-loud by <see cref="Exercise.Comping"/>).
/// </summary>
public sealed record CompingPart(RhythmPattern Pattern) : InstrumentPart;

/// <summary>The optional lead part (v1 renders as dead notes); at most one per exercise.</summary>
public sealed record LeadPart(RhythmPattern Pattern) : InstrumentPart;

/// <summary>
/// The optional drum part — a self-contained <see cref="DrumGroove"/> tiled cyclically beneath the harmony
/// (<c>drums-under-a-song</c>); at most one per exercise.
/// </summary>
public sealed record DrumPart(DrumGroove Groove) : InstrumentPart;
