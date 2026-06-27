using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The <see cref="VoicingFamily.DoubledShell"/> reduction (req IN1/IN4): mute the fifth, keep everything else
/// (incl. doublings), never repack. Validated against the engine's own derived grips.
/// </summary>
public class ShellReductionTests
{
    private static readonly PitchClass C = new(0);

    private static string DoubledShell(Quality quality, CagedShape shape, int minFret) =>
        ShellReduction.MuteFifth(CagedDerivation.Derive(quality, shape, C, minFret, minFret + 16)).FretString();

    [Fact]
    public void Dom7_CShape_DropsFifth_KeepsDoubledRoot()
    {
        // Full C-shape dom7 is "x 3 2 3 1 3": fifth on the high E (s1); the doubled root on s2 (fret 1) stays.
        Assert.Equal("x 3 2 3 1 x", DoubledShell(Quality.Dominant7, CagedShape.C, 1));
    }

    [Fact]
    public void Maj7_CShape_DropsFifth()
    {
        // Full C-shape maj7 is "x 3 2 0 0 0": fifth is the open G (s3).
        Assert.Equal("x 3 2 x 0 0", DoubledShell(Quality.Major7, CagedShape.C, 0));
    }

    [Theory]
    [InlineData(CagedShape.C, 1)]
    [InlineData(CagedShape.A, 3)]
    [InlineData(CagedShape.G, 5)]
    [InlineData(CagedShape.E, 8)]
    [InlineData(CagedShape.D, 10)]
    public void Dom7_Reduction_SoundsNoFifth_AcrossShapes(CagedShape shape, int minFret)
    {
        ChordShape reduced = ShellReduction.MuteFifth(
            CagedDerivation.Derive(Quality.Dominant7, shape, C, minFret, minFret + 16));

        // Dominant7's fifth is the perfect 5th (7 semitones); no sounded string may carry it.
        Assert.DoesNotContain(reduced.Strings.Where(s => !s.IsMuted), s => Mod12(s.Semitones) == 7);
        // …and the guide tones survive (root 0, major 3rd 4, minor 7th 10 all present).
        var sounded = reduced.Strings.Where(s => !s.IsMuted).Select(s => Mod12(s.Semitones)).ToHashSet();
        Assert.Contains(0, sounded);
        Assert.Contains(4, sounded);
        Assert.Contains(10, sounded);
    }

    private static int Mod12(int v) => ((v % 12) + 12) % 12;
}
