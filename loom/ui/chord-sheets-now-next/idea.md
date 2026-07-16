---
type: idea
id: id_01KXMS31BX5B4EABZKYSTP0SQQ
title: Now/Next boards on Chord Sheets — one shared current/next-chord feed
status: done
created: 2026-07-16
version: 1
tags: []
parent_id: null
requires_load: []
---
# Now/Next boards on Chord Sheets — one shared current/next-chord feed

## What

Add the **Now/Next current+next chord fretboards** to the Chord Sheets page (the existing `ChordFlowNowNext` component), and feed them from a **current/next-chord schedule derived once in Core** that both the exercise path (Practice) and the chord-sheet path emit. Show/hide them via the **optional now-next toggle already built into PlayerControlsR**. Granularity is **per-chord** (the boards advance per chord change, even when the sheet's marker is in visual-metronome mode).

## Why

This is the deferred fast-follow from `ui/player-controls-component` (`EX1`). Practice already shows now/next boards; Chord Sheets should have parity. Crucially, the current/next-chord feed is a property of the **`RealizedSong` + comping plan**, *not* of the presentation (tab vs sheet) — so it should be derived **once** and shared, rather than each surface re-deriving it. (Design conversation: `ui/player-controls-component/chats/chat-002.md`.)

## Shape

- **Core — one shared producer.** Lift "walk a `RealizedSong` + `CompingPlan` → an ordered chord schedule" into a shared Core producer emitting a `ChordChange[]` (`{ bar, beat (0-based), chord label, comped FretboardDiagram }`). Practice already produces this as a by-product of the render pass (attached to `loadScore`); the move is to make it a first-class shared producer both paths call.
- **Bridge.** `chordSheetResult` gains that `ChordChange[]` schedule alongside the existing `{ sheet, cellSchedule, tex }`. The comped grips are emitted **independent of** the sheet's below-cell adornment toggle (now/next needs grips even when the sheet hides per-cell diagrams).
- **JS — one consumer, both pages.** Mount the existing `ChordFlowNowNext` on the Chord Sheets page, driven by the shared **`engine.on("beat")`** bus (landed in player-controls-component). Wire the PlayerControlsR **now-next toggle** by passing `onToggleNowNext` in the page's setup. No new JS component, no new event plumbing.

## Constraints / invariants

- **Reuse, don't rebuild:** `ChordFlowNowNext` unchanged; reuse the `engine.on("beat")` bus and the optional PlayerControlsR toggle.
- Keep the sheet's **`cellSchedule`** (beat → *cell/chord*, for the SVG marker) **separate** from the chord/now-next schedule (beat → *chord + grip*) — different projections of the *same* realized-song walk.
- **ChordSheetR stays a pure SVG drawer** — the now/next boards are **page-mounted** (like Practice), never inside the sheet SVG.
- Practice now-next behavior unchanged (it consumes the same shared producer's output).

## Validation

- Chord Sheets shows now/next boards advancing **per chord**, synced to playback; the PlayerControlsR now-next toggle shows/hides them.
- Practice now/next unchanged after the producer is shared.
- Dogfood on both pages (`CHORDFLOW_DEVTOOLS`).

## Reference-doc sync (required)

Changes app architecture + data flow (a shared Core chord-schedule producer feeding both `loadScore` and `chordSheetResult`; now-next on a second surface). Update `loom/refs/chordflow-architecture-reference.md` (the §6 data flow + the now-next seam) in the same unit of work; touch `chordflow-domain-model-reference.md` if the producer lands as a named Core type.

## Origin

`ui/player-controls-component/chats/chat-002.md` — the fast-follow scoping + the "derive the feed once" decision.