using ChordFlow.Music.Harmony;
using Xunit;

namespace ChordFlow.Core.Tests;

public class QualityFacetsTests
{
    // The full design §2.2 facet table, extended with the plain Diminished triad (omitted from the
    // 10-row doc table) — every Quality in the enum, derived from its chord-tone spelling.
    [Theory]
    [InlineData(Quality.Major, ThirdFacet.Major, FifthFacet.Perfect, SeventhFacet.Triad)]
    [InlineData(Quality.Minor, ThirdFacet.Minor, FifthFacet.Perfect, SeventhFacet.Triad)]
    [InlineData(Quality.Major6, ThirdFacet.Major, FifthFacet.Perfect, SeventhFacet.Sixth)]
    [InlineData(Quality.Minor6, ThirdFacet.Minor, FifthFacet.Perfect, SeventhFacet.Sixth)]
    [InlineData(Quality.Dominant7, ThirdFacet.Major, FifthFacet.Perfect, SeventhFacet.Seventh)]
    [InlineData(Quality.Minor7, ThirdFacet.Minor, FifthFacet.Perfect, SeventhFacet.Seventh)]
    [InlineData(Quality.Major7, ThirdFacet.Major, FifthFacet.Perfect, SeventhFacet.MajorSeventh)]
    [InlineData(Quality.Augmented, ThirdFacet.Major, FifthFacet.Augmented, SeventhFacet.Triad)]
    [InlineData(Quality.HalfDiminished7, ThirdFacet.Minor, FifthFacet.Diminished, SeventhFacet.Seventh)]
    [InlineData(Quality.Diminished, ThirdFacet.Minor, FifthFacet.Diminished, SeventhFacet.Triad)]
    [InlineData(Quality.Diminished7, ThirdFacet.Minor, FifthFacet.Diminished, SeventhFacet.DiminishedSeventh)]
    public void Of_EachQuality_DerivesTheDesignFacets(
        Quality quality, ThirdFacet third, FifthFacet fifth, SeventhFacet seventh)
    {
        var facets = QualityFacets.Of(quality);

        Assert.Equal(new QualityFacets(third, fifth, seventh), facets);
    }

    [Fact]
    public void Of_EveryQuality_MapsToAUniqueCell()
    {
        var cells = Enum.GetValues<Quality>()
            .Select(QualityFacets.Of)
            .ToArray();

        Assert.Equal(cells.Length, cells.Distinct().Count());
    }

    [Theory]
    [InlineData(Quality.Major, "major", "perfect", "triad")]
    [InlineData(Quality.Major6, "major", "perfect", "6")]
    [InlineData(Quality.Dominant7, "major", "perfect", "7")]
    [InlineData(Quality.Major7, "major", "perfect", "maj7")]
    [InlineData(Quality.Augmented, "major", "augmented", "triad")]
    [InlineData(Quality.HalfDiminished7, "minor", "diminished", "7")]
    [InlineData(Quality.Diminished7, "minor", "diminished", "dim7")]
    public void Tokens_MatchTheFilterContract(
        Quality quality, string thirdToken, string fifthToken, string seventhToken)
    {
        var facets = QualityFacets.Of(quality);

        Assert.Equal(thirdToken, facets.ThirdToken);
        Assert.Equal(fifthToken, facets.FifthToken);
        Assert.Equal(seventhToken, facets.SeventhToken);
    }
}
