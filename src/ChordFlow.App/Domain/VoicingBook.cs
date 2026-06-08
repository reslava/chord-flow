namespace ChordFlow.Domain;

/// <summary>
/// Resolves a <see cref="Chord"/> to a <see cref="Voicing"/> by dispatching to the
/// <see cref="IVoicingStrategy"/> registered for a <see cref="Difficulty"/>. The MVP registers only
/// the Beginner shell shape (<see cref="BeginnerShellStrategy"/>); Intermediate/Advanced tiers slot
/// in by registering a strategy, without changing call sites.
/// </summary>
public static class VoicingBook
{
    private static readonly IReadOnlyDictionary<Difficulty, IVoicingStrategy> Strategies =
        new IVoicingStrategy[] { new BeginnerShellStrategy() }
            .ToDictionary(s => s.Difficulty);

    /// <summary>
    /// Returns the <see cref="Voicing"/> for <paramref name="chord"/> from the strategy registered
    /// for <paramref name="difficulty"/>. Throws for a difficulty with no authored strategy, or a
    /// quality the strategy does not cover.
    /// </summary>
    public static Voicing Lookup(Chord chord, Difficulty difficulty)
    {
        ArgumentNullException.ThrowIfNull(chord);

        if (!Strategies.TryGetValue(difficulty, out IVoicingStrategy? strategy))
        {
            throw new NotSupportedException(
                $"No voicing strategy is authored for the {difficulty} difficulty.");
        }

        return strategy.Voice(chord);
    }
}
