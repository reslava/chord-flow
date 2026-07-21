using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Rhythm.Generation;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// Tests the bar-pattern kind vocabulary (req IN3): generated density/placement families (enumeration counts +
/// placement) and the curated figure catalog (cell placement, multi-bar claves).
/// </summary>
public class RhythmKindTests
{
    private static readonly TimeSignature Ts = TimeSignature.FourFour;

    private static int[] Ticks(OnsetBar bar) => bar.OnsetTicks(Ts).ToArray();

    [Fact]
    public void Density_Quarter2Onset_HasSixPatternsEachWithTwoOnsets()
    {
        var kind = RhythmKind.Density(1, 2);
        Assert.Equal(6, kind.Patterns.Count); // C(4,2)
        Assert.All(kind.Patterns, p => Assert.Equal(2, p.OnsetCount));
        Assert.Equal("density", kind.Category);
    }

    [Fact]
    public void Density_Eighth2Onset_Has28Patterns()
    {
        Assert.Equal(28, RhythmKind.Density(2, 2).Patterns.Count); // C(8,2)
    }

    [Fact]
    public void Placement_EighthOffbeat_OnlyHitsTheAnds()
    {
        var kind = RhythmKind.Placement(2, "offbeat", 2);
        Assert.Equal(6, kind.Patterns.Count); // C(4,2) over the four & cells
        // Every onset sits on an & — tick ≡ 24 (mod 48) at eighth subdivision.
        Assert.All(kind.Patterns, p => Assert.All(Ticks(p), t => Assert.Equal(24, t % 48)));
    }

    [Fact]
    public void Placement_EighthOnbeat_OnlyHitsTheBeats()
    {
        var kind = RhythmKind.Placement(2, "onbeat", 2);
        Assert.All(kind.Patterns, p => Assert.All(Ticks(p), t => Assert.Equal(0, t % 48)));
    }

    [Fact]
    public void Figure_Tresillo_HasThreeThreeTwoOnsets()
    {
        var tresillo = GrooveFigures.ById("tresillo");
        Assert.NotNull(tresillo);
        Assert.Single(tresillo!.Patterns);
        Assert.Equal(new[] { 0, 72, 144 }, Ticks(tresillo.Patterns[0])); // cells 0, 3, 6 at eighth grid
    }

    [Fact]
    public void Figure_SonClave_HasTwoBars()
    {
        var clave = GrooveFigures.ById("son-clave-32");
        Assert.NotNull(clave);
        Assert.Equal(2, clave!.Patterns.Count);
        Assert.Equal(new[] { 0, 72, 144 }, Ticks(clave.Patterns[0])); // 3-side = tresillo
    }

    [Fact]
    public void Figures_AllResolveById()
    {
        Assert.All(GrooveFigures.All, f => Assert.Same(f, GrooveFigures.ById(f.Id)));
    }
}
