namespace ChordFlow.Music.Harmony;

/// <summary>
/// The harmonic-function category <see cref="HarmonicAnalyzer"/> assigns to a chord, relative to a key.
/// Split (design D6) so the applied dominant and the applied leading-tone read as distinct concepts.
/// </summary>
public enum HarmonicCategory
{
    /// <summary>
    /// A chord on a diatonic degree with the diatonic quality (triad-vs-7th tolerant). In a minor key this
    /// also covers the harmonic-minor functional dominant (V / V7) and leading-tone diminished (vii°/vii°7),
    /// which natural minor lacks.
    /// </summary>
    Diatonic,

    /// <summary>
    /// An applied dominant (<c>V/x</c>): a dominant-family chord a perfect fifth above a <b>non-tonic</b>
    /// diatonic degree, tonicizing it.
    /// </summary>
    SecondaryDominant,

    /// <summary>
    /// An applied leading-tone diminished (<c>vii°/x</c>): a diminished triad / dim7 a semitone below a
    /// <b>non-tonic</b> diatonic degree.
    /// </summary>
    SecondaryLeadingTone,

    /// <summary>Modal mixture: a chord borrowed from the parallel mode (major↔minor).</summary>
    Borrowed,

    /// <summary>A tritone substitute for the primary dominant (<c>♭II7</c>, resolving down a semitone to I).</summary>
    TritoneSub,

    /// <summary>
    /// None of the above — a chromatic chord (including the blues <c>I7</c>/<c>IV7</c>). Its honest degree is
    /// still carried in <see cref="ChordAnalysis.Function"/>.
    /// </summary>
    Chromatic,
}

/// <summary>
/// A key's mode — names the parallel mode a <see cref="HarmonicCategory.Borrowed"/> chord is drawn from
/// (clearer than a bare <c>bool</c>).
/// </summary>
public enum KeyMode
{
    Major,
    Minor,
}
