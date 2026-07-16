---
type: plan
id: pl_01KXMSS2CNJTTR5A0B0DYPTQB2
title: Now/Next boards on Chord Sheets — surface the shared chord schedule + mount the boards
status: done
created: 2026-07-16
updated: 2026-07-16
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KXMS3Z1MTACNXG5R4NXKA5TR
requires_load: []
target_version: 0.1.0
steps:
  - id: surface-the-already-produced-chord-schedule
    order: 1
    status: done
    description: Add an additive `chordSchedule` (ChordChange[]) field to ChordSheetResultEnvelope and populate it from the render pass that already runs; update handler tests.
    files_touched: [src/ChordFlow.Core/Features/ChordSheets/ChordSheetEnvelopes.cs, src/ChordFlow.Core/Features/ChordSheets/ChordSheetHandler.cs, tests/ChordFlow.Core.Tests/ChordSheets/ChordSheetHandlerTests.cs]
    blocked_by: []
    satisfies: [IN1, IN2, IN3, C1, C3, EX2, EX4]
  - id: mount-feed-sync-toggle-chordflownownext-on
    order: 2
    status: done
    description: Create a ChordFlowNowNext instance above the sheet, feed it chordSchedule, drive it from the existing onBeat callback (1-based → 0-based), reset on finish, and wire PlayerControlsR's onToggleNowNext to show/hide it.
    files_touched: [src/ChordFlow.Desktop/wwwroot/chord-sheets.js]
    blocked_by: [surface-the-already-produced-chord-schedule]
    satisfies: [IN4, IN5, IN6, C2, C4, EX1, EX3]
  - id: reference-doc-sync-architecture-6-data
    order: 3
    status: done
    description: Update chordflow-architecture-reference.md so §6 records chordSchedule on chordSheetResult and the now-next seam extending to the Chord Sheets surface; no domain-model-ref change (no new named Core type).
    files_touched: [loom/refs/chordflow-architecture-reference.md]
    blocked_by: [surface-the-already-produced-chord-schedule, mount-feed-sync-toggle-chordflownownext-on]
    satisfies: [IN7]
---
# Now/Next boards on Chord Sheets — surface the shared chord schedule + mount the boards

## Goal

Add the Now/Next current+next-chord fretboards to the Chord Sheets page, fed by the chord schedule the render pass **already produces**. Planning-session resolution of the design's TBD (idea/design/req id_01KXMS31BX5B4EABZKYSTP0SQQ / de_01KXMS3Z1MTACNXG5R4NXKA5TR / rq_01KXMS4EVN3ABZAG3ERTAF11YN): the "shared producer" IN1 asks for **already exists** — `AlphaTexRenderer.Render(realized, …, comping, …)` returns `RenderResult(Tex, Schedule)` where `Schedule` is the `ChordChange[]` (one entry per chord change, each with the comped `FretboardDiagram`), and `ChordSheetHandler.Build` already calls that exact renderer (ChordSheetHandler.cs:76-80) with comping resolved unconditionally (ChordSheetHandler.cs:59-66, satisfying IN2). Today `render.Schedule` is computed and then discarded when building the envelope. So there is **no `ChordScheduleBuilder` to extract** and no Practice-side change: the whole Core task is to stop discarding the already-shared schedule and surface it as an additive `chordSchedule` field on `chordSheetResult` (reusing `ChordChange`, EX2/EX4). The rest is JS on the Chord Sheets page — mount the existing `ChordFlowNowNext` above the sheet, feed it `chordSchedule`, drive it off the page's existing per-beat `onBeat` callback (converting alphaTab's 1-based (bar,beat) down to the schedule's 0-based, exactly as `app.js` does), and expose the show/hide via PlayerControlsR's optional `onToggleNowNext`. `ChordFlowNowNext` and ChordSheetR are untouched (EX1/EX3, C2); per-chord granularity (IN6) is inherent because `RecordChordChange` dedups by chord label. Close with the mandatory architecture-ref sync (IN7).

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add an additive `chordSchedule` (ChordChange[]) field to ChordSheetResultEnvelope and populate it from the render pass that already runs; update handler tests. | src/ChordFlow.Core/Features/ChordSheets/ChordSheetEnvelopes.cs, src/ChordFlow.Core/Features/ChordSheets/ChordSheetHandler.cs, tests/ChordFlow.Core.Tests/ChordSheets/ChordSheetHandlerTests.cs | — | IN1, IN2, IN3, C1, C3, EX2, EX4 |
| ✅ | 2 | Create a ChordFlowNowNext instance above the sheet, feed it chordSchedule, drive it from the existing onBeat callback (1-based → 0-based), reset on finish, and wire PlayerControlsR's onToggleNowNext to show/hide it. | src/ChordFlow.Desktop/wwwroot/chord-sheets.js | surface-the-already-produced-chord-schedule | IN4, IN5, IN6, C2, C4, EX1, EX3 |
| ✅ | 3 | Update chordflow-architecture-reference.md so §6 records chordSchedule on chordSheetResult and the now-next seam extending to the Chord Sheets surface; no domain-model-ref change (no new named Core type). | loom/refs/chordflow-architecture-reference.md | surface-the-already-produced-chord-schedule, mount-feed-sync-toggle-chordflownownext-on | IN7 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:surface-the-already-produced-chord-schedule -->
### Step 1 — Surface the already-produced chord schedule on chordSheetResult

**No new producer, no Practice-side change.** The shared producer IN1 asks for is the existing `AlphaTexRenderer.Render → RenderResult(Tex, Schedule)`, and `ChordSheetHandler.Build` already calls it (ChordSheetHandler.cs:76-80) with comping resolved unconditionally (lines 59-66 → IN2 already met). `render.Schedule` is computed today and discarded at the envelope construction (line 82).

1. `ChordSheetEnvelopes.cs`: add `IReadOnlyList<ChordChange> ChordSchedule` to `ChordSheetResultEnvelope` as an additive field placed **before** the `string Type = "chordSheetResult"` default (records can't have a required param after a defaulted one). Wire shape matches Practice's `loadScore` schedule exactly (reuse `ChordChange` — EX2/EX4). `Sheet`/`CellSchedule`/`Tex` unchanged; update the doc-comment JSON to `{…,"cellSchedule":[…],"chordSchedule":[…],"tex":"…"}`.
2. `ChordSheetHandler.cs`: pass `render.Schedule` into the envelope at line 82 (`new ChordSheetResultEnvelope(built.Sheet, cellSchedule, render.Schedule, render.Tex)` — mind arg order). Nothing else changes; `cellSchedule` stays its own projection (C1).
3. `ChordSheetHandlerTests.cs`: add/extend a test asserting `ChordSchedule` is non-empty for a split-bar sheet, carries comped `Diagram`s **even when the adornment is `none`** (IN2 — grips unconditional), and is a distinct projection from `CellSchedule` (C1). Practice parity (C3) needs no test change — the renderer call is untouched.

<!-- step:mount-feed-sync-toggle-chordflownownext-on -->
### Step 2 — Mount + feed + sync + toggle ChordFlowNowNext on the Chord Sheets page

Mirror Practice's `app.js` wiring; reuse `ChordFlowNowNext` and PlayerControlsR **as-is** (EX1, C4). ChordSheetR/SVG untouched — the boards are page-mounted (EX3, C2).

1. **Container**: in `init()`, create a `nowNextEl` div and append it **above** `sheetEl` (after `hintEl`, before/around `scoreWrapEl`), matching Practice's placement above the score. Add a module var `let nowNext = null;`.
2. **Create**: after the DOM is built, `if (window.ChordFlowNowNext) nowNext = window.ChordFlowNowNext.create(nowNextEl);` (its API is `{ setSchedule, onBeat, reset, dispose }` — **note: no `setVisible`**).
3. **Toggle (IN5)**: in `setupEngine`, pass `onToggleNowNext: (visible) => { nowNextEl.hidden = !visible; }` in the `ChordFlowPlayerControls.create(null, engine, { … })` opts — this renders the optional Now/Next checkbox (default checked → default visible, matching Practice). Since there is no `setVisible`, the toggle hides the page container, exactly as `app.js` flips its `now-next-pane`.
4. **Feed (IN4)**: in `onHostMessage`'s `chordSheetResult` branch, `if (nowNext) nowNext.setSchedule(msg.chordSchedule);` (alongside the existing `buildSchedule(msg.cellSchedule)` — the two schedules stay separate, C1).
5. **Sync (IN6)**: in the existing `onBeat(bar, beat)` function, add `if (nowNext) nowNext.onBeat(bar - 1, beat - 1);` — the engine reports 1-based, `ChordFlowNowNext` expects 0-based (the `app.js` convention). This is independent of `markerMode`, so the boards advance per chord regardless of the marker (IN6). No separate `engine.on("beat")` bus is needed — this `onBeat` callback IS the page's beat signal (C4).
6. **Reset**: in `setupEngine`'s `onFinished` callback, add `if (nowNext) nowNext.reset();` (back to the first chord on stop/end, schedule kept for replay — matches Practice).

<!-- step:reference-doc-sync-architecture-6-data -->
### Step 3 — Reference-doc sync — architecture §6 data-flow + now-next seam

Mandatory same-unit ref sync (CLAUDE-LOCAL). In `chordflow-architecture-reference.md`: (a) §6 data-flow — note `chordSheetResult` now carries `chordSchedule` (the same `RenderResult.Schedule` / `ChordChange[]` Practice's `loadScore` carries), derived from the one realized-song render pass alongside `cellSchedule`/`tex`; (b) the now-next seam note — the shared feed is `AlphaTexRenderer.Render`'s `Schedule` (NOT a separate `ChordScheduleBuilder`), consumed by `ChordFlowNowNext` on **both** the Practice and Chord Sheets surfaces via each page's `onBeat`→0-based conversion. **No `chordflow-domain-model-reference.md` change** — the producer is the existing renderer, `ChordChange`/`RenderResult` are already documented there and no new named type lands (this is the planning-session resolution of the design's 'if the producer lands as a named Core type' clause: it does not).
