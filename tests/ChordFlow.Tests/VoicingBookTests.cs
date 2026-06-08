using ChordFlow.Domain;
using Xunit;

namespace ChordFlow.Tests;

public class VoicingBookTests
{
    // Open-string pitch classes, indexed by alphaTab string number (1 = high E .. 6 = low E).
    // Index 0 is unused so the string number indexes directly.
    private static readonly int[] OpenStringPc = { 0, 4, 11, 7, 2, 9, 4 };

    private static int NotePc(FretPosition p) => (OpenStringPc[p.String] + p.Fret) % 12;

    // The movable shell covers every root — all 12 keys, not just the Bb blues' I/IV/V.
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)]
    public void Lookup_Dominant7Shell_SpellsRootMajorThirdMinorSeventh(int root)
    {
        var chord = new Chord(new PitchClass(root), Quality.Dominant7);

        Voicing voicing = VoicingBook.Lookup(chord, Difficulty.Beginner);

        var actual = voicing.Positions.Select(NotePc).ToHashSet();
        var expected = new HashSet<int> { root % 12, (root + 4) % 12, (root + 10) % 12 };
        Assert.Equal(3, voicing.Positions.Count);
        Assert.Equal(expected, actual);
        // Shape stays contiguous and on the fretboard (no negative frets).
        Assert.All(voicing.Positions, p => Assert.InRange(p.Fret, 0, 12));
    }

    // The three previously hand-authored rows must come out byte-identical so existing
    // Bb-blues rendering (and the renderer tests) are unchanged.
    [Theory]
    [InlineData(10, 1, 0, 1)] // Bb7
    [InlineData(3, 6, 5, 6)]  // Eb7
    [InlineData(5, 8, 7, 8)]  // F7
    public void Lookup_AuthoredBluesChords_MatchOriginalFrets(int root, int s5, int s4, int s3)
    {
        var chord = new Chord(new PitchClass(root), Quality.Dominant7);

        Voicing voicing = VoicingBook.Lookup(chord, Difficulty.Beginner);

        Assert.Equal(new FretPosition(5, s5), voicing.Positions[0]);
        Assert.Equal(new FretPosition(4, s4), voicing.Positions[1]);
        Assert.Equal(new FretPosition(3, s3), voicing.Positions[2]);
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
    public void Lookup_EveryKeyOfTheBlues_Resolves()
    {
        // What used to throw (C blues needs C7) now resolves — the movable shape covers all keys.
        foreach (Key key in SeedData.AllMajorKeys)
        {
            foreach (Chord chord in Transposer.Realize(SeedData.TwelveBarBlues, key))
            {
                Voicing voicing = VoicingBook.Lookup(chord, Difficulty.Beginner);
                Assert.Equal(3, voicing.Positions.Count);
            }
        }
    }

    [Fact]
    public void Lookup_NonDominant7Quality_Throws()
    {
        var cMajor = new Chord(new PitchClass(0), Quality.Major); // shell shape is dom7-only

        Assert.Throws<NotSupportedException>(() => VoicingBook.Lookup(cMajor, Difficulty.Beginner));
    }

    [Fact]
    public void Lookup_NonBeginnerDifficulty_Throws()
    {
        var bb7 = new Chord(new PitchClass(10), Quality.Dominant7);

        Assert.Throws<NotSupportedException>(() => VoicingBook.Lookup(bb7, Difficulty.Intermediate));
    }
}
