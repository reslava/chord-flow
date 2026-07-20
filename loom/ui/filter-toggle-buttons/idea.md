---
type: idea
id: id_01KXZNP7ATPDA8Z1EG2SWFA7YW
title: Shared faceted FilterR + genre/subgenre/tags surfacing
status: done
created: 2026-07-20
version: 1
tags: []
parent_id: null
requires_load: []
---
# Shared faceted FilterR + genre/subgenre/tags surfacing

## The notion

Two content surfaces already have faceted toggle-button filters, but they were hand-built independently and look/behave inconsistently:

- **GuitarVoicingsR** — a real faceted stack (Source / Family / 3rd / 5th / 7th), filtered **server-side** (the enabled-token sets round-trip via `voicingGrid`; the grid is *derived*, so there's no pre-existing list to narrow).
- **content-crud's Source filter** — a single-level chip row, filtered **client-side** over the already-returned `entityList` rows.

Meanwhile the catalog already models **Genre / Subgenre / Tags** (plus Description / Tonality) on every catalog entity — `CatalogMetadata`, parsed from the self-describing DSL header — but the list UI never surfaces it, because the `entityList` wire row (`ContentSummary`) doesn't carry it.

## What we want

1. **Surface** genre/subgenre/tags on the content lists and Practice pickers (a pure plumbing job — the data already exists).
2. **Unify** the filtering *look* behind one **shared, dumb, presentational FilterR** component — the toggle-filter twin of FretR / ChordSheetR / PlayerControlsR — that owns only the chip stack + enabled-set state + an `onChange`, and no data source and no filtering logic. Each consumer wires its own behavior (client re-filter for content, the existing server round-trip for voicings), so one component serves both idioms without forcing one mechanism onto the other.
3. Let users **filter by** genre/subgenre/tags on the Content pages (client-side) and the Practice page (narrowing picker options), and **fold** the two pages' existing filters onto FilterR.

## Per-surface behavior

- **Content (Songs / Progressions / Voicings / Rhythms):** show the fields on each row; mount FilterR with a Source level (folded in) + Genre/Subgenre/Tags levels whose chip *values are discovered from the rows present*. Client-side. Rhythms carry no catalog metadata, so its FilterR shows only Source — naturally, since FilterR renders only the levels that have values.
- **Voicings page:** fold today's Source/Family/3rd/5th/7th stack onto FilterR (behavior unchanged — still the server round-trip).
- **Practice page:** one g/s/t filter strip that narrows the **metadata-bearing** pickers — **Harmony (Song/Progression) + Drums (DrumGroove)**. Source is always "all". Comping/Lead are rhythm-backed and carry no metadata, so they stay full.

## Deferrals (captured as their own threads)

- **Voicings genre/subgenre/tags axis** — the grid only surfaces engine-derived `automatic` cells today (no catalog metadata); stored `package`/`user` voicings don't enumerate yet. The g/s/t axis there is inert until that lands, so it's deferred.
- **Rhythm-pattern catalog metadata** — `RhythmPatternEntity` deliberately carries none (EX3, "rhythm patterns aren't genre-filtered"). Genre-filtering Comping/Lead would reverse that with a schema change + migration — a separate thread ahead of any Practice-rhythm filtering.

## Validation

- Content pages filter by genre/subgenre/tags with the same OR-within / AND-across semantics as GuitarVoicingsR; Voicings + Content Source filters behave exactly as before, now via FilterR.
- Practice Harmony + Drums pickers narrow by the strip; Comping/Lead unaffected.
- `chordflow-architecture-reference.md` gains FilterR in the dumb-views roster (ref-sync rule).
