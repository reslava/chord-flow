---
type: plan
id: pl_01KVBTK7DEVJYNZEQDSRXNVQNH
title: Fretboard Render Component — Implementation
status: done
created: 2026-06-17
updated: 2026-06-18
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KVBSHF54Q2AMJESSQAKVV97W
requires_load: []
steps:
  - id: core-marker-model-voicing-producer-recast
    order: 1
    status: done
    description: Add the general spatial carrier in Core (`FretboardDiagram`, `FretboardMarker`, `MarkerShape`) and recast `VoicingDiagram.Build` to emit it; remove `DiagramModel`/`DiagramString`. Update the `entityPreview` envelope + `ContentCrudHandler` and `VoicingDiagramTests` to the new type. Markers support many-per-string; muted strings become diagram-level chrome, open = a fret-0 marker.
    files_touched: [src/ChordFlow.Core/Domain/Diagrams/FretboardDiagram.cs, src/ChordFlow.Core/Domain/Voicings/VoicingDiagram.cs, src/ChordFlow.Core/Domain/Voicings/DiagramModel.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudEnvelopes.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs, tests/ChordFlow.Core.Tests/VoicingDiagramTests.cs]
    blocked_by: []
    satisfies: [IN2, IN6, IN7, C1, C2, C4]
  - id: chordflowfretboard-component
    order: 2
    status: done
    description: "Build `wwwroot/fretboard-render-component.js` (`window.ChordFlowFretboard`): `create(container, opts)` → handle with `render(model)`, `setLabelMode`, `dispose`. Draws the marker list as an SVG fret-box (vertical), color from the default 5-color function palette (override via `opts.palette`), shape from `MarkerShape`, open/muted indicators, barre, auto-fit fret window (or `fretMin/fretMax`), title, owned label toggle + auto-built legend."
    files_touched: [src/ChordFlow.Desktop/wwwroot/fretboard-render-component.js]
    blocked_by: [1]
    satisfies: [IN1, IN2, IN3, IN4, IN5, C1, C3]
  - id: sandbox-test-feeder
    order: 3
    status: done
    description: "Add a hand-fed sandbox so arbitrary marker sets render immediately (the harness, usable before any derivation-engine domain type exists): a small standalone page that imports the component and draws a few fixtures (a voicing, a scratch interval/scale layout) demonstrating color/shape/label/legend."
    files_touched: [src/ChordFlow.Desktop/wwwroot/fretboard-sandbox.html]
    blocked_by: [2]
    satisfies: [IN8]
  - id: retrofit-voicings-preview
    order: 4
    status: done
    description: "Retrofit the Content/Voicings preview onto the component: `content-crud.js` `diagram` branch uses `window.ChordFlowFretboard` instead of `window.ChordFlowDiagram`; swap the `index.html` script tag (`chord-diagram.js` → `fretboard-render-component.js`); delete `chord-diagram.js`. The voicing fret-box is now the first consumer."
    files_touched: [src/ChordFlow.Desktop/wwwroot/content-crud.js, src/ChordFlow.Desktop/wwwroot/index.html, src/ChordFlow.Desktop/wwwroot/chord-diagram.js]
    blocked_by: [1, 2]
    satisfies: [IN7, C5]
  - id: reference-doc-sync
    order: 5
    status: done
    description: "Sync the reference docs in the same unit of work: architecture ref gains the SVG fretboard render component as a JS view layer alongside `score-render-component`; domain-model ref documents `FretboardDiagram`/`FretboardMarker` as the general spatial carrier with `VoicingDiagram.Build` recast as one producer (and `DiagramModel` removed)."
    files_touched: [loom/refs/chordflow-architecture-reference.md, loom/refs/chordflow-domain-model-reference.md]
    blocked_by: [1, 2, 4]
    satisfies: [IN9]
---
# Fretboard Render Component — Implementation

## Goal

Build the reusable SVG fretboard view (`window.ChordFlowFretboard`, the spatial twin of `ChordFlowScore`) and the Core marker-model it draws, then make the existing voicing fret-box its first consumer. Core-model-first: define the general `FretboardDiagram`/`FretboardMarker` carrier and recast `VoicingDiagram.Build` onto it (removing the voicing-specific `DiagramModel`/`DiagramString` — no parallel path), then build the JS component (color = interval via the default 5-color function palette, shape = layer, legend + label toggle, vertical orientation, fret-window auto-fit), prove it with a hand-fed sandbox feeder, retrofit the Content/Voicings preview onto it, and sync the reference docs. Theory stays in Core; the JS is a dumb drawer. Ships standalone — no dependency on the derivation-engine threads; their producers (interval lattice, scales, arpeggios) and a richer per-interval palette, horizontal orientation, and click-to-author all attach additively later.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add the general spatial carrier in Core (`FretboardDiagram`, `FretboardMarker`, `MarkerShape`) and recast `VoicingDiagram.Build` to emit it; remove `DiagramModel`/`DiagramString`. Update the `entityPreview` envelope + `ContentCrudHandler` and `VoicingDiagramTests` to the new type. Markers support many-per-string; muted strings become diagram-level chrome, open = a fret-0 marker. | src/ChordFlow.Core/Domain/Diagrams/FretboardDiagram.cs, src/ChordFlow.Core/Domain/Voicings/VoicingDiagram.cs, src/ChordFlow.Core/Domain/Voicings/DiagramModel.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudEnvelopes.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs, tests/ChordFlow.Core.Tests/VoicingDiagramTests.cs | — | IN2, IN6, IN7, C1, C2, C4 |
| ✅ | 2 | Build `wwwroot/fretboard-render-component.js` (`window.ChordFlowFretboard`): `create(container, opts)` → handle with `render(model)`, `setLabelMode`, `dispose`. Draws the marker list as an SVG fret-box (vertical), color from the default 5-color function palette (override via `opts.palette`), shape from `MarkerShape`, open/muted indicators, barre, auto-fit fret window (or `fretMin/fretMax`), title, owned label toggle + auto-built legend. | src/ChordFlow.Desktop/wwwroot/fretboard-render-component.js | 1 | IN1, IN2, IN3, IN4, IN5, C1, C3 |
| ✅ | 3 | Add a hand-fed sandbox so arbitrary marker sets render immediately (the harness, usable before any derivation-engine domain type exists): a small standalone page that imports the component and draws a few fixtures (a voicing, a scratch interval/scale layout) demonstrating color/shape/label/legend. | src/ChordFlow.Desktop/wwwroot/fretboard-sandbox.html | 2 | IN8 |
| ✅ | 4 | Retrofit the Content/Voicings preview onto the component: `content-crud.js` `diagram` branch uses `window.ChordFlowFretboard` instead of `window.ChordFlowDiagram`; swap the `index.html` script tag (`chord-diagram.js` → `fretboard-render-component.js`); delete `chord-diagram.js`. The voicing fret-box is now the first consumer. | src/ChordFlow.Desktop/wwwroot/content-crud.js, src/ChordFlow.Desktop/wwwroot/index.html, src/ChordFlow.Desktop/wwwroot/chord-diagram.js | 1, 2 | IN7, C5 |
| ✅ | 5 | Sync the reference docs in the same unit of work: architecture ref gains the SVG fretboard render component as a JS view layer alongside `score-render-component`; domain-model ref documents `FretboardDiagram`/`FretboardMarker` as the general spatial carrier with `VoicingDiagram.Build` recast as one producer (and `DiagramModel` removed). | loom/refs/chordflow-architecture-reference.md, loom/refs/chordflow-domain-model-reference.md | 1, 2, 4 | IN9 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:core-marker-model-voicing-producer-recast -->
### Step 1 — Core marker model + voicing producer recast

New `Domain/Diagrams/FretboardDiagram.cs`: `MarkerShape` enum (Circle/Square/Diamond/Ring), `FretboardMarker(int String, int Fret, string Note, string Interval, ChordToneFunction Function, MarkerShape Shape)`, `FretboardDiagram(string Title, IReadOnlyList<FretboardMarker> Markers, IReadOnlyList<int> MutedStrings, int? BarreFret, int? FretMin, int? FretMax)`. `VoicingDiagram.Build` keeps its existing theory (pitch class → interval/function/label via `Fretboard`/`QualityIntervals`/`NoteSpeller`) but emits one `Circle` marker per sounding string (fret 0 ⇒ open marker), pushes muted strings into `MutedStrings`, and carries the barre + first fret as `FretMin`. Delete `DiagramModel`/`DiagramString`. Retitle the diagram from the chord symbol. Update `EntityPreviewEnvelope.Diagram` to `FretboardDiagram?` and the tests' assertions to the marker shape.

<!-- step:chordflowfretboard-component -->
### Step 2 — ChordFlowFretboard component

Start from `chord-diagram.js` geometry for the vertical orientation. Render arbitrary markers (many per string), not per-string slots. Color = `palette[function]` (default root/third/fifth/seventh/tension colors); `MarkerShape` → circle/square/diamond/ring. Open string = ringed dot above the nut; muted strings (from `MutedStrings`) = `✕`. Toolbar owns the interval/note label toggle (re-renders); legend auto-builds from the functions present. `orientation` option is accepted; v1 implements `vertical` only (horizontal deferred — design §8.3). No music theory in JS (C1).

<!-- step:sandbox-test-feeder -->
### Step 3 — Sandbox test feeder

A throwaway-friendly dev page (not wired into the app nav) that constructs `FretboardDiagram`-shaped JSON by hand and calls `ChordFlowFretboard.create(...).render(model)`. Include at least one many-per-string fixture so the generalization is visibly exercised before real producers exist.

<!-- step:retrofit-voicings-preview -->
### Step 4 — Retrofit Voicings preview

`renderPreview`'s `diagram` branch: `ChordFlowFretboard.create(diagramEl, {...}).render(msg.diagram)` (or a cached handle), replacing `ChordFlowDiagram.render`. Remove the `chord-diagram.js` `<script>` and add `fretboard-render-component.js` in `index.html`. Delete the old `chord-diagram.js`. Verify the Voicings preview still renders (C5) — label toggle, legend, open/muted/barre.
