using ChordFlow.Domain;
using Microsoft.EntityFrameworkCore;

namespace ChordFlow.Persistence;

/// <summary>
/// DB-backed lookup of stored rhythm patterns — reconstructs a <see cref="RhythmPattern"/> by parsing the
/// row's canonical <c>Dsl</c> with its stored time signature. Lives in <c>Persistence/</c> (it touches the
/// DB); <c>Domain/</c> stays I/O-free (constraint C2). The stored <c>Dsl</c> is re-parsed on each lookup —
/// the same "store the definition, regenerate on load" pattern as <see cref="ProgressionStore"/> (the
/// parsed tick grid and alphaTex are never stored — C1). No catalog-header strip: rhythm patterns carry no
/// catalog metadata (EX3). Concrete (no Domain interface yet — nothing in <c>Domain/</c> resolves patterns
/// by id today; add the seam when a consumer needs it).
/// </summary>
public sealed class RhythmPatternStore : IContentStore
{
    private readonly ChordFlowDbContext _db;

    public RhythmPatternStore(ChordFlowDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ContentSummary> List() =>
        ContentSummaries.Build(_db.RhythmPatterns.AsNoTracking()
            .Select(p => new { p.Id, p.Name, p.Origin }).ToList()
            .Select(p => (p.Id, p.Name, p.Origin)));

    /// <inheritdoc/>
    public ContentDoc? Get(string id)
    {
        List<Entities.RhythmPatternEntity> rows = _db.RhythmPatterns.AsNoTracking().Where(p => p.Id == id).ToList();
        Entities.RhythmPatternEntity? row = OriginResolver.ResolveOne(rows, id);
        // Rhythm carries no catalog header (EX3) — the stored DSL is the editable body as-is.
        return row is null ? null : new ContentDoc(row.Id, row.Name, row.Dsl);
    }

    /// <inheritdoc/>
    public string Save(string? id, string name, string dsl)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(dsl);

        string targetId = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString() : id;
        var ts = TimeSignature.FourFour; // 4/4 only today (EX-meter); a future `ts:` line is additive
        RhythmPatternParser.Parse(targetId, name, dsl, ts); // throws FormatException on bad input — writes nothing

        Entities.RhythmPatternEntity? row = _db.RhythmPatterns.Find(targetId, Origin.UserDefined);
        if (row is null)
        {
            _db.RhythmPatterns.Add(new Entities.RhythmPatternEntity
            {
                Id = targetId,
                Name = name,
                Dsl = dsl,
                TsNumerator = ts.Numerator,
                TsDenominator = ts.Denominator,
                Origin = Origin.UserDefined,
                CreatedUtc = DateTime.UtcNow,
            });
        }
        else
        {
            row.Name = name;
            row.Dsl = dsl;
        }

        _db.SaveChanges();
        return targetId;
    }

    /// <inheritdoc/>
    public DeleteOutcome Delete(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        Entities.RhythmPatternEntity? row = _db.RhythmPatterns.Find(id, Origin.UserDefined);
        if (row is null)
        {
            return DeleteOutcome.NotFound;
        }

        _db.RhythmPatterns.Remove(row);
        _db.SaveChanges();
        return _db.RhythmPatterns.Any(p => p.Id == id) ? DeleteOutcome.Reverted : DeleteOutcome.Deleted;
    }

    /// <summary>Find a stored pattern by id and parse it into a <see cref="RhythmPattern"/>, or null if absent.</summary>
    public RhythmPattern? Find(string id)
    {
        // Composite (Id, Origin) PK: resolve the highest tier per id (UserDefined > Pack > BuiltIn), so a
        // locally-edited or pack pattern shadows the built-in without deleting it (IN3).
        List<Entities.RhythmPatternEntity> rows = _db.RhythmPatterns.AsNoTracking().Where(p => p.Id == id).ToList();
        Entities.RhythmPatternEntity? row = OriginResolver.ResolveOne(rows, id);
        if (row is null)
        {
            return null;
        }

        var ts = new TimeSignature(row.TsNumerator, row.TsDenominator);
        return RhythmPatternParser.Parse(row.Id, row.Name, row.Dsl, ts);
    }
}
