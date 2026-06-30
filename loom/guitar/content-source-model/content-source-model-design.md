---
type: design
id: de_01KVZ4XQ47ZZ3DWFR65Z5AW1RH
title: Multi-source content model (additive listing, source tags, filter)
status: done
created: 2026-06-25
updated: 2026-06-25
version: 4
idea_version: 2
tags: []
parent_id: id_01KVZ3QQ26RZE2H32VXQWKXFND
requires_load: []
---
# Multi-source content model (additive listing, source tags, filter)

Design for the [[content-source-model]] idea. Resolves the three open questions (edit/delete/revert, cross-source identity, filter UI) and proposes a concrete model. **Status: decisions locked** — D1 (unify), D3/D4 (fork-on-edit), D5 (transient) all confirmed by Rafa. Requirements in `content-source-model-req` (locked).

Refs loaded: `chordflow-architecture-reference` (§3 Persistence, §5 bridge). Refs to update when this lands: `chordflow-architecture-reference` (the source model + tags), `chordflow-domain-model-reference` (only if voicing source wording moves — mostly the sibling thread's job).

---

## 1. Problem

The content layer **hides sources**. `IContentStore.List()` runs `ContentSummaries.Build`, which collapses the tiered rows to **one winning row per id** via `OriginResolver` (`UserDefined > Pack > BuiltIn`). So a user edit of a built-in *replaces* it in the list, and a pack item shows only its tier (`BuiltIn`/`Pack`), never *which* pack. Rafa's rule: **a source must never hide another** — the engine (`automatic`), every package (by name), and the user's own content all coexist in the list, tagged and filterable. This is the precondition for engine-derived voicings ([[engine-derived-as-app-source]]) appearing as a real source rather than silently shadowing.

## 2. Current state (grounded)

- **Storage:** every content entity (`Progression`/`Song`/`Rhythm`/`Voicing`) has a composite PK **`(Id, Origin)`** (`ChordFlowDbContext` §OnModelCreating) — tiered copies of one id physically coexist. Each row already carries a **`PackId`** column (`ICatalogEntity.PackId`), set by `PackImporter` from the manifest id for `Pack` imports, **null** for `BuiltIn`/`UserDefined`.
- **`Origin`** = `{ BuiltIn, UserDefined, Pack }`. The **default pack imports as `BuiltIn`** (`DefaultPack.ImportInto` → `Import(Load(), Origin.BuiltIn)`), PackId null — even though its manifest is `{ id: "default", name: "ChordFlow Starter" }`. So today there is no stored link from a default-pack row back to its pack name.
- **Resolution:** `OriginResolver.Rank` (`UserDefined 2 > Pack 1 > BuiltIn 0`); `Resolve` (list) and `ResolveOne` (single) pick the highest tier per id; non-destructive (delete a shadow → lower tier wins next read).
- **CRUD writes only `UserDefined`** (`VoicingStore.Save` etc.): editing a built-in/pack id writes a same-id `UserDefined` **shadow**; delete removes the user row → `Deleted` or `Reverted`.
- **Bridge DTO:** `ContentItem(Id, Name, Origin, HasLowerTier, InitialKey)` (Origin as a string). **JS** (`content-crud.js`) renders a badge: `origin === "UserDefined" ? "User" : origin` and labels the destructive button `"Revert to default"` vs `"Delete"` by `hasLowerTier`. No filter control exists.

## 3. Target model

Three **sources** (provenance), all visible side by side per kind:

| Source | What | Stored? | Tag shown |
|--------|------|---------|-----------|
| `automatic` | engine-derived (voicings only, for now) | no — computed, always fresh | `automatic` |
| `package` | any content pack, incl. the default | yes (SQLite, seeded from `.dsl` via `PackImporter`) | the pack's **name** (e.g. *ChordFlow Starter*, *Swing*) |
| `user` | user-authored | yes (SQLite) | `user` |

Listing is **additive**: one row per item per source, each tagged; a source filter narrows the view. Resolution (which grip *plays*) is **not** this thread — hand-picked items are source-qualified by the pick; bulk voicing comping is main-source + fallback ([[engine-derived-as-app-source]]).

## 4. Key decisions (with recommendations)

### D1 — Collapse `BuiltIn` into `package` (recommended)
The default pack is conceptually just *a package* (it even has a manifest id/name). Today it's special-cased as `BuiltIn`/null-PackId, which is exactly why it can't show a package name. **Recommend:** import the default pack as **`Origin.Pack` with `PackId = "default"`** (its manifest id), and **retire `BuiltIn`** (or keep the enum member vestigially during migration). Then provenance is cleanly two stored kinds — `Pack` (carrying which pack) and `UserDefined` — plus the computed `automatic`. The UI maps `Pack` → the pack's display name (resolve `PackId` → manifest name), `UserDefined` → `user`.
- *Trade-off:* a one-time migration/re-import (BuiltIn rows → Pack rows with PackId "default"). Cheap — content regenerates from the on-disk pack anyway; the DB is a cache.
- *Alternative (smaller):* keep `BuiltIn`, and in the read path map `BuiltIn` → the default pack's name. Works, but leaves a permanent special case and a null PackId that means "the default pack." I prefer D1's clean unification (durable-over-minimal).

### D2 — Additive listing (the core change)
`IContentStore.List()` stops collapsing: return **one `ContentSummary` per (id, source)**, not per id. Drop the `OriginResolver` call in `ContentSummaries.Build` for the list path (resolver stays for any remaining bare-id resolve). `ContentSummary` (and the `ContentItem` DTO) gain a **`source`** field (`package`/`user`/`automatic`) and a **`packName`** (null unless `package`). `HasLowerTier`/`Reverted` are removed or repurposed (see D4).

### D3 — Identity: fork-on-edit, so every row is uniquely addressable (recommended)
The thorny case is "same id across sources" — today a user edit reuses the built-in's id (to shadow it). Under no-hiding that produces two visible rows with the *same id*, which makes selection ambiguous. **Recommend:** **editing a package item forks a NEW `user` item with a fresh id** (a copy), instead of writing a same-id shadow. Consequences:
- Every listed item has a **unique id** → selection is naturally source-qualified, no precedence needed anywhere in the *listing/selection* path.
- The package original is **never hidden** — it stays in the list; the user copy is a separate, additional item. This *is* the no-hiding rule, structurally enforced.
- **"Revert" disappears** — there's nothing to revert *to* because nothing was overridden; the user just deletes their copy. The destructive button is always plain "Delete" (only on `user` items).
- *Trade-off:* exercises that referenced the built-in id keep using the original, **not** the user's edit (the edit is a new item). Under no-hiding that's correct — a user edit doesn't silently rewrite every exercise; to use it you select it. This drops the current "override a built-in everywhere" behavior, which is the behavior Rafa is explicitly rejecting.
- *Alternative:* keep same-id shadows but show both rows (source-qualified selection must then carry `(id, source)`). More faithful to today's storage, but keeps precedence semantics alive and complicates selection. I prefer fork-on-edit — it makes "no hiding" a structural property, not a runtime policy.

### D4 — Delete/revert under D3
With fork-on-edit: `package` items are read-only (can't edit-in-place, can't delete — they belong to the pack; managing packs is separate). `user` items: edit-in-place (same id) and Delete (gone). `DeleteOutcome.Reverted` is removed. Editing a `package` item in the UI = "Duplicate to user" (creates a `user` copy opened for editing).

### D5 — Tag + filter UI
- **Badge:** replace `origin === "UserDefined" ? "User" : origin` with the source tag — `user` / `automatic` / the pack name. Distinct colours per source-kind (user, automatic, package), pack name as the package-kind label.
- **Filter:** a source filter (chips/checkboxes) above each list — toggle `automatic` / `user` / each package. Default: all on. `automatic` chip appears only for voicings (the only kind with a computed source for now). *Open:* persist the filter (AppSettings) or reset per page load — I lean **transient** (per load), matching the comping-picker precedent.

### D6 — Source aggregation seam (so `automatic` can join)
`automatic` has no store rows. The listing for a kind becomes: **store rows (package + user) ∪ computed rows (automatic, voicings only)**. The handler (`ContentCrudHandler.List`) gains a small aggregation: ask the store for its rows, and — for voicings — ask the computed voicing source for its `automatic` items, union, return. The computed source itself (its synthetic ids, its `Derive` calls) is built in [[engine-derived-as-app-source]]; **this thread just defines the union point** so the listing contract already accommodates a non-store source.

## 5. Scope

**In:** D1 (default pack → `Pack`/`PackId="default"`, retire/quarantine `BuiltIn`) · D2 (additive `List()`, `ContentSummary`/`ContentItem` gain `source` + `packName`) · D3/D4 (fork-on-edit, drop shadow/revert, "Duplicate to user") · D5 (source badge + filter UI on all 4 views) · D6 (the union seam, empty of `automatic` until the sibling thread fills it) · the migration/re-import.
**Out:** the engine computed voicing source + `ChordShape→Voicing` + main-source/fallback resolution + ranking ([[engine-derived-as-app-source]], [[voicing-ranking-strategies]]) · engine derivation for songs/progressions/rhythms (none exists).

## 6. Blast radius (files)

- `Persistence/Origin.cs`, `OriginResolver.cs` — D1 enum change; resolver kept for any bare-id resolve, dropped from list.
- `Persistence/IContentStore.cs` — `ContentSummary` + `ContentSummaries.Build` (no collapse; add `source`/`packName`); `DeleteOutcome` trim.
- `Persistence/*Store.cs` (×4) — `List()` returns per-(id,source) rows with source/packName; `Save` fork-on-edit for package items; resolve PackId→name.
- `Features/Packs/DefaultPack.cs`, `PackImporter.cs` — default pack imports as `Pack`/`PackId="default"`; migration of existing BuiltIn rows.
- `Features/ContentCrud/ContentCrudHandler.cs` + `ContentCrudEnvelopes.cs` — `ContentItem` gains `source`/`packName`; `List` aggregation seam (D6); "Duplicate to user".
- `wwwroot/content-crud.js` — source badges + filter control; "Duplicate to user" for package items; drop "Revert".
- `generate`/selection path — Practice pickers send a source-qualified selection where ambiguity is possible (mostly moot once ids are unique under D3).
- Tests: `OriginResolverTests`, `ContentCrudStoreTests`, `*PersistenceTests`, `PackImportTests`, `DefaultPackVoicingsTests` — updated for the new model.

## 7. Decisions — RESOLVED (Rafa, chat-001)

1. **D1 — unify.** Default pack imports as `Origin.Pack` / `PackId="default"`; `Origin.BuiltIn` retired.
2. **D3/D4 — fork-on-edit.** Editing a `package` item creates a new `user` copy (fresh id); no same-id shadow, no "Revert"; package items read-only with "Duplicate to user".
3. **D5 — transient.** The source filter resets per page load (not persisted).

## 8. Validation

- All sources for a kind list side by side; each row tagged (`user` / `automatic` / pack name); no row hides another.
- Editing a package item creates a distinct `user` copy; the package original remains listed.
- The source filter narrows correctly per kind; `automatic` appears only for voicings.
- The default pack shows as its manifest name (*ChordFlow Starter*), not "BuiltIn".
- **Dogfood:** the Content page renders the tagged, filterable, un-collapsed rows — the visible proof of the model.
