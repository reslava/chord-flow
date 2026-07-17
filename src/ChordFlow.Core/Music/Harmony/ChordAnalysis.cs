namespace ChordFlow.Music.Harmony;

/// <summary>
/// The structured result of analyzing one chord in a key (see <see cref="HarmonicAnalyzer"/>): a glyph-free,
/// introspectable record (constraint C3 — presentation is a consumer concern). <see cref="Function"/> is the
/// honest key-relative degree of the chord and is <b>always</b> populated; <see cref="Category"/> plus
/// <see cref="Target"/> / <see cref="SourceMode"/> carry the functional interpretation.
/// </summary>
/// <param name="Category">The harmonic-function category.</param>
/// <param name="Function">
/// The honest key-relative degree — degree number + chromatic accidental in the conventional major-frame Roman
/// spelling (so a minor key reads <c>i ii° ♭III iv v ♭VI ♭VII</c>) — carrying the chord's own quality. This is
/// the value that subsumes the chord sheet's inline Roman label (design IN9).
/// </param>
/// <param name="Target">
/// The tonicized degree, for <see cref="HarmonicCategory.SecondaryDominant"/> /
/// <see cref="HarmonicCategory.SecondaryLeadingTone"/> / <see cref="HarmonicCategory.TritoneSub"/>; otherwise null.
/// </param>
/// <param name="SourceMode">
/// The parallel mode a <see cref="HarmonicCategory.Borrowed"/> chord is drawn from; otherwise null.
/// </param>
public readonly record struct ChordAnalysis(
    HarmonicCategory Category,
    RomanDegree Function,
    ScaleDegree? Target = null,
    KeyMode? SourceMode = null);
