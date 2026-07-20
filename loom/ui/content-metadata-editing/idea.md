---
type: idea
id: id_01KY03B4BMNFT8QKXP39FBZ7XE
title: Editable genre/subgenre/tags in the Content CRUD editor
status: done
created: 2026-07-20
version: 1
tags: []
parent_id: null
requires_load: [rf_01KTSAPAT132QTEY5BEPRKS3MB]
---
# Editable genre/subgenre/tags in the Content CRUD editor

## The notion

`filter-toggle-buttons` **surfaces** genre/subgenre/tags and filters on them, but authoring them stays out of the UI — its EX3 says metadata is edited only by hand-writing the DSL header (`genre:` / `subgenre:` / `tags:`). This thread **reverses that EX3**: make genre/subgenre/tags first-class, editable fields in the Content CRUD editor so a user can classify their own content and have it show up in the filters.

## Shape

For the metadata-bearing entities (Song / Progression / Voicing / Drums — **not** Rhythm, which carries no catalog metadata, EX3 of [[rhythm-catalog-metadata]]):

- **Genre / Subgenre** — a combobox-style control (an `<input>` + `<datalist>`): the user **picks from all values already in use** across the catalog, or **types a new one**. Free text, discovered suggestions.
- **Tags** — the same idea but **multi-value**: add several, each chosen from existing tags or typed fresh; remove individually (tag-pill editor).

## Persistence — the pieces already exist

The plumbing is largely there, which is why this is a clean follow-up:

- `CatalogMetadata` + `CatalogHeader.Serialize/Parse` already round-trip the header.
- The stores already **preserve** the header across save/fork (they just never let the editor *change* it — the `EX3` guard).
- So the work is: (1) a bridge path to send edited metadata on save (extend the `entitySave` envelope with genre/subgenre/tags), (2) each store's `Save` writes the supplied metadata into the header instead of only preserving the source's, (3) the editor UI controls, (4) a way to enumerate existing values for the datalists (either derive from the `entityList` the page already holds, or a tiny catalog-values query).

## Open questions (for design)

- **Denormalized columns:** `ICatalogEntity` has `Genre`/`Subgenre`/`Tags` columns that `Save` currently doesn't populate. Editing metadata is the moment to start writing them — then `List()` could read columns instead of re-parsing headers (a cleanup of a [[filter-toggle-buttons]] step-1 decision). Decide whether to fold that in.
- **Where the datalist values come from:** reuse the already-loaded `entityList` (cheap, client-side) vs a dedicated "distinct catalog values" verb (authoritative across entities). Lean client-side first.
- **Fork-on-edit interaction:** editing metadata on a package item forks a user copy (existing content-source-model behavior) — confirm that's the intended UX for "re-tagging" a pack item.

## Validation

- Author a new user progression with genre "Blues" / subgenre "Shuffle" / tags [12-bar] → it round-trips, and immediately appears under those chips in the Content + Practice filters.
- Datalist offers every genre/subgenre/tag already present; a brand-new value is accepted and then becomes a suggestion for the next item.
