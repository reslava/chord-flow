using ChordFlow.Domain;
using ChordFlow.Features;
using ChordFlow.Persistence;
using ChordFlow.Rendering;
using Microsoft.EntityFrameworkCore;

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

    /// <summary>Raised after a successful voicing save/delete so the host can refresh the live voicing book (IN11).</summary>
    public event Action? VoicingsChanged;

    public ContentCrudHandler(DbContextOptions<ChordFlowDbContext> dbOptions, IScoreRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(dbOptions);
        ArgumentNullException.ThrowIfNull(renderer);
        _dbOptions = dbOptions;
        _renderer = renderer;
    }

    /// <summary>List one entity type's definitions (resolved winning tier per id).</summary>
    public EntityListEnvelope List(string entity)
    {
        ContentEntity kind = ContentEntities.Parse(entity);
        using var db = new ChordFlowDbContext(_dbOptions);
        var items = StoreFor(kind, db).List()
            .Select(s => new ContentItem(s.Id, s.Name, s.Origin.ToString(), s.HasLowerTier))
            .ToList();
        return new EntityListEnvelope(entity, items);
    }

    /// <summary>Open one definition for editing, or null if its id is unknown.</summary>
    public EntityLoadedEnvelope? Get(string entity, string id)
    {
        ContentEntity kind = ContentEntities.Parse(entity);
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

    /// <summary>Render a live preview of an unsaved DSL. Any failure throws <see cref="FormatException"/> (IN3).</summary>
    public EntityPreviewEnvelope Preview(string entity, string dsl, RenderOptions? options = null)
    {
        ContentEntity kind = ContentEntities.Parse(entity);
        ArgumentNullException.ThrowIfNull(dsl);
        RenderOptions opts = options ?? RenderOptions.Default;

        try
        {
            using var db = new ChordFlowDbContext(_dbOptions);
            return kind switch
            {
                ContentEntity.Progression => ScorePreview(entity, ProgressionPreview(dsl), db, opts),
                ContentEntity.Rhythm => ScorePreview(entity, RhythmPreview(dsl), db, opts),
                ContentEntity.Song => SongPreview(entity, dsl, db, opts),
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
        new(entity, "score", ExerciseRendering.RenderToTex(exercise, new ProgressionStore(db), _renderer, options), exercise.Tempo);

    private static Exercise ProgressionPreview(string dsl)
    {
        Progression progression = ProgressionParser.Parse("preview", "Preview", dsl, TimeSignature.FourFour);
        return new Exercise(
            Song.OfProgression(progression, PreviewKey), SeedData.Quarters, Lead: null, KeyOverride: null,
            PreviewTempo, Difficulty.Beginner);
    }

    private static Exercise RhythmPreview(string dsl)
    {
        // Preview a bare rhythm on a single I chord so the focus is the timing, not the harmony.
        Progression oneChord = ProgressionParser.Parse("preview", "Preview", "1", TimeSignature.FourFour);
        RhythmPattern rhythm = RhythmPatternParser.Parse("preview", "Preview", dsl, TimeSignature.FourFour);
        return new Exercise(
            Song.OfProgression(oneChord, PreviewKey), rhythm, Lead: null, KeyOverride: null,
            PreviewTempo, Difficulty.Beginner);
    }

    private EntityPreviewEnvelope SongPreview(string entity, string dsl, ChordFlowDbContext db, RenderOptions options)
    {
        Song song = SongParser.Parse("preview", "Preview", dsl, TimeSignature.FourFour);
        RealizedSong realized = SongExpander.Expand(song, new ProgressionStore(db));
        string tex = _renderer.Render(realized, SeedData.Quarters, PreviewTempo, Difficulty.Beginner, options: options);
        return new EntityPreviewEnvelope(entity, "score", tex, PreviewTempo);
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
