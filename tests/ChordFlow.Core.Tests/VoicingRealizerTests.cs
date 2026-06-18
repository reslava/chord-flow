using ChordFlow.Domain;
using Xunit;

using ChordFlow.Instruments.Guitar;

namespace ChordFlow.Core.Tests;

public class VoicingRealizerTests
{
    private static int? Fret(Voicing v, int stringNumber) =>
        v.Positions.Where(p => p.String == stringNumber).Select(p => (int?)p.Fret).SingleOrDefault();

    [Fact]
    public void Realize_OpenCShapeToD_BecomesTheCShapeDBarre()
    {
        // x32010 (C-shape Cmaj) slid up to D: every fretted string + 2, open strings ride the shift
        // into the barre → x54232 (a genuine D major: D F# A D F#).
        VoicingShape cShape = VoicingDslParser.Parse("voicing Cmaj shape:C root:5 frets: x 3 2 0 1 0");

        Voicing? d = cShape.Realize(new PitchClass(2));

        Assert.NotNull(d);
        Assert.Null(Fret(d!, 6));
        Assert.Equal(5, Fret(d!, 5));
        Assert.Equal(4, Fret(d!, 4));
        Assert.Equal(2, Fret(d!, 3));
        Assert.Equal(3, Fret(d!, 2));
        Assert.Equal(2, Fret(d!, 1));
        Assert.Equal(2, d!.FirstFret);
    }

    [Fact]
    public void Realize_GShapeToG_OctaveFoldsDownToOpenG()
    {
        // The G-shape canonical-C form (875558) realized at G folds down to open G (320003).
        VoicingShape gShape = VoicingDslParser.Parse("voicing Gmaj shape:G root:6 frets: 3 2 0 0 0 3");

        Voicing? g = gShape.Realize(new PitchClass(7));

        Assert.NotNull(g);
        Assert.Equal(3, Fret(g!, 6));
        Assert.Equal(2, Fret(g!, 5));
        Assert.Equal(0, Fret(g!, 4));
        Assert.Equal(0, Fret(g!, 3));
        Assert.Equal(0, Fret(g!, 2));
        Assert.Equal(3, Fret(g!, 1));
    }

    [Fact]
    public void Realize_AtAnchorRoot_ReturnsCanonicalUnchanged()
    {
        VoicingShape gShape = VoicingDslParser.Parse("voicing Gmaj shape:G root:6 frets: 3 2 0 0 0 3");

        Voicing? atC = gShape.Realize(new PitchClass(0));

        Assert.NotNull(atC);
        Assert.Equal(gShape.Canonical.Positions.OrderBy(p => p.String), atC!.Positions.OrderBy(p => p.String));
    }

    [Fact]
    public void Realize_AllTwelveRoots_StayWithinTheWindow()
    {
        VoicingShape cShape = VoicingDslParser.Parse("voicing Cmaj shape:C root:5 frets: x 3 2 0 1 0");

        for (int root = 0; root < 12; root++)
        {
            Voicing? v = cShape.Realize(new PitchClass(root));
            Assert.NotNull(v);
            Assert.All(v!.Positions, p => Assert.InRange(p.Fret, 0, VoicingRealizer.MaxFret));
        }
    }

    [Fact]
    public void Realize_MutedStrings_SurviveTheTranspose()
    {
        VoicingShape cShape = VoicingDslParser.Parse("voicing Cmaj shape:C root:5 frets: x 3 2 0 1 0");

        Voicing? f = cShape.Realize(new PitchClass(5));

        Assert.Equal(new[] { 6 }, f!.MutedStrings);
    }

    [Fact]
    public void Realize_ShapeWiderThanTheWindow_ReturnsNull()
    {
        // A contrived shape spanning past fret 15 at every octave placement cannot fit the 0..15 window.
        var wide = new Voicing(new[] { new FretPosition(6, 0), new FretPosition(1, 20) });
        var shape = new VoicingShape(Quality.Major, CagedShape.C, 6, wide);

        Assert.Null(shape.Realize(new PitchClass(0)));
    }
}
