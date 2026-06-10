namespace ChordFlow.Domain;

/// <summary>
/// A bar of rhythm as positional events on the tick grid. Holds <b>only timing</b> — chords,
/// voicings and lead targets are separate layers applied onto it. <see cref="Events"/> describe one
/// bar of <see cref="TimeSignature"/>; an optional <see cref="Pickup"/> is a shorter leading measure
/// (anacrusis). Feel is never stored here — it is a playback-time transform (ctx C4).
/// </summary>
public sealed record RhythmPattern(
    string Id,
    string Name,
    IReadOnlyList<RhythmEvent> Events,
    TimeSignature TimeSignature,
    PickupMeasure? Pickup = null);
