using ChordFlow.Music.Harmony;
using System.Collections.Generic;
using System.Linq;

using ChordFlow.Instruments.Guitar;
using Xunit;

namespace ChordFlow.Core.Tests;

public class CagedShapeDiagramTests
{
    private static readonly PitchClass C = new(0); // Key C

    [Theory]
    [InlineData(CagedShape.C)]
    [InlineData(CagedShape.A)]
    [InlineData(CagedShape.G)]
    [InlineData(CagedShape.E)]
    [InlineData(CagedShape.D)]
    public void Build_MarkersMatchTheOctaveShapeAnchors(CagedShape shape)
    {
        FretboardDiagram d = CagedShapeDiagram.Build(shape, C);

        var markerPositions = d.Markers.Select(m => new FretPosition(m.String, m.Fret)).ToHashSet();
        Assert.Equal(OctaveShape.AnchorsFor(C, shape, 0, 15).ToHashSet(), markerPositions);
        Assert.All(d.Markers, m => Assert.Equal("root", m.Function));
        Assert.Empty(d.MutedStrings);
    }

    [Theory]
    [InlineData(CagedShape.E)]
    [InlineData(CagedShape.C)]
    [InlineData(CagedShape.D)]
    public void Build_CarriesTheOctaveZoneAsTheBand(CagedShape shape)
    {
        FretboardDiagram d = CagedShapeDiagram.Build(shape, C);
        OctaveZone zone = OctaveShape.Zone(C, shape, 0, 15);

        Assert.Equal(zone.MinFret, d.ZoneFretMin);
        Assert.Equal(zone.MaxFret, d.ZoneFretMax);
    }

    [Fact]
    public void Build_FramesTheWindowAroundTheZoneWithMargin()
    {
        FretboardDiagram d = CagedShapeDiagram.Build(CagedShape.E, C); // zone 8..10
        Assert.Equal(6, d.FretMin);  // 8 - 2
        Assert.Equal(12, d.FretMax); // 10 + 2
    }

    [Fact]
    public void Build_DShape_Str2IsTheOctaveUp_NotTheInWindowUnison()
    {
        FretboardDiagram d = CagedShapeDiagram.Build(CagedShape.D, C);
        Assert.Equal(13, d.Markers.Single(m => m.String == 2).Fret); // octave-up, not the fret-1 unison
    }

    [Fact]
    public void Build_LabelsPrimaryAsRoot_AndOctavesAs8And15()
    {
        FretboardDiagram d = CagedShapeDiagram.Build(CagedShape.E, C); // anchors: str6 (1), str4 (8), str1 (15)
        Assert.Equal("1", d.Markers.Single(m => m.String == 6).Interval);
        Assert.Equal("8", d.Markers.Single(m => m.String == 4).Interval);
        Assert.Equal("15", d.Markers.Single(m => m.String == 1).Interval);
    }
}
