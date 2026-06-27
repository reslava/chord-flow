using ChordFlow.Music.Harmony;
using ChordFlow.Features.Packs;
using ChordFlow.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

using ChordFlow.Instruments.Guitar;

namespace ChordFlow.Core.Tests;

/// <summary>
/// Validation for the 46 authored CAGED grips now living in the <b>test-only golden-oracle fixture</b>
/// (engine-derived-as-app-source IN8): every <c>fixtures/caged-oracle/*.dsl</c> parses and realizes across all
/// 12 roots inside the 0–15 window; a couple of golden cells anchor the canonical frets. Also pins the other
/// half of the relocation — the default pack ships <b>no</b> voicings (the engine derives <c>automatic</c> ones).
/// </summary>
public class CagedOracleVoicingsTests
{
    // maj/min/dom7/maj7/m7/6/m6 × full CAGED (35) + aug × full CAGED (5) + m7b5/dim7 at E/A/D (6) = 46.
    private const int ExpectedVoicingCount = 46;

    private static DbContextOptions<ChordFlowDbContext> Options(SqliteConnection conn) =>
        new DbContextOptionsBuilder<ChordFlowDbContext>().UseSqlite(conn).Options;

    private static int? Fret(Voicing v, int stringNumber) =>
        v.Positions.Where(p => p.String == stringNumber).Select(p => (int?)p.Fret).SingleOrDefault();

    [Fact]
    public void Fixture_HoldsTheFullAuthored36()
    {
        Assert.Equal(ExpectedVoicingCount, OracleVoicings.Load().Count);
    }

    [Fact]
    public void EveryFixtureVoicing_ParsesAndRealizesAcrossAllTwelveRoots_WithinWindow()
    {
        foreach ((string id, _, VoicingShape shape) in OracleVoicings.Load())
        {
            // The canonical C placement always fits; the realizer slides to every other root and returns null
            // only when no octave of the shape fits 0–15. Nothing may throw, nothing may exceed 15.
            Assert.True(shape.Realize(new PitchClass(0)) is not null, $"{id}: canonical C placement does not fit 0–15.");

            for (int root = 0; root < 12; root++)
            {
                if (shape.Realize(new PitchClass(root)) is { } v)
                {
                    Assert.All(v.Positions, p => Assert.InRange(p.Fret, 0, VoicingRealizer.MaxFret));
                }
            }
        }
    }

    [Theory]
    // Golden cells — the canonical-C frets the shape must realize to (regression anchor for the authored fingerings).
    [InlineData("maj_cshape", null, 3, 2, 0, 1, 0)]      // open C major  x 3 2 0 1 0
    [InlineData("dom7_eshape", 8, 10, 8, 9, 8, 8)]       // E-shape C7     8 10 8 9 8 8
    [InlineData("dim7_ashape", null, 3, 4, 2, 4, 5)]     // A-shape Cdim7  x 3 4 2 4 5
    public void GoldenCells_RealizeToTheAuthoredCanonicalFrets(
        string id, int? s6, int? s5, int? s4, int? s3, int? s2, int? s1)
    {
        VoicingShape shape = OracleVoicings.Load().Single(v => v.Id == id).Shape;
        Voicing atC = shape.Realize(new PitchClass(0))!;

        Assert.Equal(s6, Fret(atC, 6));
        Assert.Equal(s5, Fret(atC, 5));
        Assert.Equal(s4, Fret(atC, 4));
        Assert.Equal(s3, Fret(atC, 3));
        Assert.Equal(s2, Fret(atC, 2));
        Assert.Equal(s1, Fret(atC, 1));
    }

    [Fact]
    public void Dim7Cell_ParsesAsTheDiminished7Quality()
    {
        VoicingShape shape = OracleVoicings.Load().Single(v => v.Id == "dim7_eshape").Shape;
        Assert.Equal(Quality.Diminished7, shape.Quality);
    }

    [Fact]
    public void DefaultPack_ShipsNoVoicings_TheyAreEngineDerivedNow()
    {
        // The relocation severed the seed (IN8/C1): the pack carries progressions/songs/rhythms only.
        Assert.DoesNotContain(DefaultPack.Load().Definitions, d => d.Kind == ContentKind.Voicing);

        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = new ChordFlowDbContext(Options(conn));
        db.Database.Migrate();
        DefaultPack.ImportInto(db);

        Assert.False(db.Voicings.AsNoTracking().Any());
    }
}
