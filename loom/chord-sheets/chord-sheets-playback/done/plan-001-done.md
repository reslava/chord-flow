---
type: done
id: pl_01KXJKMRCMYANGZCWRP1F4FZ34-done
title: Done — Plan 1 — Extract ChordFlowPlayback (prove via ScoreR parity)
status: done
created: 2026-07-15
version: 3
tags: []
parent_id: pl_01KXJKMRCMYANGZCWRP1F4FZ34
requires_load: []
---
# Done — Plan 1 — Extract ChordFlowPlayback (prove via ScoreR parity)

## Step 1 — New wwwroot/playback-component.js (window.ChordFlowPlayback): lift the alphaTab AlphaTabApi(player) + buildSettings/player settings + soundfont list/apply + scroll modes + transport (load/play/playPause/stop/setTempo/setTrackVolume) + the activeBeatsChanged→onBeat, playerStateChanged→onStateChange/onFinished, and soundFontLoaded wiring out of score-render-component.js. Expose handle { load, play, stop, setTempo, setTrackVolume, setSoundFont, setScrollMode, getApi, dispose } and constructor callbacks { onBeat, onStateChange, onFinished }. Load it in index.html before score-render-component.js.

Created `src/ChordFlow.Desktop/wwwroot/playback-component.js` — `window.ChordFlowPlayback`, the shared alphaTab api + transport wrapper lifted out of ScoreR.

- Owns: the `AlphaTabApi` + its surface, `buildSettings` (core + player settings), scroll-follow (`applyScrollMode` + `setScrollMode`), soundfont round-trip (`listSoundFonts`/`setSoundFont` + persisted-choice apply), per-track volume (re-asserted on `scoreLoaded`), transport (`load`/`play`/`stop`/`setTempo`/`setMetronome`/`setCountIn`), and tempo base (`seedTempo`/`getBaseTempo`).
- Events out via constructor callbacks: `onBeat(bar,beat)` (1-based, from `activeBeatsChanged`), `onStateChange`, `onFinished` (from `playerStateChanged`), `onReady` (from `soundFontLoaded`), `onSoundFontsListed(fonts, selectedId)` (so a consumer fills its own picker UI).
- `player:false` (lite) builds the api WITHOUT player settings and no-ops transport — preserves Content-preview's render-only mode.
- Consumer supplies the initial `display` settings blob (layout stays a consumer concern) and reaches render/notation concerns through `getApi()`.
- Deliberately excludes: notation options, staff profile, key/feel pickers, layout toggles, debug panel — all stay in ScoreR.

Added `<script src="playback-component.js">` before `score-render-component.js` in `index.html`.

## Step 2 — Refactor score-render-component.js so ScoreR composes a ChordFlowPlayback internally for all transport/audio/beat/soundfont, keeps the visible staff + notation-display controls + debug panel + scoreLoaded staff-flag/track-render wiring (via engine.getApi().scoreLoaded), and re-exposes its CURRENT public handle unchanged. ScoreR's onBeat/onStateChange/onFinished forward from the engine's callbacks.

Rewrote `src/ChordFlow.Desktop/wwwroot/score-render-component.js` to compose a `ChordFlowPlayback` internally; **public handle unchanged**.

- ScoreR now creates `const engine = ChordFlowPlayback.create(surface, {...})` and `const api = engine.getApi()`; all render/notation code keeps referencing `api` (minimal diff on the render half).
- **Delegated to the engine:** `load` (wraps engine.load + mirrors debug textarea + tempo input), `play`/`stop`/`setTempo`/`seedTempo`, `setTrackVolume`, `setSoundFont`, `setScrollMode`, metronome/count-in (via `engine.setMetronome/setCountIn` in `setOption`'s PLAYER_KIND branch + the initial apply). `getApi()`/`getTempo()` read through the engine (`engine.getBaseTempo()`).
- **Kept in ScoreR:** visible staff (surface), the control strip + all DOM builders (verbatim), notation-display options (`setOption` CONTENT_KIND/DISPLAY_KIND, `getRenderOptions`, `onNeedsRerender`), staff-display profile (`setStaffFlags`/`applyStaffProfile`/`onStaffProfile` + its `staffProfile` bridge listener + persist), key/feel/tempo pickers + seeds, `applyLayout`, and the alphaTex debug panel.
- **Split the two concerns cleanly:** the engine owns the soundfont bridge round-trip (fires `onSoundFontsListed` → ScoreR's `fillSoundFontPicker`) + per-track-volume re-assert on `scoreLoaded`; ScoreR attaches its own `scoreLoaded` handler (diagrams-on-top flag, staff flags, multi-track render) via `api.scoreLoaded`. Removed from ScoreR: `PlayerState`, `buildSettings`, `DEFAULT_SOUNDFONT`/`fontUrl`, `applyScrollMode`, `applySoundFont`/`onSoundFontsListed`, `applyTrackVolume`, `applyPlayerOption`, and the player event wiring — all now in the engine.

Static verification: `node --check` clean on both JS files; `dotnet build` succeeds (0 errors).

## Step 3 — Prove parity WITHOUT editing consumers: run the Practice view (app.js) and Content-preview (content-crud.js) and confirm play/pause/stop, tempo, cursor highlight, per-track volume, soundfont pick, onBeat, and now/next schedule sync all behave as before. Confirm neither app.js nor content-crud.js needed changes. Update chordflow-architecture-reference with the new ChordFlowPlayback component and ScoreR refactored onto it.

**Ref-sync:** updated `chordflow-architecture-reference` — added `playback-component` to the UI dumb-views box and a "Playback engine seam" note (ScoreR composes `ChordFlowPlayback`; headless consumers mount it with an off-screen surface).

**Parity verified (by Rafa, running the built app):** play/pause/stop, tempo, beat cursor, per-track volume, soundfont pick, scroll modes, and Now/Next sync all behave as before on the extracted engine; `app.js` and `content-crud.js` needed **zero** edits — the ScoreR public handle held.

**One pre-existing bug surfaced (NOT a regression):** metronome + count-in toggles produce no sound. Diffed against the committed original — the code path is byte-identical (`api.metronomeVolume = value?1:0`) and `alphaTab.min.js` was untouched, so the extraction did not cause it. Rafa confirms they worked when first implemented but broke in a later thread's plan and weren't re-checked. Added a re-assert hardening in the engine (hold desired metronome/count-in state, re-apply on `soundFontLoaded`/`scoreLoaded`) — did not resolve it, so the real cause is in the alphaTab metronome enablement for this bundle version. **Spun off as a separate fix item** per decision in chat-001; out of scope for this pure-extraction plan (parity of a feature already broken = parity).
