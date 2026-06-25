using ChordFlow.Music.Harmony;
using ChordFlow.Music.Progressions;
using ChordFlow.Music.Rhythm;
using Xunit;

namespace ChordFlow.Core.Tests;

public class ProgressionParserTests
{
    private static readonly TimeSignature Ts = TimeSignature.FourFour;

    private static Progression Parse(string dsl) => ProgressionParser.Parse("t", "Test", dsl, Ts);

    [Fact]
    public void Parse_Blues_RoundTripsToTwelveSingleSpanDominant7Bars()
    {
        const string dsl = "17 17 17 17 47 47 17 17 57 47 17 57";

        Progression prog = Parse(dsl);

        Assert.Equal(12, prog.Bars.Count);
        Assert.All(prog.Bars, bar =>
        {
            ChordSpan span = Assert.Single(bar.Spans);
            Assert.Equal(192, span.DurationTicks);   // single full-bar span (C4)
            Assert.Equal(Quality.Dominant7, span.Degree.Quality);
        });

        // Same degree sequence as the seed (I I I I IV IV I I V IV I V).
        Assert.Equal(SeedData.TwelveBarBlues.Degrees, prog.Degrees);
    }

    [Fact]
    public void Parse_SingleMajorChord_IsOneFullBarSpan()
    {
        Progression prog = Parse("1");

        ChordSpan span = Assert.Single(Assert.Single(prog.Bars).Spans);
        Assert.Equal(new ChordSpan(new RomanDegree(1, Quality.Major), 192), span);
    }

    [Fact]
    public void Parse_JazzBluesTurnaround_HasExpectedBarsAndSpans()
    {
        Progression prog = Parse("2-7 57 17_67 2-7_57");

        Assert.Equal(4, prog.Bars.Count);

        Assert.Equal(
            new[] { new ChordSpan(new RomanDegree(2, Quality.Minor7), 192) },
            prog.Bars[0].Spans);
        Assert.Equal(
            new[] { new ChordSpan(new RomanDegree(5, Quality.Dominant7), 192) },
            prog.Bars[1].Spans);
        Assert.Equal(
            new[]
            {
                new ChordSpan(new RomanDegree(1, Quality.Dominant7), 96),
                new ChordSpan(new RomanDegree(6, Quality.Dominant7), 96),
            },
            prog.Bars[2].Spans);
        Assert.Equal(
            new[]
            {
                new ChordSpan(new RomanDegree(2, Quality.Minor7), 96),
                new ChordSpan(new RomanDegree(5, Quality.Dominant7), 96),
            },
            prog.Bars[3].Spans);
    }

    [Fact]
    public void Parse_ThreeChordBar_ViaExplicitSlots_Yields96_48_48()
    {
        Progression prog = Parse("17:2_67:1_27:1");

        Assert.Equal(
            new[]
            {
                new ChordSpan(new RomanDegree(1, Quality.Dominant7), 96),
                new ChordSpan(new RomanDegree(6, Quality.Dominant7), 48),
                new ChordSpan(new RomanDegree(2, Quality.Dominant7), 48),
            },
            Assert.Single(prog.Bars).Spans);
    }

    [Fact]
    public void Parse_FourChordBar_EvenSplit_YieldsFourQuarters()
    {
        Progression prog = Parse("17_27_37_47");

        IReadOnlyList<ChordSpan> spans = Assert.Single(prog.Bars).Spans;
        Assert.Equal(4, spans.Count);
        Assert.All(spans, s => Assert.Equal(48, s.DurationTicks));
    }

    [Theory]
    [InlineData("1", Quality.Major)]
    [InlineData("1-", Quality.Minor)]
    [InlineData("1m", Quality.Minor)]
    [InlineData("17", Quality.Dominant7)]
    [InlineData("1-7", Quality.Minor7)]
    [InlineData("1m7", Quality.Minor7)]
    [InlineData("1maj7", Quality.Major7)]
    [InlineData("1^7", Quality.Major7)]
    [InlineData("1°", Quality.Diminished)]
    [InlineData("1dim", Quality.Diminished)]
    [InlineData("1°7", Quality.Diminished7)]
    [InlineData("1dim7", Quality.Diminished7)]
    [InlineData("1ø", Quality.HalfDiminished7)]
    [InlineData("1m7b5", Quality.HalfDiminished7)]
    [InlineData("1+", Quality.Augmented)]
    [InlineData("1aug", Quality.Augmented)]
    public void Parse_EveryQualitySuffix_MapsToTheRightQuality(string token, Quality expected)
    {
        Progression prog = Parse(token);

        ChordSpan span = Assert.Single(Assert.Single(prog.Bars).Spans);
        Assert.Equal(expected, span.Degree.Quality);
    }

    [Theory]
    [InlineData("#4", 4, Quality.Major, Accidental.Sharp)]
    [InlineData("#4dim7", 4, Quality.Diminished7, Accidental.Sharp)]
    [InlineData("b27", 2, Quality.Dominant7, Accidental.Flat)]
    [InlineData("b2", 2, Quality.Major, Accidental.Flat)]
    [InlineData("17", 1, Quality.Dominant7, Accidental.Natural)]
    public void Parse_LeadingAccidental_SetsDegreeAccidental(
        string token, int degree, Quality quality, Accidental accidental)
    {
        Progression prog = Parse(token);

        ChordSpan span = Assert.Single(Assert.Single(prog.Bars).Spans);
        Assert.Equal(new RomanDegree(degree, quality, accidental), span.Degree);
    }

    [Theory]
    [InlineData("")]          // empty DSL
    [InlineData("1_4_5")]     // even split into 3 → not quarter-aligned
    [InlineData("1:2_4:1")]   // explicit slots sum to 3, not 4
    [InlineData("1:5")]       // slots value out of 1..4
    [InlineData("1:0")]       // slots value out of 1..4
    [InlineData("1xyz")]      // unknown quality suffix
    [InlineData("8")]         // degree out of 1..7
    [InlineData("0")]         // degree out of 1..7
    [InlineData("-7")]        // missing scale degree
    [InlineData("##4")]       // double accidental — only one '#'/'b' allowed
    [InlineData("#b4")]       // mixed double accidental
    [InlineData("#")]         // bare accidental, no degree
    [InlineData("b")]         // bare accidental, no degree
    [InlineData("#8")]        // accidental on out-of-range degree
    [InlineData("1_")]        // empty chord (trailing '_')
    [InlineData("1:2_4")]     // mixed explicit/even within one bar
    [InlineData("1:2:3")]     // more than one ':slots' suffix
    [InlineData("1:x")]       // non-numeric slots
    public void Parse_MalformedInput_ThrowsFormatException(string dsl)
    {
        Assert.Throws<FormatException>(() => ProgressionParser.Parse("t", "Test", dsl, Ts));
    }
}
