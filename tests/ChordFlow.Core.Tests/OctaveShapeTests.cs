using System.Collections.Generic;
using System.Linq;

using ChordFlow.Domain;
using ChordFlow.Instruments.Guitar;
using Xunit;

namespace ChordFlow.Core.Tests;

public class OctaveShapeTests
{
    private static readonly PitchClass C = new(0); // Key C (PitchClass 0)

    // --- Step 1: the authored partition ---

    [Theory]
    [InlineData(CagedShape.C, new[] { 5, 2 })]
    [InlineData(CagedShape.A, new[] { 5, 3 })]
    [InlineData(CagedShape.G, new[] { 6, 3, 1 })]
    [InlineData(CagedShape.E, new[] { 6, 4, 1 })]
    [InlineData(CagedShape.D, new[] { 4, 2 })]
    public void RootStrings_AreTheAuthoredPartition_PrimaryFirst(CagedShape shape, int[] expected)
    {
        Assert.Equal(expected, OctaveShape.RootStrings(shape));
    }

    // --- Step 2: option-(a) anchor query ---

    [Fact]
    public void AnchorsFor_LandOnTheShapesRootStrings()
    {
        IReadOnlyList<FretPosition> anchors = OctaveShape.AnchorsFor(C, CagedShape.E, 0, 12);
        Assert.Equal(new HashSet<int> { 6, 4, 1 }, anchors.Select(a => a.String).ToHashSet());
    }

    [Fact]
    public void AnchorsFor_PrimaryIsLowestOccurrenceAtOrAboveMinFret()
    {
        // E shape primary is string 6; Key C root on string 6 is fret 8.
        IReadOnlyList<FretPosition> anchors = OctaveShape.AnchorsFor(C, CagedShape.E, 0, 12);
        Assert.Equal(8, anchors.First(a => a.String == 6).Fret);
    }

    [Fact]
    public void AnchorsFor_AreAscendingOctaves_NotInWindowUnisons()
    {
        // The D-shape regression: the str2 anchor must be the +3 octave-up (fret 13), NOT the in-window
        // unison (fret 1, the same pitch as str4 fret 10).
        IReadOnlyList<FretPosition> anchors = OctaveShape.AnchorsFor(C, CagedShape.D, 0, 12);
        Assert.Equal(10, anchors.First(a => a.String == 4).Fret);
        Assert.Equal(13, anchors.First(a => a.String == 2).Fret);
    }

    [Fact]
    public void AnchorsFor_AllSoundTheRoot()
    {
        IReadOnlyList<FretPosition> anchors = OctaveShape.AnchorsFor(C, CagedShape.G, 0, 12);
        Assert.All(anchors, a => Assert.Equal(0, Fretboard.PitchClassAt(a.String, a.Fret).Value));
    }

    [Fact]
    public void AnchorsFor_RecursEveryTwelveFrets()
    {
        FretPosition low = OctaveShape.AnchorsFor(C, CagedShape.E, 0, 12).First(a => a.String == 6);
        FretPosition high = OctaveShape.AnchorsFor(C, CagedShape.E, 9, 24).First(a => a.String == 6);
        Assert.Equal(low.Fret + 12, high.Fret);
    }

    [Fact]
    public void AnchorsFor_ReturnsEmpty_WhenRootNotOnPrimaryInWindow()
    {
        // E shape primary str6; Key C root on str6 is fret 8 — a window ending below it yields nothing.
        Assert.Empty(OctaveShape.AnchorsFor(C, CagedShape.E, 0, 5));
    }

    // --- Step 4: golden oracle — offsets, octave-zone spans, box partitions ---

    [Fact]
    public void Anchors_ReproduceTheFiveOctaveShapeOffsets_AtKeyC()
    {
        int Fret(CagedShape shape, int stringNumber) =>
            OctaveShape.AnchorsFor(C, shape, 0, 12).First(a => a.String == stringNumber).Fret;

        Assert.Equal(-2, Fret(CagedShape.C, 2) - Fret(CagedShape.C, 5)); // C: str2 = str5 - 2
        Assert.Equal(+2, Fret(CagedShape.A, 3) - Fret(CagedShape.A, 5)); // A: str3 = str5 + 2
        Assert.Equal(-3, Fret(CagedShape.G, 3) - Fret(CagedShape.G, 6)); // G: str3 = str6 - 3
        Assert.Equal(0, Fret(CagedShape.G, 1) - Fret(CagedShape.G, 6));  // G: str1 = str6 (two octaves up)
        Assert.Equal(+2, Fret(CagedShape.E, 4) - Fret(CagedShape.E, 6)); // E: str4 = str6 + 2
        Assert.Equal(0, Fret(CagedShape.E, 1) - Fret(CagedShape.E, 6));  // E: str1 = str6 (two octaves up)
        Assert.Equal(+3, Fret(CagedShape.D, 2) - Fret(CagedShape.D, 4)); // D: str2 = str4 + 3
    }

    [Theory]
    [InlineData(CagedShape.E, 8, 10)]
    [InlineData(CagedShape.C, 1, 3)]
    [InlineData(CagedShape.A, 3, 5)]
    [InlineData(CagedShape.G, 5, 8)]
    [InlineData(CagedShape.D, 10, 13)]
    public void Zone_IsTheFretSpanOfTheAnchors_AtKeyC(CagedShape shape, int min, int max)
    {
        Assert.Equal(new OctaveZone(min, max), OctaveShape.Zone(C, shape, 0, 12));
    }

    [Fact]
    public void Boxes_PartitionTheShape_PerTheChatTable()
    {
        Assert.Equal(
            new[] { new CagedBox(6, 5, false), new CagedBox(5, 2, true), new CagedBox(2, 1, false) },
            OctaveShape.Boxes(CagedShape.C));

        Assert.Equal(
            new[] { new CagedBox(6, 5, false), new CagedBox(5, 3, true), new CagedBox(3, 1, false) },
            OctaveShape.Boxes(CagedShape.A));

        Assert.Equal(
            new[] { new CagedBox(6, 3, true), new CagedBox(3, 1, true) },
            OctaveShape.Boxes(CagedShape.G));

        Assert.Equal(
            new[] { new CagedBox(6, 4, true), new CagedBox(4, 1, true) },
            OctaveShape.Boxes(CagedShape.E));

        Assert.Equal(
            new[] { new CagedBox(6, 4, false), new CagedBox(4, 2, true), new CagedBox(2, 1, false) },
            OctaveShape.Boxes(CagedShape.D));
    }
}
