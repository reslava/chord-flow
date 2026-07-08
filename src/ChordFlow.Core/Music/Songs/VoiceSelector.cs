using ChordFlow.Music.Harmony;
using ChordFlow.Music.Progressions;

namespace ChordFlow.Music.Songs;

/// <summary>
/// The left-hand side of a Song <c>voice &lt;selector&gt; = …</c> default (design D2/D7). Either
/// <b>degree-scoped</b> — a specific <see cref="RomanDegree"/> (<c>17</c>, <c>#4dim7</c>) — or
/// <b>quality-scoped</b> — a <c>*&lt;quality&gt;</c> wildcard matching every chord of that quality
/// (<c>*7</c>, <c>*m7</c>, bare <c>*</c> = every major triad). Degree-scoped is the more specific match and
/// wins the cascade over a quality-scoped default. Record value-equality is both the dedupe key for the
/// duplicate-selector guard (req <c>C6</c>) and the resolver's most-specific lookup key.
/// </summary>
/// <param name="Degree">The exact chord for a degree-scoped selector, or <c>null</c> for a quality wildcard.</param>
/// <param name="Quality">The quality matched — the degree's quality when degree-scoped, else the wildcard quality.</param>
public sealed record VoiceSelector(RomanDegree? Degree, Quality Quality)
{
    /// <summary>A degree-scoped selector matching exactly <paramref name="degree"/>.</summary>
    public static VoiceSelector ForDegree(RomanDegree degree) => new(degree, degree.Quality);

    /// <summary>A quality-scoped (<c>*&lt;quality&gt;</c>) selector matching any degree of <paramref name="quality"/>.</summary>
    public static VoiceSelector ForQuality(Quality quality) => new(null, quality);

    /// <summary>True for a <c>*&lt;quality&gt;</c> wildcard (no fixed degree).</summary>
    public bool IsQualityScoped => Degree is null;
}
