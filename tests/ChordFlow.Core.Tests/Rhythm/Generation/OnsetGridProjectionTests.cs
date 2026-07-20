using ChordFlow.Instruments.Drums;
using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Rhythm.Generation;
using ChordFlow.Rendering;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// Tests the two projections off one <see cref="OnsetGrid"/>: the legato <see cref="RhythmPattern"/> path
/// (design §2a), the single-lane <see cref="DrumGroove"/> path (design §2b), their onset agreement
/// (req C6), and that the legato projection stays inside the verified render vocabulary (req C4).
/// </summary>
public class OnsetGridProjectionTests
{
    private static readonly TimeSignature Ts = TimeSignature.FourFour;

    private static OnsetGrid Pattern(
        RhythmFamily family, BarOperator op, SequenceBehaviour behaviour, int bars, int seed = 0) =>
        PatternStrategy.Generate(new PatternParams(family, op, behaviour, bars, Ts, seed));

    private static (int Pos, int Len) PL(RhythmEvent e) => (e.Position, e.Length);

    // --- Legato RhythmPattern projection -----------------------------------

    [Fact]
    public void Legato_Quarters_RingEachToTheNextOnset()
    {
        var g = Pattern(RhythmFamily.Quarter, new BarOperator.Uniform(), new SequenceBehaviour.Repeat(), 1);
        var bar = OnsetGridToRhythmPattern.Project(g).Bars[0];
        Assert.Equal(new[] { (0, 48), (48, 48), (96, 48), (144, 48) }, bar.Events.Select(PL));
    }

    [Fact]
    public void Legato_SingleOnset_RingsToTheBarline()
    {
        var g = Pattern(RhythmFamily.Quarter, new BarOperator.Isolate(1), new SequenceBehaviour.Repeat(), 1);
        var bar = OnsetGridToRhythmPattern.Project(g).Bars[0];
        // beat 2 onset (tick 48) rings the remaining three beats — a dotted half.
        Assert.Equal(new[] { (48, 144) }, bar.Events.Select(PL));
    }

    [Fact]
    public void Legato_EmptyBar_ProducesNoEvents()
    {
        var g = Pattern(RhythmFamily.Quarter, new BarOperator.Uniform(), new SequenceBehaviour.RestBar(), 2);
        var pattern = OnsetGridToRhythmPattern.Project(g);
        Assert.NotEmpty(pattern.Bars[0].Events);
        Assert.Empty(pattern.Bars[1].Events);
    }

    // --- Single-lane DrumGroove projection ---------------------------------

    [Fact]
    public void Drums_ProjectsToOneVoice_DefaultsClosedHiHat()
    {
        var g = Pattern(RhythmFamily.Eighth, new BarOperator.Uniform(), new SequenceBehaviour.Cycle(), 3);
        var groove = OnsetGridToDrumGroove.Project(g);
        Assert.Equal(new[] { DrumVoice.HiHatClosed }, groove.DistinctVoices());
    }

    [Fact]
    public void Drums_Onsets_MapOneToOne()
    {
        var g = Pattern(RhythmFamily.Eighth, new BarOperator.Displace(1), new SequenceBehaviour.Repeat(), 1);
        var lane = OnsetGridToDrumGroove.Project(g).Bars[0].Lanes.Single();
        Assert.Equal(new[] { 24, 72, 120, 168 }, lane.Events.Select(e => e.Position));
    }

    // --- Projection agreement (req C6) -------------------------------------

    [Theory]
    [MemberData(nameof(AgreementGrids))]
    public void RhythmPatternAndDrumGroove_AgreeOnOnsetTicks(OnsetGrid grid)
    {
        var pattern = OnsetGridToRhythmPattern.Project(grid);
        var groove = OnsetGridToDrumGroove.Project(grid);

        Assert.Equal(pattern.Bars.Count, groove.Bars.Count);
        for (int b = 0; b < pattern.Bars.Count; b++)
        {
            int[] rp = pattern.Bars[b].Events.Select(e => e.Position).OrderBy(x => x).ToArray();
            int[] dg = groove.Bars[b].Lanes.SelectMany(l => l.Events).Select(e => e.Position).OrderBy(x => x).ToArray();
            Assert.Equal(rp, dg);
        }
    }

    public static IEnumerable<object[]> AgreementGrids() => new[]
    {
        new object[] { Pattern(RhythmFamily.Quarter, new BarOperator.Uniform(), new SequenceBehaviour.Repeat(), 1) },
        new object[] { Pattern(RhythmFamily.Eighth, new BarOperator.AnchorRotate(), new SequenceBehaviour.Cycle(), 4) },
        new object[] { Pattern(RhythmFamily.Quarter, new BarOperator.Isolate(0), new SequenceBehaviour.Sweep(), 4) },
        new object[] { RandomStrategy.Generate(new RandomParams(new[] { 4, 8, 16 }, 3, 1, TimeSignature.FourFour, 99)) },
    };

    // --- Verified render vocabulary (req C4) -------------------------------

    [Theory]
    [MemberData(nameof(AgreementGrids))]
    public void Legato_QuantizesWithoutHittingAnUnverifiedTie(OnsetGrid grid)
    {
        var pattern = OnsetGridToRhythmPattern.Project(grid);
        foreach (var bar in pattern.Bars)
        {
            // Throws (FormatException) if a syncopated/dotted value that needs an unverified tie reaches it.
            var slots = RhythmQuantizer.Quantize(bar.Events, grid.TimeSignature);
            Assert.NotNull(slots);
        }
    }
}
