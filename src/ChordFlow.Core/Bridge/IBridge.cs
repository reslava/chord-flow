namespace ChordFlow.Bridge;

/// <summary>
/// The C#→JS half of the WebView bridge, abstracted from the concrete host so
/// feature slices don't depend on it. Serializes an envelope and posts it to the
/// WebView. (Inbound JS→C# is handled by <see cref="WebMessageRouter"/>.)
/// Keeping this seam is what made the Photino→WebView2 host swap a one-line change.
/// </summary>
public interface IBridge
{
    /// <summary>Serialize an envelope to JSON (camelCase) and push it to the WebView.</summary>
    void Send<T>(T envelope);
}
