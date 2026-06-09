using ChordFlow.Domain;
using ChordFlow.Infrastructure;
using ChordFlow.Rendering;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChordFlow.Tests;

public class ProgressionSeedTests
{
    private static readonly AlphaTexRenderer Renderer = new();

    public static IEnumerable<object[]> BuiltIns() =>
        SeedData.BuiltInProgressions.Select(p => new object[] { p });

    [Theory]
    [MemberData(nameof(BuiltIns))]
    public void EverySeededProgression_RoundTripsDslToModelToRender(ProgressionDefinition def)
    {
        // DSL → parser → transposer → renderer must succeed for each built-in (in a major key).
        Progression prog = ProgressionParser.Parse(def.Id, def.Name, def.Dsl, TimeSignature.FourFour);
        var exercise = new Exercise(
            new Key(new PitchClass(10), false), prog, SeedData.Quarters, 90, Difficulty.Beginner);

        string tex = Renderer.Render(exercise);

        Assert.StartsWith($"\\title \"{def.Name} — Bb\"", tex);
        Assert.Contains('|', tex);
    }

    [Fact]
    public void SeedBuiltInProgressions_SeedsOnce_AndIsIdempotent()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<ChordFlowDbContext>().UseSqlite(conn).Options;

        using var db = new ChordFlowDbContext(options);
        db.Database.Migrate();

        int firstRun = db.SeedBuiltInProgressions();
        Assert.Equal(SeedData.BuiltInProgressions.Count, firstRun);
        Assert.Equal(SeedData.BuiltInProgressions.Count, db.Progressions.Count());
        Assert.All(db.Progressions.AsNoTracking(), p => Assert.Equal(ProgressionOrigin.BuiltIn, p.Origin));

        // Every built-in id is present.
        foreach (ProgressionDefinition def in SeedData.BuiltInProgressions)
        {
            Assert.True(db.Progressions.Any(p => p.Id == def.Id));
        }

        // Second run adds nothing and leaves the row count unchanged.
        int secondRun = db.SeedBuiltInProgressions();
        Assert.Equal(0, secondRun);
        Assert.Equal(SeedData.BuiltInProgressions.Count, db.Progressions.Count());
    }
}
