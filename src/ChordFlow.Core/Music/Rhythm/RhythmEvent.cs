namespace ChordFlow.Music.Rhythm;

/// <summary>
/// A single positional event on the tick grid: a note/strum that starts at <paramref name="Position"/>
/// ticks from the bar start and lasts <paramref name="Length"/> ticks (both at <see cref="TickGrid.Ppq"/>).
/// Unlike the old sequential <c>Beat</c>, this can express syncopation, ties and accents because it
/// carries an absolute position rather than implying one from order.
/// <para>
/// <see cref="TiedToNext"/> marks an <b>authored tie</b> (the Rhythm DSL <c>_</c> token): this note
/// continues into the next event as one sounding pitch with no re-attack. The quantizer turns it into a
/// <c>TiedToPrevious</c> slot on the following note.
/// </para>
/// </summary>
public readonly record struct RhythmEvent(int Position, int Length, Stroke Stroke, Accent Accent, bool TiedToNext = false)
{
    /// <summary>A plain down-stroke, unaccented event — the common case for comping hits.</summary>
    public static RhythmEvent Hit(int position, int length) =>
        new(position, length, Stroke.Down, Accent.Normal);
}
