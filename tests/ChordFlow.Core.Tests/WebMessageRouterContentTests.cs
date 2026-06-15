using ChordFlow.Bridge;
using ChordFlow.Rendering;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The router's generic content-CRUD verbs (step 2): each <c>entity*</c> message dispatches to its typed event
/// with the parsed discriminator/payload; a missing required field is dropped (forward-compatible), matching
/// the existing inbound-envelope contract.
/// </summary>
public class WebMessageRouterContentTests
{
    [Fact]
    public void EntityList_Dispatches_WithEntity()
    {
        var router = new WebMessageRouter();
        string? got = null;
        router.EntityListRequested += e => got = e;

        router.Dispatch("""{"type":"entityList","entity":"voicing"}""");

        Assert.Equal("voicing", got);
    }

    [Fact]
    public void EntityGet_Dispatches_WithEntityAndId()
    {
        var router = new WebMessageRouter();
        (string Entity, string Id)? got = null;
        router.EntityGetRequested += (e, id) => got = (e, id);

        router.Dispatch("""{"type":"entityGet","entity":"progression","entityId":"abc"}""");

        Assert.Equal(("progression", "abc"), got);
    }

    [Fact]
    public void EntityPreview_Dispatches_WithEntityAndDsl()
    {
        var router = new WebMessageRouter();
        (string Entity, string Dsl)? got = null;
        router.EntityPreviewRequested += (e, dsl, _) => got = (e, dsl);

        router.Dispatch("""{"type":"entityPreview","entity":"rhythm","dsl":"X...X...X...X..."}""");

        Assert.Equal(("rhythm", "X...X...X...X..."), got);
    }

    [Fact]
    public void EntitySave_WithId_DispatchesAllFields()
    {
        var router = new WebMessageRouter();
        (string Entity, string? Id, string Name, string Dsl)? got = null;
        router.EntitySaveRequested += (e, id, name, dsl) => got = (e, id, name, dsl);

        router.Dispatch("""{"type":"entitySave","entity":"song","entityId":"s1","name":"Demo","dsl":"intro = 1 4\nintro"}""");

        Assert.Equal("song", got!.Value.Entity);
        Assert.Equal("s1", got.Value.Id);
        Assert.Equal("Demo", got.Value.Name);
    }

    [Fact]
    public void EntitySave_WithoutId_DispatchesNullId_ForCreate()
    {
        var router = new WebMessageRouter();
        (string Entity, string? Id, string Name, string Dsl)? got = null;
        router.EntitySaveRequested += (e, id, name, dsl) => got = (e, id, name, dsl);

        router.Dispatch("""{"type":"entitySave","entity":"progression","name":"New","dsl":"1 4 5 1"}""");

        Assert.NotNull(got);
        Assert.Null(got!.Value.Id); // absent id = create
        Assert.Equal("New", got.Value.Name);
    }

    [Fact]
    public void EntityDelete_Dispatches_WithEntityAndId()
    {
        var router = new WebMessageRouter();
        (string Entity, string Id)? got = null;
        router.EntityDeleteRequested += (e, id) => got = (e, id);

        router.Dispatch("""{"type":"entityDelete","entity":"voicing","entityId":"v1"}""");

        Assert.Equal(("voicing", "v1"), got);
    }

    [Fact]
    public void Generate_WithRenderOptions_ParsesIntoRenderOptions()
    {
        var router = new WebMessageRouter();
        RenderOptions? got = null;
        router.GenerateRequested += (_, _, _, opts) => got = opts;

        router.Dispatch("""
            {"type":"generate","keyPitchClass":10,"rhythmId":"quarters","tempo":90,
             "renderOptions":{"showChordNames":true,"showChordDiagramsOverStaff":true,"showChordDiagramsOnTop":true,"voicing":"byDifficulty"}}
            """);

        Assert.NotNull(got);
        Assert.True(got!.ShowChordNames);
        Assert.True(got.ShowChordDiagramsOverStaff);
        Assert.True(got.ShowChordDiagramsOnTop);
        Assert.Equal(VoicingStrategy.ByDifficulty, got.Voicing);
    }

    [Fact]
    public void Generate_WithoutRenderOptions_UsesDefault()
    {
        var router = new WebMessageRouter();
        RenderOptions? got = null;
        router.GenerateRequested += (_, _, _, opts) => got = opts;

        router.Dispatch("""{"type":"generate","keyPitchClass":10,"rhythmId":"quarters","tempo":90}""");

        Assert.Equal(RenderOptions.Default, got);
    }

    [Fact]
    public void EntityPreview_WithRenderOptions_ParsesFlags()
    {
        var router = new WebMessageRouter();
        RenderOptions? got = null;
        router.EntityPreviewRequested += (_, _, opts) => got = opts;

        router.Dispatch("""{"type":"entityPreview","entity":"progression","dsl":"1 4 5 1","renderOptions":{"showChordNames":true}}""");

        Assert.NotNull(got);
        Assert.True(got!.ShowChordNames);
        Assert.False(got.ShowChordDiagramsOverStaff);
        Assert.False(got.ShowChordDiagramsOnTop);
    }

    [Fact]
    public void LoadExercise_WithRenderOptions_ParsesFlags()
    {
        var router = new WebMessageRouter();
        RenderOptions? got = null;
        router.LoadExerciseRequested += (_, opts) => got = opts;

        router.Dispatch("""{"type":"loadExercise","id":7,"renderOptions":{"showChordDiagramsOnTop":true}}""");

        Assert.NotNull(got);
        Assert.True(got!.ShowChordDiagramsOnTop);
    }

    [Fact]
    public void EntityList_MissingEntity_IsDropped()
    {
        var router = new WebMessageRouter();
        bool raised = false;
        router.EntityListRequested += _ => raised = true;

        router.Dispatch("""{"type":"entityList"}"""); // no entity field

        Assert.False(raised);
    }
}
