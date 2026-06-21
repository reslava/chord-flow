using ChordFlow.Music.Harmony;
using ChordFlow.Music.Progressions;
using Xunit;

namespace ChordFlow.Core.Tests;

public class TransposerTests
{
    [Fact]
    public void TwelveBarBlues_HasTwelveBars_AllDominant7()
    {
        Assert.Equal(12, SeedData.TwelveBarBlues.Degrees.Count);
        Assert.All(SeedData.TwelveBarBlues.Degrees, d => Assert.Equal(Quality.Dominant7, d.Quality));
    }

    [Theory]
    [InlineData(0)]  // C
    [InlineData(1)]  // C#/Db
    [InlineData(2)]  // D
    [InlineData(3)]  // D#/Eb
    [InlineData(4)]  // E
    [InlineData(5)]  // F
    [InlineData(6)]  // F#/Gb
    [InlineData(7)]  // G
    [InlineData(8)]  // G#/Ab
    [InlineData(9)]  // A
    [InlineData(10)] // A#/Bb
    [InlineData(11)] // B
    public void Realize_TwelveBarBlues_ProducesCorrectRootsInEveryKey(int tonic)
    {
        var key = new Key(new PitchClass(tonic), false);

        Chord[] chords = Transposer.Realize(SeedData.TwelveBarBlues, key);

        int i = tonic % 12;            // I  = tonic
        int iv = (tonic + 5) % 12;     // IV = perfect 4th above
        int v = (tonic + 7) % 12;      // V  = perfect 5th above
        int[] expectedRoots = { i, i, i, i, iv, iv, i, i, v, iv, i, v };

        Assert.Equal(12, chords.Length);
        for (int bar = 0; bar < 12; bar++)
        {
            Assert.Equal(expectedRoots[bar], chords[bar].Root.Value);
            Assert.Equal(Quality.Dominant7, chords[bar].Quality);
        }
    }

    [Fact]
    public void Realize_PassesDegreeQualityThrough()
    {
        var prog = new Progression("t", "t", new RomanDegree[] { new(2, Quality.Minor) });

        Chord chord = Transposer.Realize(prog, new Key(new PitchClass(0), false))[0];

        Assert.Equal(2, chord.Root.Value);          // ii of C major = D (pitch class 2)
        Assert.Equal(Quality.Minor, chord.Quality);
    }

    [Fact]
    public void Realize_MinorKey_UsesNaturalMinorScaleDegrees()
    {
        // 3rd degree of A natural minor is C (offset +3), not C# (+4 as in major).
        var prog = new Progression("t", "t", new RomanDegree[] { new(3, Quality.Minor) });

        Chord chord = Transposer.Realize(prog, new Key(new PitchClass(9), true))[0];

        Assert.Equal(0, chord.Root.Value); // C
    }

    [Fact]
    public void Realize_DegreeOutOfRange_Throws()
    {
        var prog = new Progression("t", "t", new RomanDegree[] { new(8, Quality.Major) });

        Assert.Throws<ArgumentOutOfRangeException>(
            () => Transposer.Realize(prog, new Key(new PitchClass(0), false)));
    }
}
