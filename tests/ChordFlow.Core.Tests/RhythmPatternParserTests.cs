using ChordFlow.Music.Rhythm;
using Xunit;

namespace ChordFlow.Core.Tests;

public class RhythmPatternParserTests
{
    private static IReadOnlyList<RhythmEvent> Parse(string dsl) =>
        RhythmPatternParser.Parse("p", "P", dsl, TimeSignature.FourFour).Bars[0].Events;

    private static (int Pos, int Len) PL(RhythmEvent e) => (e.Position, e.Length);

    [Fact]
    public void Parse_ProducesSingleBarPattern()
    {
        var pattern = RhythmPatternParser.Parse("quarters", "Quarters", "X...X...X...X...", TimeSignature.FourFour);

        Assert.Equal("quarters", pattern.Id);
        Assert.Single(pattern.Bars);
        Assert.Equal(TimeSignature.FourFour, pattern.TimeSignature);
        Assert.Null(pattern.Pickup);
    }

    [Fact]
    public void Parse_Quarters_IsFourRingingQuarters()
    {
        // Each strum rings to the next onset (sustain rule) — four quarter notes, no rests.
        Assert.Equal(
            new[] { (0, 48), (48, 48), (96, 48), (144, 48) },
            Parse("X...X...X...X...").Select(PL));
    }

    [Fact]
    public void Parse_Beat1_SustainsTheWholeBar()
    {
        // A single attack held by '.' to the bar end = one whole-bar ring (sustain-literal seed).
        Assert.Equal(new[] { (0, 192) }, Parse("X...............").Select(PL));
    }

    [Fact]
    public void Parse_Beat1And3_IsTwoHalfNotes()
    {
        Assert.Equal(new[] { (0, 96), (96, 96) }, Parse("X.......X.......").Select(PL));
    }

    [Fact]
    public void Parse_DashEndsTheRingAndStartsSilence()
    {
        // '-' cuts the note; the following silence emits no event (the quantizer fills the gap).
        Assert.Equal(new[] { (0, 48), (96, 48) }, Parse("X...-...X...-...").Select(PL));
    }

    [Fact]
    public void Parse_DottedEighthFallsOutOfTheSustainRule()
    {
        // X..X = attack, two sustains, attack → the first note is 3 cells = 36 ticks = a dotted eighth.
        Assert.Equal((0, 36), PL(Parse("X..X............")[0]));
    }

    [Fact]
    public void Parse_LeadingRowSubdivision_AppliesTripletGridToWholeRow()
    {
        var events = Parse(":3 XXX XXX XXX XXX");

        Assert.Equal(12, events.Count);
        Assert.All(events, e => Assert.Equal(16, e.Length)); // eighth-triplet = 16 ticks
        Assert.Equal(Enumerable.Range(0, 12).Select(i => i * 16), events.Select(e => e.Position));
    }

    [Fact]
    public void Parse_ContiguousRun_SplitsIntoBeatsByCount_ModelB()
    {
        // A same-subdivision run may omit inner spaces; it splits into beats by cell count.
        Assert.Equal(Parse("X... X... X... X..."), Parse("X...X...X...X..."));
        Assert.Equal(Parse(":3 XXX XXX XXX XXX"), Parse(":3 XXXXXXXXXXXX"));
    }

    [Fact]
    public void Parse_PerBeatMixedSubdivision_BlendsTripletAndStraightBeats()
    {
        // beat 1 triplet · beat 2 straight 16ths · beat 3 triplet · beat 4 straight.
        var onsets = Parse("XXX:3 X... X.X:3 X...").Select(e => e.Position).ToArray();

        Assert.Equal(new[] { 0, 16, 32, 48, 96, 128, 144 }, onsets);
    }

    [Theory]
    [InlineData("")]                       // empty
    [InlineData("X*..X...X...X...")]       // '*' is not a glyph (deferred sugar, EX8)
    [InlineData("X.. X... X... X...")]     // 3 cells: not a whole multiple of 4
    [InlineData("X... X...")]              // only 2 beats, expected 4
    [InlineData("X...X...X...X...X...")]   // 5 beats, expected 4
    [InlineData(":5 XXXXX XXXXX XXXXX XXXXX")] // subdivision 5 does not divide 48
    [InlineData("X...:3:4 X... X... X...")]    // two ':n' suffixes on one run
    [InlineData("X...:z X... X... X...")]      // non-numeric subdivision
    public void Parse_MalformedInput_ThrowsFormatException(string dsl)
    {
        Assert.Throws<FormatException>(() => Parse(dsl));
    }

    [Fact]
    public void Parse_AllEventsArePlainDownStrokes_NoStrokeOrAccentAuthored()
    {
        // C2 — the DSL authors timing only; stroke/accent stay overlays applied downstream.
        Assert.All(Parse("X...X...X...X..."), e =>
        {
            Assert.Equal(Stroke.Down, e.Stroke);
            Assert.Equal(Accent.Normal, e.Accent);
        });
    }

    // ---- Multi-bar ( | ) ----------------------------------------------------

    [Fact]
    public void Parse_TwoBars_EachParsedIndependently()
    {
        var pattern = RhythmPatternParser.Parse(
            "p", "P", "X...X...X...X... | X.......X.......", TimeSignature.FourFour);

        Assert.Equal(2, pattern.Bars.Count);
        Assert.Equal(
            new[] { (0, 48), (48, 48), (96, 48), (144, 48) },
            pattern.Bars[0].Events.Select(PL));
        Assert.Equal(new[] { (0, 96), (96, 96) }, pattern.Bars[1].Events.Select(PL));
    }

    [Fact]
    public void Parse_FourBars_DifferingContent_AllSpanAFullBar()
    {
        var pattern = RhythmPatternParser.Parse(
            "p", "P",
            "X............... | X.......X....... | X...X...X...X... | X...-...X...-...",
            TimeSignature.FourFour);

        Assert.Equal(4, pattern.Bars.Count);
        Assert.Equal(new[] { (0, 192) }, pattern.Bars[0].Events.Select(PL));
        Assert.Equal(new[] { (0, 96), (96, 96) }, pattern.Bars[1].Events.Select(PL));
        Assert.Equal(4, pattern.Bars[2].Events.Count);
        Assert.Equal(new[] { (0, 48), (96, 48) }, pattern.Bars[3].Events.Select(PL));
    }

    [Fact]
    public void Parse_NewlinesBetweenBars_AreInsignificant()
    {
        var pattern = RhythmPatternParser.Parse(
            "p", "P", "X...X...X...X...\n| X.......X.......\n", TimeSignature.FourFour);

        Assert.Equal(2, pattern.Bars.Count);
    }

    // ---- Pickup ( PICKUP: ) -------------------------------------------------

    [Fact]
    public void Parse_Pickup_ShorterThanABar_HasItsOwnLength()
    {
        // 11 sustains + a final attack = 12 cells = 144 ticks; the note opens on the last cell.
        var pattern = RhythmPatternParser.Parse(
            "p", "P", "PICKUP: ...........X | X...X...X...X...", TimeSignature.FourFour);

        Assert.NotNull(pattern.Pickup);
        Assert.Equal(144, pattern.Pickup!.LengthTicks);
        Assert.Equal(new[] { (132, 12) }, pattern.Pickup.Events.Select(PL));
        Assert.Single(pattern.Bars);
    }

    [Fact]
    public void Parse_Pickup_OneBeatTriplet_UsesPerRowSubdivision()
    {
        var pattern = RhythmPatternParser.Parse(
            "p", "P", "PICKUP: :3 XXX | X...X...X...X...", TimeSignature.FourFour);

        Assert.NotNull(pattern.Pickup);
        Assert.Equal(48, pattern.Pickup!.LengthTicks);
        Assert.Equal(new[] { (0, 16), (16, 16), (32, 16) }, pattern.Pickup.Events.Select(PL));
    }

    [Fact]
    public void Parse_Pickup_DoesNotNeedWholeBeats()
    {
        // 7 straight 16ths = 84 ticks (1¾ beats) — legal for a pickup, illegal for a bar.
        var pattern = RhythmPatternParser.Parse(
            "p", "P", "PICKUP: ...X..X | X...X...X...X...", TimeSignature.FourFour);

        Assert.Equal(84, pattern.Pickup!.LengthTicks); // 7 cells × 12
        // attack on cell 3 rings to the attack on cell 6 (dotted eighth), which rings to the end.
        Assert.Equal(new[] { (36, 36), (72, 12) }, pattern.Pickup.Events.Select(PL));
    }

    [Theory]
    [InlineData("PICKUP: X...")]                      // pickup but no bars
    [InlineData("PICKUP: X...X...X...X...X | X...X...X...X...")] // pickup longer than a bar
    [InlineData("PICKUP:  | X...X...X...X...")]        // empty pickup grid
    public void Parse_MalformedPickup_ThrowsFormatException(string dsl)
    {
        Assert.Throws<FormatException>(
            () => RhythmPatternParser.Parse("p", "P", dsl, TimeSignature.FourFour));
    }
}
