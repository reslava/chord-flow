---
type: idea
id: id_01KWAJXJAS34EZMZSQJVC56WC5
title: Guitar Voicings Render Component
status: done
created: 2026-06-29
updated: 2026-06-29
version: 2
tags: []
parent_id: null
requires_load: []
---
# Guitar Voicings Render Component

## Problem / motivation

ChordFlow's voicing engine is real but **invisible** — derived CAGED / shell / doubled-shell grips live in tests, the `automatic` Content rows, and the now/next fret-boxes, but there is no surface that *shows the voicing world at a glance*. The engine is the product's core differentiator (a chord **reasoner**, not a chord viewer — see `loom/ctx.md`), so it deserves a surface that makes it **showable** and that doubles as the **visual oracle** for everything the engine derives next.

Per the guitar-weave dogfood rule (`loom/ctx.md`), every guitar feature ships with a fretboard UI page that visualizes it. **GuitarVoicingsR is that surface for the whole voicings subsystem.**

## Concept

**GuitarVoicingsR** (`guitar-voicings-render-component.js`, `window.ChordFlowGuitarVoicings`) — a render component that shows **many voicings at once** as a distributed **grid of fretboard chord-boxes**, built on the existing **FretR** (`fretboard-render-component.js`) as the per-cell view. Above the grid sits a **filter stack** that narrows which voicings are shown.

It is a **projection + layout**, not new domain work: each cell is a realized voicing (a `FretboardDiagram` from the real-root producer `RealizedVoicingDiagram.Build`) for a `(source, family, quality, shape)` combo at the selected root. The combos come from the single catalog source of truth (`CagedVoicingCatalog.Combos` for `automatic`, plus package/user sources).

## Filter stack

- **Source / family** — `automatic` (CAGED · shell · doubled-shell), package, user.
- **Chromatic root** — the 12 roots.
- **Top quality** — major / minor / sus / dominant …
- **Subquality** — triads / 7ths / 6ths.

Filters compose; the grid re-lays-out to whatever the filters select.

## Built on / data

- **View:** reuses FretR vertical chord-boxes (the byte-identical box the voicing preview already uses) — zero new SVG/theory in JS; theory stays in Core.
- **Data:** the `automatic` rows already exist via `EngineVoicingSource` (tagged `source` + `family/quality/shape`); realization via the existing `CompingResolver` / `RealizedVoicingDiagram`. Open design question: a dedicated bridge verb for "list+realize a filtered voicing set" vs. composing the existing `entityList` / preview verbs.

## Scope

**In (v1):** a read-only **grid of realized voicings** with the filter stack, over the existing catalog (automatic + package + user); a wwwroot page hosting it; sensible layout for dozens of boxes (grouping + fit/virtualization as needed).

**Out (deferred):**
- Editing voicings (Content page already owns CRUD).
- The **engine inspector controls** (input rules / order / parameters) — that is the **Voicings Engine page** in the `voicings-engine` thread; GuitarVoicingsR is the *output* surface that page consumes.
- Piano/flute render components.

## Validation

- **Dogfood:** ship it as a wwwroot page and visually confirm the derived grids against `voicings-engine-rules-reference.md` and the golden-oracle grips — GuitarVoicingsR **is** the live visual oracle for the engine.
- Cross-check a known set (e.g. all dom7 shells across roots) against the authored oracle grips.

## Relationships / open questions

- **Consumed by** the future Voicings Engine page (`guitar/voicings-engine`): that page drives `GuitarVoicingsEngine` and renders its output *through* this component. Real dependency: `engine-page → GuitarVoicingsR`. This component is **not** blocked by new engine work — it renders today's catalog now.
- Open for design: bridge contract (new verb vs reuse), grid layout/grouping model (by quality? by shape? by root?), filter interaction, and whether a cell can also surface the *abstract* voicing (likely an engine-page concern, not here).
