---
type: reference
id: rf_01KTK7ATYGHWBRR8Z6M76976ET
title: "Photino.NET Desktop Host"
status: active
created: 2026-06-08
version: 1
tags: []
parent_id: null
child_ids: []
requires_load: []
slug: photino-net-desktop-host
description: "Practical Photino.NET API surface (verified by reflection), WebView2 runtime model, C#↔JS bridge, wwwroot serving, browser-args env var, and the ChordFlow black/blank-window investigation."
---
Practical reference for **Photino.NET** as used by the ChordFlow desktop host. Sourced from: (a) the official docs at https://docs.tryphotino.io/ — thin, mostly conceptual; (b) **reflection over the installed `Photino.NET` 4.0.16 assembly** (the authoritative API surface, marked ✅ verified below); (c) the ChordFlow Phase-2 black/blank-window investigation (see `loom/chordflow/mvp/chats/mvp-chat-002.md`).

## 1. What it is

Photino opens a **native OS window** hosting the **system WebView** and loads HTML/JS/CSS — no bundled Chromium, no HTTP server, no localhost. The UI is a web page; app logic can live in C# behind a narrow string bridge. Per-OS WebView backend:

| OS | WebView backend |
|----|-----------------|
| Windows | **WebView2** (Evergreen, Chromium/Edge) |
| macOS | WKWebView |
| Linux | WebKitGTK |

Cross-platform desktop only (Win/Mac/Linux); **no mobile**. No native UI controls — everything renders in the WebView.

## 2. Setup

- NuGet: add **`Photino.NET`**; it transitively pulls **`Photino.Native`** (the C++ window/WebView control). The two are a matched pair — `Photino.NET` 4.0.16 *depends on* `Photino.Native` 4.0.22 (intended, **not** a mismatch).
- **The only build dependency is the .NET SDK** (per docs). On **Windows the WebView2 Evergreen Runtime must be present** — bundled with Windows 11 and current Edge; on bare Windows 10 the docs note installing the Edge Dev/Insider channel.
- `OutputType=Exe`. The native runtime libs (`Photino.Native.dll` on 4.x, `WebView2Loader.dll`) are copied to the output by the package.

## 3. PhotinoWindow API ✅ (verified by reflection, Photino.NET 4.0.16)

Fluent builder — most setters return `PhotinoWindow`. Construct, configure, `Load…`, then `WaitForClose()`.

**Configuration:**
```
PhotinoWindow SetTitle(string title)
PhotinoWindow SetSize(int width, int height)
PhotinoWindow SetSize(System.Drawing.Size size)
PhotinoWindow SetUseOsDefaultSize(bool useOsDefault)
PhotinoWindow Center()
PhotinoWindow SetResizable(bool resizable)
PhotinoWindow SetContextMenuEnabled(bool enabled)   // also gates F12 DevTools
PhotinoWindow SetDevToolsEnabled(bool enabled)
PhotinoWindow SetLogVerbosity(int verbosity)         // managed-side log only; not WebView2/Chromium logs
PhotinoWindow SetTransparent(bool enabled)
```
Read-only props exist for each (`Centered`, `Resizable`, `ContextMenuEnabled`, `DevToolsEnabled`, `Transparent`, `UseOsDefaultSize`).

**Content loading:**
```
PhotinoWindow Load(string path)        // file path or relative path (resolved by Photino) or URL
PhotinoWindow Load(System.Uri uri)
PhotinoWindow LoadRawString(string content)   // load an HTML string directly (no file)
```
A `Load(localPath)` is logged twice — once as the raw path, once normalized to `file:///…`.

**Lifecycle & dialogs:**
```
void WaitForClose()                    // blocks until the window closes
PhotinoDialogResult ShowMessage(string title, string text, PhotinoDialogButtons, PhotinoDialogIcon)
string[] ShowOpenFile(...) / ShowOpenFolder(...) / string ShowSaveFile(...)   // + *Async variants
```

## 4. C#↔JS bridge ✅ (verified)

A narrow **string** channel — pass JSON envelopes; the payload is plain text.

| Direction | C# side | JS side |
|-----------|---------|---------|
| C# → JS | `void SendWebMessage(string message)` / `Task SendWebMessageAsync(string)` | `window.external.receiveMessage(callbackFn)` — callback gets the raw string |
| JS → C# | `PhotinoWindow RegisterWebMessageReceivedHandler(EventHandler<string> handler)` **or** the `WebMessageReceived` event (`event EventHandler<string>`) | `window.external.sendMessage(string)` |

The C# handler is `EventHandler<string>` → `(object? sender, string message)`. Register the receive handler **before** `Load()` so no early inbound message (e.g. a JS "ready" ping) is missed. ChordFlow wraps this in `Infrastructure/PhotinoBridge.cs` + `WebMessageRouter.cs`.

## 5. Serving static content (wwwroot)

- No web server. Photino loads from disk; for ChordFlow we `Load(Path.Combine(AppContext.BaseDirectory, "wwwroot", "index.html"))`, which Photino normalizes to a `file://` URL.
- **Files must be copied to the output dir.** FAQ: a *"Could not find 'xxxxxx' in 'window'"* / file-not-found symptom means content wasn't copied — set assets to **Copy if newer / Copy always** (csproj `<Content … CopyToOutputDirectory=PreserveNewest>`).
- **`file://` is a null origin.** Consequences for embedded web apps: Web **Workers** can't be spawned from `file://` (Chromium throws) — disable them (alphaTab: `core.useWorkers=false`); and use **relative** asset paths (a leading `/` resolves to the drive root, not wwwroot). To get a real origin instead, register a custom URL scheme handler (Photino supports custom scheme handlers) and load `app://…`.

## 6. Passing WebView2 / Chromium browser arguments

Set the environment variable **`WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS`** *before the window is created* (e.g. `Environment.SetEnvironmentVariable(...)` at the top of `Main`, or in the launching shell). WebView2 forwards it to the Chromium engine. Useful flags:
- `--disable-gpu` — force software rendering.
- `--disable-features=CalculateNativeWinOcclusion` — the classic fix for WebView2 windows that stop painting when wrongly detected as occluded (Electron/CEF/Photino all hit it).
- `--enable-logging --v=1` (+ a writable `WEBVIEW2_USER_DATA_FOLDER`) — emit real Chromium logs to disk for diagnosis (Photino's `SetLogVerbosity` does **not** surface these).

## 7. ⚠️ Known issue — blank/black client area (ChordFlow Phase-2 investigation)

Observed on this Win11 box (WebView2 runtime **149.x**, RTX 3060, console session): the native window opens correctly (title/size/center apply) and **6 `msedgewebview2.exe` processes spawn** (engine initializes), but the **navigated DOM never displays** — client area paints only the surface clear-color: **black on Photino 4.0.16**, **white/blank on Photino 3.2.3**. Reproduces even with `LoadRawString` of a hardcoded opaque page, so it is **not** a content/file-path/app bug. `--disable-gpu` and `--disable-features=CalculateNativeWinOcclusion` did not resolve it.

Signature = a WebView2 **compositing/surface** failure (Chromium renders to a surface that never reaches the visible HWND). Leading hypothesis: the very new **WebView2 runtime 149.x is ahead of these Photino native builds' SDK/loader contract**.

### ✅ RESOLUTION — root cause confirmed, host migrated off Photino

Diagnosis ladder, in order: (a) the built `wwwroot/index.html` opened **directly in Edge renders perfectly** (tablature + transport) — so it's **not** our code, and not the WebView2 runtime broadly (Edge *is* WebView2). (b) Resizing/reactivating the Photino window does **not** recover it → not the "created-while-hidden" timing variant. (c) A ~40-line **WinForms + `Microsoft.Web.WebView2` spike rendered the same `wwwroot` perfectly** on this exact machine.

**Root cause:** the **WebView2 controller type.** Photino (and the WPF `WebView2` control) host via the **composition controller** (DComp visual hosting), which fails to present on this stack (.NET 10 + runtime 149 + this GPU/driver). **Edge and the WinForms `WebView2` control use the *windowed* controller** (HWND-hosted), which renders. It's specific to Photino's composition path on a bleeding-edge stack — not a usage error (Photino's own `LoadRawString` of a bare page is also black). Also relevant: Photino.NET 4.0.16 targets **net8/net9**, run here under net10 roll-forward.

**Decision:** ChordFlow migrated **Photino → WinForms + WebView2** (windowed controller), serving `wwwroot` via `SetVirtualHostNameToFolderMapping` (real `https` origin → also fixes the `file://` soundfont-CORS block, issue B). `Infrastructure/` + the `app.js` bridge shim changed (`window.external` → `chrome.webview`); the engine/renderer/feature slices, `WebMessageRouter`, the envelope contract, and the tests were untouched. See chat `mvp-chat-002` and migration plan `[[mvp-plan-004]]`. This doc is retained as the Photino API/troubleshooting record and the rationale for moving off it.

## 8. Doc sections (https://docs.tryphotino.io/)

`/Overview` · `/Quick-Start-for-.NET-Developers` · `/Photino-.NET` · `/Photino-Native` · `/Photino-Blazor` · `/Photino-Samples` · `/Frequently-Asked-Questions` · `/Photino-Comparison-with-Electron` · `/Photino-Road-Map`. The pages are largely conceptual; the authoritative API surface is the assembly itself (§3–4 above) and the GitHub repo `tryphotino/photino.NET`.
