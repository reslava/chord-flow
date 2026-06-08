using System.Text.Json;
using Microsoft.Web.WebView2.Core;

namespace ChordFlow.Infrastructure;

/// <summary>
/// Transport seam over the WebView2 message channel — the WinForms-host analogue
/// of the retired PhotinoBridge. Serializes outbound envelopes (C#→JS) to camelCase
/// JSON via <see cref="CoreWebView2.PostWebMessageAsString"/>, and forwards inbound
/// strings (JS→C#) from the <see cref="CoreWebView2.WebMessageReceived"/> event to
/// the <see cref="WebMessageRouter"/>. The only place that touches WebView2's
/// messaging plumbing; the rest of the app speaks in typed envelopes.
/// </summary>
public sealed class WebView2Bridge : IBridge
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly CoreWebView2 _core;
    private readonly WebMessageRouter _router;

    public WebView2Bridge(CoreWebView2 core, WebMessageRouter router)
    {
        _core = core;
        _router = router;
        _core.WebMessageReceived += OnWebMessageReceived;
    }

    public void Send<T>(T envelope)
    {
        string json = JsonSerializer.Serialize(envelope, JsonOptions);
        _core.PostWebMessageAsString(json);
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        // app.js posts JSON strings via chrome.webview.postMessage(JSON.stringify(...)).
        // TryGetWebMessageAsString throws if a non-string was posted — treat that as a
        // bridge bug (drop it) rather than crashing the UI thread.
        string message;
        try
        {
            message = e.TryGetWebMessageAsString();
        }
        catch (ArgumentException)
        {
            return;
        }

        _router.Dispatch(message);
    }
}
