using ChordFlow.Exercises;
using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Songs;
using ChordFlow.Rendering;

namespace ChordFlow.Desktop.WebHost;

/// <summary>
/// A mutable <see cref="IScoreRenderer"/> wrapper whose backing renderer can be hot-swapped. The exercise
/// generator, library, and content-CRUD preview all hold this one instance, so when an authored voicing
/// changes the host rebuilds the voicing-backed <c>AlphaTexRenderer</c> and <see cref="Swap"/>s it in — every
/// consumer renders against the fresh voicing book on the next call, with no restart (IN11). A host-lifetime
/// concern, so it lives in Desktop, not Core.
/// </summary>
public sealed class SwappableRenderer : IScoreRenderer
{
    private IScoreRenderer _inner;

    public SwappableRenderer(IScoreRenderer inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    /// <summary>Replace the backing renderer (e.g. after a voicing save/delete rebuilds the voicing book).</summary>
    public void Swap(IScoreRenderer inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    public RenderResult Render(RealizedSong song, RhythmPattern rhythm, int tempo, Difficulty difficulty, TripletFeel tripletFeel = TripletFeel.None, RhythmPattern? lead = null, RenderOptions? options = null) =>
        _inner.Render(song, rhythm, tempo, difficulty, tripletFeel, lead, options);
}
