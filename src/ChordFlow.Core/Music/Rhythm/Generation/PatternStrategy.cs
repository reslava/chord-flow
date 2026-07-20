namespace ChordFlow.Music.Rhythm.Generation;

/// <summary>
/// The pedagogical generation strategy: compose an <see cref="OnsetGrid"/> from a family + operator +
/// behaviour (design §3a). For each bar index the <see cref="SequenceBehaviour"/> yields that bar's
/// <see cref="OnsetBar"/> (choosing/parameterizing the base <see cref="BarOperator"/> and filling the four
/// beats from the <see cref="RhythmFamily"/>). Pure and deterministic given the seed.
/// </summary>
public static class PatternStrategy
{
    /// <summary>Generate the onset grid for <paramref name="p"/>. Throws if <c>BarCount</c> is outside 1–4.</summary>
    public static OnsetGrid Generate(PatternParams p)
    {
        ArgumentNullException.ThrowIfNull(p);
        if (p.BarCount is < 1 or > 4)
        {
            throw new ArgumentOutOfRangeException(
                nameof(p), p.BarCount, "Pattern BarCount must be between 1 and 4 (req IN3).");
        }

        var rng = new Random(p.Seed);
        int beatsPerBar = p.Ts.Numerator;
        var bars = new OnsetBar[p.BarCount];
        for (int i = 0; i < p.BarCount; i++)
        {
            bars[i] = p.Behaviour.BarAt(i, p.Operator, p.Family, beatsPerBar, rng);
        }

        return OnsetGrid.Of(bars, p.Ts);
    }
}
