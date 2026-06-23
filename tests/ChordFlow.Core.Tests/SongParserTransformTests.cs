using ChordFlow.Exercises;
using ChordFlow.Music.Progressions;
using ChordFlow.Music.Progressions.Transforms;
using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Songs;
using ChordFlow.Rendering;
using System;
using System.Linq;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The Song-DSL <c>@op</c> hook (<see cref="SongParser"/>) and its realization through
/// <see cref="SongExpander"/>: parsing <c>@take(N)</c>, left-to-right composition, either-order
/// <c>x&lt;n&gt;</c>+<c>@op</c>, the transform-free regression (C5), grammar errors, and non-commutativity.
/// </summary>
public class SongParserTransformTests
{
    private static readonly TimeSignature Ts = TimeSignature.FourFour;

    private static Song Parse(string dsl) => SongParser.Parse("s", "Song", dsl, Ts);

    private static PartPlay FirstPlay(Song song) => song.Items.OfType<PartPlay>().First();

    private sealed class EmptyStore : IProgressionStore
    {
        public Progression? Find(string id) => null;
    }

    [Fact]
    public void Parse_TakeToken_AttachesTransform()
    {
        PartPlay play = FirstPlay(Parse("A = 1 4 5 1\nA @take(2)"));

        Assert.Equal(2, Assert.IsType<TakeTransform>(Assert.Single(play.Transforms)).Count);
        Assert.Equal(1, play.Repeat);
    }

    [Fact]
    public void Parse_TransformsComposeLeftToRight()
    {
        PartPlay play = FirstPlay(Parse("A = 1 4 5 1 6 2 5 1\nA @take(6) @take(4)"));

        Assert.Collection(
            play.Transforms,
            t => Assert.Equal(6, Assert.IsType<TakeTransform>(t).Count),
            t => Assert.Equal(4, Assert.IsType<TakeTransform>(t).Count));
    }

    [Theory]
    [InlineData("A @take(4) x2")]
    [InlineData("A x2 @take(4)")]
    public void Parse_RepeatAndTransform_EitherOrder(string playLine)
    {
        PartPlay play = FirstPlay(Parse($"A = 1 4 5 1\n{playLine}"));

        Assert.Equal(2, play.Repeat);
        Assert.Equal(4, Assert.IsType<TakeTransform>(Assert.Single(play.Transforms)).Count);
    }

    [Fact]
    public void Parse_PlainPlay_HasNoTransforms()
    {
        // Regression (C5): a transform-free play carries an empty transform list — no behaviour change.
        Assert.Empty(FirstPlay(Parse("A = 1 4 5 1\nA")).Transforms);
    }

    [Fact]
    public void Parse_UnknownTransform_ThrowsFormat()
    {
        FormatException ex = Assert.Throws<FormatException>(() => Parse("A = 1 4 5 1\nA @bogus(1)"));
        Assert.Contains("bogus", ex.Message);
    }

    [Theory]
    [InlineData("A @take(x)")]   // non-integer arg
    [InlineData("A @take()")]    // empty arg
    [InlineData("A @take(0)")]   // non-positive
    [InlineData("A @take8")]     // no parens
    public void Parse_MalformedTransform_ThrowsFormat(string playLine)
    {
        Assert.Throws<FormatException>(() => Parse($"A = 1 4 5 1\n{playLine}"));
    }

    [Fact]
    public void Parse_DoubleRepeat_ThrowsFormat()
    {
        Assert.Throws<FormatException>(() => Parse("A = 1 4 5 1\nA x2 x3"));
    }

    [Fact]
    public void Expand_Take_TrimsRealizedSectionToNBars()
    {
        RealizedSong realized =
            SongExpander.Expand(Parse("A = 1 4 5 1 6 2 5 1\nA @take(4)"), new EmptyStore());

        Assert.Equal(4, Assert.Single(realized.Sections).Bars.Count);
    }

    [Fact]
    public void Expand_NonCommutative_OrderMatters()
    {
        // take(4) leaves 4 bars, then take(6) is out of range → throws. The reverse order is the legal one,
        // so the two orderings are not interchangeable (the idea's non-commutativity contract).
        Assert.Throws<ArgumentException>(
            () => SongExpander.Expand(Parse("A = 1 4 5 1 6 2 5 1\nA @take(4) @take(6)"), new EmptyStore()));
    }

    [Fact]
    public void Dogfood_RealStandard_TakeDrillsTheHead()
    {
        // A real ii-V-I standard in Bb authored in the Song DSL (ii-7 V7 Imaj7 + a vi-ii-V-I turnaround);
        // @take(2) drills just the ii-V of the head. Exercises maj7 voicings end-to-end.
        const string dsl = """
            key Bb
            head = 2-7 57 1maj7 1maj7
            turn = 6-7 2-7 57 1maj7
            head x2
            turn
            head @take(2)
            """;

        RealizedSong realized = SongExpander.Expand(Parse(dsl), new EmptyStore());

        Assert.Equal(4, realized.Sections.Count);                      // head, head, turn, head@take(2)
        Assert.Equal(new[] { 4, 4, 4, 2 }, realized.Sections.Select(s => s.Bars.Count));

        // …and it renders end-to-end to alphaTex (maj7 chords included).
        string tex = new AlphaTexRenderer().Render(realized, SeedData.Beat1And3, 120, Difficulty.Beginner).Tex;
        Assert.False(string.IsNullOrWhiteSpace(tex));
    }
}
