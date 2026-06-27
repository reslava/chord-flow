using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The shell golden oracle (req IN14): the 12 authored compact-shell grips (root C, frets low-E→high-E) that
/// <see cref="ShellDerivation"/> must reproduce — the spec for the deriver, exactly as the authored CAGED chords
/// are the spec for <see cref="CagedDerivation"/>. Two forms (C = 5th-string root, E = 6th-string root) ×
/// dom7/min7/maj7/dim7/6/m6. Authored by Rafa in shell-voicing-derivation chat-001.
/// </summary>
public class ShellOracleTests
{
    private static readonly PitchClass C = new(0);

    public static IEnumerable<object[]> AuthoredShells() => new[]
    {
        new object[] { Quality.Dominant7, CagedShape.C, "x 3 2 3 x x" },
        new object[] { Quality.Minor7, CagedShape.C, "x 3 1 3 x x" },
        new object[] { Quality.Major7, CagedShape.C, "x 3 2 4 x x" },
        new object[] { Quality.Diminished7, CagedShape.C, "x 3 1 2 x x" },
        new object[] { Quality.Major6, CagedShape.C, "x 3 2 2 x x" },
        new object[] { Quality.Minor6, CagedShape.C, "x 3 1 2 x x" },
        new object[] { Quality.Dominant7, CagedShape.E, "8 x 8 9 x x" },
        new object[] { Quality.Minor7, CagedShape.E, "8 x 8 8 x x" },
        new object[] { Quality.Major7, CagedShape.E, "8 x 9 9 x x" },
        new object[] { Quality.Diminished7, CagedShape.E, "8 x 7 8 x x" },
        new object[] { Quality.Major6, CagedShape.E, "8 x 7 9 x x" },
        new object[] { Quality.Minor6, CagedShape.E, "8 x 7 8 x x" },
    };

    [Theory]
    [MemberData(nameof(AuthoredShells))]
    public void ShellDerivation_ReproducesTheAuthoredGrip(Quality quality, CagedShape form, string expected)
    {
        Assert.Equal(expected, ShellDerivation.Derive(quality, form, C, 0, 15).FretString());
    }
}
