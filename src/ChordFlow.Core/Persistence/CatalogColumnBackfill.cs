using ChordFlow.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChordFlow.Persistence;

/// <summary>
/// One-time-but-idempotent reconcile pass for the denormalized catalog columns (content-list-reads-columns
/// thread, IN4). Now that <see cref="IContentStore.List"/> reads genre/subgenre/tags from the
/// <see cref="ICatalogEntity"/> columns instead of parsing the DSL header, any row whose columns are stale —
/// chiefly legacy <see cref="Origin.UserDefined"/> rows saved before <c>content-metadata-editing</c> began
/// populating them — would drop its metadata from the lists. This pass reconciles <b>columns ← header</b> (the
/// header stays canonical — content-metadata-editing C2): for each catalog entity it parses the row's header
/// and, where a column disagrees, sets it from the header.
///
/// <para>Run on startup <b>after</b> <see cref="ChordFlow.Features.Packs.ContentSourceMigration.Run"/> (and the
/// default-pack import). Safe every launch: once every row's columns already match its header the pass writes
/// nothing (a no-op). Rhythm patterns carry no catalog metadata (EX3) and are skipped. Cheap at catalog scale
/// (dozens–hundreds of rows).</para>
/// </summary>
public static class CatalogColumnBackfill
{
    /// <summary>Reconcile every catalog entity's columns from its header; returns the number of rows updated.</summary>
    public static int Run(ChordFlowDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);

        Reconcile(db.Progressions);
        Reconcile(db.Songs);
        Reconcile(db.Voicings);
        Reconcile(db.DrumGrooves);

        return db.SaveChanges();
    }

    // Load the set tracked, parse each row's header, and overwrite the three columns only when they differ from
    // the canonical header value — so an already-consistent row is never marked modified (idempotence, C4).
    private static void Reconcile<T>(DbSet<T> set) where T : class, ICatalogEntity
    {
        foreach (T row in set)
        {
            CatalogMetadata meta = CatalogHeader.Parse(row.Dsl).Metadata;
            string tags = CatalogHeader.SerializeTags(meta.Tags);
            if (row.Genre != meta.Genre || row.Subgenre != meta.Subgenre || row.Tags != tags)
            {
                row.Genre = meta.Genre;
                row.Subgenre = meta.Subgenre;
                row.Tags = tags;
            }
        }
    }
}
