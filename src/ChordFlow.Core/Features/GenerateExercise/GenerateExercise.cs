using ChordFlow.Domain;
using ChordFlow.Rendering;

namespace ChordFlow.Features.GenerateExercise;

/// <summary>
/// Outbound bridge envelope: load a freshly rendered score into the WebView.
/// Serializes to <c>{"type":"loadScore","tex":"…","tempo":N}</c>. The alphaTex
/// string is the real payload; tempo rides along for the transport controls.
/// </summary>
public sealed record LoadScoreEnvelope(string Type, string Tex, int Tempo)
{
    /// <summary>
    /// Render an <see cref="Exercise"/> to alphaTex and wrap it for the bridge. The
    /// single place a loadScore envelope is built — shared by GenerateExercise (fresh)
    /// and ExerciseLibrary (regenerated on load), so alphaTex is never persisted.
    /// </summary>
    public static LoadScoreEnvelope From(Exercise exercise, IScoreRenderer renderer, RenderOptions? options = null) =>
        new("loadScore", renderer.Render(exercise, options), exercise.Tempo);
}

/// <summary>
/// GenerateExercise vertical slice: composes the Domain kernel + AlphaTexRenderer
/// to produce a real, engine-rendered score and wrap it in a <see cref="LoadScoreEnvelope"/>
/// for the bridge. No mediator — a slice is a class with a method.
/// </summary>
public sealed class GenerateExerciseHandler
{
    private readonly IScoreRenderer _renderer;

    public GenerateExerciseHandler(IScoreRenderer renderer) => _renderer = renderer;

    /// <summary>
    /// Build a 12-bar blues <see cref="Exercise"/> in the given key + rhythm, render
    /// it to alphaTex, and wrap it in a loadScore envelope ready for the bridge.
    /// </summary>
    /// <param name="keyPitchClass">Tonic pitch class 0..11 (10 = Bb). Major only for the MVP.</param>
    /// <param name="rhythmId">Seed rhythm id; falls back to "Beats 1 & 3" if unknown.</param>
    /// <param name="tempo">BPM.</param>
    public LoadScoreEnvelope Generate(int keyPitchClass, string rhythmId, int tempo) =>
        LoadScoreEnvelope.From(Build(keyPitchClass, rhythmId, tempo), _renderer);

    /// <summary>
    /// Build the 12-bar blues <see cref="Exercise"/> definition (no rendering). Exposed so
    /// the host can keep the current definition for the ExerciseLibrary save path.
    /// </summary>
    public Exercise Build(int keyPitchClass, string rhythmId, int tempo)
    {
        RhythmPattern rhythm =
            SeedData.RhythmPatterns.FirstOrDefault(r => r.Id == rhythmId) ?? SeedData.Beat1And3;

        return new Exercise(
            new Key(new PitchClass(keyPitchClass), false),
            SeedData.TwelveBarBlues,
            rhythm,
            tempo,
            Difficulty.Beginner);
    }
}
