using ChordFlow.Music.Harmony;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// A pluggable within-source voicing-ranking strategy (engine-derived-as-app-source, req IN7): given a
/// chord-occurrence and its candidate grips, pick the one to comp. The seam exists so the alternative modes
/// (all-CAGED variety; guide-tone voice-leading) can be added additively (voicing-ranking-strategies); this
/// thread ships only <see cref="ClosestRanking"/>. The strategy is sequence-aware — it reads and updates a
/// running <see cref="VoicingRankingContext"/> (the previous grip + the per-chord history) as the resolver
/// walks the progression.
/// </summary>
public interface IVoicingRanking
{
    /// <summary>
    /// Pick the grip for <paramref name="chord"/> from its non-empty <paramref name="candidates"/>, updating
    /// <paramref name="context"/> with the choice (so later chords can reference it).
    /// </summary>
    Voicing Pick(Chord chord, IReadOnlyList<Voicing> candidates, VoicingRankingContext context);
}

/// <summary>
/// The running state a sequence-aware <see cref="IVoicingRanking"/> threads across a progression: the grip
/// chosen for the previous chord-occurrence, and the grip chosen for each distinct chord so far (so a
/// strategy can reuse a chord's earlier grip). A fresh context is created per comping resolution.
/// </summary>
public sealed class VoicingRankingContext
{
    /// <summary>The grip chosen for the previous chord-occurrence; null before the first.</summary>
    public Voicing? PreviousGrip { get; set; }

    /// <summary>The grip chosen for each distinct chord seen so far.</summary>
    public Dictionary<Chord, Voicing> ChosenByChord { get; } = new();
}
