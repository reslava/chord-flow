using System.Globalization;
using ChordFlow.Domain;
using ChordFlow.Features.GenerateExercise;
using ChordFlow.Infrastructure;
using ChordFlow.Infrastructure.Entities;
using ChordFlow.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ChordFlow.Features.ExerciseLibrary;

/// <summary>
/// One saved exercise, for the library list in the UI. Raw definition fields — the
/// JS builds the human label from its key/rhythm name maps (the same ones the pickers
/// use), so no naming is duplicated in C#.
/// </summary>
public sealed record ExerciseSummary(
    int Id, int Key, string RhythmId, int Tempo, string Difficulty, string CreatedUtc, int PracticedCount);

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
            Key = NormalizePitchClass(exercise.Key.Tonic.Value),
            ProgressionId = exercise.Progression.Id,
            RhythmId = exercise.Rhythm.Id,
            Tempo = exercise.Tempo,
            Difficulty = exercise.Difficulty,
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
                e.Id, e.Key, e.RhythmId, e.Tempo,
                e.Difficulty.ToString(),
                e.CreatedUtc.ToString("o", CultureInfo.InvariantCulture),
                practiced.GetValueOrDefault(e.Id)))
            .ToList();

        return new ExerciseListEnvelope(summaries);
    }

    /// <summary>
    /// Reload a saved exercise: reconstruct the definition from seed data and regenerate
    /// its alphaTex. Returns <c>null</c> if the id is unknown.
    /// </summary>
    public LoadedExercise? Load(int id)
    {
        using var db = new ChordFlowDbContext(_dbOptions);
        ExerciseEntity? entity = db.Exercises.AsNoTracking().FirstOrDefault(e => e.Id == id);
        if (entity is null)
        {
            return null;
        }

        Exercise exercise = ToExercise(entity);
        return new LoadedExercise(exercise, LoadScoreEnvelope.From(exercise, _renderer));
    }

    // Rebuild the Domain Exercise from a stored definition. The progression/rhythm ids
    // resolve back to the seed objects; MVP keys are major-only.
    private static Exercise ToExercise(ExerciseEntity e)
    {
        var key = new Key(new PitchClass(NormalizePitchClass(e.Key)), IsMinor: false);

        // MVP has a single progression; resolve by id, fall back to it.
        Progression progression = e.ProgressionId == SeedData.TwelveBarBlues.Id
            ? SeedData.TwelveBarBlues
            : SeedData.TwelveBarBlues;

        RhythmPattern rhythm =
            SeedData.RhythmPatterns.FirstOrDefault(r => r.Id == e.RhythmId) ?? SeedData.Beat1And3;

        return new Exercise(key, progression, rhythm, e.Tempo, e.Difficulty);
    }

    private static int NormalizePitchClass(int value) => ((value % 12) + 12) % 12;
}
