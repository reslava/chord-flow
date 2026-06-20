---
type: done
id: pl_01KVJE3TVR7Q3HKJQ1PA6TGVHD-done
title: Done — CAGED shapes — dogfood page implementation
status: done
created: 2026-06-20
version: 6
tags: []
parent_id: pl_01KVJE3TVR7Q3HKJQ1PA6TGVHD
requires_load: []
---
# Done — CAGED shapes — dogfood page implementation

## Step 1 — Zone-band capability: FretboardDiagram optional zone field + fretboard-render-component band draw layer

`FretboardDiagram` gained optional `ZoneFretMin`/`ZoneFretMax` (nullable, default null — existing positional callers unaffected). `fretboard-render-component.js` draws a translucent band (`#ffd54f33`) behind the `[zoneFretMin..zoneFretMax]` fret columns in **both** `buildSvg` (vertical) and `buildSvgHorizontal`, beneath the grid + markers, clamped to the visible window. A diagram with no zone (chords, scales) renders byte-identical. Reusable — keyed only off the model, no caged-specific code.

## Step 2 — CagedShapeDiagram producer over OctaveShape — anchors as markers, carries the octave Zone (band) + sets a context fret window + unit tests

`CagedShapeDiagram.Build(shape, root, maxFret=15)` (Instruments/Guitar/Diagrams), mirroring `IntervalSetDiagram`. Markers = `OctaveShape.AnchorsFor` anchors, each labelled `1`/`8`/`15` via `IntervalLattice.LabelAt(primary, anchor)`, Note via `NoteSpeller`, Function `root`. `ZoneFretMin/Max` = the anchors' span (`OctaveShape.Zone`); `FretMin/FretMax` = that zone widened by a 2-fret context margin (clamped ≥ 0). 11 unit tests, all green — markers match the anchors for all five shapes, carried zone = `OctaveShape.Zone`, window = zone ± 2, labels 1/8/15, and the D-shape str2 = fret 13 regression.

## Step 3 — Caged Core slice: CagedShapesHandler + CagedEnvelopes + cagedPreview verb on WebMessageRouter

Caged Core slice (Features/Caged), mirroring the Scales slice. `CagedShapesHandler.Preview(string shape, int rootPitchClass)` — `Enum.TryParse<CagedShape>` (ignoreCase) → `FormatException` on an unknown shape; returns `CagedDiagramEnvelope`. `CagedEnvelopes`: `CagedDiagramEnvelope("cagedDiagram")` + `CagedErrorEnvelope("cagedError")`. `WebMessageRouter`: a `cagedPreview` case + `CagedPreviewRequested` event (mirroring `ScalePreviewRequested`) + a `Shape` field on the inbound envelope (deserialized by name, so positional order is irrelevant).

## Step 4 — Desktop host wiring in Program.cs (instantiate handler, wire CagedPreviewRequested)

`Program.cs`: `using ChordFlow.Features.Caged;`, instantiate `var caged = new CagedShapesHandler();` next to `scales`, and wire `router.CagedPreviewRequested += (shape, rootPc) => { try { bridge.Send(caged.Preview(shape, rootPc)); } catch (FormatException ex) { bridge.Send(new CagedErrorEnvelope(ex.Message)); } };` — the exact scales-style try/catch → inline error path. Desktop builds clean (0 errors).

## Step 5 — JS view caged-shapes.js (shape + root selectors) + index.html tab + app.js registration

`caged-shapes.js` (`ChordFlowCagedShapes`) mirrors `scales.js`: a Shape `<select>` (C/A/G/E/D, default E) + Root `<select>` (default A), sends `{type:"cagedPreview", shape, rootPitchClass}`, renders `cagedDiagram` via `ChordFlowFretboard` (horizontal, `controls:{orientation:false}`) with a root-family palette (`1`→bright red, `8`/`15`→dimmer reds, `*`→black); `cagedError` inline. `index.html`: a `navCaged` button, the `caged-shapes-view` section (Shape/Root selects), and the `caged-shapes.js` script tag. `app.js`: a `caged` entry in the `views` map (`onShow → ChordFlowCagedShapes.show()`).

## Step 6 — Build + dogfood visual check: step through C/A/G/E/D at a few keys

Build + dogfood validation complete. Full Core suite **564 passed / 0 failed**; Desktop builds clean (0 errors). Rafa ran the app and stepped through C/A/G/E/D — anchors land correctly and the octave-zone band frames them; the D-shape octave-up and G/E str1=str6 anchors read right. "All working nicely." Standing dogfood obligation for `octave-shapes` (its req EX5) satisfied.
