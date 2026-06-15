using ChordFlow.Domain;
using ChordFlow.Rendering;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// End-to-end checks that the assembled engine pipeline (resolve progression → voicings/targets →
/// rhythm + feel → quantize → alphaTex) produces valid output on the new tick model.
/// </summary>
public class ExercisePipelineTests
{
    private static readonly AlphaTexRenderer Renderer = new();

    // Bb 12-bar blues rendered through the canonical Render(RealizedSong, …) path (Render(Exercise) was
    // dropped in the Exercise merge — decision (a)).
    private static string RenderBbBlues(RhythmPattern rhythm, Feel feel = Feel.Straight) =>
        Renderer.RenderProgression(
            new Key(new PitchClass(10), false), // Bb major
            SeedData.TwelveBarBlues, rhythm, 80, Difficulty.Beginner, feel);

    [Fact]
    public void Render_BbTwelveBarBlues_ProducesValidAlphaTexThroughTheNewPath()
    {
        string tex = RenderBbBlues(SeedData.Beat1And3);

        string[] lines = tex.Split('\n');

        // Header block ends with a lone ".".
        Assert.Equal("\\title \"12-Bar Blues — Bb\"", lines[0]);
        Assert.Contains("\\subtitle \"Beginner — Beats 1 & 3\"", tex);
        Assert.Contains("\\tempo 80", tex);
        Assert.Contains("\\ts 4 4", tex);
        Assert.Contains("\\ks bb", tex);
        Assert.Contains(".", lines);

        // 12 bars, each terminated by a pipe; Beats 1 & 3 ring as two half notes, so ":2" appears once.
        Assert.Equal(12, tex.Count(c => c == '|'));
        Assert.Equal(1, tex.Split(":2").Length - 1);

        // I = Bb7, IV = Eb7, V = F7 shell voicings all present through the VoicingBook strategy.
        Assert.Contains("(1.5 0.4 1.3)", tex); // Bb7
        Assert.Contains("(6.5 5.4 6.3)", tex); // Eb7
        Assert.Contains("(8.5 7.4 8.3)", tex); // F7

        // Every bar line is non-empty and ends with " |".
        foreach (string bar in tex.Split('\n').Where(l => l.Contains('|')))
        {
            Assert.EndsWith(" |", bar);
        }
    }

    [Fact]
    public void Render_StraightFeel_MatchesNoFeelOutput()
    {
        // Feel.Straight is the identity warp, so it must not change the rendered score.
        string straight = RenderBbBlues(SeedData.Quarters, Feel.Straight);
        string none = RenderBbBlues(SeedData.Quarters);

        Assert.Equal(none, straight);
    }

    [Fact]
    public void Pipeline_LeadTargetBranch_ResolvesGuideTonesForEveryChordOfTheProgression()
    {
        // The other content branch: resolve each realized chord's guide tones to the fretboard.
        var bb = new Key(new PitchClass(10), false);

        foreach (Chord chord in Transposer.Realize(SeedData.TwelveBarBlues, bb))
        {
            var guides = LeadTargets.GuideTones(chord);
            Assert.Equal(2, guides.Count); // dom7 => 3 and b7

            foreach (TargetZone zone in guides)
            {
                var positions = LeadTargets.Resolve(chord, zone);
                int wantPc = LeadTargets.PitchClassOf(chord, zone).Value;
                Assert.NotEmpty(positions);
                Assert.All(positions, p => Assert.Equal(wantPc, NotePc(p)));
            }
        }
    }

    // Open-string pitch class by alphaTab string number (1 = high E .. 6 = low E), standard tuning.
    private static int NotePc(FretPosition p)
    {
        int[] openPc = { 0, 4, 11, 7, 2, 9, 4 };
        return (openPc[p.String] + p.Fret) % 12;
    }
}
