---
type: plan
id: pl_01KVH04Y6PDNHTDEEAS5MYV7M2
title: Scales — interval-set fretboard page — Implementation
status: done
created: 2026-06-19
updated: 2026-06-19
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: id_01KVGZR52DTP3KQ3CNNHD6G6F9
requires_load: []
target_version: 0.1.0
actual_release: 0.8.0
steps:
  - id: intervalspeller-parse-inverse-vocabulary
    order: 1
    status: done
    description: "Add `IntervalSpeller.Parse(string token) -> int semitone` in `domain` (next to `Name`): map an interval label to a semitone, accepting flats, sharps, and naturals (`1 2 3 4 5 6 7`, `b2 b3 b5 b6 b7`, `#4 #5 #9 #11`, compound `9 11 13` unfolded). Reject unparseable tokens with a clear exception. It is NOT a literal inverse of `Name`'s flats-only output — sharps and compound tensions must parse. Add a `ParseSet(string)` helper (space-separated tokens -> ordered distinct semitones) or leave set-splitting to the producer; cover both flats and sharps in tests."
    files_touched: [src/ChordFlow.Core/Domain/IntervalSpeller.cs, tests/ChordFlow.Core.Tests/IntervalSpellerTests.cs]
    blocked_by: []
    satisfies: [IN2, C2]
  - id: scale-producer-intervalsetdiagram-build
    order: 2
    status: done
    description: "Add `IntervalSetDiagram.Build(IReadOnlyList<string> tokens | parsed semitones, PitchClass root, int maxFret = 15)` in `Instruments/Guitar/Diagrams/` (next to `FretboardDiagram`): for each parsed semitone, query `IntervalLattice.PositionsOfInterval(rootPos, semitone, 0, maxFret)` and emit one `FretboardMarker` per position (Note spelled via `NoteSpeller`, Interval label = the degree token, Function mapped from the semitone for a sensible default color, Shape = Circle). Leave `FretMin`/`FretMax` null so the view auto-fits to the markers (root-note + auto-window). No theory beyond parse + lattice query; reuse `IntervalLattice` as-is."
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Diagrams/IntervalSetDiagram.cs, tests/ChordFlow.Core.Tests/IntervalSetDiagramTests.cs]
    blocked_by: [1]
    satisfies: [IN3, IN8, C1, C5]
  - id: bridge-verb-scaleshandler
    order: 3
    status: done
    description: "Add a `scalePreview` inbound verb: extend `WebMessageRouter` (new `InboundEnvelope` fields `intervals`/`rootPitchClass`, a `ScalePreviewRequested` event, a `case \"scalePreview\"`). Add `Features/Scales/ScalesHandler.cs` (+ a small response envelope) that calls `IntervalSetDiagram.Build` and returns the `FretboardDiagram` JSON. Subscribe the handler to the router event in the host (`Program.cs`), mirroring how `entityPreview`/`ContentCrudHandler` are wired."
    files_touched: [src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Core/Features/Scales/ScalesHandler.cs, src/ChordFlow.Core/Features/Scales/ScalesEnvelopes.cs, src/ChordFlow.Desktop/Program.cs]
    blocked_by: [2]
    satisfies: [IN1, C2]
  - id: component-horizontal-orientation-fallback-color-control
    order: 4
    status: done
    description: "Extend `fretboard-render-component.js` with the three capabilities this page is first to need: (a) horizontal orientation (neck layout, frets left->right, many markers per string) implemented for real; (b) a palette fallback color so `palette: { \"1\":\"#e2574c\", \"*\":\"#000\" }` colors non-listed intervals with the default instead of the function color; (c) per-control visibility flags `controls: { orientation, fretWindow, label, legend }` (all true by default) plus `fretMin`/`fretMax` toolbar controls. Voicing fret-box stays byte-identical: default `controls` = all shown, no palette = function colors, vertical unchanged."
    files_touched: [src/ChordFlow.Desktop/wwwroot/fretboard-render-component.js]
    blocked_by: []
    satisfies: [IN4, IN5, IN6, C3, C4]
  - id: scales-page-view-nav-wiring
    order: 5
    status: done
    description: "Add the Scales view: a nav entry + a `scales.js` view module following the `content-crud.js` pattern (interval text box, root-note selector C..B), wire its script in `index.html` and view-switching in `app.js`, and add the `scalePreview` send + diagram-receive on `bridge.js`. Render the returned `FretboardDiagram` via `ChordFlowFretboard.create(el, { orientation:\"horizontal\", controls:{orientation:false}, palette:{ \"1\":\"#e2574c\", \"*\":\"#000\" } })`. Root-note + auto-window (no fret picker)."
    files_touched: [src/ChordFlow.Desktop/wwwroot/scales.js, src/ChordFlow.Desktop/wwwroot/index.html, src/ChordFlow.Desktop/wwwroot/app.js, src/ChordFlow.Desktop/wwwroot/bridge.js]
    blocked_by: [3, 4]
    satisfies: [IN1, IN7, IN8, C4]
  - id: reference-doc-sync
    order: 6
    status: done
    description: "Same unit of work: `chordflow-domain-model-reference.md` documents `IntervalSpeller.Parse` as the inverse vocabulary (label -> semitone, flats/sharps/naturals) and `IntervalSetDiagram` as a new `FretboardDiagram` producer; note the fretboard component's new capabilities (horizontal orientation, palette fallback color, per-control visibility flags) where the component is described (architecture ref)."
    files_touched: [loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-architecture-reference.md]
    blocked_by: [1, 4]
    satisfies: [IN9]
---
# Scales — interval-set fretboard page — Implementation

## Goal

Build the **Scales** page — the dogfood harness for `[[interval-lattice]]`: type an interval set (`1 b3 4 5 b7`), pick a root, and see those degrees lit up on a horizontal fretboard. Core-first: add the inverse vocabulary `IntervalSpeller.Parse` (label → semitone; flats/sharps/naturals) in `domain`, then a guitar-side **scale producer** that turns (interval set, root) into a `FretboardDiagram` via the shipped `IntervalLattice` (auto-fit window), then a thin bridge verb + handler to serve it. On the view side, extend the shipped `fretboard-render-component.js` with the three capabilities this page is the first to need — horizontal orientation, a palette fallback-color, and per-control visibility flags — keeping the voicing fret-box byte-identical (C3). Finally wire the page (interval text box + root-note selector, root-red/rest-black page palette, locked horizontal) and sync the domain-model ref. No persistence, no named-scale catalog, no root-fret picker (deferred). Each step cites the locked req `req.md` (rq_01KVGZRT9ZBR7X56M548CYKSKN).

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add `IntervalSpeller.Parse(string token) -> int semitone` in `domain` (next to `Name`): map an interval label to a semitone, accepting flats, sharps, and naturals (`1 2 3 4 5 6 7`, `b2 b3 b5 b6 b7`, `#4 #5 #9 #11`, compound `9 11 13` unfolded). Reject unparseable tokens with a clear exception. It is NOT a literal inverse of `Name`'s flats-only output — sharps and compound tensions must parse. Add a `ParseSet(string)` helper (space-separated tokens -> ordered distinct semitones) or leave set-splitting to the producer; cover both flats and sharps in tests. | src/ChordFlow.Core/Domain/IntervalSpeller.cs, tests/ChordFlow.Core.Tests/IntervalSpellerTests.cs | — | IN2, C2 |
| ✅ | 2 | Add `IntervalSetDiagram.Build(IReadOnlyList<string> tokens \| parsed semitones, PitchClass root, int maxFret = 15)` in `Instruments/Guitar/Diagrams/` (next to `FretboardDiagram`): for each parsed semitone, query `IntervalLattice.PositionsOfInterval(rootPos, semitone, 0, maxFret)` and emit one `FretboardMarker` per position (Note spelled via `NoteSpeller`, Interval label = the degree token, Function mapped from the semitone for a sensible default color, Shape = Circle). Leave `FretMin`/`FretMax` null so the view auto-fits to the markers (root-note + auto-window). No theory beyond parse + lattice query; reuse `IntervalLattice` as-is. | src/ChordFlow.Core/Instruments/Guitar/Diagrams/IntervalSetDiagram.cs, tests/ChordFlow.Core.Tests/IntervalSetDiagramTests.cs | 1 | IN3, IN8, C1, C5 |
| ✅ | 3 | Add a `scalePreview` inbound verb: extend `WebMessageRouter` (new `InboundEnvelope` fields `intervals`/`rootPitchClass`, a `ScalePreviewRequested` event, a `case "scalePreview"`). Add `Features/Scales/ScalesHandler.cs` (+ a small response envelope) that calls `IntervalSetDiagram.Build` and returns the `FretboardDiagram` JSON. Subscribe the handler to the router event in the host (`Program.cs`), mirroring how `entityPreview`/`ContentCrudHandler` are wired. | src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Core/Features/Scales/ScalesHandler.cs, src/ChordFlow.Core/Features/Scales/ScalesEnvelopes.cs, src/ChordFlow.Desktop/Program.cs | 2 | IN1, C2 |
| ✅ | 4 | Extend `fretboard-render-component.js` with the three capabilities this page is first to need: (a) horizontal orientation (neck layout, frets left->right, many markers per string) implemented for real; (b) a palette fallback color so `palette: { "1":"#e2574c", "*":"#000" }` colors non-listed intervals with the default instead of the function color; (c) per-control visibility flags `controls: { orientation, fretWindow, label, legend }` (all true by default) plus `fretMin`/`fretMax` toolbar controls. Voicing fret-box stays byte-identical: default `controls` = all shown, no palette = function colors, vertical unchanged. | src/ChordFlow.Desktop/wwwroot/fretboard-render-component.js | — | IN4, IN5, IN6, C3, C4 |
| ✅ | 5 | Add the Scales view: a nav entry + a `scales.js` view module following the `content-crud.js` pattern (interval text box, root-note selector C..B), wire its script in `index.html` and view-switching in `app.js`, and add the `scalePreview` send + diagram-receive on `bridge.js`. Render the returned `FretboardDiagram` via `ChordFlowFretboard.create(el, { orientation:"horizontal", controls:{orientation:false}, palette:{ "1":"#e2574c", "*":"#000" } })`. Root-note + auto-window (no fret picker). | src/ChordFlow.Desktop/wwwroot/scales.js, src/ChordFlow.Desktop/wwwroot/index.html, src/ChordFlow.Desktop/wwwroot/app.js, src/ChordFlow.Desktop/wwwroot/bridge.js | 3, 4 | IN1, IN7, IN8, C4 |
| ✅ | 6 | Same unit of work: `chordflow-domain-model-reference.md` documents `IntervalSpeller.Parse` as the inverse vocabulary (label -> semitone, flats/sharps/naturals) and `IntervalSetDiagram` as a new `FretboardDiagram` producer; note the fretboard component's new capabilities (horizontal orientation, palette fallback color, per-control visibility flags) where the component is described (architecture ref). | loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-architecture-reference.md | 1, 4 | IN9 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:intervalspeller-parse-inverse-vocabulary -->
### Step 1 — IntervalSpeller.Parse (inverse vocabulary)

The single new piece of theory. `Name` already owns label generation; this is its inverse. Build a small accidental+degree token grammar: optional accidental run (`b`/`#`), then a degree number, fold compound degrees (9->2,11->4,13->6) back with the octave, apply accidentals as ±1 semitone, return mod-12. Sharps map to the same pitch class as the enharmonic flat (`#4`==`b5`==6) — that's expected; the page keeps the user's typed token for display (step 5), not this canonical value.

<!-- step:scale-producer-intervalsetdiagram-build -->
### Step 2 — Scale producer — IntervalSetDiagram.Build

Pick a representative root `FretPosition` whose pitch class is `root` (e.g. lowest occurrence) to feed `PositionsOfInterval`, which is pitch-class based so the exact root position only sets the anchor pitch class. Tests: minor pentatonic on A and a `#4` scale on C land the expected (string,fret) set across the window; the root degree is labelled `1`.

<!-- step:bridge-verb-scaleshandler -->
### Step 3 — Bridge verb + ScalesHandler

Keep the envelope contract narrow — `{ type:"scalePreview", intervals:"1 b3 4 5 b7", rootPitchClass:9 }` in, a `FretboardDiagram`-shaped reply out (reuse the same camelCase marker shape the voicing preview already sends so the JS component needs no new model). A parse failure returns an error/empty diagram, not a host crash (mirror the router's tolerant dispatch).

<!-- step:component-horizontal-orientation-fallback-color-control -->
### Step 4 — Component: horizontal orientation + fallback color + control flags

Orientation-agnostic geometry already anticipated in v1; build the horizontal path alongside the vertical one. The `*` fallback key keeps the dumb-drawer contract — the page expresses 'root red, rest black' purely as data. Verify the existing Voicings preview after (C3).

<!-- step:scales-page-view-nav-wiring -->
### Step 5 — Scales page (view + nav + wiring)

The page owns the scale-specific chrome (text box, root selector) and the root-red/rest-black palette; the component owns orientation/label/legend/fret-window. Dogfood validation: type minor & major pentatonic and a `#4` scale, confirm every dot sits where the degree should around the chosen root, root highlighted, across the auto-fit window in horizontal layout.
