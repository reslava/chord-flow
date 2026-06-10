namespace ChordFlow.Bridge;

/// <summary>
/// Outbound bridge envelope: a one-line status/error for the WebView status area.
/// Serializes to <c>{"type":"status","text":"…","isError":bool}</c>. Used so a host-side
/// failure (e.g. a render that throws) surfaces to the user instead of silently no-opping.
/// </summary>
public sealed record StatusEnvelope(string Text, bool IsError, string Type = "status");
