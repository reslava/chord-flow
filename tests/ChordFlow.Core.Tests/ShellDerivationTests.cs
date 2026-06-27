using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// Unit checks for the 2-form compact shell deriver (req IN13). The exhaustive 12-grip golden oracle lives in
/// <see cref="ShellOracleTests"/>; these cover form selection, eligibility, and the guide-tone string layout.
/// </summary>
public class ShellDerivationTests
{
    private static readonly PitchClass C = new(0);

    private static string Shell(Quality quality, CagedShape form) =>
        ShellDerivation.Derive(quality, form, C, 0, 15).FretString();

    [Fact]
    public void CForm_PlacesRootOnA_ThirdOnD_SeventhOnG()
    {
        Assert.Equal("x 3 2 3 x x", Shell(Quality.Dominant7, CagedShape.C));
    }

    [Fact]
    public void EForm_PlacesRootOnLowE_SeventhOnD_ThirdOnG()
    {
        Assert.Equal("8 x 9 9 x x", Shell(Quality.Major7, CagedShape.E));
    }

    [Fact]
    public void Triad_NotShellEligible_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => ShellDerivation.Derive(Quality.Major, CagedShape.C, C, 0, 15));
    }

    [Fact]
    public void NonShellForm_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ShellDerivation.Derive(Quality.Dominant7, CagedShape.A, C, 0, 15));
    }

    [Fact]
    public void OpenStringRoot_AnchorsTheCompactGripUpAnOctave()
    {
        // A on the open A string (fret 0) would push the guide tones ~12 frets away; the deriver must anchor the
        // compact grip at the 12th-fret octave instead (regression for the x 0 11 1 bug).
        Assert.Equal("x 12 11 13 x x", ShellDerivation.Derive(Quality.Major7, CagedShape.C, new PitchClass(9), 0, 15).FretString());
        Assert.Equal("x 12 11 12 x x", ShellDerivation.Derive(Quality.Dominant7, CagedShape.C, new PitchClass(9), 0, 15).FretString());
    }

    [Fact]
    public void HalfDiminished_ShellEqualsMinor7Shell()
    {
        // m7b5's b5 is the dropped fifth, so its shell collapses to the min7 guide tones (root, b3, b7).
        Assert.Equal(Shell(Quality.Minor7, CagedShape.C), Shell(Quality.HalfDiminished7, CagedShape.C));
        Assert.Equal(Shell(Quality.Minor7, CagedShape.E), Shell(Quality.HalfDiminished7, CagedShape.E));
    }
}
