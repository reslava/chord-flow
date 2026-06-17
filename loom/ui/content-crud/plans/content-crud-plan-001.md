---
type: plan
id: pl_01KV551H40R2DG2S89BX78HP6G
title: Content-definition CRUD UI — Plan
status: done
created: 2026-06-15
updated: 2026-06-15
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KV54ENW26AVDKP72VKY39ZEK
requires_load: []
target_version: 0.1.0
actual_release: 0.5.0
steps:
  - id: store-write-path-songstore
    order: 1
    status: done
    description: Store write path + new SongStore (List/Save/Delete) with (Id, Origin) tier-shadowing + canonicalization
    files_touched: [src/ChordFlow.Core/Persistence/ProgressionStore.cs, src/ChordFlow.Core/Persistence/RhythmPatternStore.cs, src/ChordFlow.Core/Persistence/VoicingStore.cs, src/ChordFlow.Core/Persistence/SongStore.cs, tests/ChordFlow.Core.Tests/]
    blocked_by: []
    satisfies: [IN7, IN8, IN9, C1, C2, C4]
  - id: contentcrud-slice-generic-bridge-protocol
    order: 2
    status: done
    description: ContentCrud Features slice + generic bridge envelope family (entityList/Get/Preview/Save/Delete) + router verbs
    files_touched: [src/ChordFlow.Core/Features/ContentCrud/, src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Core/Bridge/, tests/ChordFlow.Core.Tests/]
    blocked_by: [1]
    satisfies: [IN1, IN7, IN10, C3, C5]
  - id: voicing-diagrammodel
    order: 3
    status: done
    description: Voicing DiagramModel computed in Core (intervals/functions/spelling) + wire it into the voicing preview payload
    files_touched: [src/ChordFlow.Core/Domain/Voicings/, src/ChordFlow.Core/Features/ContentCrud/, tests/ChordFlow.Core.Tests/]
    blocked_by: [2]
    satisfies: [IN5, IN6]
  - id: front-end-split-view-toggle
    order: 4
    status: done
    description: "Front-end refactor: extract shared bridge.js module + add the single-page Practice⇄Content view toggle"
    files_touched: [src/ChordFlow.Desktop/wwwroot/bridge.js, src/ChordFlow.Desktop/wwwroot/app.js, src/ChordFlow.Desktop/wwwroot/index.html]
    blocked_by: [2]
    satisfies: [IN12, C6]
  - id: generic-editor-score-preview
    order: 5
    status: done
    description: "Generic content editor (content-crud.js): entity picker, list with origin badges, name+DSL fields, live parse error, score-preview strategy, Save + Delete/Revert"
    files_touched: [src/ChordFlow.Desktop/wwwroot/content-crud.js, src/ChordFlow.Desktop/wwwroot/index.html]
    blocked_by: [4, 2]
    satisfies: [IN1, IN2, IN3, IN4, IN13]
  - id: svg-voicing-fret-box
    order: 6
    status: done
    description: SVG fret-box renderer (chord-diagram.js) + the voicing diagram-preview strategy
    files_touched: [src/ChordFlow.Desktop/wwwroot/chord-diagram.js, src/ChordFlow.Desktop/wwwroot/content-crud.js]
    blocked_by: [5, 3]
    satisfies: [IN4, IN5]
  - id: voicing-live-refresh
    order: 7
    status: done
    description: Voicing live-refresh wiring in Program.cs (rebuild VoicingBook + AlphaTexRenderer on voicing save/delete)
    files_touched: [src/ChordFlow.Desktop/Program.cs]
    blocked_by: [1, 2]
    satisfies: [IN11]
---
# Content-definition CRUD UI — Plan

## Goal

Build the uniform CRUD surface for the four DSL-backed content entities and the engine write path it needs. Pure-Core foundations land first (store write path + new SongStore, the generic ContentCrud slice + bridge protocol, the voicing DiagramModel), all unit-tested with no UI dependency; then the front-end (shared bridge module + Practice⇄Content toggle, the generic editor with the score-preview strategy, the SVG fret-box voicing diagram); finally the voicing live-refresh wiring in the host. Each step is an independent, testable seam, satisfying the locked req (IN1–IN13, C1–C6) and respecting its exclusions (generator stays on SeedData, canonical-C anchor only, no catalog-metadata or pack UI).

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Store write path + new SongStore (List/Save/Delete) with (Id, Origin) tier-shadowing + canonicalization | src/ChordFlow.Core/Persistence/ProgressionStore.cs, src/ChordFlow.Core/Persistence/RhythmPatternStore.cs, src/ChordFlow.Core/Persistence/VoicingStore.cs, src/ChordFlow.Core/Persistence/SongStore.cs, tests/ChordFlow.Core.Tests/ | — | IN7, IN8, IN9, C1, C2, C4 |
| ✅ | 2 | ContentCrud Features slice + generic bridge envelope family (entityList/Get/Preview/Save/Delete) + router verbs | src/ChordFlow.Core/Features/ContentCrud/, src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Core/Bridge/, tests/ChordFlow.Core.Tests/ | 1 | IN1, IN7, IN10, C3, C5 |
| ✅ | 3 | Voicing DiagramModel computed in Core (intervals/functions/spelling) + wire it into the voicing preview payload | src/ChordFlow.Core/Domain/Voicings/, src/ChordFlow.Core/Features/ContentCrud/, tests/ChordFlow.Core.Tests/ | 2 | IN5, IN6 |
| ✅ | 4 | Front-end refactor: extract shared bridge.js module + add the single-page Practice⇄Content view toggle | src/ChordFlow.Desktop/wwwroot/bridge.js, src/ChordFlow.Desktop/wwwroot/app.js, src/ChordFlow.Desktop/wwwroot/index.html | 2 | IN12, C6 |
| ✅ | 5 | Generic content editor (content-crud.js): entity picker, list with origin badges, name+DSL fields, live parse error, score-preview strategy, Save + Delete/Revert | src/ChordFlow.Desktop/wwwroot/content-crud.js, src/ChordFlow.Desktop/wwwroot/index.html | 4, 2 | IN1, IN2, IN3, IN4, IN13 |
| ✅ | 6 | SVG fret-box renderer (chord-diagram.js) + the voicing diagram-preview strategy | src/ChordFlow.Desktop/wwwroot/chord-diagram.js, src/ChordFlow.Desktop/wwwroot/content-crud.js | 5, 3 | IN4, IN5 |
| ✅ | 7 | Voicing live-refresh wiring in Program.cs (rebuild VoicingBook + AlphaTexRenderer on voicing save/delete) | src/ChordFlow.Desktop/Program.cs | 1, 2 | IN11 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:store-write-path-songstore -->
### Step 1 — Store write path + SongStore

Add `List()` / `Save(...)` / `Delete(id)` to each store; build a new `SongStore` (none exists — `SongEntity` + `SongParser` do). Writes target the `UserDefined` tier only: a new entity inserts `(GUID, UserDefined)`; editing a user entity updates that row; editing a BuiltIn/Pack entity inserts/updates a `(id, UserDefined)` shadow without touching the lower row; delete removes the `(id, UserDefined)` row (gone if user-only, revert if a lower tier exists). Validate-by-parse on save (each parser); voicing save canonicalizes via `VoicingDslParser.Parse` → `VoicingDslWriter.ToDsl` (canonical-C); the other three store the validated DSL as typed (header round-tripped via `CatalogHeader`). Unit tests cover each path incl. shadow/revert and voicing round-trip.

<!-- step:contentcrud-slice-generic-bridge-protocol -->
### Step 2 — ContentCrud slice + generic bridge protocol

A `ContentCrud` slice composing the stores: list/get/save/delete dispatched on an `entity` discriminator, returning the outbound DTOs. Extend `WebMessageRouter` with the `entity*` inbound verbs (add a string `EntityId`/`Entity`/`Name`/`Dsl` to the inbound record; keep the existing int `Id` for `loadExercise`). Score-entity preview (progression/song/rhythm) builds a minimal preview `Exercise` with fixed defaults (key C, default rhythm / single sustained chord, default tempo) → alphaTex; invalid DSL → `entityParseError` carrying the `FormatException` message. Router-dispatch + slice tests.

<!-- step:voicing-diagrammodel -->
### Step 3 — Voicing DiagramModel

A `DiagramModel` DTO (firstFret, barreFret?, and 6 per-string entries: state muted/open/fretted, fret?, spelled note, interval label, ChordToneFunction). Built from the canonical-C `VoicingShape`: each `FretPosition` → pitch class via `Fretboard` → interval vs the root → function/label via `QualityIntervals`/`ChordToneFunction`, note via `NoteSpeller`. Shown at the canonical-C anchor (no root-picker — EX2). The voicing branch of `entityPreview` returns `{kind:"diagram", diagram}`. Unit tests assert intervals/functions/spelling for known shapes.

<!-- step:front-end-split-view-toggle -->
### Step 4 — Front-end split + view toggle

Extract the `Bridge` transport into `bridge.js` (shared, no behavior change); keep `app.js` as the Practice view consuming it. Add a header nav toggle + a `#content-view` container to `index.html` switching between Practice (existing) and Content (new) without a second HTML page or a second alphaTab bootstrap. Vanilla JS only (C6).

<!-- step:generic-editor-score-preview -->
### Step 5 — Generic editor + score preview

One component configured by an `ENTITIES` table (key, label, previewKind, placeholder, help). Renders entity picker, list (origin badges + contextual Delete/Revert label — IN13), name field, DSL textarea, inline parse-error line, and a preview pane. Debounced input → `entityPreview`; the score strategy renders the returned `tex` in a dedicated small alphaTab instance. Save → `entitySave`, Delete → `entityDelete`, list refresh on both.

<!-- step:svg-voicing-fret-box -->
### Step 6 — SVG voicing fret-box

`chord-diagram.js` draws a `DiagramModel`: 6 strings × N frets grid, colored dots by `function`, an interval⇄note label toggle, barre bar, open `○` / muted `✕` markers, first-fret indicator, and an interval-color legend. Wire it as the `diagram` preview strategy in `content-crud.js` for the voicing entity. Pure presentation — no theory in JS (IN6).

<!-- step:voicing-live-refresh -->
### Step 7 — Voicing live-refresh

Hold the voicing-backed renderer behind a swappable holder the handlers read (instead of a captured local). On a ContentCrud `VoicingsChanged` signal, re-run `VoicingStore.LoadShapes()` → new `VoicingBook` → new `AlphaTexRenderer` and swap the holder so the next generated score reflects the change without a restart. Progression/Song/Rhythm need no rebuild (read per-use, not yet consumed by the generator — EX1).
