using ChordFlow.Music.Songs;
using ChordFlow.Music.Rhythm;
using ChordFlow.Features.Packs;
using ChordFlow.Persistence;
using ChordFlow.Persistence.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// Idempotent pack import (IN5, IN8, C2): every definition is stamped <see cref="Origin.Pack"/> with the
/// manifest id as its PackId (content-source-model — there is no BuiltIn tier), upsert by the composite
/// (Id, Origin) key, non-destructive tier coexistence (defensive single-item resolve), and fail-loud
/// references (IN8).
/// </summary>
public class PackImportTests
{
    private static readonly TimeSignature Ts = TimeSignature.FourFour;

    private static DbContextOptions<ChordFlowDbContext> Options(SqliteConnection conn) =>
        new DbContextOptionsBuilder<ChordFlowDbContext>().UseSqlite(conn).Options;

    private static ContentPack Pack(string id, params PackDefinition[] defs) =>
        new(new PackManifest(id, id, "1.0.0", "content", "ChordFlow", Array.Empty<string>()), defs);

    private static PackDefinition Prog(string id, string name, string dsl) => new(ContentKind.Progression, id, name, dsl);
    private static PackDefinition Rhythm(string id, string name, string dsl) => new(ContentKind.Rhythm, id, name, dsl);
    private static PackDefinition SongDef(string id, string name, string dsl) => new(ContentKind.Song, id, name, dsl);

    [Fact]
    public void Import_StampsPackOriginAndPackId_DenormalizesCatalog()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = new ChordFlowDbContext(Options(conn));
        db.Database.Migrate();

        ContentPack pack = Pack("blues-essentials",
            Prog("shuffle_blues", "Shuffle Blues", "genre: Blues\ntags: [12-bar]\n17 17 17 17 47 47 17 17 57 47 17 57"),
            Rhythm("driving", "Driving", "X...X...X...X..."));

        int count = new PackImporter(db).Import(pack);

        Assert.Equal(2, count);
        ProgressionEntity prog = db.Progressions.AsNoTracking().Single(p => p.Id == "shuffle_blues");
        Assert.Equal(Origin.Pack, prog.Origin);
        Assert.Equal("blues-essentials", prog.PackId);
        Assert.Equal("Shuffle Blues", prog.Name);
        Assert.Equal("Blues", prog.Genre);
        Assert.Equal(new[] { "12-bar" }, CatalogHeader.DeserializeTags(prog.Tags));
        Assert.Contains("genre: Blues", prog.Dsl);   // canonical header retained in the stored DSL

        RhythmPatternEntity rhythm = db.RhythmPatterns.AsNoTracking().Single(r => r.Id == "driving");
        Assert.Equal(Origin.Pack, rhythm.Origin);
        Assert.Equal("blues-essentials", rhythm.PackId);
    }

    [Fact]
    public void Import_IsIdempotent_UpsertsBySameTier_NoDuplicates()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = new ChordFlowDbContext(Options(conn));
        db.Database.Migrate();

        var importer = new PackImporter(db);
        importer.Import(Pack("p", Prog("blues", "First Name", "1 4 5 1")));
        // Re-import the same id with an updated name/dsl: replaces the same-tier row, no duplicate.
        int second = importer.Import(Pack("p", Prog("blues", "Second Name", "1 1 1 1")));

        Assert.Equal(1, second);
        ProgressionEntity row = db.Progressions.AsNoTracking().Single(p => p.Id == "blues");
        Assert.Equal("Second Name", row.Name);
        Assert.Equal("1 1 1 1", row.Dsl);
        Assert.Single(db.Progressions.AsNoTracking());
    }

    [Fact]
    public void PackAndUser_Coexist_AndResolveHighest_NonDestructively()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = new ChordFlowDbContext(Options(conn));
        db.Database.Migrate();

        // Pack (via importer) + a UserDefined row sharing the id — both coexist; the single-item resolve picks user.
        new PackImporter(db).Import(Pack("pk", Prog("blues", "Pack", "4 4 4 4")));
        db.Progressions.Add(new ProgressionEntity { Id = "blues", Name = "Local", Dsl = "5 5 5 5", Origin = Origin.UserDefined, CreatedUtc = DateTime.UtcNow });
        db.SaveChanges();

        Assert.Equal(2, db.Progressions.AsNoTracking().Count(p => p.Id == "blues"));

        var store = new ProgressionStore(db, Ts);
        Assert.Equal("Local", store.Find("blues")!.Name);   // UserDefined wins

        // Remove the local edit → the pack copy is still on disk and wins (non-destructive).
        db.Progressions.Remove(db.Progressions.Single(p => p.Id == "blues" && p.Origin == Origin.UserDefined));
        db.SaveChanges();
        Assert.Equal("Pack", store.Find("blues")!.Name);
    }

    [Fact]
    public void Import_Rhythm_RoundTripsThroughStore()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = new ChordFlowDbContext(Options(conn));
        db.Database.Migrate();

        new PackImporter(db).Import(Pack("p", Rhythm("driving", "Driving", "X...X...X...X...")));

        RhythmPattern? pattern = new RhythmPatternStore(db).Find("driving");
        Assert.NotNull(pattern);
        Assert.Equal("Driving", pattern!.Name);
    }

    [Fact]
    public void Import_Song_MissingProgressionRef_FailsLoudAtRealize()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = new ChordFlowDbContext(Options(conn));
        db.Database.Migrate();

        // A song that references a progression present in no tier of any pack.
        new PackImporter(db).Import(
            Pack("orphan", SongDef("orphan_song", "Orphan", "verse: nonexistent_prog\nverse")));

        SongEntity row = db.Songs.AsNoTracking().Single();
        (_, string body) = CatalogHeader.Parse(row.Dsl);
        Song song = SongParser.Parse(row.Id, row.Name, body, Ts);

        // Resolve-time fail-loud (IN8) — same rule as any Song→Progression reference, not swallowed by import.
        Assert.Throws<InvalidOperationException>(() => SongExpander.Expand(song, new ProgressionStore(db, Ts)));
    }
}
