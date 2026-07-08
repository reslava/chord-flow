using ChordFlow.Music.Harmony;
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

    // ---- RealizeGrip: movable literal grips (IN3/IN11/C3/C9) ----

    private static GripSpec Grip(string spec) => Assert.IsType<GripSpec>(VoicingDslParser.ParseSpec(spec));

    [Fact]
    public void RealizeGrip_AtAuthoredRoot_KeepsFretsVerbatim()
    {
        // `8 x 7 9 8 x` — bass = low E fret 8 = C. Realized at C, it stays exactly as authored (WYSIWYG).
        Voicing? v = VoicingRealizer.RealizeGrip(Grip("8 x 7 9 8 x"), new PitchClass(0));

        Assert.NotNull(v);
        Assert.Equal(8, Fret(v!, 6));
        Assert.Null(Fret(v!, 5));
        Assert.Equal(7, Fret(v!, 4));
        Assert.Equal(9, Fret(v!, 3));
        Assert.Equal(8, Fret(v!, 2));
        Assert.Null(Fret(v!, 1));
        Assert.Equal(new[] { 5, 1 }.OrderBy(x => x), v!.MutedStrings!.OrderBy(x => x));
    }

    [Fact]
    public void RealizeGrip_ToAnotherRoot_ShiftsTheWholeShape()
    {
        // C shape (bass C) → D (+2): every fret rises 2 (min 9 stays in [0,11], no fold).
        Voicing? v = VoicingRealizer.RealizeGrip(Grip("8 x 7 9 8 x"), new PitchClass(2));

        Assert.Equal(10, Fret(v!, 6));
        Assert.Equal(9, Fret(v!, 4));
        Assert.Equal(11, Fret(v!, 3));
        Assert.Equal(10, Fret(v!, 2));
    }

    [Fact]
    public void RealizeGrip_ExplicitVoicedRoot_MatchesBassInference()
    {
        Voicing? inferred = VoicingRealizer.RealizeGrip(Grip("8 x 7 9 8 x"), new PitchClass(2));
        Voicing? declared = VoicingRealizer.RealizeGrip(Grip("8 x 7 9 8 x root:6"), new PitchClass(2));

        Assert.Equal(
            inferred!.Positions.OrderBy(p => p.String),
            declared!.Positions.OrderBy(p => p.String));
    }

    [Fact]
    public void RealizeGrip_RootlessPhantomRoot_TransposesWithoutSoundingTheRoot()
    {
        // Rootless: low E muted, phantom root C at fret 8 (string 6). At C, frets are verbatim; the root stays muted.
        GripSpec rootless = Grip("x 3 2 3 1 x root:6@8");

        Voicing? atC = VoicingRealizer.RealizeGrip(rootless, new PitchClass(0));
        Assert.NotNull(atC);
        Assert.Null(Fret(atC!, 6));                 // root never sounds
        Assert.Equal(3, Fret(atC!, 5));
        Assert.Equal(1, Fret(atC!, 2));
        Assert.Contains(6, atC!.MutedStrings!);

        // At D (+2) the played strings all rise 2, still rootless.
        Voicing? atD = VoicingRealizer.RealizeGrip(rootless, new PitchClass(2));
        Assert.Equal(5, Fret(atD!, 5));
        Assert.Equal(3, Fret(atD!, 2));
        Assert.Null(Fret(atD!, 6));
    }

    [Fact]
    public void RealizeGrip_VoicedRootOnMutedString_Throws()
    {
        // `root:6` names the (muted) low E without a phantom fret — ambiguous, must be root:6@<fret>.
        Assert.Throws<FormatException>(
            () => VoicingRealizer.RealizeGrip(Grip("x 3 2 3 1 x root:6"), new PitchClass(0)));
    }
}
