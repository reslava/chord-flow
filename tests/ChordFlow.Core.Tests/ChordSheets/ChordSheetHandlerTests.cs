using ChordFlow.Bridge;
using ChordFlow.Features.ChordSheets;
using ChordFlow.Persistence;
using ChordFlow.Persistence.Entities;
using ChordFlow.Rendering.ChordSheets;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Xunit;

namespace ChordFlow.Core.Tests.ChordSheets;

/// <summary>
/// <see cref="ChordSheetHandler"/>: the <c>chordSheet</c> verb end-to-end over a real store — resolve a stored
/// progression, realize it in the requested key, and build the sheet; the adornment gate on the fret diagram;
/// and the fail-loud on a missing reference.
/// </summary>
public class ChordSheetHandlerTests
{
    private static DbContextOptions<ChordFlowDbContext> Options(SqliteConnection conn) =>
        new DbContextOptionsBuilder<ChordFlowDbContext>().UseSqlite(conn).Options;

    private static SqliteConnection SeededConnection(string dsl = "17 47 17 17")
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = new ChordFlowDbContext(Options(conn));
        db.Database.Migrate();
        db.Progressions.Add(new ProgressionEntity
        {
            Id = "blues",
            Name = "Blues",
            Dsl = dsl,
            Origin = Origin.UserDefined,
            CreatedUtc = DateTime.UtcNow,
        });
        db.SaveChanges();
        return conn;
    }

    private static ChordSheetRequest Request(
        string adornment = "none", int? key = null, int barsPerRow = 4) =>
        new("progression", "blues", key, barsPerRow, adornment, Voicing: null);

    [Fact]
    public void Build_ResolvesProgression_IntoASheet()
    {
        using var conn = SeededConnection();
        var handler = new ChordSheetHandler(Options(conn));

        ChordSheet sheet = handler.Build(Request()).Sheet;

        Assert.Equal("Blues", sheet.Header.Title);
        Assert.Equal("C", sheet.Header.KeyName);                    // no override ⇒ the lifted C
        ChordSheetSection section = Assert.Single(sheet.Sections);
        Assert.Equal("C7", section.Rows[0].Cells[0].Chords.Single().Concrete);  // I7 in C
    }

    [Fact]
    public void Build_KeyOverride_RealizesInThatKey()
    {
        using var conn = SeededConnection();
        var handler = new ChordSheetHandler(Options(conn));

        // Key pitch class 7 = G.
        ChordSheet sheet = handler.Build(Request(key: 7)).Sheet;

        Assert.Equal("G", sheet.Header.KeyName);
        Assert.Equal("G7", sheet.Sections[0].Rows[0].Cells[0].Chords.Single().Concrete);
    }

    [Fact]
    public void Build_AdornmentNone_LeavesDiagramsNull()
    {
        using var conn = SeededConnection();
        var handler = new ChordSheetHandler(Options(conn));

        ChordSheet sheet = handler.Build(Request(adornment: "none")).Sheet;

        Assert.All(
            sheet.Sections.SelectMany(s => s.Rows).SelectMany(r => r.Cells).SelectMany(c => c.Chords),
            chord => Assert.Null(chord.Diagram));
    }

    [Fact]
    public void Build_AdornmentBoth_FillsDiagrams()
    {
        using var conn = SeededConnection();
        var handler = new ChordSheetHandler(Options(conn));

        ChordSheet sheet = handler.Build(Request(adornment: "both")).Sheet;

        Assert.All(
            sheet.Sections.SelectMany(s => s.Rows).SelectMany(r => r.Cells).SelectMany(c => c.Chords),
            chord => Assert.NotNull(chord.Diagram));
    }

    [Fact]
    public void Build_ReturnsPlayableTexAndPerBarCellSchedule()
    {
        using var conn = SeededConnection();   // 4 bars: I7 IV7 I7 I7 (bar 3 repeats bar 2 → a %)
        var handler = new ChordSheetHandler(Options(conn));

        ChordSheetResultEnvelope result = handler.Build(Request());

        Assert.False(string.IsNullOrWhiteSpace(result.Tex));   // playable alphaTex was rendered
        // A downbeat entry (bar-level highlight) for every bar 0..3, incl. the % bar (bar 3).
        Assert.Equal(
            new[] { 0, 1, 2, 3 },
            result.CellSchedule.Where(e => e.Beat == 0).Select(e => e.Bar).Distinct().OrderBy(b => b));
        Assert.Contains(result.CellSchedule, e => e is { Bar: 3, Beat: 0, Chord: 0 });
    }

    [Fact]
    public void Build_SplitBar_GetsSubChordOnsetEntry()
    {
        using var conn = SeededConnection("17_47");   // one bar, two chords (C7 then F7)
        var handler = new ChordSheetHandler(Options(conn));

        ChordSheetResultEnvelope result = handler.Build(Request());

        // The downbeat (chord segment 0) plus a mid-bar onset for the second chord (segment 1, beat > 0).
        Assert.Contains(result.CellSchedule, e => e is { Bar: 0, Beat: 0, Chord: 0 });
        Assert.Contains(result.CellSchedule, e => e.Bar == 0 && e.Chord == 1 && e.Beat > 0);
    }

    [Fact]
    public void Build_MissingReference_Throws()
    {
        using var conn = SeededConnection();
        var handler = new ChordSheetHandler(Options(conn));

        Assert.Throws<InvalidOperationException>(
            () => handler.Build(new ChordSheetRequest("progression", "nope", null, 4, "none", null)));
    }
}
