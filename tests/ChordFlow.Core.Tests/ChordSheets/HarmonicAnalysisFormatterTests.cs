using ChordFlow.Music.Harmony;
using ChordFlow.Rendering.ChordSheets;
using Xunit;

namespace ChordFlow.Core.Tests.ChordSheets;

/// <summary>
/// <see cref="HarmonicAnalysisFormatter"/>: the glyph-only presentation of a <see cref="ChordAnalysis"/>.
/// One fixture per category, all in C major — asserting the honest degree, the functional glyph (they diverge
/// only for the two secondary categories), and the colour-key.
/// </summary>
public class HarmonicAnalysisFormatterTests
{
    private static readonly Key CMajor = new(new PitchClass(0), false);

    [Fact]
    public void Diatonic_GlyphEqualsHonestDegree()
    {
        // ii = Dm in C.
        var a = new ChordAnalysis(HarmonicCategory.Diatonic, new RomanDegree(2, Quality.Minor));

        Assert.Equal("ii", HarmonicAnalysisFormatter.HonestDegree(a.Function));
        Assert.Equal("ii", HarmonicAnalysisFormatter.Glyph(a, CMajor));
        Assert.Equal("diatonic", HarmonicAnalysisFormatter.CategoryKey(a.Category));
    }

    [Fact]
    public void SecondaryDominant_GlyphIsAppliedNotation_HonestIsPosition()
    {
        // A7 in C → honest VI7, functions as V7/ii (target ii is minor → lower-case).
        var a = new ChordAnalysis(
            HarmonicCategory.SecondaryDominant, new RomanDegree(6, Quality.Dominant7), Target: new ScaleDegree(2));

        Assert.Equal("VI7", HarmonicAnalysisFormatter.HonestDegree(a.Function));
        Assert.Equal("V7/ii", HarmonicAnalysisFormatter.Glyph(a, CMajor));
        Assert.Equal("secondaryDominant", HarmonicAnalysisFormatter.CategoryKey(a.Category));
    }

    [Fact]
    public void SecondaryLeadingTone_GlyphIsAppliedDiminished()
    {
        // F#dim in C → honest #iv°, functions as vii°/V (target V is major → upper-case).
        var a = new ChordAnalysis(
            HarmonicCategory.SecondaryLeadingTone,
            new RomanDegree(4, Quality.Diminished, Accidental.Sharp),
            Target: new ScaleDegree(5));

        Assert.Equal("#iv°", HarmonicAnalysisFormatter.HonestDegree(a.Function));
        Assert.Equal("vii°/V", HarmonicAnalysisFormatter.Glyph(a, CMajor));
        Assert.Equal("secondaryLeadingTone", HarmonicAnalysisFormatter.CategoryKey(a.Category));
    }

    [Fact]
    public void SecondaryLeadingTone_DiminishedSeventh_ReadsViiDim7()
    {
        var a = new ChordAnalysis(
            HarmonicCategory.SecondaryLeadingTone,
            new RomanDegree(4, Quality.Diminished7, Accidental.Sharp),
            Target: new ScaleDegree(5));

        Assert.Equal("vii°7/V", HarmonicAnalysisFormatter.Glyph(a, CMajor));
    }

    [Fact]
    public void Borrowed_GlyphIsHonestDegree_ColourCarriesTheSignal()
    {
        // Fm (iv) in C major, borrowed from the parallel minor.
        var a = new ChordAnalysis(
            HarmonicCategory.Borrowed, new RomanDegree(4, Quality.Minor), SourceMode: KeyMode.Minor);

        Assert.Equal("iv", HarmonicAnalysisFormatter.HonestDegree(a.Function));
        Assert.Equal("iv", HarmonicAnalysisFormatter.Glyph(a, CMajor));
        Assert.Equal("borrowed", HarmonicAnalysisFormatter.CategoryKey(a.Category));
    }

    [Fact]
    public void TritoneSub_GlyphIsFlatTwoDominant()
    {
        // Db7 in C → bII7.
        var a = new ChordAnalysis(
            HarmonicCategory.TritoneSub, new RomanDegree(2, Quality.Dominant7, Accidental.Flat),
            Target: new ScaleDegree(1));

        Assert.Equal("bII7", HarmonicAnalysisFormatter.HonestDegree(a.Function));
        Assert.Equal("bII7", HarmonicAnalysisFormatter.Glyph(a, CMajor));
        Assert.Equal("tritoneSub", HarmonicAnalysisFormatter.CategoryKey(a.Category));
    }

    [Fact]
    public void Chromatic_GlyphIsHonestDegree()
    {
        // A chromatic chord keeps its honest degree; colour-key = chromatic.
        var a = new ChordAnalysis(HarmonicCategory.Chromatic, new RomanDegree(3, Quality.Major, Accidental.Flat));

        Assert.Equal("bIII", HarmonicAnalysisFormatter.HonestDegree(a.Function));
        Assert.Equal("bIII", HarmonicAnalysisFormatter.Glyph(a, CMajor));
        Assert.Equal("chromatic", HarmonicAnalysisFormatter.CategoryKey(a.Category));
    }
}
