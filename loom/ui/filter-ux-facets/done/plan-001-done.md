---
type: done
id: pl_01KY04HM48W11PCTPWY0BB6ARW-done
title: Done — Filter UX — hierarchical facets, all/none, counts
status: done
created: 2026-07-20
version: 6
tags: []
parent_id: pl_01KY04HM48W11PCTPWY0BB6ARW
requires_load: []
---
# Done — Filter UX — hierarchical facets, all/none, counts

## Step 1 — Rewrite FilterR's chip model: chips become {token,label,count?,disabled?,selected?} (selected defaults on) — render the count as a suffix, a disabled chip greyed + non-clickable + never selected; add a per-level All/None control (showAllNone, default on; All selects only available chips); onChange(enabledByKey, changedKey); drop the internal default-on/sticky-off state; clone chips so caller objects aren't mutated.

FilterR rewritten to the richer chip model.

- Chips are now `{ token, label?, count?, disabled?, selected? }` — `count` renders as a "(n)" suffix, `disabled` greys + blocks clicks + excludes from selection, `selected` defaults true (bare chips ⇒ all-on, so GuitarVoicingsR keeps working).
- Per-level **All · None** control (`showAllNone`, default on); **All selects only non-disabled chips** (C4).
- `onChange(enabledByKey, changedKey)` — the changed level's key lets a consumer reset the levels below it (the cascade).
- Dropped the internal default-on/sticky-off model + `hideSingleChoiceLevels` (the cascade owns visibility/selection now; a single-value optional facet renders instead of hiding — IN7).
- Chips are **cloned** on create/`setLevels`, so a caller's level objects (e.g. GuitarVoicingsR's module-constant LEVELS) are never mutated.
- `getState()` returns selected ∧ ¬disabled tokens per level.

Node `--check` passes. (Content/Practice still use the old-style calls until steps 3–4; not shipped mid-plan.)

## Step 2 — Add the shared pure filter-cascade.js util (browser + node compatible): given items + ordered level defs (key/label/accessor) + a per-level selected set, compute each level's chips {count, disabled(!available), selected} from the items passing the higher levels (full stable vocabulary), the filtered item set, initialSelected (all values), and resetBelow (reset lower levels to all-available on a higher change). Cover it with a node-run unit test (counts, availability, reset-on-higher-change).

Added the pure cascade util `wwwroot/filter-cascade.js` (`window.ChordFlowFilterCascade` + `module.exports`).

- `build(items, levelDefs, selected) → { levels, filtered, total }` — computes each level's chips `{count, disabled(!available), selected}` from the items passing the higher levels (stable full vocabulary), plus the filtered set. `initialSelected` (all values selected = all-on) and `resetBelow(…, changedKey)` (reset levels below the changed one to all still-available) implement the cascade-reset model.
- Filtering rule preserved: a level is unconstrained when its selected set ⊇ its available set (all-on ⇒ items with no value still pass); otherwise OR-within/AND-across.
- Browser + node dual-export so it's unit-testable (C5).
- Test `tests/js/filter-cascade.test.js` — 4 cases (all-on passes everything incl. no-value items; narrowing genre cascades + greys empty subgenres/tags + resets selection; resetBelow keeps the changed level and above; constrained level drops no-value items). Run with `node tests/js/filter-cascade.test.js` — **all assertions pass**.
- Loaded in index.html after FilterR, before the consumers.

**Note:** the JS test isn't wired into `dotnet test` (there's no JS test runner in the repo) — it's a standalone `node` script. Wiring JS tests into CI would be its own small infra task if wanted.

## Step 3 — Content pages adopt the cascade: build ordered levels (Source → Genre → Subgenre → Tags) via the util, show per-chip counts + a grand-total “N of M”, reset lower levels on a higher change; the Subgenre (and any single-value optional facet) is now visible. Replace the flat build/applyFilter.

Content pages now use the cascade.

- `filter-cascade.js` gained an optional per-def `tokenLabel(value)` so a level can label chips differently from the token (Source: `pack:default` → the pack name); test still green.
- content-crud: replaced the flat `buildLevels`/`applyFilter`/`distinct`/`valuesFor` with `CONTENT_LEVELS` (ordered Source → Genre → Subgenre → Tags defs w/ accessors) + `renderList` → `initialSelected` + `rebuildFilter` (cascade `build` → `setLevels` → `renderRows` + `renderCount`) + `onFilterChange` (adopt changed level, `resetBelow`, rebuild).
- Per-chip counts + greyed unavailable come free from the cascade; the **Subgenre facet now shows** whenever any value exists (IN7 — the single-value hide is gone).
- Added a **"N of M"** count in the Definitions heading (`#ccCount` + `.cc-count`); state var `currentLevels` → `selected` (the cascade selection), reset per entity.

Node `--check` passes.

## Step 4 — Practice strip adopts the cascade over the metadata-bearing pickers (Harmony songs+progressions + Drums) via the util, and shows the narrowed-option grand total; Comping/Lead untouched, source always all.

Practice strip now cascades (genre → subgenre → tags; no Source level — source is always all).

- Replaced `buildPracticeLevels`/`refreshPracticeLevels`/`passesPracticeFilter`/`feedHarmony`/`applyPracticeFilter` with the cascade: `PRACTICE_LEVELS` defs, `metadataItems()` (union of song+progression+drums — same object refs), `rebuildPracticeFilter` (cascade `build` → `setLevels` → feed each picker its filtered subset via a membership Set → count), `onPracticeFilterChange` (adopt changed level → `resetBelow` → rebuild).
- **Incremental-arrival handling:** `ensurePracticeSelection` + `practiceKnown` — a first-seen value defaults on, a value the user turned off stays off across catalog re-arrivals (boot sends 4 lists; the Content view also fans lists in).
- Rhythm (Comping/Lead) fed full on its own arrival — never narrowed (C4).
- Added a `#practice-filter-count` line ("N of M shown") + CSS; state `practiceLevels` → `practiceSelected`/`practiceKnown`; practiceFilter `onChange` → `onPracticeFilterChange`.

Node `--check` on app.js passes.

## Step 5 — Voicings grid gains the per-level All/None only (its chips default selected; no cascade, no counts — EX1). Pass through FilterR's new All/None; behavior otherwise unchanged (server-side voicingGrid round-trip).

Voicings grid gets the per-level All/None — for free from the rewritten FilterR (`showAllNone` defaults on). No behavioral code change was needed; the grid passes bare chips (default selected, no counts, none disabled) and keeps its server-side `voicingGrid` round-trip. Added a clarifying comment at the mount noting All/None (IN6) and that the grid has no cascade/counts (EX1 — fixed server-side vocabulary). Node `--check` passes.

## Step 6 — Sync chordflow-architecture-reference.md: add filter-cascade.js (the shared pure cascade util) beside FilterR in the wwwroot listing.

`chordflow-architecture-reference.md` synced: the wwwroot listing now describes FilterR's new capabilities (per-chip counts, greyed-unavailable, per-level All/None) and adds **`filter-cascade.js`** — the pure hierarchical-facet cascade logic behind FilterR's client-side consumers (browser+node, unit-tested). Patched via `loom_patch_doc`.
