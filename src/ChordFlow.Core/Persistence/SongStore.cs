using ChordFlow.Domain;
using ChordFlow.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChordFlow.Persistence;

/// <summary>
/// DB-backed CRUD for stored <see cref="Song"/> definitions — the content store that did not exist before the
/// content-CRUD thread (the <c>Songs</c> table + <see cref="SongEntity"/> + <see cref="SongParser"/> were
/// there, but nothing read or wrote songs by id). Mirrors <see cref="ProgressionStore"/>: the canonical Song
/// DSL is the only persisted form (alphaTex/realized songs are regenerated on load — C4), the stored <c>Dsl</c>
/// may carry a catalog header that the editor never sees (EX3), and writes target the <see cref="Origin.UserDefined"/>
/// tier only (C2). Full realization (resolving stored <c>ProgressionReference</c>s) needs an
/// <see cref="IProgressionStore"/> and lives in <see cref="SongExpander"/>; this store only validates the
/// arrangement grammar on save.
/// </summary>
public sealed class SongStore : IContentStore
{
    private readonly ChordFlowDbContext _db;
    private readonly TimeSignature _ts;

    public SongStore(ChordFlowDbContext db, TimeSignature? ts = null)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
        _ts = ts ?? TimeSignature.FourFour;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ContentSummary> List() =>
        ContentSummaries.Build(_db.Songs.AsNoTracking()
            .Select(s => new { s.Id, s.Name, s.Origin }).ToList()
            .Select(s => (s.Id, s.Name, s.Origin)));

    /// <inheritdoc/>
    public ContentDoc? Get(string id)
    {
        List<SongEntity> rows = _db.Songs.AsNoTracking().Where(s => s.Id == id).ToList();
        SongEntity? row = OriginResolver.ResolveOne(rows, id);
        if (row is null)
        {
            return null;
        }

        // The editor authors the arrangement grammar; strip any catalog header (metadata editing is EX3).
        (_, string body) = CatalogHeader.Parse(row.Dsl);
        return new ContentDoc(row.Id, row.Name, body);
    }

    /// <inheritdoc/>
    public string Save(string? id, string name, string dsl)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(dsl);

        (_, string body) = CatalogHeader.Parse(dsl);
        string targetId = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString() : id;
        Validate(targetId, name, body);

        SongEntity? row = _db.Songs.Find(targetId, Origin.UserDefined);
        if (row is null)
        {
            _db.Songs.Add(new SongEntity
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
        SongEntity? row = _db.Songs.Find(id, Origin.UserDefined);
        if (row is null)
        {
            return DeleteOutcome.NotFound;
        }

        _db.Songs.Remove(row);
        _db.SaveChanges();
        return _db.Songs.Any(s => s.Id == id) ? DeleteOutcome.Reverted : DeleteOutcome.Deleted;
    }

    // SongParser raises FormatException for grammar errors but Song.FromSections raises ArgumentException for
    // structural ones (no plays, unknown part, …). Normalize the latter to a FormatException so the CRUD
    // parse-error surface (IN3) sees one "invalid definition" exception type for every entity.
    private void Validate(string id, string name, string body)
    {
        try
        {
            SongParser.Parse(id, name, body, _ts);
        }
        catch (ArgumentException ex) when (ex is not ArgumentNullException)
        {
            throw new FormatException(ex.Message, ex);
        }
    }
}
