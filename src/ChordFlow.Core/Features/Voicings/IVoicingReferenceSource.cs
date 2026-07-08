using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;

namespace ChordFlow.Features.Voicings;

/// <summary>
/// Resolves a <b>source-qualified voicing reference</b> — the <c>{u: id}</c> / <c>{a: id}</c> /
/// <c>{&lt;packageId&gt;: id}</c> annotation form (req <c>IN2</c>) — to a concrete <see cref="Voicing"/> at a
/// chord's root. The seam behind the comping resolver's reference tier: <c>u</c> is the user library,
/// <c>a</c> is the engine <c>auto:…</c> catalog derived on the fly, and any other source token is a package id.
/// Returns <c>null</c> when the id is unknown in that source (or the source is filtered out) so the resolver
/// fails loud naming the reference (req <c>IN6</c>). Kept behind an interface so the resolver stays
/// unit-testable with a fake.
/// </summary>
public interface IVoicingReferenceSource
{
    /// <summary>The <see cref="Voicing"/> named by <paramref name="source"/>:<paramref name="id"/> realized at
    /// <paramref name="chord"/>'s root, or <c>null</c> if no such voicing exists in that source.</summary>
    Voicing? Resolve(string source, string id, Chord chord);
}
