using ChordFlow.Bridge;
using ChordFlow.Features.Rhythm;
using ChordFlow.Instruments.Drums;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// Tests the <c>rhythmGenerate</c> handler (req IN3/IN7/IN8/IN12): the three strategies (figure / pattern /
/// random) resolve and return tex + a DrumGrooveDiagram whose hit ticks match the generated onsets; the
/// Beat-1 reference adds a distinct row; a bad token fails loud as a <see cref="FormatException"/>.
/// </summary>
public class RhythmGenerateHandlerTests
{
    private static readonly RhythmGenerateHandler Handler = new();

    private static RhythmGenerationRequest Figure(
        string id, int barCount, string? voice = null, RhythmSelectionSpec? selection = null, string? referencePulse = null) =>
        new("figure", 0, voice, 100, id, null, null, null, selection, null, barCount, null, null, null, null, referencePulse);

    private static RhythmGenerationRequest Placement(int sub, int onsetCount, int barCount, RhythmSelectionSpec? selection = null) =>
        new("pattern", 0, null, 100, null, sub, "all", onsetCount, selection, null, barCount, null, null, null);

    private static int[] BarZeroTicks(RhythmGeneratedEnvelope env) =>
        env.Diagram.Lanes.Single(l => l.Voice == DrumVoice.HiHatClosed).Hits.Where(h => h.Bar == 0).Select(h => h.Tick).ToArray();

    [Fact]
    public void Figure_ReturnsTexDiagramAndGridText()
    {
        var env = Handler.Generate(Figure("four-on-floor", 1));
        Assert.False(string.IsNullOrWhiteSpace(env.Tex));
        Assert.Equal(new[] { 0, 48, 96, 144 }, BarZeroTicks(env));
        Assert.Equal("x x x x", env.Grid);
        Assert.Equal(DrumVoice.HiHatClosed, env.Diagram.Lanes.Single().Voice);
    }

    [Fact]
    public void Pattern_Placement_UsesTheGeneratedFamily()
    {
        // Placement(quarter,all,2)[0] = beats 1&2 → ticks [0,48]
        var env = Handler.Generate(Placement(1, 2, 1, new RhythmSelectionSpec("fixed", 0)));
        Assert.Equal(new[] { 0, 48 }, BarZeroTicks(env));
    }

    [Fact]
    public void Figure_HonoursTheChosenDrumVoice()
    {
        var env = Handler.Generate(Figure("four-on-floor", 1, voice: "SD"));
        Assert.Equal(DrumVoice.Snare, env.Diagram.Lanes.Single().Voice);
    }

    [Fact]
    public void Random_RestProbabilityOne_YieldsNoGeneratedHits()
    {
        var req = new RhythmGenerationRequest(
            "random", 5, "HH", 100, null, null, null, null, null, null, null, new[] { 4 }, 1, 0, RestProbability: 1.0);
        var env = Handler.Generate(req);
        Assert.Empty(env.Diagram.Lanes.SelectMany(l => l.Hits));
    }

    [Fact]
    public void ReferencePulse_Beat1_AddsADistinctRowHittingOnlyBeat1()
    {
        var env = Handler.Generate(Figure("four-on-floor", 2, voice: "HH", referencePulse: "beat1"));
        Assert.Equal(2, env.Diagram.Lanes.Count);
        var refLane = env.Diagram.Lanes.Single(l => l.Voice != DrumVoice.HiHatClosed);
        Assert.Equal(DrumVoice.Kick, refLane.Voice);
        Assert.Equal(new[] { (0, 0), (1, 0) }, refLane.Hits.Select(h => (h.Bar, h.Tick)));
    }

    [Fact]
    public void ReferencePulse_Off_AddsNoReferenceRow()
    {
        Assert.Single(Handler.Generate(Figure("four-on-floor", 1)).Diagram.Lanes);
    }

    [Fact]
    public void UnknownFigure_FailsLoud()
    {
        Assert.Throws<FormatException>(() => Handler.Generate(Figure("bogus", 1)));
    }

    [Fact]
    public void UnknownStrategy_FailsLoud()
    {
        var request = new RhythmGenerationRequest("nope", 0, null, 100, null, null, null, null, null, null, null, null, null, null);
        Assert.Throws<FormatException>(() => Handler.Generate(request));
    }

    [Fact]
    public void BarCountOutOfRange_FailsLoud()
    {
        Assert.Throws<FormatException>(() => Handler.Generate(Figure("four-on-floor", 17))); // cap is 16
    }
}
