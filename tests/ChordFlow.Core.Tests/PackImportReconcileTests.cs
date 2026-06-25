using ChordFlow.Features.Packs;
using ChordFlow.Persistence;
using ChordFlow.Persistence.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// Pack import reconciliation (engine-derived-as-app-source IN12): a pack is authoritative for its own
/// content, so re-importing it drops the `Origin.Pack` rows it previously shipped but no longer does — the
/// fix for the stale voicings the dogfood surfaced after the 36 grips were relocated out of the default pack.
/// </summary>
public class PackImportReconcileTests
{
    private static ContentPack Pack(string id, params PackDefinition[] defs) =>
        new(new PackManifest(id, id, "1.0.0", PackManifest.ContentKindLabel, "", Array.Empty<string>()), defs);

    private static PackDefinition Voicing(string id) =>
        new(ContentKind.Voicing, id, id, "voicing C7 shape:E root:6 frets: 8 10 8 9 8 8");

    private static ChordFlowDbContext NewDb()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var db = new ChordFlowDbContext(new DbContextOptionsBuilder<ChordFlowDbContext>().UseSqlite(conn).Options);
        db.Database.Migrate();
        return db;
    }

    [Fact]
    public void Reimport_WithoutAVoicing_DropsTheOrphanedPackRow()
    {
        using var db = NewDb();
        var importer = new PackImporter(db);

        importer.Import(Pack("default", Voicing("dom7_eshape")));
        Assert.Equal(1, db.Voicings.AsNoTracking().Count());

        importer.Import(Pack("default")); // the pack now ships no voicings
        Assert.Equal(0, db.Voicings.AsNoTracking().Count());
    }

    [Fact]
    public void Reconcile_LeavesUserCopiesUntouched()
    {
        using var db = NewDb();
        var importer = new PackImporter(db);

        importer.Import(Pack("default", Voicing("dom7_eshape")));
        db.Voicings.Add(new VoicingEntity
        {
            Id = "my-c7",
            Name = "My C7",
            Dsl = "voicing C7 shape:E root:6 frets: 8 10 8 9 8 8",
            Origin = Origin.UserDefined,
            Tags = "[]",
            CreatedUtc = DateTime.UtcNow,
        });
        db.SaveChanges();

        importer.Import(Pack("default")); // drops the pack voicing, keeps the user copy

        Assert.False(db.Voicings.AsNoTracking().Any(v => v.Origin == Origin.Pack));
        Assert.True(db.Voicings.AsNoTracking().Any(v => v.Id == "my-c7" && v.Origin == Origin.UserDefined));
    }

    [Fact]
    public void Reconcile_KeepsCurrentlyShippedRows()
    {
        using var db = NewDb();
        var importer = new PackImporter(db);

        importer.Import(Pack("default", Voicing("dom7_eshape")));
        importer.Import(Pack("default", Voicing("dom7_eshape"))); // idempotent re-import
        Assert.Equal(1, db.Voicings.AsNoTracking().Count());
    }
}
