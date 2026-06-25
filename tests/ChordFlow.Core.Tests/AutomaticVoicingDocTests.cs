using ChordFlow.Features.Voicings;
using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;
using Xunit;

namespace ChordFlow.Core.Tests;

public class AutomaticVoicingDocTests
{
    [Fact]
    public void DslFor_NonAutoId_ReturnsNull()
    {
        Assert.Null(AutomaticVoicingDoc.DslFor("some-user-guid"));
    }

    [Fact]
    public void DslFor_AutoId_ParsesToTheRightFamily()
    {
        string? dsl = AutomaticVoicingDoc.DslFor("auto:dom7:E");

        Assert.NotNull(dsl);
        VoicingShape shape = VoicingDslParser.Parse(dsl!);
        Assert.Equal(Quality.Dominant7, shape.Quality);
        Assert.Equal(CagedShape.E, shape.Shape);
    }

    [Fact]
    public void DslFor_EveryCatalogFamily_ResolvesToParseableDsl()
    {
        // Includes auto:maj:C — the family that errored in the app — so the lowest-valid-placement scan is exercised.
        foreach ((Quality quality, CagedShape shape) in CagedVoicingCatalog.Combos)
        {
            string id = AutomaticVoicingId.For(quality, shape);
            string? dsl = AutomaticVoicingDoc.DslFor(id);

            Assert.NotNull(dsl);
            VoicingShape parsed = VoicingDslParser.Parse(dsl!);
            Assert.Equal(quality, parsed.Quality);
            Assert.Equal(shape, parsed.Shape);
        }
    }
}
