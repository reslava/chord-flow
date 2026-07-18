using ChordFlow.Exercises;
using ChordFlow.Features.Voicings;
using ChordFlow.Music.Harmony;
using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Songs;
using ChordFlow.Persistence;
using ChordFlow.Rendering;
using ChordFlow.Rendering.ChordSheets;
using Microsoft.EntityFrameworkCore;

namespace ChordFlow.Features.GenerateExercise;

/// <summary>
/// Outbound bridge envelope: load a freshly rendered score into the WebView. Serializes to
/// <c>{"type":"loadScore","tex":"…","tempo":N,"key":P,"tripletFeel":"…","schedule":[…],"sheet":{…},"cellSchedule":[…]}</c>.
/// The alphaTex string is the real payload; <see cref="Tempo"/>/<see cref="Key"/>/<see cref="TripletFeel"/> ride
/// along so the definition controls can <b>seed</b> from the piece (scorer-render-params IN6 — a stored
/// exercise's persisted params win over content defaults, C2), and <see cref="Schedule"/> (one entry per
/// chord change, each with a fretboard diagram of the comped voicing) feeds the now/next fretboards.
/// <see cref="Sheet"/> + <see cref="CellSchedule"/> are the chord-sheet projection of the SAME render pass
/// (harmony-controls-r IN3): the Sheet view draws the model and maps score (bar,beat) → sounding cell with the
/// cellSchedule — one reply, both view surfaces, nothing can drift.
/// </summary>
public sealed record LoadScoreEnvelope(
    string Type, string Tex, int Tempo, int Key, bool KeyIsMinor, string TripletFeel, IReadOnlyList<ChordChange> Schedule,
    ChordSheet Sheet, IReadOnlyList<CellScheduleEntry> CellSchedule)
{
    /// <summary>
    /// Render an <see cref="Exercise"/> to alphaTex + chord schedule + the chord-sheet projection and wrap it
    /// for the bridge. The single place a loadScore envelope is built — shared by GenerateExercise (fresh) and
    /// ExerciseLibrary (regenerated on load), so alphaTex is never persisted. Expansion (the one I/O seam) runs
    /// through <see cref="ExerciseRendering"/> against <paramref name="store"/>; the renderer stays pure
    /// (merge decision (a)).
    /// </summary>
    public static LoadScoreEnvelope From(
        Exercise exercise, IProgressionStore store, IScoreRenderer renderer, IStoredVoicingSource voicings,
        RenderOptions? options = null, IVoicingReferenceSource? references = null)
    {
        ExerciseProjections result = ExerciseRendering.RenderWithSheet(
            exercise, store, renderer, voicings, options, references);
        // The effective key the piece renders in: an explicit override, else the Song's own initial key. The
        // Key control seeds from this (tonic + mode) so a loaded exercise shows the key it was saved in (C2).
        Key effectiveKey = exercise.KeyOverride ?? exercise.Song.InitialKey;
        return new(
            "loadScore", result.Render.Tex, exercise.Tempo, effectiveKey.Tonic.Value, effectiveKey.IsMinor,
            exercise.TripletFeel.ToString(), result.Render.Schedule, result.Sheet, result.CellSchedule);
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
        int? keyPitchClass, int tempo, Difficulty difficulty, TripletFeel tripletFeel, bool keyIsMinor = false)
    {
        using var db = new ChordFlowDbContext(_dbOptions);
        Exercise exercise = Build(
            db, harmonyEntity, harmonyId, compingPatternId, leadPatternId, keyPitchClass, tempo, difficulty, tripletFeel, keyIsMinor);
        return LoadScoreEnvelope.From(
            exercise, new ProgressionStore(db), _renderer, StoredVoicingSource.From(new VoicingStore(db)),
            references: VoicingReferenceSource.From(new VoicingStore(db)));
    }

    /// <summary>
    /// Host convenience: build the <see cref="Exercise"/> definition opening a short-lived context (the host
    /// keeps the returned definition for the save path; rendering opens its own context). Mirrors the per-use
    /// context pattern of the other handlers.
    /// </summary>
    public Exercise Build(
        string harmonyEntity, string harmonyId, string compingPatternId, string? leadPatternId,
        int? keyPitchClass, int tempo, Difficulty difficulty, TripletFeel tripletFeel, bool keyIsMinor = false)
    {
        using var db = new ChordFlowDbContext(_dbOptions);
        return Build(db, harmonyEntity, harmonyId, compingPatternId, leadPatternId, keyPitchClass, tempo, difficulty, tripletFeel, keyIsMinor);
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
        int? keyPitchClass, int tempo, Difficulty difficulty, TripletFeel tripletFeel, bool keyIsMinor = false)
    {
        Key? keyOverride = keyPitchClass is int pc ? new Key(new PitchClass(pc), keyIsMinor) : null;
        Key liftKey = keyOverride ?? DefaultLiftKey;

        Song song = ExerciseRefs.ResolveHarmony(harmonyEntity, harmonyId, liftKey, db);
        RhythmPattern comping = ExerciseRefs.ResolvePattern(compingPatternId, db);
        RhythmPattern? lead = ExerciseRefs.ResolveOptionalPattern(leadPatternId, db);

        return new Exercise(song, comping, lead, keyOverride, tempo, difficulty, tripletFeel);
    }
}
