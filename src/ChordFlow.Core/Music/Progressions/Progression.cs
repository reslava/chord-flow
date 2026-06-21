using ChordFlow.Music.Harmony;
using ChordFlow.Music.Rhythm;
namespace ChordFlow.Music.Progressions;

/// <summary>
/// A chord progression expressed as key-independent <see cref="HarmonicBar"/>s of <see cref="ChordSpan"/>s
/// (e.g. 12-bar blues), realized into concrete chords by the <see cref="Transposer"/>. A bar holds 1–4
/// chords on the 48-PPQ quarter grid; the harmonic-rhythm layer lives in the spans, the harmony stays a
/// pure <see cref="RomanDegree"/> (C1).
/// <para>
/// All construction funnels through the guarded factory <see cref="FromBars"/> (or the backward-compatible
/// degree constructor, which only ever produces valid single-span bars), so a malformed
/// <see cref="Progression"/> is unconstructable (IN4, decision Q2).
/// </para>
/// </summary>
public sealed record Progression
{
    public string Id { get; }

    public string Name { get; }

    /// <summary>The bars, in order. Each bar's spans sum to <see cref="TimeSignature.BarTicks"/>.</summary>
    public IReadOnlyList<HarmonicBar> Bars { get; }

    // Private full constructor: the only way bars reach the record. Public entry points either validate
    // (FromBars) or build provably-valid single-span bars (the degree constructor).
    private Progression(string id, string name, IReadOnlyList<HarmonicBar> bars)
    {
        Id = id;
        Name = name;
        Bars = bars;
    }

    /// <summary>
    /// Backward-compatible: one <see cref="RomanDegree"/> per bar, each realized as a single full-bar
    /// <see cref="ChordSpan"/> (<see cref="TimeSignature.BarTicks"/> ticks). A single-chord bar = one
    /// full-bar span (C4); this keeps today's 12-bar-blues seed and existing callers working unchanged.
    /// </summary>
    public Progression(string id, string name, IReadOnlyList<RomanDegree> degrees)
        : this(id, name, ToSingleSpanBars(degrees))
    {
    }

    /// <summary>
    /// Flattened one-degree-per-bar view (each bar's first span). Meaningful for single-span progressions;
    /// retained for backward compatibility with degree-oriented callers.
    /// </summary>
    public IReadOnlyList<RomanDegree> Degrees => Bars.Select(b => b.Spans[0].Degree).ToArray();

    /// <summary>
    /// Guarded factory (Q2). Validates every bar against <paramref name="ts"/>: spans must be non-empty,
    /// each <c>DurationTicks &gt; 0</c> and a multiple of <see cref="TimeSignature.BeatTicks"/> (v1
    /// quarter-aligned, C3), and sum exactly to <see cref="TimeSignature.BarTicks"/>. Throws
    /// <see cref="ArgumentException"/> naming the offending bar (1-based) so a malformed progression is
    /// unconstructable.
    /// </summary>
    public static Progression FromBars(
        string id, string name, IReadOnlyList<HarmonicBar> bars, TimeSignature ts)
    {
        ArgumentNullException.ThrowIfNull(bars);

        for (int i = 0; i < bars.Count; i++)
        {
            ValidateBar(bars[i], i + 1, ts);
        }

        return new Progression(id, name, bars);
    }

    private static void ValidateBar(HarmonicBar bar, int barNumber, TimeSignature ts)
    {
        ArgumentNullException.ThrowIfNull(bar);

        if (bar.Spans.Count == 0)
        {
            throw new ArgumentException($"Bar {barNumber} has no chord spans.", "bars");
        }

        int sum = 0;
        foreach (ChordSpan span in bar.Spans)
        {
            if (span.DurationTicks <= 0)
            {
                throw new ArgumentException(
                    $"Bar {barNumber} has a span with non-positive duration {span.DurationTicks}.", "bars");
            }

            if (span.DurationTicks % ts.BeatTicks != 0)
            {
                throw new ArgumentException(
                    $"Bar {barNumber} span duration {span.DurationTicks} is not quarter-aligned " +
                    $"(must be a multiple of {ts.BeatTicks} ticks).", "bars");
            }

            sum += span.DurationTicks;
        }

        if (sum != ts.BarTicks)
        {
            throw new ArgumentException(
                $"Bar {barNumber} spans sum to {sum} ticks, expected {ts.BarTicks}.", "bars");
        }
    }

    private static IReadOnlyList<HarmonicBar> ToSingleSpanBars(IReadOnlyList<RomanDegree> degrees)
    {
        ArgumentNullException.ThrowIfNull(degrees);

        int barTicks = TimeSignature.FourFour.BarTicks;
        var bars = new HarmonicBar[degrees.Count];
        for (int i = 0; i < degrees.Count; i++)
        {
            bars[i] = new HarmonicBar(new[] { new ChordSpan(degrees[i], barTicks) });
        }

        return bars;
    }
}
