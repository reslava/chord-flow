namespace ChordFlow.Domain;

/// <summary>
/// One realized chord span: a concrete <see cref="Chord"/> placed at <see cref="StartTick"/> for
/// <see cref="DurationTicks"/> on the 48-PPQ grid. The key-resolved counterpart of <see cref="ChordSpan"/>.
/// </summary>
public readonly record struct RealizedSpan(Chord Chord, int StartTick, int DurationTicks);

/// <summary>
/// One realized bar: the ordered <see cref="RealizedSpan"/>s for a <see cref="HarmonicBar"/> after
/// transposition into a key. <see cref="ChordCovering"/> is the renderer's slot → chord primitive.
/// </summary>
public sealed record RealizedBar(IReadOnlyList<RealizedSpan> Spans)
{
    /// <summary>The chord whose span's <c>[StartTick, StartTick + DurationTicks)</c> contains <paramref name="tick"/>.</summary>
    public Chord ChordCovering(int tick)
    {
        foreach (RealizedSpan span in Spans)
        {
            if (tick >= span.StartTick && tick < span.StartTick + span.DurationTicks)
            {
                return span.Chord;
            }
        }

        throw new ArgumentOutOfRangeException(
            nameof(tick), tick, "No realized chord span covers the tick.");
    }
}

/// <summary>
/// Pure transposition: realizes a key-independent <see cref="Progression"/> into concrete
/// <see cref="Chord"/>s for a given <see cref="Key"/> (or <see cref="Scale"/>). No I/O, no state.
/// The scale-degree offsets live in <see cref="Scale"/>; this type just maps degrees through it.
/// </summary>
public static class Transposer
{
    /// <summary>
    /// Legacy one-chord-per-bar view: maps each bar's first <see cref="ChordSpan"/> to a concrete chord
    /// in <paramref name="key"/>. Exact for single-span bars (today's progressions); multi-chord bars are
    /// realized span-by-span via <see cref="RealizeBars(Progression, Key)"/>, which the renderer uses.
    /// </summary>
    public static Chord[] Realize(Progression progression, Key key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return Realize(progression, Scale.ForKey(key));
    }

    /// <summary>
    /// Legacy one-chord-per-bar view in <paramref name="scale"/> (see <see cref="Realize(Progression, Key)"/>).
    /// </summary>
    public static Chord[] Realize(Progression progression, Scale scale)
    {
        ArgumentNullException.ThrowIfNull(progression);
        ArgumentNullException.ThrowIfNull(scale);

        var chords = new Chord[progression.Bars.Count];
        for (int i = 0; i < progression.Bars.Count; i++)
        {
            RomanDegree degree = progression.Bars[i].Spans[0].Degree;
            chords[i] = ChordFor(degree, scale);
        }

        return chords;
    }

    /// <summary>
    /// Realizes the full harmonic-rhythm layer: every <see cref="ChordSpan"/> of every bar to a concrete
    /// chord with its bar-relative <see cref="RealizedSpan.StartTick"/> and duration, in <paramref name="key"/>.
    /// </summary>
    public static IReadOnlyList<RealizedBar> RealizeBars(Progression progression, Key key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return RealizeBars(progression, Scale.ForKey(key));
    }

    /// <summary>Realizes the full harmonic-rhythm layer in <paramref name="scale"/>.</summary>
    public static IReadOnlyList<RealizedBar> RealizeBars(Progression progression, Scale scale)
    {
        ArgumentNullException.ThrowIfNull(progression);
        ArgumentNullException.ThrowIfNull(scale);

        var bars = new RealizedBar[progression.Bars.Count];
        for (int i = 0; i < progression.Bars.Count; i++)
        {
            HarmonicBar bar = progression.Bars[i];
            var spans = new RealizedSpan[bar.Spans.Count];
            int start = 0;
            for (int j = 0; j < bar.Spans.Count; j++)
            {
                ChordSpan span = bar.Spans[j];
                spans[j] = new RealizedSpan(ChordFor(span.Degree, scale), start, span.DurationTicks);
                start += span.DurationTicks;
            }

            bars[i] = new RealizedBar(spans);
        }

        return bars;
    }

    // The root pitch class is the scale degree's pitch class; the quality carries straight through from
    // the degree (e.g. Dominant7 for blues).
    private static Chord ChordFor(RomanDegree degree, Scale scale) =>
        new(scale.DegreePitchClass(degree.Degree), degree.Quality);
}
