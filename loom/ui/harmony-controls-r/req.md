---
type: req
id: rq_01KXN9QE9TANNVBPQ2WGHHPJ4B
title: HarmonyControlsR + one Practice page — design — Requirements
status: locked
created: 2026-07-16
updated: 2026-07-16
version: 1
design_version: 1
tags: []
parent_id: de_01KXN9DWFJG6QWET6VVFFS36HD
requires_load: []
---
# HarmonyControlsR + one Practice page — design — Requirements

### ✅ Included

- `IN1` A shared **HarmonyControlsR** component (PlayerControlsR precedent) holding the definition strip: harmony picker, Key, Feel, Comping + Rhythm vol, Lead + Lead vol, Difficulty, Voicing-fret window (min–max), and the Generate / Save / Mark practiced actions.
- `IN2` **One Practice page** with a Score ⇄ Sheet segmented view toggle; the standalone Chord Sheets page and its nav button are removed.
- `IN3` **Full convergence on the bridge (Option A)**: `generate` / `loadExercise` are the only render-producing requests; the reply carries both projections — score alphaTex + chord-sheet model — plus the shared schedules (`chordSchedule` for Now/Next, `cellSchedule` for the sheet marker); the `chordSheet` request/reply retires.
- `IN4` Key + Feel live in HarmonyControlsR and behave exactly as Practice today: seeded on harmony *switch* (song's `initialKey` / `defaultFeel`), a manual edit survives until the next switch.
- `IN5` Definition controls always show a **concrete value, never blank**: a song without a key seeds C; defaults are C / 80 BPM / Straight.
- `IN6` One shared instance of everything page-level: one playback engine, one PlayerControlsR, one Now/Next, one saved-exercise library — all visible/active in both views.
- `IN7` The Score ⇄ Sheet toggle works **mid-playback**: audio continues, the score cursor and the sheet bar marker keep tracking the same beat.
- `IN8` The harmony picker is literally the same component everywhere it appears (the old `Sheet` vs `Harmony` combo drift is eliminated by construction — one population path fed `entityList` payloads).
- `IN9` Each view keeps its view-specific control strip: Score — staff/notation toggles + debug panel; Sheet — Layout, Chords, + line, Below cell, Tone labels, Theme, Marker mode, and the three exports (SVG/PNG/PDF).
- `IN10` The sheet reply always carries the resolved comping-grip tone/diagram data, so the Below-cell adornment becomes a pure display toggle (no re-request).

### ❌ Excluded

- `EX1` Changes to `ChordSheetR` (the pure-SVG sheet renderer) internals and `PlayerControlsR` internals — both are consumed as-is.
- `EX2` Changes to the Content-CRUD preview — it keeps using ScoreR's opt-in key/feel/volume controls (it previews single entities; it is not a definition builder).
- `EX3` Changes to the export mechanics (SVG/PNG client-side, PDF via `#chord-sheet-print` + host print) — they move with the Sheet view unchanged.

### ⛓ Constraints

- `C1` Tempo stays a PlayerControlsR param: HarmonyControlsR never owns a tempo control; a harmony switch seeds tempo through a shell hook (`onHarmonySwitch` → `pc.setTempoValue`).
- `C2` ScoreR keeps its opt-in key/feel/volume controls for other consumers (Content-CRUD preview); the Practice page mounts ScoreR with them off (including a new `volumes: false` opt).
- `C3` Seeding fires on harmony switch only; the `loadExercise` reply path seeds the stored override values (override wins over content defaults).
- `C4` Both view surfaces stay mounted for the mid-playback toggle; the hidden surface must keep a valid alphaTab layout (hide via `visibility`/collapsed overflow, or re-`render()` on reveal).
- `C5` The Sheet view owns no engine: its marker consumes the shell's beat fan-out from the single page engine.
- `C6` A code change to the app architecture (page structure, bridge envelopes) updates `loom/refs/chordflow-architecture-reference.md` in the same unit of work.