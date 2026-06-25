using ChordFlow.Exercises;
using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;
using Xunit;

namespace ChordFlow.Core.Tests;

public class BeginnerShellStrategyTests
{
    // Open-string pitch classes for the A/D/G strings the shell uses (alphaTab strings 5/4/3).
    private static readonly Dictionary<int, int> OpenPitchClass = new() { [5] = 9, [4] = 2, [3] = 7 };

    private static readonly BeginnerShellStrategy Strategy = new();

    [Fact]
    public void Voice_Diminished7_VoicesRootFlatThirdAndDoubleFlatSeventh()
    {
        // B diminished7: root B(11), ♭3 D(2), ♭♭7 Ab(8). The shell omits the ♭5, like every other arm.
        var chord = new Chord(new PitchClass(11), Quality.Diminished7);

        Voicing voicing = Strategy.Voice(chord);

        int[] soundingPcs = voicing.Positions
            .Select(p => (OpenPitchClass[p.String] + p.Fret) % 12)
            .OrderBy(pc => pc)
            .ToArray();

        Assert.Equal(new[] { 2, 8, 11 }, soundingPcs); // D, Ab, B
    }

    [Theory]
    [InlineData(0)]  // C
    [InlineData(5)]  // F
    [InlineData(11)] // B
    public void Voice_Diminished7_AnyRoot_ProducesTheThreeShellTonesWithNoNegativeFret(int rootPc)
    {
        var chord = new Chord(new PitchClass(rootPc), Quality.Diminished7);

        Voicing voicing = Strategy.Voice(chord);

        Assert.All(voicing.Positions, p => Assert.True(p.Fret >= 0));
        int[] intervals = voicing.Positions
            .Select(p => (((OpenPitchClass[p.String] + p.Fret) - rootPc) % 12 + 12) % 12)
            .OrderBy(i => i)
            .ToArray();
        Assert.Equal(new[] { 0, 3, 9 }, intervals); // root, ♭3, ♭♭7
    }
}
