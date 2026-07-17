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
/// The default progressions ship in the on-disk default pack (<c>Content/default-pack/</c>) and are imported
/// as <see cref="Origin.Pack"/> (PackId "default") on first run (IN6) — the default pack is an ordinary package
/// (content-source-model). Each parses → realizes → renders, and the import is idempotent.
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
    public void DefaultPackImport_SeedsProgressionsAsPack_AndIsIdempotent()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<ChordFlowDbContext>().UseSqlite(conn).Options;

        using var db = new ChordFlowDbContext(options);
        db.Database.Migrate();

        int progCount = DefaultPack.Load().Definitions.Count(d => d.Kind == ContentKind.Progression);

        DefaultPack.ImportInto(db);
        Assert.Equal(progCount, db.Progressions.Count());
        Assert.All(db.Progressions.AsNoTracking(), p => Assert.Equal(Origin.Pack, p.Origin));
        Assert.All(db.Progressions.AsNoTracking(), p => Assert.Equal("default", p.PackId)); // stamped with the default pack id

        // Re-import changes nothing — idempotent upsert by (Id, Origin).
        DefaultPack.ImportInto(db);
        Assert.Equal(progCount, db.Progressions.Count());
    }

    // first-class-minor-keys dogfood (minor-progressions set): each authored minor-home progression, loaded
    // from the pack file, parses with its `tonality: minor` header and realizes in A minor to the intended
    // sounding chords — including the letter-pure raised roots (no A♭/G♭). This is the peer of the major
    // set's round-trip coverage, but asserting the realized harmony, not just that it renders.
    [Theory]
    [InlineData("minor_ii_v_i", "Bm7b5 E7 Am")]
    [InlineData("andalusian_cadence", "Am G F E")]
    [InlineData("natural_minor_i_iv_v", "Am Dm Em")]
    [InlineData("harmonic_minor_i_iv_v", "Am Dm E")]
    [InlineData("minor_turnaround", "Am F Bm7b5 E")]
    [InlineData("aeolian_loop", "Am F G Am")]
    [InlineData("picardy_cadence", "Am Dm E A")]
    [InlineData("minor_12bar_blues", "Am7 Dm7 Am7 Am7 Dm7 Dm7 Am7 Am7 E7 Dm7 Am7 E7")]
    public void MinorProgression_RealizesToExpectedChordsInAMinor(string id, string expectedSymbols)
    {
        PackDefinition def = DefaultPack.Load().Definitions
            .Single(d => d.Kind == ContentKind.Progression && d.Id == id);

        (CatalogMetadata meta, string body) = CatalogHeader.Parse(def.Dsl);
        Assert.Equal(Tonality.Minor, meta.Tonality); // authored minor-home

        Progression prog = ProgressionParser.Parse(def.Id, def.Name, body, TimeSignature.FourFour, home: meta.Tonality);

        var aMinor = new Key(new PitchClass(9), IsMinor: true);
        string symbols = string.Join(
            ' ', Transposer.Realize(prog, aMinor).Select(c => ChordSymbol.Format(c, aMinor)));

        Assert.Equal(expectedSymbols, symbols);
    }
}
