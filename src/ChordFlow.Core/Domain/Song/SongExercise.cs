namespace ChordFlow.Domain;

/// <summary>
/// The play unit for a <see cref="Song"/> — the direct analog of <see cref="Exercise"/> (decision D). A Song
/// is pure harmony + arrangement and cannot be played on its own; pairing it with a <see cref="RhythmPattern"/>,
/// tempo, difficulty, and groove <see cref="Feel"/> at play time keeps the Song reusable across rhythm settings.
/// Render path: <see cref="SongExpander.Expand"/> → <c>AlphaTexRenderer.Render(RealizedSong, …)</c>.
/// </summary>
public sealed record SongExercise(
    Song Song,
    RhythmPattern Rhythm,
    int Tempo,
    Difficulty Difficulty,
    Feel Feel = Feel.Straight);
