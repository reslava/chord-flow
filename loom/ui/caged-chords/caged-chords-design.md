---
type: design
id: de_01KVMZJWXJ47KGK6YJG2S6QHYR
title: CAGED Chords — the derivation-engine dogfood page
status: done
created: 2026-06-21
updated: 2026-06-21
version: 2
idea_version: 2
tags: []
parent_id: id_01KVMX260B9QFH71G7TBAGFX7Q
requires_load: []
---
# CAGED Chords — the derivation-engine dogfood page

Resolves the four idea questions (chat-001, 2026-06-21): **auto-pick region · anchor in the
title · zone band only · shape + quality + root selectors, all combos derivable**. A thin
vertical slice over the done [[caged-system]] engine, mirroring the [[caged-shapes]] slice on
the shared `ChordFlowFretboard` ([[fretboard-render-component]]). See
[[chordflow-architecture-reference]] (the C#↔JS bridge) / [[chordflow-domain-model-reference]]
(`FretboardDiagram`, the `Caged/` engine).

## 1. Goal

A **CAGED Chords** page: pick a **CAGED shape** (C/A/G/E/D), a **quality** (maj/min/maj7/dom7/
m7/m7b5/dim7/aug), and a **root** (0–11); the host runs `CagedDerivation.Derive` and lights the
**derived** grip on the neck — frets + the octave-**zone band** — with the **anchor finger** in
the title. The page is a **generator**: all 8×5 quality×shape combos are offered, so it renders
grips the pack never authored (m7b5·C, dim7·G…), surfacing the engine's full reach.

## 2. Where it lives

A new vertical slice, same shape as the CAGED Shapes slice:
- **Core (`Features/Caged/`):** `CagedChordHandler` behind a new `cagedChordPreview` bridge verb.
- **Core (`Instruments/Guitar/Diagrams/`):** `ChordShapeDiagram` — a new producer of the
  existing `FretboardDiagram` carrier (the `ChordShape` → diagram twin of `VoicingDiagram`).
- **Bridge:** the `cagedChordPreview` verb (+ event + inbound `quality` field) on
  `WebMessageRouter`; outbound `CagedChordDiagramEnvelope` / `CagedChordErrorEnvelope`.
- **Desktop host (`Program.cs`):** wire the router event → handler → `bridge.Send`.
- **wwwroot:** `caged-chords.js` (the page), a nav button + view in `index.html`, registration
  in `app.js`. No new JS model — reuses `ChordFlowFretboard`.
- **Tests:** `ChordShapeDiagramTests` (mirror `CagedShapeDiagramTests`).

No engine changes. `ChordShape` already carries everything the diagram needs (per-string
fret/muted + `AnchorFinger` + `Zone`).

## 3. The producer — `ChordShapeDiagram.Build(ChordShape, root)`

Mirrors `VoicingDiagram`:
- One `Circle` `FretboardMarker` per **sounded** string: `Fret`, spelled `Note` (`NoteSpeller`
  with the root's key), `Interval` label (`IntervalSpeller.Label(semitone, role)`, role-aware so
  dim7's 9 = `bb7`, aug's 8 = `#5`), and `Function` colour-key (root/third/fifth/seventh by
  tertian position in `QualityIntervals`, else `tension`).
- **Muted** strings → `MutedStrings` chrome (not markers).
- **Zone band:** `ZoneFretMin/Max` from `ChordShape.Zone`.
- **Title:** `{chordSymbol} · {shape} shape · {anchorFinger}` (e.g. `Cmaj7 · E shape · index`).
- `FretMin` = the lowest fretted fret (auto-fit otherwise).

## 4. The handler & the auto-region (the one real decision)

`CagedChordHandler.Preview(quality, shape, rootPitchClass)`:
1. Parse `quality` (the shipped `Quality` names) + `shape` (C/A/G/E/D); an unknown value →
   `FormatException` → `CagedChordErrorEnvelope` (mirrors `cagedError`).
2. **Auto-region:** call `Derive(quality, shape, root, minFret: 0, maxFret: <neck>)`. The engine
   already anchors on the shape's **lowest** placement ≥ `minFret` (`OctaveShape.AnchorsFor`), so
   `[0, neck]` *is* "auto-pick the lowest position." `maxFret` only bounds the anchor search (the
   grip's own width is the reach window, ≤4 frets), so a full-neck `maxFret` (e.g. 15) is safe.
3. `Derive` throwing (a combo with no voiceable placement in range) → `CagedChordErrorEnvelope`,
   shown inline — no pre-greying of combos.
4. Build the diagram via `ChordShapeDiagram.Build` → `CagedChordDiagramEnvelope`.

## 5. The page (`caged-chords.js`)

Mirrors `caged-shapes.js`: three selectors (`cagedChordShape`, `cagedChordQuality`,
`cagedChordRoot`), each `change` → send `{type:"cagedChordPreview", shape, quality,
rootPitchClass}`; `onHostMessage` handles `cagedChordDiagram` (render on a lazily-created
`ChordFlowFretboard`, horizontal neck layout, chord-tone palette) and `cagedChordError` (inline).
Default selection **maj7 · E · A** (a familiar barre). A nav button (`navCagedChords`) + a view
(`caged-chords-view`) in `index.html`, registered in `app.js`'s view map.

## 6. Boundaries

- **Dogfood / read-only** — no editing, no save, no playback. Pure render of a derived grip.
- **Anchor in the title only** (no per-marker finger label) and **zone band only** (no box-kind —
  `ChordShape` carries none; the partial-box trim is deferred in the engine).
- Reuses the `FretboardDiagram` carrier and `ChordFlowFretboard` view unchanged (no new JS model).
- Out: a position/region control (auto-pick only in v1), fingering for every string, barre marks.

## 7. Open / to pin in the plan

- The exact `maxFret` neck bound for the auto-region (15 like CAGED Shapes, or wider).
- Palette: reuse the chord-tone function palette the voicing preview uses (root/third/fifth/
  seventh/tension), so colour reads the same across pages.
- Whether the quality selector shows the display symbols (`maj7`, `m7b5`, `°7`, `+`) or enum names.