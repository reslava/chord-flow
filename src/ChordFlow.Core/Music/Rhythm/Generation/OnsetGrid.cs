namespace ChordFlow.Music.Rhythm.Generation;

/// <summary>
/// The generator's <b>only output type</b>: a sequence of <see cref="OnsetBar"/>s at one
/// <see cref="TimeSignature"/> — pure attack positions, no durations, no instrument, no pitch (design §1).
/// A projection turns it into the concrete play-unit: <see cref="OnsetGridToRhythmPattern"/> (comping/lead)
/// or the single-lane drum projection (Instruments/Drums). The grid tiles cyclically onto a progression the
/// same way a multi-bar <see cref="RhythmPattern"/> does.
/// </summary>
public sealed record OnsetGrid
{
    private OnsetGrid(IReadOnlyList<OnsetBar> bars, TimeSignature ts)
    {
        Bars = bars;
        TimeSignature = ts;
    }

    /// <summary>The bars, in play order (≥ 1).</summary>
    public IReadOnlyList<OnsetBar> Bars { get; }

    /// <summary>The meter (4/4 in v1).</summary>
    public TimeSignature TimeSignature { get; }

    /// <summary>
    /// Build a grid from <paramref name="bars"/> in <paramref name="ts"/>. Requires ≥ 1 bar and that every
    /// bar has exactly <c>ts.Numerator</c> beats (one <see cref="Block"/> per beat). Fail loud otherwise.
    /// </summary>
    public static OnsetGrid Of(IReadOnlyList<OnsetBar> bars, TimeSignature ts)
    {
        if (bars is null || bars.Count == 0)
        {
            throw new ArgumentException("An onset grid needs at least one bar.", nameof(bars));
        }

        for (int i = 0; i < bars.Count; i++)
        {
            if (bars[i].Beats.Count != ts.Numerator)
            {
                throw new ArgumentException(
                    $"Bar {i} has {bars[i].Beats.Count} beats, expected {ts.Numerator} for {ts.Numerator}/{ts.Denominator}.",
                    nameof(bars));
            }
        }

        return new OnsetGrid(bars, ts);
    }
}
