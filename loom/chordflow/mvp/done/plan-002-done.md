---
type: done
id: pl_01KTHKHV1JHQFMRMJWBG5HRZHA-done
title: Done — Phase 2 — Desktop Shell, Rendering & Playback
status: done
created: "2026-06-08T00:00:00.000Z"
version: 4
tags: []
parent_id: pl_01KTHKHV1JHQFMRMJWBG5HRZHA
requires_load: []
---
# Done — Phase 2 — Desktop Shell, Rendering & Playback

## Step 1 — Stand up the Photino host (PhotinoWindow loading wwwroot/index.html); scaffold wwwroot (index.html, app.js); bundle alphaTab.min.js and a small redistributable GM soundfont (confirm license).

Stood up the Photino desktop host and scaffolded wwwroot with bundled alphaTab assets.

**Files:**
- `ChordFlow.App.csproj` — added `Photino.NET` 4.0.16 (pulls `Photino.Native` 4.0.22 + `WebView2Loader.dll`); added a `<Content>` item copying `wwwroot\**\*` (excluding `.gitkeep`) to the output dir with `PreserveNewest` so the WebView loads assets from disk next to the exe (no HTTP server — C2).
- `Program.cs` — replaced the Hello-World stub with a `PhotinoWindow` (title "ChordFlow", 1100×820, centered, resizable, context menu off) that `.Load(...)`s `wwwroot/index.html` resolved via `AppContext.BaseDirectory`, then `WaitForClose()`. (IN6, C2, C6)
- `wwwroot/index.html` — minimal dark-theme shell: header with a `#status` line, a `#score` container for alphaTab, and `<script>` tags loading the bundled `alphaTab.min.js` then `app.js`.
- `wwwroot/app.js` — step-1 scope only: verifies the `alphaTab` global loaded from disk and sets the status line ("host ready · alphaTab loaded"). api.tex rendering/player/bridge deferred to later steps.

**Bundled assets (downloaded from jsdelivr, alphaTab 1.8.3):**
- `wwwroot/alphaTab.min.js`
- `wwwroot/font/Bravura.{woff2,woff,otf}` + `Bravura-OFL.txt` (SIL OFL) — required for music-glyph rendering.
- `wwwroot/soundfont/sonivox.sf2` (1.35 MB) + `LICENSE` + `README.md`.

**License confirmation (C7):** Sonivox GM soundfont is **Apache-2.0** (per bundled LICENSE, © Sonic Network Inc.) — redistributable. Bravura font is **SIL OFL** — redistributable. Both satisfy C7 (small + redistributable).

**Verification:** `dotnet build` succeeds (0 warnings/errors); confirmed all wwwroot assets and `Photino.Native.dll` + `WebView2Loader.dll` copied into `bin/Debug/net10.0`. Interactive window render not launched in this headless session — visual confirmation happens at the step-4 cursor check.

## Step 2 — Render a hardcoded alphaTex string end-to-end in the window via api.tex with player.enablePlayer/player.soundFont — proves the alphaTab integration before any bridging.

Rendered a hardcoded alphaTex score end-to-end via `api.tex` with the soundfont player enabled — proves the alphaTab integration before any bridging.

**Files:**
- `wwwroot/app.js` — rewrote into a `ChordFlow` module that: builds the alphaTab settings, creates `new alphaTab.AlphaTabApi(#score, settings)`, wires lifecycle events to the header status line, and calls `api.tex(SAMPLE_TEX)`. `SAMPLE_TEX` is the full 12-bar blues in Bb, "Beats 1 & 3" — authored to be **byte-for-byte identical** to `AlphaTexRenderer`'s output (verified against `AlphaTexRendererTests`: Bb7 `(1.5 0.4 1.3)`, Eb7 `(6.5 5.4 6.3)`, F7 `(8.5 7.4 8.3)`, `:4` stated once). Robust init guard (`document.readyState`) so it runs whether or not `DOMContentLoaded` already fired.
- `wwwroot/index.html` — `#score` render container already in place from step 1; no structural change needed (kept as the alphaTab mount point).

**alphaTab settings (decisions):**
- `core.fontDirectory: "font/"` and `player.soundFont: "soundfont/sonivox.sf2"` — **relative** paths on purpose: Photino serves wwwroot over `file://`, so leading-slash absolutes would resolve to the drive root and 404.
- `core.useWorkers: false` — Chromium/WebView2 refuses to spawn a Web Worker from a `file://` (null) origin; running layout on the main thread avoids the crash. The bundle's `ScriptProcessor` path covers the audio side if `AudioWorklet` is unavailable.
- `player.enablePlayer: true` (required for audio + synced cursor); `player.scrollMode: alphaTab.ScrollMode.Off` (fixed container, no page auto-scroll).

**C8 verification (against installed alphaTab 1.8.3):** confirmed in `alphaTab.min.js` that `AlphaTabApi`, `api.tex`, the event emitters used (`renderStarted`, `renderFinished`, `soundFontLoad`, `soundFontLoaded`, `error`), `core.useWorkers`/`core.fontDirectory`, `player.enablePlayer`/`player.soundFont`, and `ScrollMode.Off` all exist. Event subscription shape is the `api.<event>.on(handler)` form.

**Verification:** `dotnet build` succeeds (0/0); updated `app.js` confirmed copied to `bin/Debug/net10.0/wwwroot`. Note: actual on-screen render + audio is interactive (needs the GUI window) — not launched in this headless session; the visual/audible confirmation lands at the step-4 cursor check. API surface verified statically against the bundle to de-risk it.

## Step 3 — Build the C#<->JS bridge: WebMessageRouter + JSON envelopes (loadScore/play/stop out; ready/playbackFinished/beatChanged in); wire the GenerateExercise slice to push a real engine-produced score.

Built the narrow C#↔JS bridge and wired the GenerateExercise slice to push a real engine-produced score.

**Files created:**
- `Infrastructure/WebMessageRouter.cs` — parses inbound JSON envelopes (JS→C#) and raises typed events: `Ready`, `PlaybackFinished`, `BeatChanged(bar, beat)`. Uses `JsonSerializerDefaults.Web` (camelCase, case-insensitive). Malformed envelopes are swallowed (logged via WebView console, host stays up) rather than crashing — treats bridge bugs as findings. Unknown `type` values ignored (forward-compatible). Private `InboundEnvelope(Type, Bar, Beat)` DTO.
- `Infrastructure/PhotinoBridge.cs` — the only code touching Photino's message plumbing. Registers the inbound handler via `RegisterWebMessageReceivedHandler` (verified signature `EventHandler<string>`), forwards strings to the router, and `Send<T>(envelope)` serializes to camelCase JSON and calls `SendWebMessage` (verified present on PhotinoWindow 4.0.16).
- `Features/GenerateExercise/GenerateExercise.cs` — `LoadScoreEnvelope(Type, Tex, Tempo)` outbound record + `GenerateExerciseHandler.Generate(keyPitchClass, rhythmId, tempo)`: composes a 12-bar blues `Exercise` (Domain) → `IScoreRenderer.Render` (Rendering) → wraps as `{type:"loadScore", tex, tempo}`. Unknown rhythmId falls back to Beats 1 & 3.

**Files edited:**
- `Program.cs` — built `WebMessageRouter` + `PhotinoBridge` + `GenerateExerciseHandler(new AlphaTexRenderer())` **before** `Load()` (bridge registers its handler in the ctor, so no early `ready` is missed). Subscribed `router.Ready += () => bridge.Send(generate.Generate(10 /*Bb*/, "beat_1_3", 80))`.
- `wwwroot/app.js` — added a `Bridge` module over `window.external.{sendMessage,receiveMessage}`. On init: registers `onReceive` **then** posts `{type:"ready"}` (order matters — avoids missing the loadScore reply); `onHostMessage` parses the envelope and renders `loadScore` via `api.tex(msg.tex)`, stashing tempo for step 4. Kept `SAMPLE_TEX` strictly as a **no-host browser-dev fallback** (rendered only when `window.external` is absent). play/stop/setTempo are stubbed for step 4.

**Protocol (the bridge's only contract surface):** out `loadScore`/`play`/`stop`/`setTempo`; in `ready`/`playbackFinished`/`beatChanged`. Step 3 fully implements `loadScore` + `ready`; the rest are defined and stubbed for step 4.

**Verification:** `dotnet build` 0/0; `dotnet test` 26/26 pass (Domain + AlphaTexRenderer engine output the bridge relies on is green). Live round-trip (ready→loadScore→render) is interactive — confirmed statically (Photino send/receive signatures reflected from the 4.0.16 DLL); on-screen confirmation at the step-4 cursor check.

## Step 4 — Add playback: play/stop/tempo controls; map alphaTab events (playerStateChanged -> playbackFinished, activeBeatsChanged -> beatChanged) and confirm the synced beat cursor highlights in time. Verify the ⚠️ alphaTab API details against the installed version.

Added playback: transport controls, the alphaTab↔bridge event mapping, and the PracticeSession slice. Completes Phase 2.

**Files:**
- `wwwroot/app.js` — transport + events:
  - Inbound `play`→`api.playPause()`, `stop`→`api.stop()`, `setTempo`→runtime tempo (no re-render): translates absolute BPM to `api.playbackSpeed = bpm / baseTempo` (baseTempo taken from the loadScore envelope).
  - Local transport wiring: Play toggles play/pause, Stop, and a tempo `<input>`; controls enabled on `soundFontLoaded` (player ready).
  - **Event mapping (JS→C# via bridge):** `playerStateChanged` → updates the Play/Pause label and, when `e.stopped`, posts `{type:"playbackFinished"}`; `activeBeatsChanged` → posts `{type:"beatChanged", bar, beat}` (1-based) from `e.activeBeats.beats[0]` (`beat.voice.bar.index`, `beat.index`), guarded for empty arrays.
  - `PlayerState` enum resolved **defensively** (`alphaTab.synth.PlayerState` ?? top-level ?? `{Paused:0,Playing:1}`) since the minified bundle assembles namespaces at runtime.
- `wwwroot/index.html` — added a minimal `#transport` bar (Play, Stop, tempo number input), styled to match. *(Not in the step's listed files, but required for the named "play/stop/tempo controls" and to make the cursor verifiable; same precedent as Program.cs in step 3. The richer control set — key/rhythm/generate/save/list, IN10 — remains for the later UI phase.)*
- `Features/PracticeSession/PracticeSession.cs` — `PracticeSessionHandler` drives `Play()`/`Stop()`/`SetTempo(bpm)` by sending `PlayCommand`/`StopCommand`/`SetTempoCommand` envelopes over the bridge, and tracks `IsPlaying` / `CurrentBar` / `CurrentBeat` from the router's `PlaybackFinished`/`BeatChanged` events. The C# transport seam the later UI + Progress features hook into.
- `Program.cs` — instantiated `PracticeSessionHandler(bridge, router)` so it subscribes to playback echoes; added its `using`.
- `Infrastructure/WebMessageRouter.cs` — **no change needed**: the inbound `playbackFinished`/`beatChanged` vocabulary was already implemented in step 3; step 4 consumes it via `PracticeSessionHandler`. (Listed in the plan's files-to-touch, but the clean implementation required nothing further here.)

**C8 verification (against installed alphaTab 1.8.3):** confirmed in the bundle — `playPause`, `stop`, `playbackSpeed`, `playerStateChanged` (arg fields `state` + `stopped`), `activeBeatsChanged` (arg field `activeBeats.beats`), and `PlayerState` (Paused=0/Playing=1). Used `playerStateChanged` for finish-detection per the step's specified mapping; noted that `stopped` fires on both natural end and manual stop — acceptable for MVP (both end the session).

**Verification:** `dotnet build` 0 warnings/0 errors; `node --check app.js` syntax OK; `dotnet test` 26/26 pass. **Interactive confirmation deferred:** actually *seeing* the synced beat cursor highlight in time + hearing audio requires launching the GUI window (WebView2/audio), which isn't possible in this headless session. All API usage was verified statically against the 1.8.3 bundle to de-risk it; a manual run (`dotnet run --project src/ChordFlow.App` → click Play, watch the cursor track the beats) is the remaining hands-on check.
