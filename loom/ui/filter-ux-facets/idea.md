---
type: idea
id: id_01KY044A77H3YC3WTEN9WWS45X
title: Filter UX — hierarchical facets + quick all/none + result counts
status: done
created: 2026-07-20
version: 1
tags: []
parent_id: null
requires_load: []
---
# Filter UX — hierarchical facets + quick all/none + result counts

## The notion

`filter-toggle-buttons` shipped FilterR as a flat stack of independent levels (Source / Genre / Subgenre / Tags), each defaulting all-on with sticky-off. Dogfooding surfaced three UX gaps. This thread makes the filtering *legible and hierarchical*.

## Three moves

**1. Hierarchical / dependent facets (cascade).** The levels form a chain — **Source → Genre → Subgenre → Tags**. When a higher level's selection changes, each lower level is **re-derived from the items still passing the higher levels**:
- a value that still has matching items → **available + selected**,
- a value with **zero** matching items → **greyed-and-disabled** (visible, not clickable, not selected).

So changing a higher level **resets** the lower levels to "all still-available values selected" (a predictable faceted-search model, not sticky-off). This *replaces* the flat all-on/sticky-off + `hideSingleChoiceLevels` behavior — and dissolves the Subgenre-hidden issue (a facet appears whenever any value exists, even a single one, because it's no longer a <2-chip hide; a lone "Blues" shows greyed or selected per availability).

**2. Quick All / None per level.** A small "All · None" control on each level row to select/clear every (available) chip at once. Pure chip UI → lives in FilterR.

**3. Result counts.** Two granularities:
- **per-chip count** — "Blues (3)" — the number of items that value would match under the current higher-level selection (the cascade already computes this);
- **grand total** — "N of M" after the filter (the Content list length; on Practice, how many options each narrowed picker offers).

## Where each lives (FilterR stays dumb)

- **FilterR** grows: per-chip **count** display, a **disabled/greyed** chip state, per-level **All/None**, and an **initial-selection** input (so a consumer can hand it the derived availability). Still no data, no music theory.
- **The cascade recompute stays in each consumer** — it owns the items, so it recomputes each level's `{ value, count, available }` from the rows passing the higher levels and re-feeds FilterR via `setLevels`.

## Scope

- **Client-side consumers only:** the **Content pages** and the **Practice** strip, where the item set is in hand. **All/None** can also apply to the **Voicings grid** (pure UI), but the **cascade + counts on the Voicings grid are out of scope** — its facets are a fixed vocabulary filtered *server-side*, so counts/availability would need engine support (a later thread if wanted).

## Validation

- Narrow Genre → Subgenre/Tags recompute; empty values grey out; counts update.
- All/None clears or fills a level.
- The Content list shows "N of M"; each chip shows its count.
- The Subgenre facet on Progressions is visible again (greyed or selectable per availability).
