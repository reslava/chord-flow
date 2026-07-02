---
type: plan
id: pl_01KVR91RMYBGE0Y1E0TVAEC6HK
title: Triplet Feel — \tf delegation, TripletFeel rename, control in ChordFlowScore
status: done
created: 2026-06-22
updated: 2026-06-22
version: 1
design_version: 2
req_version: 1
tags: []
parent_id: de_01KVR89QNHC6NE2XHTJ6EM9MDQ
requires_load: []
target_version: 0.1.0
actual_release: 0.10.0
steps:
  - id: tripletfeel-enum-propagate-the-c-rename
    order: 1
    status: done
    description: Replace the Feel enum with TripletFeel (alphaTab members) and propagate the rename through Exercise, ExerciseEntity, GenerateRequest, the bridge parse, and FeelTransform
    files_touched: [src/ChordFlow.Core/Music/Rhythm/Feel.cs, src/ChordFlow.Core/Exercises/Exercise.cs, src/ChordFlow.Core/Persistence/Entities/ExerciseEntity.cs, src/ChordFlow.Core/Features/GenerateExercise/GenerateExercise.cs, src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Core/Music/Rhythm/FeelTransform.cs]
    blocked_by: []
    satisfies: [IN1, IN4, IN5, C4]
  - id: renderer-emits-tf-drops-the-warp
    order: 2
    status: done
    description: AlphaTexRenderer emits a single whole-song \tf on the first bar of each track (only when ≠ None) and stops calling FeelTransform
    files_touched: [src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs]
    blocked_by: [1]
    satisfies: [IN2, IN3, C1, C2]
  - id: ef-migration-for-the-renamed-column
    order: 3
    status: done
    description: Add the migration renaming the by-name Feel column to TripletFeel and remapping legacy values; update the model snapshot
    files_touched: [src/ChordFlow.Core/Persistence/ChordFlowDbContext.cs, src/ChordFlow.Core/Persistence/Migrations/]
    blocked_by: [1]
    satisfies: [IN5]
  - id: move-the-control-into-chordflowscore
    order: 4
    status: done
    description: Move the feel select out of the page builder into the ChordFlowScore transport; expose getTripletFeel() and re-render on change via onNeedsRerender
    files_touched: [src/ChordFlow.Desktop/wwwroot/index.html, src/ChordFlow.Desktop/wwwroot/score-render-component.js, src/ChordFlow.Desktop/wwwroot/app.js]
    blocked_by: [1]
    satisfies: [IN6, IN7, C3]
  - id: tests-tf-assertions-bridge-parse
    order: 5
    status: done
    description: Replace FeelTransform-warp render assertions with \tf-line assertions (and None emits none); add a bridge parse test for the new members
    files_touched: [tests/ChordFlow.Core.Tests/RhythmOverlayTests.cs, tests/ChordFlow.Core.Tests/RenderTestHelpers.cs, tests/ChordFlow.Core.Tests/WebMessageRouterContentTests.cs]
    blocked_by: [2]
    satisfies: [IN10, C6]
  - id: reference-sync-same-unit-of-work
    order: 6
    status: done
    description: Add \tf to the alphaTex ref; update the domain-model and DSL refs for the rename and the no-grammar rule
    files_touched: [loom/refs/alphatex-syntax-reference.md, loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-dsl-reference.md]
    blocked_by: [1, 2]
    satisfies: [IN9]
  - id: build-green-visual-dogfood-verify
    order: 7
    status: done
    description: Confirm \tf spelling against the bundled alphaTab, build the solution green, then visually verify swung notation + re-render-on-flip in the app
    files_touched: []
    blocked_by: [2, 3, 4, 5, 6]
    satisfies: [IN8, IN11, C5, C6]
  - id: feel-control-in-the-content-preview
    order: 8
    status: done
    description: Enable the feel control in the Content preview and thread tripletFeel through entityPreview → ContentCrudHandler.Preview → renderer
    files_touched: [src/ChordFlow.Desktop/wwwroot/content-crud.js, src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Desktop/Program.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs, tests/ChordFlow.Core.Tests/WebMessageRouterContentTests.cs]
    blocked_by: []
    satisfies: [IN12, C3, C6]
---
# Triplet Feel — \tf delegation, TripletFeel rename, control in ChordFlowScore

## Goal

Implement the locked req (rq_01KVR8ADB19EGZ0JY0B4AZ8S26) exactly: replace our self-computed playback swing with alphaTab's native \tf directive for a whole-song, play-time feel, rename the Feel model to alphaTab's TripletFeel vocabulary, and move the control into the shared ChordFlowScore component — all while keeping C4 (no feel token in any grammar; feel chosen at play time). Step 1 introduces the TripletFeel enum (wire None/Triplet8th/Triplet16th; reserve Dotted8th/Dotted16th/Scottish) and propagates the rename through the C# stack, including updating FeelTransform to the new members so it still compiles for the future export seam though the renderer no longer calls it (IN1, IN4, IN5, C4). Step 2 makes AlphaTexRenderer emit a single whole-song \tf on the first bar of each track (only when value ≠ None) and stops calling FeelTransform (IN2, IN3, C1, C2). Step 3 adds the EF migration renaming the by-name column Feel→TripletFeel and remapping legacy values (IN5). Step 4 moves the control out of the page builder into ChordFlowScore as tempo's twin — getTripletFeel() plus a content-kind re-render on change via the existing onNeedsRerender seam, kept out of the view-only RenderOptions (IN6, IN7, C3). Step 5 updates tests to assert the \tf line (and that None emits none) and the new bridge parse (IN10, C6). Step 6 syncs the three refs in the same unit of work (IN9). Step 7 confirms the \tf ident spelling against the bundled alphaTab, builds green, and visually dogfoods swung notation + re-render-on-flip (IN8, IN11, C5, C6). Per-section feel, the Dotted/Scottish feels, {tu}/:3 behavior, removing FeelTransform, and the separate {tu}-not-rendering bug are out of scope. A default feel persisted on Song/Progression/Rhythm content is deferred to a follow-up thread (it reopens C4).

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Replace the Feel enum with TripletFeel (alphaTab members) and propagate the rename through Exercise, ExerciseEntity, GenerateRequest, the bridge parse, and FeelTransform | src/ChordFlow.Core/Music/Rhythm/Feel.cs, src/ChordFlow.Core/Exercises/Exercise.cs, src/ChordFlow.Core/Persistence/Entities/ExerciseEntity.cs, src/ChordFlow.Core/Features/GenerateExercise/GenerateExercise.cs, src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Core/Music/Rhythm/FeelTransform.cs | — | IN1, IN4, IN5, C4 |
| ✅ | 2 | AlphaTexRenderer emits a single whole-song \tf on the first bar of each track (only when ≠ None) and stops calling FeelTransform | src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs | 1 | IN2, IN3, C1, C2 |
| ✅ | 3 | Add the migration renaming the by-name Feel column to TripletFeel and remapping legacy values; update the model snapshot | src/ChordFlow.Core/Persistence/ChordFlowDbContext.cs, src/ChordFlow.Core/Persistence/Migrations/ | 1 | IN5 |
| ✅ | 4 | Move the feel select out of the page builder into the ChordFlowScore transport; expose getTripletFeel() and re-render on change via onNeedsRerender | src/ChordFlow.Desktop/wwwroot/index.html, src/ChordFlow.Desktop/wwwroot/score-render-component.js, src/ChordFlow.Desktop/wwwroot/app.js | 1 | IN6, IN7, C3 |
| ✅ | 5 | Replace FeelTransform-warp render assertions with \tf-line assertions (and None emits none); add a bridge parse test for the new members | tests/ChordFlow.Core.Tests/RhythmOverlayTests.cs, tests/ChordFlow.Core.Tests/RenderTestHelpers.cs, tests/ChordFlow.Core.Tests/WebMessageRouterContentTests.cs | 2 | IN10, C6 |
| ✅ | 6 | Add \tf to the alphaTex ref; update the domain-model and DSL refs for the rename and the no-grammar rule | loom/refs/alphatex-syntax-reference.md, loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-dsl-reference.md | 1, 2 | IN9 |
| ✅ | 7 | Confirm \tf spelling against the bundled alphaTab, build the solution green, then visually verify swung notation + re-render-on-flip in the app | — | 2, 3, 4, 5, 6 | IN8, IN11, C5, C6 |
| ✅ | 8 | Enable the feel control in the Content preview and thread tripletFeel through entityPreview → ContentCrudHandler.Preview → renderer | src/ChordFlow.Desktop/wwwroot/content-crud.js, src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Desktop/Program.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs, tests/ChordFlow.Core.Tests/WebMessageRouterContentTests.cs | — | IN12, C3, C6 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:tripletfeel-enum-propagate-the-c-rename -->
### Step 1 — TripletFeel enum + propagate the C# rename

Rename Feel.cs → TripletFeel.cs. Members: `None, Triplet8th, Triplet16th` (wired/offered) + `Dotted8th, Dotted16th, Scottish8th, Scottish16th` (defined, reserved — EX1). Update the doc comment (still C4: play-time, never baked into a pattern). Propagate: `Exercise.TripletFeel` (default `None`), `ExerciseEntity.TripletFeel`, `GenerateRequest.TripletFeel`, `WebMessageRouter.ParseEnum(envelope..., TripletFeel.None)`. Update `FeelTransform.OffBeatRatio` to the new members (`None` = 1/2 identity, `Triplet8th`/`Triplet16th` = 2/3) so the class still compiles for the future export seam (IN4) — it is no longer called by the renderer (that happens in step 2). The persisted param stays (C4-recommended decision = keep persisted).

<!-- step:renderer-emits-tf-drops-the-warp -->
### Step 2 — Renderer emits \tf, drops the warp

Remove the `WarpBars`/`FeelTransform.Apply` calls from `BuildCompingBars` and `BuildLeadBars` — render the straight `b.Events` directly. Emit `\tf <value>` as bar metadata at the start of the FIRST bar of each track, only when `tripletFeel != None` (a `None` song emits no `\tf` → byte-identical to today's Straight output, C2). Prepend to the first bar string; when a pickup leads, combine with the existing `\ac` prefix (verify metadata order against the alphaTex ref). Emit lowercase ident (`triplet8th`/`triplet16th`) — spelling confirmed from alphaTab docs, re-checked against the bundled build in step 7. AlphaTexRenderer stays the only alphaTex-aware code (C1).

<!-- step:ef-migration-for-the-renamed-column -->
### Step 3 — EF migration for the renamed column

The column is stored by name (`HasConversion<string>()`). Add an EF migration that renames `Feel`→`TripletFeel` and remaps legacy string values via UPDATE: `Straight→None`, `Swing→Triplet8th`, `Triplet→Triplet8th`, `Shuffle→Triplet8th`. Regenerate the model snapshot. Solo-dev DB — a best-effort remap is acceptable.

<!-- step:move-the-control-into-chordflowscore -->
### Step 4 — Move the control into ChordFlowScore

index.html: remove the `<label for="feel">` + `<select id="feel">` from #builder. score-render-component.js: add a tripletFeel select to the transport, expose `getTripletFeel()` (parallel to `getTempo()`), and on change fire `cb.onNeedsRerender(...)` (content-kind — the \tf line changes the alphaTex). app.js: drop `feel` from `FEELS`/`selections()` builder; `selections()` reads `view.getTripletFeel()`; the onNeedsRerender handler replays `lastScoreRequest` with fresh `getTripletFeel()` + `getRenderOptions()` (harmony unchanged → cheap re-render, no full regenerate). Keep tripletFeel a first-class request field like tempo — NOT folded into the view-only `getRenderOptions()` (C3).

<!-- step:tests-tf-assertions-bridge-parse -->
### Step 5 — Tests — \tf assertions + bridge parse

Update the render tests that pinned FeelTransform warping to instead assert the emitted `\tf` line for `Triplet8th`, and that a `None` song emits NO `\tf` (byte-identical to today's straight output). Keep FeelTransform's own unit tests green against the updated members (the class is unchanged behaviorally). Add a bridge parse test mapping feel strings → `TripletFeel`. Whole suite green (C6).

<!-- step:reference-sync-same-unit-of-work -->
### Step 6 — Reference sync (same unit of work)

alphatex-syntax-reference: add `\tf` (bar metadata; values none/triplet8th/triplet16th; applies until the next \tf or song end; per-track placement) — now verified. chordflow-domain-model-reference: Feel→TripletFeel (members + which are wired); FeelTransform no longer in the alphaTex path (only the future export seam); C4 wording stays but is realized via \tf. chordflow-dsl-reference: feel terminology + explicitly state there is NO feel token in the Progression/Song/Rhythm grammar. Edit refs via loom_patch_doc/loom_update_doc (gate-excluded but versioned).

<!-- step:build-green-visual-dogfood-verify -->
### Step 7 — Build green + visual dogfood verify

Confirm the `\tf` ident spelling against the bundled alphaTab.min.js (none/triplet8th/triplet16th per the docs; numeric `\tf 2` fallback if the bundled build differs). Full solution builds; all tests green (C6). Run the app, pick `Triplet8th`, and confirm alphaTab renders SWUNG NOTATION (not straight 8ths) and plays swung, and that flipping the control re-renders without a full regenerate (IN11). A passing string assertion is explicitly not sufficient acceptance (C5).

<!-- step:feel-control-in-the-content-preview -->
### Step 8 — Feel control in the Content preview

`content-crud.js`: pass `tripletFeel: true` to `ChordFlowScore.create` and add `tripletFeel: scoreView.getTripletFeel()` to the `entityPreview` envelope (the existing `onNeedsRerender → requestPreview()` already re-previews on change). Backend thread-through: `WebMessageRouter.EntityPreviewRequested` gains a `TripletFeel` arg (dispatch parses `envelope.TripletFeel` via the shared `ParseEnum`); `Program.cs` passes it to `ContentCrudHandler.Preview`, which forwards it to `ProgressionPreview`/`RhythmPreview` (set on the preview `Exercise`) and `SongPreview` (the `Render(..., tripletFeel, ...)` arg). `RenderOptions` stays view-only — feel rides as its own field (C3). Added `EntityPreview_CarriesTripletFeel` test; suite 635/635 green (C6).
