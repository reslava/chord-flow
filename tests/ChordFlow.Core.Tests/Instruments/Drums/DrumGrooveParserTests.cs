using ChordFlow.Instruments.Drums;
using ChordFlow.Music.Rhythm;
using Xunit;

namespace ChordFlow.Core.Tests;

public class DrumGrooveParserTests
{
    private static DrumGroove Parse(string dsl) =>
        DrumGrooveParser.Parse("g", "G", dsl, TimeSignature.FourFour);

    // Onset ticks of a voice's hits in a given bar (0-based).
    private static int[] Onsets(DrumGroove g, DrumVoice voice, int bar = 0) =>
        g.Bars[bar].Lanes.Single(l => l.Voice == voice).Events.Select(e => e.Position).ToArray();

    [Fact]
    public void Parse_BasicRockBeat_PlacesEachVoiceOnItsCells()
    {
        var g = Parse(
            "HH :2 xxxxxxxx\n" +
            "SD :2 ..x...x.\n" +
            "BD :2 x...x...");

        Assert.Single(g.Bars);
        Assert.Equal(new[] { 0, 24, 48, 72, 96, 120, 144, 168 }, Onsets(g, DrumVoice.HiHatClosed));
        Assert.Equal(new[] { 48, 144 }, Onsets(g, DrumVoice.Snare)); // backbeat: beats 2 & 4
        Assert.Equal(new[] { 0, 96 }, Onsets(g, DrumVoice.Kick));    // beats 1 & 3
    }

    [Fact]
    public void Parse_WhitespaceBetweenCellsIsInsignificant()
    {
        // Grid-aligned (spaces for readability) parses identically to the packed form.
        var spaced = Parse("SD :2 . . x . . . x .");
        var packed = Parse("SD :2 ..x...x.");

        Assert.Equal(Onsets(packed, DrumVoice.Snare), Onsets(spaced, DrumVoice.Snare));
    }

    [Fact]
    public void Parse_Shuffle_UsesTripletSubdivision()
    {
        // :3 = eighth-triplets; "x.x" per beat is the shuffle (hit on cells 1 and 3 of the triplet).
        var g = Parse("HH :3 x.x x.x x.x x.x");

        Assert.Equal(new[] { 0, 32, 48, 80, 96, 128, 144, 176 }, Onsets(g, DrumVoice.HiHatClosed));
    }

    [Fact]
    public void Parse_PerRunSubdivision_MixesStraightAndTripletInOneBar()
    {
        // Beats 1-2 straight 8ths, beats 3-4 triplets — runs delimited by the :n markers.
        var g = Parse("HH :2 xxxx :3 xxxxxx");

        Assert.Equal(
            new[] { 0, 24, 48, 72, 96, 112, 128, 144, 160, 176 },
            Onsets(g, DrumVoice.HiHatClosed));
    }

    [Fact]
    public void Parse_MultiBar_SplitsOnPipe()
    {
        var g = Parse(
            "HH :2 xxxxxxxx | xxxxxxxx\n" +
            "BD :2 x...x... | x.x.x...");

        Assert.Equal(2, g.Bars.Count);
        Assert.Equal(new[] { 0, 96 }, Onsets(g, DrumVoice.Kick, 0));
        Assert.Equal(new[] { 0, 48, 96 }, Onsets(g, DrumVoice.Kick, 1));
    }

    [Fact]
    public void Parse_DefaultSubdivisionIsSixteenths()
    {
        // No :n → :4 (16 cells per 4/4 bar).
        var g = Parse("BD x...............");
        Assert.Equal(new[] { 0 }, Onsets(g, DrumVoice.Kick));
        Assert.Single(g.Bars[0].Lanes.Single(l => l.Voice == DrumVoice.Kick).Events);
    }

    [Theory]
    [InlineData("Kick :1 x...")]
    [InlineData("BD :1 x...")]
    [InlineData("KD :1 x...")]
    public void Parse_AcceptsShortAndFullVoiceAliases(string dsl)
    {
        var g = Parse(dsl);
        Assert.Equal(DrumVoice.Kick, g.Bars[0].Lanes[0].Voice);
    }

    [Fact]
    public void Parse_StripsLineComments()
    {
        var g = Parse("BD :1 x.x.   # kick on beats 1 and 3");
        Assert.Equal(new[] { 0, 96 }, Onsets(g, DrumVoice.Kick));
    }

    [Fact]
    public void Parse_HitCompilesToOneCellRhythmEvent()
    {
        // C2: a hit is a one-cell RhythmEvent (Length = the cell width).
        var g = Parse("BD :2 x.......");
        RhythmEvent hit = g.Bars[0].Lanes.Single(l => l.Voice == DrumVoice.Kick).Events.Single();
        Assert.Equal(0, hit.Position);
        Assert.Equal(24, hit.Length); // one :2 cell = BeatTicks/2 = 24
    }

    [Fact]
    public void Parse_UnknownVoice_Throws()
    {
        var ex = Assert.Throws<FormatException>(() => Parse("ZZ :2 xxxxxxxx"));
        Assert.Contains("ZZ", ex.Message);
    }

    [Fact]
    public void Parse_UppercaseXReservedForAccent_Throws()
    {
        // X is reserved for a future accent glyph (drums/drums-accent-ghost).
        Assert.Throws<FormatException>(() => Parse("BD :2 X...X..."));
    }

    [Fact]
    public void Parse_WrongBarLength_Throws()
    {
        var ex = Assert.Throws<FormatException>(() => Parse("HH :2 xxxx")); // 4 cells = 2 beats, not 4
        Assert.Contains("beat", ex.Message);
    }

    [Fact]
    public void Parse_CellCountNotMultipleOfSubdivision_Throws()
    {
        Assert.Throws<FormatException>(() => Parse("HH :3 xxxxx")); // 5 not a multiple of 3
    }

    [Fact]
    public void Parse_BarCountMismatchAcrossRows_Throws()
    {
        var ex = Assert.Throws<FormatException>(() => Parse(
            "HH :2 xxxxxxxx | xxxxxxxx\n" +
            "BD :2 x...x..."));
        Assert.Contains("bar", ex.Message);
    }

    [Fact]
    public void Parse_DuplicateVoiceRow_Throws()
    {
        var ex = Assert.Throws<FormatException>(() => Parse(
            "HH :2 xxxxxxxx\n" +
            "HH :2 x.x.x.x."));
        Assert.Contains("more than one row", ex.Message);
    }

    [Fact]
    public void Parse_NoRows_Throws()
    {
        Assert.Throws<FormatException>(() => Parse("   \n  # just a comment\n"));
    }
}
