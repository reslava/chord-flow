---
type: done
id: pl_01KXT0GN7GBAYS3GP818SW2BBQ-done
title: "Done — Fix: content Save preserves catalog metadata (tonality) on fork/edit"
status: done
created: 2026-07-18
version: 1
tags: []
parent_id: pl_01KXT0GN7GBAYS3GP818SW2BBQ
requires_load: []
---
# Done — Fix: content Save preserves catalog metadata (tonality) on fork/edit

Quick-shipped — recorded already-completed work:

1. Threaded a `sourceId` (the fork-from item) through the content-save seam: `IContentStore.Save` + all four stores, `ContentCrudHandler.Save`, the `WebMessageRouter` `EntitySaveRequested` event + inbound envelope, `Program.cs`, and `content-crud.js` (a new `forkSourceId` editor-state var set on load/save).
2. Made `ProgressionStore`/`SongStore`/`VoicingStore` re-attach the source's catalog header on Save (the in-place row's own header, else the fork-from source's) instead of stripping it — fixing the correctness bug where forking/editing a minor progression silently dropped `tonality: minor` and realized it as major (Cm/Fm/Gm instead of Am/Dm/Em). Empty header ⇒ body stored verbatim (major flows byte-identical).
3. Updated the domain-model ref (CatalogHeader now documents `tonality:`/`description:` + the Home threading; IContentStore.Save now documents metadata preservation) and added two store tests (fork + in-place edit each preserve tonality). Full Core suite green: 1010 passed.
