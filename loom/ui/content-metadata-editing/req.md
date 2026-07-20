---
type: req
id: rq_01KY06P6SJDD7N31X7DGX2F1FX
title: Editable genre/subgenre/tags — widening the editor-authoritative override — Requirements
status: locked
created: 2026-07-20
updated: 2026-07-20
version: 1
design_version: 1
tags: []
parent_id: de_01KY06N5VJJC3VN7GTNNFNMMT1
requires_load: []
---
# Editable genre/subgenre/tags — widening the editor-authoritative override — Requirements

### ✅ Included

- `IN1` Editable **genre**, **subgenre**, and **tags** fields in the Content CRUD editor for the four metadata-bearing entities — Progression, Song, Voicing, Drums.
- `IN2` Genre / Subgenre are `<input>` + `<datalist>` controls: pick a value already in use across the catalog, or type a brand-new one (free text with discovered suggestions).
- `IN3` Tags are a multi-value **pill editor**: add several (each chosen from existing tags or typed fresh), remove individually.
- `IN4` Datalist suggestion values are discovered **client-side** from the already-loaded `entityList` (distinct present values across rows) — no new round-trip.
- `IN5` Save persists the edited metadata into the canonical DSL header: the `entitySave` envelope and `IContentStore.Save` gain an authoritative metadata patch, merged over the preserved header — the editor value wins for genre/subgenre/tags.
- `IN6` On Save, populate the denormalized `ICatalogEntity` `Genre` / `Subgenre` / `Tags` columns from the final merged metadata (scope option A).
- `IN7` The editor seeds its metadata controls from the **clicked `entityList` row** (which already carries genre/subgenre/tags) — no change to the `Get` / `entityLoaded` load path.
- `IN8` Edited content round-trips and **immediately appears** under its genre/subgenre/tags chips in the Content + Practice filters.
- `IN9` A present-but-empty field **clears** that metadata field; an absent patch (rhythm / programmatic caller) **preserves** the source header as today.
- `IN10` Editing metadata on a **package** item forks a user copy (content-source-model) carrying the *edited* metadata.
- `IN11` Update `chordflow-architecture-reference.md` (the `entitySave` + `IContentStore.Save` metadata contract) and fix the `ICatalogEntity` XML summary to list all four implementers — same unit of work.

### ❌ Excluded

- `EX1` Editing metadata for **Rhythm** — it carries no catalog metadata; its editor shows no metadata controls.
- `EX2` Switching `List()` to read the denormalized columns, retiring header-parse on the list path, or backfilling existing rows — deferred to its own thread; `List()` keeps parsing headers here.
- `EX3` A dedicated "distinct catalog values across all entities" server-side query verb — suggestions come client-side from `entityList`.
- `EX4` Editing **description** or **tonality** through the new metadata block — description keeps riding the header untouched; tonality keeps its existing dedicated control.

### ⛓ Constraints

- `C1` Catalog metadata stays an **Entity-layer** value — never placed on pure `Domain/` music-theory records.
- `C2` The DSL header remains the **canonical** source of metadata; the denormalized columns are written *from* the final merged metadata, not the other way round.
- `C3` Reuse the existing **tonality-override pattern** end-to-end (per-entity JS flag → `entitySave` field → `EntitySaveRequested` → handler → store field-wise merge) — no new bespoke mechanism.
- `C4` A metadata edit must not destroy the preserved **Description** or **Tonality** — merge only the three edited fields via `record with`.
- `C5` All four catalog stores behave uniformly via `IContentStore.Save`; the Rhythm store accepts the null patch inertly.
