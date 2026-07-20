namespace ChordFlow.Music.Rhythm.Generation;

/// <summary>
/// The single entry point of the rhythm generation engine: turn a <see cref="GenerationParams"/> request
/// into an <see cref="OnsetGrid"/> by dispatching on its strategy arm (design §3). Pure and
/// <b>deterministic</b> — the same <c>{ strategy, params, seed }</c> always yields the same grid (req IN6/C7).
/// </summary>
public static class RhythmGenerator
{
    /// <summary>Generate the onset grid for <paramref name="p"/>.</summary>
    public static OnsetGrid Generate(GenerationParams p)
    {
        ArgumentNullException.ThrowIfNull(p);
        return p switch
        {
            PatternParams pattern => PatternStrategy.Generate(pattern),
            RandomParams random => RandomStrategy.Generate(random),
            _ => throw new ArgumentException(
                $"Unknown generation strategy: {p.GetType().Name}.", nameof(p)),
        };
    }
}
