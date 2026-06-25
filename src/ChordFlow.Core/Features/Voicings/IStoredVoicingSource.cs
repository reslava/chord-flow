using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;
using ChordFlow.Persistence;

namespace ChordFlow.Features.Voicings;

/// <summary>
/// Supplies the comping resolver with candidate grips for a chord from the <b>stored</b> voicing sources —
/// package and user (engine-derived-as-app-source, req IN4). The engine <c>automatic</c> source is computed
/// separately (it needs no store); this seam is the DB-backed package/user side, kept behind an interface so
/// the resolver stays unit-testable with a fake.
/// </summary>
public interface IStoredVoicingSource
{
    /// <summary>
    /// Candidate voicings for <paramref name="chord"/> from <paramref name="source"/> (User or Package),
    /// realized to the chord's root; empty when that source has none. <paramref name="packageId"/> narrows a
    /// package source to one pack (null ⇒ any pack).
    /// </summary>
    IReadOnlyList<Voicing> Candidates(Chord chord, ContentSource source, string? packageId);
}
