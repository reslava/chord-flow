---
type: idea
id: id_01KVZ3QQ26RZE2H32VXQWKXFND
title: Multi-source content model (additive listing, source tags, filter)
status: done
created: 2026-06-25
updated: 2026-06-25
version: 2
tags: []
parent_id: null
requires_load: []
---
# Multi-source content model (additive listing, source tags, filter)

## Goal

Replace the collapse-to-winner content listing with an **additive multi-source model**: every content item is shown with its **source tag**, sources never hide each other, and a **filter control** narrows by source. Three sources: **automatic** (engine-derived), **package** (the default pack + future packs, *by name*), **user** (custom-written). Foundational substrate that lets the engine become a real voicing *source* ([[engine-derived-as-app-source]]) instead of a hidden oracle.

## Origin

Spun off from `guitar/engine-derived-as-app-source` (chat-001). The engine-as-source work surfaced that the current content model **hides** sources, which is incompatible with "show engine + package + user side by side."

## Why

Today (architecture ref §3 + `IContentStore.cs`):

- `IContentStore.List()` returns **one `ContentSummary` per id** — the *winning* tier; lower tiers are hidden (`ContentSummaries.Build` collapses via `OriginResolver`, `IContentStore.cs:65-83`).
- Editing a BuiltIn/Pack item writes a **UserDefined shadow** that *replaces* it in the list (the tier law, `IContentStore.cs:10-14`).
- The tag is a flat `Origin` (`BuiltIn`/`Pack`/`UserDefined`); a pack item never says *which* pack.

Rafa's rule: **a source must not hide another.** All three sources coexist in the list, tagged, filterable. This is the precondition for engine-derived voicings appearing alongside package/user voicings.

## Shape (sketch — design firms this up)

- **Additive listing.** `List()` returns one row per **(id, source)**, not per id — drop the collapse. `ContentSummary` carries the **source** (`automatic`/`package`/`user`) and, for package, the **package id/name**.
- **Source tagging end-to-end.** Thread pack identity through `PackImporter` → store row → `ContentSummary` → the `entityList` DTO → the JS list row. Tag vocabulary becomes `{package-name}` / `user` / `automatic` (replacing the flat `built-in`/`user`).
- **Filter control.** Each content view gets a source filter (chips / multi-select). `automatic` appears only for voicings; songs/progressions/rhythms have only `package`+`user` for now.
- **Edit-a-builtin re-semantics.** With no collapse, "edit a pack item" produces a `user` row that **coexists** with the pack row (both visible, both tagged) rather than silently shadowing it. Delete/revert is redefined under the new model.

## Resolution boundary (what this thread does NOT own)

This thread owns **visibility** (listing + tags + filter). It does **not** own voicing **resolution** (which grip actually plays). Resolution splits two ways, both already decided in chat:

- **Hand-picked content** (a song/progression/rhythm, or one specific voicing) is **source-qualified by the pick** — no precedence.
- **Bulk comping-voicing auto-fill** uses a **main source + `user > package > automatic` fallback** — owned by [[engine-derived-as-app-source]].

## Scope

**In:** the additive `List()` contract, the `ContentSummary` source/package fields, pack-identity threading, the `entityList` tag DTO, the source filter UI on all 4 content views, and the edit/delete/revert re-semantics under no-collapse.
**Out:** the engine computed-source union + `ChordShape→Voicing` + relocating the 36 oracle grips + the main-source/fallback resolution (all [[engine-derived-as-app-source]]); engine derivation for songs/progressions/rhythms (none exists yet).

## Open design questions (for design)

1. Exact edit/delete/revert semantics once rows coexist — does `UserDefined` still mean "shadow," or just "a user row that may share an id"? What becomes of `OriginResolver` / `HasLowerTier` / `DeleteOutcome.Reverted`?
2. Identity when the same logical item exists in more than one source — shared `id` across sources, or source-qualified ids? (Engine items use synthetic ids; see the sibling thread.)
3. Filter UI shape: per-view chips vs a shared control; default (all on?); persist (AppSettings) or reset per load?

## Validation

- All sources for a kind list side by side, each tagged with its source; no row hides another.
- A user edit of a pack item shows **both** rows, distinctly tagged.
- The source filter narrows the list correctly per kind.
- **Dogfood:** the Content page renders the tagged, filterable, un-collapsed source rows — the visible proof of the model.
