using System.Text.Json;
using ChordFlow.Rendering;

namespace ChordFlow.Bridge;

/// <summary>
/// Parses inbound JSON envelopes from the WebView (JS→C#) and raises typed
/// events for feature slices to subscribe to. The envelope <c>type</c> string is
/// the only contract surface. Inbound vocabulary: <c>ready</c> /
/// <c>playbackFinished</c> / <c>beatChanged</c> / <c>generate</c> / <c>play</c> /
/// <c>stop</c> / <c>setTempo</c> / <c>save</c> / <c>listExercises</c> /
/// <c>loadExercise</c> / <c>markPracticed</c> / the generic content-CRUD family
/// <c>entityList</c> / <c>entityGet</c> / <c>entityPreview</c> / <c>entitySave</c> /
/// <c>entityDelete</c> (each carrying an <c>entity</c> discriminator).
/// </summary>
public sealed class WebMessageRouter
{
    // Web defaults: camelCase + case-insensitive property matching — mirrors the
    // JSON the JS side produces with JSON.stringify.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>WebView booted and alphaTab is ready to receive a score.</summary>
    public event Action? Ready;

    /// <summary>Playback reached the end (player returned to the stopped state).</summary>
    public event Action? PlaybackFinished;

    /// <summary>Active beat advanced — <c>(bar, beat)</c>, both 1-based. For progress/accuracy later.</summary>
    public event Action<int, int>? BeatChanged;

    /// <summary>Generate a new exercise — <c>(keyPitchClass, rhythmId, tempo, renderOptions)</c>.</summary>
    public event Action<int, string, int, RenderOptions>? GenerateRequested;

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
                Ready?.Invoke();
                break;
            case "playbackFinished":
                PlaybackFinished?.Invoke();
                break;
            case "beatChanged":
                BeatChanged?.Invoke(envelope.Bar ?? 0, envelope.Beat ?? 0);
                break;
            case "generate":
                GenerateRequested?.Invoke(
                    envelope.KeyPitchClass ?? 0,
                    envelope.RhythmId ?? "beat_1_3",
                    envelope.Tempo ?? 80,
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
            ShowChordDiagrams: options.ShowChordDiagrams ?? false,
            Voicing: ParseVoicing(options.Voicing));
    }

    // Only ByDifficulty ships in v1; an absent or unrecognized value falls back to it (forward-compatible).
    private static VoicingStrategy ParseVoicing(string? value) =>
        Enum.TryParse(value, ignoreCase: true, out VoicingStrategy strategy) && Enum.IsDefined(strategy)
            ? strategy
            : VoicingStrategy.ByDifficulty;

    private sealed record InboundEnvelope(
        string? Type, int? Bar, int? Beat, int? Id,
        int? KeyPitchClass, string? RhythmId, int? Tempo, int? Bpm,
        // Content-CRUD fields: Entity discriminator, the string content id (distinct from the int Id used by
        // loadExercise), and the editor's Name/Dsl payload.
        string? Entity, string? EntityId, string? Name, string? Dsl,
        // Optional render-time presentation options on the render-producing verbs (generate/loadExercise/entityPreview).
        InboundRenderOptions? RenderOptions);

    private sealed record InboundRenderOptions(bool? ShowChordNames, bool? ShowChordDiagrams, string? Voicing);
}
