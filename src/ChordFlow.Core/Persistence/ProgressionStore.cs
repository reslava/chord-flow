using ChordFlow.Domain;
using ChordFlow.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChordFlow.Persistence;

/// <summary>
/// The DB-backed implementation of <see cref="IProgressionStore"/> — the concrete seam <see cref="SongExpander"/>
/// uses to resolve a <see cref="ProgressionReference"/> to its stored <see cref="Progression"/>. Lives in
/// <c>Persistence/</c> (it touches the DB); <c>Domain/</c> only ever sees the interface (constraint C3). The
/// stored <c>Dsl</c> is header-stripped and re-parsed on each lookup — the same "store the definition,
/// regenerate on load" pattern the rest of persistence uses (alphaTex/realized forms are never stored).
/// </summary>
public sealed class ProgressionStore : IProgressionStore, IContentStore
{
    private readonly ChordFlowDbContext _db;
    private readonly TimeSignature _ts;

    public ProgressionStore(ChordFlowDbContext db, TimeSignature? ts = null)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
        _ts = ts ?? TimeSignature.FourFour;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ContentSummary> List() =>
        ContentSummaries.Build(_db.Progressions.AsNoTracking()
            .Select(p => new { p.Id, p.Name, p.Origin }).ToList()
            .Select(p => (p.Id, p.Name, p.Origin)));

    /// <inheritdoc/>
    public ContentDoc? Get(string id)
    {
        List<ProgressionEntity> rows = _db.Progressions.AsNoTracking().Where(p => p.Id == id).ToList();
        ProgressionEntity? row = OriginResolver.ResolveOne(rows, id);
        if (row is null)
        {
            return null;
        }

        // The editor authors the bar grammar; strip any catalog header (metadata editing is EX3).
        (_, string body) = CatalogHeader.Parse(row.Dsl);
        return new ContentDoc(row.Id, row.Name, body);
    }

    /// <inheritdoc/>
    public string Save(string? id, string name, string dsl)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(dsl);

        // Drop any header the user typed (EX3 — metadata isn't edited here), validate the body by parsing.
        (_, string body) = CatalogHeader.Parse(dsl);
        string targetId = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString() : id;
        ProgressionParser.Parse(targetId, name, body, _ts); // throws FormatException on bad input — writes nothing

        ProgressionEntity? row = _db.Progressions.Find(targetId, Origin.UserDefined);
        if (row is null)
        {
            _db.Progressions.Add(new ProgressionEntity
            {
                Id = targetId,
                Name = name,
                Dsl = body,
                Origin = Origin.UserDefined,
                CreatedUtc = DateTime.UtcNow,
            });
        }
        else
        {
            row.Name = name;
            row.Dsl = body;
        }

        _db.SaveChanges();
        return targetId;
    }

    /// <inheritdoc/>
    public DeleteOutcome Delete(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        ProgressionEntity? row = _db.Progressions.Find(id, Origin.UserDefined);
        if (row is null)
        {
            return DeleteOutcome.NotFound;
        }

        _db.Progressions.Remove(row);
        _db.SaveChanges();
        return _db.Progressions.Any(p => p.Id == id) ? DeleteOutcome.Reverted : DeleteOutcome.Deleted;
    }

    public Progression? Find(string id)
    {
        // Under the composite (Id, Origin) PK a definition may have several tiered rows; resolve the
        // highest tier (UserDefined > Pack > BuiltIn) so locals shadow imported shadow built-in (IN3).
        List<Entities.ProgressionEntity> rows = _db.Progressions.AsNoTracking().Where(p => p.Id == id).ToList();
        Entities.ProgressionEntity? row = OriginResolver.ResolveOne(rows, id);
        if (row is null)
        {
            return null;
        }

        // The DSL may carry a catalog header; the pure ProgressionParser only ever sees the bar grammar.
        (_, string body) = CatalogHeader.Parse(row.Dsl);
        return ProgressionParser.Parse(row.Id, row.Name, body, _ts);
    }
}
