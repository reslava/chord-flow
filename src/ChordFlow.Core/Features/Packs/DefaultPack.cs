using ChordFlow.Persistence;

namespace ChordFlow.Features.Packs;

/// <summary>
/// The free starter content that ships with ChordFlow (IN6) — the curated progressions / songs / rhythms
/// (and, later, authored voicings) imported on first run. It is an ordinary <see cref="ContentPack"/>
/// bundle on disk (<c>Content/default-pack/</c>, copied next to the engine assembly), imported through the
/// same <see cref="PackImporter"/> as any pack — there is no special-case seeding code. The default pack's
/// content is provenance <see cref="Origin.BuiltIn"/> (design §2). Curating/growing that content (more
/// genres, the authored CAGED voicings) is the <c>packages/default-pack</c> thread; this is just the
/// import <i>path</i>.
/// </summary>
public static class DefaultPack
{
    /// <summary>The default pack's manifest id. Not stored on rows (BuiltIn rows carry a null <c>PackId</c>).</summary>
    public const string PackId = "default";

    /// <summary>The bundle directory, relative to the running engine assembly (host-agnostic).</summary>
    public static string Directory => Path.Combine(AppContext.BaseDirectory, "Content", "default-pack");

    /// <summary>Load the default bundle from <see cref="Directory"/>.</summary>
    public static ContentPack Load() => PackReader.ReadFromDirectory(Directory);

    /// <summary>
    /// Import the default bundle into <paramref name="db"/> as <see cref="Origin.BuiltIn"/>, idempotently
    /// (safe to call every run — re-import is a no-op by the importer's upsert-by-(Id, Origin)). Returns the
    /// number of definitions imported.
    /// </summary>
    public static int ImportInto(ChordFlowDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        return new PackImporter(db).Import(Load(), Origin.BuiltIn);
    }
}
