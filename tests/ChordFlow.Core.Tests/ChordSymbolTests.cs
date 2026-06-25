using ChordFlow.Music.Harmony;
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
}
