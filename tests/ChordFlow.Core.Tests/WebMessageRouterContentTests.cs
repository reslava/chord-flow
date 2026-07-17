using ChordFlow.Exercises;
using ChordFlow.Music.Rhythm;
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
        router.EntityPreviewRequested += (e, dsl, _, _, _, _, _, _) => got = (e, dsl);

        router.Dispatch("""{"type":"entityPreview","entity":"rhythm","dsl":"X...X...X...X..."}""");

        Assert.Equal(("rhythm", "X...X...X...X..."), got);
    }

    [Fact]
    public void EntityPreview_CarriesTripletFeel()
    {
        var router = new WebMessageRouter();
        TripletFeel got = TripletFeel.None;
        router.EntityPreviewRequested += (_, _, _, feel, _, _, _, _) => got = feel;

        router.Dispatch("""{"type":"entityPreview","entity":"progression","dsl":"1 4 5","tripletFeel":"Triplet8th"}""");

        Assert.Equal(TripletFeel.Triplet8th, got);
    }

    [Fact]
    public void EntityPreview_CarriesCompingPatternId()
    {
        var router = new WebMessageRouter();
        string? got = "unset";
        router.EntityPreviewRequested += (_, _, _, _, compingPatternId, _, _, _) => got = compingPatternId;

        router.Dispatch("""{"type":"entityPreview","entity":"progression","dsl":"1 4 5","compingPatternId":"driving"}""");

        Assert.Equal("driving", got);
    }

    [Fact]
    public void EntityPreview_AbsentCompingPatternId_IsNull()
    {
        var router = new WebMessageRouter();
        string? got = "unset";
        router.EntityPreviewRequested += (_, _, _, _, compingPatternId, _, _, _) => got = compingPatternId;

        router.Dispatch("""{"type":"entityPreview","entity":"progression","dsl":"1 4 5"}""");

        Assert.Null(got); // absent → null; the handler applies the beat_1_3 default
    }

    [Fact]
    public void EntityPreview_CarriesKeyAndTempo()
    {
        // The preview's Key/Tempo render params ride the entityPreview envelope so the editor renders in the
        // seeded key/tempo and a live change re-voices it (scorer-render-params IN7).
        var router = new WebMessageRouter();
        (int? key, int? tempo) got = (-1, -1);
        router.EntityPreviewRequested += (_, _, _, _, _, key, _, tempo) => got = (key, tempo);

        router.Dispatch("""{"type":"entityPreview","entity":"song","dsl":"A = 1 4 5 1\nA","keyPitchClass":5,"tempo":132}""");

        Assert.Equal((5, 132), got);
    }

    [Fact]
    public void EntityPreview_CarriesKeyIsMinor() // first-class-minor-keys 8a: the key's mode rides the envelope
    {
        var router = new WebMessageRouter();
        bool got = false;
        router.EntityPreviewRequested += (_, _, _, _, _, _, keyIsMinor, _) => got = keyIsMinor;

        router.Dispatch("""{"type":"entityPreview","entity":"progression","dsl":"1- 4- 5-","keyIsMinor":true}""");

        Assert.True(got);
    }

    [Fact]
    public void EntityPreview_AbsentKeyAndTempo_AreNull()
    {
        // No ScoreR yet / key-independent content ⇒ absent ⇒ null ⇒ the handler's C / 80 preview default.
        var router = new WebMessageRouter();
        (int? key, int? tempo) got = (-1, -1);
        router.EntityPreviewRequested += (_, _, _, _, _, key, _, tempo) => got = (key, tempo);

        router.Dispatch("""{"type":"entityPreview","entity":"progression","dsl":"1 4 5"}""");

        Assert.Equal((null, null), got);
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
    public void Generate_ParsesReferencesAndParams()
    {
        var router = new WebMessageRouter();
        GenerateRequest? got = null;
        router.GenerateRequested += (req, _) => got = req;

        router.Dispatch("""
            {"type":"generate","harmonyEntity":"song","harmonyId":"blues_song_demo","compingPatternId":"beat_1_3",
             "leadPatternId":"quarters","keyPitchClass":7,"tempo":90,"difficulty":"Intermediate","tripletFeel":"Triplet8th"}
            """);

        Assert.NotNull(got);
        Assert.Equal("song", got!.HarmonyEntity);
        Assert.Equal("blues_song_demo", got.HarmonyId);
        Assert.Equal("beat_1_3", got.CompingPatternId);
        Assert.Equal("quarters", got.LeadPatternId);
        Assert.Equal(7, got.KeyPitchClass);
        Assert.Equal(90, got.Tempo);
        Assert.Equal(Difficulty.Intermediate, got.Difficulty);
        Assert.Equal(TripletFeel.Triplet8th, got.TripletFeel);
    }

    [Fact]
    public void Generate_DefaultsParamsAndDiscriminator_WhenAbsent()
    {
        var router = new WebMessageRouter();
        GenerateRequest? got = null;
        router.GenerateRequested += (req, _) => got = req;

        router.Dispatch("""{"type":"generate","harmonyId":"12bar_blues","keyPitchClass":10}""");

        Assert.NotNull(got);
        Assert.Equal("progression", got!.HarmonyEntity);    // default discriminator
        Assert.Null(got.LeadPatternId);                     // no lead
        Assert.Equal(80, got.Tempo);                        // default tempo
        Assert.Equal(Difficulty.Beginner, got.Difficulty);  // default param
        Assert.Equal(TripletFeel.None, got.TripletFeel);
    }

    [Fact]
    public void Generate_WithRenderOptions_ParsesIntoRenderOptions()
    {
        var router = new WebMessageRouter();
        RenderOptions? got = null;
        router.GenerateRequested += (_, opts) => got = opts;

        router.Dispatch("""
            {"type":"generate","harmonyEntity":"progression","harmonyId":"12bar_blues","compingPatternId":"quarters","keyPitchClass":10,"tempo":90,
             "renderOptions":{"showChordNames":true,"showChordDiagramsOverStaff":true,"showChordDiagramsOnTop":true,"voicing":{"kind":"automatic","minFret":5,"maxFret":12}}}
            """);

        Assert.NotNull(got);
        Assert.True(got!.ShowChordNames);
        Assert.True(got.ShowChordDiagramsOverStaff);
        Assert.True(got.ShowChordDiagramsOnTop);
        Assert.Equal("automatic", got.Voicing!.Kind);
        Assert.Equal(5, got.Voicing.MinFret);
        Assert.Equal(12, got.Voicing.MaxFret);
    }

    [Fact]
    public void Generate_WithoutRenderOptions_UsesDefault()
    {
        var router = new WebMessageRouter();
        RenderOptions? got = null;
        router.GenerateRequested += (_, opts) => got = opts;

        router.Dispatch("""{"type":"generate","harmonyEntity":"progression","harmonyId":"12bar_blues","keyPitchClass":10,"tempo":90}""");

        Assert.Equal(RenderOptions.Default, got);
    }

    [Fact]
    public void EntityPreview_WithRenderOptions_ParsesFlags()
    {
        var router = new WebMessageRouter();
        RenderOptions? got = null;
        router.EntityPreviewRequested += (_, _, opts, _, _, _, _, _) => got = opts;

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
        router.LoadExerciseRequested += (_, _, _, opts) => got = opts;

        router.Dispatch("""{"type":"loadExercise","id":7,"renderOptions":{"showChordDiagramsOnTop":true}}""");

        Assert.NotNull(got);
        Assert.True(got!.ShowChordDiagramsOnTop);
    }

    [Fact]
    public void LoadExercise_PlainClick_HasNoKeyOrFeelOverride()
    {
        // A library click sends no key/feel → the overrides are null so the stored exercise's own params win (C2).
        var router = new WebMessageRouter();
        int? gotKey = -1;
        TripletFeel? gotFeel = TripletFeel.Triplet8th;
        router.LoadExerciseRequested += (_, key, feel, _) => { gotKey = key; gotFeel = feel; };

        router.Dispatch("""{"type":"loadExercise","id":7}""");

        Assert.Null(gotKey);
        Assert.Null(gotFeel);
    }

    [Fact]
    public void LoadExercise_WithReplayedKeyAndFeel_ParsesOverrides()
    {
        // A live Key/Feel change ScoreR replays carries keyPitchClass + tripletFeel → transient overrides (IN4).
        var router = new WebMessageRouter();
        int? gotKey = null;
        TripletFeel? gotFeel = null;
        router.LoadExerciseRequested += (_, key, feel, _) => { gotKey = key; gotFeel = feel; };

        router.Dispatch("""{"type":"loadExercise","id":7,"keyPitchClass":5,"tripletFeel":"Triplet8th"}""");

        Assert.Equal(5, gotKey);
        Assert.Equal(TripletFeel.Triplet8th, gotFeel);
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

    [Fact]
    public void GetStaffProfile_Dispatches()
    {
        var router = new WebMessageRouter();
        bool raised = false;
        router.GetStaffProfileRequested += () => raised = true;

        router.Dispatch("""{"type":"getStaffProfile"}""");

        Assert.True(raised);
    }

    [Fact]
    public void SetStaffProfile_Dispatches_WithProfile()
    {
        var router = new WebMessageRouter();
        string? got = null;
        router.SetStaffProfileRequested += p => got = p;

        router.Dispatch("""{"type":"setStaffProfile","profile":"standard"}""");

        Assert.Equal("standard", got);
    }

    [Fact]
    public void SetStaffProfile_MissingProfile_IsDropped()
    {
        var router = new WebMessageRouter();
        bool raised = false;
        router.SetStaffProfileRequested += _ => raised = true;

        router.Dispatch("""{"type":"setStaffProfile"}"""); // no profile field

        Assert.False(raised);
    }
}
