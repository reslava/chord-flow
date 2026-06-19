using ChordFlow.Domain;
using Xunit;

using ChordFlow.Instruments.Guitar;

namespace ChordFlow.Core.Tests;

public class FretboardTuningTests
{
    // The legacy literal tuning table the absolute base must reproduce: open-string pitch classes,
    // indexed by alphaTab string number (1 = high E .. 6 = low E), index 0 unused.
    private static readonly int[] LegacyOpenPitchClass = { 0, 4, 11, 7, 2, 9, 4 };

    [Theory]
    [InlineData(1, 24)] // high E
    [InlineData(2, 19)] // B
    [InlineData(3, 15)] // G
    [InlineData(4, 10)] // D
    [InlineData(5, 5)]  // A
    [InlineData(6, 0)]  // low E
    public void AbsoluteSemitone_OpenString_IsCumulativeOffsetFromLowE(int stringNumber, int expected)
    {
        Assert.Equal(expected, Fretboard.AbsoluteSemitone(stringNumber, 0));
    }

    [Fact]
    public void AbsoluteSemitone_BStringStep_IsFourNotFive()
    {
        // The one tuning irregularity: string 3 -> 2 is +4 semitones (every other adjacent step is +5).
        int g = Fretboard.AbsoluteSemitone(3, 0);
        int b = Fretboard.AbsoluteSemitone(2, 0);
        Assert.Equal(4, b - g);
    }

    [Fact]
    public void AbsoluteSemitone_AddsFret()
    {
        Assert.Equal(Fretboard.AbsoluteSemitone(6, 0) + 5, Fretboard.AbsoluteSemitone(6, 5));
    }

    [Fact]
    public void PitchClassAt_DerivedFromAbsoluteBase_MatchesLegacyTableEverywhere()
    {
        // The single-sourced derivation must reproduce the old literal table for every (string, fret).
        for (int stringNumber = 1; stringNumber <= Fretboard.StringCount; stringNumber++)
        {
            for (int fret = 0; fret <= 24; fret++)
            {
                int expected = (LegacyOpenPitchClass[stringNumber] + fret) % 12;
                Assert.Equal(expected, Fretboard.PitchClassAt(stringNumber, fret).Value);
            }
        }
    }

    [Fact]
    public void PositionsFor_RoundTripsWithPitchClassAt()
    {
        for (int pc = 0; pc < 12; pc++)
        {
            foreach (FretPosition position in Fretboard.PositionsFor(new PitchClass(pc)))
            {
                Assert.Equal(pc, Fretboard.PitchClassAt(position.String, position.Fret).Value);
            }
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void AbsoluteSemitone_RejectsOutOfRangeString(int stringNumber)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Fretboard.AbsoluteSemitone(stringNumber, 0));
    }

    [Fact]
    public void AbsoluteSemitone_RejectsNegativeFret()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Fretboard.AbsoluteSemitone(6, -1));
    }
}
