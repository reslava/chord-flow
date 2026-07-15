---
type: done
id: pl_01KXK9NQ0WW97X9VC68GWKVY1R-done
title: Done — Plan 4 — Per-beat "visual metronome" marker mode
status: done
created: 2026-07-15
version: 2
tags: []
parent_id: pl_01KXK9NQ0WW97X9VC68GWKVY1R
requires_load: []
---
# Done — Plan 4 — Per-beat "visual metronome" marker mode

## Step 1 — In chord-sheet-render-component.js, draw N invisible per-beat highlight regions per cell (N = beats-per-bar, parsed from model.header.timeSig numerator; e.g. 4 for 4/4) as `cf-beat-hl` rects spanning equal slices of the bar width, in BOTH layouts. Add `highlightBeat(section,row,cell,beatIndex)`: wash the cell (cf-cell-hl) + brighten the addressed beat region (reuse the cf-chord-hl amber rule, extended to `.cf-beat.cf-playing`). KEEP `highlight(section,row,cell,chord?)` unchanged for the Per-chord mode (today's behavior). Re-query the current SVG + re-apply the last marker on rebuild, exactly like today. Screen-only / export-inert.

ChordSheetR gained per-beat regions + a metronome marker. `beatsPerBar()` reads N from `model.header.timeSig` numerator (default 4). `drawRow` now draws N `<g class="cf-beat" data-beat=k>` per cell, each a `cf-beat-hl` rect over slice k (x = cellX + k/N*BAR_W, width BAR_W/N, full cell height), invisible until highlighted, behind the tokens, both layouts. `HIGHLIGHT_CSS` gains `.cf-beat.cf-playing > .cf-beat-hl` = the SAME brighter amber (rgba(245,158,11,.42)) as `cf-chord-hl` (no new palette). `applyHighlight` now washes the cell + one sub-region (a beat column when `h.beat` is set, else a chord segment); added `highlightBeat(section,row,cell,beatIndex)` alongside the unchanged `highlight(...,chord?)`; `lastHighlight` carries the mode so both survive rebuilds. Exposed `highlightBeat`; header doc updated. node --check clean.

## Step 2 — In chord-sheets.js add a 'Marker' select to the transport: 'Visual metronome' (default) / 'Per chord'. Track the mode in state. In onBeat(bar,beat): metronome mode -> resolve the cell for the bar (from the bar-downbeat entry in scheduleByBar) and call view.highlightBeat(section,row,cell, beat-1); per-chord mode -> today's behavior: pick the active sub-chord entry (last with beat<=current) and call view.highlight(section,row,cell,chord). Both keep the bar wash. Dedupe redundant calls. clearHighlight on stop/finish unchanged.

chord-sheets.js drives the two modes. Added `markerMode` state (default 'metronome') + a 'Marker' select to the transport ('Visual metronome' / 'Per chord'); changing it resets `lastMarkerKey` so the next beat re-applies in the new mode (no re-render). `onBeat` now resolves the bar's cell from its downbeat entry and branches: metronome → `view.highlightBeat(section,row,cell, beat-1)` (current beat column); per-chord → the prior behavior (last entry with beat<=current → `view.highlight(section,row,cell,chord)`). Both wash the sounding bar. Dedupe keys are mode-prefixed ('b:' / 'c:'). node --check clean.

## Step 3 — Play a song in both layouts: Visual metronome ticks beat-by-beat (every bar behaves like a 17_57_17_57 bar does today, current beat brighter); Per chord lights the active chord segment (today's behavior); both keep the bar wash and read in light + dark; toggling mode mid-play works; export unaffected. Update the ref note if the marker description needs it.

Dogfood passed (Rafa, live app): Visual metronome ticks the current beat column across each bar in time; Per chord restores the active-chord-segment highlight; switchable mid-play; both keep the bar wash. "Spectacular." Architecture ref updated to note the two selectable marker modes (Visual metronome / Per chord).
