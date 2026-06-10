using ChordFlow.Domain;
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
                Origin = ProgressionOrigin.BuiltIn,
                CreatedUtc = new DateTime(2026, 6, 9, 0, 0, 0, DateTimeKind.Utc),
            });
            db.Progressions.Add(new ProgressionEntity
            {
                Id = "u-1",
                Name = "My Tune",
                Dsl = "1 4 5 1",
                Origin = ProgressionOrigin.UserDefined,
                CreatedUtc = DateTime.UtcNow,
            });
            db.SaveChanges();
        }

        using (var db = new ChordFlowDbContext(Options(conn)))
        {
            ProgressionEntity blues = db.Progressions.AsNoTracking().Single(p => p.Id == "12bar_blues");
            Assert.Equal("12-Bar Blues", blues.Name);
            Assert.Equal("17 17 17 17 47 47 17 17 57 47 17 57", blues.Dsl);
            Assert.Equal(ProgressionOrigin.BuiltIn, blues.Origin);

            ProgressionEntity user = db.Progressions.AsNoTracking().Single(p => p.Id == "u-1");
            Assert.Equal(ProgressionOrigin.UserDefined, user.Origin);
        }

        // Origin is stored by NAME (like Difficulty), not as an integer.
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT Origin FROM Progressions WHERE Id = '12bar_blues'";
            Assert.Equal("BuiltIn", (string?)cmd.ExecuteScalar());
        }
    }
}
