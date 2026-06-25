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

    [Theory]
    // key tonic (major), degree, accidental → sounding root pc, letter-pure spelling
    [InlineData(5, 4, Accidental.Sharp, 11, 'B', 0)]    // F: #4 = B natural (the bar-6 #IVdim7)
    [InlineData(5, 2, Accidental.Flat, 6, 'G', -1)]     // F: b2 = Gb (the tritone sub bII7)
    [InlineData(0, 7, Accidental.Sharp, 0, 'B', 1)]     // C: #7 = B# (letter-pure, no collapse to C)
    [InlineData(0, 4, Accidental.Flat, 4, 'F', -1)]     // C: b4 = Fb (letter-pure, no collapse to E)
    public void Realize_AltereddDegree_SpellsRootLetterPureFromWrittenDegree(
        int keyTonic, int degree, Accidental accidental, int expectedPc, char letter, int letterAccidental)
    {
        var prog = new Progression("t", "t", new RomanDegree[] { new(degree, Quality.Dominant7, accidental) });

        Chord chord = Transposer.Realize(prog, new Key(new PitchClass(keyTonic), false))[0];

        Assert.Equal(expectedPc, chord.Root.Value);
        Assert.Equal(new NoteName(letter, letterAccidental), chord.RootSpelling);
    }

    [Fact]
    public void Realize_DiatonicDegree_KeyPath_LeavesRootSpellingNull()
    {
        // A plain (un-altered) degree carries no RootSpelling, so ChordSymbol falls back to the key
        // table and existing rendered output stays byte-identical (constraint C2).
        var prog = new Progression("t", "t", new RomanDegree[] { new(1, Quality.Major) });

        Chord chord = Transposer.Realize(prog, new Key(new PitchClass(0), false))[0];

        Assert.Equal(0, chord.Root.Value); // C
        Assert.Null(chord.RootSpelling);
    }

    [Fact]
    public void Realize_ScaleOnly_ShiftsPitchByAccidentalButLeavesSpellingNull()
    {
        // No key → no letter-pure spelling, but the chromatic alteration still moves the sounding pitch.
        var prog = new Progression("t", "t", new RomanDegree[] { new(2, Quality.Dominant7, Accidental.Flat) });

        Chord chord = Transposer.Realize(prog, Scale.Major(new PitchClass(5)))[0]; // F major scale

        Assert.Equal(6, chord.Root.Value); // bII = Gb pitch
        Assert.Null(chord.RootSpelling);
    }

    [Fact]
    public void Realize_DegreeOutOfRange_Throws()
    {
        var prog = new Progression("t", "t", new RomanDegree[] { new(8, Quality.Major) });

        Assert.Throws<ArgumentOutOfRangeException>(
            () => Transposer.Realize(prog, new Key(new PitchClass(0), false)));
    }
}
