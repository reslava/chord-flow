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
/// production. These helpers build a plan from the <see cref="ShellGripFixture"/> (the retired movable shell)
/// so the renderer's formatting tests keep their byte-identical shell-grip expectations and stay decoupled from
/// the comping source.
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

    // One stable grip per chord for the renderer's formatting tests: the movable shell for dom7/min7/maj7
    // (so the byte-identical shell-grip expectations hold), and an engine caged grip for any other quality
    // (dim7, 6, m6, triads) the retired shell never covered — keeping seed-render tests no-throw.
    private static CompingPlan ShellPlan(RealizedSong song, Difficulty difficulty)
    {
        var grips = new Dictionary<Chord, Voicing>();
        foreach (RealizedSection section in song.Sections)
        {
            foreach (RealizedBar bar in section.Bars)
            {
                foreach (RealizedSpan span in bar.Spans)
                {
                    grips[span.Chord] = Grip(span.Chord);
                }
            }
        }

        return new CompingPlan(grips);
    }

    private static Voicing Grip(Chord chord)
    {
        if (chord.Quality is Quality.Dominant7 or Quality.Minor7 or Quality.Major7)
        {
            return ShellGripFixture.Voice(chord);
        }

        // Engine caged fallback: the lowest fret window of any CAGED shape that derives cleanly.
        foreach (CagedShape shape in CagedVoicingCatalog.ShapesFor(VoicingFamily.Caged, chord.Quality))
        {
            for (int minFret = 0; minFret <= 12; minFret++)
            {
                try
                {
                    return ChordShapeVoicing.ToVoicing(
                        FamilyVoicing.Derive(VoicingFamily.Caged, chord.Quality, shape, chord.Root, minFret, 15));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
                {
                    // Try a higher anchor / the next shape.
                }
            }
        }

        throw new NotSupportedException($"No test grip for {chord.Root.Value}:{chord.Quality}.");
    }
}
