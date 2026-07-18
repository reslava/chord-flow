---
type: plan
id: pl_01KXT0GN7GBAYS3GP818SW2BBQ
title: "Fix: content Save preserves catalog metadata (tonality) on fork/edit"
status: done
created: 2026-07-18
updated: 2026-07-18
version: 1
design_version: 1
tags: []
parent_id: null
requires_load: []
target_version: 0.1.0
actual_release: 0.15.0
steps:
  - id: threaded-a-the-fork-from-item
    order: 1
    status: done
    description: "Threaded a `sourceId` (the fork-from item) through the content-save seam: `IContentStore.Save` + all four stores, `ContentCrudHandler.Save`, the `WebMessageRouter` `EntitySaveRequested` event + inbound envelope, `Program.cs`, and `content-crud.js` (a new `forkSourceId` editor-state var set on load/save)."
    files_touched: []
    blocked_by: []
    satisfies: []
  - id: made-re-attach-the-source-s
    order: 2
    status: done
    description: "Made `ProgressionStore`/`SongStore`/`VoicingStore` re-attach the source's catalog header on Save (the in-place row's own header, else the fork-from source's) instead of stripping it — fixing the correctness bug where forking/editing a minor progression silently dropped `tonality: minor` and realized it as major (Cm/Fm/Gm instead of Am/Dm/Em). Empty header ⇒ body stored verbatim (major flows byte-identical)."
    files_touched: []
    blocked_by: []
    satisfies: []
  - id: updated-the-domain-model-ref-catalogheader
    order: 3
    status: done
    description: "Updated the domain-model ref (CatalogHeader now documents `tonality:`/`description:` + the Home threading; IContentStore.Save now documents metadata preservation) and added two store tests (fork + in-place edit each preserve tonality). Full Core suite green: 1010 passed."
    files_touched: []
    blocked_by: []
    satisfies: []
---
# Fix: content Save preserves catalog metadata (tonality) on fork/edit

## Goal

Quick-ship record of 3 completed changes.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Threaded a `sourceId` (the fork-from item) through the content-save seam: `IContentStore.Save` + all four stores, `ContentCrudHandler.Save`, the `WebMessageRouter` `EntitySaveRequested` event + inbound envelope, `Program.cs`, and `content-crud.js` (a new `forkSourceId` editor-state var set on load/save). | — | — | — |
| ✅ | 2 | Made `ProgressionStore`/`SongStore`/`VoicingStore` re-attach the source's catalog header on Save (the in-place row's own header, else the fork-from source's) instead of stripping it — fixing the correctness bug where forking/editing a minor progression silently dropped `tonality: minor` and realized it as major (Cm/Fm/Gm instead of Am/Dm/Em). Empty header ⇒ body stored verbatim (major flows byte-identical). | — | — | — |
| ✅ | 3 | Updated the domain-model ref (CatalogHeader now documents `tonality:`/`description:` + the Home threading; IContentStore.Save now documents metadata preservation) and added two store tests (fork + in-place edit each preserve tonality). Full Core suite green: 1010 passed. | — | — | — |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
