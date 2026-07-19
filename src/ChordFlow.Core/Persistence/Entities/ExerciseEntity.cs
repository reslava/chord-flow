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

    /// <summary>References the optional <c>DrumGroove</c> row tiled beneath the harmony (<c>drums-under-a-song</c>
    /// IN7); <c>null</c> ⇒ no drum part. The flat-column half of the <see cref="Exercises.DrumPart"/> mapping —
    /// a dynamic-roster child table is deferred behind this non-breaking seam (C7).</summary>
    public string? DrumGrooveId { get; set; }

    /// <summary>The drum part's saved mix volume (1.0 = unattenuated); a playback default seeding the UI slider,
    /// not baked into the alphaTex. Ignored when <see cref="DrumGrooveId"/> is null.</summary>
    public double DrumVolume { get; set; } = 1.0;

    /// <summary>Whether the drum part is saved muted. Ignored when <see cref="DrumGrooveId"/> is null.</summary>
    public bool DrumMuted { get; set; }

    /// <summary>Practice-key override as a <c>\ks</c>-style tonic token (e.g. <c>bb</c>, <c>g</c>); <c>null</c>
    /// ⇒ the Song's initial key. For a lifted bare progression this carries the chosen practice key.</summary>
    public string? KeyOverride { get; set; }

    /// <summary>Authored tempo in BPM.</summary>
    public int Tempo { get; set; }

    /// <summary>Difficulty level (stored as its name, e.g. <c>Beginner</c>).</summary>
    public Difficulty Difficulty { get; set; }

    /// <summary>Triplet feel / swing (stored as its name, e.g. <c>None</c>, <c>Triplet8th</c>).</summary>
    public TripletFeel TripletFeel { get; set; }

    /// <summary>When this definition was first saved (UTC).</summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>Practice events recorded against this exercise.</summary>
    public List<PracticeRecordEntity> PracticeRecords { get; set; } = new();
}
