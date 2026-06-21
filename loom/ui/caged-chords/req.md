---
type: req
id: rq_01KVMZKS5F9NSFJEQ0J47C865Q
title: CAGED Chords — the derivation-engine dogfood page — Requirements
status: locked
created: 2026-06-21
updated: 2026-06-21
version: 2
tags: []
parent_id: id_01KVMX260B9QFH71G7TBAGFX7Q
requires_load: []
---
# CAGED Chords — the derivation-engine dogfood page — Requirements

### ✅ Included

- `IN1` A **CAGED Chords** dogfood page: **shape** (C/A/G/E/D) + **quality** (maj/min/maj7/dom7/m7/m7b5/dim7/aug) + **root** (0–11) selectors → the host-derived grip rendered on `ChordFlowFretboard` — frets + the octave **zone band**, with the **anchor finger in the title**.
- `IN2` `ChordShapeDiagram.Build(ChordShape, root)` → `FretboardDiagram` producer (the `ChordShape` twin of `VoicingDiagram`): one `Circle` marker per sounded string (spelled note, role-aware interval label, function colour-key), muted strings as chrome, the `ChordShape.Zone` as the band, title = `{chordSymbol} · {shape} shape · {anchorFinger}`.
- `IN3` `cagedChordPreview` bridge verb + `CagedChordHandler.Preview(quality, shape, rootPitchClass)` with **auto-region** (derive at the shape's lowest placement on the neck); a combo with no voiceable placement → an inline `CagedChordErrorEnvelope` (mirrors `cagedError`).
- `IN4` **All 8 qualities × 5 shapes selectable** — the page is a generator over the engine, so it renders combos the pack never authored (m7b5·C, dim7·G, …), not just the 36 golden grips. No pre-greying.
- `IN5` Bridge + host wiring: the verb/event/inbound `quality` field on `WebMessageRouter`, the `Program.cs` router→handler→`bridge.Send` hookup, a nav button + view in `index.html`, and registration in `app.js`'s view map.
- `IN6` A unit test `ChordShapeDiagramTests` for the producer (mirrors `CagedShapeDiagramTests`): markers, muted strings, zone band, and title for a representative derived shape.
- `IN7` *(amended caged-chords-chat-002)* **Auto-region sub-nut fix in `OctaveShape.AnchorsFor`.** The visual check found that an open-string root on a down-stacking shape (C·maj7·A, G·maj7·E, …) anchored the grip at fret 0, where the higher-octave root anchor falls **below the nut** (negative fret) and the reach window collapses to fret 0 — so interior strings are wrongly muted. `AnchorsFor` must anchor at the lowest occurrence **whose whole octave skeleton fits (every anchor ≥ fret 0)**, skipping a too-low primary to the next octave up (the lowest *playable* placement, ≈9–12 for the reported cases). Regression-tested; the 36/36 derivation oracle is unaffected. This relaxes `EX4` for this one defect (see below).

### ❌ Excluded

- `EX1` Editing / saving / playback — a read-only render-only dogfood page.
- `EX2` A position/region control — **auto-pick the lowest placement only** in v1.
- `EX3` Per-string fingering, box-kind (main/partial), or barre marks — **anchor in the title + zone band only** (`ChordShape` carries no box-kind; the partial-box trim is deferred in the engine).
- `EX4` Any change to the CAGED engine (`CagedDerivation` / `ChordShape` / the substrates) — the page consumes it as-is. *(amended caged-chords-chat-002: relaxed for one engine defect the dogfood page surfaced — the `AnchorsFor` sub-nut anchor bug, now `IN7`. The page is exactly the harness meant to find such defects; the fix is a localized correction of an `OctaveShape` invariant, not a feature change.)*
- `EX5` A new JS render model — reuse the `FretboardDiagram` carrier + `ChordFlowFretboard` view unchanged.

### ⛓ Constraints

- `C1` All music theory stays in **Core**; `caged-chords.js` is a dumb drawer (the standing theory-in-kernel / JS-draws split).
- `C2` Lives in the established slices — `Features/Caged`, `Instruments/Guitar/Diagrams`, `Bridge`, `ChordFlow.Desktop` — dependency direction Domain ← Instruments ← Features/Bridge ← Desktop, per [[chordflow-architecture-reference]].
- `C3` Reuses the `FretboardDiagram` carrier + `ChordFlowFretboard` view **unchanged** (no new carrier or JS model — `C3`/`EX5` together).
- `C4` Mirrors the [[caged-shapes]] slice's structure (verb / handler / producer / page) for consistency across the dogfood pages.