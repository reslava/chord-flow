using ChordFlow.Domain;
using ChordFlow.Persistence;
using ChordFlow.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ChordFlow.Features.GenerateExercise;

/// <summary>
/// Outbound bridge envelope: load a freshly rendered score into the WebView.
/// Serializes to <c>{"type":"loadScore","tex":"…","tempo":N}</c>. The alphaTex
/// string is the real payload; tempo rides along for the transport controls.
/// </summary>
public sealed record LoadScoreEnvelope(string Type, string Tex, int Tempo)
{
    /// <summary>
    /// Render an <see cref="Exercise"/> to alphaTex and wrap it for the bridge. The single place a loadScore
    /// envelope is built — shared by GenerateExercise (fresh) and ExerciseLibrary (regenerated on load), so
    /// alphaTex is never persisted. Expansion (the one I/O seam) runs through <see cref="ExerciseRendering"/>
    /// against <paramref name="store"/>; the renderer stays pure (merge decision (a)).
    /// </summary>
    public static LoadScoreEnvelope From(
        Exercise exercise, IProgressionStore store, IScoreRenderer renderer, RenderOptions? options = null) =>
        new("loadScore", ExerciseRendering.RenderToTex(exercise, store, renderer, options), exercise.Tempo);
}

/// <summary>
/// GenerateExercise vertical slice: composes the Domain kernel + AlphaTexRenderer
/// to produce a real, engine-rendered score and wrap it in a <see cref="LoadScoreEnvelope"/>
/// for the bridge. No mediator — a slice is a class with a method.
/// </summary>
public sealed class GenerateExerciseHandler
{
    private readonly DbContextOptions<ChordFlowDbContext> _dbOptions;
    private readonly IScoreRenderer _renderer;

    public GenerateExerciseHandler(DbContextOptions<ChordFlowDbContext> dbOptions, IScoreRenderer renderer)
    {
        _dbOptions = dbOptions;
        _renderer = renderer;
    }

    /// <summary>
    /// Build a 12-bar blues <see cref="Exercise"/> in the given key + rhythm, render
    /// it to alphaTex, and wrap it in a loadScore envelope ready for the bridge.
    /// </summary>
    /// <param name="keyPitchClass">Tonic pitch class 0..11 (10 = Bb). Major only for the MVP.</param>
    /// <param name="rhythmId">Seed rhythm id; falls back to "Beats 1 & 3" if unknown.</param>
    /// <param name="tempo">BPM.</param>
    public LoadScoreEnvelope Generate(int keyPitchClass, string rhythmId, int tempo)
    {
        using var db = new ChordFlowDbContext(_dbOptions);
        return LoadScoreEnvelope.From(Build(keyPitchClass, rhythmId, tempo), new ProgressionStore(db), _renderer);
    }

    /// <summary>
    /// Build the 12-bar blues <see cref="Exercise"/> definition (no rendering). Exposed so
    /// the host can keep the current definition for the ExerciseLibrary save path. The bare blues
    /// progression is lifted into a single-section Song via <see cref="Song.OfProgression"/> so it rides the
    /// one realization path; no lead track and no key override (the key is the Song's initial key).
    /// </summary>
    public Exercise Build(int keyPitchClass, string rhythmId, int tempo)
    {
        RhythmPattern rhythm =
            SeedData.RhythmPatterns.FirstOrDefault(r => r.Id == rhythmId) ?? SeedData.Beat1And3;

        // The chosen practice key is carried in KeyOverride so it persists for a bare-progression drill
        // (the lifted Song isn't stored — only its id is — so KeyOverride is the key's only home; IN3/IN4).
        var key = new Key(new PitchClass(keyPitchClass), false);
        return new Exercise(
            Song.OfProgression(SeedData.TwelveBarBlues, key),
            rhythm,
            Lead: null,
            KeyOverride: key,
            Tempo: tempo,
            Difficulty: Difficulty.Beginner);
    }
}
