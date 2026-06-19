using ChordFlow.Domain;
using Xunit;

using ChordFlow.Instruments.Guitar;

namespace ChordFlow.Core.Tests;

public class IntervalLatticeTests
{
    private static int Mod12(int v) => ((v % 12) + 12) % 12;

    // --- Step 2: Absolute + signed Distance ---

    [Fact]
    public void Absolute_DelegatesToFretboard()
    {
        Assert.Equal(0, IntervalLattice.Absolute(new FretPosition(6, 0)));   // low E
        Assert.Equal(24, IntervalLattice.Absolute(new FretPosition(1, 0)));  // high E, two octaves up
        Assert.Equal(8, IntervalLattice.Absolute(new FretPosition(5, 3)));   // A string, fret 3 = C
    }

    [Fact]
    public void Distance_AdjacentStrings_IsAFourth()
    {
        // Low E -> A (string 6 -> 5) open is a perfect fourth = 5 semitones.
        Assert.Equal(5, IntervalLattice.Distance(new FretPosition(6, 0), new FretPosition(5, 0)));
    }

    [Fact]
    public void Distance_AcrossBString_IsFourNotFive()
    {
        // The B-string irregularity: string 3 -> 2 open is a major third = 4 semitones.
        Assert.Equal(4, IntervalLattice.Distance(new FretPosition(3, 0), new FretPosition(2, 0)));
    }

    [Fact]
    public void Distance_IsSigned_DescendingIsNegative()
    {
        Assert.Equal(-5, IntervalLattice.Distance(new FretPosition(5, 0), new FretPosition(6, 0)));
    }

    [Fact]
    public void Distance_LowEToHighE_IsTwoOctaves()
    {
        Assert.Equal(24, IntervalLattice.Distance(new FretPosition(6, 0), new FretPosition(1, 0)));
    }

    // --- Step 3: label views (both via IntervalSpeller.Name) ---

    [Theory]
    [InlineData(0, "1")]
    [InlineData(3, "b3")]
    [InlineData(5, "4")]
    [InlineData(7, "5")]
    [InlineData(11, "7")]
    [InlineData(12, "1")]  // octave folds away in the pitch-class view
    [InlineData(-1, "7")]  // a semitone below the root reads as 7 (pitch class)
    public void PitchClassLabel_FoldsToOneThroughSeven(int distance, string expected)
    {
        Assert.Equal(expected, IntervalLattice.PitchClassLabel(distance));
    }

    [Fact]
    public void Describe_UnfoldsOctavesAndCarriesDirection()
    {
        LatticeInterval octave = IntervalLattice.Describe(12);
        Assert.Equal(new LatticeInterval(12, "8", 1, 1), octave);

        LatticeInterval ninth = IntervalLattice.Describe(14);
        Assert.Equal(new LatticeInterval(14, "9", 1, 1), ninth);

        LatticeInterval twoOctaves = IntervalLattice.Describe(24);
        Assert.Equal(new LatticeInterval(24, "15", 2, 1), twoOctaves);

        LatticeInterval descendingFifth = IntervalLattice.Describe(-7);
        Assert.Equal(new LatticeInterval(-7, "5", 0, -1), descendingFifth);

        LatticeInterval unison = IntervalLattice.Describe(0);
        Assert.Equal(new LatticeInterval(0, "1", 0, 0), unison);
    }

    // --- Step 4: PositionsOfInterval + LabelAt ---

    [Fact]
    public void LabelAt_DescribesTargetRelativeToRoot()
    {
        Assert.Equal(IntervalLattice.Describe(5), IntervalLattice.LabelAt(new FretPosition(6, 0), new FretPosition(5, 0)));
    }

    [Fact]
    public void PositionsOfInterval_ReturnsAllOctavesOfTheDegreeInWindow()
    {
        var root = new FretPosition(6, 0); // low E
        IReadOnlyList<FretPosition> roots = IntervalLattice.PositionsOfInterval(root, 0, 0, 12);

        Assert.All(roots, p => Assert.Equal(4, Fretboard.PitchClassAt(p.String, p.Fret).Value)); // all sound E
        Assert.Contains(new FretPosition(6, 0), roots);
        Assert.Contains(new FretPosition(1, 0), roots);
    }

    [Fact]
    public void PositionsOfInterval_RespectsTheFretWindow()
    {
        var root = new FretPosition(6, 0);
        IReadOnlyList<FretPosition> roots = IntervalLattice.PositionsOfInterval(root, 0, 1, 12);

        Assert.All(roots, p => Assert.True(p.Fret >= 1));
        Assert.DoesNotContain(new FretPosition(6, 0), roots);
    }

    [Fact]
    public void PositionsOfInterval_FifthOfLowE_IsB()
    {
        // PC of low E is 4 (E); +7 semitones = 11 (B).
        var root = new FretPosition(6, 0);
        IReadOnlyList<FretPosition> fifths = IntervalLattice.PositionsOfInterval(root, 7, 0, 12);

        Assert.NotEmpty(fifths);
        Assert.All(fifths, p => Assert.Equal(11, Fretboard.PitchClassAt(p.String, p.Fret).Value));
    }

    // --- Step 5: golden check — reproduce the five octave-shape root offsets ---

    [Theory]
    // shape, primaryString, secondaryString, secondaryFretOffset (relative to the primary root)
    [InlineData("C", 5, 2, -2)] // C: string-2 root = string-5 root -2
    [InlineData("A", 5, 3, +2)] // A: string-3 root = string-5 root +2
    [InlineData("G", 6, 3, -3)] // G: string-3 root = string-6 root -3
    [InlineData("G", 6, 1, 0)]  // G: string-1 root = string-6 root, same fret (two octaves up)
    [InlineData("E", 6, 4, +2)] // E: string-4 root = string-6 root +2
    [InlineData("E", 6, 1, 0)]  // E: string-1 root = string-6 root, same fret (two octaves up)
    [InlineData("D", 4, 2, +3)] // D: string-2 root = string-4 root +3
    public void OctaveShapeOffsets_AreTheUnisonOctaveSpecialCaseOfTheLattice(
        string shape, int primaryString, int secondaryString, int offset)
    {
        const int primaryFret = 5; // any fret that keeps both positions on the neck
        var primary = new FretPosition(primaryString, primaryFret);
        var secondary = new FretPosition(secondaryString, primaryFret + offset);

        int distance = IntervalLattice.Distance(primary, secondary);

        // The two anchors are the same note (interval ≡ 0 mod 12) — the octave-shape skeleton.
        Assert.Equal(0, Mod12(distance));
        // And it is a real octave displacement, not the identical position.
        Assert.NotEqual(0, distance);
        _ = shape; // documents which CAGED shape each row asserts
    }
}
