using ChordFlow.Instruments.Drums;
using ChordFlow.Persistence;
using ChordFlow.Persistence.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// Persistence round-trip for the <c>DrumGrooves</c> table (the 5th content kind, req IN6): the EF migration
/// creates the table, and a stored row reconstructs its <see cref="DrumGroove"/> by re-parsing the canonical
/// hit-grid <c>Dsl</c> (alphaTex/lanes are never stored). Fork-on-edit, header preservation, and delete
/// mirror the progression store. In-memory SQLite kept open across contexts.
/// </summary>
public class DrumGroovePersistenceTests
{
    private const string RockDsl = "HH :2 xxxxxxxx\nSD :2 ..x...x.\nBD :2 x...x...";

    private static DbContextOptions<ChordFlowDbContext> Options(SqliteConnection conn) =>
        new DbContextOptionsBuilder<ChordFlowDbContext>().UseSqlite(conn).Options;

    private static ChordFlowDbContext Fresh(SqliteConnection conn)
    {
        var db = new ChordFlowDbContext(Options(conn));
        db.Database.Migrate();
        return db;
    }

    [Fact]
    public void Save_ThenFind_RoundTripsTheGroove()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = Fresh(conn);
        var store = new DrumGrooveStore(db);

        string id = store.Save(null, "Rock", RockDsl);
        DrumGroove? groove = store.Find(id);

        Assert.NotNull(groove);
        Assert.Equal("Rock", groove!.Name);
        Assert.Single(groove.Bars);
        Assert.Equal(
            new[] { DrumVoice.HiHatClosed, DrumVoice.Snare, DrumVoice.Kick },
            groove.DistinctVoices());
    }

    [Fact]
    public void List_AfterSave_ShowsOneUserRow()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = Fresh(conn);
        var store = new DrumGrooveStore(db);
        store.Save(null, "Rock", RockDsl);

        IReadOnlyList<ContentSummary> list = store.List();

        ContentSummary row = Assert.Single(list);
        Assert.Equal("Rock", row.Name);
        Assert.Equal(ContentSource.User, row.Source);
    }

    [Fact]
    public void Get_StripsCatalogHeader_ReturningTheBodyOnly()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = Fresh(conn);
        db.DrumGrooves.Add(new DrumGrooveEntity
        {
            Id = "rock",
            Name = "Rock",
            Dsl = "genre: Rock\ntags: [8ths]\n" + RockDsl,
            Origin = Origin.UserDefined,
            CreatedUtc = DateTime.UtcNow,
        });
        db.SaveChanges();

        ContentDoc? doc = new DrumGrooveStore(db).Get("rock");

        Assert.NotNull(doc);
        Assert.DoesNotContain("genre:", doc!.Dsl);
        Assert.StartsWith("HH", doc.Dsl.TrimStart());
    }

    [Fact]
    public void Save_InPlaceEdit_PreservesTheStoredCatalogHeader()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = Fresh(conn);
        db.DrumGrooves.Add(new DrumGrooveEntity
        {
            Id = "g1",
            Name = "Rock",
            Dsl = "genre: Rock\n" + RockDsl,
            Origin = Origin.UserDefined,
            CreatedUtc = DateTime.UtcNow,
        });
        db.SaveChanges();

        // Editing the body (metadata isn't edited here, EX3) must not destroy the genre header.
        new DrumGrooveStore(db).Save("g1", "Rock v2", "BD :2 x...x...", sourceId: "g1");

        string stored = db.DrumGrooves.AsNoTracking().Single(g => g.Id == "g1").Dsl;
        Assert.Contains("genre: Rock", stored);
    }

    [Fact]
    public void Delete_RemovesTheUserRow()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = Fresh(conn);
        var store = new DrumGrooveStore(db);
        string id = store.Save(null, "Rock", RockDsl);

        Assert.Equal(DeleteOutcome.Deleted, store.Delete(id));
        Assert.Equal(DeleteOutcome.NotFound, store.Delete(id));
        Assert.Empty(store.List());
    }

    [Fact]
    public void Save_MalformedDsl_ThrowsAndWritesNothing()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = Fresh(conn);
        var store = new DrumGrooveStore(db);

        Assert.Throws<FormatException>(() => store.Save(null, "Bad", "ZZ :2 xxxxxxxx"));
        Assert.Empty(store.List());
    }
}
