using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Rhythm.Generation;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// Tests the rhythm generation engine: determinism (req IN6/C7) and the per-operator / per-behaviour /
/// per-strategy onset shapes (req IN2/IN3/IN4). Grids are compared by their canonical per-bar onset-tick
/// arrays (structural equality — the model deliberately keeps codebase-precedent record equality).
/// </summary>
public class RhythmGeneratorTests
{
    private static readonly TimeSignature Ts = TimeSignature.FourFour;

    // A grid's canonical form: bar-by-bar bar-relative onset ticks.
    private static int[][] Canonical(OnsetGrid g) =>
        g.Bars.Select(b => b.OnsetTicks(g.TimeSignature).ToArray()).ToArray();

    private static OnsetGrid Pattern(
        RhythmFamily family, BarOperator op, SequenceBehaviour behaviour, int bars, int seed = 0) =>
        PatternStrategy.Generate(new PatternParams(family, op, behaviour, bars, Ts, seed));

    // --- Determinism -------------------------------------------------------

    [Fact]
    public void Pattern_SameSeed_SameGrid()
    {
        var a = Pattern(RhythmFamily.Eighth, new BarOperator.AnchorRotate(), new SequenceBehaviour.Cycle(), 4, seed: 7);
        var b = Pattern(RhythmFamily.Eighth, new BarOperator.AnchorRotate(), new SequenceBehaviour.Cycle(), 4, seed: 7);
        Assert.Equal(Canonical(a), Canonical(b));
    }

    [Fact]
    public void Random_SameSeed_SameGrid()
    {
        var p = new RandomParams(new[] { 4, 8, 16 }, ContentBars: 3, SilenceBars: 1, Ts, Seed: 123);
        Assert.Equal(Canonical(RandomStrategy.Generate(p)), Canonical(RandomStrategy.Generate(p)));
    }

    // --- Bar operators (single-bar Repeat) ---------------------------------

    [Fact]
    public void Uniform_Quarter_AllFourBeatsSound()
    {
        var g = Pattern(RhythmFamily.Quarter, new BarOperator.Uniform(), new SequenceBehaviour.Repeat(), 1);
        Assert.Equal(new[] { 0, 48, 96, 144 }, Canonical(g)[0]);
    }

    [Fact]
    public void Isolate_Quarter_OnlyTheChosenBeatSounds()
    {
        var g = Pattern(RhythmFamily.Quarter, new BarOperator.Isolate(2), new SequenceBehaviour.Repeat(), 1);
        Assert.Equal(new[] { 96 }, Canonical(g)[0]);
    }

    [Fact]
    public void Mask_Quarter_Backbeat_SoundsBeats2And4()
    {
        var g = Pattern(RhythmFamily.Quarter, new BarOperator.Mask(new[] { 1, 3 }), new SequenceBehaviour.Repeat(), 1);
        Assert.Equal(new[] { 48, 144 }, Canonical(g)[0]);
    }

    [Fact]
    public void Displace_Eighth_MovesEveryOnsetToTheAnd()
    {
        var g = Pattern(RhythmFamily.Eighth, new BarOperator.Displace(1), new SequenceBehaviour.Repeat(), 1);
        Assert.Equal(new[] { 24, 72, 120, 168 }, Canonical(g)[0]);
    }

    [Fact]
    public void AnchorRotate_Eighth_FixesBeat1AndRotatesTheRest()
    {
        var g = Pattern(RhythmFamily.Eighth, new BarOperator.AnchorRotate(), new SequenceBehaviour.Repeat(), 1);
        // beat0 [0]→0 · beat1 blocks[1]=[&]→72 · beat2 blocks[2]=[on,&]→96,120 · beat3 blocks[0]=[on]→144
        Assert.Equal(new[] { 0, 72, 96, 120, 144 }, Canonical(g)[0]);
    }

    [Fact]
    public void Accumulate_Quarter_SoundsTheFirstNBeats()
    {
        var g = Pattern(RhythmFamily.Quarter, new BarOperator.Accumulate(2), new SequenceBehaviour.Repeat(), 1);
        Assert.Equal(new[] { 0, 48 }, Canonical(g)[0]);
    }

    [Fact]
    public void Thin_Quarter_DropsTheLastNBeats()
    {
        var g = Pattern(RhythmFamily.Quarter, new BarOperator.Thin(1), new SequenceBehaviour.Repeat(), 1);
        Assert.Equal(new[] { 0, 48, 96 }, Canonical(g)[0]);
    }

    // --- Sequence behaviours (multi-bar) -----------------------------------

    [Fact]
    public void Sweep_Isolate_WalksTheOnsetAcrossTheBars()
    {
        var g = Pattern(RhythmFamily.Quarter, new BarOperator.Isolate(0), new SequenceBehaviour.Sweep(), 4);
        Assert.Equal(new[] { new[] { 0 }, new[] { 48 }, new[] { 96 }, new[] { 144 } }, Canonical(g));
    }

    [Fact]
    public void RestBar_AlternatesContentAndSilence()
    {
        var g = Pattern(RhythmFamily.Quarter, new BarOperator.Uniform(), new SequenceBehaviour.RestBar(), 4);
        Assert.Equal(
            new[] { new[] { 0, 48, 96, 144 }, Array.Empty<int>(), new[] { 0, 48, 96, 144 }, Array.Empty<int>() },
            Canonical(g));
    }

    [Fact]
    public void CallResponse_EvenBarsSound_OddBarsAreSilent()
    {
        var g = Pattern(RhythmFamily.Quarter, new BarOperator.Uniform(), new SequenceBehaviour.CallResponse(), 2);
        Assert.Equal(new[] { new[] { 0, 48, 96, 144 }, Array.Empty<int>() }, Canonical(g));
    }

    [Fact]
    public void Cycle_Eighth_ToursTheFamilyBlocksBarByBar()
    {
        var g = Pattern(RhythmFamily.Eighth, new BarOperator.Uniform(), new SequenceBehaviour.Cycle(), 3);
        Assert.Equal(
            new[]
            {
                new[] { 0, 48, 96, 144 },                          // blocks[0] = on-beat every beat
                new[] { 24, 72, 120, 168 },                        // blocks[1] = the & every beat
                new[] { 0, 24, 48, 72, 96, 120, 144, 168 },        // blocks[2] = both every beat
            },
            Canonical(g));
    }

    // --- Random strategy ---------------------------------------------------

    [Fact]
    public void Random_AllQuarters_FillsEveryBeat_ThenSilenceBars()
    {
        var p = new RandomParams(new[] { 4 }, ContentBars: 1, SilenceBars: 1, Ts, Seed: 42);
        var g = RandomStrategy.Generate(p);
        Assert.Equal(new[] { new[] { 0, 48, 96, 144 }, Array.Empty<int>() }, Canonical(g));
    }

    [Fact]
    public void Random_RejectsOffGridValue()
    {
        var p = new RandomParams(new[] { 3 }, ContentBars: 1, SilenceBars: 0, Ts, Seed: 1);
        Assert.Throws<ArgumentException>(() => RandomStrategy.Generate(p));
    }

    [Fact]
    public void Random_RestProbabilityOne_ProducesAnEmptyBar()
    {
        var p = new RandomParams(new[] { 4 }, ContentBars: 1, SilenceBars: 0, Ts, Seed: 5, RestProbability: 1.0);
        Assert.Empty(Canonical(RandomStrategy.Generate(p))[0]);
    }

    [Fact]
    public void Random_RestProbabilityZero_FillsSolid()
    {
        var p = new RandomParams(new[] { 4 }, ContentBars: 1, SilenceBars: 0, Ts, Seed: 5, RestProbability: 0.0);
        Assert.Equal(new[] { 0, 48, 96, 144 }, Canonical(RandomStrategy.Generate(p))[0]);
    }

    [Fact]
    public void Random_RestProbability_ThinsOutOnsets()
    {
        int Count(double rest) => Canonical(RandomStrategy.Generate(
            new RandomParams(new[] { 4, 8 }, ContentBars: 2, SilenceBars: 0, Ts, Seed: 99, RestProbability: rest)))
            .Sum(bar => bar.Length);
        Assert.True(Count(0.0) > 0);
        Assert.True(Count(0.0) >= Count(0.5));
        Assert.True(Count(0.5) >= Count(1.0));
        Assert.Equal(0, Count(1.0));
    }

    [Fact]
    public void Random_SameSeedWithRests_SameGrid()
    {
        var p = new RandomParams(new[] { 4, 8, 16 }, ContentBars: 2, SilenceBars: 1, Ts, Seed: 7, RestProbability: 0.4);
        Assert.Equal(Canonical(RandomStrategy.Generate(p)), Canonical(RandomStrategy.Generate(p)));
    }

    [Fact]
    public void Random_RejectsRestProbabilityOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RandomStrategy.Generate(
            new RandomParams(new[] { 4 }, ContentBars: 1, SilenceBars: 0, Ts, Seed: 0, RestProbability: 1.5)));
    }

    // --- Generator dispatch ------------------------------------------------

    [Fact]
    public void Generate_DispatchesOnStrategyArm()
    {
        GenerationParams pattern =
            new PatternParams(RhythmFamily.Quarter, new BarOperator.Uniform(), new SequenceBehaviour.Repeat(), 1, Ts, 0);
        GenerationParams random = new RandomParams(new[] { 4 }, 1, 0, Ts, 0);

        Assert.Equal(new[] { 0, 48, 96, 144 }, Canonical(RhythmGenerator.Generate(pattern))[0]);
        Assert.Equal(new[] { 0, 48, 96, 144 }, Canonical(RhythmGenerator.Generate(random))[0]);
    }

    [Fact]
    public void Pattern_RejectsBarCountOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Pattern(RhythmFamily.Quarter, new BarOperator.Uniform(), new SequenceBehaviour.Repeat(), 5));
    }
}
