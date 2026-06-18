using ChordFlow.Domain;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The authored-voicing library plus generated fallback. Built with the stored <see cref="VoicingShape"/>
/// set (read at the feature seam — the book itself stays pure, ctx C1), it answers two questions:
/// <list type="bullet">
/// <item><see cref="Candidates"/> — every authored voicing for a chord, realized and ranked (the
///   selection UI's source; may be empty).</item>
/// <item><see cref="Lookup"/> — the single voicing to play: the top candidate, else the strategy-generated
///   shape (the renderer's source).</item>
/// </list>
/// Stored authored voicings <b>shadow</b> generated ones — the same "stored shadows generated" rule as
/// the song layer. Matching is <b>exact-quality</b>; <c>maj7</c> never silently returns <c>maj</c> (the
/// <c>QualitySimplifier</c> reduction is a separate, deferred opt-in).
/// </summary>
public sealed class VoicingBook
{
    private static readonly IReadOnlyDictionary<Difficulty, IVoicingStrategy> DefaultStrategies =
        new IVoicingStrategy[] { new BeginnerShellStrategy() }.ToDictionary(s => s.Difficulty);

    private readonly IReadOnlyList<VoicingShape> _stored;
    private readonly IReadOnlyDictionary<Difficulty, IVoicingStrategy> _strategies;

    /// <summary>Build a book over <paramref name="stored"/> with the default strategy registry (Beginner shell).</summary>
    public VoicingBook(IReadOnlyList<VoicingShape> stored)
        : this(stored, DefaultStrategies)
    {
    }

    /// <summary>Build a book over <paramref name="stored"/> with an explicit strategy registry (test seam).</summary>
    public VoicingBook(IReadOnlyList<VoicingShape> stored, IReadOnlyDictionary<Difficulty, IVoicingStrategy> strategies)
    {
        ArgumentNullException.ThrowIfNull(stored);
        ArgumentNullException.ThrowIfNull(strategies);
        _stored = stored;
        _strategies = strategies;
    }

    /// <summary>
    /// Every authored voicing for <paramref name="chord"/>, ranked: exact-quality stored entries realized
    /// to the chord's root, kept playable (0..15), ordered by neck position then CAGED familiarity. Empty
    /// when no stored entry matches. <paramref name="difficulty"/> is reserved for the deferred
    /// difficulty-band narrowing (req EX6); it does not filter in slice 1.
    /// </summary>
    public IReadOnlyList<Voicing> Candidates(Chord chord, Difficulty difficulty)
    {
        ArgumentNullException.ThrowIfNull(chord);

        return _stored
            .Where(s => s.Quality == chord.Quality)
            .Select(s => (s.Shape, Voicing: s.Realize(chord.Root)))
            .Where(t => t.Voicing is not null)
            .OrderBy(t => LowestFret(t.Voicing!))
            .ThenBy(t => t.Shape.FamiliarityRank())
            .Select(t => t.Voicing!)
            .ToList();
    }

    /// <summary>
    /// The single voicing to play for <paramref name="chord"/>: the top <see cref="Candidates"/> entry if
    /// any authored voicing exists, otherwise the strategy-generated shape for <paramref name="difficulty"/>.
    /// Throws <see cref="NotSupportedException"/> when neither a stored voicing nor a strategy covers the
    /// chord (fail-loud, as before).
    /// </summary>
    public Voicing Lookup(Chord chord, Difficulty difficulty)
    {
        IReadOnlyList<Voicing> candidates = Candidates(chord, difficulty);
        if (candidates.Count > 0)
        {
            return candidates[0];
        }

        if (!_strategies.TryGetValue(difficulty, out IVoicingStrategy? strategy))
        {
            throw new NotSupportedException(
                $"No voicing strategy is authored for the {difficulty} difficulty.");
        }

        return strategy.Voice(chord);
    }

    private static int LowestFret(Voicing voicing) =>
        voicing.FirstFret ?? voicing.Positions.Min(p => p.Fret);
}
