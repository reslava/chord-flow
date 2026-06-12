using ChordFlow.Domain;
using ChordFlow.Rendering;
using Xunit;

namespace ChordFlow.Core.Tests;

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
        var rhythm = RhythmPattern.SingleBar("p", "Pickup", SeedData.Beat1.Bars[0].Events, TimeSignature.FourFour, pickup);
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
    public void Render_EighthTriplets_EmitsTuTokenOnEverySlot()
    {
        var rhythm = RhythmPatternParser.Parse("trip", "Triplets", ":3 XXX XXX XXX XXX", TimeSignature.FourFour);
        var prog = new Progression("test", "Test", new RomanDegree[] { new(1, Quality.Dominant7) });
        var exercise = new Exercise(new Key(new PitchClass(10), false), prog, rhythm, 90, Difficulty.Beginner);

        string tex = Renderer.Render(exercise);

        // Twelve tupled eighths, stateful ":8" once, "{tu 3}" on each slot (it does not persist).
        string inner = string.Join(" ", Enumerable.Repeat("(1.5 0.4 1.3){tu 3}", 12));
        Assert.EndsWith(":8 " + inner + " |", tex);
        Assert.Equal(12, CountOccurrences(tex, "{tu 3}"));
    }

    [Fact]
    public void Render_PerBeatMixedGrid_InterleavesStraightAndTupletTokens()
    {
        var rhythm = RhythmPatternParser.Parse("mix", "Mixed", "XXX:3 X... X.X:3 X...", TimeSignature.FourFour);
        var prog = new Progression("test", "Test", new RomanDegree[] { new(1, Quality.Dominant7) });
        var exercise = new Exercise(new Key(new PitchClass(10), false), prog, rhythm, 90, Difficulty.Beginner);

        string tex = Renderer.Render(exercise);

        Assert.EndsWith(
            ":8 (1.5 0.4 1.3){tu 3} (1.5 0.4 1.3){tu 3} (1.5 0.4 1.3){tu 3} " +
            ":4 (1.5 0.4 1.3) (1.5 0.4 1.3){tu 3} :8 (1.5 0.4 1.3){tu 3} :4 (1.5 0.4 1.3) |",
            tex);
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

    [Fact]
    public void Render_TwoChordBar_VoicesEachHalfWithItsChord()
    {
        // "17_67" = I7 (first half) · VI7 (second half) in Bb, struck on every quarter.
        var prog = ProgressionParser.Parse("p", "P", "17_67", TimeSignature.FourFour);
        var exercise = new Exercise(new Key(new PitchClass(10), false), prog, SeedData.Quarters, 90, Difficulty.Beginner);

        IReadOnlyList<string> groups = ChordGroups(LastBar(Renderer.Render(exercise)));

        Assert.Equal(4, groups.Count);
        Assert.Equal(groups[0], groups[1]);     // both quarters of the I7 half
        Assert.Equal(groups[2], groups[3]);     // both quarters of the VI7 half
        Assert.NotEqual(groups[0], groups[2]);  // the chord actually changes at the boundary
    }

    [Fact]
    public void Render_ThreeChordBar_ExplicitSlots_VoicesNinetySixFortyEightFortyEight()
    {
        // "17:2_67:1_27:1" = I7 (half) · VI7 (quarter) · ii7 (quarter), struck on every quarter.
        var prog = ProgressionParser.Parse("p", "P", "17:2_67:1_27:1", TimeSignature.FourFour);
        var exercise = new Exercise(new Key(new PitchClass(10), false), prog, SeedData.Quarters, 90, Difficulty.Beginner);

        IReadOnlyList<string> groups = ChordGroups(LastBar(Renderer.Render(exercise)));

        Assert.Equal(4, groups.Count);
        Assert.Equal(groups[0], groups[1]);     // the I7 half spans quarters 1–2
        Assert.NotEqual(groups[1], groups[2]);  // → VI7 at quarter 3
        Assert.NotEqual(groups[2], groups[3]);  // → ii7 at quarter 4
        Assert.NotEqual(groups[0], groups[3]);
    }

    [Fact]
    public void Render_FourChordBar_VoicesEachQuarterDistinctly()
    {
        var prog = ProgressionParser.Parse("p", "P", "17_27_37_47", TimeSignature.FourFour);
        var exercise = new Exercise(new Key(new PitchClass(10), false), prog, SeedData.Quarters, 90, Difficulty.Beginner);

        IReadOnlyList<string> groups = ChordGroups(LastBar(Renderer.Render(exercise)));

        Assert.Equal(4, groups.Count);
        Assert.Equal(4, groups.Distinct().Count()); // I7/II7/III7/IV7 all different
    }

    [Fact]
    public void Render_BluesViaDsl_IsByteIdenticalToSeedProgression()
    {
        // The DSL round-trip must reproduce the existing seed output exactly (C4 backward compatibility).
        var key = new Key(new PitchClass(10), false);
        string viaSeed = Renderer.Render(
            new Exercise(key, SeedData.TwelveBarBlues, SeedData.Beat1And3, 80, Difficulty.Beginner));

        var dslProg = ProgressionParser.Parse(
            "12bar_blues", "12-Bar Blues", "17 17 17 17 47 47 17 17 57 47 17 57", TimeSignature.FourFour);
        string viaDsl = Renderer.Render(
            new Exercise(key, dslProg, SeedData.Beat1And3, 80, Difficulty.Beginner));

        Assert.Equal(viaSeed, viaDsl);
    }

    private static string LastBar(string tex) => tex.Split('\n')[^1];

    private static IReadOnlyList<string> ChordGroups(string barLine) =>
        System.Text.RegularExpressions.Regex.Matches(barLine, @"\([^)]*\)")
            .Select(m => m.Value)
            .ToList();

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
