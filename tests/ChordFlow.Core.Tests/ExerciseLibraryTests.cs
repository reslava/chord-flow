using ChordFlow.Music.Progressions;
using ChordFlow.Music.Songs;
using ChordFlow.Exercises;
using ChordFlow.Instruments.Drums;
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
/// Song/comping/lead references, the practice key (carried in the <c>KeyOverride</c> token), and <c>TripletFeel</c>
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
            KeyOverride: key, Tempo: 80, Difficulty: Difficulty.Beginner, TripletFeel: TripletFeel.None);

    // A groove reference whose id is the default-pack "rock" groove — Save persists only the id, and Load
    // resolves the real groove from the store, so this stub just carries the id.
    private static DrumGroove RockGrooveRef() => DrumGroove.SingleBar(
        "rock", "Rock",
        new[] { new DrumLane(DrumVoice.Kick, new[] { RhythmEvent.Hit(0, 48) }) },
        TimeSignature.FourFour);

    private static Exercise BbBluesWithDrums(Key key, double volume) =>
        new(Song.OfProgression(SeedData.TwelveBarBlues, key),
            new InstrumentPart[] { new CompingPart(SeedData.Beat1And3), new DrumPart(RockGrooveRef()) { Volume = volume } },
            KeyOverride: key, Tempo: 80, Difficulty.Beginner, TripletFeel.None);

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
        Assert.Equal(TripletFeel.None, ex.TripletFeel);
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
    public void SaveThenLoad_RoundTripsDrumGrooveAndVolume() // drums-under-a-song IN7/IN8
    {
        (ExerciseLibraryHandler handler, SqliteConnection conn) = NewHandler();
        using SqliteConnection _ = conn;

        var bb = new Key(new PitchClass(10), false);
        int id = handler.Save(BbBluesWithDrums(bb, volume: 0.6));

        LoadedExercise? loaded = handler.Load(id);
        Assert.NotNull(loaded);
        Exercise ex = loaded!.Exercise;

        Assert.NotNull(ex.Drums);
        Assert.Equal("rock", ex.Drums!.Id);                       // groove resolved from the store on reload
        Assert.Equal(0.6, ((DrumPart)ex.Parts.Single(p => p is DrumPart)).Volume); // saved mix restored
        Assert.Contains("\\track \"Drums\"", loaded.Score.Tex);   // reloaded score has the percussion staff
    }

    [Fact]
    public void SaveThenLoad_NoDrums_HasNoDrumPart()
    {
        (ExerciseLibraryHandler handler, SqliteConnection conn) = NewHandler();
        using SqliteConnection _ = conn;

        int id = handler.Save(BbBlues(new Key(new PitchClass(10), false)));

        LoadedExercise? loaded = handler.Load(id);
        Assert.NotNull(loaded);
        Assert.Null(loaded!.Exercise.Drums);
        Assert.DoesNotContain("\\instrument percussion", loaded.Score.Tex);
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
        Assert.Equal("None", s.TripletFeel);
    }

    // ---- Minor mode threads through the loadExercise reply + re-key (minor-mode-ui-threading IN5) ----

    [Fact]
    public void LoadScore_CarriesKeyIsMinor_ForASavedMinorExercise()
    {
        (ExerciseLibraryHandler handler, SqliteConnection conn) = NewHandler();
        using SqliteConnection _ = conn;

        int id = handler.Save(BbBlues(new Key(new PitchClass(9), IsMinor: true))); // A minor

        LoadedExercise? loaded = handler.Load(id);
        Assert.NotNull(loaded);
        Assert.True(loaded!.Score.KeyIsMinor);  // the reply carries the mode → app.js seeds hc.seedKeyMode
        Assert.Equal(9, loaded.Score.Key);
    }

    [Fact]
    public void ReKey_PreservesTheModeFromTheRequest_NotHardWiredMajor()
    {
        (ExerciseLibraryHandler handler, SqliteConnection conn) = NewHandler();
        using SqliteConnection _ = conn;

        int id = handler.Save(BbBlues(new Key(new PitchClass(0), IsMinor: false))); // saved C major

        // Re-key to E with the mode toggle set to minor → the reloaded exercise is E minor, not major.
        LoadedExercise? loaded = handler.Load(id, keyOverride: 4, keyIsMinor: true);
        Assert.NotNull(loaded);
        Assert.True(loaded!.Score.KeyIsMinor);
        Assert.Equal(4, loaded.Score.Key);
    }
}
