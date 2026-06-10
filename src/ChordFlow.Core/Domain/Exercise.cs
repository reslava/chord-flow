namespace ChordFlow.Domain;

/// <summary>
/// The composed practice exercise the engine emits: a progression in a key, with a rhythm pattern,
/// tempo, difficulty, and groove <see cref="Feel"/>. This is the definition persisted to SQLite;
/// alphaTex is regenerated from it on load. <see cref="Feel"/> is a playback-time transform applied
/// during rendering (defaults to <see cref="Domain.Feel.Straight"/>), never stored on the pattern (C4).
/// </summary>
public sealed record Exercise(
    Key Key,
    Progression Progression,
    RhythmPattern Rhythm,
    int Tempo,
    Difficulty Difficulty,
    Feel Feel = Feel.Straight);
