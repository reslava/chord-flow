using ChordFlow.Persistence;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The shared shadowing policy (IN3): one Id-keyed resolver, precedence UserDefined &gt; Pack &gt; BuiltIn,
/// non-destructive (selection only — lower tiers stay available as fallback).
/// </summary>
public class OriginResolverTests
{
    private sealed record Def(string Id, Origin Origin) : IOriginated;

    [Theory]
    [InlineData(Origin.UserDefined, Origin.Pack)]
    [InlineData(Origin.Pack, Origin.BuiltIn)]
    [InlineData(Origin.UserDefined, Origin.BuiltIn)]
    public void Rank_OrdersUserDefinedAbovePackAboveBuiltIn(Origin higher, Origin lower)
    {
        Assert.True(OriginResolver.Rank(higher) > OriginResolver.Rank(lower));
    }

    [Fact]
    public void Resolve_PicksHighestTierPerId()
    {
        var candidates = new[]
        {
            new Def("12bar_blues", Origin.BuiltIn),
            new Def("12bar_blues", Origin.Pack),
            new Def("12bar_blues", Origin.UserDefined),
            new Def("jazz_blues", Origin.BuiltIn),
        };

        IReadOnlyList<Def> effective = OriginResolver.Resolve(candidates);

        Assert.Equal(2, effective.Count);
        Assert.Equal(Origin.UserDefined, effective.Single(d => d.Id == "12bar_blues").Origin);
        Assert.Equal(Origin.BuiltIn, effective.Single(d => d.Id == "jazz_blues").Origin);
    }

    [Fact]
    public void Resolve_WinnersAppearInFirstSeenIdOrder()
    {
        var candidates = new[]
        {
            new Def("b", Origin.BuiltIn),
            new Def("a", Origin.BuiltIn),
            new Def("a", Origin.UserDefined),
        };

        IReadOnlyList<Def> effective = OriginResolver.Resolve(candidates);

        Assert.Equal(new[] { "b", "a" }, effective.Select(d => d.Id));
    }

    [Fact]
    public void Resolve_IsNonDestructive_RemovingHigherTierFallsBackToNext()
    {
        var all = new List<Def>
        {
            new("x", Origin.BuiltIn),
            new("x", Origin.Pack),
            new("x", Origin.UserDefined),
        };

        // Local edit present → local wins.
        Assert.Equal(Origin.UserDefined, OriginResolver.ResolveOne(all, "x")!.Origin);

        // Remove the local copy → the pack copy is still on hand as fallback.
        all.RemoveAll(d => d.Origin == Origin.UserDefined);
        Assert.Equal(Origin.Pack, OriginResolver.ResolveOne(all, "x")!.Origin);

        // Remove the pack copy too → the built-in remains.
        all.RemoveAll(d => d.Origin == Origin.Pack);
        Assert.Equal(Origin.BuiltIn, OriginResolver.ResolveOne(all, "x")!.Origin);
    }

    [Fact]
    public void ResolveOne_UnknownId_ReturnsNull()
    {
        var candidates = new[] { new Def("a", Origin.BuiltIn) };
        Assert.Null(OriginResolver.ResolveOne(candidates, "missing"));
    }

    [Fact]
    public void Resolve_RankTie_KeepsFirstSeen()
    {
        var first = new Def("a", Origin.Pack);
        var second = new Def("a", Origin.Pack);

        Assert.Same(first, OriginResolver.Resolve(new[] { first, second }).Single());
        Assert.Same(first, OriginResolver.ResolveOne(new[] { first, second }, "a"));
    }
}
