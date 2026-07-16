using ChordFlow.Exercises;
using ChordFlow.Features.GenerateExercise;
using ChordFlow.Features.Packs;
using ChordFlow.Music.Rhythm;
using ChordFlow.Persistence;
using ChordFlow.Persistence.Entities;
using ChordFlow.Rendering;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The unified generate/loadExercise reply (harmony-controls-r IN3/IN10): one <c>loadScore</c> envelope carries
/// BOTH projections of one Exercise realization pass — the score (tex + chord schedule) and the chord sheet
/// (model + playback cellSchedule) — with the sheet's tone/diagram data always resolved, so the Sheet view's
/// Below-cell adornment is a pure display toggle.
/// </summary>
public class ExerciseProjectionsTests
{
    private static (GenerateExerciseHandler handler, SqliteConnection conn) NewHandler()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<ChordFlowDbContext>().UseSqlite(conn).Options;
        using (var db = new ChordFlowDbContext(options))
        {
            db.Database.Migrate();
            DefaultPack.ImportInto(db); // 12bar_blues + blues_song_demo + the three rhythms
        }
        return (new GenerateExerciseHandler(options, new AlphaTexRenderer()), conn);
    }

    private static LoadScoreEnvelope Generate(GenerateExerciseHandler handler, int? keyPitchClass = 0) =>
        handler.Generate(
            "progression", "12bar_blues", "beat_1_3", leadPatternId: null,
            keyPitchClass, tempo: 80, Difficulty.Beginner, TripletFeel.None);

    [Fact]
    public void Generate_CarriesBothProjections_OfOnePass()
    {
        (GenerateExerciseHandler handler, SqliteConnection conn) = NewHandler();
        using SqliteConnection _ = conn;

        LoadScoreEnvelope reply = Generate(handler);

        Assert.Equal("loadScore", reply.Type);
        Assert.False(string.IsNullOrWhiteSpace(reply.Tex));       // the score projection
        Assert.NotNull(reply.Sheet);                              // the sheet projection rides the same reply
        Assert.NotEmpty(reply.Schedule);                          // now/next feed
        Assert.NotEmpty(reply.CellSchedule);                      // sheet marker feed
        // A downbeat entry (bar-level highlight) for every one of the 12 blues bars.
        Assert.Equal(
            Enumerable.Range(0, 12),
            reply.CellSchedule.Where(e => e.Beat == 0).Select(e => e.Bar).Distinct().OrderBy(b => b));
    }

    [Fact]
    public void Generate_SheetAlwaysCarriesToneAndDiagramData()
    {
        (GenerateExerciseHandler handler, SqliteConnection conn) = NewHandler();
        using SqliteConnection _ = conn;

        // Default render options — no adornment concept on the request at all (IN10): the sheet model still
        // resolves every chord's tones + comped diagram, so Below cell flips client-side without a re-request.
        LoadScoreEnvelope reply = Generate(handler);

        var chords = reply.Sheet.Sections
            .SelectMany(s => s.Rows).SelectMany(r => r.Cells).SelectMany(c => c.Chords).ToList();
        Assert.NotEmpty(chords);
        Assert.All(chords, chord => Assert.NotNull(chord.Diagram));
        Assert.All(chords, chord => Assert.NotEmpty(chord.Tones));
    }

    [Fact]
    public void Generate_SplitBar_GetsSubChordOnsetEntry()
    {
        // Ported from the retired ChordSheetHandlerTests: a split bar (two chords in one bar) must yield the
        // downbeat entry (segment 0) PLUS a mid-bar onset entry (segment 1, beat > 0) in the cellSchedule.
        (GenerateExerciseHandler handler, SqliteConnection conn) = NewHandler();
        using SqliteConnection _ = conn;
        SeedProgression(conn, "split", "17_47");

        LoadScoreEnvelope reply = handler.Generate(
            "progression", "split", "quarters", leadPatternId: null,
            keyPitchClass: 0, tempo: 80, Difficulty.Beginner, TripletFeel.None);

        Assert.Contains(reply.CellSchedule, e => e is { Bar: 0, Beat: 0, Chord: 0 });
        Assert.Contains(reply.CellSchedule, e => e.Bar == 0 && e.Chord == 1 && e.Beat > 0);
        // And the now/next feed carries both onsets of the split bar as separate chord changes.
        Assert.Equal(new[] { "C7", "F7" }, reply.Schedule.Select(c => c.Name));
    }

    private static void SeedProgression(SqliteConnection conn, string id, string dsl)
    {
        var options = new DbContextOptionsBuilder<ChordFlowDbContext>().UseSqlite(conn).Options;
        using var db = new ChordFlowDbContext(options);
        db.Progressions.Add(new ProgressionEntity
        {
            Id = id, Name = id, Dsl = dsl, Origin = Origin.UserDefined, CreatedUtc = DateTime.UtcNow,
        });
        db.SaveChanges();
    }

    [Fact]
    public void Generate_KeyOverride_ReachesBothProjections()
    {
        (GenerateExerciseHandler handler, SqliteConnection conn) = NewHandler();
        using SqliteConnection _ = conn;

        LoadScoreEnvelope reply = Generate(handler, keyPitchClass: 10); // Bb

        Assert.Equal(10, reply.Key);                              // the seed the Key control shows
        Assert.Equal("Bb", reply.Sheet.Header.KeyName);           // the sheet header realized in the same key
        Assert.StartsWith("Bb", reply.Schedule[0].Name);          // and the comped schedule sounds in it too
    }
}
