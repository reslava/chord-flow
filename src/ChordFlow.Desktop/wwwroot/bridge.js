// ChordFlow shared bridge transport.
//
// The single owner of the WebView2 message channel (window.chrome.webview), now
// shared by every front-end view (the Practice generator in app.js and the
// Content CRUD editor in content-crud.js) instead of living inside app.js.
//
// JS→C# via postMessage(JSON.stringify(obj)); C#→JS arrives as 'message' events
// whose e.data is the string the host sent with PostWebMessageAsString. Multiple
// views register receive handlers and each inbound message fans out to all of
// them (every view ignores envelope types it doesn't own). Feature-detected, so
// opening the page in a plain browser (no host) still loads — send() is a no-op
// and no messages arrive (views fall back to their own dev behavior).
"use strict";

window.ChordFlowBridge = (function () {
  const wv =
    typeof window !== "undefined" && window.chrome ? window.chrome.webview : undefined;
  const available = !!wv && typeof wv.postMessage === "function";
  const handlers = [];

  if (available) {
    wv.addEventListener("message", (e) => {
      for (const handler of handlers) handler(e.data);
    });
  }

  return {
    available,
    send(obj) {
      if (available) wv.postMessage(JSON.stringify(obj));
    },
    // Register an inbound handler. Every registered handler sees every message;
    // a view switches on msg.type and ignores the envelopes it doesn't handle.
    onReceive(handler) {
      handlers.push(handler);
    },
  };
})();
