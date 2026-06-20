---
type: plan
id: pl_01KVJE3TVR7Q3HKJQ1PA6TGVHD
title: CAGED shapes — dogfood page implementation
status: done
created: 2026-06-20
updated: 2026-06-20
version: 2
design_version: 1
req_version: 1
tags: []
parent_id: id_01KVJDCQHPYT5V3H7VBN5AXJYJ
requires_load: []
target_version: 0.1.0
steps:
  - id: zone-band-capability-reusable
    order: 1
    status: done
    description: "Zone-band capability: FretboardDiagram optional zone field + fretboard-render-component band draw layer"
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Diagrams/FretboardDiagram.cs, src/ChordFlow.Desktop/wwwroot/fretboard-render-component.js]
    blocked_by: []
    satisfies: [IN8, C3]
  - id: cagedshapediagram-producer
    order: 2
    status: done
    description: CagedShapeDiagram producer over OctaveShape — anchors as markers, carries the octave Zone (band) + sets a context fret window + unit tests
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Diagrams/CagedShapeDiagram.cs, tests/ChordFlow.Core.Tests/CagedShapeDiagramTests.cs]
    blocked_by: []
    satisfies: [IN1, IN6, IN7, C2]
  - id: caged-vertical-slice-core
    order: 3
    status: done
    description: "Caged Core slice: CagedShapesHandler + CagedEnvelopes + cagedPreview verb on WebMessageRouter"
    files_touched: [src/ChordFlow.Core/Features/Caged/CagedShapesHandler.cs, src/ChordFlow.Core/Features/Caged/CagedEnvelopes.cs, src/ChordFlow.Core/Bridge/WebMessageRouter.cs]
    blocked_by: []
    satisfies: [IN2, C1, C4]
  - id: desktop-host-wiring
    order: 4
    status: done
    description: Desktop host wiring in Program.cs (instantiate handler, wire CagedPreviewRequested)
    files_touched: [src/ChordFlow.Desktop/Program.cs]
    blocked_by: []
    satisfies: [IN3, C1]
  - id: js-view-page-registration
    order: 5
    status: done
    description: JS view caged-shapes.js (shape + root selectors) + index.html tab + app.js registration
    files_touched: [src/ChordFlow.Desktop/wwwroot/caged-shapes.js, src/ChordFlow.Desktop/wwwroot/index.html, src/ChordFlow.Desktop/wwwroot/app.js]
    blocked_by: []
    satisfies: [IN4, IN5, C3]
  - id: build-dogfood-check
    order: 6
    status: done
    description: "Build + dogfood visual check: step through C/A/G/E/D at a few keys"
    files_touched: []
    blocked_by: []
    satisfies: [IN7, C5]
---
# CAGED shapes — dogfood page implementation

## Goal

Build the CAGED Shapes dogfood page as a faithful mirror of the Scales vertical slice: a new `CagedShapeDiagram` producer over the shipped `OctaveShape` (root anchors as markers, fret window = the octave `Zone`), a `caged` Core slice (handler + envelopes + a `cagedPreview` router verb), Desktop host wiring, and the `caged-shapes.js` JS view (CAGED-shape + root selectors) registered as a nav tab — rendered by the shipped `ChordFlowFretboard` locked horizontal. This closes the standing dogfood obligation for `octave-shapes` (its req `EX5`). The octave zone is drawn as a **shaded band** — a small reusable layer added to the fretboard component (`IN8`), with the producer carrying the zone + a context window. Pure Core producer (Domain-only deps; arch guards green); chord/scale diagrams stay byte-identical.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Zone-band capability: FretboardDiagram optional zone field + fretboard-render-component band draw layer | src/ChordFlow.Core/Instruments/Guitar/Diagrams/FretboardDiagram.cs, src/ChordFlow.Desktop/wwwroot/fretboard-render-component.js | — | IN8, C3 |
| ✅ | 2 | CagedShapeDiagram producer over OctaveShape — anchors as markers, carries the octave Zone (band) + sets a context fret window + unit tests | src/ChordFlow.Core/Instruments/Guitar/Diagrams/CagedShapeDiagram.cs, tests/ChordFlow.Core.Tests/CagedShapeDiagramTests.cs | — | IN1, IN6, IN7, C2 |
| ✅ | 3 | Caged Core slice: CagedShapesHandler + CagedEnvelopes + cagedPreview verb on WebMessageRouter | src/ChordFlow.Core/Features/Caged/CagedShapesHandler.cs, src/ChordFlow.Core/Features/Caged/CagedEnvelopes.cs, src/ChordFlow.Core/Bridge/WebMessageRouter.cs | — | IN2, C1, C4 |
| ✅ | 4 | Desktop host wiring in Program.cs (instantiate handler, wire CagedPreviewRequested) | src/ChordFlow.Desktop/Program.cs | — | IN3, C1 |
| ✅ | 5 | JS view caged-shapes.js (shape + root selectors) + index.html tab + app.js registration | src/ChordFlow.Desktop/wwwroot/caged-shapes.js, src/ChordFlow.Desktop/wwwroot/index.html, src/ChordFlow.Desktop/wwwroot/app.js | — | IN4, IN5, C3 |
| ✅ | 6 | Build + dogfood visual check: step through C/A/G/E/D at a few keys | — | — | IN7, C5 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:zone-band-capability-reusable -->
### Step 1 — Zone-band capability (reusable)

Add an optional zone fret range to `FretboardDiagram` (e.g. `ZoneFretMin`/`ZoneFretMax` ints, nullable — default null). In `fretboard-render-component.js`, draw a **translucent band** behind the `[ZoneFretMin..ZoneFretMax]` fret columns in **both** `buildSvgHorizontal` and `buildSvg`, beneath the markers. A diagram with no zone (chords, scales) renders byte-identical (no band). Reusable layer — keyed only off the diagram, no caged-specific code in the component.

<!-- step:cagedshapediagram-producer -->
### Step 2 — CagedShapeDiagram producer

`Build(CagedShape shape, PitchClass root, int maxFret = 15) → FretboardDiagram`, mirroring `IntervalSetDiagram`. Anchors from `OctaveShape.AnchorsFor(root, shape, 0, maxFret)`; one `FretboardMarker` per anchor (Note via `NoteSpeller`, Interval label via `IntervalLattice.LabelAt(primary, anchor)` → `1`/`8`/`15`, Function `root`, `MarkerShape.Circle`). `FretMin`/`FretMax` from `OctaveShape.Zone`. No muted strings, no barre. Adds no geometry/vocabulary (C2). Tests: all five shapes at a key place markers on the anchors, window = zone, and the D-shape str2 = fret 13 (not the in-window unison).

<!-- step:caged-vertical-slice-core -->
### Step 3 — Caged vertical slice (Core)

Mirror `ScalesHandler`/`ScalesEnvelopes`. `CagedShapesHandler.Preview(CagedShape shape, int rootPitchClass) → CagedDiagramEnvelope(FretboardDiagram, "cagedDiagram")`; `CagedErrorEnvelope(Message, "cagedError")` for a bad request. Add a `cagedPreview` verb to `WebMessageRouter` (parse `{type:"cagedPreview", shape, rootPitchClass}`) + a `CagedPreviewRequested` event mirroring `ScalePreviewRequested`. Stateless, pure.

<!-- step:desktop-host-wiring -->
### Step 4 — Desktop host wiring

Instantiate `var caged = new CagedShapesHandler();` and wire `router.CagedPreviewRequested += (shape, rootPc) => bridge.Send(caged.Preview(shape, rootPc));` next to the existing `ScalePreviewRequested` wiring (with the same try/catch → error-envelope path).

<!-- step:js-view-page-registration -->
### Step 5 — JS view + page registration

`caged-shapes.js` (`ChordFlowCagedShapes`) mirrors `scales.js`: a CAGED-shape `<select>` (C/A/G/E/D) + root-note `<select>`, sends `{type:"cagedPreview", shape, rootPitchClass}`, renders `cagedDiagram` via `ChordFlowFretboard.create(el, {orientation:"horizontal", labelMode:"interval", controls:{orientation:false}, palette:{"1":"#e2574c","*":"#000"}})`; `cagedError` shown inline. Add a nav button + `caged-shapes-view` container to `index.html`, and register the view in `app.js`'s `views` map (`onShow → ChordFlowCagedShapes.show()`).

<!-- step:build-dogfood-check -->
### Step 6 — Build & dogfood check

Build the solution + run the full test suite (producer tests + arch guards green), then run the Desktop app and step through C/A/G/E/D at a couple of keys: confirm the root anchors land on the right strings/frets and the neck frames to the octave zone — especially D (str2 octave-up, not the unison) and G/E (str1 = str6 same fret). This is the standing dogfood validation for octave-shapes (C5).
