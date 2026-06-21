using ChordFlow.Music.Rhythm;
using ChordFlow.Features.Packs;
using ChordFlow.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The built-in rhythm patterns now ship in the on-disk default pack and are imported as
/// <see cref="Origin.BuiltIn"/> on first run. Each parses into a one-bar pattern, and the import is idempotent.
/// </summary>
public class RhythmPatternSeedTests
{
    public static IEnumerable<object[]> BuiltIns() =>
        DefaultPack.Load().Definitions
            .Where(d => d.Kind == ContentKind.Rhythm)
            .Select(d => new object[] { d });

    [Theory]
    [MemberData(nameof(BuiltIns))]
    public void EveryDefaultPattern_ParsesToAFullBar(PackDefinition def)
    {
        // Rhythm files carry no catalog header (EX3), so the DSL is the bare grammar.
        RhythmPattern pattern = RhythmPatternParser.Parse(def.Id, def.Name, def.Dsl, TimeSignature.FourFour);

        Assert.Single(pattern.Bars);
        Assert.NotEmpty(pattern.Bars[0].Events);
    }

    [Fact]
    public void DefaultPackImport_SeedsRhythmsAsBuiltIn4_4_AndIsIdempotent()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<ChordFlowDbContext>().UseSqlite(conn).Options;

        using var db = new ChordFlowDbContext(options);
        db.Database.Migrate();

        int rhythmCount = DefaultPack.Load().Definitions.Count(d => d.Kind == ContentKind.Rhythm);

        DefaultPack.ImportInto(db);
        Assert.Equal(rhythmCount, db.RhythmPatterns.Count());
        Assert.All(db.RhythmPatterns.AsNoTracking(), p =>
        {
            Assert.Equal(Origin.BuiltIn, p.Origin);
            Assert.Equal(4, p.TsNumerator);
            Assert.Equal(4, p.TsDenominator);
        });

        // Second import adds nothing (idempotent — C3).
        DefaultPack.ImportInto(db);
        Assert.Equal(rhythmCount, db.RhythmPatterns.Count());
    }
}
