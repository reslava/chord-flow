---
type: plan
id: pl_01KVNJ1NVTPK2WZ1EMZQ869CYB
title: caged-chords Fixes Plan
status: done
created: 2026-06-21
updated: 2026-06-21
version: 1
design_version: 1
req_version: 2
tags: []
parent_id: de_01KVMZJWXJ47KGK6YJG2S6QHYR
requires_load: []
target_version: 0.1.0
steps:
  - id: sub-nut-auto-region-fix-fretboard
    order: 1
    status: done
    description: "Fix the sub-nut auto-region bug + fret-window/zone-band tweaks. (1) OctaveShape.AnchorsFor: anchor at the lowest occurrence whose whole octave skeleton fits on the neck (every anchor ≥ fret 0), skipping a too-low open-string primary to the next octave up — so C·maj7·A / G·maj7·E place at ≈9–12 instead of collapsing to fret 0 with spurious muted strings (+ regression test). (2) ChordShapeDiagram.Build: frame an explicit fret window over markers∪zone so the band is never clipped (+ test). (3) fretboard-render-component.js: computeWindow always grows the drawn window to contain the zone band plus a ZONE_MARGIN context margin (no model/override can clip it, all pages), and the toolbar min/max boxes pre-fill with the current effective window. (4) Synced chordflow-domain-model-reference (AnchorsFor semantics)."
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Geometry/OctaveShape.cs, tests/ChordFlow.Core.Tests/OctaveShapeTests.cs, src/ChordFlow.Core/Instruments/Guitar/Diagrams/ChordShapeDiagram.cs, tests/ChordFlow.Core.Tests/ChordShapeDiagramTests.cs, src/ChordFlow.Desktop/wwwroot/fretboard-render-component.js, loom/refs/chordflow-domain-model-reference.md]
    blocked_by: []
    satisfies: [IN7]
---
# caged-chords Fixes Plan

## Goal

Visual-check follow-up for the CAGED Chords dogfood page (caged-chords-chat-002): fix the engine + diagram defects the running app surfaced, and the requested fretboard-component tweaks. Satisfies the amended req IN7 (EX4 relaxed). One unit of work, already implemented and verified (full Core suite 590/590, derivation oracle 36/36, Desktop builds clean).

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Fix the sub-nut auto-region bug + fret-window/zone-band tweaks. (1) OctaveShape.AnchorsFor: anchor at the lowest occurrence whose whole octave skeleton fits on the neck (every anchor ≥ fret 0), skipping a too-low open-string primary to the next octave up — so C·maj7·A / G·maj7·E place at ≈9–12 instead of collapsing to fret 0 with spurious muted strings (+ regression test). (2) ChordShapeDiagram.Build: frame an explicit fret window over markers∪zone so the band is never clipped (+ test). (3) fretboard-render-component.js: computeWindow always grows the drawn window to contain the zone band plus a ZONE_MARGIN context margin (no model/override can clip it, all pages), and the toolbar min/max boxes pre-fill with the current effective window. (4) Synced chordflow-domain-model-reference (AnchorsFor semantics). | src/ChordFlow.Core/Instruments/Guitar/Geometry/OctaveShape.cs, tests/ChordFlow.Core.Tests/OctaveShapeTests.cs, src/ChordFlow.Core/Instruments/Guitar/Diagrams/ChordShapeDiagram.cs, tests/ChordFlow.Core.Tests/ChordShapeDiagramTests.cs, src/ChordFlow.Desktop/wwwroot/fretboard-render-component.js, loom/refs/chordflow-domain-model-reference.md | — | IN7 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
