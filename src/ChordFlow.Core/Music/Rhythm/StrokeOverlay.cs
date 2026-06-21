namespace ChordFlow.Music.Rhythm;

/// <summary>
/// A composable stroke (pick-direction) overlay: assigns <see cref="Stroke"/>s onto a pattern's
/// events (ctx IN10). Like the accent overlay it returns a new list and never mutates the base.
/// </summary>
public static class StrokeOverlay
{
    /// <summary>Set every event to <paramref name="stroke"/>.</summary>
    public static IReadOnlyList<RhythmEvent> All(IReadOnlyList<RhythmEvent> events, Stroke stroke)
    {
        ArgumentNullException.ThrowIfNull(events);
        return events.Select(e => e with { Stroke = stroke }).ToArray();
    }

    /// <summary>Alternate down/up strokes by event order (down on even indices) — basic alternate picking.</summary>
    public static IReadOnlyList<RhythmEvent> AlternateDownUp(IReadOnlyList<RhythmEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        return events
            .Select((e, i) => e with { Stroke = i % 2 == 0 ? Stroke.Down : Stroke.Up })
            .ToArray();
    }
}
