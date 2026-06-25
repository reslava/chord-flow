using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;
using Xunit;

namespace ChordFlow.Core.Tests;

public class ClosestRankingTests
{
    private readonly ClosestRanking _ranking = new();
    private static readonly Chord C7 = new(new PitchClass(0), Quality.Dominant7);
    private static readonly Chord F7 = new(new PitchClass(5), Quality.Dominant7);

    private static Voicing Grip(int fret) => new(
        new[] { new FretPosition(4, fret), new FretPosition(3, fret), new FretPosition(2, fret) },
        FirstFret: fret);

    [Fact]
    public void FirstChord_PicksLowestFretGrip()
    {
        var ctx = new VoicingRankingContext();

        Voicing chosen = _ranking.Pick(C7, new[] { Grip(7), Grip(2), Grip(5) }, ctx);

        Assert.Equal(2, chosen.FirstFret);
    }

    [Fact]
    public void RepeatedChord_ReusesItsEarlierGrip()
    {
        var ctx = new VoicingRankingContext();
        Voicing first = _ranking.Pick(C7, new[] { Grip(5), Grip(2) }, ctx); // → fret 2
        _ranking.Pick(F7, new[] { Grip(3) }, ctx);

        // C7 reappears with only far candidates — Closest must still reuse the earlier fret-2 grip.
        Voicing again = _ranking.Pick(C7, new[] { Grip(9), Grip(12) }, ctx);

        Assert.Same(first, again);
        Assert.Equal(2, again.FirstFret);
    }

    [Fact]
    public void NextNewChord_PicksClosestByPerStringDistance()
    {
        var ctx = new VoicingRankingContext();
        _ranking.Pick(C7, new[] { Grip(5) }, ctx); // previous grip anchored at fret 5

        Voicing chosen = _ranking.Pick(F7, new[] { Grip(12), Grip(6) }, ctx);

        Assert.Equal(6, chosen.FirstFret); // |6-5|×3 = 3 beats |12-5|×3 = 21
    }
}
