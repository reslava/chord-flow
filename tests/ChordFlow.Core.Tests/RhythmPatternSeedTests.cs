using ChordFlow.Domain;
using ChordFlow.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChordFlow.Core.Tests;

public class RhythmPatternSeedTests
{
    public static IEnumerable<object[]> BuiltIns() =>
        SeedData.BuiltInRhythmPatterns.Select(p => new object[] { p });

    [Theory]
    [MemberData(nameof(BuiltIns))]
    public void EverySeededPattern_ParsesToAFullBar(RhythmPatternDefinition def)
    {
        // Each built-in DSL must parse cleanly into a one-bar pattern (the seed round-trip).
        RhythmPattern pattern = RhythmPatternParser.Parse(def.Id, def.Name, def.Dsl, TimeSignature.FourFour);

        Assert.Single(pattern.Bars);
        Assert.NotEmpty(pattern.Bars[0].Events);
    }

    [Fact]
    public void SeedBuiltInRhythmPatterns_SeedsOnce_AndIsIdempotent()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<ChordFlowDbContext>().UseSqlite(conn).Options;

        using var db = new ChordFlowDbContext(options);
        db.Database.Migrate();

        int firstRun = db.SeedBuiltInRhythmPatterns();
        Assert.Equal(SeedData.BuiltInRhythmPatterns.Count, firstRun);
        Assert.Equal(SeedData.BuiltInRhythmPatterns.Count, db.RhythmPatterns.Count());
        Assert.All(db.RhythmPatterns.AsNoTracking(), p => Assert.Equal(Origin.BuiltIn, p.Origin));

        // Every built-in id is present, stored 4/4.
        foreach (RhythmPatternDefinition def in SeedData.BuiltInRhythmPatterns)
        {
            Assert.True(db.RhythmPatterns.Any(p => p.Id == def.Id && p.TsNumerator == 4 && p.TsDenominator == 4));
        }

        // Second run adds nothing and leaves the row count unchanged (idempotent — C3).
        int secondRun = db.SeedBuiltInRhythmPatterns();
        Assert.Equal(0, secondRun);
        Assert.Equal(SeedData.BuiltInRhythmPatterns.Count, db.RhythmPatterns.Count());
    }
}
