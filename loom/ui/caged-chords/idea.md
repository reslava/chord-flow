---
type: idea
id: id_01KVMX260B9QFH71G7TBAGFX7Q
title: CAGED Chords — the derivation-engine dogfood page
status: done
created: 2026-06-21
updated: 2026-06-21
version: 2
tags: []
parent_id: null
requires_load: []
---
# CAGED Chords — the derivation-engine dogfood page

## The idea

A fretboard page that renders a **derived** CAGED chord. Pick a **quality** (maj / min /
maj7 / dom7 / m7 / m7b5 / dim7 / aug), a **CAGED shape** (C/A/G/E/D), and a **root**, and the
host runs `CagedDerivation.Derive` and lights the computed grip on the neck — **frets +
anchor finger + the octave-zone band** — so we can *see* the engine's output, not just read
oracle numbers.

This is the standing **guitar-weave dogfood page** for the now-complete
[[caged-system]] derivation engine (engine + both oracles done, 36/36, committed). It's the
visual twin of [[caged-shapes]] — which is the dogfood page for the *octave-shapes* skeleton —
and it's built the same way: on the shared `ChordFlowFretboard` component
([[fretboard-render-component]]), with a thin vertical slice (`cagedChordPreview` bridge verb
→ a new `ChordShapeDiagram` producer of the existing `FretboardDiagram` carrier). No new
theory in the JS — Core computes, the view draws (the standing C1/IN6 split).

## Why now

The engine is proven against the pack in tests, but we've never *looked* at a derived grip on
a fretboard. The dogfood rule exists exactly for this: fast visual confirmation before
scales/arpeggios build on the same skeleton. It also surfaces the engine's richer outputs the
oracle doesn't show — the **anchor finger** (step-6 IP) and the **octave zone** — in a form a
guitarist can sanity-check by eye.

## In scope

- A **CAGED Chords** page: quality + shape + root selectors → a rendered derived grip.
- A `ChordShapeDiagram.Build(ChordShape, root)` producer → `FretboardDiagram` (markers per
  sounded string, muted strings as chrome, the zone as a band, the **anchor finger** surfaced).
- The `cagedChordPreview` bridge verb + `CagedChordHandler` + host wiring, mirroring the
  `cagedPreview` slice.
- Nav entry + page wiring in `app.js` / `index.html`.
- A unit test for the producer (mirror `CagedShapeDiagramTests`).

## Open questions (for the design conversation)

- **Neck region** — the engine takes `(minFret, maxFret)`; the page must choose a region per
  (shape, root). Auto-pick the lowest playable placement, or expose a region/position control?
- **How to show the anchor finger** — in the title only, a finger-number label on the root
  marker, or a dedicated readout?
- **Box kind (main/partial)** — `ChordShape` doesn't carry it yet (the partial-box trim is
  deferred in the engine). Show only the zone band for now, or add box-kind to `ChordShape`?
- **Quality vocabulary** — all 8 shipped qualities, and which (shape × quality) combos to
  offer (some shapes are E/A/D-only for m7b5/dim7).

## Dependencies

Consumes the done [[caged-system]] engine and the [[fretboard-render-component]]. Sibling of
[[caged-shapes]] (same dogfood pattern). No engine changes required (unless we choose to add
box-kind to `ChordShape`).