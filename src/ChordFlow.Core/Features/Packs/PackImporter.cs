using ChordFlow.Persistence;
using ChordFlow.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChordFlow.Features.Packs;

/// <summary>
/// Imports a loaded <see cref="ContentPack"/> into the SQLite store (IN5, C2) — a <c>Features/</c> concern,
/// Desktop → Core unchanged. Each definition is upserted by its <b>(Id, Origin)</b> composite key, so the
/// import is <b>idempotent</b>: re-importing the same pack changes nothing new, and an updated definition
/// replaces its same-tier row (no duplicates). The caller declares the tier to stamp (design D3):
/// <see cref="Origin.BuiltIn"/> for the default/starter pack, <see cref="Origin.Pack"/> for a third-party
/// bundle (whose <c>PackId</c> is the manifest id). Catalog metadata is denormalized from each DSL header
/// into entity columns exactly as first-run seeding does; the canonical header stays in the stored DSL.
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
    /// Import every definition in <paramref name="pack"/>, stamping <paramref name="origin"/>
    /// (<see cref="Origin.BuiltIn"/> or <see cref="Origin.Pack"/> — never <see cref="Origin.UserDefined"/>).
    /// Returns the number of definitions upserted. Saves once.
    /// </summary>
    public int Import(ContentPack pack, Origin origin)
    {
        ArgumentNullException.ThrowIfNull(pack);
        if (origin is not (Origin.BuiltIn or Origin.Pack))
        {
            throw new ArgumentException(
                $"a pack import stamps BuiltIn or Pack, not {origin}.", nameof(origin));
        }

        string? packId = origin == Origin.Pack ? pack.Manifest.Id : null;

        foreach (PackDefinition def in pack.Definitions)
        {
            switch (def.Kind)
            {
                case ContentKind.Progression:
                    UpsertCatalog(_db.Progressions, def, origin, packId, NewProgression);
                    break;
                case ContentKind.Song:
                    UpsertCatalog(_db.Songs, def, origin, packId, NewSong);
                    break;
                case ContentKind.Rhythm:
                    UpsertRhythm(def, origin, packId);
                    break;
                case ContentKind.Voicing:
                    UpsertCatalog(_db.Voicings, def, origin, packId, NewVoicing);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(pack), def.Kind, "Unknown content kind.");
            }
        }

        _db.SaveChanges();
        return pack.Definitions.Count;
    }

    // Upsert a catalog entity (progression / song / voicing): denormalize the DSL header into the columns,
    // insert a new (Id, Origin) row or update the existing same-tier one. The factory builds the right type.
    private void UpsertCatalog<TEntity>(
        DbSet<TEntity> set, PackDefinition def, Origin origin, string? packId,
        Func<PackDefinition, Origin, string?, CatalogMetadata, TEntity> create)
        where TEntity : class, ICatalogEntity
    {
        (CatalogMetadata meta, _) = CatalogHeader.Parse(def.Dsl);
        // Key lookup by the composite (Id, Origin) PK — avoids translating an interface-typed predicate.
        TEntity? row = set.Find(def.Id, origin);
        if (row is null)
        {
            set.Add(create(def, origin, packId, meta));
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

    private void UpsertRhythm(PackDefinition def, Origin origin, string? packId)
    {
        // Rhythm carries no catalog metadata (EX3); meter defaults to 4/4 (a future `ts:` line is additive).
        RhythmPatternEntity? row = _db.RhythmPatterns.Find(def.Id, origin);
        if (row is null)
        {
            _db.RhythmPatterns.Add(new RhythmPatternEntity
            {
                Id = def.Id,
                Name = def.Name,
                Dsl = def.Dsl,
                Origin = origin,
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

    private static ProgressionEntity NewProgression(PackDefinition def, Origin origin, string? packId, CatalogMetadata meta) =>
        new()
        {
            Id = def.Id,
            Name = def.Name,
            Dsl = def.Dsl,
            Origin = origin,
            PackId = packId,
            Genre = meta.Genre,
            Subgenre = meta.Subgenre,
            Tags = CatalogHeader.SerializeTags(meta.Tags),
            CreatedUtc = DateTime.UtcNow,
        };

    private static SongEntity NewSong(PackDefinition def, Origin origin, string? packId, CatalogMetadata meta) =>
        new()
        {
            Id = def.Id,
            Name = def.Name,
            Dsl = def.Dsl,
            Origin = origin,
            PackId = packId,
            Genre = meta.Genre,
            Subgenre = meta.Subgenre,
            Tags = CatalogHeader.SerializeTags(meta.Tags),
            CreatedUtc = DateTime.UtcNow,
        };

    private static VoicingEntity NewVoicing(PackDefinition def, Origin origin, string? packId, CatalogMetadata meta) =>
        new()
        {
            Id = def.Id,
            Name = def.Name,
            Dsl = def.Dsl,
            Origin = origin,
            PackId = packId,
            Genre = meta.Genre,
            Subgenre = meta.Subgenre,
            Tags = CatalogHeader.SerializeTags(meta.Tags),
            CreatedUtc = DateTime.UtcNow,
        };
}
