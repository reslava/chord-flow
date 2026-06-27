using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;

namespace ChordFlow.Core.Tests;

/// <summary>
/// Test-only reproduction of the retired <c>BeginnerShellStrategy</c> movable shell (root on A, 3rd on D, 7th
/// on G) for dom7/min7/maj7. The production strategy was deleted (shell-voicing-derivation IN9) — engine shells
/// supersede it — but the renderer's <i>formatting</i> tests still want a stable, byte-identical 3-note grip
/// decoupled from the comping engine. This fixture gives them exactly that.
/// </summary>
internal static class ShellGripFixture
{
    private const int AString = 5;
    private const int DString = 4;
    private const int GString = 3;
    private const int OpenAPitchClass = 9;

    /// <summary>The movable shell grip for <paramref name="chord"/> (dom7/min7/maj7 only).</summary>
    public static Voicing Voice(Chord chord)
    {
        (int thirdOffset, int seventhOffset) = chord.Quality switch
        {
            Quality.Dominant7 => (-1, 0),
            Quality.Minor7 => (-2, 0),
            Quality.Major7 => (-1, 1),
            _ => throw new NotSupportedException(
                $"The shell fixture covers Dominant7, Minor7, and Major7 only; got {chord.Quality}."),
        };

        int root = Mod12(chord.Root.Value);
        int r = Mod12(root - OpenAPitchClass);
        if (r < -thirdOffset)
        {
            r += 12;
        }

        var positions = new FretPosition[]
        {
            new(AString, r),
            new(DString, r + thirdOffset),
            new(GString, r + seventhOffset),
        };

        int firstFret = Math.Min(r + thirdOffset, r);
        return new Voicing(positions, BarreFret: null, FirstFret: firstFret, MutedStrings: new[] { 1, 2, 6 });
    }

    private static int Mod12(int value) => ((value % 12) + 12) % 12;
}
