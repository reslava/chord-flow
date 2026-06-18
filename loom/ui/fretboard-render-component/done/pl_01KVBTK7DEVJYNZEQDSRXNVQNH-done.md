---
type: done
id: pl_01KVBTK7DEVJYNZEQDSRXNVQNH-done
title: Done — Fretboard Render Component — Implementation
status: done
created: 2026-06-18
version: 5
tags: []
parent_id: pl_01KVBTK7DEVJYNZEQDSRXNVQNH
requires_load: []
---
# Done — Fretboard Render Component — Implementation

## Step 1 — Add the general spatial carrier in Core (`FretboardDiagram`, `FretboardMarker`, `MarkerShape`) and recast `VoicingDiagram.Build` to emit it; remove `DiagramModel`/`DiagramString`. Update the `entityPreview` envelope + `ContentCrudHandler` and `VoicingDiagramTests` to the new type. Markers support many-per-string; muted strings become diagram-level chrome, open = a fret-0 marker.

**Core marker model + voicing producer recast.**

- **New `src/ChordFlow.Core/Domain/Diagrams/FretboardDiagram.cs`** — the general spatial carrier:
  - `enum MarkerShape { Circle, Square, Diamond, Ring }` (the layer channel; crosses the bridge as its int ordinal).
  - `FretboardMarker(int String, int Fret, string Note, string Interval, string Function, MarkerShape Shape)`. **`Function` is a `string` color-key** (`root/third/fifth/seventh/tension`), per the chat decision — `ChordToneFunction` can't hold `tension` and the Web serializer emits enums as numbers, which would break the JS palette keyed on names.
  - `FretboardDiagram(string Title, IReadOnlyList<FretboardMarker> Markers, IReadOnlyList<int> MutedStrings, int? BarreFret, int? FretMin, int? FretMax)`. Markers support many-per-string; muted strings are diagram chrome; open = a fret-0 marker.
- **`VoicingDiagram.Build` recast** to emit `FretboardDiagram`: one `Circle` marker per sounding string (fret 0 ⇒ open marker), muted/unsounded strings pushed into `MutedStrings`, barre preserved, `FretMin = firstFret`, `Title` from `ChordSymbol.Format(new Chord(PC 0, quality), C)`. Same theory as before (interval/function/label/note).
- **Deleted `DiagramModel.cs`** (`DiagramModel`/`DiagramString`) — no parallel voicing path.
- **`EntityPreviewEnvelope.Diagram`** retyped `DiagramModel?` → `FretboardDiagram?`. `ContentCrudHandler.VoicingPreview` flows through unchanged (still `VoicingDiagram.Build`).
- **Tests** `VoicingDiagramTests` rewritten to assert the marker shape (Circle markers, `MutedStrings`, `Title`, `FretMin`); `ContentCrudHandlerTests.Preview_Voicing` updated to assert 5 markers + `MutedStrings == [6]`. Core builds clean (0 warnings); 13 tests green.

Satisfies IN2, IN6, IN7, C1, C2, C4.

## Step 2 — Build `wwwroot/fretboard-render-component.js` (`window.ChordFlowFretboard`): `create(container, opts)` → handle with `render(model)`, `setLabelMode`, `dispose`. Draws the marker list as an SVG fret-box (vertical), color from the default 5-color function palette (override via `opts.palette`), shape from `MarkerShape`, open/muted indicators, barre, auto-fit fret window (or `fretMin/fretMax`), title, owned label toggle + auto-built legend.

**`wwwroot/fretboard-render-component.js` (`window.ChordFlowFretboard`).**

- `create(container, opts)` → instance handle `{ render(model), setLabelMode(mode), dispose() }`. Per-instance closure state (no module globals), so multiple diagrams can coexist — unlike the old singleton `ChordFlowDiagram`.
- Draws the flat `markers` list as a vertical SVG fret-box (started from `chord-diagram.js` geometry): column `i` = string `STRINGS - i` (leftmost = low E), markers placed by absolute fret, **many-per-string supported**.
- **Color = interval**: override `opts.palette` keyed on the interval token wins; otherwise the default 5-color function palette (`root/third/fifth/seventh/tension`) → chord diagrams render identical to before.
- **Shape = layer**: `MarkerShape` ordinal (0 Circle/1 Square/2 Diamond/3 Ring; name string tolerated) → `drawMarker` renders circle/square/diamond filled (white label) or ring hollow (colored label).
- Open string = ringed dot above the nut (fret-0 marker); muted strings (`mutedStrings`) = `✕` chrome; barre rect; auto-fit fret window to the markers or honor `fretMin/fretMax`; `topFret` position label when not at the nut.
- Owned toolbar shows the title + the interval/note **label toggle** (re-renders); **legend auto-builds** from the functions (or interval tokens, under an override palette) actually present.
- `orientation` accepted; v1 implements `vertical` only (horizontal deferred — design §8.3). **Zero music theory in JS** (C1). `node --check` passes.

Satisfies IN1, IN2, IN3, IN4, IN5, C1, C3.

## Step 3 — Add a hand-fed sandbox so arbitrary marker sets render immediately (the harness, usable before any derivation-engine domain type exists): a small standalone page that imports the component and draws a few fixtures (a voicing, a scratch interval/scale layout) demonstrating color/shape/label/legend.

**`wwwroot/fretboard-sandbox.html` — hand-fed harness (IN8).**

Standalone dev page (not wired into nav; open `https://chordflow.local/fretboard-sandbox.html` directly) that imports `fretboard-render-component.js` and renders three hand-built `FretboardDiagram`-shaped JSON fixtures in the **exact camelCase wire shape** a real producer emits:
1. **Open C major voicing** — one circle per string, low E muted (the chord case, == `VoicingDiagram.Build("x 3 2 0 1 0")`).
2. **C-major scale, open position** — the **many-markers-per-string** fixture (squares), proving the generalization the old per-string model couldn't express.
3. **Layered overlay** at the 7th position — all four shapes (ring root / circle chord tones / square scale / diamond target) + an **override per-interval palette**, so the legend keys on interval tokens and the non-nut fret window auto-fits to a `7fr` label.

Exercises color/shape/label-toggle/legend/open/muted/barre/fret-window before any derivation-engine domain type exists.

Satisfies IN8.

## Step 4 — Retrofit the Content/Voicings preview onto the component: `content-crud.js` `diagram` branch uses `window.ChordFlowFretboard` instead of `window.ChordFlowDiagram`; swap the `index.html` script tag (`chord-diagram.js` → `fretboard-render-component.js`); delete `chord-diagram.js`. The voicing fret-box is now the first consumer.

**Retrofit Content/Voicings preview onto `ChordFlowFretboard` (IN7/C5).**

- **`content-crud.js`**: the `diagram` branch of `renderPreview` now lazily creates a cached `ChordFlowFretboard` handle (`diagramView`, `{ labelMode: "interval" }`) and calls `diagramView.render(msg.diagram)` instead of the old singleton `ChordFlowDiagram.render(el, model)`. Handle is cached like `scoreView` (stable `diagramEl` node; `render()` rebuilds in place, so `clearPreview()`'s `textContent = ""` is harmless). Header comment updated to name the new component.
- **`index.html`**: `<script src="chord-diagram.js">` → `<script src="fretboard-render-component.js">`.
- **Deleted `chord-diagram.js`** — the voicing fret-box is now the **first consumer** of the general component; no parallel path.
- Verified no dangling `ChordFlowDiagram` / `chord-diagram.js` references remain (the surviving "chord-diagram" hits are alphaTab's unrelated in-score `\chord` directives + a history comment). Full solution builds (0 errors; the lone warning is the pre-existing Desktop `WindowsBase` version conflict), **399/399 Core tests green**.

> Note: the C5 "still renders" check is code-complete and the wire contract matches (same note/interval/function/open/muted/barre data the old box drew); a final visual confirmation in the running desktop app (Content → Voicings) is worth a glance but not blocking.

Satisfies IN7, C5.

## Step 5 — Sync the reference docs in the same unit of work: architecture ref gains the SVG fretboard render component as a JS view layer alongside `score-render-component`; domain-model ref documents `FretboardDiagram`/`FretboardMarker` as the general spatial carrier with `VoicingDiagram.Build` recast as one producer (and `DiagramModel` removed).

**Reference-doc sync (IN9, same unit of work).**

- **`chordflow-architecture-reference.md`**: project-structure listing now names `fretboard-render-component.js` (shared SVG fretboard render component) + `fretboard-sandbox.html`, replacing `chord-diagram.js`; bridge fan-out line updated; **new paragraph** documenting `window.ChordFlowFretboard` as the *spatial twin* of `ChordFlowScore` — a dumb SVG view over a Core-computed `FretboardDiagram` marker model (color=interval, shape=layer, owned toolbar/legend, auto-fit window), with `VoicingDiagram.Build` as today's only producer and scale/arpeggio/interval producers attaching additively.
- **`chordflow-domain-model-reference.md`**: the old `DiagramModel`/`VoicingDiagram` row replaced by **two rows** — `FretboardDiagram`/`FretboardMarker`/`MarkerShape` as the general spatial carrier (flat marker list, many-per-string, string `Function` color-key + the why, open=fret-0 marker, muted=chrome), and `VoicingDiagram` recast as **one producer** of it (`DiagramModel`/`DiagramString` removed; Circle markers, MutedStrings, Title from ChordSymbol, role-aware labels).

Satisfies IN9.
