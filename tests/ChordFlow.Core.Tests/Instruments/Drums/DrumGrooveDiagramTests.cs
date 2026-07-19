using ChordFlow.Instruments.Drums;
using ChordFlow.Music.Rhythm;
using Xunit;

namespace ChordFlow.Core.Tests;

public class DrumGrooveDiagramTests
{
    private static DrumGrooveDiagram Build(string dsl) =>
        DrumGrooveDiagram.Build(DrumGrooveParser.Parse("g", "Groove", dsl, TimeSignature.FourFour));

    [Fact]
    public void Build_OneRowPerVoice_InFirstSeenOrder()
    {
        var d = Build(
            "HH :2 xxxxxxxx\n" +
            "SD :2 ..x...x.\n" +
            "BD :2 x...x...");

        Assert.Equal(
            new[] { DrumVoice.HiHatClosed, DrumVoice.Snare, DrumVoice.Kick },
            d.Lanes.Select(l => l.Voice));
        Assert.Equal(new[] { "HH", "SD", "BD" }, d.Lanes.Select(l => l.Label));
    }

    [Fact]
    public void Build_CarriesBarAndBeatGeometry()
    {
        var d = Build("HH :2 xxxxxxxx | xxxxxxxx");

        Assert.Equal(2, d.BarCount);
        Assert.Equal(4, d.BeatsPerBar);
        Assert.Equal(192, d.TicksPerBar);
        Assert.Equal("Groove", d.Title);
    }

    [Fact]
    public void Build_HitsCarryBarRelativeOnsets()
    {
        var d = Build("BD :2 x...x...");

        DrumGrooveLaneRow kick = d.Lanes.Single(l => l.Voice == DrumVoice.Kick);
        Assert.Equal(new[] { 0, 96 }, kick.Hits.Select(h => h.Tick));
        Assert.All(kick.Hits, h => Assert.Equal(0, h.Bar));
    }

    [Fact]
    public void Build_MultiBar_TagsHitsWithTheirBarIndex()
    {
        var d = Build(
            "BD :2 x...x... | x.x.x...");

        DrumGrooveLaneRow kick = d.Lanes.Single(l => l.Voice == DrumVoice.Kick);
        Assert.Equal(
            new[] { (0, 0), (0, 96), (1, 0), (1, 48), (1, 96) },
            kick.Hits.Select(h => (h.Bar, h.Tick)));
    }
}
