using ChordFlow.Domain;
using Xunit;

namespace ChordFlow.Tests;

public class NoteSpellerTests
{
    private static Key Major(int tonic) => new(new PitchClass(tonic), false);

    // Sharp keys spell their accidentals with sharps; flat keys with flats; C with naturals.
    [Theory]
    [InlineData(2, 1, "C#")]   // D major: pc1 = C#
    [InlineData(2, 6, "F#")]   // D major: pc6 = F#
    [InlineData(6, 6, "F#")]   // F# major tonic
    [InlineData(8, 1, "Db")]   // Ab major: pc1 = Db
    [InlineData(10, 10, "Bb")] // Bb major tonic
    [InlineData(10, 3, "Eb")]  // Bb major: pc3 = Eb
    [InlineData(0, 0, "C")]    // C major tonic
    [InlineData(0, 9, "A")]    // C major: pc9 = A
    public void Name_SpellsPerKeyAccidentalDirection(int keyTonic, int pitchClass, string expected)
    {
        Assert.Equal(expected, NoteSpeller.Name(new PitchClass(pitchClass), Major(keyTonic)));
    }

    [Theory]
    [InlineData(0, "c")]
    [InlineData(2, "d")]
    [InlineData(6, "f#")]
    [InlineData(8, "ab")]
    [InlineData(10, "bb")]
    public void KeySignatureToken_IsLowercaseSpelledTonic(int tonic, string expected)
    {
        Assert.Equal(expected, NoteSpeller.KeySignatureToken(Major(tonic)));
    }

    [Fact]
    public void KeySignatureToken_MatchesAllTwelveOldRendererTokens()
    {
        // The previous renderer's hardcoded token array — spelling must reproduce it exactly.
        string[] expected = { "c", "db", "d", "eb", "e", "f", "f#", "g", "ab", "a", "bb", "b" };
        for (int tonic = 0; tonic < 12; tonic++)
        {
            Assert.Equal(expected[tonic], NoteSpeller.KeySignatureToken(Major(tonic)));
        }
    }
}
