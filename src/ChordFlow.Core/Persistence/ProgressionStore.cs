using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Progressions;
using ChordFlow.Music.Songs;
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
    /// <remarks>The summary carries <see cref="ContentSummary.InitialKeyIsMinor"/> from the winning tier's
    /// <c>tonality:</c> header, so selecting a minor progression auto-picks minor mode (minor-mode-ui-threading IN4).</remarks>
    public IReadOnlyList<ContentSummary> List()
    {
        List<ProgressionEntity> rows = _db.Progressions.AsNoTracking().ToList();
        return ContentSummaries.Build(rows.Select(p => (p.Id, p.Name, p.Origin, p.PackId)))
            .Select(summary => summary with { InitialKeyIsMinor = IsMinorTonality(rows, summary.Id) })
            .ToList();
    }

    // The winning tier's tonality (IN4): resolve the top row for the id and read its `tonality:` header.
    // False (major) when there is no header or the winner is unresolved.
    private static bool IsMinorTonality(IReadOnlyList<ProgressionEntity> rows, string id)
    {
        ProgressionEntity? winner = OriginResolver.ResolveOne(rows.Where(r => r.Id == id).ToList(), id);
        return winner is not null && CatalogHeader.Parse(winner.Dsl).Metadata.Tonality == Tonality.Minor;
    }

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
    public string Save(string? id, string name, string dsl, string? sourceId = null, Tonality? tonality = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(dsl);

        // Drop any header the user typed (EX3 — metadata isn't edited here), validate the body by parsing.
        (_, string body) = CatalogHeader.Parse(dsl);

        // User-only, fork-on-edit (content-source-model): update an existing user row in place; a blank id or
        // a non-user id (e.g. editing a package item) forks a new user row with a fresh id — never a shadow.
        ProgressionEntity? row = string.IsNullOrWhiteSpace(id) ? null : _db.Progressions.Find(id, Origin.UserDefined);
        string targetId = row?.Id ?? Guid.NewGuid().ToString();
        ProgressionParser.Parse(targetId, name, body, _ts); // throws FormatException on bad input — writes nothing

        // Metadata isn't edited here (EX3) but must NOT be destroyed: carry the source header (incl. `tonality:`)
        // through — the in-place row's own metadata, else the forked-from source's — so a minor progression keeps
        // its tonality across fork/edit (else it silently misrealizes as major). No header ⇒ body stored verbatim.
        CatalogMetadata meta = row is not null
            ? CatalogHeader.Parse(row.Dsl).Metadata
            : SourceMetadata(sourceId ?? id);
        // The editor's tonality control is authoritative when it sends a value: override the preserved tonality
        // (authoring a new minor progression, or a major↔minor flip). Absent ⇒ keep the preserved source (C3).
        if (tonality is Tonality chosen)
        {
            meta = meta with { Tonality = chosen };
        }

        string storedDsl = CatalogHeader.Serialize(meta, body);

        if (row is null)
        {
            _db.Progressions.Add(new ProgressionEntity
            {
                Id = targetId,
                Name = name,
                Dsl = storedDsl,
                Origin = Origin.UserDefined,
                CreatedUtc = DateTime.UtcNow,
            });
        }
        else
        {
            row.Name = name;
            row.Dsl = storedDsl;
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

        List<ProgressionEntity> rows = _db.Progressions.AsNoTracking().Where(p => p.Id == sourceKey).ToList();
        ProgressionEntity? src = OriginResolver.ResolveOne(rows, sourceKey);
        return src is null ? CatalogMetadata.Empty : CatalogHeader.Parse(src.Dsl).Metadata;
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
        return DeleteOutcome.Deleted;
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

        // The DSL may carry a catalog header; the pure ProgressionParser only ever sees the bar grammar, but
        // the header's `tonality:` reaches it as the resolved Home so a minor progression's degrees convert to
        // the parent-major frame (first-class-minor-keys, IN10).
        (CatalogMetadata meta, string body) = CatalogHeader.Parse(row.Dsl);
        return ProgressionParser.Parse(row.Id, row.Name, body, _ts, home: meta.Tonality);
    }
}
