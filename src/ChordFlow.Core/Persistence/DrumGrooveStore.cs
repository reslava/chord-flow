using ChordFlow.Instruments.Drums;
using ChordFlow.Music.Progressions;
using ChordFlow.Music.Rhythm;
using ChordFlow.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChordFlow.Persistence;

/// <summary>
/// DB-backed store of drum grooves — the 5th <see cref="IContentStore"/> (req IN6). Mirrors
/// <see cref="ProgressionStore"/> for catalog-header handling (grooves are genre-tagged) and
/// <see cref="RhythmPatternStore"/> for the stored meter, persisting only the hit-grid <c>Dsl</c> string
/// (alphaTex and the parsed lanes are never stored — regenerated on load). Load = strip header →
/// <c>DrumGrooveParser.Parse(body, ts)</c>. Lives in <c>Persistence/</c> (it touches the DB; the allowed
/// <c>Persistence → Instruments</c> edge); the drum types stay in <c>Instruments/Drums/</c> (req C1).
/// </summary>
public sealed class DrumGrooveStore : IContentStore
{
    private readonly ChordFlowDbContext _db;

    public DrumGrooveStore(ChordFlowDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ContentSummary> List() =>
        ContentSummaries.Build(_db.DrumGrooves.AsNoTracking()
            .Select(g => new { g.Id, g.Name, g.Origin, g.PackId }).ToList()
            .Select(g => (g.Id, g.Name, g.Origin, g.PackId)));

    /// <inheritdoc/>
    public ContentDoc? Get(string id)
    {
        List<DrumGrooveEntity> rows = _db.DrumGrooves.AsNoTracking().Where(g => g.Id == id).ToList();
        DrumGrooveEntity? row = OriginResolver.ResolveOne(rows, id);
        if (row is null)
        {
            return null;
        }

        // The editor authors the grid; strip any catalog header (metadata editing is EX3, like progressions).
        (_, string body) = CatalogHeader.Parse(row.Dsl);
        return new ContentDoc(row.Id, row.Name, body);
    }

    /// <inheritdoc/>
    public string Save(string? id, string name, string dsl, string? sourceId = null, Tonality? tonality = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(dsl);

        _ = tonality; // grooves have no tonality/mode (percussion has no harmony); accepted inertly.
        var ts = TimeSignature.FourFour; // 4/4 only today (req C8)
        (_, string body) = CatalogHeader.Parse(dsl); // drop any typed header (EX3), validate the body below.

        // User-only, fork-on-edit (content-source-model): update an existing user row in place; a blank id or a
        // non-user id (editing a pack groove) forks a new user row with a fresh id — never a same-id shadow.
        DrumGrooveEntity? row = string.IsNullOrWhiteSpace(id) ? null : _db.DrumGrooves.Find(id, Origin.UserDefined);
        string targetId = row?.Id ?? Guid.NewGuid().ToString();
        DrumGrooveParser.Parse(targetId, name, body, ts); // throws FormatException on bad input — writes nothing

        // Preserve catalog metadata (genre/tags) — not edited here (EX3) but not destroyed: the in-place row's
        // own header, else the forked-from source's. No header ⇒ body stored verbatim.
        CatalogMetadata meta = row is not null
            ? CatalogHeader.Parse(row.Dsl).Metadata
            : SourceMetadata(sourceId ?? id);
        string storedDsl = CatalogHeader.Serialize(meta, body);

        if (row is null)
        {
            _db.DrumGrooves.Add(new DrumGrooveEntity
            {
                Id = targetId,
                Name = name,
                Dsl = storedDsl,
                TsNumerator = ts.Numerator,
                TsDenominator = ts.Denominator,
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

    // The catalog metadata to preserve when the editor didn't (EX3): resolve the source groove (across origins)
    // and read its header. Empty when there is no source or it carries no header.
    private CatalogMetadata SourceMetadata(string? sourceKey)
    {
        if (string.IsNullOrWhiteSpace(sourceKey))
        {
            return CatalogMetadata.Empty;
        }

        List<DrumGrooveEntity> rows = _db.DrumGrooves.AsNoTracking().Where(g => g.Id == sourceKey).ToList();
        DrumGrooveEntity? src = OriginResolver.ResolveOne(rows, sourceKey);
        return src is null ? CatalogMetadata.Empty : CatalogHeader.Parse(src.Dsl).Metadata;
    }

    /// <inheritdoc/>
    public DeleteOutcome Delete(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        DrumGrooveEntity? row = _db.DrumGrooves.Find(id, Origin.UserDefined);
        if (row is null)
        {
            return DeleteOutcome.NotFound;
        }

        _db.DrumGrooves.Remove(row);
        _db.SaveChanges();
        return DeleteOutcome.Deleted;
    }

    /// <summary>Find a stored groove by id (highest tier) and parse it into a <see cref="DrumGroove"/>, or null.</summary>
    public DrumGroove? Find(string id)
    {
        List<DrumGrooveEntity> rows = _db.DrumGrooves.AsNoTracking().Where(g => g.Id == id).ToList();
        DrumGrooveEntity? row = OriginResolver.ResolveOne(rows, id);
        if (row is null)
        {
            return null;
        }

        var ts = new TimeSignature(row.TsNumerator, row.TsDenominator);
        (_, string body) = CatalogHeader.Parse(row.Dsl);
        return DrumGrooveParser.Parse(row.Id, row.Name, body, ts);
    }
}
