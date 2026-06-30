---
type: design
id: de_01KWAQ35KX1PNST681WY0RVQDS
title: Guitar Voicings Render Component
status: done
created: 2026-06-29
updated: 2026-06-30
version: 4
tags: []
parent_id: id_01KWAJXJAS34EZMZSQJVC56WC5
requires_load: []
---
# Guitar Voicings Render Component

## 1. Summary

**GuitarVoicingsR** (`guitar-voicings-render-component.js`, `window.ChordFlowGuitarVoicings`) renders **many realized voicings at once** as a **grid of FretR chord-boxes** with a **faceted filter stack**. It is a *projection + layout* over the existing voicing catalog — no new derivation — and doubles as the **visual oracle** for the engine. Settled in `voicings-render-component-chat-001.md`.

---

## 2. Decisions

### 2.1 Bridge contract — a new `voicingGrid` verb (decision: option (a))

A grid is a genuinely new *read shape*, so it gets its own verb rather than N+1 `entityList`+`entityPreview` calls.

- **Inbound** `voicingGrid` carries the full filter state:
  `{ root: pitchClass, sources: string[], families: string[], thirds: string[], fifths: string[], sevenths: string[] }`.
- **Core** filters `CagedVoicingCatalog.Combos` (+ package/user sources) by the facets, realizes each surviving `(source, family, quality, shape)` at `root` via the existing `CompingResolver` / `RealizedVoicingDiagram.Build`, and replies **`voicingGridResult`** with an ordered list of cells:
  `{ id, title, family, quality, shape, diagram: FretboardDiagram }`.
- **One round-trip** for the whole filtered grid — the "computed as a by-product, can't drift from the tab" discipline the architecture ref favors. The bridge stays the narrow JSON-envelope string protocol.

### 2.2 Facet model — quality decomposed into (3rd × 5th × 7th), *derived from spelling*

Each `Quality` is decomposed into three orthogonal facets, **read off its chord-tone spelling** (not hand-maintained metadata) so the mapping is auto-correct as qualities are added:

| Axis (label) | Read from | Values |
|---|---|---|
| **3rd** (emotion) | 3rd-degree interval | `major` (3) · `minor` (♭3) · `suspended` (no 3) |
| **5th** (stability) | 5th-degree interval | `perfect` · `augmented` (♯5) · `diminished` (♭5) |
| **7th** (color) | 6th/7th-degree interval | `triad` (none) · `6` · `7` (♭7) · `maj7` (♮7) · `dim7` (♭♭7) |

The engine's 11 qualities map to **unique, collision-free** cells (the plain `Diminished` triad — `minor / diminished / triad` — is included for completeness; `CagedVoicingCatalog` authors no `automatic` combos for it, so it surfaces no grid cells yet):

| Quality | 3rd | 5th | 7th |
|---|---|---|---|
| Major | major | perfect | triad |
| Minor | minor | perfect | triad |
| Major6 | major | perfect | 6 |
| Minor6 | minor | perfect | 6 |
| Dominant7 | major | perfect | 7 |
| Minor7 | minor | perfect | 7 |
| Major7 | major | perfect | maj7 |
| Augmented | major | **augmented** | triad |
| Diminished | minor | **diminished** | triad |
| m7♭5 | minor | **diminished** | 7 |
| dim7 | minor | **diminished** | dim7 |

Implementation: a pure **`QualityFacets`** helper in **`ChordFlow.Music.Harmony`** (instrument-agnostic — it reads `ChordTones`/`ChordToneFunction`, references nothing under `Instruments/`). Derivation rule per facet:
- **3rd**: `Third` function present → major if interval 4, minor if 3; absent → suspended.
- **5th**: `Fifth` interval → 7 perfect · 8 augmented · 6 diminished.
- **7th/color**: no 6th/7th → triad; `Sixth` → 6; `Seventh` interval → 10 `7` · 11 `maj7` · 9 `dim7`.

### 2.3 Filter semantics — faceted, multi-select

- **Levels:** **Root** (single global selector) · **Source** (automatic / package / user) · **Family** (CAGED / shell / doubled-shell) · **3rd** · **5th** · **7th**. All multi-select & independent **except Root**.
- **Within a level → OR**; **across levels → AND**; **all toggles on → show everything.** Empty result ⇒ an empty grid, **never an error**.
- **UI:** toggle-button groups styled like the existing Content → Voicings → Definitions toggles.

### 2.4 Layout — rows by quality, columns by shape/form

For the selected root, the grid lays out **rows = the matching qualities**, **columns = CAGED shape / shell form**; each cell is a FretR chord-box. (Empty quality/shape combinations leave a gap, not a box.)

### 2.5 Shared display controls fan out to all cells

GuitarVoicingsR owns **one** orientation toggle (vertical/horizontal) and **one** label-mode toggle (intervals/notes); both apply to **every** FretR cell. Each cell is created with FretR's existing `controls.orientation:false` (and label control hidden), so the grid drives them globally — the Scales page already uses this per-control-visibility pattern.

### 2.6 FretR additions (`fretboard-render-component.js`)

- **`title`** — a per-cell label (e.g. "Dom7 — E shape").
- **`id` + copy-to-clipboard** — shows the synthetic voicing id (`auto:shell:dom7:E` …) with a copy control; the oracle/debug handle and the seed of a future "explain this voicing" affordance.
- **per-cell orientation toggle hidden inside a grid** — via the existing `controls` visibility flag (no new plumbing).

---

## 3. Where the code lives

| Piece | Location | Note |
|---|---|---|
| Facet decomposition `QualityFacets` | `ChordFlow.Music.Harmony` | pure theory, instrument-agnostic (architecture-test-safe) |
| `voicingGrid` handler (filter + realize → cells) | `Features/Voicings` | reuses `CagedVoicingCatalog` + `RealizedVoicingDiagram`; no parallel catalog |
| `voicingGrid` / `voicingGridResult` envelopes | `Bridge/` + `WebMessageRouter` | narrow string protocol |
| GuitarVoicingsR component | `wwwroot/guitar-voicings-render-component.js` | grid + filter stack; consumes FretR |
| FretR additions | `wwwroot/fretboard-render-component.js` | title, id+copy, control visibility |
| Hosting page | `wwwroot` (a Voicings view/page) | the dogfood surface |

Honors the **theory ↔ instrument boundary**: facets are theory (Music), realization is guitar (Instruments/Guitar), the views stay dumb (zero theory in JS).

---

## 4. Validation / dogfood

GuitarVoicingsR **is** the fretboard UI page for the voicings subsystem (guitar-weave dogfood rule). Visual oracle: cross-check rendered grids against `voicings-engine-rules-reference.md` and the authored oracle grips (e.g. all dom7 shells across roots match the 12-grip shell oracle).

---

## 5. Open / deferred

- **Engine inspector controls** (input rules/order/parameters) → the `voicings-engine` thread's Voicings Engine page, which *consumes* this component.
- **Editing/CRUD** → Content page.
- **Piano/flute** render components.
- **"All roots" mode** → single global root for v1.
- **"Explain this voicing"** beyond copy-id.
- **New families/qualities** (sus, drop2, 6/9 …) → empty filter cells until the engine derives them (engine thread).
