using ChordFlow.Music.Harmony;
using Xunit;

namespace ChordFlow.Core.Tests;

public class DiatonicChordTests
{
    // C major: I Cmaj7, ii Dm7, iii Em7, IV Fmaj7, V G7, vi Am7, vii Bm7b5.
    [Theory]
    [InlineData(1, 0, Quality.Major7)]            // C  maj7
    [InlineData(2, 2, Quality.Minor7)]            // D  m7
    [InlineData(3, 4, Quality.Minor7)]            // E  m7
    [InlineData(4, 5, Quality.Major7)]            // F  maj7
    [InlineData(5, 7, Quality.Dominant7)]         // G  7
    [InlineData(6, 9, Quality.Minor7)]            // A  m7
    [InlineData(7, 11, Quality.HalfDiminished7)]  // B  m7b5
    public void Build_CMajor_ProducesTheDiatonicSeventhChords(int degree, int expectedRoot, Quality expectedQuality)
    {
        Scale cMajor = Scale.Major(new PitchClass(0));

        Chord chord = DiatonicChord.Build(cMajor, new ScaleDegree(degree));

        Assert.Equal(expectedRoot, chord.Root.Value);
        Assert.Equal(expectedQuality, chord.Quality);
    }

    [Fact]
    public void Build_TransposesWithTheTonic()
    {
        // ii of G major is Am7 (root A = pitch class 9).
        Scale gMajor = Scale.Major(new PitchClass(7));

        Chord ii = DiatonicChord.Build(gMajor, new ScaleDegree(2));

        Assert.Equal(9, ii.Root.Value);
        Assert.Equal(Quality.Minor7, ii.Quality);
    }

    [Fact]
    public void Build_DegreeOutOfRange_Throws()
    {
        Scale cMajor = Scale.Major(new PitchClass(0));

        Assert.Throws<ArgumentOutOfRangeException>(() => DiatonicChord.Build(cMajor, new ScaleDegree(8)));
    }
}

public class ScaleTests
{
    [Fact]
    public void ForKey_MajorAndMinor_PickTheRightIntervals()
    {
        Assert.Equal(Scale.MajorIntervals, Scale.ForKey(new Key(new PitchClass(0), false)).Intervals);
        Assert.Equal(Scale.NaturalMinorIntervals, Scale.ForKey(new Key(new PitchClass(9), true)).Intervals);
    }

    [Fact]
    public void DegreePitchClass_NaturalMinorThird_IsMinorThirdAboveTonic()
    {
        // 3rd degree of A natural minor is C (offset +3).
        Scale aMinor = Scale.NaturalMinor(new PitchClass(9));

        Assert.Equal(0, aMinor.DegreePitchClass(3).Value);
    }
}
