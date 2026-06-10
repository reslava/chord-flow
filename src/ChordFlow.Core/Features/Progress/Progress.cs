using ChordFlow.Persistence;
using ChordFlow.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChordFlow.Features.Progress;

/// <summary>
/// Outbound envelope confirming a recorded practice event. Serializes to
/// <c>{"type":"practiceRecorded","exerciseId":N,"count":M}</c>; <c>count</c> is the
/// running total of practice records for that exercise, for a little UI feedback.
/// </summary>
public sealed record PracticeRecordedEnvelope(int ExerciseId, int Count, string Type = "practiceRecorded");

/// <summary>
/// Progress vertical slice: on "mark practiced" it writes a single
/// <see cref="PracticeRecordEntity"/> for the active exercise. Records only the
/// <i>event</i> (and when) — no accuracy, no scoring (EX1, out of MVP scope). A
/// short-lived <see cref="ChordFlowDbContext"/> per call; no mediator.
/// </summary>
public sealed class ProgressHandler
{
    private readonly DbContextOptions<ChordFlowDbContext> _dbOptions;

    public ProgressHandler(DbContextOptions<ChordFlowDbContext> dbOptions) => _dbOptions = dbOptions;

    /// <summary>
    /// Record a practice event for a saved exercise; returns the running total of
    /// practice records for that exercise.
    /// </summary>
    public int MarkPracticed(int exerciseId)
    {
        using var db = new ChordFlowDbContext(_dbOptions);
        db.PracticeRecords.Add(new PracticeRecordEntity
        {
            ExerciseId = exerciseId,
            PracticedUtc = DateTime.UtcNow,
        });
        db.SaveChanges();
        return db.PracticeRecords.Count(r => r.ExerciseId == exerciseId);
    }
}
