---
type: plan
id: pl_01KY0T38YXA9XPVE84GT6XHVVQ
title: Phase 2 — Rhythm Generator dogfood page
status: done
created: 2026-07-20
updated: 2026-07-21
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KY0RDXS9C7X93BX8Y1HVCMC3
requires_load: []
target_version: 0.1.0
steps:
  - id: rhythmgenerate-bridge-dtos
    order: 1
    status: done
    description: "Bridge contract for the verb: an inbound `rhythmGenerate` envelope carrying the wire request ({strategy discriminator, per-strategy params as token+args, seed, drum voice}) and the outbound `rhythmGenerated` ({diagram: DrumGrooveDiagram, tex, dsl?}) / `rhythmGenerateError` ({message}) replies. Mirror the drumPreview envelope family."
    files_touched: [src/ChordFlow.Core/Bridge/RhythmGenerateEnvelope.cs, src/ChordFlow.Core/Features/Rhythm/RhythmGeneratedEnvelope.cs]
    blocked_by: []
    satisfies: [IN7]
  - id: request-generationparams-resolver
    order: 2
    status: done
    description: "Wire→Core params resolver: map the JSON request (strategy + operator/behaviour/family/palette tokens + args) onto the Core GenerationParams / PatternParams / RandomParams discriminated unions. One place that knows the token vocabulary; unknown token → a clean parse error surfaced as rhythmGenerateError."
    files_touched: [src/ChordFlow.Core/Features/Rhythm/RhythmGenerationRequest.cs]
    blocked_by: [rhythmgenerate-bridge-dtos]
    satisfies: [IN2, IN7]
  - id: rhythmgeneratehandler
    order: 3
    status: done
    description: "RhythmGenerateHandler (Features): resolve request → RhythmGenerator.Generate → OnsetGridToDrumGroove.Project(voice) → DrumGrooveRenderer (percussion tex) + DrumGrooveDiagram.Build (the DrumsR model) in one pass (projections that can't drift). Bad input → rhythmGenerateError. Reuses existing renderers (no new alphaTex code, C3)."
    files_touched: [src/ChordFlow.Core/Features/Rhythm/RhythmGenerateHandler.cs]
    blocked_by: [request-generationparams-resolver]
    satisfies: [IN5, IN7, C3]
  - id: handler-unit-test
    order: 4
    status: done
    description: "Handler unit test: a valid Pattern request and a valid Random request each return a non-empty tex + a DrumGrooveDiagram whose hit ticks match the generated grid's onsets; an invalid request (bad token / out-of-range count) returns rhythmGenerateError, not a throw."
    files_touched: [tests/ChordFlow.Core.Tests/Rhythm/Generation/RhythmGenerateHandlerTests.cs]
    blocked_by: [rhythmgeneratehandler]
    satisfies: [IN7]
  - id: webmessagerouter-host-wiring
    order: 5
    status: done
    description: "Router wiring: WebMessageRouter parses the inbound rhythmGenerate envelope and raises a typed event; the host wires it to RhythmGenerateHandler and posts the reply. Follows the existing drumPreview inbound path."
    files_touched: [src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Desktop/Program.cs]
    blocked_by: [rhythmgenerate-bridge-dtos, rhythmgeneratehandler]
    satisfies: [IN7]
  - id: rhythm-generator-page-controls-drumsr-play
    order: 6
    status: done
    description: "Rhythm Generator JS page: strategy selector; Pattern controls (family / operator / behaviour / barCount) and Random controls (value palette / contentBars / silenceBars); seed field + reroll. On change, issue rhythmGenerate; render the returned DrumGrooveDiagram on a reused DrumsR (drums-render-component.js) and play the tex through the shared playback engine. Show the raw dsl for debug."
    files_touched: [src/ChordFlow.Desktop/wwwroot/rhythm-generator.js]
    blocked_by: [webmessagerouter-host-wiring]
    satisfies: [IN7]
  - id: count-emphasis-overlay-on-drumsr
    order: 7
    status: done
    description: "Count + emphasis overlay on DrumsR: print 1 e & a beat-position labels under the grid (from subdivision/beatsPerBar) and highlight the downbeats / trained beat. Display-only — no change to the rhythm model or DSL (C5). Add as an opt-in flag so the Drums page is unaffected."
    files_touched: [src/ChordFlow.Desktop/wwwroot/drums-render-component.js]
    blocked_by: [rhythm-generator-page-controls-drumsr-play]
    satisfies: [IN7, C5]
  - id: nav-entry-lazy-mount-bridge-fan
    order: 8
    status: done
    description: "Nav entry + lazy mount: add the Rhythm Generator top-level view to index.html (the views/onShow pattern used by Scales/CAGED), lazily create the page on first tab show, and register its envelope types in bridge.js fan-out."
    files_touched: [src/ChordFlow.Desktop/wwwroot/index.html, src/ChordFlow.Desktop/wwwroot/bridge.js]
    blocked_by: [rhythm-generator-page-controls-drumsr-play]
    satisfies: [IN7]
  - id: update-architecture-reference
    order: 9
    status: done
    description: Update the architecture reference with the new rhythmGenerate/rhythmGenerated verb (its wire shape + one-pass handler) and the Rhythm Generator page + DrumsR count-overlay reuse (CLAUDE-LOCAL architecture ref-sync, same unit of work).
    files_touched: [loom/refs/chordflow-architecture-reference.md]
    blocked_by: [count-emphasis-overlay-on-drumsr, nav-entry-lazy-mount-bridge-fan]
    satisfies: []
---
# Phase 2 — Rhythm Generator dogfood page

## Goal

Make the Phase 1 generation engine visible and audible: a `rhythmGenerate` bridge verb + a Features handler that generates an OnsetGrid, projects it to a single-voice DrumGroove, and returns the percussion tex + the DrumGrooveDiagram; and a new "Rhythm Generator" nav page that drives it with strategy/param controls and renders the result on the reused DrumsR (with a 1 e &amp; a count/emphasis overlay) plus playback. This satisfies the dogfood rule and de-risks the whole onset→projection model on-screen before Practice integration. Named-trainer presets and the reference pulse are deliberately held for Phase 3; Phase 2 exposes the raw params. Closes with the architecture-reference sync.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Bridge contract for the verb: an inbound `rhythmGenerate` envelope carrying the wire request ({strategy discriminator, per-strategy params as token+args, seed, drum voice}) and the outbound `rhythmGenerated` ({diagram: DrumGrooveDiagram, tex, dsl?}) / `rhythmGenerateError` ({message}) replies. Mirror the drumPreview envelope family. | src/ChordFlow.Core/Bridge/RhythmGenerateEnvelope.cs, src/ChordFlow.Core/Features/Rhythm/RhythmGeneratedEnvelope.cs | — | IN7 |
| ✅ | 2 | Wire→Core params resolver: map the JSON request (strategy + operator/behaviour/family/palette tokens + args) onto the Core GenerationParams / PatternParams / RandomParams discriminated unions. One place that knows the token vocabulary; unknown token → a clean parse error surfaced as rhythmGenerateError. | src/ChordFlow.Core/Features/Rhythm/RhythmGenerationRequest.cs | rhythmgenerate-bridge-dtos | IN2, IN7 |
| ✅ | 3 | RhythmGenerateHandler (Features): resolve request → RhythmGenerator.Generate → OnsetGridToDrumGroove.Project(voice) → DrumGrooveRenderer (percussion tex) + DrumGrooveDiagram.Build (the DrumsR model) in one pass (projections that can't drift). Bad input → rhythmGenerateError. Reuses existing renderers (no new alphaTex code, C3). | src/ChordFlow.Core/Features/Rhythm/RhythmGenerateHandler.cs | request-generationparams-resolver | IN5, IN7, C3 |
| ✅ | 4 | Handler unit test: a valid Pattern request and a valid Random request each return a non-empty tex + a DrumGrooveDiagram whose hit ticks match the generated grid's onsets; an invalid request (bad token / out-of-range count) returns rhythmGenerateError, not a throw. | tests/ChordFlow.Core.Tests/Rhythm/Generation/RhythmGenerateHandlerTests.cs | rhythmgeneratehandler | IN7 |
| ✅ | 5 | Router wiring: WebMessageRouter parses the inbound rhythmGenerate envelope and raises a typed event; the host wires it to RhythmGenerateHandler and posts the reply. Follows the existing drumPreview inbound path. | src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Desktop/Program.cs | rhythmgenerate-bridge-dtos, rhythmgeneratehandler | IN7 |
| ✅ | 6 | Rhythm Generator JS page: strategy selector; Pattern controls (family / operator / behaviour / barCount) and Random controls (value palette / contentBars / silenceBars); seed field + reroll. On change, issue rhythmGenerate; render the returned DrumGrooveDiagram on a reused DrumsR (drums-render-component.js) and play the tex through the shared playback engine. Show the raw dsl for debug. | src/ChordFlow.Desktop/wwwroot/rhythm-generator.js | webmessagerouter-host-wiring | IN7 |
| ✅ | 7 | Count + emphasis overlay on DrumsR: print 1 e & a beat-position labels under the grid (from subdivision/beatsPerBar) and highlight the downbeats / trained beat. Display-only — no change to the rhythm model or DSL (C5). Add as an opt-in flag so the Drums page is unaffected. | src/ChordFlow.Desktop/wwwroot/drums-render-component.js | rhythm-generator-page-controls-drumsr-play | IN7, C5 |
| ✅ | 8 | Nav entry + lazy mount: add the Rhythm Generator top-level view to index.html (the views/onShow pattern used by Scales/CAGED), lazily create the page on first tab show, and register its envelope types in bridge.js fan-out. | src/ChordFlow.Desktop/wwwroot/index.html, src/ChordFlow.Desktop/wwwroot/bridge.js | rhythm-generator-page-controls-drumsr-play | IN7 |
| ✅ | 9 | Update the architecture reference with the new rhythmGenerate/rhythmGenerated verb (its wire shape + one-pass handler) and the Rhythm Generator page + DrumsR count-overlay reuse (CLAUDE-LOCAL architecture ref-sync, same unit of work). | loom/refs/chordflow-architecture-reference.md | count-emphasis-overlay-on-drumsr, nav-entry-lazy-mount-bridge-fan | — |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
