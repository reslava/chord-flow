---
type: done
id: pl_01KVZYFBK1NWG8P2VA8R8K83AE-done
title: Done — Engine-derived voicings as the app's source
status: done
created: 2026-06-25
updated: 2026-06-25
version: 2
tags: []
parent_id: pl_01KVZYFBK1NWG8P2VA8R8K83AE
requires_load: []
---
# Done — Engine-derived voicings as the app's source

Steps 1–9 landed and are green (726/726 tests after the dogfood fixes). The engine (`CagedDerivation.Derive`) is now the app's `automatic` voicing source; the 36 authored grips are a test-only oracle.

## What shipped (steps 1–8)
- **IN1 `ChordShapeVoicing.ToVoicing`** — `ChordShape → Voicing` adapter (BarreFret null, EX5).
- **IN2/IN3 `EngineVoicingSource : IComputedContentSource`** — lists the 36 `automatic` rows (`auto:dom7:E` …) via the shared `CagedVoicingCatalog` + `AutomaticVoicingId`; wired as `ContentCrudHandler`'s `computed:` source.
- **IN7 ranking seam** — `IVoicingRanking` + `ClosestRanking` (lowest-fret first, reuse on repeat, else min per-string fret-distance sum).
- **IN4 `CompingResolver` + `CompingPlan`** — Features-layer resolution: main source → `user > package > automatic` fallback; `automatic` via `Derive → adapter`; `StoredVoicingSource` (source-tagged via `VoicingStore.LoadShapesBySource`).
- **IN6 `VoicingSource` knob** — replaced `VoicingStrategy`; structured `{kind, minFret?, maxFret?, packageId?, ranking?}` through `renderOptions.voicing`. Absent ⇒ automatic / full-neck / Closest.
- **IN5 renderer = pure formatter** — `AlphaTexRenderer` drops the `VoicingBook` ctor, takes the `CompingPlan`; `ExerciseRendering`/`ContentCrudHandler` build it via the resolver. Removed the now-pointless `SwappableRenderer` + `VoicingsChanged` rebuild (stateless renderer, voicings read fresh per render).
- **IN8 relocate** — the 36 `.dsl` `git mv`'d to `tests/.../fixtures/caged-oracle/`; default pack ships zero voicings; oracle tests + `CagedOracleVoicingsTests` read the fixture via `OracleVoicings.Load`.
- **IN9/C5 coverage gate** — `EngineVoicingCoverageTests`: the automatic catalog is exactly the 36 oracle-verified combos, each derives fully-spelled.
- **IN10 ref/comment sync** — architecture + domain refs updated; fixed the stale "34" → "36".
- **IN11 end-to-end** — `EngineCompingEndToEndTests` proves the default generate path comps engine grips for a 12-bar blues and that a region lock changes them.

### Decisions/deviations
- `CompingPlan` lives in `Rendering` (not Features) — required by dependency direction; it's the renderer's input contract + the exporter seam.

## Open from dogfood (step 9 — found by running the app) — ALL RESOLVED
- **A — stale `package` voicings** → fixed in **plan-002 IN12** (pack import reconciles, purges orphaned rows on launch).
- **B — `automatic` rows not openable** → fixed in **plan-002 IN13** (`ContentCrudHandler.Get` derives a read-only DSL for `auto:` ids; the existing read-only + Duplicate-to-user UI then works).
- **C — no Practice voicing control** → fixed in **plan-002 IN14** (min/max fret region inputs on the builder).
- **D — `Derive` `ArgumentOutOfRangeException`** → root-caused + fixed in **[[caged-derive-anchor-edge]]** (open-root anchor → Index); unblocked open-position comping. Plus `{firstfret N}` on chord diagrams (folded into the renderer here).

## Still open (separate thread)
- `guitar/fretboard-fret-label-clip` — the `10fr`→`0fr` SVG label clip (idea only).