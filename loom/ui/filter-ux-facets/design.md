---
type: design
id: de_01KY045E0MSF9W45T6TZP5JAKV
title: Filter UX — hierarchical facets + quick all/none + result counts
status: done
created: 2026-07-20
version: 1
idea_version: 1
tags: []
parent_id: id_01KY044A77H3YC3WTEN9WWS45X
requires_load: []
---
# Filter UX — hierarchical facets + quick all/none + result counts

## 1. Context

FilterR (from [[filter-toggle-buttons]]) is a flat, dumb faceted chip stack: levels are independent, each all-on with sticky-off, `<2`-chip levels hidden. Two consumers filter client-side (Content list, Practice strip); GuitarVoicingsR filters server-side. This thread adds the hierarchical cascade, quick All/None, and result counts — keeping FilterR dumb and putting the cascade recompute in the consumers.

## 2. FilterR contract changes (still presentational)

Chips gain optional per-value state; levels stay `{ key, label, mode, chips }` but a chip becomes `{ token, label, count?, disabled?, selected? }`:

- **`count`** — rendered as a suffix ("Blues (3)"); omitted ⇒ no count shown.
- **`disabled`** — greyed, non-clickable (the zero-count cascade state).
- **`selected`** — the chip's initial on/off, so a consumer can hand FilterR the derived availability directly (replaces the internal all-on default when supplied).

New/changed handle surface:
- `create(container, { levels, onChange, showAllNone? })` — `showAllNone` (default true) renders an **All · None** pair per level.
- `setLevels(levels)` now takes the fully-specified chips (count/disabled/selected) and renders them as given — it **no longer owns the default-on/sticky-off logic** (that moves to the consumer's cascade). A disabled chip never emits and never counts as selectable.
- `onChange(enabledByKey)` unchanged in shape — `{ [key]: Set<token> }` of the *enabled, non-disabled* tokens.
- All/None on a level sets/clears that level's selectable chips, then emits.

**Migration note:** the old "default on, sticky off" internal state is removed. Both existing consumers must now compute selection themselves (they already hold the items), so behavior is driven entirely by the levels they pass. This is a breaking change to FilterR's internal model but not its mount signature.

## 3. The cascade (consumer-side, shared shape)

A small helper each client consumer uses (duplicated or a tiny shared `filter-cascade.js` util — **decision D1**, see §6): given the ordered levels' *accessors* and the item list, compute per level:

```
for each level L in order [source, genre, subgenre, tags]:
  itemsPassingHigher = items filtered by the CURRENT selection of levels above L
  for each distinct value v of L (across ALL items, so the vocabulary is stable):
     count = |itemsPassingHigher having v|
     available = count > 0
     selected = available && (previously-selected-or-newly-available)
  chips = values sorted, each { token:v, label:v, count, disabled:!available, selected }
```

When a level changes, everything below recomputes and **resets to all-available-selected** (the agreed model — Rafa: "the level+1 show selected for items that have elements, unselected for items with no elements"). The value *vocabulary* per level stays the full distinct set (so a value greys rather than vanishing); only availability/selection changes.

Filtering itself is unchanged (OR within a level over selected tokens, AND across levels); with the cascade, a level's selected set already reflects availability.

## 4. Per-surface

- **Content pages (`content-crud.js`):** replace the flat level build + `applyFilter` all-on check with the cascade; show **per-chip counts**; show **"N of M"** above/below the list. Source stays the top of the chain.
- **Practice (`app.js`):** the strip cascades over the union of the metadata-bearing pickers (Harmony songs+progressions + Drums); show the grand total as "showing N of M" for the narrowed options. (No Source level here — source is always all.)
- **Voicings grid (`guitar-voicings-render-component.js`):** gets **All/None** (pure UI) only. **No cascade, no counts** — its facets are server-filtered over a fixed vocabulary; counts/availability would need a `voicingGrid` reply extension (EX1, deferred).

## 5. Counts source

Per-chip counts and the grand total are pure client-side derivations of the item set already in hand — **no bridge changes** for Content/Practice. (Voicings would need engine support — excluded.)

## 6. Decisions

- **D1 — cascade helper placement:** a shared `filter-cascade.js` (pure: items + accessors → levels) vs duplicated per consumer. Lean **shared pure util** — it's data logic, not view, so it doesn't muddy FilterR's dumb-view contract, and it kills the drift risk between the Content and Practice cascades. (FilterR stays purely presentational; the util is separate.)
- **D2 — All/None on disabled chips:** "All" selects only *available* chips (never the greyed ones). Confirmed by the greyed-and-disabled rule.

## 7. Sequencing

1. FilterR: chip `count`/`disabled`/`selected` rendering + per-level **All/None** + drop the internal default-on/sticky-off model.
2. Shared `filter-cascade.js` pure util (+ a focused unit check of the cascade counts/availability).
3. Content pages: adopt the cascade + counts + "N of M".
4. Practice strip: adopt the cascade + grand total.
5. Voicings grid: All/None only.
6. Architecture ref touch if the util is a new shared file worth listing.

## 8. Testing

- The cascade util is pure and unit-testable in isolation (Node): counts, availability, reset-on-higher-change. This is the one piece with real logic; FilterR and the wiring are dumb/visual (dogfood).
