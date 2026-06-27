using ChordFlow.Music.Harmony;
using ChordFlow.Music.Melody;
using ChordFlow.Instruments.Guitar;
using Xunit;

namespace ChordFlow.Core.Tests;

public class GuitarInstrumentTests
{
    // Open-string pitch classes, indexed by alphaTab string number (1 = high E .. 6 = low E).
    private static readonly int[] OpenStringPc = { 0, 4, 11, 7, 2, 9, 4 };

    [Fact]
    public void Diagram_DelegatesToVoicingDiagramBuild()
    {
        VoicingShape cMaj = VoicingDslParser.Parse("voicing Cmaj shape:C root:5 frets: x 3 2 0 1 0");

        FretboardDiagram viaFacade = new GuitarInstrument().Diagram(cMaj);
        FretboardDiagram direct = VoicingDiagram.Build(cMaj);

        Assert.Equal(direct.Title, viaFacade.Title);
        Assert.Equal(direct.Markers.Count, viaFacade.Markers.Count);
        Assert.Equal(direct.MutedStrings, viaFacade.MutedStrings);
    }

    // Relocated from LeadTargetsTests — fret resolution is now an instrument concern.
    [Fact]
    public void ResolveLead_G7Third_ReturnsOnlyPositionsThatSoundB()
    {
        var g7 = new Chord(new PitchClass(7), Quality.Dominant7);
        TargetZone third = LeadTargets.GuideTones(g7).Single(z => z.Tone.Function == ChordToneFunction.Third);

        IReadOnlyList<FretPosition> positions = new GuitarInstrument().ResolveLead(g7, third);

        Assert.NotEmpty(positions);
        // Every returned fret must sound a B (pitch class 11).
        Assert.All(positions, p => Assert.Equal(11, (OpenStringPc[p.String] + p.Fret) % 12));
        // Open B string (string 2, fret 0) is a B.
        Assert.Contains(new FretPosition(2, 0), positions);
    }
}
