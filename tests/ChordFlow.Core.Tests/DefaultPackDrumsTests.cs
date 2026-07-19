using ChordFlow.Features.Packs;
using ChordFlow.Instruments.Drums;
using ChordFlow.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The default pack ships the starter drum grooves (req IN7): they import through the normal
/// <see cref="PackReader"/>/<see cref="PackImporter"/> path as <see cref="ContentSource.Package"/> rows, and
/// each one re-parses into a valid <see cref="DrumGroove"/> (the stored hit-grid DSL is the only form).
/// </summary>
public class DefaultPackDrumsTests
{
    private static readonly string[] Ids = { "rock", "blues-shuffle", "jazz-swing", "funk" };

    private static ChordFlowDbContext ImportedDb(SqliteConnection conn)
    {
        var db = new ChordFlowDbContext(
            new DbContextOptionsBuilder<ChordFlowDbContext>().UseSqlite(conn).Options);
        db.Database.Migrate();
        DefaultPack.ImportInto(db);
        return db;
    }

    [Fact]
    public void Import_ListsTheStarterGrooves_AsPackageSource()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = ImportedDb(conn);

        var list = new DrumGrooveStore(db).List();

        foreach (string id in Ids)
        {
            Assert.Contains(list, s => s.Id == id && s.Source == ContentSource.Package && s.PackId == DefaultPack.PackId);
        }
    }

    [Theory]
    [InlineData("rock", 3)]
    [InlineData("blues-shuffle", 3)]
    [InlineData("jazz-swing", 3)]
    [InlineData("funk", 3)]
    public void Find_EachStarterGroove_ReParsesIntoAValidGroove(string id, int laneCount)
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = ImportedDb(conn);

        DrumGroove? groove = new DrumGrooveStore(db).Find(id);

        Assert.NotNull(groove);
        Assert.Single(groove!.Bars);
        Assert.Equal(laneCount, groove.Bars[0].Lanes.Count);
    }

    [Fact]
    public void Find_Rock_HasKickSnareHiHatOnTheExpectedBeats()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = ImportedDb(conn);

        DrumGroove groove = new DrumGrooveStore(db).Find("rock")!;

        int[] Onsets(DrumVoice v) =>
            groove.Bars[0].Lanes.Single(l => l.Voice == v).Events.Select(e => e.Position).ToArray();
        Assert.Equal(new[] { 0, 96 }, Onsets(DrumVoice.Kick));   // beats 1 & 3
        Assert.Equal(new[] { 48, 144 }, Onsets(DrumVoice.Snare)); // backbeat 2 & 4
        Assert.Equal(8, Onsets(DrumVoice.HiHatClosed).Length);    // straight 8ths
    }
}
