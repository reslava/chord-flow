---
type: design
id: de_01KXZNQA2JCG9QBWKV84ZDB7XM
title: Shared faceted FilterR + genre/subgenre/tags surfacing
status: done
created: 2026-07-20
version: 1
idea_version: 1
tags: []
parent_id: id_01KXZNP7ATPDA8Z1EG2SWFA7YW
requires_load: []
---
# Shared faceted FilterR + genre/subgenre/tags surfacing

## 1. Context

The data (`CatalogMetadata`: genre/subgenre/tags/description/tonality) already exists on the Entity layer, denormalized into `ICatalogEntity` columns (`Genre`/`Subgenre`/`Tags`) on Progression / Song / Voicing / **Drums** entities, parsed from the DSL header by `CatalogHeader`. It is **not** carried by `ContentSummary` (the `entityList` wire row), so no list UI can see it. `RhythmPatternEntity` deliberately carries **no** catalog metadata (EX3).

Two faceted filters exist and differ in *where filtering runs*:
- **GuitarVoicingsR** (`guitar-voicings-render-component.js`): server-side — enabled-token sets round-trip via `voicingGrid`, the engine derives+realizes the filtered grid.
- **content-crud.js Source filter**: client-side — chips over the already-listed rows, `applyFilter()` in JS.

## 2. Goal & non-goals

**Goal:** surface genre/subgenre/tags, and unify the *look* of faceted filtering behind one dumb, reusable **FilterR** — without collapsing the two legitimately-different filtering mechanisms into one.

**Non-goals / deferred (own threads):** the Voicings g/s/t axis (needs stored-voicing enumeration), rhythm-pattern catalog metadata (reverses EX3), and any editing of genre/subgenre/tags in the UI (metadata stays authored via the DSL header — this feature only *surfaces + filters*).

## 3. FilterR — the dumb presentational component

New `wwwroot/filter-render-component.js`, `window.ChordFlowFilter`. The toggle-filter twin of FretR/ChordSheetR/PlayerControlsR: it owns the chip stack + enabled-set state + emits changes, and **nothing else** — no data source, no filtering logic, no music theory (constraint C1).

```
ChordFlowFilter.create(container, {
  levels: [{ key, label, mode: "multi" | "single", chips: [{ token, label }] }],
  onChange(enabledByKey)   // { [key]: Set<token> }  — fired on every toggle
}) -> { setLevels(levels), getState(), dispose() }
```

- **`setLevels`** re-renders the stack (content pages call it whenever the discovered facet values change — e.g. after an `entityList` refresh).
- **`mode`**: `single` (Root-style radio — at most one active) / `multi` (default — a Set). Reproduces both GuitarVoicingsR's Root(single)+facets(multi) needs.
- Styling reuses the existing `.gv-chip` / `.cc-chip` visual (pill, active = accent) so the three surfaces look identical — extracted into the component's own scoped CSS.
- All-on (or empty selection) semantics are the **consumer's** call in `onChange`; FilterR only reports the enabled sets.

## 4. Data plumbing (foundational — everything builds on it)

1. **`ContentSummary`** gains `Genre` (string?), `Subgenre` (string?), `Tags` (IReadOnlyList<string>).
2. **`ContentSummaries.Build`** signature extends to carry those from the row projection; each store's `List()` passes them:
   - Progression / Song / Voicing / Drums stores read the denormalized `ICatalogEntity` columns directly (cheap — no DSL re-parse; `Tags` via `CatalogHeader.DeserializeTags`).
   - Rhythm store passes null/empty (no metadata — EX3 preserved).
3. **`entityList` reply** (ContentCrud envelope) carries `genre`/`subgenre`/`tags` per item.
4. **content-crud.js** renders the fields on each list row.

## 5. Filtering per surface

**Content pages** — client-side, no new round-trip (C3): the `entityList` reply already returns every row.
- Fold today's Source chips into FilterR as one level; add Genre/Subgenre/Tags levels whose chip values are **discovered from the listed rows** (distinct present values only — so Rhythms, with no metadata, shows only Source).
- `onChange` runs `applyFilter()` over the in-memory list with **OR within a level, AND across levels** semantics (matching GuitarVoicingsR — C2); empty result → empty list, never an error.

**Voicings page** — fold the existing Source/Family/3rd/5th/7th stack onto FilterR; `onChange` keeps issuing the `voicingGrid` round-trip. Behavior byte-for-byte unchanged; only the chip-rendering moves into the shared component. The g/s/t axis is **not** added here (deferred).

**Practice page** — a single filter strip above the definition controls that narrows the **metadata-bearing** picker options: **Harmony** (Song/Progression) + **Drums** (DrumGroove). Client-side over each picker's `entityList` payload; source always all. **Comping/Lead** (rhythm-backed, no metadata) are **not** narrowed. Facet values discovered from the union of the harmony + drums lists.

## 6. Architecture ref (required, same unit of work)

`chordflow-architecture-reference.md` gains FilterR in the UI dumb-views roster (§2 solution shape + §7 diagram) — the ref-sync rule.

## 7. Sequencing

1. `ContentSummary` + `ContentSummaries.Build` + each store's `List()` + the `entityList` reply carry genre/subgenre/tags (with store-level tests).
2. Extract the dumb `FilterR` component (`filter-render-component.js`).
3. Content pages: render the fields on rows + adopt FilterR (Source folded + g/s/t levels), client-side filtering.
4. Voicings page: fold its existing stack onto FilterR (no behavior change).
5. Practice page: the g/s/t strip narrowing Harmony + Drums pickers.
6. Update `chordflow-architecture-reference.md`.

## 8. Testing

- Core: store `List()` tests asserting genre/subgenre/tags populate for catalog entities and stay empty for rhythms; the `entityList` envelope carries them.
- JS: FilterR is a dumb view (no engine) — the OR-within/AND-across filter predicate lives in each consumer and is the unit worth a focused check; visual parity confirmed by dogfooding each page.
