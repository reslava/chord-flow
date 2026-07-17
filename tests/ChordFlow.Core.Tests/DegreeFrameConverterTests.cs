using ChordFlow.Music.Harmony;
using ChordFlow.Music.Progressions;
using Xunit;

namespace ChordFlow.Core.Tests;

public class DegreeFrameConverterTests
{
    [Theory]
    [InlineData(0, false, 0)]   // C major → parent C
    [InlineData(9, true, 0)]    // A minor → parent C
    [InlineData(4, true, 7)]    // E minor → parent G
    [InlineData(2, true, 5)]    // D minor → parent F
    public void ParentTonic_IsTheRelativeMajorForMinor_TheTonicForMajor(int tonic, bool isMinor, int expected)
    {
        Assert.Equal(expected, DegreeFrameConverter.ParentTonic(new Key(new PitchClass(tonic), isMinor)).Value);
    }

    // Minor's tonic sits on the parent major's 6th, so a minor author-degree rotates 1→6 2→7 3→1 4→2 5→3 6→4 7→5.
    [Theory]
    [InlineData(1, 6)]
    [InlineData(2, 7)]
    [InlineData(3, 1)]
    [InlineData(4, 2)]
    [InlineData(5, 3)]
    [InlineData(6, 4)]
    [InlineData(7, 5)]
    public void ToParent_Minor_RotatesTonicToTheParentsSixth(int authorDegree, int parentDegree)
    {
        RomanDegree result = DegreeFrameConverter.ToParent(new RomanDegree(authorDegree, Quality.Minor), Tonality.Minor);
        Assert.Equal(parentDegree, result.Degree);
    }

    [Fact]
    public void ToParent_Major_IsIdentity()
    {
        for (int d = 1; d <= 7; d++)
        {
            Assert.Equal(d, DegreeFrameConverter.ToParent(new RomanDegree(d, Quality.Major), Tonality.Major).Degree);
        }
    }

    // The accidental is carried through unchanged: minor #7 → parent #5 (both G♯), minor b2 → parent b7 (both B♭).
    [Theory]
    [InlineData(7, Accidental.Sharp, 5)]
    [InlineData(6, Accidental.Sharp, 4)]
    [InlineData(2, Accidental.Flat, 7)]
    public void ToParent_Minor_CarriesTheAccidentalUnchanged(int authorDegree, Accidental accidental, int parentDegree)
    {
        RomanDegree result = DegreeFrameConverter.ToParent(
            new RomanDegree(authorDegree, Quality.Diminished7, accidental), Tonality.Minor);

        Assert.Equal(parentDegree, result.Degree);
        Assert.Equal(accidental, result.Accidental);
    }

    [Theory]
    [InlineData(Tonality.Minor)]
    [InlineData(Tonality.Major)]
    public void ToAuthor_IsTheExactInverseOfToParent(Tonality home)
    {
        foreach (Accidental accidental in new[] { Accidental.Natural, Accidental.Sharp, Accidental.Flat })
        {
            for (int d = 1; d <= 7; d++)
            {
                var original = new RomanDegree(d, Quality.Minor, accidental);
                RomanDegree roundTripped = DegreeFrameConverter.ToAuthor(
                    DegreeFrameConverter.ToParent(original, home), home);

                Assert.Equal(original.Degree, roundTripped.Degree);
                Assert.Equal(original.Accidental, roundTripped.Accidental);
            }
        }
    }
}
