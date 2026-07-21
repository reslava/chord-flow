using ChordFlow.Bridge;
using ChordFlow.Features.Rhythm;
using ChordFlow.Instruments.Drums;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// Tests the <c>rhythmGenerate</c> handler (req IN3/IN7/IN8/IN12): a valid Pattern (kind + selection) / Random
/// request returns tex + a DrumGrooveDiagram whose hit ticks match the generated onsets; the Beat-1 reference
/// adds a distinct row; a bad token fails loud as a <see cref="FormatException"/>.
/// </summary>
public class RhythmGenerateHandlerTests
{
    private static readonly RhythmGenerateHandler Handler = new();

    private static RhythmKindSpec Figure(string id) => new("figure", FigureId: id);
    private static RhythmKindSpec Density(int sub, int count) => new("density", Subdivision: sub, OnsetCount: count);

    private static RhythmGenerationRequest Pattern(
        RhythmKindSpec kind, int barCount, string? voice = null, RhythmSelectionSpec? selection = null,
        string? referencePulse = null) =>
        new("pattern", 0, voice, 100, kind, selection, null, barCount, null, null, null, null, referencePulse);

    private static int[] BarZeroTicks(RhythmGeneratedEnvelope env) =>
        env.Diagram.Lanes.Single(l => l.Voice == DrumVoice.HiHatClosed).Hits.Where(h => h.Bar == 0).Select(h => h.Tick).ToArray();

    [Fact]
    public void Pattern_Figure_ReturnsTexDiagramAndGridText()
    {
        var env = Handler.Generate(Pattern(Figure("four-on-floor"), 1));
        Assert.False(string.IsNullOrWhiteSpace(env.Tex));
        Assert.Equal(new[] { 0, 48, 96, 144 }, BarZeroTicks(env));
        Assert.Equal("x x x x", env.Grid);
        Assert.Equal(DrumVoice.HiHatClosed, env.Diagram.Lanes.Single().Voice);
    }

    [Fact]
    public void Pattern_Density_UsesTheGeneratedFamily()
    {
        // Density(quarter,2)[0] = beats 1&2 → ticks [0,48]
        var env = Handler.Generate(Pattern(Density(1, 2), 1, selection: new RhythmSelectionSpec("fixed", 0)));
        Assert.Equal(new[] { 0, 48 }, BarZeroTicks(env));
    }

    [Fact]
    public void Pattern_HonoursTheChosenDrumVoice()
    {
        var env = Handler.Generate(Pattern(Figure("four-on-floor"), 1, voice: "SD"));
        Assert.Equal(DrumVoice.Snare, env.Diagram.Lanes.Single().Voice);
    }

    [Fact]
    public void Random_RestProbabilityOne_YieldsNoGeneratedHits()
    {
        var req = new RhythmGenerationRequest(
            "random", 5, "HH", 100, null, null, null, null, new[] { 4 }, 1, 0, RestProbability: 1.0);
        var env = Handler.Generate(req);
        Assert.Empty(env.Diagram.Lanes.SelectMany(l => l.Hits));
    }

    [Fact]
    public void ReferencePulse_Beat1_AddsADistinctRowHittingOnlyBeat1()
    {
        var env = Handler.Generate(Pattern(Figure("four-on-floor"), 2, voice: "HH", referencePulse: "beat1"));
        Assert.Equal(2, env.Diagram.Lanes.Count); // generated HH + the reference voice
        var refLane = env.Diagram.Lanes.Single(l => l.Voice != DrumVoice.HiHatClosed);
        Assert.Equal(DrumVoice.Kick, refLane.Voice);
        Assert.Equal(new[] { (0, 0), (1, 0) }, refLane.Hits.Select(h => (h.Bar, h.Tick)));
    }

    [Fact]
    public void ReferencePulse_Off_AddsNoReferenceRow()
    {
        var env = Handler.Generate(Pattern(Figure("four-on-floor"), 1));
        Assert.Single(env.Diagram.Lanes);
    }

    [Fact]
    public void UnknownFigure_FailsLoud()
    {
        Assert.Throws<FormatException>(() => Handler.Generate(Pattern(Figure("bogus"), 1)));
    }

    [Fact]
    public void UnknownStrategy_FailsLoud()
    {
        var request = new RhythmGenerationRequest("nope", 0, null, 100, null, null, null, null, null, null, null);
        Assert.Throws<FormatException>(() => Handler.Generate(request));
    }

    [Fact]
    public void BarCountOutOfRange_FailsLoud()
    {
        Assert.Throws<FormatException>(() => Handler.Generate(Pattern(Figure("four-on-floor"), 5)));
    }
}
