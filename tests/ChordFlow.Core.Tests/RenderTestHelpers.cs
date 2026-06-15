using ChordFlow.Domain;
using ChordFlow.Rendering;

namespace ChordFlow.Core.Tests;

/// <summary>
/// Test-only bridge for the renderer's now-single entry point. <c>Render(Exercise)</c> was dropped in the
/// Exercise merge (decision (a)); a bare progression is rendered the way <see cref="Song.OfProgression"/>
/// would: one section labelled with the progression name (so the title reads "{name} — {key}"), realized in
/// <paramref name="key"/>, through the canonical <c>Render(RealizedSong, …)</c> path. Byte-identical to the
/// old <c>Render(Exercise)</c> output for a single-progression drill.
/// </summary>
internal static class RenderTestHelpers
{
    public static string RenderProgression(
        this AlphaTexRenderer renderer, Key key, Progression progression, RhythmPattern rhythm, int tempo,
        Difficulty difficulty, Feel feel = Feel.Straight, RenderOptions? options = null)
    {
        var realized = new RealizedSong(new[]
        {
            new RealizedSection(progression.Name, key, Transposer.RealizeBars(progression, key)),
        });
        return renderer.Render(realized, rhythm, tempo, difficulty, feel, options: options);
    }
}
