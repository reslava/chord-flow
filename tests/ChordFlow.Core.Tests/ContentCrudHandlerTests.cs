using ChordFlow.Instruments.Guitar;
using ChordFlow.Features.ContentCrud;
using ChordFlow.Features.Packs;
using ChordFlow.Persistence;
using ChordFlow.Rendering;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The ContentCrud slice (step 2): the generic <c>entity*</c> handler maps the discriminator to the right
/// store, returns the right envelopes, raises <c>VoicingsChanged</c> on voicing writes, renders score/diagram
/// previews, and surfaces invalid DSL as <see cref="FormatException"/>. In-memory SQLite kept open across the
/// handler's per-operation contexts; preview tests import the default pack so the renderer has voicing coverage.
/// </summary>
public class ContentCrudHandlerTests
{
    private static (ContentCrudHandler Handler, SqliteConnection Conn) NewHandler(bool withDefaultPack)
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        DbContextOptions<ChordFlowDbContext> options =
            new DbContextOptionsBuilder<ChordFlowDbContext>().UseSqlite(conn).Options;

        IReadOnlyList<VoicingShape> shapes;
        using (var db = new ChordFlowDbContext(options))
        {
            db.Database.Migrate();
            if (withDefaultPack)
            {
                DefaultPack.ImportInto(db);
            }

            shapes = new VoicingStore(db).LoadShapes();
        }

        var renderer = new AlphaTexRenderer(new VoicingBook(shapes));
        return (new ContentCrudHandler(options, renderer), conn);
    }

    [Fact]
    public void Save_NewProgression_ReturnsId_AndIsListedAsUserDefined()
    {
        var (handler, conn) = NewHandler(withDefaultPack: false);
        using (conn)
        {
            EntitySavedEnvelope saved = handler.Save("progression", id: null, name: "My Tune", dsl: "1 4 5 1");
            Assert.True(Guid.TryParse(saved.Id, out _));

            ContentItem item = Assert.Single(handler.List("progression").Items);
            Assert.Equal("My Tune", item.Name);
            Assert.Equal("UserDefined", item.Origin);
            Assert.False(item.HasLowerTier);
        }
    }

    [Fact]
    public void Save_InvalidDsl_ThrowsFormatException()
    {
        var (handler, conn) = NewHandler(withDefaultPack: false);
        using (conn)
        {
            Assert.Throws<FormatException>(() => handler.Save("progression", null, "Bad", "1 4 9"));
        }
    }

    [Fact]
    public void Save_Voicing_RaisesVoicingsChanged()
    {
        var (handler, conn) = NewHandler(withDefaultPack: false);
        using (conn)
        {
            bool raised = false;
            handler.VoicingsChanged += () => raised = true;

            handler.Save("voicing", null, "Open C", "voicing Cmaj shape:C root:5 frets: x 3 2 0 1 0");

            Assert.True(raised);
        }
    }

    [Fact]
    public void Save_Progression_DoesNotRaiseVoicingsChanged()
    {
        var (handler, conn) = NewHandler(withDefaultPack: false);
        using (conn)
        {
            bool raised = false;
            handler.VoicingsChanged += () => raised = true;

            handler.Save("progression", null, "P", "1 4 5 1");

            Assert.False(raised);
        }
    }

    [Fact]
    public void GetAndDelete_RoundTrip()
    {
        var (handler, conn) = NewHandler(withDefaultPack: false);
        using (conn)
        {
            string id = handler.Save("rhythm", null, "Quarters", "X...X...X...X...").Id;

            EntityLoadedEnvelope? loaded = handler.Get("rhythm", id);
            Assert.NotNull(loaded);
            Assert.Equal("Quarters", loaded!.Name);
            Assert.Equal("X...X...X...X...", loaded.Dsl);

            EntityDeletedEnvelope deleted = handler.Delete("rhythm", id);
            Assert.Equal("Deleted", deleted.Outcome);
            Assert.Null(handler.Get("rhythm", id));
        }
    }

    [Fact]
    public void Preview_Progression_ReturnsScore()
    {
        var (handler, conn) = NewHandler(withDefaultPack: true);
        using (conn)
        {
            EntityPreviewEnvelope preview = handler.Preview("progression", "17 47 17 57");
            Assert.Equal("score", preview.Kind);
            Assert.False(string.IsNullOrWhiteSpace(preview.Tex));
            Assert.Equal(80, preview.Tempo);
        }
    }

    [Fact]
    public void Preview_Voicing_ReturnsDiagramKind()
    {
        var (handler, conn) = NewHandler(withDefaultPack: false);
        using (conn)
        {
            EntityPreviewEnvelope preview = handler.Preview("voicing", "voicing Cmaj shape:C root:5 frets: x 3 2 0 1 0");
            Assert.Equal("diagram", preview.Kind);
            Assert.NotNull(preview.Diagram);
            Assert.Equal(5, preview.Diagram!.Markers.Count); // five sounding strings (low E muted)
            Assert.Equal(new[] { 6 }, preview.Diagram.MutedStrings);
        }
    }

    [Fact]
    public void Preview_InvalidDsl_ThrowsFormatException()
    {
        var (handler, conn) = NewHandler(withDefaultPack: true);
        using (conn)
        {
            Assert.Throws<FormatException>(() => handler.Preview("progression", "1 4 9"));
        }
    }

    [Fact]
    public void UnknownEntity_ThrowsFormatException()
    {
        var (handler, conn) = NewHandler(withDefaultPack: false);
        using (conn)
        {
            Assert.Throws<FormatException>(() => handler.List("bogus"));
        }
    }
}
