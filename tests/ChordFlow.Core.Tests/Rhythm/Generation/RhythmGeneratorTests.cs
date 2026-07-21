using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Rhythm.Generation;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// Tests the rhythm generation engine (req IN2/IN3/IN4/IN6): the Pattern strategy over bar-pattern kinds
/// (selection + behaviours + Displace), the Random strategy (+ rests), and determinism. Grids compare by
/// their canonical per-bar onset-tick arrays.
/// </summary>
public class RhythmGeneratorTests
{
    private static readonly TimeSignature Ts = TimeSignature.FourFour;

    private static int[][] Canonical(OnsetGrid g) =>
        g.Bars.Select(b => b.OnsetTicks(g.TimeSignature).ToArray()).ToArray();

    private static OnsetGrid Pattern(
        RhythmKind kind, PatternSelection selection, int bars, int seed = 0, params SequenceBehaviour[] behaviours) =>
        PatternStrategy.Generate(new PatternParams(kind, selection, behaviours, bars, Ts, seed));

    private static RhythmKind Figure(string id) => GrooveFigures.ById(id)!;

    // --- Pattern strategy: selection ---------------------------------------

    [Fact]
    public void Fixed_RepeatsTheChosenPattern()
    {
        var g = Pattern(RhythmKind.Density(1, 2), new PatternSelection.Fixed(0), 2);
        // Density(1,2)[0] = cells {0,1} = beats 1&2 → ticks [0,48]
        Assert.Equal(new[] { new[] { 0, 48 }, new[] { 0, 48 } }, Canonical(g));
    }

    [Fact]
    public void Cycle_ToursTheKind()
    {
        var g = Pattern(RhythmKind.Density(1, 2), new PatternSelection.Cycle(), 3);
        Assert.Equal(new[] { new[] { 0, 48 }, new[] { 0, 96 }, new[] { 0, 144 } }, Canonical(g));
    }

    [Fact]
    public void RandomInKind_SameSeed_SameGrid()
    {
        var kind = RhythmKind.Density(2, 3);
        Assert.Equal(
            Canonical(Pattern(kind, new PatternSelection.RandomInKind(), 4, seed: 9)),
            Canonical(Pattern(kind, new PatternSelection.RandomInKind(), 4, seed: 9)));
    }

    [Fact]
    public void FixedPlusRotating_AlternatesFixedAndCycling()
    {
        var g = Pattern(RhythmKind.Density(1, 2), new PatternSelection.FixedPlusRotating(0), 4);
        // even bars = fixed[0]=[0,48]; odd bar1 = cycle[0]=[0,48]; odd bar3 = cycle[1]=[0,96]
        Assert.Equal(new[] { new[] { 0, 48 }, new[] { 0, 48 }, new[] { 0, 48 }, new[] { 0, 96 } }, Canonical(g));
    }

    // --- Pattern strategy: figures + behaviours ----------------------------

    [Fact]
    public void Figure_Backbeat_SoundsBeats2And4()
    {
        Assert.Equal(new[] { new[] { 48, 144 } }, Canonical(Pattern(Figure("backbeat"), new PatternSelection.Fixed(0), 1)));
    }

    [Fact]
    public void Displace_ShiftsDownbeatsToTheBackbeat()
    {
        // downbeats x.x. displaced 1 cell → .x.x = backbeat
        var g = Pattern(Figure("downbeats"), new PatternSelection.Fixed(0), 1, behaviours: new SequenceBehaviour.Displace(1));
        Assert.Equal(new[] { new[] { 48, 144 } }, Canonical(g));
    }

    [Fact]
    public void Sweep_WalksTheSingleOnsetAcrossTheBars()
    {
        var g = Pattern(Figure("beat1"), new PatternSelection.Fixed(0), 4, behaviours: new SequenceBehaviour.Sweep());
        Assert.Equal(new[] { new[] { 0 }, new[] { 48 }, new[] { 96 }, new[] { 144 } }, Canonical(g));
    }

    [Fact]
    public void RestBar_InsertsSilentBars()
    {
        var g = Pattern(Figure("four-on-floor"), new PatternSelection.Fixed(0), 4, behaviours: new SequenceBehaviour.RestBar());
        Assert.Equal(
            new[] { new[] { 0, 48, 96, 144 }, Array.Empty<int>(), new[] { 0, 48, 96, 144 }, Array.Empty<int>() },
            Canonical(g));
    }

    [Fact]
    public void CallResponse_SilencesOddBars()
    {
        var g = Pattern(Figure("four-on-floor"), new PatternSelection.Fixed(0), 2, behaviours: new SequenceBehaviour.CallResponse());
        Assert.Equal(new[] { new[] { 0, 48, 96, 144 }, Array.Empty<int>() }, Canonical(g));
    }

    [Fact]
    public void Clave_CycleProducesTheTwoBarLoop()
    {
        var g = Pattern(Figure("son-clave-32"), new PatternSelection.Cycle(), 2);
        Assert.Equal(new[] { new[] { 0, 72, 144 }, new[] { 48, 96 } }, Canonical(g)); // 3-side then 2-side
    }

    [Fact]
    public void Pattern_RejectsBarCountOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Pattern(RhythmKind.Density(1, 2), new PatternSelection.Fixed(0), 5));
    }

    // --- Random strategy ---------------------------------------------------

    [Fact]
    public void Random_SameSeed_SameGrid()
    {
        var p = new RandomParams(new[] { 4, 8, 16 }, ContentBars: 3, SilenceBars: 1, Ts, Seed: 123);
        Assert.Equal(Canonical(RandomStrategy.Generate(p)), Canonical(RandomStrategy.Generate(p)));
    }

    [Fact]
    public void Random_AllQuarters_FillsEveryBeat_ThenSilenceBars()
    {
        var p = new RandomParams(new[] { 4 }, ContentBars: 1, SilenceBars: 1, Ts, Seed: 42);
        Assert.Equal(new[] { new[] { 0, 48, 96, 144 }, Array.Empty<int>() }, Canonical(RandomStrategy.Generate(p)));
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
        GenerationParams pattern = new PatternParams(
            RhythmKind.Density(1, 1), new PatternSelection.Fixed(0), Array.Empty<SequenceBehaviour>(), 1, Ts, 0);
        GenerationParams random = new RandomParams(new[] { 4 }, 1, 0, Ts, 0);

        Assert.Equal(new[] { 0 }, Canonical(RhythmGenerator.Generate(pattern))[0]); // Density(1,1)[0] = {0}
        Assert.Equal(new[] { 0, 48, 96, 144 }, Canonical(RhythmGenerator.Generate(random))[0]);
    }
}
