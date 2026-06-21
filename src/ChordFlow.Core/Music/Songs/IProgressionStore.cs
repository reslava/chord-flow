using ChordFlow.Music.Progressions;
namespace ChordFlow.Music.Songs;

/// <summary>
/// The lookup seam for <b>stored</b> progressions: maps a <see cref="ProgressionReference.ProgressionId"/> to
/// its <see cref="Progression"/>. Declared in <c>Domain/</c> as an interface so <see cref="SongExpander"/> can
/// resolve references while the domain stays I/O-free (constraint C3); the concrete, DB-backed implementation
/// lives in <c>Persistence/</c>. Inline parts never touch it.
/// </summary>
public interface IProgressionStore
{
    /// <summary>The stored progression with <paramref name="id"/>, or <c>null</c> when none exists.</summary>
    Progression? Find(string id);
}
