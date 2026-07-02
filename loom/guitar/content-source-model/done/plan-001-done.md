---
type: done
id: pl_01KVZHRZZS5RF1Z8GQV3FFA479-done
title: Done — Multi-source content model — additive listing, source tags, filter
status: done
created: 2026-06-25
updated: 2026-06-25
version: 2
tags: []
parent_id: pl_01KVZHRZZS5RF1Z8GQV3FFA479
requires_load: []
---
# Done — Multi-source content model — additive listing, source tags, filter

Shipped the additive multi-source content model: sources never hide each other; every list row is tagged with its source and filterable.

## What landed

- **D1 — `BuiltIn` retired.** The default/starter pack now imports as `Origin.Pack` with `PackId="default"` (an ordinary package), so it can show its manifest name. The `Origin` enum is now just `{ UserDefined, Pack }`. `ContentSourceMigration.Run(db)` (called from `Program.cs` after `DefaultPack.ImportInto`) converts legacy `BuiltIn` rows → `Pack` and forks legacy same-id user shadows into unique-id copies — idempotent, a no-op once migrated.
- **D2 — additive `List()`.** `IContentStore.List()` no longer collapses to one winner per id; it returns one `ContentSummary` per `(id, source)`. `ContentSummary` gained `ContentSource` (`Automatic`/`Package`/`User`) + `PackId`. `ContentSummaries.Build` drops the `OriginResolver` collapse (the resolver remains for single-item `Get`/`Find` + the voicing-book load).
- **D3/D4 — fork-on-edit.** Editing a package item mints a **new** `user` row with a fresh id (no same-id shadow), so the package original stays listed. `Delete` removes only the user row → `Deleted`/`NotFound`; `DeleteOutcome.Reverted` removed. Package/automatic items are read-only ("Duplicate to user").
- **D5 — tag + filter UI.** `content-crud.js` renders a per-source badge (pack name / `user` / `automatic`) and a transient source filter; resets per page load.
- **D6 — union seam.** `IComputedContentSource` + the union point in `ContentCrudHandler.List` (`items.AddRange(_computed.List(kind))`) — empty today; the engine-derived voicing source (engine-derived-as-app-source thread) fills it. `ContentItem` gained `Source` + `PackName`; `Program.cs` passes a `PackId → display-name` map.

## Refs

- `chordflow-architecture-reference.md` §3 (Persistence/source model) + §5 (bridge source/packName, badge + filter, Duplicate-to-user) — updated.
- `chordflow-domain-model-reference.md` §6 — corrected in the engine-derived-as-app-source thread (chat-002) after it was found still describing the retired `BuiltIn`/shadow/collapse model.