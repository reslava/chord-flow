namespace ChordFlow.Music.Progressions;

/// <summary>
/// One bar of harmony: an ordered list of <see cref="ChordSpan"/>s whose durations partition the bar
/// (they sum to <see cref="TimeSignature.BarTicks"/> for a valid bar — enforced by
/// <see cref="Progression.FromBars"/>). A bar holds 1–4 chords on the 48-PPQ quarter grid; a single-chord
/// bar is one full-bar span (C4). The harmonic-rhythm layer — the rate chords change — lives here, kept
/// separate from the strum/articulation <see cref="RhythmPattern"/>.
/// </summary>
public sealed record HarmonicBar(IReadOnlyList<ChordSpan> Spans)
{
    /// <summary>Total ticks covered by every span (equals <see cref="TimeSignature.BarTicks"/> when valid).</summary>
    public int TotalTicks => Spans.Sum(s => s.DurationTicks);

    /// <summary>
    /// The span whose half-open tick range <c>[start, start + DurationTicks)</c> contains
    /// <paramref name="tick"/> — the primitive the renderer uses to map a rhythm slot to its chord.
    /// Throws <see cref="ArgumentOutOfRangeException"/> if no span covers the tick.
    /// </summary>
    public ChordSpan SpanCovering(int tick)
    {
        int start = 0;
        foreach (ChordSpan span in Spans)
        {
            int end = start + span.DurationTicks;
            if (tick >= start && tick < end)
            {
                return span;
            }

            start = end;
        }

        throw new ArgumentOutOfRangeException(
            nameof(tick), tick, $"No chord span covers tick {tick} (bar is {TotalTicks} ticks).");
    }
}
