using ChordFlow.Exercises;
using ChordFlow.Music.Harmony;
using ChordFlow.Music.Melody;
using ChordFlow.Music.Progressions;
using ChordFlow.Music.Rhythm;
using ChordFlow.Rendering;
using Xunit;

using ChordFlow.Instruments.Guitar;

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
    private static string RenderBbBlues(RhythmPattern rhythm, TripletFeel tripletFeel = TripletFeel.None) =>
        Renderer.RenderProgression(
            new Key(new PitchClass(10), false), // Bb major
            SeedData.TwelveBarBlues, rhythm, 80, Difficulty.Beginner, tripletFeel);

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
    public void Render_NoneFeel_EmitsNoTfAndMatchesDefaultOutput()
    {
        // TripletFeel.None emits no \tf directive, so it must be byte-identical to the default (no-feel) render.
        string none = RenderBbBlues(SeedData.Quarters, TripletFeel.None);
        string dflt = RenderBbBlues(SeedData.Quarters);

        Assert.Equal(dflt, none);
        Assert.DoesNotContain("\\tf", none);
    }

    [Fact]
    public void Render_Triplet8thFeel_EmitsWholeSongTfOnTheFirstBar()
    {
        // Swing is delegated to alphaTab: a swung feel emits one \tf on the first bar (bar metadata, ahead of
        // the bar's beats), and the pattern itself stays straight (12 bars, unchanged pipe count).
        string swung = RenderBbBlues(SeedData.Beat1And3, TripletFeel.Triplet8th);

        Assert.Contains("\\tf triplet8th ", swung);
        Assert.Equal(1, swung.Split("\\tf").Length - 1); // exactly one \tf — whole-song, not per-bar
        Assert.Equal(12, swung.Count(c => c == '|'));     // bar count unchanged by the directive
        // The \tf sits at the very start of the first bar's content.
        string firstBar = swung.Split('\n').First(l => l.Contains('|'));
        Assert.StartsWith("\\tf triplet8th ", firstBar);
    }

    [Fact]
    public void Pipeline_LeadTargetBranch_ResolvesGuideTonesForEveryChordOfTheProgression()
    {
        // The other content branch: resolve each realized chord's guide tones to the fretboard
        // (fret resolution is now a guitar concern — GuitarInstrument.ResolveLead).
        var bb = new Key(new PitchClass(10), false);
        var guitar = new GuitarInstrument();

        foreach (Chord chord in Transposer.Realize(SeedData.TwelveBarBlues, bb))
        {
            var guides = LeadTargets.GuideTones(chord);
            Assert.Equal(2, guides.Count); // dom7 => 3 and b7

            foreach (TargetZone zone in guides)
            {
                var positions = guitar.ResolveLead(chord, zone);
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
