using ChordFlow.Exercises;
using ChordFlow.Music.Harmony;
using ChordFlow.Instruments.Guitar;
using ChordFlow.Persistence;
using ChordFlow.Persistence.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// End-to-end stored-first wiring: a voicing persisted to SQLite is loaded by <see cref="VoicingStore"/>,
/// fed to a <see cref="VoicingBook"/>, and <b>shadows</b> the generated strategy shape — the real
/// repository → book path the renderer uses (req IN4, C2).
/// </summary>
public class VoicingBookIntegrationTests
{
    private static DbContextOptions<ChordFlowDbContext> Options(SqliteConnection conn) =>
        new DbContextOptionsBuilder<ChordFlowDbContext>().UseSqlite(conn).Options;

    [Fact]
    public void StoredVoicing_LoadedFromDb_ShadowsTheGeneratedShape()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        // A stored C7 voicing (six-string E-shape) — different from the three-note generated shell.
        using (var db = new ChordFlowDbContext(Options(conn)))
        {
            db.Database.Migrate();
            db.Voicings.Add(new VoicingEntity
            {
                Id = "u-c7",
                Name = "E-shape C7",
                Dsl = "voicing C7 shape:E root:6 frets: 8 10 8 9 8 8",
                Origin = Origin.UserDefined,
                CreatedUtc = DateTime.UtcNow,
            });
            db.SaveChanges();
        }

        var chord = new Chord(new PitchClass(0), Quality.Dominant7);

        using (var db = new ChordFlowDbContext(Options(conn)))
        {
            var book = new VoicingBook(new VoicingStore(db).LoadShapes());

            Voicing played = book.Lookup(chord, Difficulty.Beginner);
            Voicing generated = new VoicingBook(Array.Empty<VoicingShape>()).Lookup(chord, Difficulty.Beginner);

            // The stored voicing is used, not the 3-note shell.
            Assert.Equal(6, played.Positions.Count);
            Assert.Equal(3, generated.Positions.Count);
            Assert.Single(book.Candidates(chord, Difficulty.Beginner));
        }
    }

    [Fact]
    public void NoStoredVoicing_FallsBackToTheGeneratedShape()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        using var db = new ChordFlowDbContext(Options(conn));
        db.Database.Migrate();

        var book = new VoicingBook(new VoicingStore(db).LoadShapes());
        var chord = new Chord(new PitchClass(10), Quality.Dominant7); // Bb7

        Voicing played = book.Lookup(chord, Difficulty.Beginner);

        Assert.Equal(3, played.Positions.Count); // the generated shell
        Assert.Empty(book.Candidates(chord, Difficulty.Beginner));
    }
}
