using ChordFlow.Music.Progressions;
using ChordFlow.Music.Songs;
using ChordFlow.Exercises;
using ChordFlow.Music.Harmony;
using ChordFlow.Music.Rhythm;
using ChordFlow.Features.ExerciseLibrary;
using ChordFlow.Features.Packs;
using ChordFlow.Persistence;
using ChordFlow.Rendering;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The refactored <c>ExerciseEntity</c> (references + param columns, IN4): a saved exercise round-trips its
/// Song/comping/lead references, the practice key (carried in the <c>KeyOverride</c> token), and <c>Feel</c>
/// through the new schema + migration.
/// </summary>
public class ExerciseLibraryTests
{
    private static (ExerciseLibraryHandler handler, SqliteConnection conn) NewHandler()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<ChordFlowDbContext>().UseSqlite(conn).Options;
        using (var db = new ChordFlowDbContext(options))
        {
            db.Database.Migrate(); // runs the full migration chain incl. RefactorExerciseToSongRefs
            // Load now resolves references against the live stores (no more hard-wired seed blues), so the
            // built-in content (12bar_blues + the three rhythms) must exist for a round-trip to resolve.
            DefaultPack.ImportInto(db);
        }

        return (new ExerciseLibraryHandler(options, new AlphaTexRenderer()), conn);
    }

    private static Exercise BbBlues(Key key, RhythmPattern? lead = null) =>
        new(Song.OfProgression(SeedData.TwelveBarBlues, key), SeedData.Beat1And3, lead,
            KeyOverride: key, Tempo: 80, Difficulty: Difficulty.Beginner, Feel: Feel.Straight);

    [Fact]
    public void SaveThenLoad_RoundTripsKeyCompingFeelAndLead()
    {
        (ExerciseLibraryHandler handler, SqliteConnection conn) = NewHandler();
        using SqliteConnection _ = conn;

        var bb = new Key(new PitchClass(10), false); // Bb major
        int id = handler.Save(BbBlues(bb, lead: SeedData.Quarters));

        LoadedExercise? loaded = handler.Load(id);
        Assert.NotNull(loaded);
        Exercise ex = loaded!.Exercise;

        Assert.Equal(bb, ex.KeyOverride);                       // practice key survived via the token
        Assert.Equal(SeedData.TwelveBarBlues.Id, ex.Song.Id);
        Assert.Equal(SeedData.Beat1And3.Id, ex.Comping.Id);
        Assert.Equal(SeedData.Quarters.Id, ex.Lead?.Id);        // lead reference round-tripped
        Assert.Equal(Feel.Straight, ex.Feel);
        Assert.Contains("\\track \"Lead\"", loaded.Score.Tex);  // two-track score (lead present)
    }

    [Fact]
    public void SaveThenLoad_NoLead_IsSingleTrack()
    {
        (ExerciseLibraryHandler handler, SqliteConnection conn) = NewHandler();
        using SqliteConnection _ = conn;

        int id = handler.Save(BbBlues(new Key(new PitchClass(10), false)));

        LoadedExercise? loaded = handler.Load(id);
        Assert.NotNull(loaded);
        Assert.Null(loaded!.Exercise.Lead);
        Assert.DoesNotContain("\\track", loaded.Score.Tex);
    }

    [Fact]
    public void List_ProjectsTheNewReferenceColumns()
    {
        (ExerciseLibraryHandler handler, SqliteConnection conn) = NewHandler();
        using SqliteConnection _ = conn;

        handler.Save(BbBlues(new Key(new PitchClass(7), false))); // G major

        ExerciseListEnvelope list = handler.List();
        ExerciseSummary s = Assert.Single(list.Exercises);
        Assert.Equal(SeedData.TwelveBarBlues.Id, s.SongId);
        Assert.Equal(SeedData.Beat1And3.Id, s.CompingPatternId);
        Assert.Null(s.LeadPatternId);
        Assert.Equal("g", s.KeyOverride);
        Assert.Equal("Straight", s.Feel);
    }
}
