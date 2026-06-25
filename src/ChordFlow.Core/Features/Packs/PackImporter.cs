using ChordFlow.Persistence;
using ChordFlow.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChordFlow.Features.Packs;

/// <summary>
/// Imports a loaded <see cref="ContentPack"/> into the SQLite store (IN5, C2) — a <c>Features/</c> concern,
/// Desktop → Core unchanged. Every imported definition is stamped <see cref="Origin.Pack"/> with the
/// manifest's id as its <c>PackId</c> (content-source-model: there is no special "built-in" tier — the
/// default pack is just a package). Each definition is upserted by its <b>(Id, Origin)</b> composite key, so
/// the import is <b>idempotent</b>: re-importing the same pack changes nothing new, and an updated definition
/// replaces its same-tier row (no duplicates). Catalog metadata is denormalized from each DSL header into
/// entity columns; the canonical header stays in the stored DSL.
///
/// <para><b>Referential integrity (IN8)</b> is fail-loud at <i>realize</i> time: a song that references a
/// missing progression throws when <c>SongExpander</c> resolves it (same rule as any Song→Progression ref).
/// The importer adds no separate pre-validation pass — it just must not swallow that throw.</para>
/// </summary>
public sealed class PackImporter
{
    private readonly ChordFlowDbContext _db;

    public PackImporter(ChordFlowDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    /// <summary>
    /// Import every definition in <paramref name="pack"/>, stamping <see cref="Origin.Pack"/> with the
    /// manifest id as the <c>PackId</c>. Returns the number of definitions upserted. Saves once.
    /// </summary>
    public int Import(ContentPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);

        string packId = pack.Manifest.Id;

        foreach (PackDefinition def in pack.Definitions)
        {
            switch (def.Kind)
            {
                case ContentKind.Progression:
                    UpsertCatalog(_db.Progressions, def, packId, NewProgression);
                    break;
                case ContentKind.Song:
                    UpsertCatalog(_db.Songs, def, packId, NewSong);
                    break;
                case ContentKind.Rhythm:
                    UpsertRhythm(def, packId);
                    break;
                case ContentKind.Voicing:
                    UpsertCatalog(_db.Voicings, def, packId, NewVoicing);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(pack), def.Kind, "Unknown content kind.");
            }
        }

        // Reconcile (IN12): a pack is authoritative for its own content, so drop the rows this pack previously
        // imported that it no longer ships (e.g. voicings relocated out of the pack) — keyed by (Origin.Pack,
        // PackId). User copies are forked with fresh ids (never Origin.Pack), so they are untouched.
        Dictionary<ContentKind, HashSet<string>> shipped = pack.Definitions
            .GroupBy(d => d.Kind)
            .ToDictionary(g => g.Key, g => g.Select(d => d.Id).ToHashSet(StringComparer.Ordinal));
        ReconcileOrphans(_db.Progressions, packId, Shipped(shipped, ContentKind.Progression), e => e.PackId);
        ReconcileOrphans(_db.Songs, packId, Shipped(shipped, ContentKind.Song), e => e.PackId);
        ReconcileOrphans(_db.RhythmPatterns, packId, Shipped(shipped, ContentKind.Rhythm), e => e.PackId);
        ReconcileOrphans(_db.Voicings, packId, Shipped(shipped, ContentKind.Voicing), e => e.PackId);

        _db.SaveChanges();
        return pack.Definitions.Count;
    }

    private static HashSet<string> Shipped(Dictionary<ContentKind, HashSet<string>> map, ContentKind kind) =>
        map.TryGetValue(kind, out HashSet<string>? ids) ? ids : new HashSet<string>(StringComparer.Ordinal);

    // Remove this pack's previously-imported rows (Origin.Pack, same PackId) whose id is no longer shipped.
    private void ReconcileOrphans<TEntity>(
        DbSet<TEntity> set, string packId, HashSet<string> shippedIds, Func<TEntity, string?> packIdOf)
        where TEntity : class, IOriginated
    {
        List<TEntity> orphans = set.Where(e => e.Origin == Origin.Pack).ToList()
            .Where(e => packIdOf(e) == packId && !shippedIds.Contains(e.Id))
            .ToList();
        if (orphans.Count > 0)
        {
            set.RemoveRange(orphans);
        }
    }

    // Upsert a catalog entity (progression / song / voicing): denormalize the DSL header into the columns,
    // insert a new (Id, Pack) row or update the existing one. The factory builds the right type.
    private void UpsertCatalog<TEntity>(
        DbSet<TEntity> set, PackDefinition def, string packId,
        Func<PackDefinition, string, CatalogMetadata, TEntity> create)
        where TEntity : class, ICatalogEntity
    {
        (CatalogMetadata meta, _) = CatalogHeader.Parse(def.Dsl);
        // Key lookup by the composite (Id, Origin) PK — avoids translating an interface-typed predicate.
        TEntity? row = set.Find(def.Id, Origin.Pack);
        if (row is null)
        {
            set.Add(create(def, packId, meta));
        }
        else
        {
            row.Name = def.Name;
            row.Dsl = def.Dsl;
            row.PackId = packId;
            row.Genre = meta.Genre;
            row.Subgenre = meta.Subgenre;
            row.Tags = CatalogHeader.SerializeTags(meta.Tags);
        }
    }

    private void UpsertRhythm(PackDefinition def, string packId)
    {
        // Rhythm carries no catalog metadata (EX3); meter defaults to 4/4 (a future `ts:` line is additive).
        RhythmPatternEntity? row = _db.RhythmPatterns.Find(def.Id, Origin.Pack);
        if (row is null)
        {
            _db.RhythmPatterns.Add(new RhythmPatternEntity
            {
                Id = def.Id,
                Name = def.Name,
                Dsl = def.Dsl,
                Origin = Origin.Pack,
                PackId = packId,
                CreatedUtc = DateTime.UtcNow,
            });
        }
        else
        {
            row.Name = def.Name;
            row.Dsl = def.Dsl;
            row.PackId = packId;
        }
    }

    private static ProgressionEntity NewProgression(PackDefinition def, string packId, CatalogMetadata meta) =>
        new()
        {
            Id = def.Id,
            Name = def.Name,
            Dsl = def.Dsl,
            Origin = Origin.Pack,
            PackId = packId,
            Genre = meta.Genre,
            Subgenre = meta.Subgenre,
            Tags = CatalogHeader.SerializeTags(meta.Tags),
            CreatedUtc = DateTime.UtcNow,
        };

    private static SongEntity NewSong(PackDefinition def, string packId, CatalogMetadata meta) =>
        new()
        {
            Id = def.Id,
            Name = def.Name,
            Dsl = def.Dsl,
            Origin = Origin.Pack,
            PackId = packId,
            Genre = meta.Genre,
            Subgenre = meta.Subgenre,
            Tags = CatalogHeader.SerializeTags(meta.Tags),
            CreatedUtc = DateTime.UtcNow,
        };

    private static VoicingEntity NewVoicing(PackDefinition def, string packId, CatalogMetadata meta) =>
        new()
        {
            Id = def.Id,
            Name = def.Name,
            Dsl = def.Dsl,
            Origin = Origin.Pack,
            PackId = packId,
            Genre = meta.Genre,
            Subgenre = meta.Subgenre,
            Tags = CatalogHeader.SerializeTags(meta.Tags),
            CreatedUtc = DateTime.UtcNow,
        };
}
