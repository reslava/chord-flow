---
type: req
id: rq_01KVJE1P8S806QHK7JZRK7AYGG
title: CAGED shapes — fretboard dogfood page (octave-shapes visual check) — Requirements
status: locked
created: 2026-06-20
updated: 2026-06-20
version: 2
tags: []
parent_id: id_01KVJDCQHPYT5V3H7VBN5AXJYJ
requires_load: []
---
# CAGED shapes — fretboard dogfood page (octave-shapes visual check) — Requirements

Authoritative scope for the `caged-shapes` (ui) thread — extracted from `caged-shapes-idea.md` + chat-001. A **faithful mirror of the Scales vertical slice** with a new `CagedShapeDiagram` producer over the shipped `[[octave-shapes|OctaveShape]]`. The octave zone is shown by a **shaded band** (flipped in chat-001 — Rafa wants it in v1); the band is a small reusable `[[fretboard-render-component]]` layer. No design doc — small thread, settled in chat-001.

### ✅ Included

- `IN1` `CagedShapeDiagram` producer (`Instruments/Guitar/Diagrams/`): `Build(CagedShape shape, PitchClass root, int maxFret = 15) → FretboardDiagram` — markers = the shape's root anchors from `OctaveShape.AnchorsFor`, each carrying its octave-aware interval label (`1`/`8`/`15` via `IntervalLattice`) + note name (`NoteSpeller`). Adds no geometry/vocabulary (mirrors `IntervalSetDiagram`). Unit-tested.
- `IN2` The **caged vertical slice** (Core): `CagedShapesHandler.Preview(shape, rootPc) → CagedDiagramEnvelope` + `CagedEnvelopes` (diagram + error records), mirroring `ScalesHandler` / `ScalesEnvelopes`; a `cagedPreview` → `cagedDiagram` / `cagedError` verb on `WebMessageRouter`.
- `IN3` Desktop host wiring (`Program.cs`): instantiate `CagedShapesHandler`, wire `router.CagedPreviewRequested → bridge.Send(handler.Preview(...))` (mirrors the `ScalePreviewRequested` wiring).
- `IN4` JS view `caged-shapes.js` (`ChordFlowCagedShapes`): a CAGED-shape selector (C/A/G/E/D) + root-note selector, sends `cagedPreview`, renders the returned `FretboardDiagram` via `ChordFlowFretboard` locked to horizontal (`controls:{orientation:false}`). Mirrors `scales.js`.
- `IN5` Page registration: a nav tab + `caged-shapes-view` container in `index.html`, registered in `app.js` (the `views` map; `onShow → ChordFlowCagedShapes.show()`).
- `IN6` Octave-zone shown by a **shaded band**: the producer carries the `OctaveShape.Zone` on the diagram and sets a **context fret window** (the zone widened by a small margin) so the band reads in the neck's context; the band tints the zone's fret columns. *(amended chat-001: was framing-only.)*
- `IN7` Tests: `CagedShapeDiagram` unit tests — for all five shapes at a key, markers land on the anchors, the carried zone = `OctaveShape.Zone`, and the **D-shape octave-up** (str2 fret 13, not the unison) is asserted. Plus the standing **dogfood** visual check (step through C/A/G/E/D in the running app).
- `IN8` Zone-band capability (reusable): `FretboardDiagram` carries an **optional zone fret range**, and `[[fretboard-render-component]]` gains a **band draw layer** — a translucent rect behind the `[min,max]` fret columns (both orientations). Diagrams that pass no zone (chords, scales) render **byte-identical**. *(added chat-001.)*

### ❌ Excluded

- `EX1` Chord-quality rendering (full CAGED chords) — `[[caged-system]]`.
- `EX2` CAGED **boxes** as a drawn layer — additive once anchors + zone read well.
- `EX3` ~dropped~ — **now in scope** (`IN6` + `IN8`). *(was: a shaded zone band deferred; flipped in chat-001 — Rafa wants the band in v1.)*
- `EX4` Persistence, page polish, alternate tunings.
- `EX5` A root-**fret** / neck-region picker — root-note only (lowest occurrence) in v1.
- `EX6` Any change to `OctaveShape` / Core geometry — consumed as shipped.

### ⛓ Constraints

- `C1` Mirror the Scales vertical slice (handler / envelopes / router verb / JS view / registration) — no new patterns or seams.
- `C2` The producer adds no geometry/vocabulary: it calls `OctaveShape` + `Fretboard` / `NoteSpeller` / `IntervalLattice` only (theory stays upstream). Pure, deterministic, unit-tested.
- `C3` Reuse the shipped `ChordFlowFretboard`; the **only** new rendering capability is the additive **zone-band layer** (`IN8`), which leaves chord/scale diagrams byte-identical. *(amended chat-001.)*
- `C4` Dependency direction: producer in `Instruments` (Domain-only deps), handler in `Features`, Desktop wires it — the arch guards stay green.
- `C5` Standing dogfood rule: this page **is** the visual validation of `[[octave-shapes]]` before `[[caged-system]]` builds on it.
- `C6` Sequencing: depends on `[[octave-shapes]]` (shipped) + `[[fretboard-render-component]]` (shipped) + `[[intervals-scales]]` (the slice pattern + horizontal orientation).