using ChordFlow.Domain;
using ChordFlow.Persistence;
using ChordFlow.Persistence.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// Persistence round-trip for the <c>RhythmPatterns</c> table: the EF migration creates the table, and a
/// stored row reconstructs its <see cref="RhythmPattern"/> by re-parsing the canonical <c>Dsl</c> (C1 —
/// the tick grid is never stored). Uses an in-memory SQLite connection kept open across contexts.
/// </summary>
public class RhythmPatternPersistenceTests
{
    private static DbContextOptions<ChordFlowDbContext> Options(SqliteConnection conn) =>
        new DbContextOptionsBuilder<ChordFlowDbContext>().UseSqlite(conn).Options;

    private static (int Pos, int Len) PL(RhythmEvent e) => (e.Position, e.Length);

    [Fact]
    public void Find_SeededBeat1_ReconstructsTheWholeBarRing()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        using var db = new ChordFlowDbContext(Options(conn));
        db.Database.Migrate();
        db.SeedBuiltInRhythmPatterns();

        RhythmPattern? beat1 = new RhythmPatternStore(db).Find("beat_1");

        Assert.NotNull(beat1);
        Assert.Equal("Beat 1", beat1!.Name);
        Assert.Single(beat1.Bars);
        // The sustain-literal DSL "X..............." rings the whole bar.
        Assert.Equal(new[] { (0, 192) }, beat1.Bars[0].Events.Select(PL));
    }

    [Fact]
    public void Find_StoredMultiBarPickupDsl_RoundTripsBarsAndPickup()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        using (var db = new ChordFlowDbContext(Options(conn)))
        {
            db.Database.Migrate();
            db.RhythmPatterns.Add(new RhythmPatternEntity
            {
                Id = "u-1",
                Name = "My Groove",
                Dsl = "PICKUP: ...........X | X...X...X...X... | X.......X.......",
                Origin = Origin.UserDefined,
                CreatedUtc = DateTime.UtcNow,
            });
            db.SaveChanges();
        }

        using (var db = new ChordFlowDbContext(Options(conn)))
        {
            RhythmPattern? p = new RhythmPatternStore(db).Find("u-1");

            Assert.NotNull(p);
            Assert.Equal(2, p!.Bars.Count);
            Assert.NotNull(p.Pickup);
            Assert.Equal(144, p.Pickup!.LengthTicks);
            Assert.Equal(new[] { (0, 48), (48, 48), (96, 48), (144, 48) }, p.Bars[0].Events.Select(PL));
            Assert.Equal(new[] { (0, 96), (96, 96) }, p.Bars[1].Events.Select(PL));
        }
    }

    [Fact]
    public void Find_MissingId_ReturnsNull()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        using var db = new ChordFlowDbContext(Options(conn));
        db.Database.Migrate();

        Assert.Null(new RhythmPatternStore(db).Find("nope"));
    }
}
