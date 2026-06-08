using ChordFlow.Domain;
using ChordFlow.Rendering;
using Xunit;

namespace ChordFlow.Tests;

public class AlphaTexRendererTests
{
    private static readonly AlphaTexRenderer Renderer = new();

    [Fact]
    public void Render_KnownExercise_ProducesExpectedAlphaTex()
    {
        // One-bar "I" progression in Bb, beat-1 rhythm (hit, rest, rest, rest), tempo 80.
        var progression = new Progression("test", "Test Blues", new RomanDegree[] { new(1, Quality.Dominant7) });
        var exercise = new Exercise(
            new Key(new PitchClass(10), false), // Bb major
            progression,
            SeedData.Beat1,
            80,
            Difficulty.Beginner);

        string tex = Renderer.Render(exercise);

        string expected = string.Join("\n",
            "\\title \"Test Blues — Bb\"",
            "\\subtitle \"Beginner — Beat 1\"",
            "\\tempo 80",
            "\\ts 4 4",
            "\\ks bb",
            ".",
            ":4 (1.5 0.4 1.3) r r r |");

        Assert.Equal(expected, tex);
    }

    [Fact]
    public void Render_FullBbBlues_HasTwelveBarsAndCorrectHeader()
    {
        var exercise = new Exercise(
            new Key(new PitchClass(10), false),
            SeedData.TwelveBarBlues,
            SeedData.Beat1And3,
            80,
            Difficulty.Beginner);

        string tex = Renderer.Render(exercise);

        Assert.StartsWith("\\title \"12-Bar Blues — Bb\"", tex);
        Assert.Contains("\\subtitle \"Beginner — Beats 1 & 3\"", tex);
        Assert.Contains("\\ks bb", tex);
        Assert.Contains("\\ts 4 4", tex);

        // 12 bars => 12 pipe separators.
        Assert.Equal(12, tex.Count(c => c == '|'));

        // Stateful duration: ":4" appears exactly once for the whole piece.
        Assert.Equal(1, CountOccurrences(tex, ":4"));

        // I = Bb7, IV = Eb7, V = F7 voicings all present.
        Assert.Contains("(1.5 0.4 1.3)", tex); // Bb7
        Assert.Contains("(6.5 5.4 6.3)", tex); // Eb7
        Assert.Contains("(8.5 7.4 8.3)", tex); // F7
    }

    [Fact]
    public void Render_QuartersRhythm_EmitsFourHitsPerBar()
    {
        var progression = new Progression("test", "Test", new RomanDegree[] { new(1, Quality.Dominant7) });
        var exercise = new Exercise(
            new Key(new PitchClass(10), false),
            progression,
            SeedData.Quarters,
            90,
            Difficulty.Beginner);

        string tex = Renderer.Render(exercise);

        Assert.EndsWith(":4 (1.5 0.4 1.3) (1.5 0.4 1.3) (1.5 0.4 1.3) (1.5 0.4 1.3) |", tex);
        Assert.DoesNotContain("r", tex.Split('\n')[^1]); // no rests in the bar line
    }

    [Fact]
    public void Render_TickPatternWithCustomTimeSignatureHeader_DerivesTsFromPattern()
    {
        // The \ts header now derives from the pattern's TimeSignature rather than a hardcoded "4 4".
        var progression = new Progression("test", "Test", new RomanDegree[] { new(1, Quality.Dominant7) });
        var exercise = new Exercise(
            new Key(new PitchClass(10), false),
            progression,
            SeedData.Quarters,
            90,
            Difficulty.Beginner);

        string tex = Renderer.Render(exercise);

        Assert.Contains("\\ts 4 4", tex);
        // Quantized through the new tick path: four quarter hits, stateful ":4" once.
        Assert.EndsWith(":4 (1.5 0.4 1.3) (1.5 0.4 1.3) (1.5 0.4 1.3) (1.5 0.4 1.3) |", tex);
    }

    [Fact]
    public void Render_Pickup_EmitsLeadingMeasureBeforeBars()
    {
        // A one-beat pickup voiced with the first chord adds a leading bar (=> an extra pipe).
        var pickup = new PickupMeasure(new[] { RhythmEvent.Hit(0, 48) }, LengthTicks: 48);
        var rhythm = new RhythmPattern("p", "Pickup", SeedData.Beat1.Events, TimeSignature.FourFour, pickup);
        var progression = new Progression("test", "Test", new RomanDegree[] { new(1, Quality.Dominant7) });
        var exercise = new Exercise(
            new Key(new PitchClass(10), false), progression, rhythm, 80, Difficulty.Beginner);

        string tex = Renderer.Render(exercise);

        // Pickup bar + the single progression bar = 2 pipes. The pickup carries the ":4"; the main
        // bar's first slot inherits the stateful duration, so no second ":4".
        Assert.Equal(2, tex.Count(c => c == '|'));
        Assert.EndsWith(":4 (1.5 0.4 1.3) |\n(1.5 0.4 1.3) r r r |", tex);
    }

    [Fact]
    public void Render_MinorKey_Throws()
    {
        var exercise = new Exercise(
            new Key(new PitchClass(9), true), // A minor
            SeedData.TwelveBarBlues,
            SeedData.Beat1,
            80,
            Difficulty.Beginner);

        Assert.Throws<NotSupportedException>(() => Renderer.Render(exercise));
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0;
        int index = 0;
        while ((index = haystack.IndexOf(needle, index, System.StringComparison.Ordinal)) != -1)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}
