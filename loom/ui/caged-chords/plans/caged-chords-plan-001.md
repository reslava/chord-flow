---
type: plan
id: pl_01KVMZNE5YMVK02RTSSQKNTTAJ
title: caged-chords Plan
status: done
created: 2026-06-21
updated: 2026-06-21
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KVMZJWXJ47KGK6YJG2S6QHYR
requires_load: []
target_version: 0.1.0
steps:
  - id: chordshapediagram-producer-test
    order: 1
    status: done
    description: ChordShapeDiagram.Build(ChordShape, root) → FretboardDiagram producer (+ ChordShapeDiagramTests). One Circle marker per sounded string (spelled note via NoteSpeller, role-aware interval via IntervalSpeller.Label, function colour-key by tertian position in QualityIntervals else tension), muted strings as chrome, ChordShape.Zone as the band, title = chordSymbol · shape · anchorFinger, FretMin = lowest fretted fret. Mirrors VoicingDiagram; reuses the FretboardDiagram carrier unchanged.
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Diagrams/ChordShapeDiagram.cs, tests/ChordFlow.Core.Tests/ChordShapeDiagramTests.cs]
    blocked_by: []
    satisfies: [IN2, IN6, C1, C3]
  - id: cagedchordhandler-envelopes
    order: 2
    status: done
    description: CagedChordHandler.Preview(quality, shape, rootPitchClass) + outbound envelopes (CagedChordDiagramEnvelope type cagedChordDiagram, CagedChordErrorEnvelope type cagedChordError). Parse the Quality + CagedShape names (unknown → FormatException), derive at the auto-region (minFret 0 .. full-neck maxFret, so the engine anchors the lowest placement), build the diagram via ChordShapeDiagram; a Derive throw (unvoiceable combo) maps to the error envelope. All 8×5 combos derivable — no pre-greying.
    files_touched: [src/ChordFlow.Core/Features/Caged/CagedChordHandler.cs, src/ChordFlow.Core/Features/Caged/CagedEnvelopes.cs]
    blocked_by: [1]
    satisfies: [IN3, IN4, C2, C4]
  - id: cagedchordpreview-bridge-verb
    order: 3
    status: done
    description: "Add the cagedChordPreview verb to WebMessageRouter: a CagedChordPreviewRequested event (shape, quality, rootPitchClass), the dispatch case, and the inbound Quality field on InboundEnvelope (reusing Shape + RootPitchClass). Update the router's inbound-vocabulary doc comment."
    files_touched: [src/ChordFlow.Core/Bridge/WebMessageRouter.cs]
    blocked_by: []
    satisfies: [IN3, IN5]
  - id: host-wiring
    order: 4
    status: done
    description: "Wire the host in Program.cs: construct CagedChordHandler, subscribe router.CagedChordPreviewRequested → bridge.Send(handler.Preview(...)), catching FormatException → CagedChordErrorEnvelope (mirrors the cagedPreview hookup)."
    files_touched: [src/ChordFlow.Desktop/Program.cs]
    blocked_by: [2, 3]
    satisfies: [IN5, C2]
  - id: caged-chords-page-nav
    order: 5
    status: done
    description: "The page: caged-chords.js (shape + quality + root selectors → send cagedChordPreview; render cagedChordDiagram on a lazily-created ChordFlowFretboard, horizontal neck, chord-tone palette; cagedChordError inline; default maj7·E·A), a nav button (navCagedChords) + view (caged-chords-view) in index.html, and registration in app.js's view map. No theory in JS (dumb drawer)."
    files_touched: [src/ChordFlow.Desktop/wwwroot/caged-chords.js, src/ChordFlow.Desktop/wwwroot/index.html, src/ChordFlow.Desktop/wwwroot/app.js]
    blocked_by: [3]
    satisfies: [IN1, IN4, IN5, C1]
---
# caged-chords Plan

## Goal

Ship the CAGED Chords dogfood page — a read-only fretboard view that renders a derived CAGED grip (frets + zone band + anchor finger in the title) for a chosen shape × quality × root, as a generator over the done caged-system engine (all 8×5 combos, not just the authored 36). A thin vertical slice mirroring the caged-shapes page: a new ChordShapeDiagram producer of the existing FretboardDiagram carrier, a CagedChordHandler behind a cagedChordPreview bridge verb with auto-region placement, the host wiring, and the JS page on the unchanged ChordFlowFretboard view. No engine changes. Built bottom-up: producer (+test) → handler/envelopes → bridge verb → host wiring → page. Steps cite the locked req (IN1–IN6 / C1–C4, EX1–EX5).

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | ChordShapeDiagram.Build(ChordShape, root) → FretboardDiagram producer (+ ChordShapeDiagramTests). One Circle marker per sounded string (spelled note via NoteSpeller, role-aware interval via IntervalSpeller.Label, function colour-key by tertian position in QualityIntervals else tension), muted strings as chrome, ChordShape.Zone as the band, title = chordSymbol · shape · anchorFinger, FretMin = lowest fretted fret. Mirrors VoicingDiagram; reuses the FretboardDiagram carrier unchanged. | src/ChordFlow.Core/Instruments/Guitar/Diagrams/ChordShapeDiagram.cs, tests/ChordFlow.Core.Tests/ChordShapeDiagramTests.cs | — | IN2, IN6, C1, C3 |
| ✅ | 2 | CagedChordHandler.Preview(quality, shape, rootPitchClass) + outbound envelopes (CagedChordDiagramEnvelope type cagedChordDiagram, CagedChordErrorEnvelope type cagedChordError). Parse the Quality + CagedShape names (unknown → FormatException), derive at the auto-region (minFret 0 .. full-neck maxFret, so the engine anchors the lowest placement), build the diagram via ChordShapeDiagram; a Derive throw (unvoiceable combo) maps to the error envelope. All 8×5 combos derivable — no pre-greying. | src/ChordFlow.Core/Features/Caged/CagedChordHandler.cs, src/ChordFlow.Core/Features/Caged/CagedEnvelopes.cs | 1 | IN3, IN4, C2, C4 |
| ✅ | 3 | Add the cagedChordPreview verb to WebMessageRouter: a CagedChordPreviewRequested event (shape, quality, rootPitchClass), the dispatch case, and the inbound Quality field on InboundEnvelope (reusing Shape + RootPitchClass). Update the router's inbound-vocabulary doc comment. | src/ChordFlow.Core/Bridge/WebMessageRouter.cs | — | IN3, IN5 |
| ✅ | 4 | Wire the host in Program.cs: construct CagedChordHandler, subscribe router.CagedChordPreviewRequested → bridge.Send(handler.Preview(...)), catching FormatException → CagedChordErrorEnvelope (mirrors the cagedPreview hookup). | src/ChordFlow.Desktop/Program.cs | 2, 3 | IN5, C2 |
| ✅ | 5 | The page: caged-chords.js (shape + quality + root selectors → send cagedChordPreview; render cagedChordDiagram on a lazily-created ChordFlowFretboard, horizontal neck, chord-tone palette; cagedChordError inline; default maj7·E·A), a nav button (navCagedChords) + view (caged-chords-view) in index.html, and registration in app.js's view map. No theory in JS (dumb drawer). | src/ChordFlow.Desktop/wwwroot/caged-chords.js, src/ChordFlow.Desktop/wwwroot/index.html, src/ChordFlow.Desktop/wwwroot/app.js | 3 | IN1, IN4, IN5, C1 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
