using ChordFlow.Features.Voicings;
using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;
using ChordFlow.Persistence;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// <see cref="VoicingReferenceSource"/>: origin-strict <c>u:</c>/<c>pkg:</c> resolution against id-tagged rows,
/// engine <c>a:</c> derivation, and fail-loud misses (null) — the reference tier of the cascade (req IN2/IN6).
/// </summary>
public class VoicingReferenceSourceTests
{
    private static readonly VoicingShape OpenC =
        VoicingDslParser.Parse("voicing Cmaj shape:C root:5 frets: x 3 2 0 1 0");

    private static int? Fret(Voicing v, int stringNumber) =>
        v.Positions.Where(p => p.String == stringNumber).Select(p => (int?)p.Fret).SingleOrDefault();

    private static VoicingReferenceSource Source(
        params (string Id, VoicingShape Shape, ContentSource Source, string? PackId)[] rows) => new(rows);

    private static readonly Chord CMajor = new(new PitchClass(0), Quality.Major);

    [Fact]
    public void Resolve_UserReference_RealizesTheStoredShapeAtTheChordRoot()
    {
        var source = Source(("openC", OpenC, ContentSource.User, null));

        Voicing? v = source.Resolve(VoicingReferenceSource.UserSource, "openC", CMajor);

        Assert.NotNull(v);
        Assert.Equal(3, Fret(v!, 5));   // x 3 2 0 1 0 at C = verbatim
        Assert.Equal(0, Fret(v!, 1));
    }

    [Fact]
    public void Resolve_UnknownUserId_ReturnsNull()
    {
        var source = Source(("openC", OpenC, ContentSource.User, null));
        Assert.Null(source.Resolve(VoicingReferenceSource.UserSource, "nope", CMajor));
    }

    [Fact]
    public void Resolve_UserId_ThatOnlyExistsAsPackage_ReturnsNull()
    {
        // Origin-strict: `u: shared` must not match a package row of the same id (req IN6 — filtered-out source).
        var source = Source(("shared", OpenC, ContentSource.Package, "swing"));
        Assert.Null(source.Resolve(VoicingReferenceSource.UserSource, "shared", CMajor));
    }

    [Fact]
    public void Resolve_PackageReference_MatchesSourceAndPackId()
    {
        var source = Source(("openC", OpenC, ContentSource.Package, "swing"));

        Assert.NotNull(source.Resolve("swing", "openC", CMajor));   // right pack
        Assert.Null(source.Resolve("bebop", "openC", CMajor));      // wrong pack → miss
    }

    [Fact]
    public void Resolve_AutomaticReference_DerivesTheEngineGrip()
    {
        // auto:shell:dom7:E derived at a C7 chord — no stored rows needed.
        var cDom7 = new Chord(new PitchClass(0), Quality.Dominant7);
        string id = AutomaticVoicingId.For(VoicingFamily.Shell, Quality.Dominant7, CagedShape.E);

        Voicing? v = VoicingReferenceSource.Empty.Resolve(VoicingReferenceSource.AutomaticSource, id, cDom7);

        Assert.NotNull(v);
        Assert.NotEmpty(v!.Positions);
    }

    [Fact]
    public void Resolve_MalformedAutomaticId_ReturnsNull()
    {
        Assert.Null(VoicingReferenceSource.Empty.Resolve(VoicingReferenceSource.AutomaticSource, "not-an-auto-id", CMajor));
    }
}
