using ChordFlow.Domain;
using ChordFlow.Instruments.Guitar;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The <see cref="ChordShapeDiagram"/> producer: a derived <see cref="ChordShape"/> → a <see cref="FretboardDiagram"/>
/// (the carrier the CAGED Chords page draws). Mirrors <c>CagedShapeDiagramTests</c> — markers per sounded string,
/// muted strings as chrome, the zone band, and the anchor finger in the title.
/// </summary>
public class ChordShapeDiagramTests
{
    private static readonly PitchClass C = new(0);

    [Fact]
    public void Build_EShapeMajor_LightsAllSixStrings_AnchorInTitle_ZoneBand()
    {
        // C major, E shape at C = 8 10 10 9 8 8 (no muted strings), index-anchored.
        ChordShape shape = CagedDerivation.Derive(Quality.Major, CagedShape.E, C, 8, 24);

        FretboardDiagram diagram = ChordShapeDiagram.Build(shape, C);

        Assert.Equal(6, diagram.Markers.Count);
        Assert.Empty(diagram.MutedStrings);
        Assert.Contains("E shape", diagram.Title);
        Assert.Contains("index", diagram.Title);                 // anchor finger surfaced
        Assert.Contains(diagram.Markers, m => m.Function == "root");
        Assert.NotNull(diagram.ZoneFretMin);                     // the octave zone is carried as a band
        Assert.NotNull(diagram.ZoneFretMax);
        Assert.Equal(8, diagram.FretMin);                        // lowest fretted fret
    }

    [Fact]
    public void Build_FretWindowContainsTheWholeZone_SoTheBandIsNeverClipped()
    {
        // caged-chords-chat-002: the explicit fret window must span the union of the fretted markers and the octave
        // zone, so the JS view never auto-fits to the top marker and clips the band. C·maj7·A places high (≈9-12).
        ChordShape shape = CagedDerivation.Derive(Quality.Major7, CagedShape.C, new PitchClass(9), 0, 15);

        FretboardDiagram diagram = ChordShapeDiagram.Build(shape, new PitchClass(9));

        Assert.NotNull(diagram.FretMin);
        Assert.NotNull(diagram.FretMax);
        Assert.True(diagram.FretMin <= diagram.ZoneFretMin, "window min must not clip the zone");
        Assert.True(diagram.FretMax >= diagram.ZoneFretMax, "window max must not clip the zone");
        // …and it must also contain every fretted marker.
        foreach (FretboardMarker m in diagram.Markers)
            if (m.Fret > 0)
                Assert.InRange(m.Fret, diagram.FretMin!.Value, diagram.FretMax!.Value);
    }

    [Fact]
    public void Build_AShapeMajor_MutesTheLowStringAsChrome()
    {
        // C major, A shape at C = x 3 5 5 5 3 — string 6 muted, five sounded.
        ChordShape shape = CagedDerivation.Derive(Quality.Major, CagedShape.A, C, 3, 24);

        FretboardDiagram diagram = ChordShapeDiagram.Build(shape, C);

        Assert.Equal(5, diagram.Markers.Count);
        Assert.Contains(6, diagram.MutedStrings);                // the muted string is chrome, not a marker
        Assert.DoesNotContain(diagram.Markers, m => m.String == 6);
    }

    [Fact]
    public void Build_DerivesCombosBeyondTheAuthoredPack()
    {
        // m7b5 is authored only for E/A/D, but the engine derives it in the C shape — the generator case (IN4).
        ChordShape shape = CagedDerivation.Derive(Quality.HalfDiminished7, CagedShape.C, C, 0, 24);

        FretboardDiagram diagram = ChordShapeDiagram.Build(shape, C);

        Assert.NotEmpty(diagram.Markers);
        Assert.Contains("C shape", diagram.Title);
    }
}
