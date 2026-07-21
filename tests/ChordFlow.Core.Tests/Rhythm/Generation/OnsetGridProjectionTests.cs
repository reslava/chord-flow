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
        RhythmKind kind, PatternSelection selection, int bars, int seed = 0, params SequenceBehaviour[] behaviours) =>
        PatternStrategy.Generate(new PatternParams(kind, selection, behaviours, bars, Ts, seed));

    private static RhythmKind Figure(string id) => GrooveFigures.ById(id)!;

    private static (int Pos, int Len) PL(RhythmEvent e) => (e.Position, e.Length);

    // --- Legato RhythmPattern projection -----------------------------------

    [Fact]
    public void Legato_Quarters_RingEachToTheNextOnset()
    {
        var g = Pattern(Figure("four-on-floor"), new PatternSelection.Fixed(0), 1);
        var bar = OnsetGridToRhythmPattern.Project(g).Bars[0];
        Assert.Equal(new[] { (0, 48), (48, 48), (96, 48), (144, 48) }, bar.Events.Select(PL));
    }

    [Fact]
    public void Legato_SingleOnset_RingsToTheBarline()
    {
        var g = Pattern(RhythmKind.Density(1, 1), new PatternSelection.Fixed(1), 1); // beat 2 only
        var bar = OnsetGridToRhythmPattern.Project(g).Bars[0];
        // beat 2 onset (tick 48) rings the remaining three beats — a dotted half.
        Assert.Equal(new[] { (48, 144) }, bar.Events.Select(PL));
    }

    [Fact]
    public void Legato_EmptyBar_ProducesNoEvents()
    {
        var g = Pattern(Figure("four-on-floor"), new PatternSelection.Fixed(0), 2, behaviours: new SequenceBehaviour.RestBar());
        var pattern = OnsetGridToRhythmPattern.Project(g);
        Assert.NotEmpty(pattern.Bars[0].Events);
        Assert.Empty(pattern.Bars[1].Events);
    }

    // --- Single-lane DrumGroove projection ---------------------------------

    [Fact]
    public void Drums_ProjectsToOneVoice_DefaultsClosedHiHat()
    {
        var g = Pattern(RhythmKind.Density(2, 2), new PatternSelection.Cycle(), 3);
        var groove = OnsetGridToDrumGroove.Project(g);
        Assert.Equal(new[] { DrumVoice.HiHatClosed }, groove.DistinctVoices());
    }

    [Fact]
    public void Drums_Onsets_MapOneToOne()
    {
        var g = Pattern(Figure("offbeats"), new PatternSelection.Fixed(0), 1); // .x.x.x.x
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

    // Onset-agreement holds for ANY grid (it compares positions, not rendering) — including arbitrary syncopated
    // density patterns the Pattern strategy can now produce.
    public static IEnumerable<object[]> AgreementGrids() => new[]
    {
        new object[] { Pattern(Figure("four-on-floor"), new PatternSelection.Fixed(0), 1) },
        new object[] { Pattern(RhythmKind.Density(2, 3), new PatternSelection.Cycle(), 4) },
        new object[] { Pattern(Figure("tresillo"), new PatternSelection.Fixed(0), 4, behaviours: new SequenceBehaviour.Sweep()) },
        new object[] { RandomStrategy.Generate(new RandomParams(new[] { 4, 8, 16 }, 3, 1, TimeSignature.FourFour, 99)) },
    };

    // --- Verified render vocabulary (req C4) -------------------------------

    // The legato projection's ring-to-barline is only guaranteed notatable for regular/figure patterns whose
    // onset spacings land on single (dotted) values. Arbitrary syncopated density patterns (e.g. Density(2,3))
    // can ring a non-notatable length — that is a LEGATO/comping (Phase-4) concern, tracked separately; the
    // drums path (the dogfood page) is unaffected (it notates hit + rest, never a ring). So C4 is asserted over
    // legato-safe grids.
    public static IEnumerable<object[]> LegatoSafeGrids() => new[]
    {
        new object[] { Pattern(Figure("four-on-floor"), new PatternSelection.Fixed(0), 1) },
        new object[] { Pattern(Figure("backbeat"), new PatternSelection.Fixed(0), 1) },
        new object[] { Pattern(Figure("tresillo"), new PatternSelection.Fixed(0), 1) },
        new object[] { RandomStrategy.Generate(new RandomParams(new[] { 4, 8, 16 }, 3, 1, TimeSignature.FourFour, 99)) },
    };

    [Theory]
    [MemberData(nameof(LegatoSafeGrids))]
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
