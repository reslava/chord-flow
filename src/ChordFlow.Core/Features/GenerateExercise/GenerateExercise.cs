using ChordFlow.Exercises;
using ChordFlow.Features.Voicings;
using ChordFlow.Music.Harmony;
using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Songs;
using ChordFlow.Persistence;
using ChordFlow.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ChordFlow.Features.GenerateExercise;

/// <summary>
/// Outbound bridge envelope: load a freshly rendered score into the WebView.
/// Serializes to <c>{"type":"loadScore","tex":"…","tempo":N,"schedule":[…]}</c>. The alphaTex string is the
/// real payload; tempo rides along for the transport controls, and <see cref="Schedule"/> (one entry per chord
/// change, each with a fretboard diagram of the comped voicing) feeds the now/next fretboards.
/// </summary>
public sealed record LoadScoreEnvelope(string Type, string Tex, int Tempo, IReadOnlyList<ChordChange> Schedule)
{
    /// <summary>
    /// Render an <see cref="Exercise"/> to alphaTex + chord schedule and wrap it for the bridge. The single
    /// place a loadScore envelope is built — shared by GenerateExercise (fresh) and ExerciseLibrary
    /// (regenerated on load), so alphaTex is never persisted. Expansion (the one I/O seam) runs through
    /// <see cref="ExerciseRendering"/> against <paramref name="store"/>; the renderer stays pure (merge decision (a)).
    /// </summary>
    public static LoadScoreEnvelope From(
        Exercise exercise, IProgressionStore store, IScoreRenderer renderer, IStoredVoicingSource voicings,
        RenderOptions? options = null)
    {
        RenderResult result = ExerciseRendering.Render(exercise, store, renderer, voicings, options);
        return new("loadScore", result.Tex, exercise.Tempo, result.Schedule);
    }
}

/// <summary>
/// GenerateExercise vertical slice: composes a canonical <see cref="Exercise"/> from the UI's chosen
/// <b>content references</b> (a stored Song or a bare Progression for harmony, a Comping pattern, an optional
/// Lead pattern) + params, renders it to alphaTex, and wraps it in a <see cref="LoadScoreEnvelope"/> for the
/// bridge. The reference → Domain resolution is the shared <see cref="ExerciseRefs"/> seam (also used by the
/// library load path). No mediator — a slice is a class with a method.
/// </summary>
public sealed class GenerateExerciseHandler
{
    // A bare progression has no inherent key; if the UI sends no key override we anchor the lift at C major.
    private static readonly Key DefaultLiftKey = new(new PitchClass(0), IsMinor: false);

    private readonly DbContextOptions<ChordFlowDbContext> _dbOptions;
    private readonly IScoreRenderer _renderer;

    public GenerateExerciseHandler(DbContextOptions<ChordFlowDbContext> dbOptions, IScoreRenderer renderer)
    {
        _dbOptions = dbOptions;
        _renderer = renderer;
    }

    /// <summary>
    /// Build the chosen <see cref="Exercise"/>, render it to alphaTex, and wrap it in a loadScore envelope
    /// ready for the bridge.
    /// </summary>
    public LoadScoreEnvelope Generate(
        string harmonyEntity, string harmonyId, string compingPatternId, string? leadPatternId,
        int? keyPitchClass, int tempo, Difficulty difficulty, TripletFeel tripletFeel)
    {
        using var db = new ChordFlowDbContext(_dbOptions);
        Exercise exercise = Build(
            db, harmonyEntity, harmonyId, compingPatternId, leadPatternId, keyPitchClass, tempo, difficulty, tripletFeel);
        return LoadScoreEnvelope.From(exercise, new ProgressionStore(db), _renderer, StoredVoicingSource.From(new VoicingStore(db)));
    }

    /// <summary>
    /// Host convenience: build the <see cref="Exercise"/> definition opening a short-lived context (the host
    /// keeps the returned definition for the save path; rendering opens its own context). Mirrors the per-use
    /// context pattern of the other handlers.
    /// </summary>
    public Exercise Build(
        string harmonyEntity, string harmonyId, string compingPatternId, string? leadPatternId,
        int? keyPitchClass, int tempo, Difficulty difficulty, TripletFeel tripletFeel)
    {
        using var db = new ChordFlowDbContext(_dbOptions);
        return Build(db, harmonyEntity, harmonyId, compingPatternId, leadPatternId, keyPitchClass, tempo, difficulty, tripletFeel);
    }

    /// <summary>
    /// Build the <see cref="Exercise"/> definition (no rendering) from the chosen references + params, resolving
    /// each reference against the stores via <see cref="ExerciseRefs"/>. Exposed so the host can keep the
    /// current definition for the ExerciseLibrary save path. <paramref name="harmonyEntity"/> is
    /// <c>"song"</c> or <c>"progression"</c>; a bare progression is lifted via <see cref="Song.OfProgression"/>
    /// so it rides the one realization path. The chosen practice key is carried in <c>KeyOverride</c> (the only
    /// persistent home for a lifted progression's key); a stored Song with no override keeps its own initial key.
    /// </summary>
    public Exercise Build(
        ChordFlowDbContext db,
        string harmonyEntity, string harmonyId, string compingPatternId, string? leadPatternId,
        int? keyPitchClass, int tempo, Difficulty difficulty, TripletFeel tripletFeel)
    {
        Key? keyOverride = keyPitchClass is int pc ? new Key(new PitchClass(pc), false) : null;
        Key liftKey = keyOverride ?? DefaultLiftKey;

        Song song = ExerciseRefs.ResolveHarmony(harmonyEntity, harmonyId, liftKey, db);
        RhythmPattern comping = ExerciseRefs.ResolvePattern(compingPatternId, db);
        RhythmPattern? lead = ExerciseRefs.ResolveOptionalPattern(leadPatternId, db);

        return new Exercise(song, comping, lead, keyOverride, tempo, difficulty, tripletFeel);
    }
}
