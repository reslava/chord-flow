using ChordFlow.Music.Progressions;
using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Songs;
using ChordFlow.Exercises;
using ChordFlow.Music.Harmony;
using ChordFlow.Features;
using ChordFlow.Features.Voicings;
using ChordFlow.Persistence;
using ChordFlow.Rendering;
using Microsoft.EntityFrameworkCore;

using ChordFlow.Instruments.Guitar;

namespace ChordFlow.Features.ContentCrud;

/// <summary>
/// ContentCrud vertical slice: the one handler behind the generic <c>entity*</c> bridge protocol. It maps an
/// entity discriminator to the matching <see cref="IContentStore"/> and turns list/get/preview/save/delete
/// into outbound envelopes. A short-lived <see cref="ChordFlowDbContext"/> per operation (like
/// <c>ExerciseLibraryHandler</c>); no mediator — a slice is a class with methods.
///
/// <para><b>Preview</b> renders the entity to something visual without persisting: progression/song/rhythm →
/// a small alphaTex <i>score</i> built with fixed preview defaults (key C, a default rhythm / a single chord,
/// 80 BPM); voicing → a <i>diagram</i> (the fret-box model is wired in step 3). Any preview failure (a parse
/// error, a missing song reference, an unrenderable chord) surfaces as a single <see cref="FormatException"/>
/// so the host's one catch maps it to an <c>entityParseError</c> (IN3).</para>
///
/// <para><b>Save/Delete</b> raise <see cref="VoicingsChanged"/> after a voicing write so the host can rebuild
/// the in-memory voicing book + renderer (step 7 / IN11); the other three entities aren't snapshotted.</para>
/// </summary>
public sealed class ContentCrudHandler
{
    private static readonly Key PreviewKey = new(new PitchClass(0), IsMinor: false); // C major
    private const int PreviewTempo = 80;

    private readonly DbContextOptions<ChordFlowDbContext> _dbOptions;
    private readonly IScoreRenderer _renderer;
    private readonly IReadOnlyDictionary<string, string> _packNames;
    private readonly IComputedContentSource? _computed;

    /// <summary>Raised after a successful voicing save/delete so the host can refresh the live voicing book (IN11).</summary>
    public event Action? VoicingsChanged;

    /// <param name="packNames">PackId → display-name map for source tagging (content-source-model IN2); empty if omitted.</param>
    /// <param name="computed">Optional computed (non-store) content source unioned into the list (IN8); none if omitted.</param>
    public ContentCrudHandler(
        DbContextOptions<ChordFlowDbContext> dbOptions,
        IScoreRenderer renderer,
        IReadOnlyDictionary<string, string>? packNames = null,
        IComputedContentSource? computed = null)
    {
        ArgumentNullException.ThrowIfNull(dbOptions);
        ArgumentNullException.ThrowIfNull(renderer);
        _dbOptions = dbOptions;
        _renderer = renderer;
        _packNames = packNames ?? new Dictionary<string, string>();
        _computed = computed;
    }

    /// <summary>
    /// List one entity type's definitions — every source shown (content-source-model): the store's package +
    /// user rows, each tagged with its source/packName, unioned with any computed (automatic) source (IN8).
    /// </summary>
    public EntityListEnvelope List(string entity)
    {
        ContentEntity kind = ContentEntities.Parse(entity);
        using var db = new ChordFlowDbContext(_dbOptions);
        var items = StoreFor(kind, db).List()
            .Select(ToItem)
            .ToList();
        if (_computed is not null)
        {
            items.AddRange(_computed.List(kind));
        }

        return new EntityListEnvelope(entity, items);
    }

    private ContentItem ToItem(ContentSummary s) => new(
        s.Id,
        s.Name,
        SourceLabel(s.Source),
        s.Source == ContentSource.Package ? PackName(s.PackId) : null,
        s.InitialKey,
        s.DefaultFeel,
        s.DefaultTempo);

    private static string SourceLabel(ContentSource source) => source switch
    {
        ContentSource.Package => "package",
        ContentSource.User => "user",
        ContentSource.Automatic => "automatic",
        _ => "user",
    };

    // PackId → display name (e.g. "default" → "ChordFlow Starter"); fall back to the id when unknown.
    private string? PackName(string? packId) =>
        packId is null ? null : (_packNames.TryGetValue(packId, out string? name) ? name : packId);

    /// <summary>Open one definition, or null if its id is unknown. An <c>automatic</c> voicing has no DB row —
    /// it resolves to a derived, read-only DSL (IN13) so the editor can show its grip + "Duplicate to user".</summary>
    public EntityLoadedEnvelope? Get(string entity, string id)
    {
        ContentEntity kind = ContentEntities.Parse(entity);
        if (kind == ContentEntity.Voicing && AutomaticVoicingDoc.DslFor(id) is { } autoDsl)
        {
            return new EntityLoadedEnvelope(entity, id, EngineVoicingSource.DisplayNameFor(id) ?? id, autoDsl);
        }

        using var db = new ChordFlowDbContext(_dbOptions);
        ContentDoc? doc = StoreFor(kind, db).Get(id);
        return doc is null ? null : new EntityLoadedEnvelope(entity, doc.Id, doc.Name, doc.Dsl);
    }

    /// <summary>Create/update a definition (UserDefined tier). Throws <see cref="FormatException"/> on invalid DSL.</summary>
    public EntitySavedEnvelope Save(string entity, string? id, string name, string dsl)
    {
        ContentEntity kind = ContentEntities.Parse(entity);
        using var db = new ChordFlowDbContext(_dbOptions);
        string savedId = StoreFor(kind, db).Save(id, name, dsl);
        if (kind == ContentEntity.Voicing)
        {
            VoicingsChanged?.Invoke();
        }

        return new EntitySavedEnvelope(entity, savedId);
    }

    /// <summary>Delete (or revert) a definition's UserDefined row.</summary>
    public EntityDeletedEnvelope Delete(string entity, string id)
    {
        ContentEntity kind = ContentEntities.Parse(entity);
        using var db = new ChordFlowDbContext(_dbOptions);
        DeleteOutcome outcome = StoreFor(kind, db).Delete(id);
        if (kind == ContentEntity.Voicing)
        {
            VoicingsChanged?.Invoke();
        }

        return new EntityDeletedEnvelope(entity, id, outcome.ToString());
    }

    /// <summary>
    /// Render a live preview of an unsaved DSL. Any failure throws <see cref="FormatException"/> (IN3). The
    /// progression/song preview comps with <paramref name="compingPatternId"/> resolved against the rhythm
    /// catalog (blank → the app default <c>beat_1_3</c>); rhythm/voicing previews ignore it.
    /// </summary>
    public EntityPreviewEnvelope Preview(string entity, string dsl, RenderOptions? options = null, TripletFeel tripletFeel = TripletFeel.None, string? compingPatternId = null, int? keyPitchClass = null, int? tempo = null)
    {
        ContentEntity kind = ContentEntities.Parse(entity);
        ArgumentNullException.ThrowIfNull(dsl);
        RenderOptions opts = options ?? RenderOptions.Default;

        // ScoreR-seeded render params (scorer-render-params IN7): the key the preview renders in (a live transpose)
        // and the tempo it carries. A null key means "no opinion" — a lifted progression/rhythm falls back to C,
        // but a Song keeps its OWN authored InitialKey (never forced to C). Absent tempo ⇒ the 80 preview default.
        Key? overrideKey = keyPitchClass is int pc ? new Key(new PitchClass(pc), IsMinor: false) : null;
        int previewTempo = tempo ?? PreviewTempo;

        try
        {
            using var db = new ChordFlowDbContext(_dbOptions);
            return kind switch
            {
                ContentEntity.Progression => ScorePreview(entity, ProgressionPreview(dsl, tripletFeel, ResolveComping(compingPatternId, db), overrideKey ?? PreviewKey, previewTempo), db, opts),
                ContentEntity.Rhythm => ScorePreview(entity, RhythmPreview(dsl, tripletFeel, overrideKey ?? PreviewKey, previewTempo), db, opts),
                ContentEntity.Song => SongPreview(entity, dsl, db, opts, tripletFeel, ResolveComping(compingPatternId, db), overrideKey, previewTempo),
                ContentEntity.Voicing => VoicingPreview(entity, dsl),
                _ => throw new FormatException($"Cannot preview entity \"{entity}\"."),
            };
        }
        catch (FormatException)
        {
            throw; // already a located, user-facing message
        }
        catch (Exception ex)
        {
            // Expansion/render failures (missing song ref, unrenderable chord, structural error) → one uniform surface.
            throw new FormatException(ex.Message, ex);
        }
    }

    // Expansion (the one I/O seam) runs through ExerciseRendering against the live db's progression store,
    // so a progression/rhythm preview goes down the exact same path a saved exercise renders through.
    private EntityPreviewEnvelope ScorePreview(string entity, Exercise exercise, ChordFlowDbContext db, RenderOptions options) =>
        new(entity, "score",
            ExerciseRendering.RenderToTex(exercise, new ProgressionStore(db), _renderer, StoredVoicingSource.From(new VoicingStore(db)), options),
            exercise.Tempo);

    // Resolve the chosen comping id → RhythmPattern via the shared seam (also used by generate/load); a blank id
    // falls back to the app default beat_1_3, an unknown non-blank id fails loud (→ entityParseError, IN6).
    private static RhythmPattern ResolveComping(string? compingPatternId, ChordFlowDbContext db) =>
        ExerciseRefs.ResolvePattern(string.IsNullOrWhiteSpace(compingPatternId) ? "beat_1_3" : compingPatternId, db);

    private static Exercise ProgressionPreview(string dsl, TripletFeel tripletFeel, RhythmPattern comping, Key liftKey, int tempo)
    {
        Progression progression = ProgressionParser.Parse("preview", "Preview", dsl, TimeSignature.FourFour);
        return new Exercise(
            Song.OfProgression(progression, liftKey), comping, Lead: null, KeyOverride: null,
            tempo, Difficulty.Beginner, tripletFeel);
    }

    private static Exercise RhythmPreview(string dsl, TripletFeel tripletFeel, Key liftKey, int tempo)
    {
        // Preview a bare rhythm on a single I chord so the focus is the timing, not the harmony.
        Progression oneChord = ProgressionParser.Parse("preview", "Preview", "1", TimeSignature.FourFour);
        RhythmPattern rhythm = RhythmPatternParser.Parse("preview", "Preview", dsl, TimeSignature.FourFour);
        return new Exercise(
            Song.OfProgression(oneChord, liftKey), rhythm, Lead: null, KeyOverride: null,
            tempo, Difficulty.Beginner, tripletFeel);
    }

    // startKey null ⇒ the Song renders in its OWN authored InitialKey (the preview's no-key default); a supplied
    // key transposes it live (scorer-render-params IN4). tempo drives the rendered \tempo so playback matches the seed.
    private EntityPreviewEnvelope SongPreview(string entity, string dsl, ChordFlowDbContext db, RenderOptions options, TripletFeel tripletFeel, RhythmPattern comping, Key? startKey, int tempo)
    {
        Song song = SongParser.Parse("preview", "Preview", dsl, TimeSignature.FourFour);
        RealizedSong realized = SongExpander.Expand(song, new ProgressionStore(db), startKey);
        CompingPlan plan = CompingResolver.Resolve(realized, options.VoicingOrDefault, StoredVoicingSource.From(new VoicingStore(db)));
        string tex = _renderer.Render(realized, comping, tempo, Difficulty.Beginner, plan, tripletFeel, options: options).Tex;
        return new EntityPreviewEnvelope(entity, "score", tex, tempo);
    }

    private static EntityPreviewEnvelope VoicingPreview(string entity, string dsl)
    {
        // Validate + canonicalize, then compute the fret-box model in Core (IN6); JS only draws it.
        VoicingShape shape = VoicingDslParser.Parse(CatalogHeader.Parse(dsl).Body);
        return new EntityPreviewEnvelope(entity, "diagram", Diagram: VoicingDiagram.Build(shape));
    }

    private static IContentStore StoreFor(ContentEntity kind, ChordFlowDbContext db) => kind switch
    {
        ContentEntity.Progression => new ProgressionStore(db),
        ContentEntity.Song => new SongStore(db),
        ContentEntity.Rhythm => new RhythmPatternStore(db),
        ContentEntity.Voicing => new VoicingStore(db),
        _ => throw new FormatException($"Unknown content entity \"{kind}\"."),
    };
}
