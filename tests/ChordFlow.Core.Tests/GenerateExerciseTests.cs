using ChordFlow.Exercises;
using ChordFlow.Music.Harmony;
using ChordFlow.Music.Rhythm;
using ChordFlow.Features.GenerateExercise;
using ChordFlow.Features.Packs;
using ChordFlow.Persistence;
using ChordFlow.Rendering;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The generate slice now composes an <see cref="Exercise"/> from chosen <b>content references</b> (ui/
/// exercise-workbench IN8) resolved against the stores, rather than the hard-wired seed blues. Covers the
/// progression-lift path, the stored-Song path, the optional lead, and fail-loud on a missing reference.
/// </summary>
public class GenerateExerciseTests
{
    private static (GenerateExerciseHandler handler, ChordFlowDbContext db, SqliteConnection conn) NewHandler()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<ChordFlowDbContext>().UseSqlite(conn).Options;
        var db = new ChordFlowDbContext(options);
        db.Database.Migrate();
        DefaultPack.ImportInto(db); // 12bar_blues + blues_song_demo + the three rhythms
        return (new GenerateExerciseHandler(options, new AlphaTexRenderer()), db, conn);
    }

    [Fact]
    public void Build_Progression_LiftsToSongAtChosenKey()
    {
        (GenerateExerciseHandler handler, ChordFlowDbContext db, SqliteConnection conn) = NewHandler();
        using SqliteConnection _ = conn;
        using ChordFlowDbContext __ = db;

        Exercise ex = handler.Build(
            db, "progression", "12bar_blues", "beat_1_3", leadPatternId: null,
            keyPitchClass: 10, tempo: 80, Difficulty.Beginner, TripletFeel.None);

        Assert.Equal("12bar_blues", ex.Song.Id);                 // bare progression lifted to a Song
        Assert.Equal("beat_1_3", ex.Comping.Id);
        Assert.Null(ex.Lead);
        Assert.Equal(new Key(new PitchClass(10), false), ex.KeyOverride); // Bb carried in the override
    }

    [Fact]
    public void Build_MinorKey_CarriesMinorKeyOverride() // first-class-minor-keys 8a: keyIsMinor threads into the override
    {
        (GenerateExerciseHandler handler, ChordFlowDbContext db, SqliteConnection conn) = NewHandler();
        using SqliteConnection _ = conn;
        using ChordFlowDbContext __ = db;

        Exercise ex = handler.Build(
            db, "progression", "12bar_blues", "beat_1_3", leadPatternId: null,
            keyPitchClass: 9, tempo: 80, Difficulty.Beginner, TripletFeel.None, keyIsMinor: true);

        Assert.Equal(new Key(new PitchClass(9), IsMinor: true), ex.KeyOverride); // A minor, not A major
    }

    [Fact]
    public void Build_Song_UsesStoredSong_NoOverrideWhenKeyAbsent()
    {
        (GenerateExerciseHandler handler, ChordFlowDbContext db, SqliteConnection conn) = NewHandler();
        using SqliteConnection _ = conn;
        using ChordFlowDbContext __ = db;

        Exercise ex = handler.Build(
            db, "song", "blues_song_demo", "beat_1_3", leadPatternId: "quarters",
            keyPitchClass: null, tempo: 90, Difficulty.Intermediate, TripletFeel.Triplet8th);

        Assert.Equal("blues_song_demo", ex.Song.Id);
        Assert.Equal("quarters", ex.Lead?.Id);                   // optional lead resolved
        Assert.Null(ex.KeyOverride);                             // absent key → the Song's own InitialKey
        Assert.Equal(TripletFeel.Triplet8th, ex.TripletFeel);
    }

    [Fact]
    public void Build_MissingReference_FailsLoud()
    {
        (GenerateExerciseHandler handler, ChordFlowDbContext db, SqliteConnection conn) = NewHandler();
        using SqliteConnection _ = conn;
        using ChordFlowDbContext __ = db;

        Assert.Throws<InvalidOperationException>(() => handler.Build(
            db, "progression", "does_not_exist", "beat_1_3", leadPatternId: null,
            keyPitchClass: 0, tempo: 80, Difficulty.Beginner, TripletFeel.None));
    }

    [Fact]
    public void Build_WithDrumGroove_AppendsDrumPartCarryingVolume() // drums-under-a-song IN8
    {
        (GenerateExerciseHandler handler, ChordFlowDbContext db, SqliteConnection conn) = NewHandler();
        using SqliteConnection _ = conn;
        using ChordFlowDbContext __ = db;

        Exercise ex = handler.Build(
            db, "progression", "12bar_blues", "beat_1_3", leadPatternId: null,
            keyPitchClass: 0, tempo: 80, Difficulty.Beginner, TripletFeel.None,
            drumGrooveId: "rock", drumVolume: 0.7);

        Assert.NotNull(ex.Drums);
        Assert.Equal("rock", ex.Drums!.Id);
        Assert.Equal(0.7, ((DrumPart)ex.Parts.Single(p => p is DrumPart)).Volume);
    }

    [Fact]
    public void Build_BlankDrumGroove_NoDrumPart()
    {
        (GenerateExerciseHandler handler, ChordFlowDbContext db, SqliteConnection conn) = NewHandler();
        using SqliteConnection _ = conn;
        using ChordFlowDbContext __ = db;

        Exercise ex = handler.Build(
            db, "progression", "12bar_blues", "beat_1_3", leadPatternId: null,
            keyPitchClass: 0, tempo: 80, Difficulty.Beginner, TripletFeel.None, drumGrooveId: null);

        Assert.Null(ex.Drums);
    }

    [Fact]
    public void Build_MissingDrumGroove_FailsLoud()
    {
        (GenerateExerciseHandler handler, ChordFlowDbContext db, SqliteConnection conn) = NewHandler();
        using SqliteConnection _ = conn;
        using ChordFlowDbContext __ = db;

        Assert.Throws<InvalidOperationException>(() => handler.Build(
            db, "progression", "12bar_blues", "beat_1_3", leadPatternId: null,
            keyPitchClass: 0, tempo: 80, Difficulty.Beginner, TripletFeel.None, drumGrooveId: "does_not_exist"));
    }
}
