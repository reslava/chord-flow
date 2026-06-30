---
type: plan
id: pl_01KWAR95YC7EK8AZT87BF9DX4D
title: GuitarVoicingsR — faceted voicings grid
status: done
created: 2026-06-29
updated: 2026-06-30
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KWAQ35KX1PNST681WY0RVQDS
requires_load: []
target_version: 0.1.0
steps:
  - id: qualityfacets-3rd-5th-7th-derived-from
    order: 1
    status: done
    description: Add a pure `QualityFacets` to Music.Harmony that derives a quality's (3rd, 5th, 7th-color) facets from its chord-tone spelling; unit-test all 10 qualities against the agreed table.
    files_touched: [src/ChordFlow.Core/Music/Harmony/QualityFacets.cs, tests/ChordFlow.Core.Tests/QualityFacetsTests.cs]
    blocked_by: []
    satisfies: [IN4, C2]
  - id: voicinggrid-bridge-verb-filter-realize-handler
    order: 2
    status: done
    description: Add the `voicingGrid` inbound + `voicingGridResult` outbound envelopes and a Features/Voicings handler that filters CagedVoicingCatalog (+ package/user sources) by the facet/family/source sets via QualityFacets, realizes each surviving combo at the root through RealizedVoicingDiagram, and returns ordered cells; wire it into WebMessageRouter.
    files_touched: [src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Core/Features/Voicings/VoicingGridHandler.cs, src/ChordFlow.Core/Bridge/, tests/ChordFlow.Core.Tests/VoicingGridHandlerTests.cs]
    blocked_by: [qualityfacets-3rd-5th-7th-derived-from]
    satisfies: [IN2, IN3, IN5, C3, C4, C5, C6]
  - id: fretr-additions-title-id-copy-in
    order: 3
    status: done
    description: Extend fretboard-render-component.js with a `title`, an `id` shown with a copy-to-clipboard control, and confirm the orientation/label controls honor the per-control visibility flag when the cell is hosted inside a grid.
    files_touched: [src/ChordFlow.Desktop/wwwroot/fretboard-render-component.js, src/ChordFlow.Desktop/wwwroot/fretboard-sandbox.html]
    blocked_by: []
    satisfies: [IN7, C1]
  - id: guitarvoicingsr-component-filter-stack-grid-fan
    order: 4
    status: done
    description: "New guitar-voicings-render-component.js (window.ChordFlowGuitarVoicings): the faceted toggle-button filter stack (Root selector + multi-select Source/Family/3rd/5th/7th), the rows-by-quality × cols-by-shape grid of FretR cells, and shared orientation + intervals/notes controls fanned out to every cell; issues `voicingGrid` and renders `voicingGridResult`."
    files_touched: [src/ChordFlow.Desktop/wwwroot/guitar-voicings-render-component.js, src/ChordFlow.Desktop/wwwroot/bridge.js]
    blocked_by: [voicinggrid-bridge-verb-filter-realize-handler, fretr-additions-title-id-copy-in]
    satisfies: [IN1, IN3, IN5, IN6, C1, C3]
  - id: hosting-page-the-dogfood-surface
    order: 5
    status: done
    description: Add a Voicings view/page to wwwroot that mounts GuitarVoicingsR, with a nav entry, and fan the inbound bridge messages out to it.
    files_touched: [src/ChordFlow.Desktop/wwwroot/index.html, src/ChordFlow.Desktop/wwwroot/bridge.js]
    blocked_by: [guitarvoicingsr-component-filter-stack-grid-fan]
    satisfies: [IN8]
  - id: oracle-cross-check-dogfood-pass
    order: 6
    status: done
    description: Add a Core test asserting that voicingGrid cells for a known filter (e.g. all dom7 shells across roots) match the authored oracle grips, and confirm the page renders the grid correctly against the rules-reference.
    files_touched: [tests/ChordFlow.Core.Tests/VoicingGridOracleTests.cs]
    blocked_by: [voicinggrid-bridge-verb-filter-realize-handler, hosting-page-the-dogfood-surface]
    satisfies: [IN9]
---
# GuitarVoicingsR — faceted voicings grid

## Goal

Implement GuitarVoicingsR — a faceted grid of FretR chord-boxes that renders many realized voicings at once — per the locked req. Core-first and reuse-don't-fork: a spelling-derived QualityFacets in Music.Harmony, a one-round-trip `voicingGrid` bridge verb + handler that filters the existing CagedVoicingCatalog and realizes cells via RealizedVoicingDiagram, then the JS component (filter stack + grid + fanned-out controls), the FretR additions, a hosting page, and an oracle cross-check. The page is the engine's visual oracle and the dogfood surface; no new voicing derivation is introduced.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add a pure `QualityFacets` to Music.Harmony that derives a quality's (3rd, 5th, 7th-color) facets from its chord-tone spelling; unit-test all 10 qualities against the agreed table. | src/ChordFlow.Core/Music/Harmony/QualityFacets.cs, tests/ChordFlow.Core.Tests/QualityFacetsTests.cs | — | IN4, C2 |
| ✅ | 2 | Add the `voicingGrid` inbound + `voicingGridResult` outbound envelopes and a Features/Voicings handler that filters CagedVoicingCatalog (+ package/user sources) by the facet/family/source sets via QualityFacets, realizes each surviving combo at the root through RealizedVoicingDiagram, and returns ordered cells; wire it into WebMessageRouter. | src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Core/Features/Voicings/VoicingGridHandler.cs, src/ChordFlow.Core/Bridge/, tests/ChordFlow.Core.Tests/VoicingGridHandlerTests.cs | qualityfacets-3rd-5th-7th-derived-from | IN2, IN3, IN5, C3, C4, C5, C6 |
| ✅ | 3 | Extend fretboard-render-component.js with a `title`, an `id` shown with a copy-to-clipboard control, and confirm the orientation/label controls honor the per-control visibility flag when the cell is hosted inside a grid. | src/ChordFlow.Desktop/wwwroot/fretboard-render-component.js, src/ChordFlow.Desktop/wwwroot/fretboard-sandbox.html | — | IN7, C1 |
| ✅ | 4 | New guitar-voicings-render-component.js (window.ChordFlowGuitarVoicings): the faceted toggle-button filter stack (Root selector + multi-select Source/Family/3rd/5th/7th), the rows-by-quality × cols-by-shape grid of FretR cells, and shared orientation + intervals/notes controls fanned out to every cell; issues `voicingGrid` and renders `voicingGridResult`. | src/ChordFlow.Desktop/wwwroot/guitar-voicings-render-component.js, src/ChordFlow.Desktop/wwwroot/bridge.js | voicinggrid-bridge-verb-filter-realize-handler, fretr-additions-title-id-copy-in | IN1, IN3, IN5, IN6, C1, C3 |
| ✅ | 5 | Add a Voicings view/page to wwwroot that mounts GuitarVoicingsR, with a nav entry, and fan the inbound bridge messages out to it. | src/ChordFlow.Desktop/wwwroot/index.html, src/ChordFlow.Desktop/wwwroot/bridge.js | guitarvoicingsr-component-filter-stack-grid-fan | IN8 |
| ✅ | 6 | Add a Core test asserting that voicingGrid cells for a known filter (e.g. all dom7 shells across roots) match the authored oracle grips, and confirm the page renders the grid correctly against the rules-reference. | tests/ChordFlow.Core.Tests/VoicingGridOracleTests.cs | voicinggrid-bridge-verb-filter-realize-handler, hosting-page-the-dogfood-surface | IN9 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:qualityfacets-3rd-5th-7th-derived-from -->
### Step 1 — QualityFacets (3rd × 5th × 7th), derived from spelling

Facet tokens: 3rd ∈ {major, minor, suspended}; 5th ∈ {perfect, augmented, diminished}; 7th/color ∈ {triad, 6, 7, maj7, dim7}. Derivation via ChordTones/ChordToneFunction: 3rd from the Third interval (4→major, 3→minor, absent→suspended); 5th from the Fifth interval (7→perfect, 8→augmented, 6→diminished); 7th from the 6th/7th degree (none→triad, Sixth→6, Seventh 10→7 / 11→maj7 / 9→dim7). References nothing under Instruments/ (architecture-test-safe, C2). Test asserts the full design §2.2 table.

<!-- step:voicinggrid-bridge-verb-filter-realize-handler -->
### Step 2 — voicingGrid bridge verb + filter/realize handler

Filter state {root, sources[], families[], thirds[], fifths[], sevenths[]}; OR within a level, AND across. Reuse CagedVoicingCatalog as the single source of truth and RealizedVoicingDiagram for realization — no parallel catalog/realizer (C3). Whole grid resolved in one round-trip (C4). Empty result ⇒ empty cell list, never an error (C5). Cells ordered rows-by-quality then cols-by-shape/form (IN5). Narrow JSON-envelope string protocol (C6).

<!-- step:fretr-additions-title-id-copy-in -->
### Step 3 — FretR additions: title, id + copy, in-grid control hiding

title = per-cell label (e.g. 'Dom7 — E shape'); id = the synthetic voicing id (auto:shell:dom7:E …) with a copy button. Reuse the existing controls:{orientation,label,…} visibility flags so the grid can hide the per-cell orientation toggle. Dumb view — no theory in JS (C1). Exercise via the sandbox.

<!-- step:guitarvoicingsr-component-filter-stack-grid-fan -->
### Step 4 — GuitarVoicingsR component (filter stack + grid + fan-out controls)

Toggle groups styled like Content → Voicings → Definitions. All-on shows everything; empty grid on no matches. Each cell created with controls.orientation:false (+ label hidden) so the grid drives them globally (IN6). Reuses FretR for every cell (C3); no theory in JS (C1).

<!-- step:hosting-page-the-dogfood-surface -->
### Step 5 — Hosting page (the dogfood surface)

A new view alongside Practice/Content/Scales; bridge.js routes voicingGridResult to it. The page is the fretboard UI surface required by the guitar-weave dogfood rule.

<!-- step:oracle-cross-check-dogfood-pass -->
### Step 6 — Oracle cross-check + dogfood pass

GuitarVoicingsR is the engine's visual oracle (IN9): the automated test pins a representative filtered grid to the authored oracle, and a manual pass checks the live page against voicings-engine-rules-reference.md.
