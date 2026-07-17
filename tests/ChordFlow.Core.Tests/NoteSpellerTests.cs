using ChordFlow.Music.Harmony;
using Xunit;

namespace ChordFlow.Core.Tests;

public class NoteSpellerTests
{
    private static Key Major(int tonic) => new(new PitchClass(tonic), false);

    private static Key Minor(int tonic) => new(new PitchClass(tonic), true);

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

    // first-class-minor-keys (IN3): a minor key appends the alphaTab-native `minor` mode suffix; the
    // tonic itself is spelled from the relative major (Am → C table, C#m → E table's sharps).
    [Theory]
    [InlineData(9, "aminor")]   // A minor (relative C)
    [InlineData(0, "cminor")]   // C minor (relative Eb → flats)
    [InlineData(1, "c#minor")]  // C# minor (relative E → sharps)
    [InlineData(4, "eminor")]   // E minor (relative G → sharps)
    public void KeySignatureToken_MinorKey_AppendsMinorSuffix(int tonic, string expected)
    {
        Assert.Equal(expected, NoteSpeller.KeySignatureToken(Minor(tonic)));
    }

    // Round-trips a persisted token, major and minor (IN3).
    [Theory]
    [InlineData("c", 0, false)]
    [InlineData("f#", 6, false)]
    [InlineData("bb", 10, false)]
    [InlineData("aminor", 9, true)]
    [InlineData("c#minor", 1, true)]
    [InlineData("cminor", 0, true)]
    public void KeyFromSignatureToken_ParsesModeAndTonic(string token, int expectedTonic, bool expectedMinor)
    {
        Key key = NoteSpeller.KeyFromSignatureToken(token);
        Assert.Equal(expectedTonic, key.Tonic.Value);
        Assert.Equal(expectedMinor, key.IsMinor);
    }

    [Fact]
    public void KeySignatureToken_RoundTripsThroughKeyFromSignatureToken()
    {
        foreach (bool isMinor in new[] { false, true })
        {
            for (int tonic = 0; tonic < 12; tonic++)
            {
                var key = new Key(new PitchClass(tonic), isMinor);
                Key back = NoteSpeller.KeyFromSignatureToken(NoteSpeller.KeySignatureToken(key));
                Assert.Equal(key.Tonic.Value, back.Tonic.Value);
                Assert.Equal(key.IsMinor, back.IsMinor);
            }
        }
    }

    // first-class-minor-keys (IN4): a minor key spells its diatonic pitch classes from its RELATIVE
    // major's table — already delivered by UsesSharps(tonic + 3). A minor ⇒ C's all-naturals; E minor
    // ⇒ G's sharps. Confirms no new spelling code is needed.
    [Theory]
    [InlineData(9, 0, "C")]    // A minor: pc0 = C (relative C, natural)
    [InlineData(9, 5, "F")]    // A minor: pc5 = F
    [InlineData(9, 7, "G")]    // A minor: pc7 = G
    [InlineData(4, 6, "F#")]   // E minor (relative G): pc6 = F#, not Gb
    [InlineData(0, 3, "Eb")]   // C minor (relative Eb): pc3 = Eb
    public void Name_MinorKey_SpellsFromRelativeMajorTable(int keyTonic, int pitchClass, string expected)
    {
        Assert.Equal(expected, NoteSpeller.Name(new PitchClass(pitchClass), Minor(keyTonic)));
    }
}
