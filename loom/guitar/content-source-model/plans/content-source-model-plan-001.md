---
type: plan
id: pl_01KVZHRZZS5RF1Z8GQV3FFA479
title: Multi-source content model — additive listing, source tags, filter
status: done
created: 2026-06-25
updated: 2026-06-25
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KVZ4XQ47ZZ3DWFR65Z5AW1RH
requires_load: []
target_version: 0.1.0
steps:
  - id: unify-provenance-default-pack-is-a
    order: 1
    status: done
    description: "Unify provenance: retire Origin.BuiltIn (enum → {Pack, UserDefined}); OriginResolver.Rank drops BuiltIn (UserDefined > Pack). DefaultPack.ImportInto imports the default pack as Origin.Pack with PackId=\"default\". Persist pack identity for read-time tagging: a small Packs registry (PackEntity {Id, Name}) upserted by PackImporter from each manifest (default → \"ChordFlow Starter\"). Migration (C2 — DB is a cache): on startup, drop the now-invalid BuiltIn rows and re-import the default pack as Pack."
    files_touched: [src/ChordFlow.Core/Persistence/Origin.cs, src/ChordFlow.Core/Persistence/OriginResolver.cs, src/ChordFlow.Core/Features/Packs/DefaultPack.cs, src/ChordFlow.Core/Features/Packs/PackImporter.cs, src/ChordFlow.Core/Persistence/ChordFlowDbContext.cs, src/ChordFlow.Desktop/Program.cs]
    blocked_by: []
    satisfies: [IN3, IN9, C2]
  - id: additive-listing-source-packname-on-summaries
    order: 2
    status: done
    description: "Make IContentStore.List() additive: ContentSummary gains Source (package/user) + PackName (null unless package); ContentSummaries.Build returns one summary per (id, source) and no longer collapses via OriginResolver. Each of the four stores maps its rows → summaries, deriving Source from Origin and resolving PackName from the Packs registry (step 1). OriginResolver is retained only for any remaining single-item bare-id resolve."
    files_touched: [src/ChordFlow.Core/Persistence/IContentStore.cs, src/ChordFlow.Core/Persistence/ProgressionStore.cs, src/ChordFlow.Core/Persistence/SongStore.cs, src/ChordFlow.Core/Persistence/RhythmPatternStore.cs, src/ChordFlow.Core/Persistence/VoicingStore.cs]
    blocked_by: [1]
    satisfies: [IN1, IN2, C1]
  - id: user-only-writes-fork-on-edit
    order: 3
    status: done
    description: Store write semantics → user-only, no shadow, no revert. Save updates an existing UserDefined row or mints a new UserDefined row (fresh GUID); it never writes a same-id shadow over a package row. Delete removes a user row only → {Deleted, NotFound}; DeleteOutcome.Reverted is removed and the revert branch deleted. This is the storage half of fork-on-edit.
    files_touched: [src/ChordFlow.Core/Persistence/IContentStore.cs, src/ChordFlow.Core/Persistence/ProgressionStore.cs, src/ChordFlow.Core/Persistence/SongStore.cs, src/ChordFlow.Core/Persistence/RhythmPatternStore.cs, src/ChordFlow.Core/Persistence/VoicingStore.cs]
    blocked_by: [1]
    satisfies: [IN4, IN6]
  - id: bridge-dto-handler-source-tags-union
    order: 4
    status: done
    description: "Bridge DTO + handler. ContentItem gains source + packName (drop hasLowerTier); EntityList maps the new summaries. ContentCrudHandler.List gains the source-aggregation union point (store rows ∪ optional computed rows) — contributing no automatic rows here, just the seam (IN8). Add a \"Duplicate to user\" action: copy a package/automatic item's name+DSL into a new user item (Save with null id)."
    files_touched: [src/ChordFlow.Core/Features/ContentCrud/ContentCrudEnvelopes.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs]
    blocked_by: [2, 3]
    satisfies: [IN2, IN4, IN5, IN6, IN8, C3]
  - id: content-ui-source-badges-transient-filter
    order: 5
    status: done
    description: "Content UI: render the source badge per item (user / automatic / the pack name) with a per-source-kind colour; add a transient source filter (chips, all-on default, resets per page load; the automatic chip only for voicings); make package/automatic items read-only in the editor with a \"Duplicate to user\" button; remove the \"Revert to default\" label/branch."
    files_touched: [src/ChordFlow.Desktop/wwwroot/content-crud.js, src/ChordFlow.Desktop/wwwroot/index.html]
    blocked_by: [4]
    satisfies: [IN5, IN7, C5]
  - id: migration-check-tests-and-architecture-ref
    order: 6
    status: done
    description: Migrate + verify + ref-sync. Confirm the startup migration (BuiltIn rows → re-imported as Pack) runs clean on an existing DB. Update the impacted tests (OriginResolverTests, ContentCrudStoreTests, the four *PersistenceTests, PackImportTests, DefaultPackVoicingsTests) for the additive/fork-on-edit model. Sync chordflow-architecture-reference (§3 Persistence + §5 bridge) with the source model, tags, fork-on-edit, and the union seam. Full suite green.
    files_touched: [tests/ChordFlow.Core.Tests/OriginResolverTests.cs, tests/ChordFlow.Core.Tests/ContentCrudStoreTests.cs, tests/ChordFlow.Core.Tests/PackImportTests.cs, tests/ChordFlow.Core.Tests/DefaultPackVoicingsTests.cs, loom/refs/chordflow-architecture-reference.md]
    blocked_by: [5]
    satisfies: [IN9, C1, C5]
---
# Multi-source content model — additive listing, source tags, filter

## Goal

Implement the multi-source content model per the locked req: every content item lists with its source (`package` by name / `user` / future `automatic`), no source hides another, and a transient source filter narrows each view. Built Core-up so each layer rests on the one below: (1) unify provenance — the default pack becomes an ordinary `package` (PackId "default") and `Origin.BuiltIn` is retired, with pack identity (id+name) persisted for read-time tagging; (2) make `IContentStore.List()` additive (one summary per (id, source), carrying source + packName) — dropping the per-id `OriginResolver` collapse; (3) switch store writes to user-only — no same-id shadow, no revert (fork-on-edit lands as new user copies); (4) extend the bridge DTO + handler with source/packName, the "Duplicate to user" action, and the source-aggregation union seam (empty of `automatic` here — engine-derived fills it); (5) the Content UI — source badges, the transient filter, duplicate-to-user, and drop "Revert"; (6) migrate existing data, sync the architecture ref, and make the full suite green. Steps cite the locked req (IN1–IN9 / C1–C5). Resolution (which item plays) stays out — owned by engine-derived / voicing-ranking-strategies.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Unify provenance: retire Origin.BuiltIn (enum → {Pack, UserDefined}); OriginResolver.Rank drops BuiltIn (UserDefined > Pack). DefaultPack.ImportInto imports the default pack as Origin.Pack with PackId="default". Persist pack identity for read-time tagging: a small Packs registry (PackEntity {Id, Name}) upserted by PackImporter from each manifest (default → "ChordFlow Starter"). Migration (C2 — DB is a cache): on startup, drop the now-invalid BuiltIn rows and re-import the default pack as Pack. | src/ChordFlow.Core/Persistence/Origin.cs, src/ChordFlow.Core/Persistence/OriginResolver.cs, src/ChordFlow.Core/Features/Packs/DefaultPack.cs, src/ChordFlow.Core/Features/Packs/PackImporter.cs, src/ChordFlow.Core/Persistence/ChordFlowDbContext.cs, src/ChordFlow.Desktop/Program.cs | — | IN3, IN9, C2 |
| ✅ | 2 | Make IContentStore.List() additive: ContentSummary gains Source (package/user) + PackName (null unless package); ContentSummaries.Build returns one summary per (id, source) and no longer collapses via OriginResolver. Each of the four stores maps its rows → summaries, deriving Source from Origin and resolving PackName from the Packs registry (step 1). OriginResolver is retained only for any remaining single-item bare-id resolve. | src/ChordFlow.Core/Persistence/IContentStore.cs, src/ChordFlow.Core/Persistence/ProgressionStore.cs, src/ChordFlow.Core/Persistence/SongStore.cs, src/ChordFlow.Core/Persistence/RhythmPatternStore.cs, src/ChordFlow.Core/Persistence/VoicingStore.cs | 1 | IN1, IN2, C1 |
| ✅ | 3 | Store write semantics → user-only, no shadow, no revert. Save updates an existing UserDefined row or mints a new UserDefined row (fresh GUID); it never writes a same-id shadow over a package row. Delete removes a user row only → {Deleted, NotFound}; DeleteOutcome.Reverted is removed and the revert branch deleted. This is the storage half of fork-on-edit. | src/ChordFlow.Core/Persistence/IContentStore.cs, src/ChordFlow.Core/Persistence/ProgressionStore.cs, src/ChordFlow.Core/Persistence/SongStore.cs, src/ChordFlow.Core/Persistence/RhythmPatternStore.cs, src/ChordFlow.Core/Persistence/VoicingStore.cs | 1 | IN4, IN6 |
| ✅ | 4 | Bridge DTO + handler. ContentItem gains source + packName (drop hasLowerTier); EntityList maps the new summaries. ContentCrudHandler.List gains the source-aggregation union point (store rows ∪ optional computed rows) — contributing no automatic rows here, just the seam (IN8). Add a "Duplicate to user" action: copy a package/automatic item's name+DSL into a new user item (Save with null id). | src/ChordFlow.Core/Features/ContentCrud/ContentCrudEnvelopes.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs | 2, 3 | IN2, IN4, IN5, IN6, IN8, C3 |
| ✅ | 5 | Content UI: render the source badge per item (user / automatic / the pack name) with a per-source-kind colour; add a transient source filter (chips, all-on default, resets per page load; the automatic chip only for voicings); make package/automatic items read-only in the editor with a "Duplicate to user" button; remove the "Revert to default" label/branch. | src/ChordFlow.Desktop/wwwroot/content-crud.js, src/ChordFlow.Desktop/wwwroot/index.html | 4 | IN5, IN7, C5 |
| ✅ | 6 | Migrate + verify + ref-sync. Confirm the startup migration (BuiltIn rows → re-imported as Pack) runs clean on an existing DB. Update the impacted tests (OriginResolverTests, ContentCrudStoreTests, the four *PersistenceTests, PackImportTests, DefaultPackVoicingsTests) for the additive/fork-on-edit model. Sync chordflow-architecture-reference (§3 Persistence + §5 bridge) with the source model, tags, fork-on-edit, and the union seam. Full suite green. | tests/ChordFlow.Core.Tests/OriginResolverTests.cs, tests/ChordFlow.Core.Tests/ContentCrudStoreTests.cs, tests/ChordFlow.Core.Tests/PackImportTests.cs, tests/ChordFlow.Core.Tests/DefaultPackVoicingsTests.cs, loom/refs/chordflow-architecture-reference.md | 5 | IN9, C1, C5 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:unify-provenance-default-pack-is-a -->
### Step 1 — Unify provenance — default pack is a package; retire BuiltIn

Origin becomes a two-member provenance: `Pack` (carrying PackId) and `UserDefined`. The default pack stops being special — it imports through the same PackImporter path as any pack, stamped `Pack`/`PackId="default"`. A new `PackEntity {Id, Name}` table (upserted on import from the manifest) gives the read path a PackId→name lookup without re-reading disk. Migration is a clean cache rebuild: delete legacy BuiltIn content rows and re-import (no user data lost — user rows are UserDefined). Unit-test the resolver's new 2-tier ranking.

<!-- step:additive-listing-source-packname-on-summaries -->
### Step 2 — Additive listing + source/packName on summaries

`ContentSummary(Id, Name, Source, PackName, InitialKey)` — drop `Origin`/`HasLowerTier` from the summary (HasLowerTier is meaningless without shadowing). `ContentSummaries.Build` stops grouping-to-winner: it projects every row. PackName comes from the Packs registry table. Stores stay I/O-free of Music (C1). Unit-test: a pack item + a user item with related content both appear, each tagged.

<!-- step:user-only-writes-fork-on-edit -->
### Step 3 — User-only writes — fork-on-edit storage, no revert

The fork itself (copying a package item's DSL into a new user item) is driven by the handler/UI passing a null id to Save (step 4); the store change here is to (a) remove the shadow path — Save with a package id no longer creates a UserDefined shadow — and (b) trim Delete to user rows + drop Reverted. Unit-test: saving a copy mints a new id; the package row is untouched; delete removes only the user row.

<!-- step:bridge-dto-handler-source-tags-union -->
### Step 4 — Bridge DTO + handler — source tags, union seam, duplicate-to-user

`ContentItem(Id, Name, Source, PackName, InitialKey)` — additive wire change (C3): new fields, no rename of the entity* family. The union seam is a small per-kind aggregation in `List` so a future computed voicing source plugs in without more plumbing. "Duplicate to user" is a handler method (or a Save(entity, null, name, dsl) call from the UI) — no new envelope type needed if it reuses entitySave.

<!-- step:content-ui-source-badges-transient-filter -->
### Step 5 — Content UI — source badges, transient filter, duplicate-to-user

Badge text = source tag (replacing the `origin === "UserDefined" ? "User" : origin` logic). The filter is client-side over the entityList items (transient — no persistence, D5). Editing is gated by source: user → edit/delete; package/automatic → read-only + Duplicate to user. Dogfood (C5): the Content page now shows the tagged, filterable, un-collapsed rows — the visible proof.

<!-- step:migration-check-tests-and-architecture-ref -->
### Step 6 — Migration check, tests, and architecture-ref sync

The mandatory reference-doc sync (contract rule): the architecture ref's Persistence (§3) and bridge (§5) sections must describe the package/user/automatic source model, the additive listing, fork-on-edit (no shadow/revert), and the union seam in the same unit of work. Verify migration against a pre-existing DB (legacy BuiltIn rows).
