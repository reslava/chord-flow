namespace ChordFlow.Domain;

/// <summary>
/// One bar of rhythm as positional events on the tick grid — the building block of a
/// <see cref="RhythmPattern"/>. Holds <b>only timing</b>; chords, voicings, accents and strokes are
/// separate layers applied onto it.
/// </summary>
public sealed record PatternBar(IReadOnlyList<RhythmEvent> Events);

/// <summary>
/// A rhythm pattern as one or more <see cref="PatternBar"/>s on the tick grid. Holds <b>only timing</b> —
/// chords, voicings and lead targets are separate layers applied onto it. Each <see cref="PatternBar"/>
/// spans one bar of <see cref="TimeSignature"/>; an optional <see cref="Pickup"/> is a shorter leading
/// measure (anacrusis). The type is <b>multi-bar from the start</b> (durable shape): a single-bar pattern
/// is one <see cref="PatternBar"/> — use <see cref="SingleBar"/> for that common case — so multi-bar
/// becomes an additive feature, never a breaking refactor. Richer pattern↔progression alignment (fills,
/// divisibility) is owned by the <c>domain/multi-bar</c> thread; the v1 default is cyclic tiling. Feel is
/// never stored here — it is a playback-time transform (ctx C4).
/// </summary>
public sealed record RhythmPattern(
    string Id,
    string Name,
    IReadOnlyList<PatternBar> Bars,
    TimeSignature TimeSignature,
    PickupMeasure? Pickup = null)
{
    /// <summary>
    /// Construct a single-bar pattern from a flat event list — the common case while multi-bar authoring
    /// is still additive. Equivalent to one <see cref="PatternBar"/> wrapping <paramref name="events"/>.
    /// </summary>
    public static RhythmPattern SingleBar(
        string id,
        string name,
        IReadOnlyList<RhythmEvent> events,
        TimeSignature timeSignature,
        PickupMeasure? pickup = null) =>
        new(id, name, new[] { new PatternBar(events) }, timeSignature, pickup);
}
