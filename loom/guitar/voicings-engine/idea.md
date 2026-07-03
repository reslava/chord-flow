---
type: idea
id: id_01KWK9BE5EP699GSDNPC01ZECB
title: GuitarVoicingsEngine — introspectable operator library + inspector page
status: done
created: 2026-07-03
version: 1
tags: []
parent_id: null
requires_load: []
---
# GuitarVoicingsEngine — introspectable operator library + inspector page

## Why

ChordFlow's north star is that it is a **chord *reasoner*, not a chord viewer** — voicings are *derived* from theory through a declarative operator library, so the app can *explain*, *re-voice*, and *voice-lead* chords instead of displaying a frozen dictionary. That library already half-exists (`CagedDerivation`, `ShellDerivation`, `ShellReduction`, `CagedVoicingCatalog`) but it is **invisible and non-introspectable**: three static classes with tone-selection and realization entangled, each returning a bare `ChordShape`. The reasoning is real but lives only in tests.

This thread turns that substrate into a first-class, **introspectable `GuitarVoicingsEngine`** and dogfoods it through a **Voicings Engine inspector page** — the live form of the golden oracle, and the surface where the "explainable voicings" differentiator becomes something you can *see*.

Grounded in `voicings-engine-rules-reference.md` (the operator taxonomy + tone-selection/realization split + the exact rules of the 3 current families) and chat-001 / chat-002 of this thread.

## What

Two deliverables.

### 1. `GuitarVoicingsEngine` — reify the operators (in place)

Refactor the 3 existing families into a **declarative operator model**, introspectable but *still inside* `Instruments/Guitar` — introspection, **not** the deferred cross-instrument extraction (that stays deferred to Drop2 / instrument #2, per the rules-ref §2/§7).

- **`IVoicingOperator`** — a named transform declaring its `Kind` (DeriveFromFormula | Reduce | Revoice | Augment), its display identity, and a **`ParameterSchema`** — typed inputs the operator declares so any UI can auto-render the right controls and stay generic as operators grow. CAGED → `shape` + `region`; Shell → `form` + `region`; DoubledShell → operates on another operator's output (an operand), not raw params.
- **`VoicingDerivation`** (the trace, first-class) — `{ operator, kind, params, toneSelection[], realizationSteps[], grip }`. `toneSelection[]` is the abstract, instrument-agnostic voicing (`(interval, function, pitch)`); `realizationSteps[]` is the ordered "show your work" of how those tones landed on the neck; `grip` is the final `ChordShape`. Existing consumers (comping resolver, `EngineVoicingSource`) keep reading `grip` unchanged.
- **Minimal composition seam** — Reduce operators (doubled-shell) take another operator's output as their operand. v1 models *only the compositions that already exist*, **not** arbitrary user-authored pipelines.
- `CagedVoicingCatalog.Combos` stays the coverage source of truth — the engine and the page both walk it, so they can't drift.

### 2. Voicings Engine inspector page

An **inspector-first** playground (not an open rule-builder): pick operator + quality + root + the operator's declared params, see the result rendered live — **both** the abstract voicing (left) *and* the realized guitar grip (right), with the derivation steps shown. Built on `guitar-voicings-render-component` (sibling thread) — the page's build is step-level blocked on R; the engine model is not.

## Scope

**In:** reify the 3 current families (CAGED, shell, doubled-shell) against their existing golden oracles with zero grip regressions; the `VoicingDerivation` trace; the `ParameterSchema` introspection; the inspector page over the operators that exist.

**Out (deferred):** any new family (Drop2/6/9/etc. — demand-driven later); the cross-instrument `VoicingsE`/`IVoicingsE` core; open-ended user-authored operator pipelines; the physical Music/Guitar namespace split of tone-selection vs realization.

## Validation

- All existing golden oracles (36 CAGED grips, 12 shell grips, doubled-shell's inherited oracle) pass **byte-identical** after the reify — the refactor changes structure, never a grip.
- `EngineVoicingSource` + comping resolver output is unchanged (they still read `grip`).
- The `VoicingDerivation` trace is correct and complete for every catalog combo (toneSelection matches the quality formula by function; realizationSteps reconstruct the grip).
- **Dogfood:** render on the Voicings Engine inspector page (built on `guitar-voicings-render-component`) — pick any operator/quality/root and see the abstract voicing + realized grip + derivation steps live.

## Flow

idea → design → req → plan. The **design** carries the heavy detail (the exact `ParameterSchema` type, how structured-vs-prose `realizationSteps` are, the per-family reify path). When the architecture doc is authored at the end, add its row (`voicings-engine-reference.md`) to `CLAUDE-LOCAL.md → Reference-doc sync`.
