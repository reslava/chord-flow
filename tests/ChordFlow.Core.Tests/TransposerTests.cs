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

    // first-class-minor-keys (C frame): the author types tonic-relative minor; DegreeFrameConverter.ToParent
    // normalizes to the parent-major degrees the Transposer realizes (the parser will do this conversion).
    private static Chord[] RealizeMinor(Key key, params RomanDegree[] authorDegrees)
    {
        RomanDegree[] parent = authorDegrees
            .Select(d => DegreeFrameConverter.ToParent(d, Tonality.Minor))
            .ToArray();
        return Transposer.Realize(new Progression("t", "t", parent), key);
    }

    [Fact]
    public void Realize_MinorKey_C_RealizesParentMajorDegreesDirectly()
    {
        // The Transposer's C contract: stored degrees are parent-major. In A minor (parent major C),
        // parent degree `6-` is the tonic Am and parent degree `1` is C (the natural ♭III).
        var aMinor = new Key(new PitchClass(9), IsMinor: true);

        Assert.Equal(9, Transposer.Realize(
            new Progression("t", "t", new RomanDegree[] { new(6, Quality.Minor) }), aMinor)[0].Root.Value); // Am
        Assert.Equal(0, Transposer.Realize(
            new Progression("t", "t", new RomanDegree[] { new(1, Quality.Major) }), aMinor)[0].Root.Value); // C
    }

    [Fact]
    public void Realize_MinorKey_C_NaturalMinorTonicSubdominantDominant()
    {
        // Author `1- 4- 5-` in A minor → ToParent → `6- 2- 3-` → Am, Dm, Em.
        Chord[] chords = RealizeMinor(new Key(new PitchClass(9), IsMinor: true),
            new RomanDegree(1, Quality.Minor), new RomanDegree(4, Quality.Minor), new RomanDegree(5, Quality.Minor));

        Assert.Equal(new[] { 9, 2, 4 }, chords.Select(c => c.Root.Value)); // Am, Dm, Em
        Assert.All(chords, c => Assert.Equal(Quality.Minor, c.Quality));
    }

    [Fact]
    public void Realize_MinorKey_C_NaturalThirdSixthSeventhAreBare()
    {
        // The C ergonomic win: ♭III/♭VI/♭VII are authored BARE `3 6 7` (not flatted) — they are diatonic
        // to the parent major, so ToParent maps them to `1 4 5` → C, F, G.
        Chord[] chords = RealizeMinor(new Key(new PitchClass(9), IsMinor: true),
            new RomanDegree(3, Quality.Major), new RomanDegree(6, Quality.Major), new RomanDegree(7, Quality.Major));

        Assert.Equal(new[] { 0, 5, 7 }, chords.Select(c => c.Root.Value)); // C, F, G
    }

    [Fact]
    public void Realize_MinorKey_C_MinorTwoFiveOne()
    {
        // Author `2ø 57 1-` in A minor → ToParent → `7ø 37 6-` → Bm7♭5, E7, Am.
        Chord[] chords = RealizeMinor(new Key(new PitchClass(9), IsMinor: true),
            new RomanDegree(2, Quality.HalfDiminished7), new RomanDegree(5, Quality.Dominant7), new RomanDegree(1, Quality.Minor));

        Assert.Equal(new[] { 11, 4, 9 }, chords.Select(c => c.Root.Value)); // Bm7♭5, E7, Am
        Assert.Equal(Quality.HalfDiminished7, chords[0].Quality);
        Assert.Equal(Quality.Dominant7, chords[1].Quality);
        Assert.Equal(Quality.Minor, chords[2].Quality);
    }

    [Fact]
    public void Realize_MinorKey_C_HarmonicAndMelodicRaisedRootsSpellLetterPure()
    {
        // The payoff C unlocks over A1: a chord rooted on a raised tone spells correctly, because the raised
        // tone is an accidental'd degree in the parent-major frame (→ letter-pure RootSpelling), not a bare one.
        var aMinor = new Key(new PitchClass(9), IsMinor: true);

        // Harmonic-minor vii°7: author `#7dim7` → ToParent `#5dim7` → G♯dim7, root spelled G♯ (not A♭).
        Chord vii = RealizeMinor(aMinor, new RomanDegree(7, Quality.Diminished7, Accidental.Sharp))[0];
        Assert.Equal(8, vii.Root.Value);
        Assert.Equal(new NoteName('G', 1), vii.RootSpelling); // G♯, not A♭

        // Melodic-minor vi°: author `#6ø` → ToParent `#4ø` → F♯m7♭5, root spelled F♯ (not G♭).
        Chord vi = RealizeMinor(aMinor, new RomanDegree(6, Quality.HalfDiminished7, Accidental.Sharp))[0];
        Assert.Equal(6, vi.Root.Value);
        Assert.Equal(new NoteName('F', 1), vi.RootSpelling); // F♯, not G♭
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
