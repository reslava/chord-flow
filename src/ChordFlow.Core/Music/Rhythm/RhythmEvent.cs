namespace ChordFlow.Music.Rhythm;

/// <summary>
/// A single positional event on the tick grid: a note/strum that starts at <paramref name="Position"/>
/// ticks from the bar start and lasts <paramref name="Length"/> ticks (both at <see cref="TickGrid.Ppq"/>).
/// Unlike the old sequential <c>Beat</c>, this can express syncopation, ties and accents because it
/// carries an absolute position rather than implying one from order.
/// </summary>
public readonly record struct RhythmEvent(int Position, int Length, Stroke Stroke, Accent Accent)
{
    /// <summary>A plain down-stroke, unaccented event — the common case for comping hits.</summary>
    public static RhythmEvent Hit(int position, int length) =>
        new(position, length, Stroke.Down, Accent.Normal);
}
