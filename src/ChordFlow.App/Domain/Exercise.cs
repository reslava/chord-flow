namespace ChordFlow.Domain;

/// <summary>
/// The composed practice exercise the engine emits: a progression in a key, with a
/// rhythm pattern, tempo, and difficulty. This is the definition persisted to SQLite;
/// alphaTex is regenerated from it on load.
/// </summary>
public sealed record Exercise(
    Key Key,
    Progression Progression,
    RhythmPattern Rhythm,
    int Tempo,
    Difficulty Difficulty);
