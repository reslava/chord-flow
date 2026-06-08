using System.Text.Json;

namespace ChordFlow.Infrastructure;

/// <summary>
/// Parses inbound JSON envelopes from the WebView (JS→C#) and raises typed
/// events for feature slices to subscribe to. The envelope <c>type</c> string is
/// the only contract surface. Inbound vocabulary: <c>ready</c> /
/// <c>playbackFinished</c> / <c>beatChanged</c>.
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
            // Unknown / null types are ignored — forward-compatible.
        }
    }

    private sealed record InboundEnvelope(string? Type, int? Bar, int? Beat);
}
