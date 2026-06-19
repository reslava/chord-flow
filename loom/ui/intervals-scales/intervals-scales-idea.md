---
type: idea
id: id_01KVGZR52DTP3KQ3CNNHD6G6F9
title: Scales — interval-set fretboard page (interval-lattice dogfood)
status: done
created: 2026-06-19
updated: 2026-06-19
version: 2
tags: []
parent_id: null
requires_load: []
---
# Scales — interval-set fretboard page (interval-lattice dogfood)

## Origin

The dogfood page the `[[interval-lattice]]` idea already commits to (its Validation §: *"ship a fretboard UI page that lights up every interval around a chosen root… before building chord-qualities / caged on top"*). Discussion: `chats/intervals-scales-chat-001.md`. It is also the **first many-per-string producer**, which is the trigger `[[fretboard-render-component]]` design §8.3 named for finally implementing **horizontal orientation**.

## The idea

A new **Scales** screen: a text box where the user types an interval set — e.g. `1 b3 4 5 b7` (minor pentatonic), `1 2 3 5 6` (major pentatonic) — and the app lights those intervals up on the fretboard around a chosen root. A fast **visual confirmation** that the interval-lattice math places each degree where it should, before chord-qualities / CAGED are built on top.

The data spine is almost entirely existing seams:

**parse intervals (new, in `domain`) → place via `IntervalLattice` (exists) → render horizontal (new component orientation) → page chrome.**

### What's genuinely new

1. **`label → semitone` parser** — the inverse of `IntervalSpeller.Name`, the reverse parser `[[interval-lattice]]` explicitly deferred *"until a consumer asks."* This page asks. It is pure theory vocabulary, so the **code lands in `domain` (`IntervalSpeller.Parse`, next to `Name`)** with the domain-model ref updated in the same unit of work — even though the work is tracked in this UI thread (folded here by decision, since this is the consumer that justifies it). **Spec:** `Parse` accepts **flats, sharps, and naturals** (`b3`, `#4`, `5`, `#5`, `#9`, `#11`, `13`…), each token → semitone (mod-12, with `9/11/13` unfolded) — it is *not* a literal inverse of `Name`'s flats-only output, or it chokes on lydian/altered scales.
2. **Horizontal orientation** in `fretboard-render-component.js` — the neck layout (frets left→right, many markers per string), which v1 of the component accepted but deferred to vertical.

### Reused as-is

- `IntervalLattice.PositionsOfInterval(root, semitone, minFret, maxFret)` already returns every fret of each degree across the window (all octaves by pitch class) — exactly a neck-wide scale spread.
- The **scale producer** builds a `FretboardDiagram` (one marker per placed degree). This is one of the `EX2` producers the fretboard design parked — additive, no view change.
- The model already carries `fretMin`/`fretMax`.

### Component control model (agreed)

Each control has a default value **and** a default-visible flag; a consumer hides the controls it fixes. A `controls: { orientation, fretWindow, label, legend }` bag, all `true` by default (today's voicing view unaffected). The Scales page passes `orientation:"horizontal", controls:{orientation:false}` (locked horizontal); the voicing retrofit will pass `controls:{fretWindow:false}` and set the window itself.

### Coloring (agreed)

For scale-shape clarity: **root red, everything else black** — the per-dot interval **label** already carries identity, so color only needs to anchor the root. This is the **page's** palette (the component stays a dumb drawer), and it needs one tiny component tweak: the palette mechanism must accept a **fallback color** (today a non-palette interval falls back to its *function* color, not black). Chord diagrams pass no palette and stay byte-identical.

### Control ownership

- **Component-owned chrome (reusable):** orientation toggle, label toggle, legend, fret-window controls — plus the per-control visibility flags above.
- **Page-owned (scale-specific):** the interval **text box** and the **root selector** (selection/theory, not drawing).

### Root selector

**Root-note only** (C…B); the page auto-fits the fret window to the placed markers. A root-*fret* picker is a later refinement.

## In scope

- The `Scales` page: interval text box + root-note selector, rendering the parsed set on a horizontal fretboard.
- `IntervalSpeller.Parse` (label → semitone; flats/sharps/naturals) in `domain`, + domain-model ref update.
- A **scale producer** that turns (interval set, root) into a `FretboardDiagram` via `IntervalLattice`.
- **Horizontal orientation** + a **fallback-color** palette option + the per-control `controls` visibility flags in `fretboard-render-component.js`.

## Out of scope / deferred

- **Persistence** — storing scales in the database (Rafa: future).
- Page polish / richer interactions — future; this is a fast visual check.
- Named-scale catalog / dropdown — text input is the general v1 surface.
- A root-fret picker (root-note + auto-window only in v1).
- Alternate tunings (`Fretboard` is fixed-tuning).
- Arpeggio / multi-layer overlays — additive once chord qualities land.

## Dependencies

`[[interval-lattice]]` (`IntervalLattice`, shipped) · `[[fretboard-render-component]]` (the SVG view, shipped — extended here with horizontal + fallback color + control flags) · `[[intervals]]` / `IntervalSpeller` in `domain` (extended here with `Parse`). Naming: working title **Scales** (vs `Scales/Intervals` — decide at build).

## Validation

Type a known shape (minor/major pentatonic, then a `#4`/`#5` scale) and confirm every dot lands where the interval should sit around the chosen root, root highlighted, across the auto-fit window in horizontal layout. This page *is* the dogfood harness for `[[interval-lattice]]`.

Related: `[[interval-lattice]]`, `[[fretboard-render-component]]`, `[[intervals]]`, `[[caged-system]]`, `[[interval-derivation-engine-vision]]`, `[[chordflow-domain-model-reference]]`.