using ChordFlow.Persistence;
using ChordFlow.Persistence.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// Persistence round-trip for the new <c>Progressions</c> table: the EF migration creates the table and
/// a row survives save → reload with its <c>Dsl</c> and <c>Origin</c> intact (Origin stored by name).
/// Uses an in-memory SQLite connection kept open across two contexts.
/// </summary>
public class ProgressionPersistenceTests
{
    private static DbContextOptions<ChordFlowDbContext> Options(SqliteConnection conn) =>
        new DbContextOptionsBuilder<ChordFlowDbContext>().UseSqlite(conn).Options;

    [Fact]
    public void Migration_CreatesProgressionsTable_AndRoundTripsDslAndOrigin()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        // The migration (not EnsureCreated) builds the schema, proving it adds the Progressions table.
        using (var db = new ChordFlowDbContext(Options(conn)))
        {
            db.Database.Migrate();

            db.Progressions.Add(new ProgressionEntity
            {
                Id = "12bar_blues",
                Name = "12-Bar Blues",
                Dsl = "17 17 17 17 47 47 17 17 57 47 17 57",
                Origin = Origin.Pack,
                PackId = "default",
                CreatedUtc = new DateTime(2026, 6, 9, 0, 0, 0, DateTimeKind.Utc),
            });
            db.Progressions.Add(new ProgressionEntity
            {
                Id = "u-1",
                Name = "My Tune",
                Dsl = "1 4 5 1",
                Origin = Origin.UserDefined,
                CreatedUtc = DateTime.UtcNow,
            });
            db.SaveChanges();
        }

        using (var db = new ChordFlowDbContext(Options(conn)))
        {
            ProgressionEntity blues = db.Progressions.AsNoTracking().Single(p => p.Id == "12bar_blues");
            Assert.Equal("12-Bar Blues", blues.Name);
            Assert.Equal("17 17 17 17 47 47 17 17 57 47 17 57", blues.Dsl);
            Assert.Equal(Origin.Pack, blues.Origin);

            ProgressionEntity user = db.Progressions.AsNoTracking().Single(p => p.Id == "u-1");
            Assert.Equal(Origin.UserDefined, user.Origin);
        }

        // Origin is stored by NAME (like Difficulty), not as an integer.
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT Origin FROM Progressions WHERE Id = '12bar_blues'";
            Assert.Equal("Pack", (string?)cmd.ExecuteScalar());
        }
    }

    [Fact]
    public void PackProvenance_StoresOriginByName_AndRoundTripsPackId()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        using (var db = new ChordFlowDbContext(Options(conn)))
        {
            db.Database.Migrate();
            db.Progressions.Add(new ProgressionEntity
            {
                Id = "blues-shuffle",
                Name = "Blues Shuffle",
                Dsl = "17 17 47 17",
                Origin = Origin.Pack,
                PackId = "blues-essentials",
                CreatedUtc = new DateTime(2026, 6, 12, 0, 0, 0, DateTimeKind.Utc),
            });
            db.SaveChanges();
        }

        using (var db = new ChordFlowDbContext(Options(conn)))
        {
            ProgressionEntity row = db.Progressions.AsNoTracking().Single(p => p.Id == "blues-shuffle");
            Assert.Equal(Origin.Pack, row.Origin);
            Assert.Equal("blues-essentials", row.PackId);
        }

        // Origin discriminator stored by name; PackId is null for non-pack rows (the design's "discriminator + optional PackId").
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT Origin FROM Progressions WHERE Id = 'blues-shuffle'";
            Assert.Equal("Pack", (string?)cmd.ExecuteScalar());
        }
    }

    [Fact]
    public void Migration_AddsCatalogColumns_AndRoundTripsGenreSubgenreTags()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        using (var db = new ChordFlowDbContext(Options(conn)))
        {
            db.Database.Migrate();
            db.Progressions.Add(new ProgressionEntity
            {
                Id = "12bar_blues",
                Name = "12-Bar Blues",
                Dsl = "17 17 17 17 47 47 17 17 57 47 17 57",
                Origin = Origin.Pack,
                PackId = "default",
                Genre = "Blues",
                Subgenre = "Shuffle",
                Tags = CatalogHeader.SerializeTags(new[] { "12-bar", "beginner" }),
                CreatedUtc = new DateTime(2026, 6, 12, 0, 0, 0, DateTimeKind.Utc),
            });
            db.SaveChanges();
        }

        using (var db = new ChordFlowDbContext(Options(conn)))
        {
            ProgressionEntity row = db.Progressions.AsNoTracking().Single(p => p.Id == "12bar_blues");
            Assert.Equal("Blues", row.Genre);
            Assert.Equal("Shuffle", row.Subgenre);
            Assert.Equal(new[] { "12-bar", "beginner" }, CatalogHeader.DeserializeTags(row.Tags));
        }

        // Tags is a JSON-array TEXT column (C3); json_each() can index into it for SQL-side filtering.
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText =
                "SELECT COUNT(*) FROM Progressions, json_each(Progressions.Tags) WHERE json_each.value = '12-bar'";
            Assert.Equal(1L, (long?)cmd.ExecuteScalar());
        }
    }
}
