---
type: plan
id: pl_01KVSR3075H7FYRHH52533Y2F6
title: content-preview-comping Plan
status: done
created: 2026-06-23
updated: 2026-06-23
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KVSQ96H93G8GNAM6DHTBPYD4
requires_load: []
target_version: 0.1.0
actual_release: 0.11.0
steps:
  - id: backend-preview-resolves-comping
    order: 1
    status: done
    description: Resolve a comping id in ContentCrudHandler.Preview and feed it to the progression/song builders
    files_touched: [src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs]
    blocked_by: []
    satisfies: [IN3, IN4, IN6, C2, C6]
  - id: bridge-wiring-carry-compingpatternid
    order: 2
    status: done
    description: Widen EntityPreviewRequested and pass envelope.CompingPatternId through the router and Program wiring
    files_touched: [src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Desktop/Program.cs]
    blocked_by: [1]
    satisfies: [IN3, C3]
  - id: frontend-comping-picker
    order: 3
    status: done
    description: "Add the comping picker to content-crud.js: toolbar select, catalog fetch, envelope field, re-preview on change"
    files_touched: [src/ChordFlow.Desktop/wwwroot/content-crud.js, src/ChordFlow.Desktop/wwwroot/styles.css]
    blocked_by: [2]
    satisfies: [IN1, IN2, IN5, C1, C5]
  - id: tests
    order: 4
    status: done
    description: Tests — entityPreview carries the id; Preview resolves it, falls back to beat_1_3, and fails loud on a bad id
    files_touched: [tests/ChordFlow.Core.Tests/WebMessageRouterContentTests.cs, tests/ChordFlow.Core.Tests/ContentCrudStoreTests.cs]
    blocked_by: [2]
    satisfies: [IN3, IN4, IN6]
  - id: validate-ref-sync
    order: 5
    status: done
    description: Manual dogfood + sync the bridge contract note in the architecture reference
    files_touched: [loom/refs/chordflow-architecture-reference.md]
    blocked_by: [3, 4]
    satisfies: [IN1, IN3]
---
# content-preview-comping Plan

## Goal

Thread a chosen comping rhythm through the Content preview path so progression and song previews play with a real strum instead of the hard-wired SeedData.Quarters. The feature is plumbing one id (compingPatternId) from a new content-page picker through the existing entityPreview bridge verb into ContentCrudHandler.Preview, where it resolves via the existing ExerciseRefs.ResolvePattern seam. No new engine capability; ScoreR stays content-agnostic; only the preview path changes (generate/save/library already carry CompingPatternId). Default beat_1_3, transient (resets each page load), comping-only.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Resolve a comping id in ContentCrudHandler.Preview and feed it to the progression/song builders | src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs | — | IN3, IN4, IN6, C2, C6 |
| ✅ | 2 | Widen EntityPreviewRequested and pass envelope.CompingPatternId through the router and Program wiring | src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Desktop/Program.cs | 1 | IN3, C3 |
| ✅ | 3 | Add the comping picker to content-crud.js: toolbar select, catalog fetch, envelope field, re-preview on change | src/ChordFlow.Desktop/wwwroot/content-crud.js, src/ChordFlow.Desktop/wwwroot/styles.css | 2 | IN1, IN2, IN5, C1, C5 |
| ✅ | 4 | Tests — entityPreview carries the id; Preview resolves it, falls back to beat_1_3, and fails loud on a bad id | tests/ChordFlow.Core.Tests/WebMessageRouterContentTests.cs, tests/ChordFlow.Core.Tests/ContentCrudStoreTests.cs | 2 | IN3, IN4, IN6 |
| ✅ | 5 | Manual dogfood + sync the bridge contract note in the architecture reference | loom/refs/chordflow-architecture-reference.md | 3, 4 | IN1, IN3 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:backend-preview-resolves-comping -->
### Step 1 — Backend — Preview resolves comping

Add a trailing optional param `string? compingPatternId = null` to `Preview` (keeps existing callers compiling). Inside the `using db` block resolve once: `RhythmPattern comping = ExerciseRefs.ResolvePattern(string.IsNullOrWhiteSpace(compingPatternId) ? "beat_1_3" : compingPatternId, db);` Pass `comping` into `ProgressionPreview` and `SongPreview` in place of `SeedData.Quarters` (parameterize those two builders to take the pattern). Leave `RhythmPreview` and `VoicingPreview` untouched (C6). A non-blank id that ResolvePattern can't find throws InvalidOperationException → caught by Preview's existing outer catch → FormatException → entityParseError (IN6, fail-loud, consistent with generate/load). Note the intentional default flip Quarters → beat_1_3.

<!-- step:bridge-wiring-carry-compingpatternid -->
### Step 2 — Bridge + wiring — carry compingPatternId

InboundEnvelope already has CompingPatternId (no new field — C3). Widen the event from `Action<string,string,RenderOptions,TripletFeel>` to `Action<string,string,RenderOptions,TripletFeel,string?>` and pass `envelope.CompingPatternId` in the `entityPreview` dispatch arm. In Program.cs add the extra lambda arg and forward it to `contentCrud.Preview(entity, dsl, renderOptions, tripletFeel, compingPatternId)`. Update the EntityPreviewRequested XML-doc tuple comment to include the id.

<!-- step:frontend-comping-picker -->
### Step 3 — Frontend — comping picker

Add a `cc-preview-toolbar` row above the score holding a comping `<label>`+`<select>`, shown only for progression/song (hidden for rhythm/voicing). On `selectEntity` for progression/song, send `{type:"entityList", entity:"rhythm"}` to fill the picker; add a carve-out at the top of `onMessage` BEFORE the `msg.entity !== current.key` guard: `if (msg.type==="entityList" && msg.entity==="rhythm" && current.key!=="rhythm") { populateCompingOptions(msg.items||[]); return; }`. Options `{value:it.id,label:it.name}`, default-select `beat_1_3`. `requestPreview()` adds `compingPatternId` (the select value) to the entityPreview envelope when the picker is visible. On `<select>` change → `requestPreview()`. Transient = the select's value only; resets to beat_1_3 each load (IN5). Vanilla JS only (C5). Confirm the styles.css touch is needed only if the toolbar needs layout.

<!-- step:tests -->
### Step 4 — Tests

Router test: an `entityPreview` envelope with a `compingPatternId` raises EntityPreviewRequested with that id (and absence → null). Handler tests (against an in-memory db seeded with the rhythm catalog): a progression/song preview with a chosen comping id renders that pattern (distinguishable from Quarters); a blank/absent id falls back to beat_1_3; a non-blank unknown id throws FormatException (the entityParseError surface). Place handler tests where the existing ContentCrud handler/store tests live.

<!-- step:validate-ref-sync -->
### Step 5 — Validate + ref sync

Run the app: Content → Progression (`17 47 17 57`) and a Song, pick several comping patterns → preview re-renders and plays with the chosen strum; rhythm/voicing show no picker; an unknown id surfaces inline, not a crash. Update chordflow-architecture-reference.md where it documents the entityPreview bridge verb to note it now carries compingPatternId (bridge-contract change → ref sync). The fretboard dogfood rule does not apply (score-preview UI knob, not a fretboard/engine capability).
