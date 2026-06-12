using System.Linq;
using ChordFlow.Domain;
using ChordFlow.Persistence;
using ChordFlow.Persistence.Entities;
using ChordFlow.Rendering;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// Song persistence: the EF migration creates the <c>Songs</c> table; built-in seeding is idempotent and
/// denormalizes the catalog header; a seeded row round-trips DSL → parse → expand (resolving stored references
/// through <see cref="ProgressionStore"/>) → render; and the store reports hits/misses.
/// </summary>
public class SongPersistenceTests
{
    private static readonly TimeSignature Ts = TimeSignature.FourFour;

    private static DbContextOptions<ChordFlowDbContext> Options(SqliteConnection conn) =>
        new DbContextOptionsBuilder<ChordFlowDbContext>().UseSqlite(conn).Options;

    [Fact]
    public void Migration_CreatesSongsTable_AndSeedRoundTripsThroughRender()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        using (var db = new ChordFlowDbContext(Options(conn)))
        {
            db.Database.Migrate();
            db.SeedBuiltInProgressions();   // the demo song references the stored 12bar_blues
            int added = db.SeedBuiltInSongs();
            Assert.True(added >= 1);
        }

        using (var db = new ChordFlowDbContext(Options(conn)))
        {
            SongEntity row = db.Songs.AsNoTracking().Single(s => s.Id == "blues_song_demo");

            // Header was denormalized into filter columns; the DSL stays canonical (header included).
            Assert.Equal("Blues", row.Genre);
            Assert.Equal("Shuffle", row.Subgenre);
            Assert.Equal(new[] { "12-bar", "demo" }, CatalogHeader.DeserializeTags(row.Tags));
            Assert.Equal(Origin.BuiltIn, row.Origin);

            // Load path: strip header → parse → expand over the store → render — end to end, no throw.
            (_, string body) = CatalogHeader.Parse(row.Dsl);
            Song song = SongParser.Parse(row.Id, row.Name, body, Ts);
            RealizedSong realized = SongExpander.Expand(song, new ProgressionStore(db, Ts));

            // intro + verse x2 + chorus + verse = 5 sections; verse resolves to the 12-bar blues (12 bars).
            Assert.Equal(5, realized.Sections.Count);
            Assert.Contains(realized.Sections, s => s.Bars.Count == 12);

            string tex = new AlphaTexRenderer().Render(realized, SeedData.Beat1And3, 100, Difficulty.Beginner);
            Assert.Contains("\\title", tex);
            Assert.Contains("\\ks", tex);   // header key + the inline change at "mod V"
        }
    }

    [Fact]
    public void SeedBuiltInSongs_IsIdempotent()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        using var db = new ChordFlowDbContext(Options(conn));
        db.Database.Migrate();

        int first = db.SeedBuiltInSongs();
        int second = db.SeedBuiltInSongs();

        Assert.True(first >= 1);
        Assert.Equal(0, second);
        Assert.Equal(SeedData.BuiltInSongs.Count, db.Songs.Count());
    }

    [Fact]
    public void OriginStoredByName_NotInteger()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        using (var db = new ChordFlowDbContext(Options(conn)))
        {
            db.Database.Migrate();
            db.SeedBuiltInSongs();
        }

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Origin FROM Songs WHERE Id = 'blues_song_demo'";
        Assert.Equal("BuiltIn", (string?)cmd.ExecuteScalar());
    }

    [Fact]
    public void ProgressionStore_Find_HitAndMiss()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        using var db = new ChordFlowDbContext(Options(conn));
        db.Database.Migrate();
        db.SeedBuiltInProgressions();

        var store = new ProgressionStore(db, Ts);

        Progression? hit = store.Find("12bar_blues");
        Assert.NotNull(hit);
        Assert.Equal(12, hit!.Bars.Count);

        Assert.Null(store.Find("does-not-exist"));
    }
}
