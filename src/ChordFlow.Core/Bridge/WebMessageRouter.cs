using System.Text.Json;

namespace ChordFlow.Bridge;

/// <summary>
/// Parses inbound JSON envelopes from the WebView (JS→C#) and raises typed
/// events for feature slices to subscribe to. The envelope <c>type</c> string is
/// the only contract surface. Inbound vocabulary: <c>ready</c> /
/// <c>playbackFinished</c> / <c>beatChanged</c> / <c>generate</c> / <c>play</c> /
/// <c>stop</c> / <c>setTempo</c> / <c>save</c> / <c>listExercises</c> /
/// <c>loadExercise</c> / <c>markPracticed</c>.
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

    /// <summary>Generate a new exercise — <c>(keyPitchClass, rhythmId, tempo)</c>.</summary>
    public event Action<int, string, int>? GenerateRequested;

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

    /// <summary>Reload a saved exercise by id and push its regenerated score.</summary>
    public event Action<int>? LoadExerciseRequested;

    /// <summary>Record a practice event for the active exercise.</summary>
    public event Action? MarkPracticedRequested;

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
                    envelope.Tempo ?? 80);
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
                    LoadExerciseRequested?.Invoke(id);
                }
                break;
            case "markPracticed":
                MarkPracticedRequested?.Invoke();
                break;
            // Unknown / null types are ignored — forward-compatible.
        }
    }

    private sealed record InboundEnvelope(
        string? Type, int? Bar, int? Beat, int? Id,
        int? KeyPitchClass, string? RhythmId, int? Tempo, int? Bpm);
}
