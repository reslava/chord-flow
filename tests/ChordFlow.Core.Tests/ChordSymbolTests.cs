using ChordFlow.Music.Harmony;
using ChordFlow.Music.Progressions;
using ChordFlow.Music.Rhythm;
using Xunit;

namespace ChordFlow.Core.Tests;

public class ChordSymbolTests
{
    private static readonly Key FMajor = new(new PitchClass(5), false);

    [Theory]
    // A present RootSpelling names the root verbatim, regardless of key — the written degree wins.
    [InlineData('B', 0, Quality.Diminished7, "Bdim7")]   // F: #4dim7
    [InlineData('G', -1, Quality.Dominant7, "Gb7")]      // F: b27 tritone sub
    [InlineData('F', 1, Quality.Diminished7, "F#dim7")]
    [InlineData('B', 1, Quality.Major, "B#")]            // letter-pure edge, no collapse to C
    public void Format_HonorsRootSpelling_OverKey(char letter, int accidental, Quality quality, string expected)
    {
        var chord = new Chord(new PitchClass(0), quality, new NoteName(letter, accidental));

        Assert.Equal(expected, ChordSymbol.Format(chord, FMajor));
    }

    [Fact]
    public void Format_NoRootSpelling_FallsBackToKeyTableSpelling()
    {
        // Diatonic chord with no override spells from the key exactly as before (constraint C2).
        var chord = new Chord(new PitchClass(10), Quality.Dominant7); // pc10 in F major = Bb

        Assert.Equal("Bb7", ChordSymbol.Format(chord, FMajor));
        Assert.Equal(NoteSpeller.Name(new PitchClass(10), FMajor) + "7", ChordSymbol.Format(chord, FMajor));
    }

    [Fact]
    public void Format_MinorProgression_SpellsNaturalAndRaisedChordsCorrectly()
    {
        // first-class-minor-keys (C), end-to-end: a minor tune's user-visible chord symbols. Natural-minor
        // diatonic chords spell from the relative major (A minor → C's naturals); the harmonic vii°7 and
        // melodic vi° raised roots spell letter-pure — G♯dim7 / F♯m7♭5, never A♭/G♭.
        var aMinor = new Key(new PitchClass(9), IsMinor: true);
        Progression prog = ProgressionParser.Parse(
            "t", "T", "1- 2ø 3 4- 5- 6 7 #7dim7 #6ø", TimeSignature.FourFour, home: Tonality.Minor);

        string[] symbols = Transposer.Realize(prog, aMinor)
            .Select(c => ChordSymbol.Format(c, aMinor))
            .ToArray();

        Assert.Equal(
            new[] { "Am", "Bm7b5", "C", "Dm", "Em", "F", "G", "G#dim7", "F#m7b5" },
            symbols);
    }
}
