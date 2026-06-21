using System.Text.Json;
using ChordFlow.Domain;
using ChordFlow.Rendering;

namespace ChordFlow.Bridge;

/// <summary>
/// The UI's chosen content references + params for one <c>generate</c> request — a stored Song or a bare
/// Progression for harmony (the <see cref="HarmonyEntity"/> discriminator mirrors the content-CRUD
/// <c>entity</c> string), a required Comping pattern, an optional Lead pattern, plus the param values. The
/// host resolves the references against the stores (<c>ExerciseRefs</c>) into a canonical Exercise.
/// </summary>
public sealed record GenerateRequest(
    string HarmonyEntity, string HarmonyId, string CompingPatternId, string? LeadPatternId,
    int? KeyPitchClass, int Tempo, Difficulty Difficulty, Feel Feel);

/// <summary>
/// Parses inbound JSON envelopes from the WebView (JS→C#) and raises typed
/// events for feature slices to subscribe to. The envelope <c>type</c> string is
/// the only contract surface. Inbound vocabulary: <c>ready</c> /
/// <c>playbackFinished</c> / <c>beatChanged</c> / <c>generate</c> / <c>play</c> /
/// <c>stop</c> / <c>setTempo</c> / <c>save</c> / <c>listExercises</c> /
/// <c>loadExercise</c> / <c>markPracticed</c> / the generic content-CRUD family
/// <c>entityList</c> / <c>entityGet</c> / <c>entityPreview</c> / <c>entitySave</c> /
/// <c>entityDelete</c> (each carrying an <c>entity</c> discriminator) / <c>scalePreview</c> /
/// <c>cagedPreview</c> / <c>cagedChordPreview</c>.
/// </summary>
public sealed class WebMessageRouter
{
    // Web defaults: camelCase + case-insensitive property matching — mirrors the
    // JSON the JS side produces with JSON.stringify.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>WebView booted and alphaTab is ready to receive a score — carries the UI's initial render options.</summary>
    public event Action<RenderOptions>? Ready;

    /// <summary>Playback reached the end (player returned to the stopped state).</summary>
    public event Action? PlaybackFinished;

    /// <summary>Active beat advanced — <c>(bar, beat)</c>, both 1-based. For progress/accuracy later.</summary>
    public event Action<int, int>? BeatChanged;

    /// <summary>Generate a new exercise from the UI's chosen references + params — <c>(request, renderOptions)</c>.</summary>
    public event Action<GenerateRequest, RenderOptions>? GenerateRequested;

    /// <summary>Start/resume playback (routes to PracticeSession).</summary>
    public event Action? PlayRequested;

    /// <summary>Stop playback (routes to PracticeSession).</summary>
    public event Action? StopRequested;

    /// <summary>Set playback tempo in BPM (routes to PracticeSession).</summary>
    public event Action<int>? SetTempoRequested;

    /// <summary>Save the currently active exercise definition to the library.</summary>
    public event Action? SaveRequested;

    /// <summary>Send the saved-exercise list back to the WebView.</summary>
    public event Action? ListExercisesRequested;

    /// <summary>Reload a saved exercise by id and push its regenerated score — <c>(id, renderOptions)</c>.</summary>
    public event Action<int, RenderOptions>? LoadExerciseRequested;

    /// <summary>Record a practice event for the active exercise.</summary>
    public event Action? MarkPracticedRequested;

    /// <summary>List one content entity type's definitions — <c>(entity)</c>.</summary>
    public event Action<string>? EntityListRequested;

    /// <summary>Open one content definition for editing — <c>(entity, id)</c>.</summary>
    public event Action<string, string>? EntityGetRequested;

    /// <summary>Live-preview an unsaved content DSL — <c>(entity, dsl, renderOptions)</c>.</summary>
    public event Action<string, string, RenderOptions>? EntityPreviewRequested;

    /// <summary>Create/update a content definition — <c>(entity, id?, name, dsl)</c> (null id = create).</summary>
    public event Action<string, string?, string, string>? EntitySaveRequested;

    /// <summary>Delete (or revert) a content definition — <c>(entity, id)</c>.</summary>
    public event Action<string, string>? EntityDeleteRequested;

    /// <summary>List the available playback soundfonts (+ the persisted selection) back to the WebView.</summary>
    public event Action? ListSoundFontsRequested;

    /// <summary>Persist a new global playback soundfont choice — <c>(id)</c>.</summary>
    public event Action<string>? SetSoundFontRequested;

    /// <summary>Preview an interval set on the fretboard (the Scales page) — <c>(intervals, rootPitchClass)</c>.</summary>
    public event Action<string, int>? ScalePreviewRequested;

    /// <summary>Preview a CAGED octave shape on the fretboard (the CAGED Shapes page) — <c>(shape, rootPitchClass)</c>.</summary>
    public event Action<string, int>? CagedPreviewRequested;

    /// <summary>Preview a derived CAGED chord on the fretboard (the CAGED Chords page) — <c>(shape, quality, rootPitchClass)</c>.</summary>
    public event Action<string, string, int>? CagedChordPreviewRequested;

    /// <summary>Deserialize one inbound message string and dispatch it to subscribers.</summary>
    public void Dispatch(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        InboundEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<InboundEnvelope>(message, JsonOptions);
        }
        catch (JsonException)
        {
            // A malformed envelope is a bridge bug, not a reason to crash the host.
            // Drop it and keep running; the symptom surfaces in the WebView console.
            return;
        }

        switch (envelope?.Type)
        {
            case "ready":
                Ready?.Invoke(ToRenderOptions(envelope.RenderOptions));
                break;
            case "playbackFinished":
                PlaybackFinished?.Invoke();
                break;
            case "beatChanged":
                BeatChanged?.Invoke(envelope.Bar ?? 0, envelope.Beat ?? 0);
                break;
            case "generate":
                GenerateRequested?.Invoke(
                    new GenerateRequest(
                        envelope.HarmonyEntity ?? "progression",
                        envelope.HarmonyId ?? "",
                        envelope.CompingPatternId ?? "beat_1_3",
                        envelope.LeadPatternId,
                        envelope.KeyPitchClass,
                        envelope.Tempo ?? 80,
                        ParseEnum(envelope.Difficulty, Difficulty.Beginner),
                        ParseEnum(envelope.Feel, Feel.Straight)),
                    ToRenderOptions(envelope.RenderOptions));
                break;
            case "play":
                PlayRequested?.Invoke();
                break;
            case "stop":
                StopRequested?.Invoke();
                break;
            case "setTempo":
                if (envelope.Bpm is int bpm)
                {
                    SetTempoRequested?.Invoke(bpm);
                }
                break;
            case "save":
                SaveRequested?.Invoke();
                break;
            case "listExercises":
                ListExercisesRequested?.Invoke();
                break;
            case "loadExercise":
                if (envelope.Id is int id)
                {
                    LoadExerciseRequested?.Invoke(id, ToRenderOptions(envelope.RenderOptions));
                }
                break;
            case "markPracticed":
                MarkPracticedRequested?.Invoke();
                break;
            case "entityList":
                if (envelope.Entity is { } listEntity)
                {
                    EntityListRequested?.Invoke(listEntity);
                }
                break;
            case "entityGet":
                if (envelope.Entity is { } getEntity && envelope.EntityId is { } getId)
                {
                    EntityGetRequested?.Invoke(getEntity, getId);
                }
                break;
            case "entityPreview":
                if (envelope.Entity is { } prevEntity && envelope.Dsl is { } prevDsl)
                {
                    EntityPreviewRequested?.Invoke(prevEntity, prevDsl, ToRenderOptions(envelope.RenderOptions));
                }
                break;
            case "entitySave":
                if (envelope.Entity is { } saveEntity && envelope.Name is { } saveName && envelope.Dsl is { } saveDsl)
                {
                    // EntityId is optional: null/absent means "create" (the store mints a GUID).
                    EntitySaveRequested?.Invoke(saveEntity, envelope.EntityId, saveName, saveDsl);
                }
                break;
            case "entityDelete":
                if (envelope.Entity is { } delEntity && envelope.EntityId is { } delId)
                {
                    EntityDeleteRequested?.Invoke(delEntity, delId);
                }
                break;
            case "listSoundFonts":
                ListSoundFontsRequested?.Invoke();
                break;
            case "setSoundFont":
                if (envelope.SoundFontId is { } soundFontId)
                {
                    SetSoundFontRequested?.Invoke(soundFontId);
                }
                break;
            case "scalePreview":
                if (envelope.Intervals is { } scaleIntervals)
                {
                    ScalePreviewRequested?.Invoke(scaleIntervals, envelope.RootPitchClass ?? 0);
                }
                break;
            case "cagedPreview":
                if (envelope.Shape is { } cagedShape)
                {
                    CagedPreviewRequested?.Invoke(cagedShape, envelope.RootPitchClass ?? 0);
                }
                break;
            case "cagedChordPreview":
                if (envelope.Shape is { } chordShape && envelope.Quality is { } chordQuality)
                {
                    CagedChordPreviewRequested?.Invoke(chordShape, chordQuality, envelope.RootPitchClass ?? 0);
                }
                break;
            // Unknown / null types are ignored — forward-compatible.
        }
    }

    // Map the optional inbound render-options object to the Core RenderOptions; absent ⇒ Default (today's render).
    private static RenderOptions ToRenderOptions(InboundRenderOptions? options)
    {
        if (options is null)
        {
            return RenderOptions.Default;
        }

        return new RenderOptions(
            ShowChordNames: options.ShowChordNames ?? false,
            ShowChordDiagramsOverStaff: options.ShowChordDiagramsOverStaff ?? false,
            ShowChordDiagramsOnTop: options.ShowChordDiagramsOnTop ?? false,
            Voicing: ParseVoicing(options.Voicing));
    }

    // Only ByDifficulty ships in v1; an absent or unrecognized value falls back to it (forward-compatible).
    private static VoicingStrategy ParseVoicing(string? value) =>
        Enum.TryParse(value, ignoreCase: true, out VoicingStrategy strategy) && Enum.IsDefined(strategy)
            ? strategy
            : VoicingStrategy.ByDifficulty;

    // Parse a string enum param (Difficulty/Feel) case-insensitively; an absent or unrecognized value falls
    // back to the supplied default (forward-compatible — a new value the host doesn't know is ignored).
    private static T ParseEnum<T>(string? value, T fallback) where T : struct, Enum =>
        Enum.TryParse(value, ignoreCase: true, out T parsed) && Enum.IsDefined(parsed) ? parsed : fallback;

    private sealed record InboundEnvelope(
        string? Type, int? Bar, int? Beat, int? Id,
        // generate references + params: a song/progression harmony discriminator + id, the comping pattern id,
        // an optional lead pattern id, the chosen key (null → the Song's own key), tempo, and the Difficulty/Feel
        // param values (enum names). KeyPitchClass/Tempo are reused by setTempo's Bpm sibling below.
        string? HarmonyEntity, string? HarmonyId, string? CompingPatternId, string? LeadPatternId,
        int? KeyPitchClass, int? Tempo, string? Difficulty, string? Feel, int? Bpm,
        // Content-CRUD fields: Entity discriminator, the string content id (distinct from the int Id used by
        // loadExercise), and the editor's Name/Dsl payload.
        string? Entity, string? EntityId, string? Name, string? Dsl,
        // setSoundFont: the chosen soundfont id (file name). A string, distinct from the int Id / string EntityId.
        string? SoundFontId,
        // scalePreview: the interval set text ("1 b3 4 5 b7") + the chosen root pitch class (0..11).
        string? Intervals, int? RootPitchClass,
        // cagedPreview: the CAGED shape name ("C"/"A"/"G"/"E"/"D"); reuses RootPitchClass for the root.
        // cagedChordPreview adds Quality (the quality enum name) alongside Shape + RootPitchClass.
        string? Shape, string? Quality,
        // Optional render-time presentation options on the render-producing verbs (generate/loadExercise/entityPreview).
        InboundRenderOptions? RenderOptions);

    private sealed record InboundRenderOptions(
        bool? ShowChordNames, bool? ShowChordDiagramsOverStaff, bool? ShowChordDiagramsOnTop, string? Voicing);
}
