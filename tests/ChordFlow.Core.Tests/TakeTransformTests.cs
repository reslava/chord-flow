using ChordFlow.Music.Progressions;
using ChordFlow.Music.Progressions.Transforms;
using ChordFlow.Music.Rhythm;
using System;
using System.Linq;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// <see cref="TakeTransform"/>: keeps the first N whole bars, preserves multi-span bars and id/name, and
/// fails loud on an out-of-range count (constraints C3, C4).
/// </summary>
public class TakeTransformTests
{
    private static readonly TimeSignature Ts = TimeSignature.FourFour;

    private static Progression Parse(string dsl) => ProgressionParser.Parse("p", "P", dsl, Ts);

    [Fact]
    public void Apply_KeepsFirstNBars()
    {
        Progression result = new TakeTransform(4).Apply(Parse("1 4 5 1 6 2 5 1"));

        Assert.Equal(4, result.Bars.Count);
        Assert.Equal(new[] { 1, 4, 5, 1 }, result.Bars.Select(b => b.Spans[0].Degree.Degree));
    }

    [Fact]
    public void Apply_PreservesMultiSpanBars()
    {
        // The first bar has two spans (I7 half + VI7 half); take(1) keeps the whole bar intact.
        Progression result = new TakeTransform(1).Apply(Parse("17:2_67:2 1 4 5"));

        HarmonicBar bar = Assert.Single(result.Bars);
        Assert.Equal(2, bar.Spans.Count);
        Assert.Equal(192, bar.TotalTicks);
    }

    [Fact]
    public void Apply_PreservesIdAndName()
    {
        Progression source = Parse("1 4 5 1");
        Progression result = new TakeTransform(2).Apply(source);

        Assert.Equal(source.Id, result.Id);
        Assert.Equal(source.Name, result.Name);
    }

    [Fact]
    public void Apply_TakeAllBars_IsAllowed()
    {
        Assert.Equal(4, new TakeTransform(4).Apply(Parse("1 4 5 1")).Bars.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(5)]   // > 4 bars
    public void Apply_OutOfRange_Throws(int count)
    {
        Assert.Throws<ArgumentException>(() => new TakeTransform(count).Apply(Parse("1 4 5 1")));
    }
}
