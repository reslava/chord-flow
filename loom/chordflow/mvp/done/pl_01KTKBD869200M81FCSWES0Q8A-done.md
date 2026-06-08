---
type: done
id: pl_01KTKBD869200M81FCSWES0Q8A-done
title: "Done — Phase 2b — Host migration: Photino → WinForms + WebView2"
status: done
created: "2026-06-08T00:00:00.000Z"
version: 4
tags: []
parent_id: pl_01KTKBD869200M81FCSWES0Q8A
requires_load: []
---
# Done — Phase 2b — Host migration: Photino → WinForms + WebView2

## Step 1 — Repoint the host project: csproj TargetFramework → net10.0-windows, UseWindowsForms=true, OutputType=WinExe; remove Photino.NET, add Microsoft.Web.WebView2; keep the wwwroot copy-to-output.

Repointed the host project to WinForms + WebView2.

**`src/ChordFlow.App/ChordFlow.App.csproj`:** `OutputType` Exe→**WinExe**; `TargetFramework` net10.0→**net10.0-windows**; added **`UseWindowsForms=true`**; removed `Photino.NET`; added **`Microsoft.Web.WebView2` 1.0.3296.44**. Kept the `wwwroot\**\*` copy-to-output `<Content>` item (now served via virtual-host instead of file://).

**`tests/ChordFlow.Tests/ChordFlow.Tests.csproj`:** TFM net10.0 → **net10.0-windows** (necessary consequence: it `ProjectReference`s ChordFlow.App, and a net10.0 project can't reference a net10.0-windows one). Added a comment noting the Domain/Rendering types under test are platform-neutral, so a future cleanup could extract them into a net10.0 library to keep the tests off Windows.

**Decision:** aligned the test TFM rather than splitting Domain/Rendering into a separate library now — the migration is meant to be host-scoped/minimal, and the design already anticipates that extraction as later work.

**Verification:** `dotnet restore` succeeds (WebView2 package resolves). Full build deferred to step 2 — the existing Photino `Program.cs`/`PhotinoBridge.cs` won't compile until they're replaced.

## Step 2 — Rewrite Program.cs as a WinForms host (Form + dock-filled WebView2; EnsureCoreWebView2Async → SetVirtualHostNameToFolderMapping("chordflow.local", wwwroot, Allow) → Navigate https://chordflow.local/index.html) and replace PhotinoBridge with WebView2Bridge (PostWebMessageAsString out; WebMessageReceived → WebMessageRouter in). Re-wire GenerateExercise + PracticeSession.

Rewrote the host as WinForms + WebView2 and replaced the Photino bridge.

**New `Infrastructure/IBridge.cs`:** `interface IBridge { void Send<T>(T envelope); }` — the C#→JS seam, so feature slices no longer depend on the concrete host. (Adding this is what makes any future host swap a one-liner — the lesson from this migration.)

**New `Infrastructure/WebView2Bridge.cs`** (`: IBridge`): wraps `CoreWebView2`. `Send<T>` → `PostWebMessageAsString(camelCaseJson)`; subscribes `CoreWebView2.WebMessageReceived`, reads `TryGetWebMessageAsString()` (guards the `ArgumentException` if a non-string is posted — dropped, not crashed), forwards to `WebMessageRouter.Dispatch`. Same envelope contract as the old PhotinoBridge.

**Rewrote `Program.cs`** as a WinForms host: `ApplicationConfiguration.Initialize()`, a `Form` (1100×820, centered, "ChordFlow") with a dock-filled `WebView2`. On `Form.Load` (async, wrapped in try/catch → MessageBox on failure): `EnsureCoreWebView2Async()` → `SetVirtualHostNameToFolderMapping("chordflow.local", <BaseDirectory>/wwwroot, Allow)` → build `WebMessageRouter` + `WebView2Bridge` + `GenerateExerciseHandler` + `PracticeSessionHandler` → `router.Ready += push 12-bar blues in Bb @80` → `Navigate("https://chordflow.local/index.html")`. Bridge built before navigation so the JS "ready" ping isn't missed.

**`Features/PracticeSession/PracticeSession.cs`:** ctor param + field `PhotinoBridge` → **`IBridge`** (only change; envelopes/logic untouched).

**Deleted `Infrastructure/PhotinoBridge.cs`.**

**Verification:** `dotnet build` succeeds, 0 errors. One benign `MSB3277` warning — WebView2's NuGet ships an unused WPF control assembly (`Microsoft.Web.WebView2.Wpf.dll` → WindowsBase) that triggers a unification note; irrelevant to the WinForms host. (App won't drive via the bridge until step 3 swaps `window.external` → `chrome.webview` in app.js; until then it'd hit the no-host `SAMPLE_TEX` fallback.)

## Step 3 — Update the app.js Bridge module: window.external.{sendMessage,receiveMessage} → window.chrome.webview.postMessage / addEventListener('message'); keep the no-host fallback; re-enable core.useWorkers (real origin) and confirm the same-origin soundfont path.

Swapped the `app.js` transport shim to WebView2 and re-enabled workers.

**`wwwroot/app.js`:**
- **Bridge module** rewritten from Photino's `window.external.{sendMessage,receiveMessage}` to **`window.chrome.webview`**: `send` → `wv.postMessage(JSON.stringify(obj))` (JS→C#); `onReceive(handler)` → `wv.addEventListener("message", e => handler(e.data))`, where `e.data` is the string the host sent via `PostWebMessageAsString` (C#→JS). Still feature-detected (`available`), so the no-host `SAMPLE_TEX` fallback path is preserved for standalone browser dev.
- **`core.useWorkers: false → true`** — the page now loads from the real `https://chordflow.local/` origin (not `file://`), so alphaTab's layout Web Worker can spawn; comment updated accordingly. *(This is the one behavior change to watch at step-4 verification; trivially revertible to `false` — the known-good main-thread path — if rendering/cursor misbehaves.)*
- Updated the file header comment (Photino/file:// → WebView2/`chrome.webview`). Soundfont (`soundfont/sonivox.sf2`) and `fontDirectory: "font/"` stay relative — now same-origin under the virtual host, so the soundfont XHR is no longer CORS-blocked.

No change to `onHostMessage`, the envelope handling, transport buttons, or the event mapping — the contract is identical; only the transport layer moved.

**Verification:** `node --check app.js` syntax OK; `dotnet build` 0 errors (updated app.js copied to output). Live render/audio confirmation is step 4.

## Step 4 — Build, run and verify: tablature renders, status reaches "ready · soundfont loaded", Play → audio + synced beat cursor; dotnet test stays green; remove the throwaway spike and any Photino leftovers.

Verified the migrated host end-to-end and cleaned up.

**Build/tests:** `dotnet build` 0 errors; **`dotnet test` 26/26 green** on net10.0-windows. Throwaway `spike/` deleted; clean-rebuild confirms **no Photino leftovers** in output and the WebView2 assemblies present.

**Live verification (user-confirmed on the running app):**
- ✅ **Tablature renders** under WinForms + WebView2 (issue A — Photino's black window — solved; windowed controller).
- ✅ **Audio plays** and the soundfont loads over the `https://chordflow.local/` virtual host (issue B — file:// soundfont CORS — solved).
- ✅ **Synced beat cursor** — required two fixes beyond the host swap: (1) alphaTab's cursor is **off by default** even with the player enabled → added `player.enableCursor` / `enableAnimatedBeatCursor` / `enableElementHighlighting`; (2) alphaTab **positions but doesn't color** the cursor → added the `.at-cursor-bar` / `.at-cursor-beat` / `.at-highlight` CSS (+ `position: relative` on `#score`) in `index.html`. Result confirmed live: current-bar highlight + moving blue beat line + active-note highlighting tracking in time (req **IN7**). Click-to-seek (alphaTab built-in) also works.

**Files touched in this step:** `wwwroot/app.js` (cursor settings), `wwwroot/index.html` (cursor CSS), removed `spike/`.

Phase 2b complete: ChordFlow fully migrated off Photino to WinForms + WebView2, rendering + playback + synced cursor all verified.
