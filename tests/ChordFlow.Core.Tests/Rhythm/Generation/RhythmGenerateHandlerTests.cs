using ChordFlow.Bridge;
using ChordFlow.Features.Rhythm;
using ChordFlow.Instruments.Drums;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// Tests the <c>rhythmGenerate</c> handler (req IN7): a valid Pattern / Random request returns tex + a
/// DrumGrooveDiagram whose hit ticks match the generated onsets; a bad token / out-of-range count fails loud
/// as a <see cref="FormatException"/> (which the host maps to <c>rhythmGenerateError</c>).
/// </summary>
public class RhythmGenerateHandlerTests
{
    private static readonly RhythmGenerateHandler Handler = new();

    private static RhythmGenerationRequest Pattern(
        string family, RhythmOperatorSpec op, RhythmBehaviourSpec behaviour, int barCount,
        string? voice = null, int seed = 0) =>
        new("pattern", seed, voice, 100, family, op, behaviour, barCount, null, null, null);

    private static int[] BarZeroTicks(RhythmGeneratedEnvelope env) =>
        env.Diagram.Lanes.Single().Hits.Where(h => h.Bar == 0).Select(h => h.Tick).ToArray();

    [Fact]
    public void Pattern_Uniform_ReturnsTexDiagramAndGridText()
    {
        var env = Handler.Generate(
            Pattern("quarter", new RhythmOperatorSpec("uniform"), new RhythmBehaviourSpec("repeat"), 1));

        Assert.False(string.IsNullOrWhiteSpace(env.Tex));
        Assert.Equal(new[] { 0, 48, 96, 144 }, BarZeroTicks(env));
        Assert.Equal("x x x x", env.Grid);
        Assert.Equal(DrumVoice.HiHatClosed, env.Diagram.Lanes.Single().Voice);
    }

    [Fact]
    public void Pattern_HonoursTheChosenDrumVoice()
    {
        var env = Handler.Generate(
            Pattern("quarter", new RhythmOperatorSpec("uniform"), new RhythmBehaviourSpec("repeat"), 1, voice: "SD"));
        Assert.Equal(DrumVoice.Snare, env.Diagram.Lanes.Single().Voice);
    }

    [Fact]
    public void Pattern_Mask_UsesTheOperatorArgs()
    {
        var env = Handler.Generate(
            Pattern("quarter", new RhythmOperatorSpec("mask", new[] { 1, 3 }), new RhythmBehaviourSpec("repeat"), 1));
        Assert.Equal(new[] { 48, 144 }, BarZeroTicks(env));
    }

    [Fact]
    public void Random_AllQuarters_FillsTheBar()
    {
        var request = new RhythmGenerationRequest(
            "random", 42, null, 100, null, null, null, null, new[] { 4 }, ContentBars: 1, SilenceBars: 0);
        var env = Handler.Generate(request);

        Assert.False(string.IsNullOrWhiteSpace(env.Tex));
        Assert.Equal(new[] { 0, 48, 96, 144 }, BarZeroTicks(env));
    }

    [Fact]
    public void UnknownOperator_FailsLoud()
    {
        Assert.Throws<FormatException>(() => Handler.Generate(
            Pattern("quarter", new RhythmOperatorSpec("bogus"), new RhythmBehaviourSpec("repeat"), 1)));
    }

    [Fact]
    public void UnknownStrategy_FailsLoud()
    {
        var request = new RhythmGenerationRequest(
            "nope", 0, null, 100, null, null, null, null, null, null, null);
        Assert.Throws<FormatException>(() => Handler.Generate(request));
    }

    [Fact]
    public void BarCountOutOfRange_FailsLoud()
    {
        Assert.Throws<FormatException>(() => Handler.Generate(
            Pattern("quarter", new RhythmOperatorSpec("uniform"), new RhythmBehaviourSpec("repeat"), 5)));
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
        var req = new RhythmGenerationRequest(
            "pattern", 0, "HH", 100, "quarter", new RhythmOperatorSpec("uniform"),
            new RhythmBehaviourSpec("repeat"), 2, null, null, null, ReferencePulse: "beat1");
        var env = Handler.Generate(req);

        Assert.Equal(2, env.Diagram.Lanes.Count); // generated HH + the reference voice
        var refLane = env.Diagram.Lanes.Single(l => l.Voice != DrumVoice.HiHatClosed);
        Assert.Equal(DrumVoice.Kick, refLane.Voice); // distinct from the HH generated voice
        Assert.Equal(new[] { (0, 0), (1, 0) }, refLane.Hits.Select(h => (h.Bar, h.Tick))); // beat 1 of each bar
    }

    [Fact]
    public void ReferencePulse_Off_AddsNoReferenceRow()
    {
        var env = Handler.Generate(
            Pattern("quarter", new RhythmOperatorSpec("uniform"), new RhythmBehaviourSpec("repeat"), 1));
        Assert.Single(env.Diagram.Lanes); // just the generated voice
    }
}
