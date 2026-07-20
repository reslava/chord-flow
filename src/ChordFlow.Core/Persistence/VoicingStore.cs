using Microsoft.EntityFrameworkCore;

using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Progressions;

namespace ChordFlow.Persistence;

/// <summary>
/// DB-backed source of authored voicings — reconstructs <see cref="VoicingShape"/>s by parsing each row's
/// canonical-C <c>Dsl</c>. Lives in <c>Persistence/</c> (it touches the DB); <c>Domain/</c> stays I/O-free
/// (constraint C1). The stored <c>Dsl</c> is re-parsed on load — the same "store the definition, regenerate
/// on load" pattern as <see cref="RhythmPatternStore"/> (the realized frets are never stored). <see cref="LoadShapes"/>
/// is what the feature seam hands to a <see cref="VoicingBook"/>; <see cref="Find"/> resolves a single row.
/// </summary>
public sealed class VoicingStore : IContentStore
{
    private readonly ChordFlowDbContext _db;

    public VoicingStore(ChordFlowDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ContentSummary> List() =>
        // Catalog metadata reads straight from the denormalized columns — no header parse on the list path at all
        // now (content-list-reads-columns IN1/IN3): a voicing carries only genre/subgenre/tags, all three columns.
        ContentSummaries.Build(_db.Voicings.AsNoTracking()
            .Select(v => new { v.Id, v.Name, v.Origin, v.PackId, v.Genre, v.Subgenre, v.Tags }).ToList()
            .Select(v => (v.Id, v.Name, v.Origin, v.PackId, v.Genre, v.Subgenre, CatalogHeader.DeserializeTags(v.Tags))));

    /// <inheritdoc/>
    public ContentDoc? Get(string id)
    {
        List<Entities.VoicingEntity> rows = _db.Voicings.AsNoTracking().Where(v => v.Id == id).ToList();
        Entities.VoicingEntity? row = OriginResolver.ResolveOne(rows, id);
        // The editor authors the voicing line; strip any catalog header (metadata editing is EX3).
        return row is null ? null : new ContentDoc(row.Id, row.Name, StripHeader(row.Dsl));
    }

    /// <inheritdoc/>
    public string Save(string? id, string name, string dsl, string? sourceId = null, Tonality? tonality = null, CatalogMetadataPatch? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(dsl);

        // A voicing has no tonality — the explicit tonality is inert (only ProgressionStore acts on it, C4).
        _ = tonality;
        // Validate AND canonicalize: any authoring anchor folds to the stored canonical-C form (IN9).
        VoicingShape shape = VoicingDslParser.Parse(StripHeader(dsl)); // throws FormatException on bad input
        string canonicalDsl = VoicingDslWriter.ToDsl(shape);

        // User-only, fork-on-edit (content-source-model): update an existing user row in place; a blank id or
        // a non-user id (e.g. editing a package item) forks a new user row with a fresh id — never a shadow.
        Entities.VoicingEntity? row = string.IsNullOrWhiteSpace(id) ? null : _db.Voicings.Find(id, Origin.UserDefined);
        string targetId = row?.Id ?? Guid.NewGuid().ToString();

        // The preserved header is the baseline: the in-place row's own, else the forked-from source's. The
        // editor's authoritative genre/subgenre/tags patch (content-metadata-editing IN5) overlays those three
        // fields, keeping description + tonality (C4); no patch ⇒ preserve verbatim.
        CatalogMetadata preserved = row is not null
            ? CatalogHeader.Parse(row.Dsl).Metadata
            : SourceMetadata(sourceId ?? id);
        CatalogMetadata meta = metadata is not null ? metadata.ApplyTo(preserved) : preserved;
        string storedDsl = CatalogHeader.Serialize(meta, canonicalDsl);

        if (row is null)
        {
            _db.Voicings.Add(new Entities.VoicingEntity
            {
                Id = targetId,
                Name = name,
                Dsl = storedDsl,
                Genre = meta.Genre,
                Subgenre = meta.Subgenre,
                Tags = CatalogHeader.SerializeTags(meta.Tags),
                Origin = Origin.UserDefined,
                CreatedUtc = DateTime.UtcNow,
            });
        }
        else
        {
            row.Name = name;
            row.Dsl = storedDsl;
            row.Genre = meta.Genre;
            row.Subgenre = meta.Subgenre;
            row.Tags = CatalogHeader.SerializeTags(meta.Tags);
        }

        _db.SaveChanges();
        return targetId;
    }

    // The catalog metadata to preserve when the editor didn't (and can't, EX3) carry it: resolve the source
    // definition (across origins) and read its header. Empty when there is no source, or it carries no header.
    private CatalogMetadata SourceMetadata(string? sourceKey)
    {
        if (string.IsNullOrWhiteSpace(sourceKey))
        {
            return CatalogMetadata.Empty;
        }

        List<Entities.VoicingEntity> rows = _db.Voicings.AsNoTracking().Where(v => v.Id == sourceKey).ToList();
        Entities.VoicingEntity? src = OriginResolver.ResolveOne(rows, sourceKey);
        return src is null ? CatalogMetadata.Empty : CatalogHeader.Parse(src.Dsl).Metadata;
    }

    /// <inheritdoc/>
    public DeleteOutcome Delete(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        Entities.VoicingEntity? row = _db.Voicings.Find(id, Origin.UserDefined);
        if (row is null)
        {
            return DeleteOutcome.NotFound;
        }

        _db.Voicings.Remove(row);
        _db.SaveChanges();
        return DeleteOutcome.Deleted;
    }

    /// <summary>
    /// Every stored voicing parsed into a <see cref="VoicingShape"/> — the tier-collapsed authored library.
    /// Under the composite (Id, Origin) PK each id may have tiered rows; the highest tier per id wins
    /// (UserDefined > Pack > BuiltIn, IN3), so one shape per id reaches the caller. (The comping path now reads
    /// <see cref="LoadShapesBySource"/>; this remains the tier-collapsed read used by persistence round-trips.)
    /// </summary>
    public IReadOnlyList<VoicingShape> LoadShapes()
    {
        List<Entities.VoicingEntity> rows = _db.Voicings.AsNoTracking().OrderBy(v => v.Id).ToList();
        return OriginResolver.Resolve(rows)
            .Select(v => VoicingDslParser.Parse(StripHeader(v.Dsl)))
            .ToList();
    }

    /// <summary>
    /// Every stored voicing parsed into a <see cref="VoicingShape"/> tagged with its content source (package
    /// or user) and pack id — no tier collapse (content-source-model). The source-aware library the comping
    /// resolver draws its package and user candidates from (engine-derived-as-app-source IN4); the engine
    /// <c>automatic</c> source is computed elsewhere and never appears here.
    /// </summary>
    public IReadOnlyList<(VoicingShape Shape, ContentSource Source, string? PackId)> LoadShapesBySource() =>
        _db.Voicings.AsNoTracking().OrderBy(v => v.Id).ToList()
            .Select(v => (
                Shape: VoicingDslParser.Parse(StripHeader(v.Dsl)),
                Source: ContentSummaries.SourceOf(v.Origin),
                v.PackId))
            .ToList();

    /// <summary>
    /// Every stored voicing parsed into a <see cref="VoicingShape"/> tagged with its <b>id</b>, content source,
    /// and pack id — the id-carrying peer of <see cref="LoadShapesBySource"/> that the explicit-voicing
    /// reference resolver (<c>IVoicingReferenceSource</c>) needs to resolve a <c>{u: id}</c>/<c>{pkg: id}</c>
    /// reference. No tier collapse (a user and a package row of the same id are distinct reference targets).
    /// </summary>
    public IReadOnlyList<(string Id, VoicingShape Shape, ContentSource Source, string? PackId)> LoadShapesWithIds() =>
        _db.Voicings.AsNoTracking().OrderBy(v => v.Id).ToList()
            .Select(v => (
                v.Id,
                Shape: VoicingDslParser.Parse(StripHeader(v.Dsl)),
                Source: ContentSummaries.SourceOf(v.Origin),
                v.PackId))
            .ToList();

    /// <summary>Find a stored voicing by id (resolving the highest tier) and parse it into a <see cref="VoicingShape"/>, or null if absent.</summary>
    public VoicingShape? Find(string id)
    {
        List<Entities.VoicingEntity> rows = _db.Voicings.AsNoTracking().Where(v => v.Id == id).ToList();
        Entities.VoicingEntity? row = OriginResolver.ResolveOne(rows, id);
        return row is null ? null : VoicingDslParser.Parse(StripHeader(row.Dsl));
    }

    // A voicing row's DSL may carry a leading catalog header (genre/subgenre/tags); the VoicingDslParser only
    // ever sees the voicing grammar, matching the progression/song load path (constraint C1).
    private static string StripHeader(string dsl)
    {
        (_, string body) = CatalogHeader.Parse(dsl);
        return body;
    }
}
