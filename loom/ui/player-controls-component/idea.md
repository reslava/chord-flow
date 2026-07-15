---
type: idea
id: id_01KXKSWXMZG7Y6K5S25042XVZZ
title: Shared PlayerControlsR — extract the player transport from ScoreR so both render surfaces mount it
status: done
created: 2026-07-15
version: 1
tags: []
parent_id: null
requires_load: []
---
# Shared PlayerControlsR — extract the player transport from ScoreR so both render surfaces mount it

## What

Extract a shared **PlayerControlsR** — the *player-kind* control strip (play/pause · stop · tempo · soundfont · **metronome** · **count-in** · **now-next** toggle) — out of ScoreR into its own render component that binds to a `ChordFlowPlayback` handle. Both render surfaces mount it, so the whole transport (and metronome/count-in/now-next in particular) behaves identically on the Practice page (ScoreR) and the Chord Sheets page (ChordSheetR).

Target shape (Rafa's model):

```
ScoreR      = ScoreControls      + Score       ─┐
                                                 ├─→ PlayerControlsR ⇄ ChordFlowPlayback
ChordSheetR = ChordSheetControls + ChordSheet  ─┘
```

## Why

Today the two render components are **asymmetric**:

- **ScoreR** is a *fat player-widget*: it owns a `ChordFlowPlayback` engine **and** a full control strip (transport + player-kind toggles + notation-display toggles).
- **ChordSheetR** is a *pure SVG drawer* (no engine, no controls — C1, one SVG for screen+export parity). The chord-sheets **page** hand-rolls a **partial** transport (play/stop/tempo/sound/marker/Show-tab), missing metronome / count-in / now-next.

Consequences already felt in `playback/metronome-countin-fix/chat-001`:

- The metronome/count-in "regression" was a `syncToggle` helper silently **dropped** during the ChordFlowPlayback extraction (commit `aadd147`) — a drift bug that a single shared control strip makes structurally impossible.
- Chord Sheets never got metronome / count-in / now-next because that wiring lives *inside* ScoreR, not in a reusable place.

Durable-over-minimal: **one place to add or fix a player control**, consumed by every surface.

## Shape / boundaries

- **PlayerControlsR (new, shared).** Pure UI over a `ChordFlowPlayback` **handle** — it does **not** own the engine. Renders + wires: play/pause, stop, tempo, soundfont picker, **metronome**, **count-in**, and the **now-next** toggle. Calls `engine.play/stop/setTempo/setSoundFont/setMetronome/setCountIn`; reflects `onStateChange` / `onReady` / `onSoundFontsListed`. Each page keeps owning its **own** engine instance — preserves the deliberate "no cross-page transport / option a" decision from `chord-sheets/chord-sheets-playback`.
- **ScoreControls (ScoreR-specific, stays).** Staff-display profile, chord-names, diagrams-over-staff / on-top, auto-layout, key/feel pickers, scroll mode, the alphaTex debug panel — notation-display concerns, not player concerns.
- **ChordSheetControls (chord-sheets-specific, stays).** Layout A/B, notation (letter/nashville/roman) + secondary line, below-cell adornment (tones/diagram/both), tone labels, theme, export (SVG/PNG/PDF), marker-mode (visual-metronome vs per-chord).
- **now-next boards.** The current/next chord fretboards are a separate component (`ChordFlowNowNext`) that each page mounts; PlayerControlsR only exposes the **toggle** that shows/hides them. Practice already wires this via `app.js`; Chord Sheets would newly mount `ChordFlowNowNext` too.

## Constraints / invariants

- **No cross-page shared engine.** PlayerControlsR takes a handle, never an owner role. The existing "each surface owns its `ChordFlowPlayback`" holds.
- **ChordSheetR stays a pure SVG drawer** (export-parity intact) — the shared controls sit in the *page*, not in the drawer.
- **Guard against silent handler failure.** The shared toggle handlers should log-not-die: the `syncToggle` `ReferenceError` was swallowed by the DOM `change` dispatcher — a small wrapper that catches + logs would have surfaced it immediately. Bake that in.

## Validation

- Metronome, count-in, and now-next work identically on the Practice page (ScoreR) **and** the Chord Sheets page (ChordSheetR).
- No regression to ScoreR's display toggles or the alphaTex debug panel.
- Chord-sheet export (SVG/PNG/PDF) still works and stays light-pinned (IN11).
- Dogfood: toggle each control on both pages, confirm audio + marker behaviour.

## Reference-doc sync (required)

This changes **app architecture** — a new shared UI render component and a dependency edge (ScoreR / chord-sheets page → **PlayerControlsR** → `ChordFlowPlayback`). Update `loom/refs/chordflow-architecture-reference.md` in the same unit of work when it lands.

## Origin

Surfaced in `loom/playback/metronome-countin-fix/chats/chat-001.md` — the metronome/count-in regression hunt, where the ScoreR/ChordSheetR asymmetry and the missing chord-sheets toggles were diagnosed. Related: the debug facility (`CHORDFLOW_DEVTOOLS` env var → devtools + `window.__cfApi`/`__cfEngine`) added in that thread is handy for verifying this refactor live.