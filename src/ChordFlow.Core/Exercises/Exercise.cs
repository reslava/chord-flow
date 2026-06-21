using ChordFlow.Music.Harmony;
using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Songs;
namespace ChordFlow.Exercises;

/// <summary>
/// The composed practice unit the engine plays — the one canonical play-unit (merge decision (a),
/// <c>exercises-definition-ui-chat-002</c>): it supersedes both the old
/// <c>Exercise(Key, Progression, …)</c> and the <c>song</c> thread's <c>SongExercise(Song, …)</c>,
/// which are deleted. Definition is <b>references</b> — a <see cref="Song"/> (harmony +
/// arrangement; a bare progression is lifted via <see cref="Song.OfProgression"/>), a required
/// <see cref="Comping"/> rhythm-guitar pattern, and an optional <see cref="Lead"/> pattern (v1 renders
/// as dead notes). Params are values with saved defaults: <see cref="KeyOverride"/> (null →
/// <see cref="Song.InitialKey"/>; else a global transpose), <see cref="Tempo"/>,
/// <see cref="Difficulty"/>, and groove <see cref="Feel"/>.
/// <para>
/// Realization is the single path <see cref="SongExpander.Expand"/> → <c>RealizedSong</c> →
/// <c>AlphaTexRenderer.Render(RealizedSong, …)</c>; the expansion (the one I/O seam, needs the
/// <see cref="IProgressionStore"/>) lives in the Features layer, never the renderer (decision (a)).
/// Pure/immutable, no I/O (C3). <see cref="Feel"/> is a render-time transform, never baked into the
/// pattern.
/// </para>
/// </summary>
public sealed record Exercise(
    // ── Definition (references — what to play) ──
    Song Song,
    RhythmPattern Comping,
    RhythmPattern? Lead,

    // ── Params (values — saved defaults, user-editable at play) ──
    Key? KeyOverride,
    int Tempo,
    Difficulty Difficulty,
    Feel Feel = Feel.Straight);
