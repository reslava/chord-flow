---
type: done
id: pl_01KXZP3GCMNSAA61SNJV362WV9-done
title: Done — Shared FilterR + genre/subgenre/tags surfacing
status: done
created: 2026-07-20
version: 6
tags: []
parent_id: pl_01KXZP3GCMNSAA61SNJV362WV9
requires_load: []
---
# Done — Shared FilterR + genre/subgenre/tags surfacing

## Step 1 — Carry genre/subgenre/tags on ContentSummary + entityList: extend ContentSummary and ContentSummaries.Build, populate from each store's List() (Progression/Song/Voicing/Drums from ICatalogEntity columns; Rhythm null/empty), and add the fields to the entityList reply.

Genre/subgenre/tags now flow from the stores to the `entityList` wire.

- **`ContentSummary`** (IContentStore.cs) gains `Genre` / `Subgenre` / `Tags` (trailing optional params, back-compat).
- **`ContentSummaries.Build`** signature extends its row tuple with a `CatalogMetadata Meta` and sets the three fields from it — per-row (so a fork shows its own inherited header), not tier-collapsed.
- **Design call:** metadata is read from each row's **DSL header** (`CatalogHeader.Parse(row.Dsl).Metadata`), the canonical source — *not* the denormalized `ICatalogEntity` columns, because `Save` doesn't populate those columns on user rows (they'd be empty for forks/user content). This matches how `ProgressionStore` already reads `tonality:`.
- Each store's `List()` supplies `Meta`: Progression/Song/Voicing/Drums parse their row's header; **RhythmPatternStore** passes `CatalogMetadata.Empty` (EX3 — rhythms aren't genre-filtered).
- **`ContentItem`** (ContentCrudEnvelopes.cs) + **`ToItem`** (ContentCrudHandler.cs) carry the fields onto the wire (`Tags ?? Array.Empty<string>()`).

Tests: 2 added to `ContentCrudStoreTests` — progression surfaces genre/subgenre/tags from its header (and null/empty without one); rhythm carries none. Full store suite green (27). Solution builds clean.

## Step 2 — Extract the dumb FilterR component (filter-render-component.js, window.ChordFlowFilter): a faceted toggle-chip stack driven by a levels config, emitting enabled-token sets via onChange, with setLevels/getState/dispose. No data source, no filtering logic, no music theory. Load it in index.html.

Extracted the dumb **FilterR** — `wwwroot/filter-render-component.js`, `window.ChordFlowFilter`.

- `create(container, { levels, onChange, hideSingleChoiceLevels? }) → { setLevels, getState, render, dispose }`.
- `levels` = `[{ key, label, mode?, chips: [{token, label}] }]`; `onChange(enabledByKey)` fires each toggle with `{ [key]: Set<token> }` (the enabled tokens present per level). `getState()` returns the same shape.
- **Semantics kept in the component, dumb-ly:** "default on, sticky off" — it tracks only the tokens the user explicitly turned *off* (per level), so a narrowed filter survives `setLevels` and newly-appearing values arrive on. Filtering logic itself (OR-within / AND-across / all-on-unconstrained) stays the consumer's job (C1) — the convention is documented in the header.
- `mode:"single"` gives a radio-like level (used by none of steps 3–5, but there for generality); default multi.
- `hideSingleChoiceLevels` (default true) drops a level with <2 chips — so the Content Rhythms tab, which only ever has a Source level, shows nothing to narrow when there's a single source.
- Own scoped CSS (`.cf-*`), reusing the app's dark pill look (`.gv-chip`/`.cc-chip`).
- Loaded in `index.html` after `fretboard-render-component.js`, before its consumers (content-crud / guitar-voicings / app).

Node `--check` passes; solution still builds (wwwroot is copied, not compiled).

## Step 3 — Content pages adopt FilterR: render genre/subgenre/tags on each list row, mount FilterR with the Source level folded in + Genre/Subgenre/Tags levels discovered from the listed rows, and filter client-side with OR-within/AND-across semantics (empty match → empty list).

Content pages (`content-crud.js`) now show the metadata and filter through FilterR.

- Dropped the bespoke source-only filter (`activeSources`/`knownKeys`/`renderFilter`) and its per-toggle bookkeeping — that lives in FilterR now.
- **`buildLevels(list)`** derives the levels from the rows present: **Source** (automatic/user/packs, same tokens as before via `filterKey`) + **Genre** + **Subgenre** + **Tags**, each discovered from the list via `distinct()`. Rhythm rows carry no metadata, so those three come out empty and FilterR hides them → the Rhythms tab keeps just Source (and hides even that when there's a single source).
- **`renderList`** creates the FilterR once per entity (mounts into `#ccFilter`) and `setLevels` on refresh (preserving sticky-off selections); the filter is disposed + rebuilt all-on on entity switch (the transient-per-entity behavior, content-source-model D5).
- **`applyFilter`** implements the shared semantics client-side (C3): OR within a level (`valuesFor(it, key).some(...)`), AND across levels (`currentLevels.every(...)`), a level with all chips on (or a single/empty level, `set.size === chips.length`) treated as unconstrained so rows with no value still show, and an emptied level → empty list, never error (C2).
- Each row gained a **meta line** (`renderMeta`): "Genre · Subgenre" + tag pills, omitted for rows without metadata. New `.cc-row-top` / `.cc-meta` / `.cc-meta-genre` / `.cc-tag` CSS in index.html; the `li` is now a column.
- Empty-state copy generalized ("No definitions match the filter").

Node `--check` passes. (The old `.cc-chip` CSS in index.html is now dead but harmless — left in place; FilterR ships its own `.cf-*` styles.)

## Step 4 — Fold GuitarVoicingsR's existing Source/Family/3rd/5th/7th stack onto FilterR — chip rendering moves into the shared component; the server-side voicingGrid round-trip behavior is byte-for-byte unchanged. The g/s/t axis is NOT added here (EX1).

GuitarVoicingsR now renders its Source/Family/3rd/5th/7th stack through the shared FilterR — wire behavior byte-for-byte unchanged.

- `LEVELS` converted from `{t,l}` to FilterR's `{token,label}` shape; the local `enabled` Set-map + `buildLevel()` deleted.
- `build()` mounts `ChordFlowFilter.create(filters, { levels: LEVELS, onChange: sendQuery })`.
- `sendQuery()` reads `filterR.getState()` and spreads each level's Set into the `voicingGrid` arrays exactly as before: all-on ⇒ full token sets (server treats as everything), a level emptied ⇒ `[]` (admits nothing ⇒ empty grid). The **g/s/t axis is NOT added here** (EX1 — deferred; the grid only lists engine-derived `automatic` cells, which have no catalog metadata).
- Removed the now-dead `.gv-level`/`.gv-chip` CSS (FilterR ships `.cf-*`); `.gv-filters` kept as the mount wrapper. `dispose()` disposes the FilterR.
- Root stays a dropdown (single global root), not a chip level — unchanged.

Node `--check` passes.

## Step 5 — Practice page gains a single genre/subgenre/tags filter strip that narrows the metadata-bearing pickers — Harmony (Song/Progression) + Drums (DrumGroove) — client-side over their entityList payloads; source always all; Comping/Lead untouched.

The Practice page gained a genre/subgenre/tags filter strip (`app.js` + a `#practice-filter` mount in index.html) that narrows the metadata-bearing pickers only.

- New `#practice-filter` div above `#harmony-controls`; a `ChordFlowFilter` is created there during boot (right after HarmonyControlsR), `onChange: applyPracticeFilter`.
- **`buildPracticeLevels()`** discovers Genre/Subgenre/Tags from the union of `catalog.song + catalog.progression + catalog.drums` — the metadata-bearing pickers (Harmony + Drums). **Rhythm (Comping/Lead) is excluded** (C4/EX3). Source is always all (not a level).
- **`onCatalogList`** now: caches → `refreshPracticeLevels()` (rebuild chips + `setLevels`, sticky-off preserved) → `feedHarmony(entity)` (feeds that entity into `hc.setCatalog` narrowed by the current filter; rhythm passed through full).
- **`feedHarmony` / `passesPracticeFilter`**: client-side (C3), same OR-within / AND-across / all-on-unconstrained predicate as the Content page (duplicated deliberately — the locked req C1 keeps filtering logic in the consumer, not FilterR). `applyPracticeFilter` re-feeds song/progression/drums on a toggle.
- `HarmonyControlsR.setCatalog` preserves the current selection when it survives the filter (its `prev`-value logic), so a filter narrows the picker without disrupting an unaffected selection; if the current harmony is filtered out it falls back — expected.

Node `--check` on app.js passes; full solution build + **1149** Core tests green.

## Step 6 — Update chordflow-architecture-reference.md to list FilterR in the UI dumb-views roster (§2 solution shape + §7 diagram), per the ref-sync rule.

`chordflow-architecture-reference.md` synced (IN7):

- §2 solution-shape `wwwroot/` listing now names `filter-render-component.js` (FilterR — the shared dumb faceted toggle-chip filter, mounted by the Content pages, GuitarVoicingsR, and the Practice content filter).
- §7 "UI (JS) — dumb views" diagram box gains the `filter-render-component (shared faceted chip filter)` row alongside its siblings.

Edited via `loom_patch_doc` (the ref is gate-excluded but versioned; patching keeps frontmatter consistent).
