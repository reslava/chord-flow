using ChordFlow.Exercises;
using ChordFlow.Music.Harmony;
using ChordFlow.Music.Progressions;
using ChordFlow.Music.Rhythm;
using ChordFlow.Features.Packs;
using ChordFlow.Persistence;
using ChordFlow.Rendering;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The built-in progressions now ship in the on-disk default pack (<c>Content/default-pack/</c>) and are
/// imported as <see cref="Origin.BuiltIn"/> on first run (IN6). Each parses → realizes → renders, and the
/// import is idempotent.
/// </summary>
public class ProgressionSeedTests
{
    private static readonly AlphaTexRenderer Renderer = new();

    public static IEnumerable<object[]> BuiltIns() =>
        DefaultPack.Load().Definitions
            .Where(d => d.Kind == ContentKind.Progression)
            .Select(d => new object[] { d });

    [Theory]
    [MemberData(nameof(BuiltIns))]
    public void EveryDefaultProgression_RoundTripsDslToModelToRender(PackDefinition def)
    {
        // DSL → parser → transposer → renderer must succeed for each built-in (in a major key).
        (_, string body) = CatalogHeader.Parse(def.Dsl);
        Progression prog = ProgressionParser.Parse(def.Id, def.Name, body, TimeSignature.FourFour);

        string tex = Renderer.RenderProgression(
            new Key(new PitchClass(10), false), prog, SeedData.Quarters, 90, Difficulty.Beginner);

        Assert.StartsWith($"\\title \"{def.Name} — Bb\"", tex);
        Assert.Contains('|', tex);
    }

    [Fact]
    public void DefaultPackImport_SeedsProgressionsAsBuiltIn_AndIsIdempotent()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<ChordFlowDbContext>().UseSqlite(conn).Options;

        using var db = new ChordFlowDbContext(options);
        db.Database.Migrate();

        int progCount = DefaultPack.Load().Definitions.Count(d => d.Kind == ContentKind.Progression);

        DefaultPack.ImportInto(db);
        Assert.Equal(progCount, db.Progressions.Count());
        Assert.All(db.Progressions.AsNoTracking(), p => Assert.Equal(Origin.BuiltIn, p.Origin));
        Assert.All(db.Progressions.AsNoTracking(), p => Assert.Null(p.PackId));   // BuiltIn rows carry no PackId

        // Re-import changes nothing — idempotent upsert by (Id, Origin).
        DefaultPack.ImportInto(db);
        Assert.Equal(progCount, db.Progressions.Count());
    }
}
