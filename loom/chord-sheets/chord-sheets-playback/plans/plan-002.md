---
type: plan
id: pl_01KXJM98ZZMPCYW4XNH3KY6B6K
title: Plan 2 — Chord-sheet playback (marker on ChordSheetR)
status: done
created: 2026-07-15
updated: 2026-07-15
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KXJJJYD9XBRYED1F8HYTG74H
requires_load: []
target_version: 0.1.0
steps:
  - id: core-cellschedule-from-the-builder
    order: 1
    status: done
    description: "Add a pure CellScheduleEntry(Bar, Beat, Section, Row, Cell, Chord) record + have ChordSheetBuilder emit a per-bar BAR-MAP while it walks bars: one entry per bar (global 0-based bar index -> Section/Row/Cell) at the bar downbeat (Beat=0, Chord=0), covering % RepeatOfPrev AND sustained bars. The builder does NOT compute rhythm-slot beats (it has no rhythm layout) — split-cell sub-chord onset beats are overlaid in step 2 from the render schedule (approach A). Test the bar-map covers every bar incl. % and multi-chord cells."
    files_touched: [src/ChordFlow.Core/Rendering/ChordSheets/ChordSheet.cs, src/ChordFlow.Core/Features/ChordSheets/ChordSheetBuilder.cs, tests/ChordFlow.Core.Tests/ChordSheets/ChordSheetBuilderTests.cs]
    blocked_by: []
    satisfies: [IN5, C3]
  - id: bridge-chordsheetresult-carries-sheet-cellschedule-tex
    order: 2
    status: done
    description: "Realize the Song ONCE in ChordSheetHandler and feed the builder (sheet + bar-map) AND the alphaTex renderer (tex + its ChordChange schedule) from that single expansion. ASSEMBLE the final cellSchedule in the handler: per-bar downbeat entries from the bar-map (covers %/sustained), overlaid with sub-chord onset beats from RenderResult.Schedule for split bars, so (bar,beat) aligns with the audio timeline by construction (D1-a). Extend chordSheetResult to { sheet, cellSchedule, tex } + the envelope DTOs + serialization."
    files_touched: [src/ChordFlow.Core/Bridge/ChordSheetEnvelopes.cs, src/ChordFlow.Core/Features/ChordSheets/ChordSheetHandler.cs, tests/ChordFlow.Core.Tests/ChordSheets/ChordSheetHandlerTests.cs]
    blocked_by: [core-cellschedule-from-the-builder]
    satisfies: [IN4, IN6, C3]
  - id: chordsheetr-addressable-g-groups
    order: 3
    status: done
    description: Wrap each drawn bar-cell in <g data-section data-row data-cell> and each chord segment of a split cell in a nested <g data-chord>, in BOTH layouts (A flowing, B grid). Pure structural change — same pixels, now addressable. ChordSheetR stays a dumb view with zero alphaTab dependency.
    files_touched: [src/ChordFlow.Desktop/wwwroot/chord-sheet-render-component.js]
    blocked_by: []
    satisfies: [IN7, C1]
  - id: chordsheetr-highlight-clearhighlight-visual
    order: 4
    status: done
    description: "Add sheet.highlight(section,row,cell,chord?) and sheet.clearHighlight(): toggle a screen-only 'cf-playing' state on the addressed <g> by RE-QUERYING the current SVG (no held node refs). Cell-level highlight always + the active chord segment within a split bar (D2); adornments ride the cell wash. A light/dark-safe accent (translucent rect behind the tokens) from the shared palette. Never present in toSvgString/toPngBlob/lightSvg output."
    files_touched: [src/ChordFlow.Desktop/wwwroot/chord-sheet-render-component.js]
    blocked_by: [chordsheetr-addressable-g-groups]
    satisfies: [IN8, IN9, IN10, C5]
  - id: page-own-a-chordflowplayback-drive-the
    order: 5
    status: done
    description: "chord-sheets.js gains a hidden ChordFlowPlayback surface + a transport strip (play/stop/tempo, soundfont). On chordSheetResult: load tex into the engine, build a 'bar:beat'->cell map from cellSchedule. onBeat(bar,beat) -> lookup -> sheet.highlight(...); unknown sub-onset beats keep the last cell. onStateChange/onFinished -> clearHighlight on stop/end. Start/stop/seek land on the correct cell. Staff hidden by default with an optional 'Show tab' collapsible (D4)."
    files_touched: [src/ChordFlow.Desktop/wwwroot/chord-sheets.js, src/ChordFlow.Desktop/wwwroot/index.html, src/ChordFlow.Desktop/wwwroot/bridge.js]
    blocked_by: [bridge-chordsheetresult-carries-sheet-cellschedule-tex, chordsheetr-highlight-clearhighlight-visual, pl_01KXJKMRCMYANGZCWRP1F4FZ34]
    satisfies: [IN11, IN12, C4]
  - id: dogfood-ref-sync
    order: 6
    status: done
    description: "Play Jazz Blues + a pop song in both layouts: confirm the marker tracks the sounding bar/cell, matches the ScoreR cursor beat-for-beat, handles a multi-chord split bar and a % bar, clears on stop, reads in light + dark, and that export is unaffected. Update chordflow-domain-model-reference (cellSchedule projection off ChordSheetBuilder) and chordflow-architecture-reference (chordSheetResult now carries cellSchedule + tex)."
    files_touched: [loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-architecture-reference.md]
    blocked_by: [page-own-a-chordflowplayback-drive-the]
    satisfies: [IN13, C6]
---
# Plan 2 — Chord-sheet playback (marker on ChordSheetR)

## Goal

On the proven ChordFlowPlayback engine from Plan 1, wire an animated playback marker onto ChordSheetR. Core: ChordSheetBuilder emits a cellSchedule ((bar,beat)->cell) from the same realized Song, and the chordSheet handler returns sheet + cellSchedule + alphaTex in one aligned pass (D1-a). JS: ChordSheetR becomes addressable (per-cell and per-chord-segment <g> groups) and gains highlight()/clearHighlight() as a screen-only, light/dark-safe, export-inert state; the Chord Sheets page owns its own ChordFlowPlayback (hidden staff), plays the tex, and drives the marker from onBeat with correct handling of split bars and % similes, plus start/stop/seek. Dogfooded on Jazz Blues + a pop song in both layouts; both reference docs updated in the same unit of work.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add a pure CellScheduleEntry(Bar, Beat, Section, Row, Cell, Chord) record + have ChordSheetBuilder emit a per-bar BAR-MAP while it walks bars: one entry per bar (global 0-based bar index -> Section/Row/Cell) at the bar downbeat (Beat=0, Chord=0), covering % RepeatOfPrev AND sustained bars. The builder does NOT compute rhythm-slot beats (it has no rhythm layout) — split-cell sub-chord onset beats are overlaid in step 2 from the render schedule (approach A). Test the bar-map covers every bar incl. % and multi-chord cells. | src/ChordFlow.Core/Rendering/ChordSheets/ChordSheet.cs, src/ChordFlow.Core/Features/ChordSheets/ChordSheetBuilder.cs, tests/ChordFlow.Core.Tests/ChordSheets/ChordSheetBuilderTests.cs | — | IN5, C3 |
| ✅ | 2 | Realize the Song ONCE in ChordSheetHandler and feed the builder (sheet + bar-map) AND the alphaTex renderer (tex + its ChordChange schedule) from that single expansion. ASSEMBLE the final cellSchedule in the handler: per-bar downbeat entries from the bar-map (covers %/sustained), overlaid with sub-chord onset beats from RenderResult.Schedule for split bars, so (bar,beat) aligns with the audio timeline by construction (D1-a). Extend chordSheetResult to { sheet, cellSchedule, tex } + the envelope DTOs + serialization. | src/ChordFlow.Core/Bridge/ChordSheetEnvelopes.cs, src/ChordFlow.Core/Features/ChordSheets/ChordSheetHandler.cs, tests/ChordFlow.Core.Tests/ChordSheets/ChordSheetHandlerTests.cs | core-cellschedule-from-the-builder | IN4, IN6, C3 |
| ✅ | 3 | Wrap each drawn bar-cell in <g data-section data-row data-cell> and each chord segment of a split cell in a nested <g data-chord>, in BOTH layouts (A flowing, B grid). Pure structural change — same pixels, now addressable. ChordSheetR stays a dumb view with zero alphaTab dependency. | src/ChordFlow.Desktop/wwwroot/chord-sheet-render-component.js | — | IN7, C1 |
| ✅ | 4 | Add sheet.highlight(section,row,cell,chord?) and sheet.clearHighlight(): toggle a screen-only 'cf-playing' state on the addressed <g> by RE-QUERYING the current SVG (no held node refs). Cell-level highlight always + the active chord segment within a split bar (D2); adornments ride the cell wash. A light/dark-safe accent (translucent rect behind the tokens) from the shared palette. Never present in toSvgString/toPngBlob/lightSvg output. | src/ChordFlow.Desktop/wwwroot/chord-sheet-render-component.js | chordsheetr-addressable-g-groups | IN8, IN9, IN10, C5 |
| ✅ | 5 | chord-sheets.js gains a hidden ChordFlowPlayback surface + a transport strip (play/stop/tempo, soundfont). On chordSheetResult: load tex into the engine, build a 'bar:beat'->cell map from cellSchedule. onBeat(bar,beat) -> lookup -> sheet.highlight(...); unknown sub-onset beats keep the last cell. onStateChange/onFinished -> clearHighlight on stop/end. Start/stop/seek land on the correct cell. Staff hidden by default with an optional 'Show tab' collapsible (D4). | src/ChordFlow.Desktop/wwwroot/chord-sheets.js, src/ChordFlow.Desktop/wwwroot/index.html, src/ChordFlow.Desktop/wwwroot/bridge.js | bridge-chordsheetresult-carries-sheet-cellschedule-tex, chordsheetr-highlight-clearhighlight-visual, pl_01KXJKMRCMYANGZCWRP1F4FZ34 | IN11, IN12, C4 |
| ✅ | 6 | Play Jazz Blues + a pop song in both layouts: confirm the marker tracks the sounding bar/cell, matches the ScoreR cursor beat-for-beat, handles a multi-chord split bar and a % bar, clears on stop, reads in light + dark, and that export is unaffected. Update chordflow-domain-model-reference (cellSchedule projection off ChordSheetBuilder) and chordflow-architecture-reference (chordSheetResult now carries cellSchedule + tex). | loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-architecture-reference.md | page-own-a-chordflowplayback-drive-the | IN13, C6 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:core-cellschedule-from-the-builder -->
### Step 1 — Core: cellSchedule from the builder

The builder already assigns section/row/cell positions while walking; emit the schedule entry as each cell/chord is placed. Beat = chord onset within the bar in the same units alphaTab reports (derive from ChordSpan tick offsets → beat index). Test the split-bar and %-bar cases explicitly.

<!-- step:bridge-chordsheetresult-carries-sheet-cellschedule-tex -->
### Step 2 — Bridge: chordSheetResult carries sheet + cellSchedule + tex

If ChordSheetBuilder currently realizes the Song itself, refactor so the handler owns the single realization and passes the expanded Song to both the builder and the renderer. tex is rendered even when the user only exports — acceptable per D1-a.

<!-- step:chordsheetr-addressable-g-groups -->
### Step 3 — ChordSheetR: addressable <g> groups

drawRow/drawCell append into a per-cell <g> instead of straight onto the svg; drawChord segments of a split cell go into per-chord <g>. Keep export identical (the groups are inert markup).

<!-- step:chordsheetr-highlight-clearhighlight-visual -->
### Step 4 — ChordSheetR: highlight()/clearHighlight() + visual

Because render() does innerHTML='' and the page disposes/recreates on display toggles, highlight must re-query and the page re-applies the last highlight after a re-render. Export builds a fresh SVG with no highlight state, so it is inert by construction — confirm no 'cf-playing' leaks into serialized output.

<!-- step:page-own-a-chordflowplayback-drive-the -->
### Step 5 — Page: own a ChordFlowPlayback + drive the marker

Each page owns its own engine (option a) — no cross-page transport. onBeat is 1-based from the engine; cellSchedule is 0-based — convert like app.js does for NowNext. Re-apply last highlight after a pure-JS re-render (layout/notation/theme toggle).
