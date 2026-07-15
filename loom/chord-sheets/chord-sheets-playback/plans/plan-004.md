---
type: plan
id: pl_01KXK9NQ0WW97X9VC68GWKVY1R
title: Plan 4 — Per-beat "visual metronome" marker mode
status: done
created: 2026-07-15
updated: 2026-07-15
version: 3
design_version: 1
req_version: 1
tags: []
parent_id: de_01KXJJJYD9XBRYED1F8HYTG74H
requires_load: []
target_version: 0.1.0
steps:
  - id: chordsheetr-per-beat-regions-highlightbeat
    order: 1
    status: done
    description: "In chord-sheet-render-component.js, draw N invisible per-beat highlight regions per cell (N = beats-per-bar, parsed from model.header.timeSig numerator; e.g. 4 for 4/4) as `cf-beat-hl` rects spanning equal slices of the bar width, in BOTH layouts. Add `highlightBeat(section,row,cell,beatIndex)`: wash the cell (cf-cell-hl) + brighten the addressed beat region (reuse the cf-chord-hl amber rule, extended to `.cf-beat.cf-playing`). KEEP `highlight(section,row,cell,chord?)` unchanged for the Per-chord mode (today's behavior). Re-query the current SVG + re-apply the last marker on rebuild, exactly like today. Screen-only / export-inert."
    files_touched: [src/ChordFlow.Desktop/wwwroot/chord-sheet-render-component.js]
    blocked_by: []
    satisfies: [IN9, IN14, C1, C5]
  - id: page-marker-mode-toggle-drive-it
    order: 2
    status: done
    description: "In chord-sheets.js add a 'Marker' select to the transport: 'Visual metronome' (default) / 'Per chord'. Track the mode in state. In onBeat(bar,beat): metronome mode -> resolve the cell for the bar (from the bar-downbeat entry in scheduleByBar) and call view.highlightBeat(section,row,cell, beat-1); per-chord mode -> today's behavior: pick the active sub-chord entry (last with beat<=current) and call view.highlight(section,row,cell,chord). Both keep the bar wash. Dedupe redundant calls. clearHighlight on stop/finish unchanged."
    files_touched: [src/ChordFlow.Desktop/wwwroot/chord-sheets.js]
    blocked_by: [chordsheetr-per-beat-regions-highlightbeat]
    satisfies: [IN9, IN11, IN14, C4]
  - id: dogfood-the-two-modes
    order: 3
    status: done
    description: "Play a song in both layouts: Visual metronome ticks beat-by-beat (every bar behaves like a 17_57_17_57 bar does today, current beat brighter); Per chord lights the active chord segment (today's behavior); both keep the bar wash and read in light + dark; toggling mode mid-play works; export unaffected. Update the ref note if the marker description needs it."
    files_touched: [loom/refs/chordflow-architecture-reference.md]
    blocked_by: [page-marker-mode-toggle-drive-it]
    satisfies: [IN13, IN14, C6]
---
# Plan 4 — Per-beat "visual metronome" marker mode

## Goal

Add a marker-mode toggle to the Chord Sheets transport: "Visual metronome" (default) subdivides every bar into its beats and lights the current beat in time — the bar washed light amber, the current beat brighter — reusing the EXACT existing amber highlight visuals (cf-cell-hl + cf-chord-hl), no new palette; "Per chord" keeps today's active-chord-segment highlight. Both modes keep the bar-level wash marking the sounding bar; the toggle only swaps the sub-highlight (current beat vs current chord segment). Beats-per-bar is read from the sheet's time signature (4/4 -> 4), consistent with the always-rendered quarter-note comp (so alphaTab's beat index 0..3 maps 1:1 onto the drawn beat regions). ChordSheetR gains per-beat highlight regions + a highlightBeat() method alongside the existing highlight(); the page adds the toggle and drives the selected mode from onBeat. The per-chord highlight is retained as the non-default mode (not replaced).

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | In chord-sheet-render-component.js, draw N invisible per-beat highlight regions per cell (N = beats-per-bar, parsed from model.header.timeSig numerator; e.g. 4 for 4/4) as `cf-beat-hl` rects spanning equal slices of the bar width, in BOTH layouts. Add `highlightBeat(section,row,cell,beatIndex)`: wash the cell (cf-cell-hl) + brighten the addressed beat region (reuse the cf-chord-hl amber rule, extended to `.cf-beat.cf-playing`). KEEP `highlight(section,row,cell,chord?)` unchanged for the Per-chord mode (today's behavior). Re-query the current SVG + re-apply the last marker on rebuild, exactly like today. Screen-only / export-inert. | src/ChordFlow.Desktop/wwwroot/chord-sheet-render-component.js | — | IN9, IN14, C1, C5 |
| ✅ | 2 | In chord-sheets.js add a 'Marker' select to the transport: 'Visual metronome' (default) / 'Per chord'. Track the mode in state. In onBeat(bar,beat): metronome mode -> resolve the cell for the bar (from the bar-downbeat entry in scheduleByBar) and call view.highlightBeat(section,row,cell, beat-1); per-chord mode -> today's behavior: pick the active sub-chord entry (last with beat<=current) and call view.highlight(section,row,cell,chord). Both keep the bar wash. Dedupe redundant calls. clearHighlight on stop/finish unchanged. | src/ChordFlow.Desktop/wwwroot/chord-sheets.js | chordsheetr-per-beat-regions-highlightbeat | IN9, IN11, IN14, C4 |
| ✅ | 3 | Play a song in both layouts: Visual metronome ticks beat-by-beat (every bar behaves like a 17_57_17_57 bar does today, current beat brighter); Per chord lights the active chord segment (today's behavior); both keep the bar wash and read in light + dark; toggling mode mid-play works; export unaffected. Update the ref note if the marker description needs it. | loom/refs/chordflow-architecture-reference.md | page-marker-mode-toggle-drive-it | IN13, IN14, C6 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:chordsheetr-per-beat-regions-highlightbeat -->
### Step 1 — ChordSheetR: per-beat regions + highlightBeat()

Beat regions are a sibling subdivision to the chord groups: `<g class="cf-beat" data-beat=k>` with a `cf-beat-hl` rect at x = cellX + k/N*BAR_W, width BAR_W/N, height CHORD_ROW_H. The existing HIGHLIGHT_CSS gains a `.cf-beat.cf-playing > .cf-beat-hl` rule using the SAME brighter amber as cf-chord-hl. Number-of-alphaTab-beats == time-sig numerator holds because the handler always renders SeedData.Quarters; note that coupling.

<!-- step:page-marker-mode-toggle-drive-it -->
### Step 2 — Page: marker-mode toggle + drive it from onBeat

Metronome mode keys off (bar) for the cell + (beat) for the region, so it does not need the sub-chord schedule entries; the per-bar bar-downbeat entry gives the cell. Switching mode mid-play should take effect on the next beat (no need to re-render).
