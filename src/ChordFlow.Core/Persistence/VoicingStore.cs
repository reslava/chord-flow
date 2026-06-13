using ChordFlow.Domain;
using Microsoft.EntityFrameworkCore;

namespace ChordFlow.Persistence;

/// <summary>
/// DB-backed source of authored voicings — reconstructs <see cref="VoicingShape"/>s by parsing each row's
/// canonical-C <c>Dsl</c>. Lives in <c>Persistence/</c> (it touches the DB); <c>Domain/</c> stays I/O-free
/// (constraint C1). The stored <c>Dsl</c> is re-parsed on load — the same "store the definition, regenerate
/// on load" pattern as <see cref="RhythmPatternStore"/> (the realized frets are never stored). <see cref="LoadShapes"/>
/// is what the feature seam hands to a <see cref="VoicingBook"/>; <see cref="Find"/> resolves a single row.
/// </summary>
public sealed class VoicingStore
{
    private readonly ChordFlowDbContext _db;

    public VoicingStore(ChordFlowDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
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
