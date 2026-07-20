---
type: plan
id: pl_01KXZP3GCMNSAA61SNJV362WV9
title: Shared FilterR + genre/subgenre/tags surfacing
status: done
created: 2026-07-20
updated: 2026-07-20
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KXZNQA2JCG9QBWKV84ZDB7XM
requires_load: []
target_version: 0.1.0
steps:
  - id: data-plumbing-metadata-on-the-wire
    order: 1
    status: done
    description: "Carry genre/subgenre/tags on ContentSummary + entityList: extend ContentSummary and ContentSummaries.Build, populate from each store's List() (Progression/Song/Voicing/Drums from ICatalogEntity columns; Rhythm null/empty), and add the fields to the entityList reply."
    files_touched: [src/ChordFlow.Core/Persistence/IContentStore.cs, src/ChordFlow.Core/Persistence/ProgressionStore.cs, src/ChordFlow.Core/Persistence/SongStore.cs, src/ChordFlow.Core/Persistence/VoicingStore.cs, src/ChordFlow.Core/Persistence/DrumGrooveStore.cs, src/ChordFlow.Core/Persistence/RhythmPatternStore.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudEnvelopes.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs]
    blocked_by: []
    satisfies: [IN1, IN2]
  - id: filterr-dumb-presentational-component
    order: 2
    status: done
    description: "Extract the dumb FilterR component (filter-render-component.js, window.ChordFlowFilter): a faceted toggle-chip stack driven by a levels config, emitting enabled-token sets via onChange, with setLevels/getState/dispose. No data source, no filtering logic, no music theory. Load it in index.html."
    files_touched: [src/ChordFlow.Desktop/wwwroot/filter-render-component.js, src/ChordFlow.Desktop/wwwroot/index.html]
    blocked_by: []
    satisfies: [IN3, C1]
  - id: content-pages-fields-filterr
    order: 3
    status: done
    description: "Content pages adopt FilterR: render genre/subgenre/tags on each list row, mount FilterR with the Source level folded in + Genre/Subgenre/Tags levels discovered from the listed rows, and filter client-side with OR-within/AND-across semantics (empty match → empty list)."
    files_touched: [src/ChordFlow.Desktop/wwwroot/content-crud.js]
    blocked_by: [data-plumbing-metadata-on-the-wire, filterr-dumb-presentational-component]
    satisfies: [IN4, C2, C3, C5]
  - id: voicings-page-fold-onto-filterr
    order: 4
    status: done
    description: Fold GuitarVoicingsR's existing Source/Family/3rd/5th/7th stack onto FilterR — chip rendering moves into the shared component; the server-side voicingGrid round-trip behavior is byte-for-byte unchanged. The g/s/t axis is NOT added here (EX1).
    files_touched: [src/ChordFlow.Desktop/wwwroot/guitar-voicings-render-component.js]
    blocked_by: [filterr-dumb-presentational-component]
    satisfies: [IN5, C5]
  - id: practice-page-g-s-t-strip
    order: 5
    status: done
    description: Practice page gains a single genre/subgenre/tags filter strip that narrows the metadata-bearing pickers — Harmony (Song/Progression) + Drums (DrumGroove) — client-side over their entityList payloads; source always all; Comping/Lead untouched.
    files_touched: [src/ChordFlow.Desktop/wwwroot/app.js]
    blocked_by: [data-plumbing-metadata-on-the-wire, filterr-dumb-presentational-component]
    satisfies: [IN6, C3, C4]
  - id: architecture-ref-sync
    order: 6
    status: done
    description: Update chordflow-architecture-reference.md to list FilterR in the UI dumb-views roster (§2 solution shape + §7 diagram), per the ref-sync rule.
    files_touched: [loom/refs/chordflow-architecture-reference.md]
    blocked_by: []
    satisfies: [IN7]
---
# Shared FilterR + genre/subgenre/tags surfacing

## Goal

Surface the already-modeled catalog metadata (genre/subgenre/tags) on the content lists and Practice pickers, and unify faceted filtering behind one dumb, presentational FilterR component — without collapsing the two legitimately-different filtering mechanisms (Content = client-side over the returned rows; Voicings = the existing server round-trip). Builds against the locked req IN1–IN7 / C1–C5; the Voicings g/s/t axis (EX1) and rhythm-pattern metadata (EX2) are deferred to their own threads.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Carry genre/subgenre/tags on ContentSummary + entityList: extend ContentSummary and ContentSummaries.Build, populate from each store's List() (Progression/Song/Voicing/Drums from ICatalogEntity columns; Rhythm null/empty), and add the fields to the entityList reply. | src/ChordFlow.Core/Persistence/IContentStore.cs, src/ChordFlow.Core/Persistence/ProgressionStore.cs, src/ChordFlow.Core/Persistence/SongStore.cs, src/ChordFlow.Core/Persistence/VoicingStore.cs, src/ChordFlow.Core/Persistence/DrumGrooveStore.cs, src/ChordFlow.Core/Persistence/RhythmPatternStore.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudEnvelopes.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs | — | IN1, IN2 |
| ✅ | 2 | Extract the dumb FilterR component (filter-render-component.js, window.ChordFlowFilter): a faceted toggle-chip stack driven by a levels config, emitting enabled-token sets via onChange, with setLevels/getState/dispose. No data source, no filtering logic, no music theory. Load it in index.html. | src/ChordFlow.Desktop/wwwroot/filter-render-component.js, src/ChordFlow.Desktop/wwwroot/index.html | — | IN3, C1 |
| ✅ | 3 | Content pages adopt FilterR: render genre/subgenre/tags on each list row, mount FilterR with the Source level folded in + Genre/Subgenre/Tags levels discovered from the listed rows, and filter client-side with OR-within/AND-across semantics (empty match → empty list). | src/ChordFlow.Desktop/wwwroot/content-crud.js | data-plumbing-metadata-on-the-wire, filterr-dumb-presentational-component | IN4, C2, C3, C5 |
| ✅ | 4 | Fold GuitarVoicingsR's existing Source/Family/3rd/5th/7th stack onto FilterR — chip rendering moves into the shared component; the server-side voicingGrid round-trip behavior is byte-for-byte unchanged. The g/s/t axis is NOT added here (EX1). | src/ChordFlow.Desktop/wwwroot/guitar-voicings-render-component.js | filterr-dumb-presentational-component | IN5, C5 |
| ✅ | 5 | Practice page gains a single genre/subgenre/tags filter strip that narrows the metadata-bearing pickers — Harmony (Song/Progression) + Drums (DrumGroove) — client-side over their entityList payloads; source always all; Comping/Lead untouched. | src/ChordFlow.Desktop/wwwroot/app.js | data-plumbing-metadata-on-the-wire, filterr-dumb-presentational-component | IN6, C3, C4 |
| ✅ | 6 | Update chordflow-architecture-reference.md to list FilterR in the UI dumb-views roster (§2 solution shape + §7 diagram), per the ref-sync rule. | loom/refs/chordflow-architecture-reference.md | — | IN7 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
