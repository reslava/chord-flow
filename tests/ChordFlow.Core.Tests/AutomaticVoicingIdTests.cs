using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;
using Xunit;

namespace ChordFlow.Core.Tests;

public class AutomaticVoicingIdTests
{
    [Fact]
    public void For_BuildsFourSegmentId()
    {
        Assert.Equal("auto:shell:dom7:E", AutomaticVoicingId.For(VoicingFamily.Shell, Quality.Dominant7, CagedShape.E));
        Assert.Equal("auto:caged:maj7:C", AutomaticVoicingId.For(VoicingFamily.Caged, Quality.Major7, CagedShape.C));
        Assert.Equal("auto:dshell:m7b5:D", AutomaticVoicingId.For(VoicingFamily.DoubledShell, Quality.HalfDiminished7, CagedShape.D));
    }

    [Fact]
    public void TryParse_RoundTripsEveryCatalogCombo()
    {
        foreach ((VoicingFamily family, Quality quality, CagedShape shape) in CagedVoicingCatalog.Combos)
        {
            string id = AutomaticVoicingId.For(family, quality, shape);
            Assert.True(AutomaticVoicingId.TryParse(id, out VoicingFamily f, out Quality q, out CagedShape s), id);
            Assert.Equal((family, quality, shape), (f, q, s));
        }
    }

    [Theory]
    [InlineData("auto:dom7:E")]       // the old 3-segment form is no longer valid
    [InlineData("auto:zzz:dom7:E")]   // unknown family token
    [InlineData("auto:shell:nope:E")] // unknown quality token
    [InlineData("auto:shell:dom7:Z")] // unknown shape
    [InlineData("nope")]
    public void TryParse_RejectsMalformed(string id)
    {
        Assert.False(AutomaticVoicingId.TryParse(id, out _, out _, out _));
    }
}
