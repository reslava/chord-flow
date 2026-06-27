using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// Regression for the open-root anchor edge (caged-derive-anchor-edge): <see cref="CagedDerivation.Derive"/>
/// threw <see cref="ArgumentOutOfRangeException"/> when the root sat on an <b>open</b> string (fret 0), because
/// the anchor box is built from fretted notes only so the open root fell below it. The open D7
/// (<c>x x 0 2 1 2</c>) is the canonical victim; the oracle never hit it (its canonical-C grips are high on the neck).
/// </summary>
public class CagedDeriveAnchorEdgeTests
{
    [Fact]
    public void DShapeDominant7_OpenRoot_DerivesTheOpenGrip()
    {
        ChordShape grip = CagedDerivation.Derive(Quality.Dominant7, CagedShape.D, new PitchClass(2), 0, 15);

        Assert.Equal("x x 0 2 1 2", grip.FretString());
        Assert.Equal(Finger.Index, grip.AnchorFinger); // open root → index / open position
    }

    [Fact]
    public void EveryCatalogCombo_AcrossAllRoots_NeverThrowsOutOfRange_AtFullNeck()
    {
        foreach ((Quality quality, CagedShape shape) in
                 CagedVoicingCatalog.Combos.Where(c => c.Family == VoicingFamily.Caged).Select(c => (c.Quality, c.Shape)))
        {
            for (int root = 0; root < 12; root++)
            {
                try
                {
                    CagedDerivation.Derive(quality, shape, new PitchClass(root), 0, 15);
                }
                catch (InvalidOperationException)
                {
                    // No anchor / unspellable in this window is legitimate — only the out-of-range throw is the bug.
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    Assert.Fail($"{quality} {shape} at root {root}: {ex.Message}");
                }
            }
        }
    }
}
