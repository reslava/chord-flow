---
type: req
id: rq_01KVZHBK35Q9AFADECZKSFWEDY
title: Multi-source content model (additive listing, source tags, filter) — Requirements
status: locked
created: 2026-06-25
updated: 2026-06-25
version: 1
design_version: 4
tags: []
parent_id: de_01KVZ4XQ47ZZ3DWFR65Z5AW1RH
requires_load: []
---
# Multi-source content model (additive listing, source tags, filter) — Requirements

Requirements for [[content-source-model]]. Anchors the design (`content-source-model-design`) and the plan. Decisions D1/D3/D5 locked by Rafa in chat-001.

### ✅ Included

- `IN1` **Additive listing.** `IContentStore.List()` returns one `ContentSummary` per **(id, source)** — no tier collapse. The per-id `OriginResolver` collapse is dropped from the list path.
- `IN2` **Source on every item.** `ContentSummary` and the `ContentItem` bridge DTO carry a `source` (`package` / `user` / `automatic`) and a `packName` (the originating pack's manifest name; null unless `source == package`).
- `IN3` **Default pack unified as a package.** It imports as `Origin.Pack` with `PackId="default"` and lists under its manifest name ("ChordFlow Starter"). `Origin.BuiltIn` is retired.
- `IN4` **Fork-on-edit.** Editing a `package` item creates a **new `user` item with a fresh id** (a copy); the package original remains listed and unchanged.
- `IN5` **Editor affordances by source.** `user` items: edit-in-place + Delete. `package` items: read-only in the editor, with a "Duplicate to user" action (creates a `user` copy opened for editing). `automatic` items: read-only (no editor).
- `IN6` **No revert.** The destructive action on a `user` item is "Delete" (the item is gone). `DeleteOutcome.Reverted` is removed.
- `IN7` **Source filter.** Each content view shows a source filter (toggle `automatic` / `user` / each package). Default: all on. **Transient** — resets per page load. The `automatic` chip appears only for kinds that have a computed source (voicings).
- `IN8` **Source-aggregation union point.** The per-kind listing unions store rows (`package` + `user`) with optional computed (non-store) rows. This thread defines the union seam; it contributes **no** `automatic` rows yet — [[engine-derived-as-app-source]] fills it.
- `IN9` **Migration.** Existing `BuiltIn` rows become `Pack` / `PackId="default"` on upgrade (acceptable alternative: wipe the content cache and re-import from the on-disk pack).

### ❌ Excluded

- `EX1` The engine computed voicing source, the `ChordShape → Voicing` adapter, main-source/fallback resolution, and ranking — [[engine-derived-as-app-source]] / [[voicing-ranking-strategies]].
- `EX2` Engine derivation for songs / progressions / rhythms (none exists).
- `EX3` Pack-management UI (installing / removing third-party packs) — only display + tagging of already-imported packs is in scope.
- `EX4` Changes to catalog-metadata filtering (genre / subgenre / tags) — the source filter is a new orthogonal axis; existing catalog filters are untouched.

### ⛓ Constraints

- `C1` `Music/` stays I/O-free; changes are confined to `Persistence/`, `Features/`, `Bridge/`, and `wwwroot/`.
- `C2` Single offline SQLite file; the content DB is a cache regenerated from on-disk packs, so a re-import is an acceptable migration path.
- `C3` The bridge contract is extended **additively** (new fields on `ContentItem`); no breaking rename of the `entity*` envelope family.
- `C4` Resolution (which item actually plays) is **not** introduced here; selection must address a specific item unambiguously — the unique ids from `IN4` make this structural.
- `C5` **Dogfood** (guitar-weave rule): the Content page renders the tagged, filterable, un-collapsed source rows.
