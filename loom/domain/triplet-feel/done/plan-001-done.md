---
type: done
id: pl_01KVR91RMYBGE0Y1E0TVAEC6HK-done
title: Done — Triplet Feel — \tf delegation, TripletFeel rename, control in ChordFlowScore
status: done
created: 2026-06-22
version: 5
tags: []
parent_id: pl_01KVR91RMYBGE0Y1E0TVAEC6HK
requires_load: []
---
# Done — Triplet Feel — \tf delegation, TripletFeel rename, control in ChordFlowScore

## Step 1 — Replace the Feel enum with TripletFeel (alphaTab members) and propagate the rename through Exercise, ExerciseEntity, GenerateRequest, the bridge parse, and FeelTransform

Renamed `Feel`→`TripletFeel` (new file `Music/Rhythm/TripletFeel.cs`, deleted `Feel.cs`). Members: `None, Triplet8th, Triplet16th` (wired) + `Dotted8th, Dotted16th, Scottish8th, Scottish16th` (reserved). Propagated the type + property/param rename across: `Exercise.TripletFeel` (default `None`), `ExerciseEntity.TripletFeel`, `ChordFlowDbContext` (`HasConversion<string>`), `GenerateExercise` (3 signatures), `GenerateRequest.TripletFeel` + the `InboundEnvelope.TripletFeel` field + `ParseEnum(..., TripletFeel.None)`, `ExerciseRendering`, `ExerciseLibrary` (entity map + `ExerciseSummary.TripletFeel`), `IScoreRenderer`/`AlphaTexRenderer`/`SwappableRenderer` signatures, `Program.cs` boot + generate. `FeelTransform` kept (IN4) — its `OffBeatRatio` switch updated to the new members (None=1/2, Triplet8th/16th=2/3, Dotted=3/4, Scottish=1/3) so it still compiles for the future export seam.

## Step 2 — AlphaTexRenderer emits a single whole-song \tf on the first bar of each track (only when ≠ None) and stops calling FeelTransform

`AlphaTexRenderer`: removed `WarpBars`/`FeelTransform.Apply` from both bar builders (replaced with `PatternEventBars` = straight `b.Events`). Added `PrependTripletFeel(bars, feel)` + `TripletFeelToken(feel)`; `Render` prepends one `\tf <ident> ` to the first bar of the comping track and (two-track) the lead track, guarded by `feel != None` — so a `None` song emits no `\tf` and stays byte-identical to the old straight output. `\tf` lands ahead of the bar's `:N`/beats and composes with a leading `\ac` pickup prefix.

## Step 4 — Move the feel select out of the page builder into the ChordFlowScore transport; expose getTripletFeel() and re-render on change via onNeedsRerender

Control moved into `ChordFlowScore`: new `tripletFeel: true` create option renders a "Feel" `<select>` (TRIPLET_FEELS = None/Triplet8th/Triplet16th, values = C# enum names) in the transport; `getTripletFeel()` exposes the value, `setTripletFeel()` updates it and fires `onNeedsRerender` (content-kind → cheap re-emit, harmony unchanged). Kept OUT of `getRenderOptions()` (C3) — it rides as a first-class request field. `app.js`: removed the builder feel picker + `FEELS`; `selections()` reads `view.getTripletFeel()`; the `onNeedsRerender` handler merges `tripletFeel: view.getTripletFeel()` into the replayed request; enabled `tripletFeel: true` on create. `index.html`: removed the `feel` label+select from #builder.

## Step 7 — Confirm \tf spelling against the bundled alphaTab, build the solution green, then visually verify swung notation + re-render-on-flip in the app

Build green; full suite **634/634 pass**. IN8 confirmed against the bundled `alphaTab.min.js` (it recognizes lowercase `triplet8th`/`triplet16th`/`notripletfeel`) — emitting the readable ident, no numeric fallback needed. **Visual dogfood (IN11/C5) still PENDING** — needs the running WinForms app to confirm swung *notation* (not straight 8ths) + swung playback, and that flipping the Feel select re-renders without a full regenerate. Left for Rafa to verify in-app before this step closes.

## Step 8 — Enable the feel control in the Content preview and thread tripletFeel through entityPreview → ContentCrudHandler.Preview → renderer

Reported by Rafa: the Feel control was missing in the Content view (progression/song/rhythm) — root cause: `content-crud.js` never opted into the new `tripletFeel` component option, and the `entityPreview` path didn't thread feel. Fixed end-to-end: `content-crud.js` passes `tripletFeel: true` + sends `tripletFeel: scoreView.getTripletFeel()` on `entityPreview` (existing `onNeedsRerender → requestPreview()` re-previews on change); `WebMessageRouter.EntityPreviewRequested` gained a `TripletFeel` arg (parsed via shared `ParseEnum`); `Program.cs` forwards it to `ContentCrudHandler.Preview` → `ProgressionPreview`/`RhythmPreview` (on the preview `Exercise`) + `SongPreview` (`Render` arg). `RenderOptions` stays view-only (C3). Two existing event-subscription test lambdas updated to the 4-arg signature + added `EntityPreview_CarriesTripletFeel`. Build green, **635/635 tests pass**. In-app visual confirm rides with the step-7 dogfood pass (Rafa to eyeball the Feel select now showing in Content).
