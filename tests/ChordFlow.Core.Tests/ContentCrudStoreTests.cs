using ChordFlow.Domain;
using ChordFlow.Persistence;
using ChordFlow.Persistence.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The content-CRUD write path (step 1): <see cref="IContentStore"/> List/Get/Save/Delete on all four stores,
/// the <c>(Id, Origin)</c> tier-shadowing law (edit a BuiltIn → UserDefined shadow; delete → delete vs revert),
/// voicing canonicalization-to-C on save, and validate-by-parse (invalid DSL throws and writes nothing).
/// In-memory SQLite kept open across the test; the EF migration builds the schema.
/// </summary>
public class ContentCrudStoreTests
{
    private static DbContextOptions<ChordFlowDbContext> Options(SqliteConnection conn) =>
        new DbContextOptionsBuilder<ChordFlowDbContext>().UseSqlite(conn).Options;

    private static SqliteConnection MigratedConnection()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = new ChordFlowDbContext(Options(conn));
        db.Database.Migrate();
        return conn;
    }

    // ---- Progression: full CRUD + tier-shadowing -----------------------------------------------

    [Fact]
    public void Save_NewProgression_GetsGuidId_AndIsListedAndGettable()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        var store = new ProgressionStore(db);

        string id = store.Save(id: null, name: "My Tune", dsl: "1 4 5 1");

        Assert.True(Guid.TryParse(id, out _)); // a new definition gets a GUID id
        ContentSummary summary = Assert.Single(store.List());
        Assert.Equal("My Tune", summary.Name);
        Assert.Equal(Origin.UserDefined, summary.Origin);
        Assert.False(summary.HasLowerTier);

        ContentDoc? doc = store.Get(id);
        Assert.NotNull(doc);
        Assert.Equal("1 4 5 1", doc!.Dsl);
    }

    [Fact]
    public void Save_EditingBuiltIn_WritesUserDefinedShadow_LeavingBuiltInRowIntact()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        db.Progressions.Add(new ProgressionEntity
        {
            Id = "12bar_blues",
            Name = "12-Bar Blues",
            Dsl = "17 17 17 17 47 47 17 17 57 47 17 57",
            Origin = Origin.BuiltIn,
            CreatedUtc = DateTime.UtcNow,
        });
        db.SaveChanges();
        var store = new ProgressionStore(db);

        string id = store.Save(id: "12bar_blues", name: "My Blues", dsl: "17 47 17 57");

        Assert.Equal("12bar_blues", id); // same id, new tier
        // The BuiltIn row is untouched; a UserDefined shadow now also exists (C2).
        ProgressionEntity builtIn = db.Progressions.AsNoTracking().Single(p => p.Id == "12bar_blues" && p.Origin == Origin.BuiltIn);
        Assert.Equal("17 17 17 17 47 47 17 17 57 47 17 57", builtIn.Dsl);
        ProgressionEntity shadow = db.Progressions.AsNoTracking().Single(p => p.Id == "12bar_blues" && p.Origin == Origin.UserDefined);
        Assert.Equal("17 47 17 57", shadow.Dsl);

        // The list collapses to one row — the winning UserDefined tier, with a lower tier under it.
        ContentSummary summary = Assert.Single(store.List());
        Assert.Equal(Origin.UserDefined, summary.Origin);
        Assert.True(summary.HasLowerTier);
        Assert.Equal("My Blues", store.Get("12bar_blues")!.Name);
    }

    [Fact]
    public void Delete_UserOnly_RemovesIt()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        var store = new ProgressionStore(db);
        string id = store.Save(null, "Temp", "1 4 5 1");

        DeleteOutcome outcome = store.Delete(id);

        Assert.Equal(DeleteOutcome.Deleted, outcome);
        Assert.Empty(store.List());
    }

    [Fact]
    public void Delete_ShadowOverBuiltIn_RevertsToBuiltIn()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        db.Progressions.Add(new ProgressionEntity
        {
            Id = "12bar_blues",
            Name = "12-Bar Blues",
            Dsl = "17 17 17 17 47 47 17 17 57 47 17 57",
            Origin = Origin.BuiltIn,
            CreatedUtc = DateTime.UtcNow,
        });
        db.SaveChanges();
        var store = new ProgressionStore(db);
        store.Save("12bar_blues", "My Blues", "17 47 17 57"); // shadow

        DeleteOutcome outcome = store.Delete("12bar_blues");

        Assert.Equal(DeleteOutcome.Reverted, outcome);
        ContentSummary summary = Assert.Single(store.List());
        Assert.Equal(Origin.BuiltIn, summary.Origin); // the built-in resurfaces
        Assert.False(summary.HasLowerTier);
        Assert.Equal("12-Bar Blues", summary.Name);
    }

    [Fact]
    public void Delete_MissingUserRow_IsNotFound()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        Assert.Equal(DeleteOutcome.NotFound, new ProgressionStore(db).Delete("nope"));
    }

    [Fact]
    public void Save_InvalidProgression_Throws_AndWritesNothing()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        var store = new ProgressionStore(db);

        Assert.Throws<FormatException>(() => store.Save(null, "Bad", "1 4 9")); // degree 9 outside 1..7
        Assert.Empty(store.List());
    }

    [Fact]
    public void Get_StripsCatalogHeader_FromEditableBody()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        db.Progressions.Add(new ProgressionEntity
        {
            Id = "p-hdr",
            Name = "Headered",
            Dsl = "genre: Blues\ntags: [12-bar]\n17 47 17 57",
            Origin = Origin.UserDefined,
            CreatedUtc = DateTime.UtcNow,
        });
        db.SaveChanges();

        ContentDoc? doc = new ProgressionStore(db).Get("p-hdr");
        Assert.Equal("17 47 17 57", doc!.Dsl); // header peeled off; only the body is editable (EX3)
    }

    // ---- Rhythm --------------------------------------------------------------------------------

    [Fact]
    public void Rhythm_SaveGetDelete_RoundTrips()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        var store = new RhythmPatternStore(db);

        string id = store.Save(null, "Four On The Floor", "X...X...X...X...");
        Assert.Equal("X...X...X...X...", store.Get(id)!.Dsl);
        Assert.Equal(DeleteOutcome.Deleted, store.Delete(id));
    }

    [Fact]
    public void Rhythm_SaveInvalid_Throws()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        Assert.Throws<FormatException>(() => new RhythmPatternStore(db).Save(null, "Bad", "ZZZZ"));
    }

    // ---- Voicing: canonicalization-to-C --------------------------------------------------------

    [Fact]
    public void Voicing_Save_CanonicalizesAnyAnchorToC()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        var store = new VoicingStore(db);

        // Author the open-C shape at a D anchor (frets two higher); it must store as canonical C.
        string id = store.Save(null, "Open C", "voicing Dmaj shape:C root:5 frets: x 5 4 2 3 2");

        Assert.Equal("voicing Cmaj shape:C root:5 frets: x 3 2 0 1 0", store.Get(id)!.Dsl);
        VoicingShape shape = Assert.Single(new VoicingStore(db).LoadShapes());
        Assert.Equal(Quality.Major, shape.Quality);
        Assert.Equal(CagedShape.C, shape.Shape);
    }

    [Fact]
    public void Voicing_SaveInvalid_Throws()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        Assert.Throws<FormatException>(() => new VoicingStore(db).Save(null, "Bad", "voicing Cmaj shape:Z root:5 frets: x 3 2 0 1 0"));
    }

    // ---- Song: new store + structural-error normalization --------------------------------------

    [Fact]
    public void Song_SaveGetDelete_RoundTrips()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        var store = new SongStore(db);

        string dsl = "intro = 17 47 17 17\nintro";
        string id = store.Save(null, "Demo", dsl);
        Assert.Equal(dsl, store.Get(id)!.Dsl);
        Assert.Equal(DeleteOutcome.Deleted, store.Delete(id));
    }

    [Fact]
    public void Song_SaveStructurallyInvalid_ThrowsFormatException()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        // A definition with no part play is a structural error (ArgumentException in the domain) — the
        // store normalizes it to FormatException so the CRUD parse-error surface is uniform (IN3).
        Assert.Throws<FormatException>(() => new SongStore(db).Save(null, "Empty", "verse = 1 4 5 1"));
    }
}
