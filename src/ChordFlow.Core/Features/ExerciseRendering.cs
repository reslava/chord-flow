using ChordFlow.Features.ChordSheets;
using ChordFlow.Music.Harmony;
using ChordFlow.Exercises;
using ChordFlow.Features.Voicings;
using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Songs;
using ChordFlow.Rendering;
using ChordFlow.Rendering.ChordSheets;

namespace ChordFlow.Features;

/// <summary>
/// The projections of one <see cref="Exercise"/> realization pass (harmony-controls-r IN3): the score
/// <see cref="Render"/> (alphaTex + chord schedule) plus the chord-sheet projection — the <see cref="Sheet"/>
/// model and its playback <see cref="CellSchedule"/>. All derive from the SAME <c>RealizedSong</c> +
/// <c>CompingPlan</c> + render, so the tab, the sheet, its marker, and the now/next feed cannot drift.
/// </summary>
public sealed record ExerciseProjections(
    RenderResult Render, ChordSheet Sheet, IReadOnlyList<CellScheduleEntry> CellSchedule);

/// <summary>
/// The single place an <see cref="Exercise"/> is turned into alphaTex (merge decision (a)). It owns the one
/// I/O seam — expanding the exercise's <see cref="Song"/> against an <see cref="IProgressionStore"/> — so the
/// renderer stays pure/store-free: <c>AlphaTexRenderer</c> only ever sees a <c>RealizedSong</c>. Every render
/// path (generate, library reload, content preview) routes through here, so the
/// <see cref="Exercise.KeyOverride"/> transpose and the lead-track wiring live in exactly one spot.
/// </summary>
public static class ExerciseRendering
{
    // The printed bars per sheet row — a fixed request-side constant (harmony-controls-r design); sheet layout
    // beyond the row chunking is a JS display concern.
    private const int SheetBarsPerRow = 4;

    /// <summary>
    /// Expand <paramref name="exercise"/>'s Song (re-anchored to <see cref="Exercise.KeyOverride"/> when set,
    /// else <see cref="Song.InitialKey"/>) against <paramref name="store"/>, then render both tracks
    /// (<see cref="Exercise.Comping"/> + optional <see cref="Exercise.Lead"/>) — returning the alphaTex string
    /// and its chord schedule (the now/next-fretboards feed).
    /// </summary>
    public static RenderResult Render(
        Exercise exercise, IProgressionStore store, IScoreRenderer renderer, IStoredVoicingSource voicings,
        RenderOptions? options = null, IVoicingReferenceSource? references = null) =>
        RenderCore(exercise, store, renderer, voicings, options, references).Render;

    /// <summary>The alphaTex string only — for callers that don't need the chord schedule (e.g. Content preview).</summary>
    public static string RenderToTex(
        Exercise exercise, IProgressionStore store, IScoreRenderer renderer, IStoredVoicingSource voicings,
        RenderOptions? options = null, IVoicingReferenceSource? references = null) =>
        Render(exercise, store, renderer, voicings, options, references).Tex;

    /// <summary>
    /// <see cref="Render"/> plus the chord-sheet projection of the SAME pass (harmony-controls-r IN3): one
    /// expansion, one <c>CompingPlan</c>, one render — projected into the score result, the
    /// <see cref="ChordSheet"/> model, and the sheet's playback <see cref="ExerciseProjections.CellSchedule"/>
    /// (the builder's per-bar downbeats overlaid with the render schedule's mid-bar onsets). The comping plan is
    /// passed to the sheet builder <b>unconditionally</b> (IN10) so the model always carries tone + diagram
    /// data — the Below-cell adornment is a pure display toggle in JS, never a re-request.
    /// </summary>
    public static ExerciseProjections RenderWithSheet(
        Exercise exercise, IProgressionStore store, IScoreRenderer renderer, IStoredVoicingSource voicings,
        RenderOptions? options = null, IVoicingReferenceSource? references = null)
    {
        (RenderResult render, RealizedSong realized, Key baseKey, CompingPlan plan) =
            RenderCore(exercise, store, renderer, voicings, options, references);

        // The comping pattern's pickup rides along so the sheet emits the anacrusis lead-in cell as schedule
        // bar 0, keeping the cellSchedule on the renderer/alphaTab master-bar axis (sheet-pickup-bar D1/D2).
        ChordSheetBuildResult built = ChordSheetBuilder.Build(
            exercise.Song, realized, baseKey, TimeSignature.FourFour, new ChordSheetOptions(SheetBarsPerRow), plan,
            exercise.Comping.Pickup);

        return new ExerciseProjections(
            render, built.Sheet, ChordSheetBuilder.OverlaySchedule(built.BarSchedule, render.Schedule));
    }

    // The one realization pass every projection derives from: base key → expansion → comping plan → render.
    // Resolving the comping grips here (the I/O seam) keeps the renderer a pure formatter (D4=(B));
    // `references` supplies the source-qualified voicing references (u:/a:/pkg:); null ⇒ engine-only.
    private static (RenderResult Render, RealizedSong Realized, Key BaseKey, CompingPlan Plan) RenderCore(
        Exercise exercise, IProgressionStore store, IScoreRenderer renderer, IStoredVoicingSource voicings,
        RenderOptions? options, IVoicingReferenceSource? references)
    {
        ArgumentNullException.ThrowIfNull(exercise);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(voicings);

        Key baseKey = exercise.KeyOverride ?? exercise.Song.InitialKey;
        RealizedSong realized = SongExpander.Expand(exercise.Song, store, startKey: baseKey);
        CompingPlan plan = CompingResolver.Resolve(
            realized, (options ?? RenderOptions.Default).VoicingOrDefault, voicings, references);
        RenderResult render = renderer.Render(
            realized, exercise.Comping, exercise.Tempo, exercise.Difficulty, plan, exercise.TripletFeel,
            lead: exercise.Lead, drums: exercise.Drums, options: options);
        return (render, realized, baseKey, plan);
    }
}
