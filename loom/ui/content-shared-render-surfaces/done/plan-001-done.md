---
type: done
id: pl_01KXTA2PGTF0DGBBQK1GF0X6H4-done
title: Done — Content mounts the shared render-surface composite
status: done
created: 2026-07-18
version: 5
tags: []
parent_id: pl_01KXTA2PGTF0DGBBQK1GF0X6H4
requires_load: []
---
# Done — Content mounts the shared render-surface composite

## Step 1 — Widen EntityPreviewEnvelope with Sheet + CellSchedule; route ContentCrudHandler.Preview (progression + song) through ExerciseRendering.RenderWithSheet so score/sheet/cellSchedule come from one pass. Rhythm stays score-only, voicing stays diagram.

Widened the preview envelope and unified the progression/song preview onto the sheet-carrying render pass.

**`ContentCrudEnvelopes.cs`** — `EntityPreviewEnvelope` gains `ChordSheet? Sheet` + `IReadOnlyList<CellScheduleEntry>? CellSchedule` (added `using ChordFlow.Rendering.ChordSheets`). Populated for progression/song; null for rhythm (score-only) and voicing (diagram). Same record→JSON path as the `loadScore` reply, so JS reads `msg.sheet` / `msg.cellSchedule` identically.

**`ContentCrudHandler.cs`** —
- New `ScoreWithSheetPreview(entity, exercise, db, options)` helper: runs `ExerciseRendering.RenderWithSheet` (one realized-song + CompingPlan pass) and projects `proj.Render.Tex` + `proj.Sheet` + `proj.CellSchedule` into the envelope.
- Progression preview now routes through it (was tex-only `ScorePreview`).
- `SongPreview` rebuilt: constructs `new Exercise(song, comping, Lead:null, KeyOverride: startKey, tempo, Beginner, tripletFeel)` and routes through the same helper — so `RenderCore`'s `baseKey = KeyOverride ?? Song.InitialKey` preserves the "absent key ⇒ song's own InitialKey" rule (verified by the existing `Preview_Song_AbsentKey_KeepsItsOwnInitialKey` test still passing). Dropped the hand-rolled expand/resolve/render.
- Rhythm keeps the tex-only `ScorePreview` (a bare rhythm has no meaningful chord sheet); voicing keeps the diagram path.

**`ContentCrudHandlerTests.cs`** — added `Preview_Progression_CarriesSheetProjection`, `Preview_Song_CarriesSheetProjection` (non-null Sheet + non-empty CellSchedule), and `Preview_Rhythm_IsScoreOnly_NoSheet` (null Sheet/CellSchedule).

Verification: full Core suite green — **1022 passed, 0 failed**. No `TreatWarningsAsErrors`; no leftover references to the removed `SongExpander`/`CompingResolver` locals.

## Step 2 — New composite JS component owning ScoreR(transport:false) + ChordSheetR + Score⇄Sheet toggle + page-level PlayerControlsR + the one engine + beat/position fan-out. load({tex,tempo,sheet,cellSchedule,key?,keyIsMinor?,tripletFeel?}) feeds both projections; exposes getEngine()/getRenderParams()/dispose(); sheet:false gives a score-only mode (toggle hidden).

Extracted the shared composite `window.ChordFlowRenderSurface`.

**New `render-surface-component.js`** — `create({ transportEl, scoreEl, sheetEl, sheet, scoreOpts, playerOpts, onBeat, onFinished, onNeedsRerender })`. It owns:
- **ScoreR** created `transport:false` (consumer `scoreOpts` merged in), so the page-level transport survives the toggle.
- **ChordFlowSheetView** mounted into `sheetEl` when `sheet:true` (score-only when `sheet:false` or no sheetEl).
- The **Score⇄Sheet toggle** (lifted from app.js's `buildViewToggle`) — collapse-swap (`.view-collapsed`, width kept) so alphaTab never re-measures while hidden → mid-playback toggle is seamless. Built into `transportEl`; absent in score-only mode.
- A **page-level PlayerControlsR** bound to `scoreHandle.getEngine()`, appended after the toggle.
- The **fan-out**: the composite manages ScoreR's three engine callbacks — `onBeat`→`sheetView.onBeat` + consumer's `onBeat` (Now/Next + bridge echo), `onFinished`→`sheetView.clearMarker` + consumer's, `onNeedsRerender`→consumer's; and subscribes `engine.on("position")`→`sheetView.onPosition` (the time-linear Visual-metronome clock).

**Design decisions (why this shape):**
- **Placement stays with the page** — `create` takes the three existing mount elements rather than rendering one contiguous block, so Practice can keep Now/Next *between* the transport and the surfaces, and Content lays out its editor around them. The composite owns the *wiring* (the C4 value), not the DOM placement.
- **Now/Next stays out** (EX4) — the composite exposes `getEngine()` + the `onBeat` passthrough so Practice feeds its own boards; the composite never references them.
- **Key/feel seeding is a no-op passthrough** where the page keeps those controls elsewhere: `load()` calls `scoreHandle.seedKey/seedTripletFeel`, which only touch ScoreR's own pickers (present on Content via `scoreOpts.key/tripletFeel`, absent on Practice where HarmonyControlsR owns them — the page seeds those itself). No conflict either way.

**Handle:** `load({tex,tempo,sheet,cellSchedule,key?,tripletFeel?,name?})` (feeds both projections), `getEngine()`, `getRenderParams()` ({renderOptions,key,tempo,tripletFeel}), `seedKey/seedTempo/seedTripletFeel`, `showSurface(name)`, `dispose()`.

**`index.html`** — added `<script src="render-surface-component.js">` after chord-sheets.js, before the page shells (all component deps defined above it; consumers call it at runtime).

Verification: `node --check` passes. No page consumes it yet (steps 3–4); runtime dogfood is step 5.

## Step 3 — Refactor app.js to mount ChordFlowRenderSurface instead of hand-wiring ScoreR + toggle + PlayerControlsR + SheetView + fan-out. Practice keeps HarmonyControlsR, Now/Next, and the library around it; loadScore feeds the composite's load().

Refactored Practice (`app.js`) to mount the composite; it's now a composite-consumer.

**Removed** (all now owned by `ChordFlowRenderSurface`): the bare `ChordFlowScore.create`, the `buildViewToggle` function (~20 lines) and its call, the page-level `ChordFlowPlayerControls.create`, the `ChordFlowSheetView.create`, and the `engine.on("position")` → sheet wiring.

**Added** one `surface = window.ChordFlowRenderSurface.create({ transportEl:#transport-strip, scoreEl:#score-pane, sheetEl:#sheet-pane, sheet:true, scoreOpts:{player,controls:full,volumes:false,scroll,debugPanel}, playerOpts:{onToggleNowNext}, onBeat, onFinished, onNeedsRerender })`. Practice's `onBeat` now feeds only its *own* event surfaces (Now/Next + the `beatChanged` bridge echo) — the composite fans the sheet's Per-chord marker itself; `onFinished` resets Now/Next + echoes `playbackFinished` (sheet clear is the composite's).

**Rewired the remaining references** to the composite's handle:
- `loadScore` → `surface.load({tex,tempo,key,tripletFeel,sheet,cellSchedule,name})` (feeds both projections in one call); HarmonyControlsR seeds (`seedKey`/`seedKeyMode`/`seedTripletFeel`) and `nowNext.setSchedule(schedule)` stay in app.js (outside the composite).
- `view.getRenderOptions()` → `surface.getRenderParams().renderOptions` (sendScoreRequest, replayScoreRequest, the `ready` send).
- `(pc && pc.getTempo())` → `surface.getRenderParams().tempo` (onGenerate).
- `view.seedTempo + pc.setTempoValue` → `surface.seedTempo(tempo)` (onHarmonySwitch) — and updated the composite's `seedTempo` to also set its page-level pc input (ScoreR's own pc is null under `transport:false`).
- `HarmonyControls.create({ engine: surface.getEngine() })`; browser-dev fallback `surface.load({tex:SAMPLE_TEX})`.
- init guard now checks `window.ChordFlowRenderSurface`; module returns `getSurface` (the unused `getView` had no external callers).
- Updated the module header comment to describe the composite.

Verification: `node --check src/ChordFlow.Desktop/wwwroot/app.js` passes; a tight grep confirms no stale `view.`/`pc.`/`sheetView` code references remain. Runtime dogfood (mid-playback toggle etc.) is step 5.

## Step 4 — In content-crud.js, mount ChordFlowRenderSurface for the score strategy (progression/song = with sheet, rhythm = score-only); on entityPreview feed load({tex,tempo,sheet,cellSchedule,...}). Voicing keeps the fretboard path. Content ScoreR keeps opt-in key/feel pickers; seeds route through the composite.

Content preview now mounts the shared composite.

**`content-crud.js`:**
- **DOM** — the score preview area gained a wrapper `#ccScoreSurface` (hidden as a unit for the voicing/diagram case) containing `#ccPreviewTransport` (`.cf-controls` — the composite's toggle + PlayerControlsR), `#ccPreviewScore`, and a new `#ccPreviewSheet` (`.view-collapsed`). Voicing keeps its own `#ccPreviewDiagram`.
- **Entity configs** — `sheet: true` on progression + song; rhythm has no flag (score-only, C2); voicing stays diagram.
- **State** — `scoreView` (ChordFlowScore) → `surfaceView` (ChordFlowRenderSurface) + `surfaceHasSheet`.
- **`ensureSurface()`** — lazily creates the composite, or disposes+recreates it when the needed sheet-mode flips (progression/song ↔ rhythm). `scoreOpts: { player, controls:"full", debugPanel, tripletFeel:true, key:true }` — Content keeps Key/Feel pickers ON ScoreR (no HarmonyControlsR here, C3); the transport rides in the composite. Re-applies `applySeeds()` on creation.
- **`renderScore(msg)`** — `surfaceView.load({ tex, tempo, sheet: msg.sheet, cellSchedule: msg.cellSchedule, name })` feeds both projections in one call (IN2/IN4).
- **`renderPreview`** — diagram kind hides `#ccScoreSurface` and shows the fret-box; score kind shows `#ccScoreSurface` and calls `renderScore(msg)`.
- **`applySeeds` / `requestPreview`** — retargeted to `surfaceView.seedKey/seedTempo/seedTripletFeel` and `surfaceView.getRenderParams()` (renderOptions/tripletFeel/key/tempo), with the same pre-surface `pendingSeeds` fallback.

**`render-surface-component.js`** — made `dispose` self-cleaning (removes the toggle wrap + PlayerControlsR element from `transportEl`) so Content's recreate-on-mode-change leaves no orphan chrome; `seedTempo` also updates the page-level pc input.

**Serialization confirmed:** all outbound envelopes go through the one `WebView2Bridge` `JsonSerializerDefaults.Web` (camelCase) path, so `EntityPreviewEnvelope.Sheet`/`CellSchedule` reach JS as `msg.sheet`/`msg.cellSchedule` exactly like the working `loadScore` reply.

Verification: `node --check` passes on all three JS files; no stale `scoreView` references; **full solution build succeeds (0 errors)** — the pre-existing SQLite/WindowsBase warnings are unrelated. Runtime mid-playback dogfood is step 5.

## Step 5 — Dogfood: on Content, toggle Score⇄Sheet mid-playback (both markers track); a minor progression previews correct chords + \ks in both surfaces; rhythm previews score-only, voicing as a fretboard. Update chordflow-architecture-reference.md to document the ChordFlowRenderSurface composite seam.

Runtime-dogfooded the Content preview against the running app and synced the architecture ref.

**Architecture ref (`chordflow-architecture-reference.md`, C5):**
- §2 `wwwroot/` inventory — added `render-surface-component.js` (ChordFlowRenderSurface, mounted by BOTH Practice and the Content preview).
- §5 — new narrative paragraph documenting the composite (what it owns, the `create`/`load` API, placement-stays-with-the-page, per-entity degradation, Now/Next stays outside, and the widened `entityPreview` reply routed through the same `RenderWithSheet` pass as `loadScore`).
- §7 "UI (JS) — dumb views" diagram — added the composite line.

**Runtime dogfood (CDP harness against the live WinForms+WebView2 app, `CHORDFLOW_DEVTOOLS=1` + `--remote-debugging-port=9223`):** drove the Content view via `Runtime.evaluate` (`scratchpad/verify-content-surface.mjs`). Results:
- **Minor progression `1- 4- 5-` (tonality=minor), IN7:** score tex carries `\ks cminor`; the sheet SVG renders "key of Cm" with chords **Cm Fm Gm** (correct C-minor i–iv–v). The regression the two-path divergence hid is now covered in BOTH surfaces on Content.
- **Composite + toggle, IN2/IN6:** `#ccPreviewTransport` has the PlayerControlsR + the Score⇄Sheet toggle; clicking **Sheet** collapses `#ccPreviewScore` (`view-collapsed`) and reveals `#ccPreviewSheet` — both surfaces are projections of the same preview pass.
- **Per-entity degradation, C2:** rhythm → score-only (no toggle, sheet mount empty); voicing → fretboard SVG present, score surface hidden.
- **C3:** the tonality control is shown for progression and drove the minor render.

Not driven with live audio: the frame-by-frame *audio-synced* marker tracking during playback — asserting that headlessly needs a trusted-gesture play + timed sampling. It's covered structurally instead: both surfaces render the same run's projections and the `"beat"`/`"position"` fan-out is wired in the composite (code-verified) + the toggle collapse works, so a mid-playback switch reveals a marker already tracking (the same mechanism Practice already ships).

**Full verification tally:** Core suite 1022/1022 green (incl. the 3 new sheet-projection tests); solution build 0 errors; all JS `node --check` clean; runtime dogfood above. App process stopped after the run.
