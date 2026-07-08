using ChordFlow.Music.Harmony;
using ChordFlow.Exercises;
using ChordFlow.Features.Voicings;
using ChordFlow.Music.Songs;
using ChordFlow.Rendering;

namespace ChordFlow.Features;

/// <summary>
/// The single place an <see cref="Exercise"/> is turned into alphaTex (merge decision (a)). It owns the one
/// I/O seam — expanding the exercise's <see cref="Song"/> against an <see cref="IProgressionStore"/> — so the
/// renderer stays pure/store-free: <c>AlphaTexRenderer</c> only ever sees a <c>RealizedSong</c>. Every render
/// path (generate, library reload, content preview) routes through here, so the
/// <see cref="Exercise.KeyOverride"/> transpose and the lead-track wiring live in exactly one spot.
/// </summary>
public static class ExerciseRendering
{
    /// <summary>
    /// Expand <paramref name="exercise"/>'s Song (re-anchored to <see cref="Exercise.KeyOverride"/> when set,
    /// else <see cref="Song.InitialKey"/>) against <paramref name="store"/>, then render both tracks
    /// (<see cref="Exercise.Comping"/> + optional <see cref="Exercise.Lead"/>) — returning the alphaTex string
    /// and its chord schedule (the now/next-fretboards feed).
    /// </summary>
    public static RenderResult Render(
        Exercise exercise, IProgressionStore store, IScoreRenderer renderer, IStoredVoicingSource voicings,
        RenderOptions? options = null, IVoicingReferenceSource? references = null)
    {
        ArgumentNullException.ThrowIfNull(exercise);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(voicings);

        Key baseKey = exercise.KeyOverride ?? exercise.Song.InitialKey;
        RealizedSong realized = SongExpander.Expand(exercise.Song, store, startKey: baseKey);
        // Resolve the comping grips here (the I/O seam), so the renderer stays a pure formatter (D4=(B)).
        // `references` supplies the source-qualified voicing references (u:/a:/pkg:); null ⇒ engine-only.
        CompingPlan plan = CompingResolver.Resolve(
            realized, (options ?? RenderOptions.Default).VoicingOrDefault, voicings, references);
        return renderer.Render(
            realized, exercise.Comping, exercise.Tempo, exercise.Difficulty, plan, exercise.TripletFeel,
            lead: exercise.Lead, options: options);
    }

    /// <summary>The alphaTex string only — for callers that don't need the chord schedule (e.g. Content preview).</summary>
    public static string RenderToTex(
        Exercise exercise, IProgressionStore store, IScoreRenderer renderer, IStoredVoicingSource voicings,
        RenderOptions? options = null, IVoicingReferenceSource? references = null) =>
        Render(exercise, store, renderer, voicings, options, references).Tex;
}
