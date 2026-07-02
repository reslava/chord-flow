---
type: chat
id: ch_01KWD58KYP70688FAM0B0EB7BD
title: fretboard-fret-label-clip Chat
status: done
created: 2026-06-30
version: 1
tags: []
parent_id: null
requires_load: []
last_ai_block: 5
---
# fretboard-fret-label-clip Chat

## Rafa:

read idea
is it all grounded to do the fix?
I think we could write a small plan, no design, no req needed, agree?

## AI:

Read the idea and confirmed it against `fretboard-render-component.js`. It's fully grounded — and the diagnosis is even more precise than the idea sketch.

**Root cause (vertical chord box only):** in `buildSvg`, the position label is drawn at
`x: boxLeft - 8` (= `LEFT - 8` = `14`) with `text-anchor: "end"`, so the glyphs extend *leftward* from x=14. The viewBox starts at x=0 (`viewBox: "0 0 ${width} ${height}"`). A 4-glyph `10fr` at font-size 11 overruns past x=0, so the leading `1` is clipped off the canvas → reads as `0fr`. `5fr` (3 glyphs) just barely fits, which is why single-digit was never caught. `LEFT = 22` is even commented "room for a position label" — it's simply undersized for two digits.

**Horizontal neck is fine** — its label is `text-anchor: "middle"` centered at `nutX = 34`, well inside the viewBox. So the fix is contained to the vertical orientation.

So the real scope is narrower than the idea's "check both orientations / widen canvas" sketch: it's a one-spot layout fix in `buildSvg`. Cleanest fix is to give the label real room on the left — e.g. bump `LEFT` (and/or start the viewBox at a small negative x) so the end-anchored two-digit label clears x=0. Then visually confirm a fret-≥10 grip in the vertical box, and re-check single-digit + horizontal are unchanged.

**Agree on process:** small plan only — no design, no req. It's a localized, well-understood JS SVG layout fix with no domain/DSL/architecture surface. A 2–3 step plan covers it: 1) widen the vertical-box left room so `10fr`/`12fr` fits, 2) dogfood-verify the D-shape C voicing (fret 10+) on the Content fret-box both for ≥10 and single-digit, 3) update the architecture ref only if any shared geometry constant's contract changes (likely not — `LEFT`'s comment already claims this room).

Want me to draft the plan?

## Rafa:

Write plan, start it, do whole plan
Stop for visual check

## AI:

Plan written, started, and both steps done — `fretboard-fret-label-clip-plan-001`.

**The fix (1 line):** in `fretboard-render-component.js`, bumped the vertical chord-box `LEFT` constant 22 → 34. The position label is end-anchored at `x = LEFT - 8`, so it now sits at x=26; a ~20px-wide "12fr" spans roughly [6, 26] and clears the viewBox's x=0 edge with margin. Single-digit "5fr" still fits. Fret lines + markers derive from `colX = LEFT + i*COL_GAP`, so they shifted +12px consistently — no misalignment. `buildSvgHorizontal` (its own `padLeft`/`nutX`) was already fine and is untouched. No ref-doc update (LEFT is an internal render constant, not an architecture boundary).

✅ Step 1 — widen LEFT
✅ Step 2 — geometry/diff self-verify

**Stopping for your visual check.** Note: the source `wwwroot` is fixed, but the running app serves from `bin/…/wwwroot` — rebuild/run `ChordFlow.Desktop` so the copy picks it up. Then look at a fret-≥10 grip in the vertical chord-box (the D-shape canonical C at fret 10+ on the Content fret-box) and confirm `10fr`/`12fr` show fully, plus a single-digit box (`5fr`) unchanged.

## Rafa:

Fixed
Commit

## AI:

Committed and pushed to `main`.

`27e2dc3 fix: two-digit fret-position label clipped in vertical chord-box`

Staged in the one unit: the `LEFT 22→34` fix in `fretboard-render-component.js` plus this thread's idea/chat/plan docs. Thread's done — nice clean one-liner.