using ChordFlow.Music.Harmony;
using ChordFlow.Music.Progressions;
using ChordFlow.Instruments.Guitar;
using ChordFlow.Persistence;
using ChordFlow.Persistence.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The content-CRUD write path under the multi-source model (content-source-model): <see cref="IContentStore"/>
/// List/Get/Save/Delete on all four stores, additive listing tagged by <see cref="ContentSource"/>, fork-on-edit
/// (editing a package item mints a new user copy — never a same-id shadow), delete-only-user (no revert),
/// voicing canonicalization-to-C, and validate-by-parse (invalid DSL throws and writes nothing). In-memory
/// SQLite kept open across the test; the EF migration builds the schema.
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

    // ---- Progression: full CRUD + multi-source listing ------------------------------------------

    [Fact]
    public void Save_NewProgression_GetsGuidId_AndIsListedAsUser()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        var store = new ProgressionStore(db);

        string id = store.Save(id: null, name: "My Tune", dsl: "1 4 5 1");

        Assert.True(Guid.TryParse(id, out _)); // a new definition gets a GUID id
        ContentSummary summary = Assert.Single(store.List());
        Assert.Equal("My Tune", summary.Name);
        Assert.Equal(ContentSource.User, summary.Source);
        Assert.Null(summary.PackId);

        ContentDoc? doc = store.Get(id);
        Assert.NotNull(doc);
        Assert.Equal("1 4 5 1", doc!.Dsl);
    }

    [Fact]
    public void Save_EditingPackItem_ForksANewUserCopy_LeavingPackRowIntact()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        db.Progressions.Add(new ProgressionEntity
        {
            Id = "12bar_blues",
            Name = "12-Bar Blues",
            Dsl = "17 17 17 17 47 47 17 17 57 47 17 57",
            Origin = Origin.Pack,
            PackId = "default",
            CreatedUtc = DateTime.UtcNow,
        });
        db.SaveChanges();
        var store = new ProgressionStore(db);

        string id = store.Save(id: "12bar_blues", name: "My Blues", dsl: "17 47 17 57");

        // Fork-on-edit: a fresh GUID id, NOT a same-id shadow.
        Assert.NotEqual("12bar_blues", id);
        Assert.True(Guid.TryParse(id, out _));

        // The pack row is untouched; the user copy is a separate row.
        ProgressionEntity pack = db.Progressions.AsNoTracking().Single(p => p.Id == "12bar_blues" && p.Origin == Origin.Pack);
        Assert.Equal("17 17 17 17 47 47 17 17 57 47 17 57", pack.Dsl);
        ProgressionEntity copy = db.Progressions.AsNoTracking().Single(p => p.Id == id && p.Origin == Origin.UserDefined);
        Assert.Equal("17 47 17 57", copy.Dsl);

        // No hiding: the list shows BOTH the package original and the user copy.
        IReadOnlyList<ContentSummary> list = store.List();
        Assert.Equal(2, list.Count);
        Assert.Contains(list, s => s.Source == ContentSource.Package && s.PackId == "default");
        Assert.Contains(list, s => s.Source == ContentSource.User && s.Name == "My Blues");
    }

    [Fact]
    public void Save_EditingExistingUserRow_UpdatesInPlace()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        var store = new ProgressionStore(db);
        string id = store.Save(null, "Mine", "1 4 5 1");

        string again = store.Save(id, "Mine v2", "1 5 4 1");

        Assert.Equal(id, again); // same user id, updated in place
        ContentSummary summary = Assert.Single(store.List());
        Assert.Equal("Mine v2", summary.Name);
        Assert.Equal("1 5 4 1", store.Get(id)!.Dsl);
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
    public void Delete_UserCopy_LeavesThePackRowListed()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        db.Progressions.Add(new ProgressionEntity
        {
            Id = "12bar_blues", Name = "12-Bar Blues", Dsl = "17 47 17 57",
            Origin = Origin.Pack, PackId = "default", CreatedUtc = DateTime.UtcNow,
        });
        db.SaveChanges();
        var store = new ProgressionStore(db);
        string copyId = store.Save(null, "My Blues", "17 17 47 57"); // an independent user item

        Assert.Equal(DeleteOutcome.Deleted, store.Delete(copyId));

        // The package original was never hidden, so it simply remains.
        ContentSummary summary = Assert.Single(store.List());
        Assert.Equal(ContentSource.Package, summary.Source);
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

    [Fact]
    public void Save_ForkingAMinorProgression_PreservesTonality_SoItStillRealizesMinor()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        db.Progressions.Add(new ProgressionEntity
        {
            Id = "nat_min",
            Name = "Natural Minor i-iv-v",
            Dsl = "tonality: minor\n1- 4- 5-",
            Origin = Origin.Pack,
            PackId = "default",
            CreatedUtc = DateTime.UtcNow,
        });
        db.SaveChanges();
        var store = new ProgressionStore(db);

        // The UI fork: editingId is null (a new user copy), sourceId names the shown package item.
        string id = store.Save(id: null, name: "My Minor", dsl: "1- 4- 5-", sourceId: "nat_min");

        // The fork is a fresh user row that KEEPS the source's tonality (not silently stripped to major).
        Assert.NotEqual("nat_min", id);
        ProgressionEntity copy = db.Progressions.AsNoTracking().Single(p => p.Id == id && p.Origin == Origin.UserDefined);
        Assert.Contains("tonality: minor", copy.Dsl);
        Assert.Equal(Tonality.Minor, store.Find(id)!.Home); // still realizes minor, not major
    }

    [Fact]
    public void Save_EditingAUserRowWithTonality_KeepsItOnUpdate()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        db.Progressions.Add(new ProgressionEntity
        {
            Id = "u1", Name = "Mine", Dsl = "tonality: minor\n1- 4- 5-",
            Origin = Origin.UserDefined, CreatedUtc = DateTime.UtcNow,
        });
        db.SaveChanges();
        var store = new ProgressionStore(db);

        string again = store.Save(id: "u1", name: "Mine v2", dsl: "1- 4- 6-", sourceId: "u1");

        Assert.Equal("u1", again);
        Assert.Equal(Tonality.Minor, store.Find("u1")!.Home); // tonality survived the in-place edit
    }

    [Fact]
    public void Save_WithExplicitMinorTonality_AuthorsANewMinorProgression()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        var store = new ProgressionStore(db);

        // The editor's tonality control sends "minor" on a brand-new progression (no source).
        string id = store.Save(id: null, name: "My Minor", dsl: "1- 4- 5-", sourceId: null, tonality: Tonality.Minor);

        Assert.Contains("tonality: minor", db.Progressions.AsNoTracking().Single(p => p.Id == id).Dsl);
        Assert.Equal(Tonality.Minor, store.Find(id)!.Home);
    }

    [Fact]
    public void Save_WithExplicitMajorTonality_WritesNoHeader_ForByteIdenticalMajor()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        var store = new ProgressionStore(db);

        string id = store.Save(id: null, name: "My Major", dsl: "1 4 5 1", sourceId: null, tonality: Tonality.Major);

        Assert.Equal("1 4 5 1", db.Progressions.AsNoTracking().Single(p => p.Id == id).Dsl); // no header emitted (C1)
        Assert.Equal(Tonality.Major, store.Find(id)!.Home);
    }

    [Fact]
    public void Save_ExplicitTonality_OverridesTheSourceOnFork()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        db.Progressions.Add(new ProgressionEntity { Id = "src", Name = "Src", Dsl = "tonality: minor\n1- 4- 5-", Origin = Origin.Pack, PackId = "default", CreatedUtc = DateTime.UtcNow });
        db.SaveChanges();
        var store = new ProgressionStore(db);

        // A minor source, but the editor flips the control to major on save → the fork is major (explicit wins).
        string id = store.Save(id: null, name: "Flipped", dsl: "1 4 5", sourceId: "src", tonality: Tonality.Major);

        Assert.Equal(Tonality.Major, store.Find(id)!.Home);
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

    // ---- Song: list surfaces InitialKey so the Practice key picker can seed from it (play-ui-key-init IN1) ----

    [Fact]
    public void SongList_SurfacesInitialKey_FromTheSongsOwnKey()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        db.Songs.Add(new SongEntity { Id = "in_f", Name = "In F", Dsl = "head = 1 4 5 1\nkey F\nhead", Origin = Origin.Pack, PackId = "default", CreatedUtc = DateTime.UtcNow });
        db.Songs.Add(new SongEntity { Id = "no_key", Name = "No Key", Dsl = "head = 1 4 5 1\nhead", Origin = Origin.Pack, PackId = "default", CreatedUtc = DateTime.UtcNow });
        db.SaveChanges();

        var byId = new SongStore(db).List().ToDictionary(s => s.Id);

        Assert.Equal(5, byId["in_f"].InitialKey);   // explicit "key F" → tonic pitch class 5
        Assert.Equal(0, byId["no_key"].InitialKey);  // no key line → the Song DSL C default
    }

    // ---- Song: list surfaces DefaultFeel so the transport feel control can seed from it (song-default-feel IN4) ----

    [Fact]
    public void SongList_SurfacesDefaultFeel_FromTheSongsOwnFeelDirective()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        db.Songs.Add(new SongEntity { Id = "swing", Name = "Swing", Dsl = "feel triplet8th\nhead = 1 4 5 1\nhead", Origin = Origin.Pack, PackId = "default", CreatedUtc = DateTime.UtcNow });
        db.Songs.Add(new SongEntity { Id = "straight", Name = "Straight", Dsl = "feel none\nhead = 1 4 5 1\nhead", Origin = Origin.Pack, PackId = "default", CreatedUtc = DateTime.UtcNow });
        db.Songs.Add(new SongEntity { Id = "no_feel", Name = "No Feel", Dsl = "head = 1 4 5 1\nhead", Origin = Origin.Pack, PackId = "default", CreatedUtc = DateTime.UtcNow });
        db.SaveChanges();

        var byId = new SongStore(db).List().ToDictionary(s => s.Id);

        Assert.Equal("Triplet8th", byId["swing"].DefaultFeel);   // `feel triplet8th` → the enum-name ident the UI seeds with
        Assert.Equal("None", byId["straight"].DefaultFeel);      // explicit `feel none` → "None" (a straight tune), not null (IN7)
        Assert.Null(byId["no_feel"].DefaultFeel);                // no directive → null (no opinion)
    }

    // ---- Song: list surfaces DefaultTempo so the transport tempo control can seed from it (scorer-render-params IN1) ----

    [Fact]
    public void SongList_SurfacesDefaultTempo_FromTheSongsOwnTempoDirective()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        db.Songs.Add(new SongEntity { Id = "fast", Name = "Fast", Dsl = "tempo 132\nhead = 1 4 5 1\nhead", Origin = Origin.Pack, PackId = "default", CreatedUtc = DateTime.UtcNow });
        db.Songs.Add(new SongEntity { Id = "no_tempo", Name = "No Tempo", Dsl = "head = 1 4 5 1\nhead", Origin = Origin.Pack, PackId = "default", CreatedUtc = DateTime.UtcNow });
        db.SaveChanges();

        var byId = new SongStore(db).List().ToDictionary(s => s.Id);

        Assert.Equal(132, byId["fast"].DefaultTempo);   // `tempo 132` → the BPM the tempo control seeds with
        Assert.Null(byId["no_tempo"].DefaultTempo);      // no directive → null (the 80 default applies downstream)
    }

    [Fact]
    public void ProgressionList_HasNullInitialKey_BecauseProgressionsAreKeyIndependent()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        var store = new ProgressionStore(db);
        store.Save(id: null, name: "Tune", dsl: "1 4 5 1");

        Assert.Null(Assert.Single(store.List()).InitialKey);
    }

    // ---- List surfaces InitialKeyIsMinor so the harmony controls auto-pick minor (minor-mode-ui-threading IN4) ----

    [Fact]
    public void ProgressionList_SurfacesInitialKeyIsMinor_FromTheTonalityHeader()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        db.Progressions.Add(new ProgressionEntity { Id = "min", Name = "Min", Dsl = "tonality: minor\n1- 4- 5-", Origin = Origin.Pack, PackId = "default", CreatedUtc = DateTime.UtcNow });
        db.Progressions.Add(new ProgressionEntity { Id = "maj", Name = "Maj", Dsl = "1 4 5 1", Origin = Origin.Pack, PackId = "default", CreatedUtc = DateTime.UtcNow });
        db.SaveChanges();

        var byId = new ProgressionStore(db).List().ToDictionary(s => s.Id);

        Assert.True(byId["min"].InitialKeyIsMinor);   // `tonality: minor` header → the control seeds minor
        Assert.False(byId["maj"].InitialKeyIsMinor);  // no header → major
    }

    // ---- List surfaces genre/subgenre/tags for the list fields + shared FilterR (filter-toggle-buttons IN1) ----

    [Fact]
    public void ProgressionList_SurfacesGenreSubgenreTags_FromTheCatalogHeader()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        db.Progressions.Add(new ProgressionEntity
        {
            Id = "blues", Name = "12-Bar Blues",
            Dsl = "genre: Blues\nsubgenre: Shuffle\ntags: [12-bar, beginner]\n17 47 17 57",
            Origin = Origin.Pack, PackId = "default", CreatedUtc = DateTime.UtcNow,
        });
        db.Progressions.Add(new ProgressionEntity
        {
            Id = "bare", Name = "Bare", Dsl = "1 4 5 1", Origin = Origin.Pack, PackId = "default", CreatedUtc = DateTime.UtcNow,
        });
        db.SaveChanges();

        var byId = new ProgressionStore(db).List().ToDictionary(s => s.Id);

        Assert.Equal("Blues", byId["blues"].Genre);
        Assert.Equal("Shuffle", byId["blues"].Subgenre);
        Assert.Equal(new[] { "12-bar", "beginner" }, byId["blues"].Tags);
        Assert.Null(byId["bare"].Genre);          // no header → no metadata
        Assert.Null(byId["bare"].Subgenre);
        Assert.Empty(byId["bare"].Tags!);
    }

    [Fact]
    public void RhythmList_CarriesNoCatalogMetadata_BecauseRhythmsAreNotGenreFiltered()
    {
        // EX3: rhythm patterns carry no genre/subgenre/tags — the Rhythms tab's FilterR shows only Source.
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        var store = new RhythmPatternStore(db);
        store.Save(null, "Four On The Floor", "X...X...X...X...");

        ContentSummary summary = Assert.Single(store.List());
        Assert.Null(summary.Genre);
        Assert.Null(summary.Subgenre);
        Assert.Empty(summary.Tags!);
    }

    [Fact]
    public void SongList_SurfacesInitialKeyIsMinor_FromTheSongsOwnKeyMode()
    {
        using var conn = MigratedConnection();
        using var db = new ChordFlowDbContext(Options(conn));
        db.Songs.Add(new SongEntity { Id = "am", Name = "In Am", Dsl = "head = 1 4 5 1\nkey Am\nhead", Origin = Origin.Pack, PackId = "default", CreatedUtc = DateTime.UtcNow });
        db.Songs.Add(new SongEntity { Id = "cmaj", Name = "In C", Dsl = "head = 1 4 5 1\nkey C\nhead", Origin = Origin.Pack, PackId = "default", CreatedUtc = DateTime.UtcNow });
        db.SaveChanges();

        var byId = new SongStore(db).List().ToDictionary(s => s.Id);

        Assert.True(byId["am"].InitialKeyIsMinor);     // `key Am` → minor mode carried to the list
        Assert.False(byId["cmaj"].InitialKeyIsMinor);  // `key C` → major
    }
}
