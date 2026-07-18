---
type: plan
id: pl_01KXKVQR28RWR6PF1STVZ3XEDX
title: Extract shared PlayerControlsR + engine event bus
status: done
created: 2026-07-15
updated: 2026-07-15
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KXKV7EN0F0QCC3196R17Z3AC
requires_load: []
target_version: 0.1.0
actual_release: 0.15.0
steps:
  - id: engine-event-bus
    order: 1
    status: done
    description: "Add a light multi-subscriber event API to ChordFlowPlayback: engine.on(\"stateChange\"|\"ready\"|\"soundFontsListed\"|\"beat\"|\"finished\", cb), keeping the existing create({onBeat,…}) callbacks as sugar that register on the same buses."
    files_touched: [src/ChordFlow.Desktop/wwwroot/playback-component.js]
    blocked_by: []
    satisfies: [IN2]
  - id: playercontrolsr-component
    order: 2
    status: done
    description: "Create wwwroot/player-controls-component.js (window.ChordFlowPlayerControls): create(container, engine, opts) builds play/pause·stop·tempo·(soundfont)·(metronome)·(count-in)·(optional now-next), wires each to the engine handle, self-subscribes to stateChange/ready/soundFontsListed/finished, and wraps every handler in a log-not-die guard. Add its <script> to index.html."
    files_touched: [src/ChordFlow.Desktop/wwwroot/player-controls-component.js, src/ChordFlow.Desktop/wwwroot/index.html]
    blocked_by: [engine-event-bus]
    satisfies: [IN1, IN5, IN6]
  - id: recompose-scorer
    order: 3
    status: done
    description: "Split ScoreR.buildControls: mount PlayerControlsR for the player controls (play/stop/tempo/soundfont/metronome/count-in/now-next) and drop that wiring; keep ScoreControls (staff-profile, chord-names, diagrams, auto-layout, key/feel, scroll-mode, alphaTex debug panel + their syncToggle). ScoreR keeps owning the engine + cf-score-surface + getApi. Preserve ScoreR's public API and Practice behavior."
    files_touched: [src/ChordFlow.Desktop/wwwroot/score-render-component.js]
    blocked_by: [playercontrolsr-component]
    satisfies: [IN3, C4]
  - id: recompose-chord-sheets-page
    order: 4
    status: done
    description: Replace chord-sheets.js buildTransport's hand-rolled transport (play/stop/tempo/soundfont) with PlayerControlsR mounted on the page's own engine — gaining metronome + count-in. Keep ChordSheetControls (layout/notation/adornments/tone-labels/theme/export), marker-mode, and Show-tab. Do not pass onToggleNowNext (now-next boards deferred, EX1).
    files_touched: [src/ChordFlow.Desktop/wwwroot/chord-sheets.js]
    blocked_by: [playercontrolsr-component]
    satisfies: [IN4, C1, C2, C3]
  - id: verify-parity-on-both-pages
    order: 5
    status: done
    description: "Dogfood both surfaces: Practice (ScoreR) transport/metronome/count-in/now-next/display toggles/debug panel unchanged; Chord Sheets play/stop/tempo/soundfont still work AND metronome + count-in now work, marker-mode/Show-tab/export unchanged and export still light-pinned. Use CHORDFLOW_DEVTOOLS to spot-check engine.metronomeVolume live."
    files_touched: []
    blocked_by: [recompose-scorer, recompose-chord-sheets-page]
    satisfies: [C4]
  - id: architecture-ref-diagram
    order: 6
    status: done
    description: Update loom/refs/chordflow-architecture-reference.md with a diagram of the ScoreR/ChordSheetR-page → PlayerControlsR → ChordFlowPlayback composition and the engine event bus, plus prose for the new shared component and dependency edge.
    files_touched: [loom/refs/chordflow-architecture-reference.md]
    blocked_by: [verify-parity-on-both-pages]
    satisfies: [IN7]
---
# Extract shared PlayerControlsR + engine event bus

## Goal

Extract the player transport (play/stop/tempo/soundfont/metronome/count-in, plus an optional now-next toggle) out of ScoreR into one shared PlayerControlsR component that both render surfaces mount, so these controls live and are fixed in exactly one place. To let the controls widget react to the engine without per-page event forwarding (the duplication that let the syncToggle regression hide), first make ChordFlowPlayback a small multi-subscriber event source and have PlayerControlsR self-subscribe. ScoreR keeps owning its engine + staff surface + getApi and its notation-display controls; the Chord Sheets page keeps its display/export controls, marker-mode and Show-tab, and gains metronome + count-in for free. now-next boards on Chord Sheets are out of scope (EX1, fast follow).

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add a light multi-subscriber event API to ChordFlowPlayback: engine.on("stateChange"\|"ready"\|"soundFontsListed"\|"beat"\|"finished", cb), keeping the existing create({onBeat,…}) callbacks as sugar that register on the same buses. | src/ChordFlow.Desktop/wwwroot/playback-component.js | — | IN2 |
| ✅ | 2 | Create wwwroot/player-controls-component.js (window.ChordFlowPlayerControls): create(container, engine, opts) builds play/pause·stop·tempo·(soundfont)·(metronome)·(count-in)·(optional now-next), wires each to the engine handle, self-subscribes to stateChange/ready/soundFontsListed/finished, and wraps every handler in a log-not-die guard. Add its <script> to index.html. | src/ChordFlow.Desktop/wwwroot/player-controls-component.js, src/ChordFlow.Desktop/wwwroot/index.html | engine-event-bus | IN1, IN5, IN6 |
| ✅ | 3 | Split ScoreR.buildControls: mount PlayerControlsR for the player controls (play/stop/tempo/soundfont/metronome/count-in/now-next) and drop that wiring; keep ScoreControls (staff-profile, chord-names, diagrams, auto-layout, key/feel, scroll-mode, alphaTex debug panel + their syncToggle). ScoreR keeps owning the engine + cf-score-surface + getApi. Preserve ScoreR's public API and Practice behavior. | src/ChordFlow.Desktop/wwwroot/score-render-component.js | playercontrolsr-component | IN3, C4 |
| ✅ | 4 | Replace chord-sheets.js buildTransport's hand-rolled transport (play/stop/tempo/soundfont) with PlayerControlsR mounted on the page's own engine — gaining metronome + count-in. Keep ChordSheetControls (layout/notation/adornments/tone-labels/theme/export), marker-mode, and Show-tab. Do not pass onToggleNowNext (now-next boards deferred, EX1). | src/ChordFlow.Desktop/wwwroot/chord-sheets.js | playercontrolsr-component | IN4, C1, C2, C3 |
| ✅ | 5 | Dogfood both surfaces: Practice (ScoreR) transport/metronome/count-in/now-next/display toggles/debug panel unchanged; Chord Sheets play/stop/tempo/soundfont still work AND metronome + count-in now work, marker-mode/Show-tab/export unchanged and export still light-pinned. Use CHORDFLOW_DEVTOOLS to spot-check engine.metronomeVolume live. | — | recompose-scorer, recompose-chord-sheets-page | C4 |
| ✅ | 6 | Update loom/refs/chordflow-architecture-reference.md with a diagram of the ScoreR/ChordSheetR-page → PlayerControlsR → ChordFlowPlayback composition and the engine event bus, plus prose for the new shared component and dependency edge. | loom/refs/chordflow-architecture-reference.md | verify-parity-on-both-pages | IN7 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:engine-event-bus -->
### Step 1 — Engine event bus

Turn the single cb.* callbacks into per-event subscriber lists dispatched by a small emit helper. `create` seeds the passed onBeat/onStateChange/onReady/onFinished/onSoundFontsListed onto the buses (back-compat). Expose `on(event, handler)` on the returned handle. No behavior change for existing consumers yet.

<!-- step:playercontrolsr-component -->
### Step 2 — PlayerControlsR component

opts = { soundFont=true, metronome=true, countIn=true, onToggleNowNext=null }. now-next toggle rendered only when onToggleNowNext is supplied. Returns { el, setTempoValue(bpm), getTempo(), dispose }. Handlers call engine.play/stop/setTempo/setSoundFont/setMetronome/setCountIn. try/catch+console.error around each handler so a broken handler surfaces instead of being swallowed by the DOM dispatcher.

<!-- step:recompose-scorer -->
### Step 3 — Recompose ScoreR

Wire the engine created in ScoreR to PlayerControlsR (pc = ChordFlowPlayerControls.create(strip, engine, { onToggleNowNext: opts.onToggleNowNext })). Remove the metronome/count-in toggles + their handler branch from ScoreR (now owned by PlayerControlsR); syncToggle stays for the remaining display toggles.

<!-- step:recompose-chord-sheets-page -->
### Step 4 — Recompose Chord Sheets page

Engine still created in setupEngine (page owns it, C1). PlayerControlsR mounts where buildTransport's transport was; marker-mode select + Show-tab checkbox remain page-owned. Soundfont picker moves into PlayerControlsR (still the single global host-persisted choice, C3). ChordSheetR untouched (C2).

<!-- step:verify-parity-on-both-pages -->
### Step 5 — Verify parity on both pages

No new automated harness (EX4) — manual verification, matching the rest of wwwroot. Confirm the soundfont picker reflects the same global choice on both pages.

<!-- step:architecture-ref-diagram -->
### Step 6 — Architecture ref + diagram

Diagram (mermaid or ASCII, matching the ref's existing style) showing both surfaces mounting the shared PlayerControlsR, each over its own ChordFlowPlayback, with PlayerControlsR self-subscribing via engine.on(). Final step so the diagram reflects the landed shape (Rafa's request).
