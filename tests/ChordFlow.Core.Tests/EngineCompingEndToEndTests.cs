using ChordFlow.Exercises;
using ChordFlow.Features;
using ChordFlow.Features.GenerateExercise;
using ChordFlow.Features.Packs;
using ChordFlow.Features.Voicings;
using ChordFlow.Music.Rhythm;
using ChordFlow.Persistence;
using ChordFlow.Rendering;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// End-to-end dogfood for the engine-as-comping-source (engine-derived-as-app-source IN11): the default
/// generate path comps <b>engine-derived</b> grips for a 12-bar blues (the chord schedule that drives the
/// now/next fretboards carries them), and a region-locked main source changes the grips. Visual confirmation
/// on the now/next fret-boxes is a manual app run; this pins the data the boxes consume.
/// </summary>
public class EngineCompingEndToEndTests
{
    private static (DbContextOptions<ChordFlowDbContext> Options, SqliteConnection Conn) NewDb()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        DbContextOptions<ChordFlowDbContext> options =
            new DbContextOptionsBuilder<ChordFlowDbContext>().UseSqlite(conn).Options;
        using (var db = new ChordFlowDbContext(options))
        {
            db.Database.Migrate();
            DefaultPack.ImportInto(db);
            ContentSourceMigration.Run(db);
        }

        return (options, conn);
    }

    [Fact]
    public void Generate_TwelveBarBlues_CompsEngineDerivedGrips()
    {
        var (options, conn) = NewDb();
        using (conn)
        {
            var handler = new GenerateExerciseHandler(options, new AlphaTexRenderer());

            // The boot path: 12-bar blues in C, beats 1 & 3 — no renderOptions ⇒ automatic / full neck / Closest.
            LoadScoreEnvelope env = handler.Generate(
                "progression", "12bar_blues", "beat_1_3", leadPatternId: null,
                keyPitchClass: 0, tempo: 80, Difficulty.Beginner, TripletFeel.None);

            Assert.NotEmpty(env.Schedule);
            Assert.Equal("C7", env.Schedule[0].Name);
            // An engine CAGED grip sounds 4–6 strings; the old 3-note BeginnerShell never did. This is the
            // proof the comped voicing is engine-derived, not the shell fallback.
            Assert.True(env.Schedule[0].Diagram.Markers.Count > 3,
                $"expected a multi-string engine grip, got {env.Schedule[0].Diagram.Markers.Count} markers");
        }
    }

    [Fact]
    public void RegionLockedMainSource_ChangesTheCompedGrips()
    {
        var (options, conn) = NewDb();
        using (conn)
        using (var db = new ChordFlowDbContext(options))
        {
            var renderer = new AlphaTexRenderer();
            var store = new ProgressionStore(db);
            var voicings = StoredVoicingSource.From(new VoicingStore(db));
            Exercise exercise = new GenerateExerciseHandler(options, renderer).Build(
                "progression", "12bar_blues", "beat_1_3", null, 0, 80, Difficulty.Beginner, TripletFeel.None);

            string fullNeck = ExerciseRendering.RenderToTex(
                exercise, store, renderer, voicings, new RenderOptions(Voicing: new VoicingSource()));
            string region = ExerciseRendering.RenderToTex(
                exercise, store, renderer, voicings, new RenderOptions(Voicing: new VoicingSource(MinFret: 5, MaxFret: 12)));

            Assert.NotEqual(fullNeck, region); // the region knob is a real practice dial
        }
    }
}
