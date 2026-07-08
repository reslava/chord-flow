using ChordFlow.Music.Harmony;
using Xunit;

using ChordFlow.Instruments.Guitar;

namespace ChordFlow.Core.Tests;

public class VoicingDslParserTests
{
    // Fret on a given alphaTab string (6 = low E .. 1 = high E), or null when that string is muted.
    private static int? Fret(Voicing v, int stringNumber) =>
        v.Positions.Where(p => p.String == stringNumber).Select(p => (int?)p.Fret).SingleOrDefault();

    [Fact]
    public void Parse_OpenCShapeAtC_KeepsAuthoredFrets()
    {
        // x32010 authored at C stays at C (semisToC = 0).
        VoicingShape shape = VoicingDslParser.Parse("voicing Cmaj shape:C root:5 frets: x 3 2 0 1 0");

        Assert.Equal(Quality.Major, shape.Quality);
        Assert.Equal(CagedShape.C, shape.Shape);
        Assert.Equal(5, shape.RootString);
        Assert.Null(Fret(shape.Canonical, 6));            // low E muted
        Assert.Equal(new[] { 6 }, shape.Canonical.MutedStrings);
        Assert.Equal(3, Fret(shape.Canonical, 5));
        Assert.Equal(2, Fret(shape.Canonical, 4));
        Assert.Equal(0, Fret(shape.Canonical, 3));
        Assert.Equal(1, Fret(shape.Canonical, 2));
        Assert.Equal(0, Fret(shape.Canonical, 1));
        Assert.Equal(0, shape.Canonical.FirstFret);
    }

    [Fact]
    public void Parse_OpenGShape_NormalizesToGShapeAtC()
    {
        // Open G (320003) authored at G normalizes up to the G-shape at C: 8 7 5 5 5 8.
        VoicingShape shape = VoicingDslParser.Parse("voicing Gmaj shape:G root:6 frets: 3 2 0 0 0 3");

        Assert.Equal(Quality.Major, shape.Quality);
        Assert.Equal(CagedShape.G, shape.Shape);
        Assert.Equal(8, Fret(shape.Canonical, 6));
        Assert.Equal(7, Fret(shape.Canonical, 5));
        Assert.Equal(5, Fret(shape.Canonical, 4));
        Assert.Equal(5, Fret(shape.Canonical, 3));
        Assert.Equal(5, Fret(shape.Canonical, 2));
        Assert.Equal(8, Fret(shape.Canonical, 1));
        Assert.Equal(5, shape.Canonical.FirstFret);
    }

    [Fact]
    public void Parse_SameShapeDifferentAnchors_DedupeToIdenticalCanonical()
    {
        // The E-shape authored at C and the same E-shape authored at B must collapse to one canonical form.
        VoicingShape atC = VoicingDslParser.Parse("voicing Cmaj shape:E root:6 frets: 8 10 10 9 8 8");
        VoicingShape atB = VoicingDslParser.Parse("voicing Bmaj shape:E root:6 frets: 7 9 9 8 7 7");

        Assert.Equal(atC.Canonical.Positions.OrderBy(p => p.String), atB.Canonical.Positions.OrderBy(p => p.String));
    }

    [Theory]
    [InlineData("C7", Quality.Dominant7)]
    [InlineData("Cm7", Quality.Minor7)]
    [InlineData("Cmin", Quality.Minor)]
    [InlineData("Cmaj7", Quality.Major7)]
    [InlineData("C-", Quality.Minor)]
    [InlineData("C6", Quality.Major6)]
    [InlineData("Cm6", Quality.Minor6)]
    [InlineData("C-6", Quality.Minor6)]
    public void Parse_QualitySuffixes_MapToQuality(string chord, Quality expected)
    {
        VoicingShape shape = VoicingDslParser.Parse($"voicing {chord} shape:A root:5 frets: x 3 2 0 1 3");
        Assert.Equal(expected, shape.Quality);
    }

    [Fact]
    public void Parse_FlatAnchor_NormalizesFromTheFlatRoot()
    {
        // Bb (pc 10): semisToC = 2, so every fret rises 2 before the octave-fold.
        VoicingShape shape = VoicingDslParser.Parse("voicing Bbmaj shape:A root:5 frets: x 1 3 3 3 1");

        // x13331 + 2 = x 3 5 5 5 3, min 3 in [0,11] → unchanged by the fold.
        Assert.Equal(3, Fret(shape.Canonical, 5));
        Assert.Equal(5, Fret(shape.Canonical, 4));
        Assert.Equal(3, Fret(shape.Canonical, 1));
    }

    [Fact]
    public void Parse_TrailingComment_IsIgnored()
    {
        VoicingShape shape = VoicingDslParser.Parse("voicing Cmaj shape:C root:5 frets: x 3 2 0 1 0   # open C");
        Assert.Equal(CagedShape.C, shape.Shape);
        Assert.Equal(5, shape.Canonical.Positions.Count);
    }

    [Theory]
    [InlineData("voicing Cmaj shape:C root:5 x 3 2 0 1 0")]            // no frets: clause
    [InlineData("voicing Cmaj shape:C root:5 frets: x 3 2 0 1")]       // 5 fret values, not 6
    [InlineData("voicing Cmaj shape:C root:5 frets: x 3 2 0 1 q")]     // invalid fret token
    [InlineData("voicing Czzz shape:C root:5 frets: x 3 2 0 1 0")]     // unknown quality suffix
    [InlineData("voicing Cmaj shape:B root:5 frets: x 3 2 0 1 0")]     // invalid CAGED shape
    [InlineData("voicing Cmaj shape:C root:7 frets: x 3 2 0 1 0")]     // root string out of 1..6
    [InlineData("voicing Cmaj shape:C root:5 frets: x x x x x x")]     // no fretted strings
    [InlineData("chord Cmaj shape:C root:5 frets: x 3 2 0 1 0")]       // missing 'voicing' keyword
    public void Parse_MalformedInput_Throws(string dsl)
    {
        Assert.Throws<FormatException>(() => VoicingDslParser.Parse(dsl));
    }

    // ---- ParseSpec: the shared voicing-spec grammar (per-chord {…} + voice defaults) ----

    private static int? SpecFret(GripSpec g, int stringNumber) =>
        g.Positions.Where(p => p.String == stringNumber).Select(p => (int?)p.Fret).SingleOrDefault();

    [Fact]
    public void ParseSpec_BareGrip_ReadsFretsVerbatim()
    {
        var grip = Assert.IsType<GripSpec>(VoicingDslParser.ParseSpec("8 x 7 9 8 x"));

        Assert.Equal(8, SpecFret(grip, 6));
        Assert.Null(SpecFret(grip, 5));
        Assert.Equal(7, SpecFret(grip, 4));
        Assert.Equal(9, SpecFret(grip, 3));
        Assert.Equal(8, SpecFret(grip, 2));
        Assert.Null(SpecFret(grip, 1));
        Assert.Equal(new[] { 5, 1 }.OrderBy(x => x), grip.MutedStrings.OrderBy(x => x));
        Assert.Null(grip.Anchor);
    }

    [Fact]
    public void ParseSpec_CustomPrefix_IsSugarForBareGrip()
    {
        var bare = Assert.IsType<GripSpec>(VoicingDslParser.ParseSpec("8 x 7 9 8 x"));
        var prefixed = Assert.IsType<GripSpec>(VoicingDslParser.ParseSpec("c: 8 x 7 9 8 x"));

        Assert.Equal(
            bare.Positions.OrderBy(p => p.String),
            prefixed.Positions.OrderBy(p => p.String));
    }

    [Fact]
    public void ParseSpec_VoicedRootAnchor_HasNoPhantomFret()
    {
        var grip = Assert.IsType<GripSpec>(VoicingDslParser.ParseSpec("8 x 7 9 8 x root:6"));

        Assert.NotNull(grip.Anchor);
        Assert.Equal(6, grip.Anchor!.String);
        Assert.Null(grip.Anchor.Fret);
    }

    [Fact]
    public void ParseSpec_PhantomRootOnMutedString_CarriesTheFret()
    {
        // Rootless shell: low E muted, root declared as a phantom at fret 8.
        var grip = Assert.IsType<GripSpec>(VoicingDslParser.ParseSpec("x 3 2 3 1 x root:6@8"));

        Assert.Contains(6, grip.MutedStrings);
        Assert.Equal(6, grip.Anchor!.String);
        Assert.Equal(8, grip.Anchor.Fret);
    }

    [Theory]
    [InlineData("u: C6", "u", "C6")]
    [InlineData("a: shell-C6", "a", "shell-C6")]
    [InlineData("swing: C6", "swing", "C6")]
    [InlineData("swing:C6", "swing", "C6")] // glued form
    public void ParseSpec_Reference_SplitsSourceAndId(string spec, string source, string id)
    {
        var reference = Assert.IsType<ReferenceSpec>(VoicingDslParser.ParseSpec(spec));

        Assert.Equal(source, reference.Source);
        Assert.Equal(id, reference.Id);
    }

    [Theory]
    [InlineData("8 x 7 9 8 x")]
    [InlineData("8 x 7 9 8 x root:6")]
    [InlineData("x 3 2 3 1 x root:6@8")]
    [InlineData("u: C6")]
    [InlineData("a: shell-C6")]
    public void ParseSpec_RoundTripsThroughWriter(string spec)
    {
        string written = VoicingDslWriter.SpecToDsl(VoicingDslParser.ParseSpec(spec));

        Assert.Equal(spec, written);
        // Writer is idempotent through a re-parse (record equality can't compare the List<> members).
        Assert.Equal(written, VoicingDslWriter.SpecToDsl(VoicingDslParser.ParseSpec(written)));
    }

    [Theory]
    [InlineData("")]                       // empty
    [InlineData("8 x 7 9 8")]              // 5 frets, not 6
    [InlineData("8 x 7 9 8 x x")]          // 7 frets
    [InlineData("8 x 7 9 8 q")]            // invalid fret token
    [InlineData("x x x x x x")]            // no fretted strings
    [InlineData("8 x 7 9 8 x root:7")]     // root string out of 1..6
    [InlineData("8 x 7 9 8 x root:6@-1")]  // negative phantom fret
    [InlineData("u:")]                     // reference missing id
    [InlineData("u: C6 C7")]               // reference with an extra token
    public void ParseSpec_MalformedInput_Throws(string spec)
    {
        Assert.Throws<FormatException>(() => VoicingDslParser.ParseSpec(spec));
    }
}
