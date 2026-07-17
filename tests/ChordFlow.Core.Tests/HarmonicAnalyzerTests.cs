using ChordFlow.Music.Harmony;
using Xunit;

namespace ChordFlow.Core.Tests;

public class HarmonicAnalyzerTests
{
    private static Chord Ch(int pc, Quality q) => new(new PitchClass(pc), q);

    private static Key CMajor => new(new PitchClass(0), false);

    private static Key CMinor => new(new PitchClass(0), true);

    private static RomanDegree Deg(int degree, Quality quality, Accidental accidental = Accidental.Natural) =>
        new(degree, quality, accidental);

    // ---- Diatonic (major) --------------------------------------------------

    [Fact]
    public void IiVI_InCMajor_AllDiatonic()
    {
        Assert.Equal(
            new ChordAnalysis(HarmonicCategory.Diatonic, Deg(2, Quality.Minor7)),
            HarmonicAnalyzer.Analyze(Ch(2, Quality.Minor7), CMajor)); // Dm7 = ii
        Assert.Equal(
            new ChordAnalysis(HarmonicCategory.Diatonic, Deg(5, Quality.Dominant7)),
            HarmonicAnalyzer.Analyze(Ch(7, Quality.Dominant7), CMajor)); // G7 = V
        Assert.Equal(
            new ChordAnalysis(HarmonicCategory.Diatonic, Deg(1, Quality.Major7)),
            HarmonicAnalyzer.Analyze(Ch(0, Quality.Major7), CMajor)); // Cmaj7 = I
    }

    [Fact]
    public void PlainTriads_MatchTheirDiatonicSeventh_TriadTolerance()
    {
        // A bare G major triad is still the diatonic V (whose diatonic 7th is G7); D minor triad is ii;
        // B diminished triad is vii°.
        Assert.Equal(HarmonicCategory.Diatonic, HarmonicAnalyzer.Analyze(Ch(7, Quality.Major), CMajor).Category);
        Assert.Equal(HarmonicCategory.Diatonic, HarmonicAnalyzer.Analyze(Ch(2, Quality.Minor), CMajor).Category);
        Assert.Equal(HarmonicCategory.Diatonic, HarmonicAnalyzer.Analyze(Ch(11, Quality.Diminished), CMajor).Category);
        // But a B *minor* triad (perfect 5th) is NOT the diatonic vii° (diminished 5th).
        Assert.NotEqual(HarmonicCategory.Diatonic, HarmonicAnalyzer.Analyze(Ch(11, Quality.Minor), CMajor).Category);
    }

    // ---- Secondary dominants ----------------------------------------------

    [Fact]
    public void CircleOfSecondaryDominants_InCMajor()
    {
        // E7 A7 D7 G7 → V/vi, V/ii, V/V, then the diatonic V.
        AssertSecondaryDominant(Ch(4, Quality.Dominant7), CMajor, target: 6, function: Deg(3, Quality.Dominant7));
        AssertSecondaryDominant(Ch(9, Quality.Dominant7), CMajor, target: 2, function: Deg(6, Quality.Dominant7));
        AssertSecondaryDominant(Ch(2, Quality.Dominant7), CMajor, target: 5, function: Deg(2, Quality.Dominant7));
        Assert.Equal(HarmonicCategory.Diatonic, HarmonicAnalyzer.Analyze(Ch(7, Quality.Dominant7), CMajor).Category);
    }

    // ---- Secondary leading-tone -------------------------------------------

    [Fact]
    public void SecondaryLeadingTone_Diminished_TonicizesTheNextDegree()
    {
        // F#dim7 a semitone below G(V) → vii°/V; C#dim7 a semitone below D(ii) → vii°/ii.
        AssertSecondaryLeadingTone(Ch(6, Quality.Diminished7), CMajor, target: 5);
        AssertSecondaryLeadingTone(Ch(1, Quality.Diminished7), CMajor, target: 2);
    }

    // ---- Tritone sub -------------------------------------------------------

    [Fact]
    public void FlatII7_IsTritoneSub_ResolvingToI()
    {
        ChordAnalysis a = HarmonicAnalyzer.Analyze(Ch(1, Quality.Dominant7), CMajor); // Db7
        Assert.Equal(HarmonicCategory.TritoneSub, a.Category);
        Assert.Equal(new ScaleDegree(1), a.Target);
        Assert.Equal(Deg(2, Quality.Dominant7, Accidental.Flat), a.Function); // ♭II7
    }

    // ---- Borrowed / modal mixture -----------------------------------------

    [Fact]
    public void BorrowedFromParallelMinor_InCMajor()
    {
        AssertBorrowed(Ch(5, Quality.Minor), CMajor, KeyMode.Minor, Deg(4, Quality.Minor));                 // Fm = iv
        AssertBorrowed(Ch(10, Quality.Major), CMajor, KeyMode.Minor, Deg(7, Quality.Major, Accidental.Flat)); // Bb = ♭VII
        AssertBorrowed(Ch(8, Quality.Major), CMajor, KeyMode.Minor, Deg(6, Quality.Major, Accidental.Flat));  // Ab = ♭VI
    }

    // ---- Minor tonic -------------------------------------------------------

    [Fact]
    public void MinorIiVI_InCMinor_AllDiatonic()
    {
        // Dm7b5 (ii°), G7 (the harmonic-minor V), Cm (i).
        Assert.Equal(HarmonicCategory.Diatonic, HarmonicAnalyzer.Analyze(Ch(2, Quality.HalfDiminished7), CMinor).Category);
        ChordAnalysis five = HarmonicAnalyzer.Analyze(Ch(7, Quality.Dominant7), CMinor);
        Assert.Equal(HarmonicCategory.Diatonic, five.Category);
        Assert.Equal(Deg(5, Quality.Dominant7), five.Function);
        Assert.Equal(HarmonicCategory.Diatonic, HarmonicAnalyzer.Analyze(Ch(0, Quality.Minor), CMinor).Category);
    }

    [Fact]
    public void PicardyThird_MajorTonicInMinorKey_IsBorrowedFromMajor()
    {
        ChordAnalysis a = HarmonicAnalyzer.Analyze(Ch(0, Quality.Major), CMinor); // C major in C minor
        Assert.Equal(HarmonicCategory.Borrowed, a.Category);
        Assert.Equal(KeyMode.Major, a.SourceMode);
        Assert.Equal(Deg(1, Quality.Major), a.Function);
    }

    // ---- The blues stress test --------------------------------------------

    [Fact]
    public void DominantBlues_IsNotOverLabelledAsSecondaryDominants()
    {
        // I7 and IV7 are chromatic (dominant colour on a diatonic root), NOT V/IV etc.; only V7 is diatonic.
        ChordAnalysis i7 = HarmonicAnalyzer.Analyze(Ch(0, Quality.Dominant7), CMajor);
        Assert.Equal(HarmonicCategory.Chromatic, i7.Category);
        Assert.Equal(Deg(1, Quality.Dominant7), i7.Function);
        Assert.Null(i7.Target);

        ChordAnalysis iv7 = HarmonicAnalyzer.Analyze(Ch(5, Quality.Dominant7), CMajor);
        Assert.Equal(HarmonicCategory.Chromatic, iv7.Category);
        Assert.Equal(Deg(4, Quality.Dominant7), iv7.Function);
        Assert.Null(iv7.Target);

        Assert.Equal(HarmonicCategory.Diatonic, HarmonicAnalyzer.Analyze(Ch(7, Quality.Dominant7), CMajor).Category);
    }

    // ---- Sequence API ------------------------------------------------------

    [Fact]
    public void SequenceApi_AnalyzesPerPositionKey()
    {
        // A ii–V–I that modulates: Dm7 in C, then G7 → C is fine, but here prove the key can vary per position —
        // the same Am7 chord reads ii in G major and vi in C major.
        var seq = new (Chord, Key)[]
        {
            (Ch(9, Quality.Minor7), new Key(new PitchClass(7), false)), // Am7 in G major = ii
            (Ch(9, Quality.Minor7), CMajor),                            // Am7 in C major = vi
        };

        IReadOnlyList<ChordAnalysis> results = HarmonicAnalyzer.Analyze(seq);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(HarmonicCategory.Diatonic, r.Category));
        Assert.Equal(new ScaleDegree(2).Number, results[0].Function.Degree); // ii of G
        Assert.Equal(new ScaleDegree(6).Number, results[1].Function.Degree); // vi of C
    }

    // ---- helpers -----------------------------------------------------------

    private static void AssertSecondaryDominant(Chord chord, Key key, int target, RomanDegree function)
    {
        ChordAnalysis a = HarmonicAnalyzer.Analyze(chord, key);
        Assert.Equal(HarmonicCategory.SecondaryDominant, a.Category);
        Assert.Equal(new ScaleDegree(target), a.Target);
        Assert.Equal(function, a.Function);
    }

    private static void AssertSecondaryLeadingTone(Chord chord, Key key, int target)
    {
        ChordAnalysis a = HarmonicAnalyzer.Analyze(chord, key);
        Assert.Equal(HarmonicCategory.SecondaryLeadingTone, a.Category);
        Assert.Equal(new ScaleDegree(target), a.Target);
    }

    private static void AssertBorrowed(Chord chord, Key key, KeyMode source, RomanDegree function)
    {
        ChordAnalysis a = HarmonicAnalyzer.Analyze(chord, key);
        Assert.Equal(HarmonicCategory.Borrowed, a.Category);
        Assert.Equal(source, a.SourceMode);
        Assert.Equal(function, a.Function);
    }
}
