---
type: req
id: rq_01KXKV82RYA68Q6BY0FKQKMRKH
title: Shared PlayerControlsR — extract the player transport from ScoreR so both render surfaces mount it — Requirements
status: locked
created: 2026-07-15
updated: 2026-07-15
version: 1
design_version: 1
tags: []
parent_id: de_01KXKV7EN0F0QCC3196R17Z3AC
requires_load: []
---
# Shared PlayerControlsR — extract the player transport from ScoreR so both render surfaces mount it — Requirements

### ✅ Included

- `IN1` A shared **PlayerControlsR** component (`wwwroot/player-controls-component.js`, `window.ChordFlowPlayerControls`) rendering the player transport — play/pause, stop, tempo, soundfont picker — plus **metronome** and **count-in** toggles, bound to a `ChordFlowPlayback` handle.
- `IN2` A light multi-subscriber event API on `ChordFlowPlayback` (`engine.on("stateChange"|"ready"|"soundFontsListed"|"beat"|"finished", cb)`), with the existing `create({onBeat,…})` callbacks kept as sugar registering on the same buses. PlayerControlsR self-subscribes to `stateChange`/`ready`/`soundFontsListed` (no per-page event forwarding).
- `IN3` **ScoreR** recomposed to mount PlayerControlsR for its player controls; ScoreR keeps owning the engine + `cf-score-surface` + `getApi()`, and its notation-display controls (staff-profile, chord-names, diagrams, auto-layout, key/feel, scroll-mode, debug panel) stay in ScoreR.
- `IN4` The **Chord Sheets page** (`chord-sheets.js`) recomposed to mount PlayerControlsR in place of its hand-rolled transport, **gaining metronome + count-in**; its display/export controls, **marker-mode**, and **Show-tab** stay.
- `IN5` An **optional now-next toggle** in PlayerControlsR, rendered only when the consumer supplies an `onToggleNowNext` handler (Practice supplies it; Chord Sheets does not, this thread).
- `IN6` **Log-not-die** wrapping of every PlayerControlsR control handler, so a failing handler surfaces in the console instead of being swallowed by the DOM event dispatcher (prevents the `syncToggle`-class silent failure).
- `IN7` Update `loom/refs/chordflow-architecture-reference.md` with the new component and dependency edge in the same unit of work.

### ❌ Excluded

- `EX1` **now-next fretboard boards on Chord Sheets** (the current/next-chord feed derived from `cellSchedule`) — fast follow, separate thread or trailing step.
- `EX2` Any change to `alphaTab.min.js` or the soundfont assets.
- `EX3` A cross-page shared transport or a single shared engine instance.
- `EX4` An automated JS test harness for `wwwroot` — validation is manual dogfood, as elsewhere in this project.

### ⛓ Constraints

- `C1` PlayerControlsR **binds to a `ChordFlowPlayback` handle** and does not own/create the engine; each surface owns its **own** engine instance (preserves the "no cross-page transport / option a" decision from `chord-sheets/chord-sheets-playback`).
- `C2` **ChordSheetR stays a pure SVG drawer** (screen+export parity intact) — the shared controls live in the *page*, never in the drawer.
- `C3` The soundfont choice stays a **single global, host-persisted** value; both pages' pickers reflect the same selection.
- `C4` **Behavioral parity** — post-refactor, the Practice player behaves exactly as before (transport, metronome, count-in, now-next, display toggles, debug panel), and Chord-Sheets export (SVG/PNG/PDF) stays unchanged and light-pinned.