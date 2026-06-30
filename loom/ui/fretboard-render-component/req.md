---
type: req
id: rq_01KVBSE7S4QNJEGZ7P8BXHS7S8
title: Fretboard Render Component — Requirements
status: locked
created: 2026-06-17
updated: 2026-06-17
version: 1
design_version: 3
tags: []
parent_id: de_01KVBSHF54Q2AMJESSQAKVV97W
requires_load: []
---
# Fretboard Render Component — Requirements

One reusable JS component that draws **any music entity on a fretboard** — intervals, scales, notes, chord voicings, arpeggios — from a **Core-computed marker model**, with a declarative option set. The **spatial twin** of the `score-render-component` (which centralizes alphaTex → alphaTab notation/playback): this centralizes the SVG fretboard/spatial display layer. Built **before** the `intervals`/CAGED derivation engine on purpose — it is the **visualization harness** that engine is built and validated through (against the 34-voicing golden oracle). Scope confirmed in `fretboard-render-component-chat-001`.

### ✅ Included

- `IN1` A **single reusable fretboard render component** (`wwwroot/fretboard-render-component.js`, `window.ChordFlowFretboard`) that draws an arbitrary set of fretboard positions as an SVG diagram — usable for voicings, intervals, scales, arpeggios, and any future positional entity.
- `IN2` A **marker-list input model**: a flat list of markers, each a position `{ string, fret }` plus presentation/semantic fields `{ label, interval/role, shape? }`. The model supports **many markers per string** (a scale/arpeggio/interval lattice has multiple notes per string) — this is the core generalization over today's one-entry-per-string voicing model.
- `IN3` **Diagram-level options**: `title`, a fret window (`fretMin`/`fretMax`), `orientation` (`horizontal` | `vertical`), and optional barre/nut presentation.
- `IN4` **Color encodes interval/function** — reuse the established function→color key (root / 3rd / 5th / 7th / tension), with a legend.
- `IN5` **Shape encodes the layer/category** (e.g. chord tone vs scale degree vs target/guide tone vs root), so overlaid entities read in one diagram. Color = which interval; shape = which layer.
- `IN6` A **Core model carrier** (`FretboardDiagram` + a marker type) is the general input the component draws. **All music theory stays in Core** — the JS component holds zero theory (same discipline as today's `chord-diagram.js`).
- `IN7` **Retrofit the existing voicing fret-box** (`chord-diagram.js` / `VoicingDiagram.Build` → `DiagramModel`) so it becomes **one producer** of the general model rather than a parallel voicing-specific path — preserving its current presentation in the Content/Voicings view.
- `IN8` A **hand-fed / test producer** so arbitrary markers can be rendered immediately, before the derivation-engine domain types exist (the harness must be usable from day one).
- `IN9` **Reference-doc updates** in the same unit of work: architecture ref (the SVG fretboard render component as a JS view layer alongside `score-render-component`) and domain-model ref (the `FretboardDiagram` carrier + `VoicingDiagram` as one of its producers).

### ❌ Excluded

- `EX1` **User input / click-to-author** — clicking fretboard positions to define a voicing (emit a voicing DSL string). Cleanly additive on the same coordinate system; deferred to a future thread.
- `EX2` **Producers for domain types that do not exist yet** (interval lattice, scales, arpeggios) — these land **additively** as the `intervals` / `octave-shapes` / `chord-qualities` / `caged-system` threads ship. This thread builds the component (stable seam) + the voicing producer + a test feeder only; no producer for a domain type that isn't built.
- `EX3` **Alternate tunings** — standard tuning only (the `Fretboard` is fixed-tuning in v1).
- `EX4` **The alphaTab notation/playback path** — orthogonal. This is the spatial twin of `score-render-component`, not a replacement for it.

### ⛓ Constraints

- `C1` **Zero music theory in JS** — the component draws a Core-computed model only (IN6 discipline; theory lives in `Domain/`).
- `C2` Dependency direction **Desktop → Core** unchanged; Core stays UI-agnostic (compile-enforced).
- `C3` **No new build step or framework** in `wwwroot` — vanilla JS modules over the existing virtual host.
- `C4` **No dependency on the derivation-engine threads** to ship — the component + voicing producer + test feeder stand alone; derivation-engine producers attach later (EX2).
- `C5` The retrofit (IN7) must keep the **Voicings view rendering** — the voicing fret-box continues to display after being re-pointed at the general component.