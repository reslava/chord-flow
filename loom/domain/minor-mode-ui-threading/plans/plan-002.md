---
type: plan
id: pl_01KXT1CMNQMAV9EAQMS001XYHH
title: Minor key mode — thread through content preview, list seeding & loadExercise
status: done
created: 2026-07-18
updated: 2026-07-18
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KXT0K179D9EVX7CY1EE40WYN
requires_load: []
target_version: 0.1.0
actual_release: 0.15.0
steps:
  - id: list-payload-surfaces-initialkeyisminor
    order: 1
    status: done
    description: ContentSummary gains InitialKeyIsMinor; each store's List projection sets it (progression from CatalogMetadata.Tonality, song from its key mode), and the entityList payload carries it.
    files_touched: [src/ChordFlow.Core/Persistence/IContentStore.cs, src/ChordFlow.Core/Persistence/ProgressionStore.cs, src/ChordFlow.Core/Persistence/SongStore.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs, src/ChordFlow.Core/Bridge/WebMessageRouter.cs, tests/ChordFlow.Core.Tests/ContentCrudStoreTests.cs, loom/refs/chordflow-domain-model-reference.md]
    blocked_by: []
    satisfies: [IN4, IN7, C1]
  - id: save-persists-an-explicit-tonality
    order: 2
    status: done
    description: entitySave carries an optional explicit tonality; the store serializes it into the catalog header when present (else the plan-001 preserve-source behavior), so a new minor progression can be authored and a major↔minor flip is written.
    files_touched: [src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs, src/ChordFlow.Core/Persistence/IContentStore.cs, src/ChordFlow.Core/Persistence/ProgressionStore.cs, src/ChordFlow.Desktop/Program.cs, tests/ChordFlow.Core.Tests/ContentCrudStoreTests.cs, loom/refs/chordflow-domain-model-reference.md]
    blocked_by: []
    satisfies: [IN3, IN7, C1, C3, C4]
  - id: tonality-control-in-the-content-editor
    order: 3
    status: done
    description: content-crud.js gains a major/minor control (progressions only) seeded from the list item's initialKeyIsMinor; it sends keyIsMinor on entityPreview (→ the reported \ks A becomes \ks Aminor) and the explicit tonality on entitySave.
    files_touched: [src/ChordFlow.Desktop/wwwroot/content-crud.js, src/ChordFlow.Desktop/wwwroot/index.html, loom/refs/chordflow-dsl-reference.md]
    blocked_by: [list-payload-surfaces-initialkeyisminor, save-persists-an-explicit-tonality]
    satisfies: [IN1, IN2, IN7, C2, C5]
  - id: loadexercise-reply-carries-the-key-mode
    order: 4
    status: done
    description: The loadExercise reply carries keyIsMinor from Exercise.KeyOverride; the load path seeds it via the existing hc.seedKeyMode so a saved minor exercise reopens minor and re-keying keeps mode.
    files_touched: [src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Core/Features/ExerciseLibrary/ExerciseLibrary.cs, src/ChordFlow.Desktop/Program.cs, src/ChordFlow.Desktop/wwwroot/app.js, tests/ChordFlow.Core.Tests/ExerciseLibraryTests.cs]
    blocked_by: []
    satisfies: [IN5, C1]
  - id: goldens-multi-key-song-payload-coverage
    order: 5
    status: done
    description: A golden that a multi-section Song in several keys/modes (via key/mod) realizes each section correctly, plus payload tests that entityList and loadExercise carry the mode.
    files_touched: [tests/ChordFlow.Core.Tests/SongExpanderTests.cs, tests/ChordFlow.Core.Tests/ContentCrudStoreTests.cs]
    blocked_by: [list-payload-surfaces-initialkeyisminor, loadexercise-reply-carries-the-key-mode]
    satisfies: [IN6, C1]
---
# Minor key mode — thread through content preview, list seeding & loadExercise

## Goal

Surface the already-wired minor `tonality` through the remaining app surfaces so a minor key is first-class everywhere, not just on the Practice generate path. The kernel/parse/realize/storage of `tonality:` is settled (first-class-minor-keys) and the fork/edit-drops-tonality correctness bug is already fixed (plan-001). This plan adds the last mile: a tonality control in the content editor that seeds from the content and drives both the live preview (→ `\ks Aminor`) and save (persisting `tonality:`, so a new minor progression can be authored), a list payload that carries the mode so the harmony controls auto-pick minor, and a `loadExercise` reply that carries mode so a saved minor exercise reopens minor. Every step keeps major flows byte-identical (absent/`major` ⇒ no header ⇒ verbatim body) and reuses the delivered `keyIsMinor` bridge/Preview path, `hc.seedKeyMode`, and the `item.initialKeyIsMinor` reader rather than rebuilding them. Reference docs are updated in the same unit as the code they describe.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | ContentSummary gains InitialKeyIsMinor; each store's List projection sets it (progression from CatalogMetadata.Tonality, song from its key mode), and the entityList payload carries it. | src/ChordFlow.Core/Persistence/IContentStore.cs, src/ChordFlow.Core/Persistence/ProgressionStore.cs, src/ChordFlow.Core/Persistence/SongStore.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs, src/ChordFlow.Core/Bridge/WebMessageRouter.cs, tests/ChordFlow.Core.Tests/ContentCrudStoreTests.cs, loom/refs/chordflow-domain-model-reference.md | — | IN4, IN7, C1 |
| ✅ | 2 | entitySave carries an optional explicit tonality; the store serializes it into the catalog header when present (else the plan-001 preserve-source behavior), so a new minor progression can be authored and a major↔minor flip is written. | src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs, src/ChordFlow.Core/Persistence/IContentStore.cs, src/ChordFlow.Core/Persistence/ProgressionStore.cs, src/ChordFlow.Desktop/Program.cs, tests/ChordFlow.Core.Tests/ContentCrudStoreTests.cs, loom/refs/chordflow-domain-model-reference.md | — | IN3, IN7, C1, C3, C4 |
| ✅ | 3 | content-crud.js gains a major/minor control (progressions only) seeded from the list item's initialKeyIsMinor; it sends keyIsMinor on entityPreview (→ the reported \ks A becomes \ks Aminor) and the explicit tonality on entitySave. | src/ChordFlow.Desktop/wwwroot/content-crud.js, src/ChordFlow.Desktop/wwwroot/index.html, loom/refs/chordflow-dsl-reference.md | list-payload-surfaces-initialkeyisminor, save-persists-an-explicit-tonality | IN1, IN2, IN7, C2, C5 |
| ✅ | 4 | The loadExercise reply carries keyIsMinor from Exercise.KeyOverride; the load path seeds it via the existing hc.seedKeyMode so a saved minor exercise reopens minor and re-keying keeps mode. | src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Core/Features/ExerciseLibrary/ExerciseLibrary.cs, src/ChordFlow.Desktop/Program.cs, src/ChordFlow.Desktop/wwwroot/app.js, tests/ChordFlow.Core.Tests/ExerciseLibraryTests.cs | — | IN5, C1 |
| ✅ | 5 | A golden that a multi-section Song in several keys/modes (via key/mod) realizes each section correctly, plus payload tests that entityList and loadExercise carry the mode. | tests/ChordFlow.Core.Tests/SongExpanderTests.cs, tests/ChordFlow.Core.Tests/ContentCrudStoreTests.cs | list-payload-surfaces-initialkeyisminor, loadexercise-reply-carries-the-key-mode | IN6, C1 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:list-payload-surfaces-initialkeyisminor -->
### Step 1 — List payload surfaces InitialKeyIsMinor

Add `bool? InitialKeyIsMinor` to ContentSummary (peer of InitialKey/DefaultFeel/DefaultTempo — null for the mode-independent entities). ProgressionStore.List reads it from each row's `CatalogHeader.Parse(Dsl).Metadata.Tonality`; SongStore.List from the song's own key mode (the same place InitialKey is derived); Rhythm/Voicing leave it null. Surface it on the entityList envelope so `content-crud.js`/`app.js` can seed from it (harmony-controls already reads `item.initialKeyIsMinor`). Tests: a minor progression and a minor song each list InitialKeyIsMinor=true; a major one false/null (C1). Update the ContentSummary row in the domain-model ref.

<!-- step:save-persists-an-explicit-tonality -->
### Step 2 — Save persists an explicit tonality

The inbound envelope + EntitySaveRequested event gain an optional `tonality` ("major"/"minor", parsed fail-loud via the CatalogHeader mapping). ContentCrudHandler.Save + IContentStore.Save take `Tonality? tonality = null`; ProgressionStore, when it is present, overrides the preserved metadata's Tonality before CatalogHeader.Serialize. Absent ⇒ unchanged preserve-source (C3); Major ⇒ no header emitted ⇒ body verbatim (C1). Only ProgressionStore acts on it (C4); the other stores accept the param inertly. Tests: authoring a new progression with tonality=minor persists `tonality: minor` and Find().Home==Minor; tonality=major writes no header. Update the IContentStore.Save row in the domain-model ref.

<!-- step:tonality-control-in-the-content-editor -->
### Step 3 — Tonality control in the content editor + live preview

Add a major/minor control shown only for the progression entity, seeded from pendingSeeds.keyIsMinor (from the step-1 list payload) so opening a minor progression shows minor; a manual change still wins. requestPreview sends keyIsMinor from the control (the bridge->Preview(keyIsMinor) path already exists, step 8a); onSave sends the control's value as `tonality`. No raw header text is exposed (C2). Dogfood: pick/author a minor progression -> preview emits \ks Aminor and spells from the relative-major table. Update the DSL ref's minor-authoring note to the editor-control model.

<!-- step:loadexercise-reply-carries-the-key-mode -->
### Step 4 — loadExercise reply carries the key mode

The loadExercise reply envelope gains keyIsMinor, sourced from the loaded Exercise.KeyOverride (which already round-trips IsMinor). app.js calls hc.seedKeyMode(msg.keyIsMinor) alongside the existing hc.seedKey(msg.key). A major exercise carries false ⇒ unchanged (C1). Test: a saved minor exercise's load reply carries keyIsMinor=true.

<!-- step:goldens-multi-key-song-payload-coverage -->
### Step 5 — Goldens: multi-key song + payload coverage

Confirm (not build) that the Song key/mod stream already expresses a multi-key/multi-mode arrangement: a song with `key Am` / `key G` / `key Bm` sections realizes each in its own key + mode. Consolidate the payload assertions (InitialKeyIsMinor in the list, keyIsMinor in the loadExercise reply) so the mode threading is regression-guarded end to end.
