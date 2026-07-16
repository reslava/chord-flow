---
type: design
id: de_01KXMS3Z1MTACNXG5R4NXKA5TR
title: Now/Next boards on Chord Sheets — one shared current/next-chord feed
status: draft
created: 2026-07-16
version: 1
idea_version: 1
tags: []
parent_id: id_01KXMS31BX5B4EABZKYSTP0SQQ
requires_load: []
---
# Now/Next boards on Chord Sheets — one shared current/next-chord feed

## Context

Deferred fast-follow from `ui/player-controls-component`. Practice already renders now/next chord fretboards (`ChordFlowNowNext`) off the `loadScore` **chord schedule** (`ChordChange[]` — 0-based bar/beat + comped `FretboardDiagram`, a by-product of the render pass). Chord Sheets has no now/next boards. We add them **and** make the schedule a single shared Core producer, because the current/next-chord feed is a property of the `RealizedSong` + comping, not of tab-vs-sheet presentation.

## The shared producer (Core)

Extract the "RealizedSong + CompingPlan → chord schedule" logic (today a by-product of the exercise render pass) into **one named Core producer** — e.g. `ChordScheduleBuilder.Build(realizedSong, compingPlan) → ChordChange[]`, living in `Rendering/` (the presentation/schedule seam) or as a Features helper — TBD during planning by where the current by-product code sits. Output DTO is the existing **`ChordChange`** (`{ bar, beat, label, FretboardDiagram }`); reuse it, don't fork a parallel type.

Two callers:
1. **Exercise/loadScore path** — replace the inline by-product with a call to the shared producer (behavior identical; this is a refactor, verified by Practice now/next being unchanged).
2. **`ChordSheetHandler`** — it already builds `{ sheet, cellSchedule }` from a `RealizedSong` and resolves comping grips for the `diagram`/`both` adornments. It calls the same producer to attach a `ChordChange[]`. **Grips for now/next are resolved unconditionally** (not gated by the sheet's below-cell adornment toggle) — now/next needs the comped diagram regardless of what the sheet chooses to draw per cell.

> Decision to confirm: reuse the existing `ChordChange` DTO + producer shape verbatim (Practice's is the oracle) rather than inventing a new schedule type. Keeps one feed, one component, and makes the Practice-side change a pure extraction.

## Bridge

`chordSheetResult` payload grows from `{ sheet, cellSchedule, tex }` to `{ sheet, cellSchedule, tex, chordSchedule }` where `chordSchedule` is the `ChordChange[]` (same wire shape Practice's `loadScore` already carries). No new verb; additive field. `cellSchedule` is untouched (it stays the marker's cell-addressing projection).

## JS composition (Chord Sheets page)

`chord-sheets.js`:
- **Mount** a `ChordFlowNowNext` instance into a page container above the sheet (mirroring Practice's placement above the score). It draws two FretR boxes (current + next).
- **Toggle:** pass `onToggleNowNext: (visible) => nowNext.setVisible(visible)` (or show/hide the container) when creating PlayerControlsR in `setupEngine` — this renders the now-next checkbox we already built as optional. Default visible, matching Practice.
- **Feed:** on `chordSheetResult`, hand `msg.chordSchedule` to the `ChordFlowNowNext` instance (its existing "load a schedule" entry point).
- **Sync:** drive it off the shared bus — `engine.on("beat", (bar, beat) => nowNext.update(bar, beat))` — advancing to the `ChordChange` active at `(bar,beat)`. **Per-chord granularity**: the boards track the current chord change regardless of the marker mode (visual-metronome vs per-chord); the marker and the boards are independent projections of the same beat.

Reuse `ChordFlowNowNext` as-is — the fresh session confirms its exact API (`create`/`load`/`update`/`setVisible`/`dispose`) against `now-next-fretboards.js` when planning.

## Invariants

- `ChordFlowNowNext` unchanged; the `engine.on("beat")` bus + the optional PlayerControlsR toggle are reused (no new component, no new event plumbing).
- `cellSchedule` (marker) and `chordSchedule` (now/next) are separate projections of one realized-song walk.
- ChordSheetR (the SVG drawer) is untouched — boards are page-mounted, never in the SVG (export parity intact).
- Practice now/next behavior is byte-for-byte equivalent (the extraction is verified by it, not changed).

## Validation

- Chord Sheets: now/next boards advance per chord, synced to audio; the PlayerControlsR now-next toggle shows/hides them; export unaffected.
- Practice: now/next unchanged after the producer extraction.
- Dogfood both pages via `CHORDFLOW_DEVTOOLS`.

## Reference-doc sync (required)

Update `loom/refs/chordflow-architecture-reference.md` — the §6 data-flow (the shared chord-schedule producer feeding both `loadScore` and `chordSheetResult`) and the now-next seam note — in the same unit of work. If the producer lands as a named Core type, add it to `chordflow-domain-model-reference.md`.

## Open / for the plan session

- Confirm where the current `ChordChange` by-product is produced (Rendering vs Features) → where the shared producer lives.
- Confirm `ChordFlowNowNext`'s exact create/load/update/visibility API.
- Confirm the page container + placement for the boards on the Chord Sheets layout.