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

    /// <summary>Every stored voicing parsed into a <see cref="VoicingShape"/> — the authored library for a <see cref="VoicingBook"/>.</summary>
    public IReadOnlyList<VoicingShape> LoadShapes() =>
        _db.Voicings.AsNoTracking()
            .OrderBy(v => v.Id)
            .Select(v => v.Dsl)
            .AsEnumerable()
            .Select(VoicingDslParser.Parse)
            .ToList();

    /// <summary>Find a stored voicing by id and parse it into a <see cref="VoicingShape"/>, or null if absent.</summary>
    public VoicingShape? Find(string id)
    {
        Entities.VoicingEntity? row = _db.Voicings.AsNoTracking().FirstOrDefault(v => v.Id == id);
        return row is null ? null : VoicingDslParser.Parse(row.Dsl);
    }
}
