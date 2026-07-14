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

    private static SqliteConnection SeededConnection()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = new ChordFlowDbContext(Options(conn));
        db.Database.Migrate();
        db.Progressions.Add(new ProgressionEntity
        {
            Id = "blues",
            Name = "Blues",
            Dsl = "17 47 17 17",
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
    public void Build_MissingReference_Throws()
    {
        using var conn = SeededConnection();
        var handler = new ChordSheetHandler(Options(conn));

        Assert.Throws<InvalidOperationException>(
            () => handler.Build(new ChordSheetRequest("progression", "nope", null, 4, "none", null)));
    }
}
