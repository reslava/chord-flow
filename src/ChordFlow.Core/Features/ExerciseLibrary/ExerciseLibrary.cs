using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Songs;
using ChordFlow.Exercises;
using ChordFlow.Music.Harmony;
using System.Globalization;
using ChordFlow.Features.GenerateExercise;
using ChordFlow.Features.Voicings;
using ChordFlow.Persistence;
using ChordFlow.Persistence.Entities;
using ChordFlow.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ChordFlow.Features.ExerciseLibrary;

/// <summary>
/// One saved exercise, for the library list in the UI. Raw definition fields (references + params) — the JS
/// builds the human label from its own name maps, so no naming is duplicated in C#. Reshaped for the merged
/// <see cref="Exercise"/> model (IN4): <c>SongId</c>/<c>CompingPatternId</c>/<c>LeadPatternId</c> references +
/// the <c>KeyOverride</c> token + <c>TripletFeel</c> (was <c>Key</c>/<c>RhythmId</c>). NOTE: <c>app.js</c>'s list
/// rendering still reads the old fields — the UI rewire belongs to the <c>ui/exercise-workbench</c> thread.
/// </summary>
public sealed record ExerciseSummary(
    int Id, string SongId, string CompingPatternId, string? LeadPatternId, string? KeyOverride,
    int Tempo, string Difficulty, string TripletFeel, string CreatedUtc, int PracticedCount);

/// <summary>Outbound envelope: the saved-exercise list. Serializes to <c>{"type":"exerciseList","exercises":[…]}</c>.</summary>
public sealed record ExerciseListEnvelope(IReadOnlyList<ExerciseSummary> Exercises, string Type = "exerciseList");

/// <summary>A loaded exercise: the reconstructed definition (for host state) plus its freshly rendered score (for the bridge).</summary>
public sealed record LoadedExercise(Exercise Exercise, LoadScoreEnvelope Score);

/// <summary>
/// ExerciseLibrary vertical slice: persists exercise <b>definitions</b> to SQLite,
/// lists them, and reloads one — regenerating alphaTex from the definition via
/// <see cref="AlphaTexRenderer"/> on load (alphaTex is never stored, so a renderer
/// fix improves every saved exercise). A short-lived <see cref="ChordFlowDbContext"/>
/// per operation; no mediator — a slice is a class with methods.
/// </summary>
public sealed class ExerciseLibraryHandler
{
    private readonly DbContextOptions<ChordFlowDbContext> _dbOptions;
    private readonly IScoreRenderer _renderer;

    public ExerciseLibraryHandler(DbContextOptions<ChordFlowDbContext> dbOptions, IScoreRenderer renderer)
    {
        _dbOptions = dbOptions;
        _renderer = renderer;
    }

    /// <summary>Persist an exercise definition; returns the new row id.</summary>
    public int Save(Exercise exercise)
    {
        using var db = new ChordFlowDbContext(_dbOptions);
        var entity = new ExerciseEntity
        {
            SongId = exercise.Song.Id,
            CompingPatternId = exercise.Comping.Id,
            LeadPatternId = exercise.Lead?.Id,
            KeyOverride = exercise.KeyOverride is { } k ? NoteSpeller.KeySignatureToken(k) : null,
            Tempo = exercise.Tempo,
            Difficulty = exercise.Difficulty,
            TripletFeel = exercise.TripletFeel,
            CreatedUtc = DateTime.UtcNow,
        };
        db.Exercises.Add(entity);
        db.SaveChanges();
        return entity.Id;
    }

    /// <summary>List saved exercises, newest first, as a ready-to-send envelope.</summary>
    public ExerciseListEnvelope List()
    {
        using var db = new ChordFlowDbContext(_dbOptions);
        // Materialize, then project in memory — enum.ToString()/DateTime formatting
        // don't translate to SQL.
        List<ExerciseEntity> rows = db.Exercises
            .AsNoTracking()
            .OrderByDescending(e => e.CreatedUtc)
            .ToList();

        // Practice counts per exercise, for the ✓ marker in the list.
        Dictionary<int, int> practiced = db.PracticeRecords
            .AsNoTracking()
            .GroupBy(r => r.ExerciseId)
            .Select(g => new { ExerciseId = g.Key, Count = g.Count() })
            .ToDictionary(x => x.ExerciseId, x => x.Count);

        var summaries = rows
            .Select(e => new ExerciseSummary(
                e.Id, e.SongId, e.CompingPatternId, e.LeadPatternId, e.KeyOverride, e.Tempo,
                e.Difficulty.ToString(), e.TripletFeel.ToString(),
                e.CreatedUtc.ToString("o", CultureInfo.InvariantCulture),
                practiced.GetValueOrDefault(e.Id)))
            .ToList();

        return new ExerciseListEnvelope(summaries);
    }

    /// <summary>
    /// Reload a saved exercise: reconstruct the definition from seed data and regenerate its alphaTex. Returns
    /// <c>null</c> if the id is unknown. <paramref name="keyOverride"/>/<paramref name="tripletFeel"/> are the
    /// transient render-param overrides ScoreR replays on a live Key/Feel change (scorer-render-params IN4): when
    /// present they re-voice the <i>displayed</i> exercise (a transpose / a new <c>\tf</c>) without touching the
    /// stored definition — the saved exercise's own params still seed the controls on a plain load (C2).
    /// </summary>
    public LoadedExercise? Load(int id, RenderOptions? options = null, int? keyOverride = null, TripletFeel? tripletFeel = null)
    {
        using var db = new ChordFlowDbContext(_dbOptions);
        ExerciseEntity? entity = db.Exercises.AsNoTracking().FirstOrDefault(e => e.Id == id);
        if (entity is null)
        {
            return null;
        }

        Exercise exercise = ToExercise(entity, db);
        if (keyOverride is int pc)
        {
            exercise = exercise with { KeyOverride = new Key(new PitchClass(pc), IsMinor: false) };
        }
        if (tripletFeel is TripletFeel feel)
        {
            exercise = exercise with { TripletFeel = feel };
        }
        return new LoadedExercise(exercise, LoadScoreEnvelope.From(
            exercise, new ProgressionStore(db), _renderer, StoredVoicingSource.From(new VoicingStore(db)), options,
            references: VoicingReferenceSource.From(new VoicingStore(db))));
    }

    // Rebuild the Domain Exercise from a stored definition, resolving its references against the live stores
    // (ui/exercise-workbench IN8 — was hard-wired to the single seed blues + seed patterns). The KeyOverride
    // token round-trips the chosen practice key (the only place a lifted bare-progression's key is stored).
    // The stored SongId carries no song-vs-progression discriminator, so ExerciseRefs tries the Song store
    // first then falls back to a lifted Progression. A missing reference fails loud (surfaced as a load status).
    private static Exercise ToExercise(ExerciseEntity e, ChordFlowDbContext db)
    {
        Key? keyOverride = e.KeyOverride is { Length: > 0 } token
            ? NoteSpeller.KeyFromSignatureToken(token)
            : null;
        Key liftKey = keyOverride ?? SeedDefaultKey;

        Song song = ExerciseRefs.ResolveHarmonyById(e.SongId, liftKey, db);
        RhythmPattern comping = ExerciseRefs.ResolvePattern(e.CompingPatternId, db);
        RhythmPattern? lead = ExerciseRefs.ResolveOptionalPattern(e.LeadPatternId, db);

        return new Exercise(song, comping, lead, keyOverride, e.Tempo, e.Difficulty, e.TripletFeel);
    }

    private static readonly Key SeedDefaultKey = new(new PitchClass(0), IsMinor: false); // C major fallback
}
