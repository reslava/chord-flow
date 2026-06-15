using ChordFlow.Domain;

namespace ChordFlow.Rendering;

/// <summary>
/// Renders an <see cref="Exercise"/> to a score-notation string. The seam that keeps
/// future exporters (MIDI / Guitar Pro / MusicXML) additive — alphaTex is just the
/// first implementation.
/// </summary>
public interface IScoreRenderer
{
    /// <param name="options">Render-time presentation options; <c>null</c> ⇒ <see cref="RenderOptions.Default"/> (today's render).</param>
    string Render(Exercise exercise, RenderOptions? options = null);

    /// <summary>
    /// Render a <see cref="RealizedSong"/> as a single score: one header (seeded from the first section's
    /// key), then each section's bars with an inline <c>\ks</c> emitted only when its <see cref="RealizedSection.Key"/>
    /// changes. The stateful <c>:N</c> duration flows across section seams. The play params come from a
    /// <see cref="SongExercise"/> (the Song itself carries no rhythm/tempo/feel — decision D).
    /// </summary>
    /// <param name="options">Render-time presentation options; <c>null</c> ⇒ <see cref="RenderOptions.Default"/> (today's render).</param>
    string Render(RealizedSong song, RhythmPattern rhythm, int tempo, Difficulty difficulty, Feel feel = Feel.Straight, RenderOptions? options = null);
}
