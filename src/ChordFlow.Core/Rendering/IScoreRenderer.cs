using ChordFlow.Exercises;
using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Songs;

namespace ChordFlow.Rendering;

/// <summary>
/// Renders a <see cref="RealizedSong"/> to a score-notation string. The seam that keeps future exporters
/// (MIDI / Guitar Pro / MusicXML) additive — alphaTex is just the first implementation. The renderer is
/// pure/store-free: an <see cref="Exercise"/> is expanded into a <see cref="RealizedSong"/> in the
/// Features layer (the one I/O seam — see <c>ExerciseRendering</c>) before it reaches here (merge decision (a)).
/// </summary>
public interface IScoreRenderer
{
    /// <summary>
    /// Render a <see cref="RealizedSong"/> as a single score: one header (seeded from the first section's
    /// key), then each section's bars with an inline <c>\ks</c> emitted only when its <see cref="RealizedSection.Key"/>
    /// changes. The stateful <c>:N</c> duration flows across section seams. <paramref name="rhythm"/> is the
    /// comping track; <paramref name="lead"/>, when non-null, is rendered as a second <c>\track</c> of dead
    /// notes (single-track when null).
    /// </summary>
    /// <param name="lead">Optional lead-guitar pattern; <c>null</c> ⇒ single-track output (no lead staff).</param>
    /// <param name="options">Render-time presentation options; <c>null</c> ⇒ <see cref="RenderOptions.Default"/> (today's render).</param>
    string Render(RealizedSong song, RhythmPattern rhythm, int tempo, Difficulty difficulty, TripletFeel tripletFeel = TripletFeel.None, RhythmPattern? lead = null, RenderOptions? options = null);
}
