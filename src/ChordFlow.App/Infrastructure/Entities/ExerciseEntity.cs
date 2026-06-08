using ChordFlow.Domain;

namespace ChordFlow.Infrastructure.Entities;

/// <summary>
/// Persisted exercise <b>definition</b> — the fields of the Domain <see cref="Exercise"/>
/// record, never the rendered alphaTex. alphaTex is regenerated from this on load
/// (via <see cref="Rendering.AlphaTexRenderer"/>) so a renderer fix improves every
/// saved exercise. MVP keys are major-only, so the mode flag isn't stored — only the
/// tonic pitch class in <see cref="Key"/>.
/// </summary>
public sealed class ExerciseEntity
{
    /// <summary>Surrogate key (SQLite autoincrement).</summary>
    public int Id { get; set; }

    /// <summary>Tonic pitch class 0..11 (10 = Bb). Major mode assumed for the MVP.</summary>
    public int Key { get; set; }

    /// <summary>Seed progression id (MVP: <c>12bar_blues</c>).</summary>
    public string ProgressionId { get; set; } = "";

    /// <summary>Seed rhythm id (<c>beat_1</c> / <c>beat_1_3</c> / <c>quarters</c>).</summary>
    public string RhythmId { get; set; } = "";

    /// <summary>Authored tempo in BPM.</summary>
    public int Tempo { get; set; }

    /// <summary>Difficulty level (stored as its name, e.g. <c>Beginner</c>).</summary>
    public Difficulty Difficulty { get; set; }

    /// <summary>When this definition was first saved (UTC).</summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>Practice events recorded against this exercise.</summary>
    public List<PracticeRecordEntity> PracticeRecords { get; set; } = new();
}
