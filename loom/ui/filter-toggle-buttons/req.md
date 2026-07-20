---
type: req
id: rq_01KXZNQVCWE87Q5VC5KKPATRPR
title: Shared faceted FilterR + genre/subgenre/tags surfacing — Requirements
status: locked
created: 2026-07-20
updated: 2026-07-20
version: 1
design_version: 1
tags: []
parent_id: de_01KXZNQA2JCG9QBWKV84ZDB7XM
requires_load: []
---
# Shared faceted FilterR + genre/subgenre/tags surfacing — Requirements

### ✅ Included

- `IN1` — `ContentSummary` carries `Genre` (string?), `Subgenre` (string?) and `Tags` (list); each content store's `List()` populates them — catalog entities (Progression/Song/Voicing/Drums) from their denormalized `ICatalogEntity` columns, the rhythm store as null/empty.
- `IN2` — the `entityList` reply carries `genre`/`subgenre`/`tags` per item over the bridge.
- `IN3` — a shared **FilterR** component (`filter-render-component.js`, `window.ChordFlowFilter`): a faceted toggle-chip stack driven by a `levels` config (each level `{ key, label, mode, chips }`) that emits the enabled-token sets via `onChange`, plus `setLevels`/`getState`/`dispose`.
- `IN4` — Content pages (Songs/Progressions/Voicings/Rhythms) render genre/subgenre/tags on each list row and filter via FilterR, client-side; the Genre/Subgenre/Tags chip values are discovered from the listed rows, and the existing Source filter is folded in as a FilterR level.
- `IN5` — the Voicings page's existing Source/Family/3rd/5th/7th filter stack is rendered through FilterR, with its server-side `voicingGrid` round-trip behavior unchanged.
- `IN6` — the Practice page gains a single genre/subgenre/tags filter strip that narrows the option lists of the metadata-bearing pickers — **Harmony (Song/Progression)** and **Drums (DrumGroove)** — client-side; source is always all.
- `IN7` — `chordflow-architecture-reference.md` is updated in the same unit of work to list FilterR in the UI dumb-views roster.

### ⛓ Constraints

- `C1` — FilterR is a **dumb view**: no music theory, no data source, no filtering logic — it renders chips and reports enabled sets; each consumer owns its data and its filter behavior (the FretR/ChordSheetR/PlayerControlsR pattern).
- `C2` — filter semantics are **OR within a level, AND across levels** (matching GuitarVoicingsR); an empty match yields an empty list, never an error.
- `C3` — Content-page and Practice-page filtering is **client-side** over the already-returned `entityList` rows — no new bridge round-trip is introduced for them.
- `C4` — `RhythmPatternEntity` metadata is unchanged (EX3 preserved): Comping and Lead pickers are not narrowed by the Practice strip.
- `C5` — the two existing filters keep their current behavior after folding onto FilterR: the Content Source filter stays client-side and the Voicings stack stays the server round-trip.

### ❌ Excluded

- `EX1` — the Voicings-page genre/subgenre/tags filter axis (inert until stored `package`/`user` voicings enumerate into the grid — deferred to its own thread).
- `EX2` — catalog metadata for rhythm patterns / genre-filtering the Comping and Lead pickers (reverses the current EX3 no-metadata decision; schema change + migration — deferred to its own thread).
- `EX3` — editing genre/subgenre/tags in the UI; metadata stays authored via the DSL header. This feature only surfaces and filters on it.
