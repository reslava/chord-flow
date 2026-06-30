---
type: req
id: rq_01KWAQ3J56YTBX1X0YP1RB3X1R
title: Guitar Voicings Render Component — Requirements
status: locked
created: 2026-06-29
updated: 2026-06-29
version: 1
tags: []
parent_id: id_01KWAJXJAS34EZMZSQJVC56WC5
requires_load: []
---
# Guitar Voicings Render Component — Requirements

### ✅ Included

- `IN1` A guitar voicings render component (**GuitarVoicingsR**, `guitar-voicings-render-component.js`) that renders **many realized voicings at once** as a grid of FretR chord-boxes.
- `IN2` A new bridge verb **`voicingGrid`** (inbound) carrying the filter state `{root, sources[], families[], thirds[], fifths[], sevenths[]}`, with a **`voicingGridResult`** reply listing realized cells `{id, title, family, quality, shape, diagram(FretboardDiagram)}` — the whole filtered grid resolved in Core in **one round-trip**.
- `IN3` A **faceted filter stack**: **Root** (single global selector), **Source** (automatic/package/user), **Family** (CAGED/shell/doubled-shell), **3rd**, **5th**, **7th** — all multi-select & independent except Root. Within a level OR, across levels AND, all-on shows everything.
- `IN4` A **`QualityFacets`** decomposition of each `Quality` into **(3rd × 5th × 7th)** facets, **derived from chord-tone spelling**, living in `ChordFlow.Music.Harmony`. 3rd ∈ {major, minor, suspended}; 5th ∈ {perfect, augmented, diminished}; 7th/color ∈ {triad, 6, 7, maj7, dim7}.
- `IN5` Grid layout: **rows by quality, columns by CAGED shape / shell form**, for the selected root.
- `IN6` Shared display controls on GuitarVoicingsR — **orientation** (vertical/horizontal) and **label mode** (intervals/notes) — that fan out to **every** FretR cell.
- `IN7` FretR additions: a **`title`**, an **`id` shown with copy-to-clipboard**, and the per-cell **orientation toggle hidden when inside a grid**.
- `IN8` A wwwroot page hosting GuitarVoicingsR (the dogfood surface).
- `IN9` Dogfood validation: the rendered grids are cross-checked as the **visual oracle** against `voicings-engine-rules-reference.md` and the authored oracle grips.

### ❌ Excluded

- `EX1` The **engine inspector controls** (input rules / order / parameters) — owned by the `voicings-engine` thread's Voicings Engine page, which consumes this component.
- `EX2` Editing/CRUD of voicings — the Content page owns it.
- `EX3` Piano/flute render components.
- `EX4` An "all roots" multi-root mode — v1 is a single global root.
- `EX5` An "explain this voicing" affordance beyond copying the id.
- `EX6` Deriving new voicing families/qualities (sus, drop2, 6/9 …) — those are empty filter cells until the engine derives them (engine thread's concern).

### ⛓ Constraints

- `C1` **No music theory in JS** — facets and realization stay in Core; FretR and GuitarVoicingsR are dumb views.
- `C2` **`QualityFacets` lives in `Music.Harmony`** and references nothing under `Instruments/` (honors the theory↔instrument boundary; architecture-test-safe).
- `C3` **Reuse, don't fork** — cells use the existing `fretboard-render-component`; combos come from `CagedVoicingCatalog` (single source of truth) and realization from `RealizedVoicingDiagram`. No parallel catalog or realizer.
- `C4` The `voicingGrid` verb resolves the **whole filtered grid in one round-trip** (no N+1 per cell).
- `C5` An **empty filter result renders as an empty grid, never an error**.
- `C6` The C#↔JS bridge stays the **narrow JSON-envelope string protocol**.
