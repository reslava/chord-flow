namespace ChordFlow.Music.Rhythm.Generation;

/// <summary>
/// The pedagogical generation strategy (design §3a v2): compose an <see cref="OnsetGrid"/> from a kind of
/// bar patterns. For each bar the <see cref="PatternSelection"/> draws a pattern from the
/// <see cref="RhythmKind"/>, then each <see cref="SequenceBehaviour"/> overlay is applied in order. Pure and
/// deterministic given the seed.
/// </summary>
public static class PatternStrategy
{
    /// <summary>Generate the onset grid for <paramref name="p"/>. Throws on an out-of-range BarCount or empty kind.</summary>
    public static OnsetGrid Generate(PatternParams p)
    {
        ArgumentNullException.ThrowIfNull(p);
        if (p.BarCount is < 1 or > 4)
        {
            throw new ArgumentOutOfRangeException(
                nameof(p), p.BarCount, "Pattern BarCount must be between 1 and 4 (req IN3).");
        }

        if (p.Kind.Patterns.Count == 0)
        {
            throw new ArgumentException("The kind has no bar patterns.", nameof(p));
        }

        var rng = new Random(p.Seed);
        int beatsPerBar = p.Ts.Numerator;
        var bars = new OnsetBar[p.BarCount];
        for (int i = 0; i < p.BarCount; i++)
        {
            OnsetBar bar = p.Selection.BarAt(i, p.Kind, rng);
            foreach (SequenceBehaviour behaviour in p.Behaviours)
            {
                bar = behaviour.Apply(i, bar, beatsPerBar);
            }

            bars[i] = bar;
        }

        return OnsetGrid.Of(bars, p.Ts);
    }
}
