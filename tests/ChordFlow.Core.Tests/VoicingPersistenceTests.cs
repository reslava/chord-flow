using ChordFlow.Music.Harmony;
using ChordFlow.Persistence;
using ChordFlow.Persistence.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

using ChordFlow.Instruments.Guitar;

namespace ChordFlow.Core.Tests;

/// <summary>
/// Persistence round-trip for the <c>Voicings</c> table: the EF migration creates the table, and a stored
/// row reconstructs its <see cref="VoicingShape"/> by re-parsing the canonical-C <c>Dsl</c> (C3 — realized
/// frets are never stored). Uses an in-memory SQLite connection kept open across contexts.
/// </summary>
public class VoicingPersistenceTests
{
    private static DbContextOptions<ChordFlowDbContext> Options(SqliteConnection conn) =>
        new DbContextOptionsBuilder<ChordFlowDbContext>().UseSqlite(conn).Options;

    private static int? Fret(Voicing v, int stringNumber) =>
        v.Positions.Where(p => p.String == stringNumber).Select(p => (int?)p.Fret).SingleOrDefault();

    private static VoicingEntity Row(string id, string name, string dsl) => new()
    {
        Id = id,
        Name = name,
        Dsl = dsl,
        Origin = Origin.UserDefined,
        CreatedUtc = DateTime.UtcNow,
    };

    [Fact]
    public void LoadShapes_ReconstructsStoredVoicings()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        using (var db = new ChordFlowDbContext(Options(conn)))
        {
            db.Database.Migrate();
            db.Voicings.Add(Row("v-c", "Open C", "voicing Cmaj shape:C root:5 frets: x 3 2 0 1 0"));
            db.Voicings.Add(Row("v-e", "E-shape C", "voicing Cmaj shape:E root:6 frets: 8 10 10 9 8 8"));
            db.SaveChanges();
        }

        using (var db = new ChordFlowDbContext(Options(conn)))
        {
            IReadOnlyList<VoicingShape> shapes = new VoicingStore(db).LoadShapes();

            Assert.Equal(2, shapes.Count);
            Assert.All(shapes, s => Assert.Equal(Quality.Major, s.Quality));
            Assert.Contains(shapes, s => s.Shape == CagedShape.C);
            Assert.Contains(shapes, s => s.Shape == CagedShape.E);
        }
    }

    [Fact]
    public void Find_StoredVoicing_ReconstructsCanonicalShape()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        using (var db = new ChordFlowDbContext(Options(conn)))
        {
            db.Database.Migrate();
            db.Voicings.Add(Row("v-c", "Open C", "voicing Cmaj shape:C root:5 frets: x 3 2 0 1 0"));
            db.SaveChanges();
        }

        using (var db = new ChordFlowDbContext(Options(conn)))
        {
            VoicingShape? shape = new VoicingStore(db).Find("v-c");

            Assert.NotNull(shape);
            Assert.Equal(CagedShape.C, shape!.Shape);
            Assert.Equal(5, shape.RootString);
            Assert.Null(Fret(shape.Canonical, 6)); // low E muted
            Assert.Equal(0, Fret(shape.Canonical, 3)); // open G
        }
    }

    [Fact]
    public void Find_MissingId_ReturnsNull()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        using var db = new ChordFlowDbContext(Options(conn));
        db.Database.Migrate();

        Assert.Null(new VoicingStore(db).Find("nope"));
    }

    [Fact]
    public void Voicings_CarryCatalogColumns_ForPackFiltering()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        using (var db = new ChordFlowDbContext(Options(conn)))
        {
            db.Database.Migrate();
            VoicingEntity row = Row("v-jazz", "Jazz E7", "voicing C7 shape:E root:6 frets: 8 10 8 9 8 8");
            row.Genre = "jazz";
            row.Tags = "[\"shell\"]";
            db.Voicings.Add(row);
            db.SaveChanges();
        }

        using (var db = new ChordFlowDbContext(Options(conn)))
        {
            VoicingEntity stored = db.Voicings.Single(v => v.Id == "v-jazz");
            Assert.Equal("jazz", stored.Genre);
            Assert.Equal("[\"shell\"]", stored.Tags);
        }
    }
}
