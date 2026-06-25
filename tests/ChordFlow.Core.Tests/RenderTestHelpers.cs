using ChordFlow.Music.Songs;
using ChordFlow.Exercises;
using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;
using ChordFlow.Music.Progressions;
using ChordFlow.Music.Rhythm;
using ChordFlow.Rendering;

namespace ChordFlow.Core.Tests;

/// <summary>
/// Test-only bridges for the renderer. Under engine-derived-as-app-source (D4=(B)) the renderer is a pure
/// formatter that consumes a <see cref="CompingPlan"/>; the Features comping resolver builds that plan in
/// production. These helpers build a plan from the <b>BeginnerShellStrategy</b> (an empty
/// <see cref="VoicingBook"/>) so the renderer's formatting tests keep their byte-identical shell-grip
/// expectations and stay decoupled from the comping source.
/// </summary>
internal static class RenderTestHelpers
{
    /// <summary>Render a realized song with a shell-strategy comping plan — the pre-(B) <c>Render</c> signature.</summary>
    public static RenderResult Render(
        this AlphaTexRenderer renderer, RealizedSong song, RhythmPattern rhythm, int tempo, Difficulty difficulty,
        TripletFeel tripletFeel = TripletFeel.None, RhythmPattern? lead = null, RenderOptions? options = null) =>
        renderer.Render(song, rhythm, tempo, difficulty, ShellPlan(song, difficulty), tripletFeel, lead, options);

    public static string RenderProgression(
        this AlphaTexRenderer renderer, Key key, Progression progression, RhythmPattern rhythm, int tempo,
        Difficulty difficulty, TripletFeel tripletFeel = TripletFeel.None, RenderOptions? options = null)
    {
        var realized = new RealizedSong(new[]
        {
            new RealizedSection(progression.Name, key, Transposer.RealizeBars(progression, key)),
        });
        return renderer.Render(realized, rhythm, tempo, difficulty, tripletFeel, options: options).Tex;
    }

    // One grip per chord via BeginnerShellStrategy (empty book) — what the old default render path comped.
    private static CompingPlan ShellPlan(RealizedSong song, Difficulty difficulty)
    {
        var book = new VoicingBook(Array.Empty<VoicingShape>());
        var grips = new Dictionary<Chord, Voicing>();
        foreach (RealizedSection section in song.Sections)
        {
            foreach (RealizedBar bar in section.Bars)
            {
                foreach (RealizedSpan span in bar.Spans)
                {
                    grips[span.Chord] = book.Lookup(span.Chord, difficulty);
                }
            }
        }

        return new CompingPlan(grips);
    }
}
