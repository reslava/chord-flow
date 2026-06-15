using System.Globalization;
using ChordFlow.Domain;
using ChordFlow.Features.GenerateExercise;
using ChordFlow.Persistence;
using ChordFlow.Persistence.Entities;
using ChordFlow.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ChordFlow.Features.ExerciseLibrary;

/// <summary>
/// One saved exercise, for the library list in the UI. Raw definition fields (references + params) — the JS
/// builds the human label from its own name maps, so no naming is duplicated in C#. Reshaped for the merged
/// <see cref="Exercise"/> model (IN4): <c>SongId</c>/<c>CompingPatternId</c>/<c>LeadPatternId</c> references +
/// the <c>KeyOverride</c> token + <c>Feel</c> (was <c>Key</c>/<c>RhythmId</c>). NOTE: <c>app.js</c>'s list
/// rendering still reads the old fields — the UI rewire belongs to the <c>ui/exercise-workbench</c> thread.
/// </summary>
public sealed record ExerciseSummary(
    int Id, string SongId, string CompingPatternId, string? LeadPatternId, string? KeyOverride,
    int Tempo, string Difficulty, string Feel, string CreatedUtc, int PracticedCount);

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
            Feel = exercise.Feel,
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
                e.Difficulty.ToString(), e.Feel.ToString(),
                e.CreatedUtc.ToString("o", CultureInfo.InvariantCulture),
                practiced.GetValueOrDefault(e.Id)))
            .ToList();

        return new ExerciseListEnvelope(summaries);
    }

    /// <summary>
    /// Reload a saved exercise: reconstruct the definition from seed data and regenerate
    /// its alphaTex. Returns <c>null</c> if the id is unknown.
    /// </summary>
    public LoadedExercise? Load(int id, RenderOptions? options = null)
    {
        using var db = new ChordFlowDbContext(_dbOptions);
        ExerciseEntity? entity = db.Exercises.AsNoTracking().FirstOrDefault(e => e.Id == id);
        if (entity is null)
        {
            return null;
        }

        Exercise exercise = ToExercise(entity);
        return new LoadedExercise(exercise, LoadScoreEnvelope.From(exercise, new ProgressionStore(db), _renderer, options));
    }

    // Rebuild the Domain Exercise from a stored definition. The KeyOverride token round-trips the chosen
    // practice key (the only place a lifted bare-progression's key is stored). The MVP resolves the single
    // seed progression + seed patterns by id and lifts the progression to a single-section Song so it rides
    // the one realization path. Full Song/RhythmPattern store resolution (fail-loud on a missing row, §3) is
    // a ui/exercise-workbench concern, once real authored content is saved as exercises.
    private static Exercise ToExercise(ExerciseEntity e)
    {
        Key? keyOverride = e.KeyOverride is { Length: > 0 } token
            ? NoteSpeller.KeyFromSignatureToken(token)
            : null;
        Key liftKey = keyOverride ?? SeedDefaultKey;

        Progression progression = SeedData.TwelveBarBlues;
        RhythmPattern comping =
            SeedData.RhythmPatterns.FirstOrDefault(r => r.Id == e.CompingPatternId) ?? SeedData.Beat1And3;
        RhythmPattern? lead = e.LeadPatternId is { Length: > 0 } leadId
            ? SeedData.RhythmPatterns.FirstOrDefault(r => r.Id == leadId)
            : null;

        return new Exercise(
            Song.OfProgression(progression, liftKey), comping, lead, keyOverride, e.Tempo, e.Difficulty, e.Feel);
    }

    private static readonly Key SeedDefaultKey = new(new PitchClass(0), IsMinor: false); // C major fallback
}
