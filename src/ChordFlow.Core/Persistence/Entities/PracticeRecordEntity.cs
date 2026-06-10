namespace ChordFlow.Persistence.Entities;

/// <summary>
/// One "marked practiced" event for a saved exercise. Records only <i>that</i> the
/// exercise was practiced and <i>when</i> — no accuracy, no score (EX1, out of MVP scope).
/// </summary>
public sealed class PracticeRecordEntity
{
    /// <summary>Surrogate key (SQLite autoincrement).</summary>
    public int Id { get; set; }

    /// <summary>FK to the practiced <see cref="ExerciseEntity"/>.</summary>
    public int ExerciseId { get; set; }

    /// <summary>When the practice event was recorded (UTC).</summary>
    public DateTime PracticedUtc { get; set; }

    /// <summary>Navigation back to the owning exercise.</summary>
    public ExerciseEntity? Exercise { get; set; }
}
