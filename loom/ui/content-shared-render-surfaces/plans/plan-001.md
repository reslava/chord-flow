---
type: plan
id: pl_01KXTA2PGTF0DGBBQK1GF0X6H4
title: Content mounts the shared render-surface composite
status: done
created: 2026-07-18
updated: 2026-07-18
version: 1
design_version: 4
req_version: 1
tags: []
parent_id: de_01KXT9SEKXG87T69JPW7AQ018T
requires_load: []
target_version: 0.1.0
actual_release: 0.15.0
steps:
  - id: sheet-projection-on-the-preview-envelope
    order: 1
    status: done
    description: Widen EntityPreviewEnvelope with Sheet + CellSchedule; route ContentCrudHandler.Preview (progression + song) through ExerciseRendering.RenderWithSheet so score/sheet/cellSchedule come from one pass. Rhythm stays score-only, voicing stays diagram.
    files_touched: [src/ChordFlow.Core/Features/ContentCrud/ContentCrudEnvelopes.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs, tests/ChordFlow.Core.Tests/ContentCrudHandlerTests.cs]
    blocked_by: []
    satisfies: [IN4, IN5, C1]
  - id: extract-chordflowrendersurface-composite
    order: 2
    status: done
    description: "New composite JS component owning ScoreR(transport:false) + ChordSheetR + Score⇄Sheet toggle + page-level PlayerControlsR + the one engine + beat/position fan-out. load({tex,tempo,sheet,cellSchedule,key?,keyIsMinor?,tripletFeel?}) feeds both projections; exposes getEngine()/getRenderParams()/dispose(); sheet:false gives a score-only mode (toggle hidden)."
    files_touched: [src/ChordFlow.Desktop/wwwroot/render-surface-component.js, src/ChordFlow.Desktop/wwwroot/index.html]
    blocked_by: []
    satisfies: [IN1, C1, C4]
  - id: practice-mounts-the-composite
    order: 3
    status: done
    description: Refactor app.js to mount ChordFlowRenderSurface instead of hand-wiring ScoreR + toggle + PlayerControlsR + SheetView + fan-out. Practice keeps HarmonyControlsR, Now/Next, and the library around it; loadScore feeds the composite's load().
    files_touched: [src/ChordFlow.Desktop/wwwroot/app.js]
    blocked_by: [extract-chordflowrendersurface-composite]
    satisfies: [IN3, C4]
  - id: content-mounts-the-composite
    order: 4
    status: done
    description: In content-crud.js, mount ChordFlowRenderSurface for the score strategy (progression/song = with sheet, rhythm = score-only); on entityPreview feed load({tex,tempo,sheet,cellSchedule,...}). Voicing keeps the fretboard path. Content ScoreR keeps opt-in key/feel pickers; seeds route through the composite.
    files_touched: [src/ChordFlow.Desktop/wwwroot/content-crud.js]
    blocked_by: [sheet-projection-on-the-preview-envelope, extract-chordflowrendersurface-composite]
    satisfies: [IN2, IN6, C2, C3]
  - id: verify-both-surfaces-sync-architecture-ref
    order: 5
    status: done
    description: "Dogfood: on Content, toggle Score⇄Sheet mid-playback (both markers track); a minor progression previews correct chords + \\ks in both surfaces; rhythm previews score-only, voicing as a fretboard. Update chordflow-architecture-reference.md to document the ChordFlowRenderSurface composite seam."
    files_touched: [loom/refs/chordflow-architecture-reference.md]
    blocked_by: [practice-mounts-the-composite, content-mounts-the-composite]
    satisfies: [IN7, C5]
---
# Content mounts the shared render-surface composite

## Goal

Converge the Content preview onto the same render surface as Practice by extracting a shared composite (ChordFlowRenderSurface = ScoreR + ChordSheetR + Score⇄Sheet toggle + page-level PlayerControlsR + the one engine + beat/position fan-out), which both app.js and content-crud.js mount. The Core work makes Content's Sheet real: route the progression/song preview through the existing ExerciseRendering.RenderWithSheet and widen EntityPreviewEnvelope to carry the sheet projection, so the score and sheet derive from one realized-song pass and cannot drift (killing the class of divergence that hid the minor-preview bug). Consolidation only — no new render capability, no page merge. Order: Core/bridge first (independently testable), then the composite, then re-point both pages, then verify + sync the architecture ref.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Widen EntityPreviewEnvelope with Sheet + CellSchedule; route ContentCrudHandler.Preview (progression + song) through ExerciseRendering.RenderWithSheet so score/sheet/cellSchedule come from one pass. Rhythm stays score-only, voicing stays diagram. | src/ChordFlow.Core/Features/ContentCrud/ContentCrudEnvelopes.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs, tests/ChordFlow.Core.Tests/ContentCrudHandlerTests.cs | — | IN4, IN5, C1 |
| ✅ | 2 | New composite JS component owning ScoreR(transport:false) + ChordSheetR + Score⇄Sheet toggle + page-level PlayerControlsR + the one engine + beat/position fan-out. load({tex,tempo,sheet,cellSchedule,key?,keyIsMinor?,tripletFeel?}) feeds both projections; exposes getEngine()/getRenderParams()/dispose(); sheet:false gives a score-only mode (toggle hidden). | src/ChordFlow.Desktop/wwwroot/render-surface-component.js, src/ChordFlow.Desktop/wwwroot/index.html | — | IN1, C1, C4 |
| ✅ | 3 | Refactor app.js to mount ChordFlowRenderSurface instead of hand-wiring ScoreR + toggle + PlayerControlsR + SheetView + fan-out. Practice keeps HarmonyControlsR, Now/Next, and the library around it; loadScore feeds the composite's load(). | src/ChordFlow.Desktop/wwwroot/app.js | extract-chordflowrendersurface-composite | IN3, C4 |
| ✅ | 4 | In content-crud.js, mount ChordFlowRenderSurface for the score strategy (progression/song = with sheet, rhythm = score-only); on entityPreview feed load({tex,tempo,sheet,cellSchedule,...}). Voicing keeps the fretboard path. Content ScoreR keeps opt-in key/feel pickers; seeds route through the composite. | src/ChordFlow.Desktop/wwwroot/content-crud.js | sheet-projection-on-the-preview-envelope, extract-chordflowrendersurface-composite | IN2, IN6, C2, C3 |
| ✅ | 5 | Dogfood: on Content, toggle Score⇄Sheet mid-playback (both markers track); a minor progression previews correct chords + \ks in both surfaces; rhythm previews score-only, voicing as a fretboard. Update chordflow-architecture-reference.md to document the ChordFlowRenderSurface composite seam. | loom/refs/chordflow-architecture-reference.md | practice-mounts-the-composite, content-mounts-the-composite | IN7, C5 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:sheet-projection-on-the-preview-envelope -->
### Step 1 — Sheet projection on the preview envelope

Add `ChordSheet? Sheet` + `IReadOnlyList<CellScheduleEntry>? CellSchedule` to `EntityPreviewEnvelope` (null for rhythm/voicing). In `Preview`, replace the progression/song `RenderToTex`/bespoke `SongPreview` render with `ExerciseRendering.RenderWithSheet` and project its `ExerciseProjections` into the envelope (tex from `Render.Tex`, plus `Sheet` + `CellSchedule`). Keep the FormatException→entityParseError contract. Tests: a progression/song preview now carries a non-null Sheet + CellSchedule; rhythm/voicing carry null. Outbound serialization mirrors the loadScore reply (same record→JSON path).

<!-- step:extract-chordflowrendersurface-composite -->
### Step 2 — Extract ChordFlowRenderSurface composite

Lift the composition currently hand-wired in app.js (buildViewToggle collapse-swap; ScoreR created transport:false; PlayerControlsR bound to getEngine(); SheetView mount; engine `position`→sheet.onPosition + onBeat→sheet.onBeat fan-out; load feeding view.load + sheetView.render + sheetView.setSchedule) into `window.ChordFlowRenderSurface`. Pass-through `scoreOpts` (so a consumer sets key/tripletFeel/volumes per page) and optional onBeat/onFinished/onNeedsRerender for the consumer's own surfaces (Now/Next). Dumb view: no theory, all inputs from Core. Add the script tag to index.html before app.js/content-crud.js.

<!-- step:practice-mounts-the-composite -->
### Step 3 — Practice mounts the composite

Replace the ScoreR create + buildViewToggle + PlayerControlsR + SheetView + position/beat wiring with one `ChordFlowRenderSurface.create($('render-surface-mount'), { scoreOpts:{ transport:false, volumes:false, ... }, sheet:true, onBeat, onFinished, onNeedsRerender })`. HarmonyControlsR + volume binds use `handle.getEngine()`; Now/Next is fed from the same engine's beats via the onBeat passthrough. loadScore → `handle.load({tex,tempo,sheet,cellSchedule,key,keyIsMinor,tripletFeel})`. Behavior must be unchanged (mid-playback toggle, both markers, dev SAMPLE_TEX fallback).

<!-- step:content-mounts-the-composite -->
### Step 4 — Content mounts the composite

Replace `renderScore`'s bare `ChordFlowScore.create` with `ChordFlowRenderSurface.create` using `scoreOpts:{ transport:false, key:true, tripletFeel:true, debugPanel:true }` and `sheet: current.key !== 'rhythm'`. `renderPreview` (score kind) calls `handle.load({ tex, tempo, sheet: msg.sheet, cellSchedule: msg.cellSchedule, ... })`; requestPreview reads render params from `handle.getRenderParams()` (replacing the direct scoreView.getKey/getTempo/getTripletFeel + pendingSeeds application). Comping picker + tonality control unchanged; the voicing diagram strategy is untouched (C2).

<!-- step:verify-both-surfaces-sync-architecture-ref -->
### Step 5 — Verify both surfaces + sync architecture ref

Run the build + Core tests. Drive the app (CHORDFLOW_DEVTOOLS + CDP if needed) to confirm the mid-playback toggle and minor-key correctness on Content, and per-entity degradation. Update the architecture ref's UI (JS) component list + the §5 render-component narrative to introduce `render-surface-component.js` (ChordFlowRenderSurface) as the shared composite both Practice and Content mount, and note the widened entityPreview reply carrying the sheet projection.
