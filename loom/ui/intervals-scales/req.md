---
type: req
id: rq_01KVGZRT9ZBR7X56M548CYKSKN
title: Scales — interval-set fretboard page (interval-lattice dogfood) — Requirements
status: locked
created: 2026-06-19
updated: 2026-06-19
version: 1
tags: []
parent_id: id_01KVGZR52DTP3KQ3CNNHD6G6F9
requires_load: []
---
# Scales — interval-set fretboard page (interval-lattice dogfood) — Requirements

The **Scales** page: type an interval set (`1 b3 4 5 b7`) and light it up on the fretboard around a chosen root — the dogfood harness for `[[interval-lattice]]` and the first many-per-string producer (so it brings horizontal orientation). Scope confirmed in `intervals-scales-chat-001`.

### ✅ Included

- `IN1` A new **Scales** screen with an **interval text box** (space-separated tokens, e.g. `1 b3 4 5 b7`) and a **root-note selector** (C…B), rendering the parsed interval set on the fretboard around that root.
- `IN2` **`IntervalSpeller.Parse`** (in `domain`, next to `Name`): maps an interval label → semitone, accepting **flats, sharps, and naturals** (`b3`, `#4`, `5`, `#5`, `#9`, `#11`, `13`…), mod-12 with `9/11/13` unfolded. It is the explicit inverse-vocabulary the `[[interval-lattice]]` idea deferred until a consumer asked — not a literal inverse of `Name`'s flats-only output.
- `IN3` A **scale producer**: turns (parsed interval set, root) into a `FretboardDiagram` — one marker per placed degree across the fret window — built on `IntervalLattice.PositionsOfInterval`. No new theory in the producer beyond the parse + lattice query.
- `IN4` **Horizontal orientation** implemented in `fretboard-render-component.js` (neck layout, frets left→right, many markers per string) — the orientation the component accepted but deferred in v1.
- `IN5` A **fallback-color** option in the component's palette mechanism, so a page can specify "this interval → color, everything else → a default color" (enables root-red / rest-black). Today a non-palette interval falls back to its function color.
- `IN6` Per-control **visibility flags** (`controls: { orientation, fretWindow, label, legend }`, all `true` by default) so a consumer hides controls it fixes — the Scales page locks horizontal (`controls.orientation:false`).
- `IN7` **Coloring for the Scales page:** root red, every other degree black, with the per-dot interval **label** carrying identity. Supplied as the page's palette (not a change to the component default).
- `IN8` **Root-note + auto-fit window:** the page auto-fits the fret window to the placed markers; no manual fret picker required in v1.
- `IN9` **Reference-doc update in the same unit of work:** `chordflow-domain-model-reference.md` documents `IntervalSpeller.Parse` as the inverse vocabulary; note the new component capabilities (horizontal, fallback color, control flags) where the fretboard component is described.

### ❌ Excluded

- `EX1` **Persistence** — storing scales in the database (future thread).
- `EX2` **Named-scale catalog / dropdown** — text input is the general v1 surface; no curated scale list.
- `EX3` **Root-fret picker** — v1 is root-note + auto-window only; choosing *where* on the neck the root sits is a later refinement.
- `EX4` **Arpeggio / multi-layer (shape-channel) overlays** — additive once chord qualities land.
- `EX5` **Page polish / richer interaction** — this is a fast visual check, intentionally minimal.
- `EX6` **Alternate tunings** — `Fretboard` is fixed-tuning.

### ⛓ Constraints

- `C1` **Zero music theory in JS** — the Scales page builds the model via Core (`Parse` + scale producer); the component stays a dumb drawer (inherits `[[fretboard-render-component]]` C1).
- `C2` `IntervalSpeller.Parse` lives in **`domain`** (pure, no UI/host refs); dependency direction Desktop → Core unchanged.
- `C3` The component changes (IN4/IN5/IN6) must keep the **existing voicing fret-box rendering byte-identical** — default `controls` = all shown, no palette = function colors, vertical unchanged.
- `C4` **No new build step or framework** in `wwwroot` — vanilla JS over the existing virtual host.
- `C5` Reuse `IntervalLattice` as-is (it's shipped) — the producer queries it, it is not re-implemented or duplicated.