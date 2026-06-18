using ChordFlow.Domain;
using Microsoft.EntityFrameworkCore;

using ChordFlow.Instruments.Guitar;

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
        ContentSummaries.Build(_db.Voicings.AsNoTracking()
            .Select(v => new { v.Id, v.Name, v.Origin }).ToList()
            .Select(v => (v.Id, v.Name, v.Origin)));

    /// <inheritdoc/>
    public ContentDoc? Get(string id)
    {
        List<Entities.VoicingEntity> rows = _db.Voicings.AsNoTracking().Where(v => v.Id == id).ToList();
        Entities.VoicingEntity? row = OriginResolver.ResolveOne(rows, id);
        // The editor authors the voicing line; strip any catalog header (metadata editing is EX3).
        return row is null ? null : new ContentDoc(row.Id, row.Name, StripHeader(row.Dsl));
    }

    /// <inheritdoc/>
    public string Save(string? id, string name, string dsl)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(dsl);

        // Validate AND canonicalize: any authoring anchor folds to the stored canonical-C form (IN9).
        VoicingShape shape = VoicingDslParser.Parse(StripHeader(dsl)); // throws FormatException on bad input
        string canonicalDsl = VoicingDslWriter.ToDsl(shape);
        string targetId = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString() : id;

        Entities.VoicingEntity? row = _db.Voicings.Find(targetId, Origin.UserDefined);
        if (row is null)
        {
            _db.Voicings.Add(new Entities.VoicingEntity
            {
                Id = targetId,
                Name = name,
                Dsl = canonicalDsl,
                Origin = Origin.UserDefined,
                CreatedUtc = DateTime.UtcNow,
            });
        }
        else
        {
            row.Name = name;
            row.Dsl = canonicalDsl;
        }

        _db.SaveChanges();
        return targetId;
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
        return _db.Voicings.Any(v => v.Id == id) ? DeleteOutcome.Reverted : DeleteOutcome.Deleted;
    }

    /// <summary>
    /// Every stored voicing parsed into a <see cref="VoicingShape"/> — the authored library for a
    /// <see cref="VoicingBook"/>. Under the composite (Id, Origin) PK each id may have tiered rows; the
    /// highest tier per id wins (UserDefined > Pack > BuiltIn, IN3), so one shape per id reaches the book.
    /// </summary>
    public IReadOnlyList<VoicingShape> LoadShapes()
    {
        List<Entities.VoicingEntity> rows = _db.Voicings.AsNoTracking().OrderBy(v => v.Id).ToList();
        return OriginResolver.Resolve(rows)
            .Select(v => VoicingDslParser.Parse(StripHeader(v.Dsl)))
            .ToList();
    }

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
