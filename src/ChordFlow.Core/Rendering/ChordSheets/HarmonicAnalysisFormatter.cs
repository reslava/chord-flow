using ChordFlow.Music.Harmony;

namespace ChordFlow.Rendering.ChordSheets;

/// <summary>
/// The presentation seam for the <see cref="HarmonicAnalyzer"/>: turns a glyph-free
/// <see cref="ChordAnalysis"/> into the strings a chord sheet paints. The analyzer stays glyph-free
/// (its constraint C3); this is where the roman numerals, the <c>V7/x</c> applied-function notation, and
/// the category colour-key vocabulary live — reusable by any future renderer, exporter, or tool, and kept
/// out of the dumb JS drawer (chord-sheets-maker C1).
///
/// <para>Two label strings come out of one analysis (harmonic-overlay design §4): the <b>honest</b> degree
/// (<see cref="HonestDegree"/> — the position + the chord's own quality, e.g. <c>VI7</c>) and the
/// <b>functional</b> glyph (<see cref="Glyph"/> — the role, e.g. <c>V7/ii</c>). They differ only for the two
/// secondary categories (the <c>/target</c> suffix); for borrowed / tritone-sub / chromatic the honest degree
/// <i>is</i> the conventional glyph and the colour carries the signal, so the two strings coincide.</para>
/// </summary>
public static class HarmonicAnalysisFormatter
{
    /// <summary>
    /// The honest key-relative roman label of a degree: an accidental prefix + the roman numeral (case carries
    /// major/minor) + a quality decoration. Formats the degree's <b>own</b> quality only — no applied-function
    /// inference. This is the body of the retired <c>ChordSheetBuilder.RomanFunction</c>, now fed the analyzer's
    /// pitch-derived <see cref="ChordAnalysis.Function"/> so the sheet's inline label and the analyzer agree by
    /// construction (harmonic-analysis IN9).
    /// </summary>
    public static string HonestDegree(RomanDegree degree)
    {
        string numeral = Numerals[degree.Degree];
        return AccidentalPrefix(degree.Accidental)
            + (IsLowerCase(degree.Quality) ? numeral.ToLowerInvariant() : numeral)
            + QualitySuffix(degree.Quality);
    }

    /// <summary>
    /// The functional glyph for an analysis in <paramref name="key"/>: the applied-function
    /// <c>V7/x</c> / <c>vii°/x</c> notation for the secondary categories, else the honest degree. The
    /// <paramref name="key"/> is needed only to case the tonicized target's numeral by its own diatonic quality
    /// (formatting, no new theory — harmonic-overlay design §5).
    /// </summary>
    public static string Glyph(ChordAnalysis analysis, Key key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return analysis.Category switch
        {
            HarmonicCategory.SecondaryDominant when analysis.Target is { } t
                => "V7/" + TargetLabel(t, key),
            HarmonicCategory.SecondaryLeadingTone when analysis.Target is { } t
                => LeadingToneNumerator(analysis.Function.Quality) + "/" + TargetLabel(t, key),
            _ => HonestDegree(analysis.Function),
        };
    }

    /// <summary>
    /// The stable colour-key a drawer keys its palette on. A small camelCase vocabulary mirroring
    /// <see cref="HarmonicCategory"/> (the FretR-palette equivalent for functional colour): <c>diatonic</c> is the
    /// neutral case, the rest are the non-diatonic hues.
    /// </summary>
    public static string CategoryKey(HarmonicCategory category) => category switch
    {
        HarmonicCategory.Diatonic => "diatonic",
        HarmonicCategory.SecondaryDominant => "secondaryDominant",
        HarmonicCategory.SecondaryLeadingTone => "secondaryLeadingTone",
        HarmonicCategory.Borrowed => "borrowed",
        HarmonicCategory.TritoneSub => "tritoneSub",
        HarmonicCategory.Chromatic => "chromatic",
        _ => "chromatic",
    };

    // The applied leading-tone numerator reflects the chord's own quality: a diminished 7th reads vii°7, a
    // diminished triad vii°.
    private static string LeadingToneNumerator(Quality quality) =>
        quality == Quality.Diminished7 ? "vii°7" : "vii°";

    // The tonicized target as a bare roman numeral, cased by its own diatonic quality in the key (ii lowercase,
    // V uppercase) with a diminished/half-diminished marker — e.g. V7/ii, V7/V, vii°/vi. No quality suffix: the
    // target names the chord being tonicized, not its full symbol.
    private static string TargetLabel(ScaleDegree target, Key key)
    {
        Quality quality = DiatonicChord.Build(Scale.ForKey(key), target).Quality;
        string numeral = Numerals[target.Number];
        string cased = IsLowerCase(quality) ? numeral.ToLowerInvariant() : numeral;
        string mark = quality switch
        {
            Quality.Diminished or Quality.Diminished7 => "°",
            Quality.HalfDiminished7 => "ø",
            _ => "",
        };
        return cased + mark;
    }

    // Lower-case the numeral for the minor-family qualities (their third is minor), upper-case otherwise.
    private static bool IsLowerCase(Quality quality) => quality is Quality.Minor or Quality.Minor7
        or Quality.Minor6 or Quality.Diminished or Quality.Diminished7 or Quality.HalfDiminished7;

    private static string QualitySuffix(Quality quality) => quality switch
    {
        Quality.Dominant7 => "7",
        Quality.Minor7 => "7",
        Quality.Major7 => "maj7",
        Quality.HalfDiminished7 => "ø7",
        Quality.Diminished => "°",
        Quality.Diminished7 => "°7",
        Quality.Augmented => "+",
        Quality.Major6 => "6",
        Quality.Minor6 => "6",
        _ => "",
    };

    // Roman numerals indexed by scale degree (1..7); index 0 is unused.
    private static readonly string[] Numerals = { "", "I", "II", "III", "IV", "V", "VI", "VII" };

    private static string AccidentalPrefix(Accidental accidental) => accidental switch
    {
        Accidental.Sharp => "#",
        Accidental.Flat => "b",
        _ => "",
    };
}
