---
type: plan
id: pl_01KWD66VNFKG67AFXA6HJQNPA4
title: Fix two-digit fret-position label clipping in the vertical chord-box
status: done
created: 2026-06-30
updated: 2026-06-30
version: 1
design_version: 1
tags: []
parent_id: null
requires_load: []
target_version: 0.1.0
steps:
  - id: widen-vertical-box-left-margin-so
    order: 1
    status: done
    description: In fretboard-render-component.js, bump the vertical chord-box LEFT constant from 22 so the end-anchored position label (x = LEFT - 8) has room for a 4-glyph "10fr"/"12fr" at font-size 11 (~20px wide) before x=0. Confirm single-digit labels still fit and that fret lines + markers stay aligned (all derive from colX = LEFT + i*COL_GAP, so they shift consistently). Leave buildSvgHorizontal untouched.
    files_touched: [src/ChordFlow.Desktop/wwwroot/fretboard-render-component.js]
    blocked_by: []
    satisfies: []
  - id: self-verify-geometry-diff
    order: 2
    status: done
    description: "Re-read the changed buildSvg region and confirm the margin math holds: end-anchored \"12fr\" at x = LEFT - 8 fully clears x=0, the box stays centered (margin:auto), and nothing in the horizontal path or shared constants regressed. No reference-doc update needed — LEFT is an internal render constant, not an architecture boundary."
    files_touched: [src/ChordFlow.Desktop/wwwroot/fretboard-render-component.js]
    blocked_by: []
    satisfies: []
---
# Fix two-digit fret-position label clipping in the vertical chord-box

## Goal

Fix the clipped two-digit fret-position label (10fr → 0fr) in the shared fretboard-render-component.js. Root cause is confined to the vertical chord-box (buildSvg): the position label is end-anchored at x = boxLeft - 8 (= LEFT - 8 = 14) and extends leftward past the viewBox x=0 edge, so a 4-glyph label like "10fr"/"12fr" loses its leading digit. The horizontal neck is unaffected (its label is middle-anchored well inside the canvas). The fix widens the vertical-box left margin (the LEFT geometry constant, already commented "room for a position label") so an end-anchored two-digit label clears x=0, with all column/fret/marker geometry — which derives from colX→LEFT — shifting consistently. Then stop for a human visual check (the D-shape C voicing at fret 10+ on the Content fret-box).

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | In fretboard-render-component.js, bump the vertical chord-box LEFT constant from 22 so the end-anchored position label (x = LEFT - 8) has room for a 4-glyph "10fr"/"12fr" at font-size 11 (~20px wide) before x=0. Confirm single-digit labels still fit and that fret lines + markers stay aligned (all derive from colX = LEFT + i*COL_GAP, so they shift consistently). Leave buildSvgHorizontal untouched. | src/ChordFlow.Desktop/wwwroot/fretboard-render-component.js | — | — |
| ✅ | 2 | Re-read the changed buildSvg region and confirm the margin math holds: end-anchored "12fr" at x = LEFT - 8 fully clears x=0, the box stays centered (margin:auto), and nothing in the horizontal path or shared constants regressed. No reference-doc update needed — LEFT is an internal render constant, not an architecture boundary. | src/ChordFlow.Desktop/wwwroot/fretboard-render-component.js | — | — |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
