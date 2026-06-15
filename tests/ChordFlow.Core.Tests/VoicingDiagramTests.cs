using ChordFlow.Domain;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The voicing <see cref="DiagramModel"/> builder (step 3): per-string state (muted/open/fretted), spelled note,
/// interval label, and chord-tone function are computed in Core from a canonical-C <see cref="VoicingShape"/>
/// (IN5/IN6). Covers the open-C golden mapping and the role-aware enharmonic labels (dim7 → bb7, aug → #5).
/// </summary>
public class VoicingDiagramTests
{
    private static DiagramString Str(DiagramModel d, int stringNumber) =>
        d.Strings.Single(s => s.String == stringNumber);

    [Fact]
    public void OpenCMajor_MapsEveryStringToItsNoteIntervalAndFunction()
    {
        VoicingShape shape = VoicingDslParser.Parse("voicing Cmaj shape:C root:5 frets: x 3 2 0 1 0");

        DiagramModel d = VoicingDiagram.Build(shape);

        Assert.Equal(0, d.FirstFret); // open shape sits at the nut
        Assert.Equal(6, d.Strings.Count);

        Assert.Equal("muted", Str(d, 6).State); // low E muted

        DiagramString a = Str(d, 5); // A string, 3rd fret → C, the root
        Assert.Equal("fretted", a.State);
        Assert.Equal(3, a.Fret);
        Assert.Equal("C", a.Note);
        Assert.Equal("R", a.Interval);
        Assert.Equal("root", a.Function);

        DiagramString d4 = Str(d, 4); // D string, 2nd fret → E, the major third
        Assert.Equal("E", d4.Note);
        Assert.Equal("3", d4.Interval);
        Assert.Equal("third", d4.Function);

        DiagramString g = Str(d, 3); // open G → the fifth
        Assert.Equal("open", g.State);
        Assert.Equal(0, g.Fret);
        Assert.Equal("G", g.Note);
        Assert.Equal("5", g.Interval);
        Assert.Equal("fifth", g.Function);

        DiagramString b = Str(d, 2); // B string, 1st fret → C, the root again
        Assert.Equal("R", b.Interval);
        Assert.Equal("root", b.Function);

        DiagramString e1 = Str(d, 1); // open high E → the third
        Assert.Equal("open", e1.State);
        Assert.Equal("3", e1.Interval);
    }

    [Fact]
    public void Diminished7_LabelsTheDiminishedSeventhAsBb7()
    {
        // C dim7 = C Eb Gb Bbb(=A, pc 9). An open A string sounds pc 9 → the bb7 of a dim7.
        var shape = new VoicingShape(
            Quality.Diminished7, CagedShape.A, RootString: 5,
            new Voicing(new[] { new FretPosition(5, 0) }, FirstFret: 0));

        DiagramString a = VoicingDiagram.Build(shape).Strings.Single(s => s.String == 5);

        Assert.Equal("A", a.Note);
        Assert.Equal("bb7", a.Interval);
        Assert.Equal("seventh", a.Function);
    }

    [Fact]
    public void Augmented_LabelsTheRaisedFifthAsSharp5()
    {
        // C aug = C E G#(pc 8). Low E string, 4th fret sounds pc 8 → the #5 of an augmented chord.
        var shape = new VoicingShape(
            Quality.Augmented, CagedShape.E, RootString: 6,
            new Voicing(new[] { new FretPosition(6, 4) }, FirstFret: 4));

        DiagramString s6 = VoicingDiagram.Build(shape).Strings.Single(s => s.String == 6);

        Assert.Equal("#5", s6.Interval);
        Assert.Equal("fifth", s6.Function);
    }

    [Fact]
    public void NoteOutsideTheQuality_IsLabelledAsTension()
    {
        // A major triad with an added pc 2 (the 9th) on the open D string — not a chord tone, so a tension.
        var shape = new VoicingShape(
            Quality.Major, CagedShape.C, RootString: 5,
            new Voicing(new[] { new FretPosition(4, 0) }, FirstFret: 0));

        DiagramString d = VoicingDiagram.Build(shape).Strings.Single(s => s.String == 4);

        Assert.Equal("tension", d.Function);
        Assert.Equal("9", d.Interval);
    }
}
