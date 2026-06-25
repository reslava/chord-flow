using ChordFlow.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChordFlow.Features.Packs;

/// <summary>
/// One-time-but-idempotent data migration for the multi-source content model (content-source-model thread):
/// the legacy <c>BuiltIn</c> provenance tier was retired when the default pack became an ordinary package.
/// Run on startup <b>after</b> <see cref="DefaultPack.ImportInto"/> has re-imported the default content as
/// <see cref="Origin.Pack"/>. Two steps, both raw SQL (the <c>Origin</c> enum no longer has a <c>BuiltIn</c>
/// member, so EF can't materialize those legacy rows):
/// <list type="number">
/// <item>delete every legacy <c>Origin='BuiltIn'</c> row — its content now lives as a <c>Pack</c> row;</item>
/// <item>fork any <c>UserDefined</c> row whose id collides with a <c>Pack</c> row (a legacy shadow of a
///   built-in) into an independent user copy by re-iding it — so every listed item has a unique id
///   (fork-on-edit, IN4) and the package original is never hidden.</item>
/// </list>
/// Safe to call every launch: after the first run there are no <c>BuiltIn</c> rows and no id collisions, so
/// both statements are no-ops.
/// </summary>
public static class ContentSourceMigration
{
    private static readonly string[] ContentTables = { "Progressions", "Songs", "RhythmPatterns", "Voicings" };

    public static void Run(ChordFlowDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);

        // The only interpolated value is a table name from the hardcoded ContentTables constant (never user
        // input), so the EF SQL-injection analyzers (EF1002/EF1003) don't apply here.
#pragma warning disable EF1002, EF1003
        foreach (string table in ContentTables)
        {
            // (1) Retire legacy BuiltIn rows — the default content is re-imported as Pack.
            db.Database.ExecuteSqlRaw($"DELETE FROM {table} WHERE Origin = 'BuiltIn'");

            // (2) Fork legacy user shadows of a built-in into independent copies (unique ids).
            db.Database.ExecuteSqlRaw(
                $"UPDATE {table} SET Id = Id || ':user:' || lower(hex(randomblob(6))) " +
                $"WHERE Origin = 'UserDefined' AND Id IN (SELECT Id FROM {table} WHERE Origin = 'Pack')");
        }
#pragma warning restore EF1002, EF1003
    }
}
