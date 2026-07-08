using ChordFlow.Music.Progressions;
using ChordFlow.Music.Harmony;
namespace ChordFlow.Music.Songs;

/// <summary>
/// One realized section of a <see cref="RealizedSong"/>: a labelled run of <see cref="RealizedBar"/>s in a
/// concrete <see cref="Key"/>. Pure keyed data — no alphaTex (design §8.2): the <see cref="Key"/> is an
/// <i>output</i> of the expander's modulation fold, never an input (decision E). The renderer walks these to
/// emit one score, changing <c>\ks</c> only when <see cref="Key"/> differs from the previous section.
/// </summary>
public sealed record RealizedSection(string Label, Key Key, IReadOnlyList<RealizedBar> Bars);

/// <summary>
/// A fully realized song: the ordered <see cref="RealizedSection"/>s produced by <see cref="SongExpander"/>
/// (references resolved, modulations folded, repeats expanded). Holds no alphaTex and is never persisted —
/// it is regenerated from the Song DSL on load (constraint C4) and consumed section-by-section by the renderer.
/// </summary>
public sealed record RealizedSong(IReadOnlyList<RealizedSection> Sections)
{
    private static readonly IReadOnlyDictionary<VoiceSelector, string> NoVoices = new Dictionary<VoiceSelector, string>();

    /// <summary>
    /// The Song's <c>voice</c> defaults (<see cref="Song.Voices"/>) carried through to realization so the
    /// Features-layer comping resolver can apply them per chord — <see cref="VoiceSelector"/> → raw spec text
    /// (opaque, design D9). Empty for a bare progression or any Song without <c>voice</c> directives.
    /// </summary>
    public IReadOnlyDictionary<VoiceSelector, string> Voices { get; init; } = NoVoices;
}
