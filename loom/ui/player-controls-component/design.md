---
type: design
id: de_01KXKV7EN0F0QCC3196R17Z3AC
title: Shared PlayerControlsR — extract the player transport from ScoreR so both render surfaces mount it
status: done
created: 2026-07-15
version: 1
idea_version: 1
tags: []
parent_id: id_01KXKSWXMZG7Y6K5S25042XVZZ
requires_load: []
---
# Shared PlayerControlsR — extract the player transport from ScoreR so both render surfaces mount it

## Context

The two render surfaces are asymmetric today. **ScoreR** (`score-render-component.js`) is a fat player-widget that owns a `ChordFlowPlayback` engine **and** a control strip mixing player controls (play/stop/tempo/soundfont/metronome/count-in/now-next) with notation-display controls. **ChordSheetR** (`chord-sheet-render-component.js`) is a pure SVG drawer; the chord-sheets **page** hand-rolls a *partial* transport (play/stop/tempo/sound/marker/Show-tab) and never got metronome/count-in/now-next. That duplication is what let the `syncToggle` regression hide and what left Chord Sheets short of controls (origin: `playback/metronome-countin-fix/chat-001`).

**Goal:** extract the shared player controls into one `PlayerControlsR` component both surfaces mount, so the transport + metronome + count-in behave identically everywhere and live in **one** place.

**Non-goals (this thread):** now-next *boards* on Chord Sheets (a data feed from `cellSchedule`) — deferred to a fast follow. No alphaTab/soundfont changes. No new automated test harness.

## Key design decision — the engine event seam

The idea framed PlayerControlsR as "binds to a handle, doesn't own the engine." Keeping that (ScoreR must keep owning its engine + surface, because that surface *is* its rendered staff), the clean way to let a controls widget react to the engine without each page re-forwarding events is to make **`ChordFlowPlayback` a small multi-subscriber event source**.

Today the engine takes single callbacks at `create` (`onBeat`, `onStateChange`, `onReady`, `onFinished`, `onSoundFontsListed`). We add a light fan-out:

```js
engine.on("stateChange", cb);  // playing:bool
engine.on("ready", cb);        // soundfont loaded → transport enable
engine.on("soundFontsListed", cb);  // (fonts, selectedId)
engine.on("beat", cb);         // (bar, beat) 1-based
engine.on("finished", cb);
```

The existing `create({ onBeat, … })` callbacks stay as sugar that register on the same buses (back-compat, and the page can still pass `onBeat`/`onFinished` there). Now **PlayerControlsR self-subscribes** to `stateChange`/`ready`/`soundFontsListed` — no per-page forwarding, so there is no second place to drift. This is the durable fix for the class of bug that started this thread.

> Decision to confirm: this **refines** the idea's wording. PlayerControlsR still does not *own* the engine (the page/component creates it and owns its lifecycle + surface); it *binds* to the handle and *subscribes* to it. If you'd rather PlayerControlsR own the engine outright, say so — but I recommend handle-bind, because ScoreR owning its own render surface is the more natural boundary.

## PlayerControlsR — the component

`wwwroot/player-controls-component.js`, `window.ChordFlowPlayerControls`.

```js
const pc = ChordFlowPlayerControls.create(container, engine, {
  soundFont: true,          // show the soundfont picker (default true)
  metronome: true,          // show the metronome toggle (default true)
  countIn:   true,          // show the count-in toggle (default true)
  onToggleNowNext: null,    // fn(visible) → show the now-next toggle ONLY when supplied
});
// returns:
//   pc.el            — the controls strip DOM node (consumer decides where to mount)
//   pc.setTempoValue(bpm)  — seed the tempo input without firing setTempo (post-load)
//   pc.getTempo()    — current input BPM
//   pc.dispose()
```

Internally it:
- Builds play/pause · stop · tempo · (soundfont) · (metronome) · (count-in) · (now-next) — each optional control omitted when its opt is false/absent.
- Wires each control to the engine handle: `engine.play/stop`, `engine.setTempo`, `engine.setSoundFont`, `engine.setMetronome`, `engine.setCountIn`.
- Subscribes to the engine: `ready`→enable transport + tempo, `stateChange`→play/pause label, `soundFontsListed`→fill the picker, `finished`→reset label.
- **Wraps every control handler in a log-not-die guard** (`try { … } catch (e) { console.error("[PlayerControlsR]", name, e); }`) so a future broken handler surfaces in the console instead of being swallowed by the DOM `change` dispatcher (the exact failure mode of the `syncToggle` regression).

The now-next toggle is a *view* toggle: PlayerControlsR just calls `onToggleNowNext(visible)`; the boards themselves stay the consumer's `ChordFlowNowNext` (Practice today; Chord Sheets = fast follow).

## How ScoreR recomposes

ScoreR keeps owning the engine + the `cf-score-surface` + `getApi()` (its staff, cursor, notation, debug panel all need the api). Its `buildControls` splits:
- **PlayerControlsR** takes over play/stop/tempo/soundfont/metronome/count-in/now-next. ScoreR mounts `pc.el` into its strip and drops that wiring (incl. the restored `syncToggle` path for metronome/count-in — now handled once inside PlayerControlsR).
- **ScoreControls (stays in ScoreR):** staff-display profile, chord-names, diagrams-over-staff/on-top, auto-layout, key/feel pickers, scroll-mode, the alphaTex debug panel. These call `getApi()` / re-render and are notation concerns, not player concerns. `syncToggle` stays for *these* display toggles.

Net: ScoreR's public API and Practice behavior are unchanged; only the internal composition moves.

## How the Chord Sheets page recomposes

`chord-sheets.js` keeps owning its engine (`setupEngine`) and its display/export controls. Its `buildTransport` is replaced:
- Play/stop/tempo/soundfont → **PlayerControlsR** (mounted where the hand-rolled transport was), and it **gains metronome + count-in** for free.
- **ChordSheetControls (stays):** layout A/B, notation + secondary line, adornments, tone-labels, theme, export (SVG/PNG/PDF), **marker-mode** (visual-metronome vs per-chord — chord-sheets-only), **Show-tab** (reveals the hidden staff surface — chord-sheets-only).
- now-next toggle: not shown here this thread (no `onToggleNowNext`) — arrives with the fast-follow.
- Keep the `cf-score-surface` fix on the hidden staff surface (already landed).

## Parity / migration checklist (validation)

- Practice: transport, metronome, count-in, now-next, all display toggles, and the debug panel behave exactly as before.
- Chord Sheets: play/stop/tempo/soundfont still work; **metronome + count-in now work**; marker-mode, Show-tab, export unchanged and export stays light-pinned.
- Soundfont picker on both pages reflects the single global host-persisted choice.
- Dogfood on both pages; use `CHORDFLOW_DEVTOOLS` to spot-check `engine.metronomeVolume` etc. live.

## Reference-doc sync (required)

Landing this updates **app architecture**: a new shared UI component + a dependency edge (ScoreR / chord-sheets page → **PlayerControlsR** → `ChordFlowPlayback`, plus the engine's new `on()` event API). Update `loom/refs/chordflow-architecture-reference.md` in the same unit of work.

## Deferred / open

- **now-next boards on Chord Sheets** — needs a current/next-chord feed derived from `cellSchedule`; fast follow (separate thread or a trailing step).
- **No JS test harness** for `wwwroot` — validation is manual dogfood, as elsewhere in this project.