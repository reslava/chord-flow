---
type: design
id: de_01KVBSHF54Q2AMJESSQAKVV97W
title: Fretboard Render Component
status: done
created: 2026-06-17
updated: 2026-06-18
version: 3
tags: []
parent_id: null
requires_load: []
---
# Fretboard Render Component

A reusable JS component that draws **any positional music entity on a fretboard** — voicings, intervals, scales, arpeggios — from a **Core-computed marker model**, with a declarative option set. It is the **spatial twin** of `score-render-component`: that one centralizes the alphaTex → alphaTab *notation/playback* layer; this one centralizes the SVG *fretboard/spatial* layer. Theory stays in Core; the JS is a dumb view (same discipline as today's `chord-diagram.js`).

> Origin discussion: `chats/fretboard-render-component-chat-001.md`. Requirements: `req.md`.

---

## 1. Why

Today the only fretboard view is `chord-diagram.js`, driven by `VoicingDiagram.Build` → `DiagramModel`. It is **voicing-shaped**: exactly one entry per string (state muted/open/fretted + one fret). That is correct for a chord (one note per string) but wrong for everything the derivation engine will produce — scales, arpeggios, and the two-octave **interval lattice** (`intervals` thread) all have *many* notes per string.

We're building this **before** the `intervals`/CAGED derivation engine on purpose. That engine derives every CAGED shape from `quality intervals × octave shape` and is validated against the 34 authored voicings as a **golden oracle**. The natural way to build a derivation engine against an oracle is with a **visualization harness**: feed it positions, *see* where the dots land vs. the authored shape. This component is that harness — so it earns its place first (chat decision).

---

## 2. The model — markers, not strings (the load-bearing distinction)

The input is a **flat list of markers** plus diagram-level chrome. Many markers may share a string.

**Core carrier (`Domain/` — pure):**

```csharp
public enum MarkerShape { Circle, Square, Diamond, Ring }   // the LAYER channel

public sealed record FretboardMarker(
    int String,            // alphaTab numbering: 1 = high E .. 6 = low E (matches FretPosition)
    int Fret,              // 0 = open
    string Note,           // spelled note name  — both carried so the view toggles label mode
    string Interval,       // interval label ("1","b3","5","b7","#5","bb7")
    ChordToneFunction Function,  // the COLOR channel (root/third/fifth/seventh; tension otherwise)
    MarkerShape Shape);    // the LAYER channel (chord tone vs scale vs target …)

public sealed record FretboardDiagram(
    string Title,
    IReadOnlyList<FretboardMarker> Markers,
    IReadOnlyList<int> MutedStrings,   // voicing chrome; empty for scale/interval views
    int? BarreFret,
    int? FretMin, int? FretMax);       // optional window; view auto-fits to markers if null
```

- **Open** strings are just markers at `Fret = 0` (drawn as a ringed dot above the nut). **Muted** strings are *not* markers — they are diagram-level chrome (`MutedStrings`) shown as an `✕`. A voicing producer fills `MutedStrings`; a scale view leaves it empty. This cleanly separates "a note to draw" from "a string the player should not sound."
- `FretboardMarker` carries **both** `Note` and `Interval` so the view's label toggle needs no re-fetch (mirrors today's `DiagramString`).

This single shape — *markers, not per-string slots* — is what makes the view reusable (req `IN2`). Rafa confirmed it directly.

---

## 3. Visual encoding — color = interval, shape = layer (`IN4`/`IN5`)

Two independent channels, confirmed in chat:

- **Color = interval.** Resolved from the marker via an **overridable palette**. The **default palette is today's 5 function colors** (root `#e2574c`, third `#3b82f6`, fifth `#22a06b`, seventh `#a855f7`, tension `#9aa0a6`) — so chord diagrams look identical. A full chromatic interval map can pass a richer **per-interval palette** (up to 12 entries) keyed on the `Interval` token. The **legend auto-builds** from the tokens actually present.
- **Shape = layer/category.** `MarkerShape` distinguishes overlaid entities in one diagram — e.g. draw a Cmaj7 **arpeggio** (filled circles) *over* the C-major **scale** (small squares), root as a ring, target/guide tone as a diamond. Color still says *which interval*; shape says *which layer*.

(Color granularity — 5 function buckets by default vs. a 12-entry interval palette — is the only knob worth confirming; see §9.1. The model supports both; the default keeps chords unchanged.)

---

## 4. Component contract (`wwwroot/fretboard-render-component.js`)

Exposes `window.ChordFlowFretboard`:

```js
const view = ChordFlowFretboard.create(containerEl, {
  orientation: "vertical",  // "vertical" = chord box (strings as columns) | "horizontal" = neck (frets left→right)
  labelMode:   "interval",  // "interval" | "note"  — toggled by the component's own toolbar
  showLegend:  true,
  palette:     null,        // null = default 5-color function palette; or { "b3": "#…", … } per-interval
});

view.render(model);         // model = the FretboardDiagram JSON above
view.setLabelMode("note");  // re-renders with the other label set
view.dispose();
```

- The component **owns its toolbar** (the label toggle, like today's `buildToolbar`) and its **legend** (auto-derived from the markers present), so chrome is consistent wherever it's embedded — exactly as `score-render-component` owns its control strip.
- It draws an SVG fret-box (`vertical`) or a horizontal neck, fits the fret window to the markers (or honors `fretMin/fretMax`), drawing nut/position-label, barre, open/muted indicators, dots + labels, legend. The existing `chord-diagram.js` geometry is the starting point for the vertical orientation.
- **No music theory** — it consumes `Note`/`Interval`/`Function`/`Shape` already computed in Core (`C1`).

---

## 5. Producers & retrofit

The component is the stable seam; **producers** (Core code that builds a `FretboardDiagram`) are additive. v1 ships only the producers whose domain exists:

1. **Voicing producer (retrofit, `IN7`).** `VoicingDiagram.Build(shape)` today returns the voicing-specific `DiagramModel`. It is re-pointed to emit a `FretboardDiagram`: one `Circle` marker per sounding string (fret 0 ⇒ open marker), muted strings → `MutedStrings`, barre preserved. `chord-diagram.js` is **replaced by** `fretboard-render-component.js` in the Content/Voicings view, which keeps rendering (`C5`). `DiagramModel`/`DiagramString` are subsumed (no parallel voicing path).
2. **Test/hand-fed feeder (`IN8`).** A trivial way to render an arbitrary marker list immediately (a small dev page / fixture), so the harness is usable before any derivation-engine domain type exists.

**Deferred producers (`EX2`)** — interval lattice, scale, arpeggio — attach as the `intervals` / `octave-shapes` / `chord-qualities` / `caged-system` threads ship. Each is "build a `FretboardDiagram`," no view change.

---

## 6. Out of scope / deferred

- `EX1` **Click-to-author** — clicking positions to define a voicing (emit a voicing DSL string). The marker list *is* the coordinate system, so this is an additive interaction layer (an `onMarkerClick` callback + a toggle→DSL writer). Seam noted; not built — and no inert callback added speculatively.
- `EX2` **Producers for unbuilt domain types** (interval lattice, scales, arpeggios).
- `EX3` **Alternate tunings** — standard tuning only (`Fretboard` is fixed-tuning in v1).
- `EX4` **The alphaTab notation/playback path** — orthogonal; this is the spatial twin, not a replacement.

---

## 7. Reference-doc updates (same unit of work)

- `chordflow-architecture-reference.md` — add the SVG **fretboard render component** as a JS view layer alongside `score-render-component` (notation/playback). Both: dumb JS views over Core-computed models.
- `chordflow-domain-model-reference.md` — document `FretboardDiagram` / `FretboardMarker` as the general spatial carrier, with `VoicingDiagram.Build` recast as **one producer** of it (and `DiagramModel` subsumed).

---

## 8. Resolved decisions (was: open questions)

1. **Color granularity → ship the default only.** v1 ships the 5-color function palette (chord diagrams stay byte-identical); the future `intervals` producer brings its own richer per-interval palette. The model/component still accept an override `palette`, so nothing is foreclosed.
2. **`DiagramModel` removal → confirmed.** `VoicingDiagram` is recast onto `FretboardDiagram` and the old `DiagramModel`/`DiagramString` are removed — no parallel voicing path. The voicing fret-box is the **first consumer** of the new component. (Touches a shipped view — verify the Voicings preview after.)
3. **Orientation → vertical first.** Build the model + component orientation-agnostic; implement `vertical` (chord box) in v1 — all the voicing retrofit needs — and add `horizontal` (neck) when the first many-per-string producer (scale/interval) lands.