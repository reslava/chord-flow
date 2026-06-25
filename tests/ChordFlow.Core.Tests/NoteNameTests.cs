using ChordFlow.Music.Harmony;
using Xunit;

namespace ChordFlow.Core.Tests;

public class NoteNameTests
{
    [Theory]
    [InlineData('F', 0, "F")]
    [InlineData('F', 1, "F#")]
    [InlineData('B', -1, "Bb")]
    [InlineData('B', 1, "B#")]   // letter-pure, no collapse to C
    [InlineData('F', -1, "Fb")]  // letter-pure, no collapse to E
    [InlineData('C', 2, "C##")]
    [InlineData('B', -2, "Bbb")]
    public void Symbol_SpellsLetterPlusAccidentals(char letter, int accidental, string expected)
    {
        Assert.Equal(expected, new NoteName(letter, accidental).Symbol);
    }
}
