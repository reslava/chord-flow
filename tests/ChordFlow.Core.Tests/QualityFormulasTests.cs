using ChordFlow.Music.Harmony;
using Xunit;

namespace ChordFlow.Core.Tests;

public class QualityFormulasTests
{
    // The golden oracle for the formula layer: each quality's authored formula string AND the
    // semitones it must derive to (the design's Semitones column, authored independently here).
    // QualityFormulas is the single authored source; QualityIntervals derives — this pins what it
    // derives to, so a formula typo or an IntervalSpeller change breaks against a human constant.
    [Theory]
    [InlineData(Quality.Major, "1 3 5", new[] { 0, 4, 7 })]
    [InlineData(Quality.Minor, "1 b3 5", new[] { 0, 3, 7 })]
    [InlineData(Quality.Dominant7, "1 3 5 b7", new[] { 0, 4, 7, 10 })]
    [InlineData(Quality.Major7, "1 3 5 7", new[] { 0, 4, 7, 11 })]
    [InlineData(Quality.Minor7, "1 b3 5 b7", new[] { 0, 3, 7, 10 })]
    [InlineData(Quality.HalfDiminished7, "1 b3 b5 b7", new[] { 0, 3, 6, 10 })]
    [InlineData(Quality.Diminished, "1 b3 b5", new[] { 0, 3, 6 })]
    [InlineData(Quality.Diminished7, "1 b3 b5 bb7", new[] { 0, 3, 6, 9 })]
    [InlineData(Quality.Augmented, "1 3 #5", new[] { 0, 4, 8 })]
    public void Formula_AndDerivedSemitones_MatchTheAuthoredOracle(
        Quality quality, string expectedFormula, int[] expectedSemitones)
    {
        Assert.Equal(expectedFormula, QualityFormulas.Formula(quality));
        Assert.Equal(expectedSemitones, QualityIntervals.Intervals(quality));
    }

    [Fact]
    public void EveryQuality_HasANonEmptyFormula()
    {
        foreach (Quality quality in Enum.GetValues<Quality>())
        {
            Assert.False(string.IsNullOrWhiteSpace(QualityFormulas.Formula(quality)));
        }
    }

    [Fact]
    public void Formula_UnmappedQuality_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => QualityFormulas.Formula((Quality)999));
    }
}
