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

        using (var db = new ChordFlowDbContext(options))
        {
            db.Database.Migrate();
            if (withDefaultPack)
            {
                DefaultPack.ImportInto(db);
            }
        }

        // The renderer is a pure formatter now (D4=(B)); the handler resolves comping voicings per render.
        var renderer = new AlphaTexRenderer();
        return (new ContentCrudHandler(options, renderer), conn);
    }

    [Fact]
    public void Get_AutomaticVoicing_ReturnsADerivedReadOnlyDoc()
    {
        // A computed `auto:` voicing has no DB row — Get derives it instead of returning "not found" (IN13).
        var (handler, conn) = NewHandler(withDefaultPack: false);
        using (conn)
        {
            EntityLoadedEnvelope? loaded = handler.Get("voicing", "auto:caged:dom7:E");

            Assert.NotNull(loaded);
            Assert.Equal("auto:caged:dom7:E", loaded!.Id);
            Assert.Contains("Dominant 7", loaded.Name);
            Assert.Contains("voicing", loaded.Dsl); // a real voicing DSL the read-only preview can render
        }
    }

    [Fact]
    public void Save_NewProgression_ReturnsId_AndIsListedAsUser()
    {
        var (handler, conn) = NewHandler(withDefaultPack: false);
        using (conn)
        {
            EntitySavedEnvelope saved = handler.Save("progression", id: null, name: "My Tune", dsl: "1 4 5 1");
            Assert.True(Guid.TryParse(saved.Id, out _));

            ContentItem item = Assert.Single(handler.List("progression").Items);
            Assert.Equal("My Tune", item.Name);
            Assert.Equal("user", item.Source);
            Assert.Null(item.PackName);
        }
    }

    [Fact]
    public void List_DefaultPackItems_AreTaggedAsPackage_WithThePackId()
    {
        var (handler, conn) = NewHandler(withDefaultPack: true);
        using (conn)
        {
            // Imported default-pack content is tagged source="package". The handler here has no packNames map,
            // so PackName falls back to the PackId ("default").
            IReadOnlyList<ContentItem> items = handler.List("progression").Items;
            Assert.NotEmpty(items);
            Assert.All(items, i =>
            {
                Assert.Equal("package", i.Source);
                Assert.Equal("default", i.PackName);
            });
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
    public void Preview_Progression_UsesChosenComping() // IN4: the chosen id, not the hard-wired default, drives the render
    {
        var (handler, conn) = NewHandler(withDefaultPack: true);
        using (conn)
        {
            string withBeat13 = handler.Preview("progression", "17 47 17 57", compingPatternId: "beat_1_3").Tex!;
            string withQuarters = handler.Preview("progression", "17 47 17 57", compingPatternId: "quarters").Tex!;
            Assert.NotEqual(withBeat13, withQuarters); // a different comping changes which notes render
        }
    }

    [Fact]
    public void Preview_Song_UsesChosenComping() // same resolve seam on the song arm
    {
        var (handler, conn) = NewHandler(withDefaultPack: true);
        using (conn)
        {
            const string songDsl = "verse = 17 47 17 57\nverse";
            string withBeat13 = handler.Preview("song", songDsl, compingPatternId: "beat_1_3").Tex!;
            string withQuarters = handler.Preview("song", songDsl, compingPatternId: "quarters").Tex!;
            Assert.NotEqual(withBeat13, withQuarters);
        }
    }

    [Fact]
    public void Preview_BlankComping_DefaultsToBeat1And3() // IN5: blank id → the app default beat_1_3
    {
        var (handler, conn) = NewHandler(withDefaultPack: true);
        using (conn)
        {
            string defaulted = handler.Preview("progression", "17 47 17 57").Tex!;
            string explicit13 = handler.Preview("progression", "17 47 17 57", compingPatternId: "beat_1_3").Tex!;
            Assert.Equal(explicit13, defaulted);
        }
    }

    [Fact]
    public void Preview_KeyPitchClass_TransposesTheScore() // scorer-render-params IN7: the preview renders in the seeded/chosen key
    {
        var (handler, conn) = NewHandler(withDefaultPack: true);
        using (conn)
        {
            string inC = handler.Preview("progression", "17 47 17 57", keyPitchClass: 0).Tex!;
            string inF = handler.Preview("progression", "17 47 17 57", keyPitchClass: 5).Tex!;
            Assert.NotEqual(inC, inF); // a different key transposes the realized pitches
        }
    }

    [Fact]
    public void Preview_MinorKey_RendersInMinor() // first-class-minor-keys 8a: keyIsMinor + a tonality: minor header
    {
        var (handler, conn) = NewHandler(withDefaultPack: true);
        using (conn)
        {
            // A minor-home progression previewed in A minor emits the minor key signature (\ks aminor).
            string tex = handler.Preview(
                "progression", "tonality: minor\n1- 4- 5-", keyPitchClass: 9, keyIsMinor: true).Tex!;

            Assert.Contains("\\ks aminor", tex);
        }
    }

    [Fact]
    public void Preview_MinorProgression_HeaderStripped_StillRendersMinor() // minor-mode-ui-threading: the control's mode drives Home
    {
        var (handler, conn) = NewHandler(withDefaultPack: true);
        using (conn)
        {
            // The content editor strips the catalog header (EX3) and sends the body + keyIsMinor from the tonality
            // control. That header-stripped preview must realize IDENTICALLY to the same progression WITH its
            // `tonality:` header — both are A-minor i-iv-v (Am/Dm/Em). Before the fix the stripped body parsed as
            // major-home and previewed the wrong chords (Cm/Fm/Gm).
            string stripped = handler.Preview("progression", "1- 4- 5-", keyPitchClass: 9, keyIsMinor: true).Tex!;
            string headered = handler.Preview("progression", "tonality: minor\n1- 4- 5-", keyPitchClass: 9, keyIsMinor: true).Tex!;

            Assert.Equal(headered, stripped);
            Assert.Contains("\\ks aminor", stripped);
        }
    }

    [Fact]
    public void Preview_Tempo_DrivesTheEnvelopeTempo() // the seeded tempo rides back so playback matches the control
    {
        var (handler, conn) = NewHandler(withDefaultPack: true);
        using (conn)
        {
            Assert.Equal(132, handler.Preview("progression", "17 47 17 57", tempo: 132).Tempo);
            Assert.Equal(80, handler.Preview("progression", "17 47 17 57").Tempo); // absent → the 80 default
        }
    }

    [Fact]
    public void Preview_Song_AbsentKey_KeepsItsOwnInitialKey() // a null key never forces a Song to C (it keeps its DSL key)
    {
        var (handler, conn) = NewHandler(withDefaultPack: true);
        using (conn)
        {
            const string songInF = "key F\nverse = 17 47 17 57\nverse";
            string noKey = handler.Preview("song", songInF).Tex!;               // no override → renders in F
            string forcedF = handler.Preview("song", songInF, keyPitchClass: 5).Tex!; // explicit F (pc 5)
            string forcedC = handler.Preview("song", songInF, keyPitchClass: 0).Tex!; // explicit C (pc 0)
            Assert.Equal(forcedF, noKey);      // absent key == the song's own F, NOT C
            Assert.NotEqual(forcedC, noKey);   // and C is a real transpose away
        }
    }

    [Fact]
    public void Preview_UnknownComping_ThrowsFormatException() // IN6: a non-blank id that does not resolve fails loud
    {
        var (handler, conn) = NewHandler(withDefaultPack: true);
        using (conn)
        {
            Assert.Throws<FormatException>(() => handler.Preview("progression", "17 47 17 57", compingPatternId: "no_such_pattern"));
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
