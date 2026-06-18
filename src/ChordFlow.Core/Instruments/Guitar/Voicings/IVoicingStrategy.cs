using ChordFlow.Domain;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// Chooses a chord shape for a difficulty tier (ctx IN7: voicing selection is a strategy, not a
/// table). <see cref="VoicingBook"/> maps a <see cref="Difficulty"/> to its strategy and delegates
/// the actual voicing. New tiers (Intermediate shell, Advanced inversions) add a strategy without
/// touching the book or the renderer.
/// </summary>
public interface IVoicingStrategy
{
    /// <summary>The difficulty tier this strategy voices for.</summary>
    Difficulty Difficulty { get; }

    /// <summary>The voicing for <paramref name="chord"/> under this strategy.</summary>
    Voicing Voice(Chord chord);
}
