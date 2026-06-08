using ChordFlow.Domain;
using Xunit;

namespace ChordFlow.Tests;

public class VoicingBookTests
{
    // Open-string pitch classes, indexed by alphaTab string number (1 = high E .. 6 = low E).
    // Index 0 is unused so the string number indexes directly.
    private static readonly int[] OpenStringPc = { 0, 4, 11, 7, 2, 9, 4 };

    private static int NotePc(FretPosition p) => (OpenStringPc[p.String] + p.Fret) % 12;

    [Theory]
    [InlineData(10)] // Bb7
    [InlineData(3)]  // Eb7
    [InlineData(5)]  // F7
    public void Lookup_Dominant7Shell_SpellsRootMajorThirdMinorSeventh(int root)
    {
        var chord = new Chord(new PitchClass(root), Quality.Dominant7);

        Voicing voicing = VoicingBook.Lookup(chord, Difficulty.Beginner);

        var actual = voicing.Positions.Select(NotePc).ToHashSet();
        var expected = new HashSet<int> { root % 12, (root + 4) % 12, (root + 10) % 12 };
        Assert.Equal(3, voicing.Positions.Count);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Lookup_EveryChordOfTheBbBlues_Resolves()
    {
        var bb = new Key(new PitchClass(10), false);

        foreach (Chord chord in Transposer.Realize(SeedData.TwelveBarBlues, bb))
        {
            Voicing voicing = VoicingBook.Lookup(chord, Difficulty.Beginner);
            Assert.Equal(3, voicing.Positions.Count);
        }
    }

    [Fact]
    public void Lookup_UnauthoredChord_Throws()
    {
        var c7 = new Chord(new PitchClass(0), Quality.Dominant7); // not in the MVP table

        Assert.Throws<KeyNotFoundException>(() => VoicingBook.Lookup(c7, Difficulty.Beginner));
    }

    [Fact]
    public void Lookup_NonBeginnerDifficulty_Throws()
    {
        var bb7 = new Chord(new PitchClass(10), Quality.Dominant7);

        Assert.Throws<NotSupportedException>(() => VoicingBook.Lookup(bb7, Difficulty.Intermediate));
    }
}
