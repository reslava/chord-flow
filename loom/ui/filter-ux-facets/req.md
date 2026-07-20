---
type: req
id: rq_01KY04691YPE04WHVX9PKV286A
title: Filter UX — hierarchical facets + quick all/none + result counts — Requirements
status: locked
created: 2026-07-20
updated: 2026-07-20
version: 1
design_version: 1
tags: []
parent_id: de_01KY045E0MSF9W45T6TZP5JAKV
requires_load: []
---
# Filter UX — hierarchical facets + quick all/none + result counts — Requirements

### ✅ Included

- `IN1` — FilterR chips gain per-value `count` (rendered as a suffix, e.g. "Blues (3)"), `disabled` (greyed, non-clickable), and `selected` (initial on/off); `setLevels` renders chips exactly as given and FilterR no longer owns the default-on/sticky-off model.
- `IN2` — FilterR renders a per-level **All / None** control (opt-out via `showAllNone:false`); "All" selects only the available (non-disabled) chips.
- `IN3` — a shared, pure `filter-cascade.js` util computes, per ordered level (Source → Genre → Subgenre → Tags), each value's count + availability + selection from the items passing the higher levels; the value vocabulary per level stays the full distinct set, empty values are marked disabled, and changing a higher level re-derives the lower levels to "all still-available values selected".
- `IN4` — the Content pages adopt the cascade: per-chip counts + a grand-total "N of M" for the list; Source is the head of the chain.
- `IN5` — the Practice strip adopts the cascade over the metadata-bearing pickers (Harmony songs+progressions + Drums) and shows the narrowed-option total.
- `IN6` — the Voicings grid gains the per-level All / None control.
- `IN7` — the Subgenre facet (and any optional facet) is shown whenever at least one value exists — the single-value `hideSingleChoiceLevels` regression is gone.

### ⛓ Constraints

- `C1` — FilterR stays a dumb view: the cascade recompute lives in the shared util / consumer, not in FilterR, which only renders the chip state (count/disabled/selected) it is handed.
- `C2` — the cascade and counts for Content + Practice are pure client-side derivations of the item set already in hand — no new bridge round-trip.
- `C3` — filter semantics are unchanged (OR within a level over the selected tokens, AND across levels); the cascade's reset-on-higher-change is the agreed selection model.
- `C4` — a zero-count value is greyed-and-disabled (visible, not hidden, not clickable, never selected); "All" never selects a greyed chip.
- `C5` — the `filter-cascade.js` util is pure and unit-tested (counts, availability, reset-on-higher-change).

### ❌ Excluded

- `EX1` — the cascade + per-chip counts on the **Voicings grid** (its facets are a fixed vocabulary filtered server-side; counts/availability would need a `voicingGrid` reply extension — deferred to its own thread). The grid gets only All/None here.
- `EX2` — genre-filtering the Comping/Lead (rhythm) pickers — rhythm still carries no catalog metadata ([[rhythm-catalog-metadata]]).
- `EX3` — editing genre/subgenre/tags values ([[content-metadata-editing]]); this thread only presents and filters.
