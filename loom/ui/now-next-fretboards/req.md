---
type: req
id: rq_01KVT07CYYVRWSR52383VKVEX9
title: Now/Next Fretboards — Requirements
status: locked
created: 2026-06-23
updated: 2026-06-23
version: 1
tags: []
parent_id: id_01KVSYWCXFGDW3EX0Q02MR8895
requires_load: []
---
# Now/Next Fretboards — Requirements

### ✅ Included

- `IN1` Engine emits a **chord schedule** as a by-product of the render walk (design D1 = Option A): the render seam returns `(tex, schedule)` where `schedule` is one `ChordChange(int Bar, int Beat, string Name, FretboardDiagram Diagram)` per **chord change**, captured at the boundaries the renderer already detects (`RenderState.CurrentChordName`).
- `IN2` New diagram producer **`RealizedVoicingDiagram.Build(Chord chord, Voicing voicing, Key key)` → `FretboardDiagram`** of the concrete comped voicing at its **real root** (same marker logic as `VoicingDiagram`, anchored at `chord.Root` not canonical-C; `Title = ChordSymbol.Format(chord, key)`).
- `IN3` `LoadScoreEnvelope` gains a **`schedule`** field carrying `ChordChange[]` (camelCase serialization, incl. each entry's `FretboardDiagram`, matching the existing diagram model the FretR already consumes).
- `IN4` New **sibling shared JS module** `now-next-fretboards.js` (`window.ChordFlowNowNext`): `create(container) → { setSchedule(schedule), onBeat(bar, beat), reset(), dispose() }`. Owns the two FretR, the beat→schedule lookup, and the now/next update logic.
- `IN5` Two FretR **side-by-side** above the ScoreR (now | next), fixed **vertical chord-box** (`controls:{ orientation:false, fretWindow:false }`), each with a static "Now"/"Next" caption; the chord name shows via the diagram `title`. "Next" = next distinct change; **blank/"—" past the last entry**.
- `IN6` Drive the now/next update off the **existing** `activeBeatsChanged` handler (`score-render-component.js`) via its `onBeat(bar, beatInBar)` signal — **no new alphaTab event wiring**. Reset on `loadScore` and on `stop`.
- `IN7` Schedule is built from the **comping track only** (track 0 invariant); works for **mono- and multi-track** scores.
- `IN8` Mount the module on **Practice** (`app.js`): forward `loadScore.schedule → setSchedule`, the score component's `onBeat → onBeat`; this playback surface is the dogfood for the feature.

### ❌ Excluded

- `EX1` Guide tones / scales / arpeggios overlays (this slice proves the now/next-chord signal only).
- `EX2` Canonical/CAGED-shape toggle for the FretR (comped voicing only).
- `EX3` Any harmony re-derivation in JS — chords come **only** from the engine schedule.
- `EX4` Retrofitting the Content voicing-preview onto the new real-root producer (movable-root unification of `VoicingDiagram` is separate).
- `EX5` The rejected **sibling-builder** production path (re-walking the song independently of the renderer).
- `EX6` Mounting on the Progressions/Songs views — the module is built reusable for them, but only Practice is wired in this slice.

### ⛓ Constraints

- `C1` The C# engine is the **single source of truth** for harmony; JS never derives chords (reinforces EX3).
- `C2` The now FretR shows the **same voicing the tab comps** for that chord — guaranteed structurally by producing the schedule from the render pass (IN1), not a parallel builder (EX5).
- `C3` Reuse shared components: `ChordFlowFretboard` for the boxes and the existing `activeBeatsChanged` handler. FretR stays a **dumb view** — no music theory in JS.
- `C4` Beat lookup keyed **positionally** `(bar.index, beat.index)`, **not** `beat.id`; the renderer's `(Bar, Beat)` ordinals must be verified to match alphaTab's post-parse indexing (rests/tuplets are the risk).
- `C5` `RealizedVoicingDiagram` lives under `Instruments/Guitar/Diagrams/`; the `Rendering → Instruments` edge stays allowed and `Music/` stays instrument-agnostic.
- `C6` Update `loom/refs/chordflow-architecture-reference.md` (Bridge contract: `loadScore` gains `schedule`; new `ChordFlowNowNext` module) in the **same unit of work** (reference-doc sync rule).
- `C7` xUnit tests cover `RealizedVoicingDiagram` (real-root markers/functions) and schedule emission (positions + names at chord changes).
