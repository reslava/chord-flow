using ChordFlow.Exercises;
using ChordFlow.Music.Rhythm;

namespace ChordFlow.Persistence.Entities;

/// <summary>
/// Persisted exercise <b>definition</b> — references to content rows plus playback params, never the
/// rendered alphaTex (regenerated on load via <see cref="Rendering.AlphaTexRenderer"/>, so a renderer fix
/// improves every saved exercise). Refactored from the old <c>(Key, ProgressionId, RhythmId)</c> shape to the
/// merged <see cref="Exercise"/> model (decision (a), IN4): a Song reference + comping/lead pattern
/// references + the key-override token and param columns.
/// </summary>
public sealed class ExerciseEntity
{
    /// <summary>Surrogate key (SQLite autoincrement).</summary>
    public int Id { get; set; }

    /// <summary>References a Song row — or, for a bare-progression drill, the lifted progression's id
    /// (<see cref="Song.OfProgression"/> reuses it). Was <c>ProgressionId</c>.</summary>
    public string SongId { get; set; } = "";

    /// <summary>References the comping (rhythm-guitar) <c>RhythmPattern</c> row. Was <c>RhythmId</c>.</summary>
    public string CompingPatternId { get; set; } = "";

    /// <summary>References the optional lead <c>RhythmPattern</c> row; <c>null</c> ⇒ no lead track.</summary>
    public string? LeadPatternId { get; set; }

    /// <summary>Practice-key override as a <c>\ks</c>-style tonic token (e.g. <c>bb</c>, <c>g</c>); <c>null</c>
    /// ⇒ the Song's initial key. For a lifted bare progression this carries the chosen practice key.</summary>
    public string? KeyOverride { get; set; }

    /// <summary>Authored tempo in BPM.</summary>
    public int Tempo { get; set; }

    /// <summary>Difficulty level (stored as its name, e.g. <c>Beginner</c>).</summary>
    public Difficulty Difficulty { get; set; }

    /// <summary>Groove feel (stored as its name, e.g. <c>Straight</c>).</summary>
    public Feel Feel { get; set; }

    /// <summary>When this definition was first saved (UTC).</summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>Practice events recorded against this exercise.</summary>
    public List<PracticeRecordEntity> PracticeRecords { get; set; } = new();
}
