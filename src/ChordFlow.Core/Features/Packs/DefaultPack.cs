using ChordFlow.Persistence;

namespace ChordFlow.Features.Packs;

/// <summary>
/// The free starter content that ships with ChordFlow (IN6) — the curated progressions / songs / rhythms /
/// voicings imported on first run. It is an ordinary <see cref="ContentPack"/> bundle on disk
/// (<c>Content/default-pack/</c>, copied next to the engine assembly), imported through the same
/// <see cref="PackImporter"/> as any pack — there is no special-case seeding code, and (content-source-model)
/// no special provenance: its content is <see cref="Origin.Pack"/> with <c>PackId = "default"</c>, listed
/// under its manifest name like any package. Curating/growing that content is the
/// <c>packages/default-pack</c> thread; this is just the import <i>path</i>.
/// </summary>
public static class DefaultPack
{
    /// <summary>The default pack's manifest id — stamped as <c>PackId</c> on every imported row.</summary>
    public const string PackId = "default";

    /// <summary>The bundle directory, relative to the running engine assembly (host-agnostic).</summary>
    public static string Directory => Path.Combine(AppContext.BaseDirectory, "Content", "default-pack");

    /// <summary>Load the default bundle from <see cref="Directory"/>.</summary>
    public static ContentPack Load() => PackReader.ReadFromDirectory(Directory);

    /// <summary>
    /// Import the default bundle into <paramref name="db"/> as <see cref="Origin.Pack"/>, idempotently
    /// (safe to call every run — re-import upserts by the (Id, Origin) key). Returns the number of
    /// definitions imported.
    /// </summary>
    public static int ImportInto(ChordFlowDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        return new PackImporter(db).Import(Load());
    }
}
