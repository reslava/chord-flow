---
type: req
id: rq_01KWKAEZ1KDKBM1SB9ARTSZ9DC
title: GuitarVoicingsEngine — introspectable operator library + inspector page — Requirements
status: locked
created: 2026-07-03
updated: 2026-07-03
version: 2
design_version: 2
tags: []
parent_id: de_01KWKA4A1PF30X28FXZE8HWJNT
requires_load: []
---
# GuitarVoicingsEngine — introspectable operator library + inspector page — Requirements

Authoritative scope for `voicings-engine` (GuitarVoicingsEngine — introspectable operator library + inspector page). Derived from `design.md` and grounded in the current code (`CagedDerivation`, `ShellDerivation`, `ShellReduction`, `FamilyVoicing`, `CagedVoicingCatalog`, `EngineVoicingSource`, `CompingResolver`, `VoicingGridHandler`). This thread makes the **existing** engine introspectable + explainable — it adds **no new voicing**. All new engine types live in `ChordFlow.Instruments.Guitar` (reify *in place*, no cross-instrument extraction). Decisions OD-1/2/3 are settled in the design (§6).

### ✅ Included

- `IN1` New `OperatorKind` enum: `DeriveFromFormula, Reduce, Revoice, Augment` (rules-ref §1). Only the first two are instantiated this thread; the latter two are named for the taxonomy.
- `IN2` New `IVoicingOperator` (guitar-scoped, `Instruments/Guitar`): `VoicingFamily Family` (reuse the existing enum + `VoicingFamilies.Token()`), `OperatorKind Kind`, `string DisplayName`, `ParameterSchema Parameters`, `VoicingDerivation Derive(VoicingRequest)`.
- `IN3` New `VoicingRequest` — the universal axes every operator takes: `Quality`, `PitchClass Root`, `FretRegion (MinFret, MaxFret)` (default `[0,15]`, mirroring `VoicingGridHandler.NeckMaxFret`) + a `ParameterValues` bag validated against the operator's `ParameterSchema`. Quality/root/region are the *request*, not operator params.
- `IN4` New `VoicingDerivation` record: `(VoicingFamily Family, OperatorKind Kind, IReadOnlyList<ParameterValue> Params, IReadOnlyList<ToneSelection> ToneSelection, IReadOnlyList<RealizationStep> Realization, ChordShape Grip)`. `Grip` is the same `ChordShape` old consumers already read — the backward-compat field.
- `IN5` New `ToneSelection` = `(int Interval, ChordToneFunction Function, Pitch Pitch)` — the abstract, instrument-agnostic voicing, read from `ChordTones.Of(chord)` filtered by the family's tone-selection rule: **caged** = full chord tones; **doubled-shell** = full chord **minus the Fifth** (function-based, mirroring `ShellReduction`); **shell** = root + Third + (Seventh|Sixth) (mirroring `ShellDerivation`). Represented as data in `Instruments/Guitar` now — the seam a future `Music.Harmony` extraction lifts out (EX2).
- `IN6` New `RealizationStep` = a **structured** record `(StepKind, <data>, string Label)` (OD-1) — the ordered "show your work" of the geometry pass (anchor · bass root · reach window · mutes · per-string pick · anchor finger; plus the reduce step for doubled-shell). Structured for test assertions + page styling; `Label` for display.
- `IN7` New `ParameterSchema` = `IReadOnlyList<ParameterDef>` with two v1 kinds: `EnumParam(name, enumType, default)` (CAGED `shape`: CagedShape C/A/G/E/D; Shell `form`: CagedShape C/E; DoubledShell `baseShape`: C only today) and `RegionParam(name, min, max default)`. `ParameterValues` validates a request against the schema (unknown key / out-of-enum ⇒ fail loud). `CagedVoicingCatalog.ShapesFor(family, quality)` still gates *which* enum values are offered per quality (no shell of a triad).
- `IN8` Three operators wrapping the **existing** derivers with **no derivation-logic change**: `CagedOperator`(DeriveFromFormula→`CagedDerivation.Derive`), `ShellOperator`(DeriveFromFormula→`ShellDerivation.Derive`), `DoubledShellOperator`(Reduce = `CagedOperator.Derive` then `ShellReduction.MuteFifth`). DoubledShell models the **one** composition v1 supports (`Reduce(operand: Caged)`) — no arbitrary pipelines (EX4).
- `IN9` New static registry `VoicingOperators` (`All : IReadOnlyList<IVoicingOperator>`, `For(VoicingFamily)`) — the enumeration surface for the page and the new dispatch home. `FamilyVoicing.Derive` becomes a **grip shim** over it (`VoicingOperators.For(family).Derive(req).Grip`), leaving both callers untouched.
- `IN10` `CagedDerivation` (and `ShellDerivation`) **emit** their intermediate values as the trace (OD-2, emit-from-deriver): each returns/produces a `VoicingDerivation`; the grip shim extracts `.Grip`. One source of "why" — no separate narrator.
- `IN11` New bridge verb `voicingDerive` `{ family, quality, root, params:{ shape|form, minFret, maxFret } }` → reply `voicingDerivation` `{ id, family, kind, toneSelection:[{interval, function, note}], realizationSteps:[{kind, label}], diagram:FretboardDiagram }`. One `VoicingDeriveHandler` (Features/Voicings), one round-trip, reusing `RealizedVoicingDiagram.Build` (the same path `VoicingGridHandler` uses) and `AutomaticVoicingId.For` for the `id`. Wired in `Program.cs` like the other verbs.
- `IN12` New **"Voicings Engine"** top-level nav view (OD-3) in `index.html`, lazily mounted (`views`/`onShow`, sibling of Voicings/Scales/CAGED). Controls: operator picker + quality + root + the operator's declared params. Two columns: **left** = abstract voicing (tone-selection: function · interval · note) + ordered derivation steps; **right** = the realized grip via a single `ChordFlowFretboard` box. A dumb view — theory stays in Core.
- `IN13` Ref-sync in the same unit of work: update `voicings-engine-rules-reference.md` §2 (tone-selection vs realization now carried *as data* on the derivation, still one namespace) and §4 (each family is now an `IVoicingOperator` behind `FamilyVoicing`/`VoicingOperators`).
- `IN14` Tests: (a) the **regression gate** — all existing oracle + coverage + `CompingResolver` + `VoicingGridHandler` tests green, grips byte-identical (C1); (b) **trace tests** — `ToneSelection` matches the quality formula by function per family, and the `RealizationStep`s' end state reconstructs `Grip`; (c) **verb test** — `VoicingDeriveHandler` returns a well-formed reply and fails loud on an unknown param / ineligible `(family,quality)` combo (mirrors `VoicingGridHandlerTests`).
- `IN15` Dogfood: the engine renders on the **Voicings Engine inspector page** — pick any operator/quality/root and see the abstract voicing + realized grip + derivation steps live.
- `IN16` New bridge verb `voicingOperators` (companion to `IN11`) — the **catalog/introspection** verb: `{}` → reply `voicingOperators` `{ operators:[{ family, kind, displayName, params:[ParameterDef], eligibleShapesByQuality }] }`, projecting the `VoicingOperators` registry + each operator's declared `ParameterSchema` (+ `CagedVoicingCatalog.ShapesFor` gating). Makes the inspector a **schema-driven dumb view** (`IN12`) rather than hardcoding the 3 operators in JS. Same `VoicingDeriveHandler`/Features slice; one round-trip. Documented in the architecture reference §5 (the bridge wire-protocol home, alongside `voicingGrid`) in the same unit of work as the verb step.

### ❌ Excluded

- `EX1` Any **new voicing family** (Drop2/Drop3, 6/9, add9, sus, rootless) — demand-driven, added later against real songs. This thread ships zero new grips.
- `EX2` The **physical `Music.Harmony` extraction** of tone-selection operators — deferred until the first re-voice operator (Drop2) or a second instrument forces it (extract from ≥2 real cases). Tone-selection is represented as data in `Instruments/Guitar` now.
- `EX3` The **cross-instrument core** `VoicingsE`/`IVoicingsE` — deferred to instrument #2.
- `EX4` **User-authored / drag-your-own operator pipelines** — inspector-first only; the one composition (doubled-shell over CAGED) is fixed in code.
- `EX5` **`voicings-engine-reference.md`** (the engine *architecture* map) — authored at the **end** of this thread once the operator set is exercised, then its row is added to `CLAUDE-LOCAL.md → Reference-doc sync`. Not part of this build.
- `EX6` Any **change to a grip, oracle, or catalog coverage** — the reify is structural only.

### ⛓ Constraints

- `C1` **Grip byte-identity (the acceptance gate):** for every one of the 64 `CagedVoicingCatalog.Combos` at every root, `operator.Derive(req).Grip` equals today's `FamilyVoicing.Derive(...)`. The 36 CAGED + 12 shell authored oracles and the doubled-shell inherited oracle pass unchanged.
- `C2` **Consumers untouched:** `CompingResolver` and `VoicingGridHandler` compile and behave identically — they read `.Grip` through the `FamilyVoicing` grip shim (IN9).
- `C3` **Pure & deterministic:** the operator types have no I/O/UI, fully unit-tested; they read `Music.Harmony` (`ChordTones`/`QualityIntervals`) and never the reverse — no magic semitone.
- `C4` **Dependency direction:** operator model in `Instruments/Guitar`; verb/handler in `Features`; view in `wwwroot`. `Music` never references `Instruments` — NetArchTest + `MusicLayeringTests` stay green.
- `C5` **Reify in place:** no cross-instrument extraction and no `Music`↔`Guitar` namespace move this thread — honors the deferral (EX2/EX3, decision #1).
- `C6` **Single source of "why":** the deriver emits the trace (OD-2); no parallel narrator that could drift from the pipeline.
- `C7` **Page dependency:** the inspector view depends on the existing single `ChordFlowFretboard` box (already shipped in `voicings-render-component`); only the `voicingDerive` verb is net-new backend. Expressed as a step-level `blockedBy`, not a thread block (decision #3).
- `C8` **Computed, never stored:** derivations + traces never flow through SQLite.
