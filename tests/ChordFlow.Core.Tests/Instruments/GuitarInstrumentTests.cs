using ChordFlow.Exercises;
using ChordFlow.Music.Harmony;
using ChordFlow.Music.Melody;
using ChordFlow.Instruments.Guitar;
using Xunit;

namespace ChordFlow.Core.Tests;

public class GuitarInstrumentTests
{
    private static GuitarInstrument StrategyOnly() => new(new VoicingBook(Array.Empty<VoicingShape>()));

    // Open-string pitch classes, indexed by alphaTab string number (1 = high E .. 6 = low E).
    private static readonly int[] OpenStringPc = { 0, 4, 11, 7, 2, 9, 4 };

    [Fact]
    public void Realize_DelegatesToVoicingBookLookup()
    {
        var g7 = new Chord(new PitchClass(7), Quality.Dominant7);

        Voicing viaFacade = StrategyOnly().Realize(g7, Difficulty.Beginner);
        Voicing viaBook = new VoicingBook(Array.Empty<VoicingShape>()).Lookup(g7, Difficulty.Beginner);

        Assert.Equal(viaBook.Positions, viaFacade.Positions);
    }

    [Fact]
    public void Realize_StoredVoicing_ShadowsTheGeneratedShape()
    {
        // The facade carries the book's authored-shadows-generated behaviour through unchanged.
        VoicingShape stored = VoicingDslParser.Parse("voicing C7 shape:E root:6 frets: 8 10 8 9 8 8");
        var instrument = new GuitarInstrument(new VoicingBook(new[] { stored }));
        var c7 = new Chord(new PitchClass(0), Quality.Dominant7);

        Voicing got = instrument.Realize(c7, Difficulty.Beginner);

        Assert.Equal(stored.Realize(c7.Root)!.Positions.OrderBy(p => p.String), got.Positions.OrderBy(p => p.String));
    }

    [Fact]
    public void Diagram_DelegatesToVoicingDiagramBuild()
    {
        VoicingShape cMaj = VoicingDslParser.Parse("voicing Cmaj shape:C root:5 frets: x 3 2 0 1 0");

        FretboardDiagram viaFacade = StrategyOnly().Diagram(cMaj);
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

        IReadOnlyList<FretPosition> positions = StrategyOnly().ResolveLead(g7, third);

        Assert.NotEmpty(positions);
        // Every returned fret must sound a B (pitch class 11).
        Assert.All(positions, p => Assert.Equal(11, (OpenStringPc[p.String] + p.Fret) % 12));
        // Open B string (string 2, fret 0) is a B.
        Assert.Contains(new FretPosition(2, 0), positions);
    }
}
