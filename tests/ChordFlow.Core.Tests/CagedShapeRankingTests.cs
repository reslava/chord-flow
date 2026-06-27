using ChordFlow.Instruments.Guitar;
using Xunit;

namespace ChordFlow.Core.Tests;

public class CagedShapeRankingTests
{
    [Fact]
    public void FamiliarityRank_OrdersTheBarreRootsFirst()
    {
        // E, A (the barre-root shapes) before G, C, D — the default familiarity order.
        Assert.True(CagedShape.E.FamiliarityRank() < CagedShape.A.FamiliarityRank());
        Assert.True(CagedShape.A.FamiliarityRank() < CagedShape.G.FamiliarityRank());
        Assert.True(CagedShape.G.FamiliarityRank() < CagedShape.C.FamiliarityRank());
        Assert.True(CagedShape.C.FamiliarityRank() < CagedShape.D.FamiliarityRank());
    }
}
