namespace ChordFlow.Domain;

/// <summary>
/// A composable accent overlay: marks events that fall on the given 0-based beat indices as
/// <see cref="Accent.Accented"/> (ctx IN10). Separate from timing — it layers onto a pattern's events
/// and returns a new list, never mutating the base. The classic example is the backbeat (beats 2 &amp; 4).
/// </summary>
public sealed record AccentPattern(IReadOnlyList<int> AccentedBeats)
{
    /// <summary>Backbeat: accents on beats 2 and 4 (indices 1 and 3).</summary>
    public static readonly AccentPattern Backbeat = new(new[] { 1, 3 });

    /// <summary>
    /// Return a new event list with <see cref="Accent.Accented"/> on events whose beat is in
    /// <see cref="AccentedBeats"/>; other events keep their existing accent (the overlay only adds).
    /// </summary>
    public IReadOnlyList<RhythmEvent> Apply(IReadOnlyList<RhythmEvent> events, TimeSignature timeSignature)
    {
        ArgumentNullException.ThrowIfNull(events);
        var accented = AccentedBeats.ToHashSet();
        int beat = timeSignature.BeatTicks;

        return events
            .Select(e => accented.Contains(e.Position / beat) ? e with { Accent = Accent.Accented } : e)
            .ToArray();
    }
}
