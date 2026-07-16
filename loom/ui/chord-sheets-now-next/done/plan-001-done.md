---
type: done
id: pl_01KXMSS2CNJTTR5A0B0DYPTQB2-done
title: Done — Now/Next boards on Chord Sheets — surface the shared chord schedule + mount the boards
status: done
created: 2026-07-16
version: 1
tags: []
parent_id: pl_01KXMSS2CNJTTR5A0B0DYPTQB2
requires_load: []
---
# Done — Now/Next boards on Chord Sheets — surface the shared chord schedule + mount the boards

## Step 1 — Add an additive `chordSchedule` (ChordChange[]) field to ChordSheetResultEnvelope and populate it from the render pass that already runs; update handler tests.

**Surfaced the already-produced chord schedule on `chordSheetResult`.** No new producer — the shared feed is the existing `AlphaTexRenderer.Render → RenderResult.Schedule`, which `ChordSheetHandler.Build` already computes (and, before this change, discarded).

- `ChordSheetEnvelopes.cs`: added `IReadOnlyList<ChordChange> ChordSchedule` to `ChordSheetResultEnvelope` (positioned before the `Tex` param, ahead of the defaulted `Type`); added `using ChordFlow.Rendering;`; refreshed the doc-comment wire shape to `{…,"cellSchedule":[…],"chordSchedule":[…],"tex":"…"}` and noted the three are separate projections of one render pass.
- `ChordSheetHandler.cs`: passed `render.Schedule` into the envelope (`new ChordSheetResultEnvelope(built.Sheet, cellSchedule, render.Schedule, render.Tex)`). Comping was already resolved unconditionally (lines 59-66) → IN2 needed no code change.
- `ChordSheetHandlerTests.cs`: added `Build_SurfacesChordSchedule_WithCompedGrips_EvenWhenAdornmentNone` (asserts the feed is populated, every `ChordChange` carries a non-null `Diagram` at adornment `none`, and the labels are `C7,F7,C7` — dedup-by-label proves per-chord granularity, IN6) and `Build_ChordScheduleAndCellSchedule_AreSeparateProjections` (split bar `17_47` → `C7,F7` with the two onsets, distinct from `cellSchedule`, C1).
- Satisfies IN1, IN2, IN3, C1, C3, EX2, EX4. `dotnet test` green: 922 passed.

## Step 2 — Create a ChordFlowNowNext instance above the sheet, feed it chordSchedule, drive it from the existing onBeat callback (1-based → 0-based), reset on finish, and wire PlayerControlsR's onToggleNowNext to show/hide it.

**Mounted + fed + synced + toggled `ChordFlowNowNext` on the Chord Sheets page** — `src/ChordFlow.Desktop/wwwroot/chord-sheets.js`, mirroring Practice's `app.js`, reusing the component and PlayerControlsR as-is (EX1, C4).

- Added module vars `nowNext` / `nowNextEl`.
- `init()`: created a `nowNextEl` container appended **above** `sheetEl` (below the collapsed tab surface), and `nowNext = window.ChordFlowNowNext.create(nowNextEl)` (its API is `{setSchedule, onBeat, reset, dispose}` — no `setVisible`).
- `setupEngine()`: passed `onToggleNowNext: (visible) => { nowNextEl.hidden = !visible; }` to `ChordFlowPlayerControls.create` (renders the optional Now/Next checkbox, default checked/visible — IN5); added `nowNext.reset()` to `onFinished`.
- `onHostMessage` `chordSheetResult`: `nowNext.setSchedule(msg.chordSchedule)` alongside the existing `buildSchedule(msg.cellSchedule)` (the two schedules stay separate — C1).
- `onBeat(bar,beat)`: `nowNext.onBeat(bar - 1, beat - 1)` at the top (before the `!view` guard) — the engine reports 1-based, the schedule/component is 0-based (the `app.js` convention); independent of `markerMode`, so boards advance per chord (IN6). The `onBeat` callback IS the page's beat signal — no separate `engine.on("beat")` (C4).
- Satisfies IN4, IN5, IN6, C2, C4, EX1, EX3. ChordSheetR/SVG untouched (boards page-mounted). Desktop builds clean; JS dogfood via `CHORDFLOW_DEVTOOLS` is the manual check (EX5 — no JS harness).

## Step 3 — Update chordflow-architecture-reference.md so §6 records chordSchedule on chordSheetResult and the now-next seam extending to the Chord Sheets surface; no domain-model-ref change (no new named Core type).

**Architecture-ref sync** — `loom/refs/chordflow-architecture-reference.md` (3 `loom_patch_doc` edits):

1. ChordSheetR paragraph (§5): `chordSheetResult` shape updated `{sheet}` → `{sheet, cellSchedule, chordSchedule, tex}`; corrected 'comping resolved only for diagram/both' → **unconditionally** (the adornment gates only per-cell sheet diagrams); described `chordSchedule` as the now/next feed surfaced from the one `AlphaTexRenderer.Render` pass and the `ChordFlowNowNext` mount on the Chord Sheets page.
2. FretR paragraph (§5): `ChordFlowNowNext` now noted as mounting on **both** Practice and Chord Sheets, fed each surface's schedule (same `ChordChange[]`), each converting 1-based `onBeat` → 0-based.
3. §6 data-flow: added the Chord Sheets branch sharing the exact `RenderResult.Schedule` as `chordSheetResult.chordSchedule` (no separate `ChordScheduleBuilder`).

**No `chordflow-domain-model-reference.md` change** — the producer is the existing renderer; `ChordChange`/`RenderResult` are already documented and no new named type landed (the planning-session resolution of the design's 'if it lands as a named Core type' clause: it does not). Satisfies IN7.
