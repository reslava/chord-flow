using ChordFlow.Music.Progressions;
using ChordFlow.Music.Rhythm;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// Locks the canonical DSL spelling of the three MVP seed patterns and the <b>sustain-literal</b>
/// semantics the parser gives them (IN8). The DSL is the future authoring surface for these seeds; the
/// live <see cref="SeedData"/> migration (replacing the hand-built <see cref="RhythmEvent"/> arrays with
/// these DSL strings) changes rendering for Beat 1 / Beats 1 &amp; 3 — a sustained ring instead of a
/// staccato quarter — so it is deferred to slice 2 (EX2). These tests pin the target the migration must
/// produce.
/// </summary>
public class RhythmSeedDslTests
{
    // The canonical sustain-literal DSL for each seed (16 cells = one 4/4 bar at the default :4 grid).
    private const string Beat1Dsl = "X...............";
    private const string Beat1And3Dsl = "X.......X.......";
    private const string QuartersDsl = "X...X...X...X...";

    private static IReadOnlyList<(int Pos, int Len)> Parse(string dsl) =>
        RhythmPatternParser.Parse("seed", "Seed", dsl, TimeSignature.FourFour)
            .Bars[0].Events.Select(e => (e.Position, e.Length)).ToList();

    [Fact]
    public void Beat1_Dsl_RingsTheWholeBar()
    {
        // A single attack held by sustains to the bar end = one whole-bar note (not a staccato quarter).
        Assert.Equal(new[] { (0, 192) }, Parse(Beat1Dsl));
    }

    [Fact]
    public void Beat1And3_Dsl_IsTwoHalfNotes()
    {
        Assert.Equal(new[] { (0, 96), (96, 96) }, Parse(Beat1And3Dsl));
    }

    [Fact]
    public void Quarters_Dsl_IsFourQuarterHits()
    {
        Assert.Equal(new[] { (0, 48), (48, 48), (96, 48), (144, 48) }, Parse(QuartersDsl));
    }

    [Fact]
    public void Quarters_Dsl_RoundTripsTheLiveSeedExactly()
    {
        // Quarters is already sustain-literal (each quarter rings to the next), so its DSL parse equals
        // the live seed byte-for-byte — the migration of this one seed is a no-op.
        var live = SeedData.Quarters.Bars[0].Events.Select(e => (e.Position, e.Length));

        Assert.Equal(live, Parse(QuartersDsl));
    }

    [Fact]
    public void LiveSeeds_AreNowDslDerived_SingleSourceOfTruth()
    {
        // Slice 2 (IN6) migrated the live seeds to the sustain-literal DSL, so each constant now equals
        // its DSL parse exactly — Beat 1 rings the whole bar, Beats 1 & 3 are two halves. (The inverse of
        // the slice-1 guard, which asserted they diverged.)
        var beat1Live = SeedData.Beat1.Bars[0].Events.Select(e => (e.Position, e.Length));
        var beat13Live = SeedData.Beat1And3.Bars[0].Events.Select(e => (e.Position, e.Length));
        var quartersLive = SeedData.Quarters.Bars[0].Events.Select(e => (e.Position, e.Length));

        Assert.Equal(Parse(Beat1Dsl), beat1Live);
        Assert.Equal(Parse(Beat1And3Dsl), beat13Live);
        Assert.Equal(Parse(QuartersDsl), quartersLive);
    }
}
