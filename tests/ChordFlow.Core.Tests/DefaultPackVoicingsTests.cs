using ChordFlow.Domain;
using ChordFlow.Features.Packs;
using ChordFlow.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

using ChordFlow.Instruments.Guitar;

namespace ChordFlow.Core.Tests;

/// <summary>
/// Verification for the authored default-pack voicings content (packages/default-pack thread, req IN4/IN5/C1):
/// every shipped <c>Content/default-pack/voicings/*.dsl</c> parses and realizes across all 12 roots inside the
/// 0–15 window; the import path stores them as <see cref="Origin.BuiltIn"/> and a <see cref="VoicingBook"/> over
/// the stored set shadows the generated shell for the shipped qualities; a couple of golden cells anchor frets.
/// </summary>
public class DefaultPackVoicingsTests
{
    // The authored matrix: maj/min/dom7/maj7/m7 × full CAGED (5 each = 25) + aug × full CAGED (5) +
    // m7b5/dim7 at the playable E/A/D grips (3 each = 6). Total 36. A drift here flags an accidental add/drop.
    private const int ExpectedVoicingCount = 36;

    private static DbContextOptions<ChordFlowDbContext> Options(SqliteConnection conn) =>
        new DbContextOptionsBuilder<ChordFlowDbContext>().UseSqlite(conn).Options;

    private static IReadOnlyList<PackDefinition> VoicingDefs() =>
        DefaultPack.Load().Definitions.Where(d => d.Kind == ContentKind.Voicing).ToList();

    private static int? Fret(Voicing v, int stringNumber) =>
        v.Positions.Where(p => p.String == stringNumber).Select(p => (int?)p.Fret).SingleOrDefault();

    [Fact]
    public void DefaultPack_ShipsTheFullAuthoredVoicingMatrix()
    {
        Assert.Equal(ExpectedVoicingCount, VoicingDefs().Count);
    }

    [Fact]
    public void EveryAuthoredVoicing_ParsesAndRealizesAcrossAllTwelveRoots_WithinWindow()
    {
        foreach (PackDefinition def in VoicingDefs())
        {
            // Parse must not throw (IN5) — the id names the offender if it does.
            VoicingShape shape = VoicingDslParser.Parse(def.Dsl);

            // The canonical C placement (root 0) always fits; the realizer slides to every other root and
            // returns null only when no octave of the shape fits 0–15. Nothing may throw, nothing may exceed 15.
            Voicing? atC = shape.Realize(new PitchClass(0));
            Assert.True(atC is not null, $"{def.Id}: canonical C placement does not fit the 0–15 window.");

            for (int root = 0; root < 12; root++)
            {
                Voicing? v = shape.Realize(new PitchClass(root));
                if (v is not null)
                {
                    Assert.All(v.Positions, p => Assert.InRange(p.Fret, 0, VoicingRealizer.MaxFret));
                }
            }
        }
    }

    [Fact]
    public void ImportedAsBuiltIn_AVoicingBookShadowsTheGeneratedShell_ForShippedQualities()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = new ChordFlowDbContext(Options(conn));
        db.Database.Migrate();

        DefaultPack.ImportInto(db);

        // Only the 31 voicings reach the Voicings table; progressions/songs/rhythms land elsewhere.
        IReadOnlyList<VoicingShape> shapes = new VoicingStore(db).LoadShapes();
        Assert.Equal(ExpectedVoicingCount, shapes.Count);

        var book = new VoicingBook(shapes);

        // The shipped blues progressions need dom7 and m7 — both must now have stored candidates that shadow
        // the 3-note BeginnerShell (IN4). C7 and Cm7 both author multi-string grips.
        var c7 = new Chord(new PitchClass(0), Quality.Dominant7);
        var cm7 = new Chord(new PitchClass(0), Quality.Minor7);

        Assert.NotEmpty(book.Candidates(c7, Difficulty.Beginner));
        Assert.NotEmpty(book.Candidates(cm7, Difficulty.Beginner));
        Assert.True(book.Lookup(c7, Difficulty.Beginner).Positions.Count > 3, "stored C7 voicing should shadow the 3-note shell");
        Assert.True(book.Lookup(cm7, Difficulty.Beginner).Positions.Count > 3, "stored Cm7 voicing should shadow the 3-note shell");
    }

    [Fact]
    public void ImportedVoicings_AreStampedBuiltIn_WithNullPackId()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = new ChordFlowDbContext(Options(conn));
        db.Database.Migrate();

        DefaultPack.ImportInto(db);

        Assert.Equal(ExpectedVoicingCount, db.Voicings.AsNoTracking().Count());
        Assert.All(db.Voicings.AsNoTracking().ToList(), v =>
        {
            Assert.Equal(Origin.BuiltIn, v.Origin);
            Assert.Null(v.PackId);
        });
    }

    [Theory]
    // Golden cells — the canonical-C frets the shape must realize to (regression anchor for the authored fingerings).
    [InlineData("maj_cshape", null, 3, 2, 0, 1, 0)]      // open C major  x 3 2 0 1 0
    [InlineData("dom7_eshape", 8, 10, 8, 9, 8, 8)]       // E-shape C7     8 10 8 9 8 8
    [InlineData("dim7_ashape", null, 3, 4, 2, 4, 5)]     // A-shape Cdim7  x 3 4 2 4 5
    public void GoldenCells_RealizeToTheAuthoredCanonicalFrets(
        string id, int? s6, int? s5, int? s4, int? s3, int? s2, int? s1)
    {
        PackDefinition def = VoicingDefs().Single(d => d.Id == id);
        VoicingShape shape = VoicingDslParser.Parse(def.Dsl);

        Voicing atC = shape.Realize(new PitchClass(0))!;

        Assert.Equal(s6, Fret(atC, 6));
        Assert.Equal(s5, Fret(atC, 5));
        Assert.Equal(s4, Fret(atC, 4));
        Assert.Equal(s3, Fret(atC, 3));
        Assert.Equal(s2, Fret(atC, 2));
        Assert.Equal(s1, Fret(atC, 1));
    }

    [Fact]
    public void Dim7Cell_ParsesAsTheNewDiminished7Quality()
    {
        PackDefinition def = VoicingDefs().Single(d => d.Id == "dim7_eshape");

        VoicingShape shape = VoicingDslParser.Parse(def.Dsl);

        Assert.Equal(Quality.Diminished7, shape.Quality);
    }
}
