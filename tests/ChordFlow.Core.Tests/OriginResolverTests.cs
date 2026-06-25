using ChordFlow.Persistence;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The single-item tier resolver (content-source-model): precedence UserDefined &gt; Pack, non-destructive
/// (selection only — a lower tier stays available as fallback). Used by the Get/Find single-item paths and
/// the voicing book's load; the multi-source <i>list</i> path no longer collapses.
/// </summary>
public class OriginResolverTests
{
    private sealed record Def(string Id, Origin Origin) : IOriginated;

    [Fact]
    public void Rank_OrdersUserDefinedAbovePack()
    {
        Assert.True(OriginResolver.Rank(Origin.UserDefined) > OriginResolver.Rank(Origin.Pack));
    }

    [Fact]
    public void Resolve_PicksHighestTierPerId()
    {
        var candidates = new[]
        {
            new Def("12bar_blues", Origin.Pack),
            new Def("12bar_blues", Origin.UserDefined),
            new Def("jazz_blues", Origin.Pack),
        };

        IReadOnlyList<Def> effective = OriginResolver.Resolve(candidates);

        Assert.Equal(2, effective.Count);
        Assert.Equal(Origin.UserDefined, effective.Single(d => d.Id == "12bar_blues").Origin);
        Assert.Equal(Origin.Pack, effective.Single(d => d.Id == "jazz_blues").Origin);
    }

    [Fact]
    public void Resolve_WinnersAppearInFirstSeenIdOrder()
    {
        var candidates = new[]
        {
            new Def("b", Origin.Pack),
            new Def("a", Origin.Pack),
            new Def("a", Origin.UserDefined),
        };

        IReadOnlyList<Def> effective = OriginResolver.Resolve(candidates);

        Assert.Equal(new[] { "b", "a" }, effective.Select(d => d.Id));
    }

    [Fact]
    public void Resolve_IsNonDestructive_RemovingHigherTierFallsBackToPack()
    {
        var all = new List<Def>
        {
            new("x", Origin.Pack),
            new("x", Origin.UserDefined),
        };

        // Local edit present → local wins.
        Assert.Equal(Origin.UserDefined, OriginResolver.ResolveOne(all, "x")!.Origin);

        // Remove the local copy → the pack copy is still on hand as fallback.
        all.RemoveAll(d => d.Origin == Origin.UserDefined);
        Assert.Equal(Origin.Pack, OriginResolver.ResolveOne(all, "x")!.Origin);
    }

    [Fact]
    public void ResolveOne_UnknownId_ReturnsNull()
    {
        var candidates = new[] { new Def("a", Origin.Pack) };
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
