---
type: plan
id: pl_01KXJKMRCMYANGZCWRP1F4FZ34
title: Plan 1 — Extract ChordFlowPlayback (prove via ScoreR parity)
status: done
created: 2026-07-15
updated: 2026-07-15
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KXJJJYD9XBRYED1F8HYTG74H
requires_load: []
target_version: 0.1.0
actual_release: 0.15.0
steps:
  - id: create-chordflowplayback
    order: 1
    status: done
    description: "New wwwroot/playback-component.js (window.ChordFlowPlayback): lift the alphaTab AlphaTabApi(player) + buildSettings/player settings + soundfont list/apply + scroll modes + transport (load/play/playPause/stop/setTempo/setTrackVolume) + the activeBeatsChanged→onBeat, playerStateChanged→onStateChange/onFinished, and soundFontLoaded wiring out of score-render-component.js. Expose handle { load, play, stop, setTempo, setTrackVolume, setSoundFont, setScrollMode, getApi, dispose } and constructor callbacks { onBeat, onStateChange, onFinished }. Load it in index.html before score-render-component.js."
    files_touched: [src/ChordFlow.Desktop/wwwroot/playback-component.js, src/ChordFlow.Desktop/wwwroot/index.html]
    blocked_by: []
    satisfies: [IN1]
  - id: refactor-scorer-onto-the-engine
    order: 2
    status: done
    description: Refactor score-render-component.js so ScoreR composes a ChordFlowPlayback internally for all transport/audio/beat/soundfont, keeps the visible staff + notation-display controls + debug panel + scoreLoaded staff-flag/track-render wiring (via engine.getApi().scoreLoaded), and re-exposes its CURRENT public handle unchanged. ScoreR's onBeat/onStateChange/onFinished forward from the engine's callbacks.
    files_touched: [src/ChordFlow.Desktop/wwwroot/score-render-component.js]
    blocked_by: [create-chordflowplayback]
    satisfies: [IN2, C2]
  - id: verify-scorer-parity-ref-sync
    order: 3
    status: done
    description: "Prove parity WITHOUT editing consumers: run the Practice view (app.js) and Content-preview (content-crud.js) and confirm play/pause/stop, tempo, cursor highlight, per-track volume, soundfont pick, onBeat, and now/next schedule sync all behave as before. Confirm neither app.js nor content-crud.js needed changes. Update chordflow-architecture-reference with the new ChordFlowPlayback component and ScoreR refactored onto it."
    files_touched: [loom/refs/chordflow-architecture-reference.md]
    blocked_by: [refactor-scorer-onto-the-engine]
    satisfies: [IN3, C6]
---
# Plan 1 — Extract ChordFlowPlayback (prove via ScoreR parity)

## Goal

Extract the alphaTab playback engine out of ScoreR into a shared JS `ChordFlowPlayback` component, and refactor ScoreR to compose it while re-exposing its current public handle unchanged. This is a pure internal refactor whose acceptance gate is ScoreR parity: the Practice view (app.js) and the Content-preview (content-crud.js) require zero edits and behave byte-identically (play/pause/stop, tempo, cursor, per-track volume, soundfont, onBeat, schedule/now-next sync). No new feature ships here — this lays the proven engine that Plan 2 (sheet playback) rides on. The architecture reference is updated in the same unit of work.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | New wwwroot/playback-component.js (window.ChordFlowPlayback): lift the alphaTab AlphaTabApi(player) + buildSettings/player settings + soundfont list/apply + scroll modes + transport (load/play/playPause/stop/setTempo/setTrackVolume) + the activeBeatsChanged→onBeat, playerStateChanged→onStateChange/onFinished, and soundFontLoaded wiring out of score-render-component.js. Expose handle { load, play, stop, setTempo, setTrackVolume, setSoundFont, setScrollMode, getApi, dispose } and constructor callbacks { onBeat, onStateChange, onFinished }. Load it in index.html before score-render-component.js. | src/ChordFlow.Desktop/wwwroot/playback-component.js, src/ChordFlow.Desktop/wwwroot/index.html | — | IN1 |
| ✅ | 2 | Refactor score-render-component.js so ScoreR composes a ChordFlowPlayback internally for all transport/audio/beat/soundfont, keeps the visible staff + notation-display controls + debug panel + scoreLoaded staff-flag/track-render wiring (via engine.getApi().scoreLoaded), and re-exposes its CURRENT public handle unchanged. ScoreR's onBeat/onStateChange/onFinished forward from the engine's callbacks. | src/ChordFlow.Desktop/wwwroot/score-render-component.js | create-chordflowplayback | IN2, C2 |
| ✅ | 3 | Prove parity WITHOUT editing consumers: run the Practice view (app.js) and Content-preview (content-crud.js) and confirm play/pause/stop, tempo, cursor highlight, per-track volume, soundfont pick, onBeat, and now/next schedule sync all behave as before. Confirm neither app.js nor content-crud.js needed changes. Update chordflow-architecture-reference with the new ChordFlowPlayback component and ScoreR refactored onto it. | loom/refs/chordflow-architecture-reference.md | refactor-scorer-onto-the-engine | IN3, C6 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:create-chordflowplayback -->
### Step 1 — Create ChordFlowPlayback

Engine owns its own render surface (a div it creates or is handed) so the api can render off-screen for a headless consumer. Per-track volume re-assert on scoreLoaded belongs to the engine (playback concern). Soundfont: the engine owns the listSoundFonts/setSoundFont bridge round-trip + persisted-choice apply. Keep the PlayerState defensive resolution. The engine must NOT know about notation-display options, staff profiles, key/feel pickers, or the debug panel — those stay in ScoreR.

<!-- step:refactor-scorer-onto-the-engine -->
### Step 2 — Refactor ScoreR onto the engine

Split the two bridge.onReceive listeners: the engine takes soundFontsListed; ScoreR keeps staffProfile. ScoreR attaches its staff-flags/multi-track-render/globalDisplayChordDiagramsOnTop concerns to engine.getApi().scoreLoaded; the engine re-asserts per-track volumes on the same event. Every handle method Practice/Content-preview call today (load, play, stop, setTempo, setOption, getRenderOptions, seedKey/seedTempo/seedTripletFeel, setKey, setTripletFeel, setStaffProfile, setScrollMode, setTrackVolume, setSoundFont, toggleNowNext, getApi, getKey/getTempo/getTripletFeel, dispose) keeps identical signature + behaviour — delegating transport/audio to the engine.

<!-- step:verify-scorer-parity-ref-sync -->
### Step 3 — Verify ScoreR parity + ref-sync

Parity is the gate for the whole plan — any consumer edit needed means the handle wasn't preserved (fix step 2, don't patch the consumer). Verify via running the app (Practice + Content preview), not just build. Ref update covers only the Plan-1 surface (the ChordFlowPlayback extraction + ScoreR refactor); the cellSchedule / chordSheetResult ref change lands with Plan 2.
