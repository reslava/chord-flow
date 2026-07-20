---
type: plan
id: pl_01KY07MWVRATS2GFKWACQR79J1
title: Editable genre/subgenre/tags — implementation
status: done
created: 2026-07-20
updated: 2026-07-20
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KY06N5VJJC3VN7GTNNFNMMT1
requires_load: []
target_version: 0.1.0
steps:
  - id: store-contract-merge-column-population
    order: 1
    status: done
    description: Add an authoritative `CatalogMetadataPatch(Genre, Subgenre, Tags)` and thread it through `IContentStore.Save`; each catalog store overlays the patch onto the preserved header via `record with` (keeping Description + Tonality) and writes the final genre/subgenre/tags into the denormalized `ICatalogEntity` columns; the Rhythm store accepts a null patch inertly. A present-but-empty patch clears; a null patch preserves as today. Store-level tests.
    files_touched: [src/ChordFlow.Core/Persistence/IContentStore.cs, src/ChordFlow.Core/Persistence/CatalogMetadata.cs, src/ChordFlow.Core/Persistence/ProgressionStore.cs, src/ChordFlow.Core/Persistence/SongStore.cs, src/ChordFlow.Core/Persistence/VoicingStore.cs, src/ChordFlow.Core/Persistence/DrumGrooveStore.cs, src/ChordFlow.Core/Persistence/RhythmPatternStore.cs, tests/ChordFlow.Core.Tests/]
    blocked_by: []
    satisfies: [IN5, IN6, IN9, C1, C2, C4, C5]
  - id: bridge-threads-the-fields-through
    order: 2
    status: done
    description: Extend the `entitySave` inbound envelope + the `EntitySaveRequested` event + its `Program.cs` subscription + `ContentCrudHandler.Save` to carry genre/subgenre/tags into the store patch — mirroring the existing `tonality` hop. Router test asserting the envelope round-trips the fields to the store.
    files_touched: [src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs, src/ChordFlow.Desktop/Program.cs, tests/ChordFlow.Core.Tests/WebMessageRouterContentTests.cs]
    blocked_by: [store-contract-merge-column-population]
    satisfies: [IN5, C3]
  - id: editor-ui-datalist-inputs-tags-pill
    order: 3
    status: done
    description: "Add a `metadata`-gated editor block (per-entity flag mirroring `tonality: true`): genre/subgenre `<input>`+`<datalist>` whose options are distinct present values discovered client-side from the current `entityList`, and a tags pill editor (add from datalist or type; remove individually). Seed the controls from the clicked list row (no load-path change); send the values on save. Shown for Progression/Song/Voicing/Drums, hidden for Rhythm."
    files_touched: [src/ChordFlow.Desktop/wwwroot/metadata-editor-component.js, src/ChordFlow.Desktop/wwwroot/content-crud.js, src/ChordFlow.Desktop/wwwroot/drums.js, src/ChordFlow.Desktop/wwwroot/index.html]
    blocked_by: [bridge-threads-the-fields-through]
    satisfies: [IN1, IN2, IN3, IN4, IN7, IN8, IN10]
  - id: ref-sync-icatalogentity-doc-nit
    order: 4
    status: done
    description: Update `chordflow-architecture-reference.md` for the editable-metadata contract (the `entitySave` envelope + `IContentStore.Save` now carry an authoritative metadata patch; columns populated on save). Fix the `ICatalogEntity` XML summary to list all four implementers (add `DrumGrooveEntity`).
    files_touched: [loom/refs/chordflow-architecture-reference.md, src/ChordFlow.Core/Persistence/Entities/ICatalogEntity.cs]
    blocked_by: [store-contract-merge-column-population, bridge-threads-the-fields-through]
    satisfies: [IN11]
---
# Editable genre/subgenre/tags — implementation

## Goal

Make genre/subgenre/tags editable in the Content CRUD editor for the four metadata-bearing entities (Progression / Song / Voicing / Drums) by widening the existing editor-authoritative override — the pattern the `tonality` control already proves end-to-end. Bottom-up: first the store contract carries an authoritative `CatalogMetadataPatch` (merged over the preserved header, keeping Description/Tonality) and populates the denormalized `ICatalogEntity` columns from the final metadata (scope option A — `List()` keeps parsing headers); then the bridge threads the fields through; then the editor UI adds the datalist inputs + tags pill editor, seeding from the clicked list row; finally the architecture ref and the `ICatalogEntity` doc are synced. Rhythm carries no metadata (EX1) and is untouched.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add an authoritative `CatalogMetadataPatch(Genre, Subgenre, Tags)` and thread it through `IContentStore.Save`; each catalog store overlays the patch onto the preserved header via `record with` (keeping Description + Tonality) and writes the final genre/subgenre/tags into the denormalized `ICatalogEntity` columns; the Rhythm store accepts a null patch inertly. A present-but-empty patch clears; a null patch preserves as today. Store-level tests. | src/ChordFlow.Core/Persistence/IContentStore.cs, src/ChordFlow.Core/Persistence/CatalogMetadata.cs, src/ChordFlow.Core/Persistence/ProgressionStore.cs, src/ChordFlow.Core/Persistence/SongStore.cs, src/ChordFlow.Core/Persistence/VoicingStore.cs, src/ChordFlow.Core/Persistence/DrumGrooveStore.cs, src/ChordFlow.Core/Persistence/RhythmPatternStore.cs, tests/ChordFlow.Core.Tests/ | — | IN5, IN6, IN9, C1, C2, C4, C5 |
| ✅ | 2 | Extend the `entitySave` inbound envelope + the `EntitySaveRequested` event + its `Program.cs` subscription + `ContentCrudHandler.Save` to carry genre/subgenre/tags into the store patch — mirroring the existing `tonality` hop. Router test asserting the envelope round-trips the fields to the store. | src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs, src/ChordFlow.Desktop/Program.cs, tests/ChordFlow.Core.Tests/WebMessageRouterContentTests.cs | store-contract-merge-column-population | IN5, C3 |
| ✅ | 3 | Add a `metadata`-gated editor block (per-entity flag mirroring `tonality: true`): genre/subgenre `<input>`+`<datalist>` whose options are distinct present values discovered client-side from the current `entityList`, and a tags pill editor (add from datalist or type; remove individually). Seed the controls from the clicked list row (no load-path change); send the values on save. Shown for Progression/Song/Voicing/Drums, hidden for Rhythm. | src/ChordFlow.Desktop/wwwroot/metadata-editor-component.js, src/ChordFlow.Desktop/wwwroot/content-crud.js, src/ChordFlow.Desktop/wwwroot/drums.js, src/ChordFlow.Desktop/wwwroot/index.html | bridge-threads-the-fields-through | IN1, IN2, IN3, IN4, IN7, IN8, IN10 |
| ✅ | 4 | Update `chordflow-architecture-reference.md` for the editable-metadata contract (the `entitySave` envelope + `IContentStore.Save` now carry an authoritative metadata patch; columns populated on save). Fix the `ICatalogEntity` XML summary to list all four implementers (add `DrumGrooveEntity`). | loom/refs/chordflow-architecture-reference.md, src/ChordFlow.Core/Persistence/Entities/ICatalogEntity.cs | store-contract-merge-column-population, bridge-threads-the-fields-through | IN11 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
