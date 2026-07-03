---
type: plan
id: pl_01KWKAX0SHRY28PKX3WSE60V30
title: Reify the voicings engine as an introspectable operator library + inspector page
status: done
created: 2026-07-03
updated: 2026-07-03
version: 3
design_version: 2
req_version: 1
tags: []
parent_id: de_01KWKA4A1PF30X28FXZE8HWJNT
requires_load: []
target_version: 0.1.0
steps:
  - id: type-foundation
    order: 1
    status: done
    description: "Add the pure operator-model types (no behavior yet): OperatorKind, IVoicingOperator, VoicingRequest (+ FretRegion), VoicingDerivation, ToneSelection, RealizationStep (StepKind + Label), and ParameterSchema/ParameterDef/ParameterValue(s) with validation."
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Caged/OperatorKind.cs, src/ChordFlow.Core/Instruments/Guitar/Caged/IVoicingOperator.cs, src/ChordFlow.Core/Instruments/Guitar/Caged/VoicingRequest.cs, src/ChordFlow.Core/Instruments/Guitar/Caged/VoicingDerivation.cs, src/ChordFlow.Core/Instruments/Guitar/Caged/RealizationStep.cs, src/ChordFlow.Core/Instruments/Guitar/Caged/ParameterSchema.cs]
    blocked_by: []
    satisfies: [IN1, IN2, IN3, IN4, IN5, IN6, IN7]
  - id: cagedoperator-emit-trace
    order: 2
    status: done
    description: Make CagedDerivation emit its intermediate values (anchors, stacksUp, reach window, per-string picks, anchor finger) as RealizationSteps and build the full-chord ToneSelection, returning a VoicingDerivation. Wrap it in CagedOperator (Kind=DeriveFromFormula, params shape+region).
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Caged/CagedDerivation.cs, src/ChordFlow.Core/Instruments/Guitar/Caged/CagedOperator.cs]
    blocked_by: []
    satisfies: [IN8, IN10, IN5, IN6, IN7]
  - id: shelloperator-emit-trace
    order: 3
    status: done
    description: Make ShellDerivation emit its form-based RealizationSteps (root string, guide tones nearest-fret, compact anchor) and build the shell ToneSelection (root + 3rd + 7th|6th), returning a VoicingDerivation. Wrap it in ShellOperator (Kind=DeriveFromFormula, params form+region).
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Caged/ShellDerivation.cs, src/ChordFlow.Core/Instruments/Guitar/Caged/ShellOperator.cs]
    blocked_by: []
    satisfies: [IN8, IN5, IN6, IN7]
  - id: doubledshelloperator-reduce-composition
    order: 4
    status: done
    description: "Model doubled-shell as Reduce(operand: Caged): derive via CagedOperator, mute the fifth (ShellReduction.MuteFifth), append the reduce RealizationStep, and drop the Fifth from ToneSelection. Params baseShape+region, Kind=Reduce."
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Caged/DoubledShellOperator.cs, src/ChordFlow.Core/Instruments/Guitar/Caged/ShellReduction.cs]
    blocked_by: []
    satisfies: [IN8, IN5, IN6]
  - id: registry-grip-shim-regression-gate
    order: 5
    status: done
    description: "Add the VoicingOperators registry (All / For(family)) and rewrite FamilyVoicing.Derive as a grip shim over it. Run the full existing suite: all oracles, coverage, CompingResolver and VoicingGridHandler must be green with byte-identical grips."
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Caged/VoicingOperators.cs, src/ChordFlow.Core/Instruments/Guitar/Caged/FamilyVoicing.cs]
    blocked_by: []
    satisfies: [IN9, C1, C2]
  - id: trace-tests
    order: 6
    status: done
    description: "New tests asserting the trace is correct: ToneSelection matches the quality formula by function per family, and the RealizationSteps end state reconstructs the Grip."
    files_touched: [tests/ChordFlow.Core.Tests/VoicingDerivationTests.cs]
    blocked_by: []
    satisfies: [IN14]
  - id: voicingderive-verb-handler
    order: 7
    status: done
    description: Add the voicingDerive bridge verb + VoicingDeriveHandler returning id/family/kind/toneSelection/realizationSteps/diagram, plus a voicingOperators catalog verb exposing each operator's ParameterSchema so the page can auto-render controls. Wire in Program.cs; add handler tests.
    files_touched: [src/ChordFlow.Core/Features/Voicings/VoicingDeriveHandler.cs, src/ChordFlow.Core/Bridge/VoicingDeriveEnvelopes.cs, src/ChordFlow.Desktop/Program.cs, tests/ChordFlow.Core.Tests/VoicingDeriveHandlerTests.cs]
    blocked_by: []
    satisfies: [IN11, IN14, IN16]
  - id: voicings-engine-inspector-view
    order: 8
    status: done
    description: "Add the Voicings Engine top-level nav view: operator + quality + root + declared-params controls, a left column (abstract voicing: function/interval/note + ordered derivation steps) and a right column (realized grip via a single ChordFlowFretboard box)."
    files_touched: [src/ChordFlow.Desktop/wwwroot/index.html, src/ChordFlow.Desktop/wwwroot/voicings-engine.js, src/ChordFlow.Desktop/wwwroot/bridge.js]
    blocked_by: []
    satisfies: [IN12, IN15]
  - id: ref-sync-voicings-engine-rules-reference
    order: 9
    status: done
    description: "Reference-doc sync (same unit of work): update voicings-engine-rules-reference §2/§4 (the operator model as built) and chordflow-architecture-reference §5 (the new voicingDerive + voicingOperators wire contract and the Voicings Engine nav view)."
    files_touched: [loom/refs/voicings-engine-rules-reference.md, loom/refs/chordflow-architecture-reference.md]
    blocked_by: []
    satisfies: [IN13, IN16]
---
# Reify the voicings engine as an introspectable operator library + inspector page

## Goal

Turn the existing three static derivation classes (CagedDerivation / ShellDerivation / ShellReduction) into a declarative, introspectable operator library behind the existing FamilyVoicing dispatch seam, and dogfood it through a new Voicings Engine inspector page. Each family becomes an IVoicingOperator that declares its kind + typed ParameterSchema and emits a first-class VoicingDerivation trace (abstract ToneSelection + structured RealizationSteps + the unchanged ChordShape grip). The reify is structural only: the acceptance gate (C1) is that every one of the 64 catalog combos derives a byte-identical grip and every existing oracle/consumer stays green — FamilyVoicing.Derive becomes a grip shim so CompingResolver and VoicingGridHandler are untouched. Then a voicingDerive bridge verb + a two-column inspector view (abstract voicing plus realized grip and derivation steps) make the explainable-voicings differentiator visible. No new voicing family ships; tone-selection stays represented as data in Instruments/Guitar (the Music.Harmony extraction is deferred to Drop2 / instrument #2). Steps are ordered regression-gate-first: build the operators, prove grip identity, then add tests, the verb, the view, and the ref-sync.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add the pure operator-model types (no behavior yet): OperatorKind, IVoicingOperator, VoicingRequest (+ FretRegion), VoicingDerivation, ToneSelection, RealizationStep (StepKind + Label), and ParameterSchema/ParameterDef/ParameterValue(s) with validation. | src/ChordFlow.Core/Instruments/Guitar/Caged/OperatorKind.cs, src/ChordFlow.Core/Instruments/Guitar/Caged/IVoicingOperator.cs, src/ChordFlow.Core/Instruments/Guitar/Caged/VoicingRequest.cs, src/ChordFlow.Core/Instruments/Guitar/Caged/VoicingDerivation.cs, src/ChordFlow.Core/Instruments/Guitar/Caged/RealizationStep.cs, src/ChordFlow.Core/Instruments/Guitar/Caged/ParameterSchema.cs | — | IN1, IN2, IN3, IN4, IN5, IN6, IN7 |
| ✅ | 2 | Make CagedDerivation emit its intermediate values (anchors, stacksUp, reach window, per-string picks, anchor finger) as RealizationSteps and build the full-chord ToneSelection, returning a VoicingDerivation. Wrap it in CagedOperator (Kind=DeriveFromFormula, params shape+region). | src/ChordFlow.Core/Instruments/Guitar/Caged/CagedDerivation.cs, src/ChordFlow.Core/Instruments/Guitar/Caged/CagedOperator.cs | — | IN8, IN10, IN5, IN6, IN7 |
| ✅ | 3 | Make ShellDerivation emit its form-based RealizationSteps (root string, guide tones nearest-fret, compact anchor) and build the shell ToneSelection (root + 3rd + 7th\|6th), returning a VoicingDerivation. Wrap it in ShellOperator (Kind=DeriveFromFormula, params form+region). | src/ChordFlow.Core/Instruments/Guitar/Caged/ShellDerivation.cs, src/ChordFlow.Core/Instruments/Guitar/Caged/ShellOperator.cs | — | IN8, IN5, IN6, IN7 |
| ✅ | 4 | Model doubled-shell as Reduce(operand: Caged): derive via CagedOperator, mute the fifth (ShellReduction.MuteFifth), append the reduce RealizationStep, and drop the Fifth from ToneSelection. Params baseShape+region, Kind=Reduce. | src/ChordFlow.Core/Instruments/Guitar/Caged/DoubledShellOperator.cs, src/ChordFlow.Core/Instruments/Guitar/Caged/ShellReduction.cs | — | IN8, IN5, IN6 |
| ✅ | 5 | Add the VoicingOperators registry (All / For(family)) and rewrite FamilyVoicing.Derive as a grip shim over it. Run the full existing suite: all oracles, coverage, CompingResolver and VoicingGridHandler must be green with byte-identical grips. | src/ChordFlow.Core/Instruments/Guitar/Caged/VoicingOperators.cs, src/ChordFlow.Core/Instruments/Guitar/Caged/FamilyVoicing.cs | — | IN9, C1, C2 |
| ✅ | 6 | New tests asserting the trace is correct: ToneSelection matches the quality formula by function per family, and the RealizationSteps end state reconstructs the Grip. | tests/ChordFlow.Core.Tests/VoicingDerivationTests.cs | — | IN14 |
| ✅ | 7 | Add the voicingDerive bridge verb + VoicingDeriveHandler returning id/family/kind/toneSelection/realizationSteps/diagram, plus a voicingOperators catalog verb exposing each operator's ParameterSchema so the page can auto-render controls. Wire in Program.cs; add handler tests. | src/ChordFlow.Core/Features/Voicings/VoicingDeriveHandler.cs, src/ChordFlow.Core/Bridge/VoicingDeriveEnvelopes.cs, src/ChordFlow.Desktop/Program.cs, tests/ChordFlow.Core.Tests/VoicingDeriveHandlerTests.cs | — | IN11, IN14, IN16 |
| ✅ | 8 | Add the Voicings Engine top-level nav view: operator + quality + root + declared-params controls, a left column (abstract voicing: function/interval/note + ordered derivation steps) and a right column (realized grip via a single ChordFlowFretboard box). | src/ChordFlow.Desktop/wwwroot/index.html, src/ChordFlow.Desktop/wwwroot/voicings-engine.js, src/ChordFlow.Desktop/wwwroot/bridge.js | — | IN12, IN15 |
| ✅ | 9 | Reference-doc sync (same unit of work): update voicings-engine-rules-reference §2/§4 (the operator model as built) and chordflow-architecture-reference §5 (the new voicingDerive + voicingOperators wire contract and the Voicings Engine nav view). | loom/refs/voicings-engine-rules-reference.md, loom/refs/chordflow-architecture-reference.md | — | IN13, IN16 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:type-foundation -->
### Step 1 — Type foundation

Pure types only, all in ChordFlow.Instruments.Guitar. OperatorKind = DeriveFromFormula|Reduce|Revoice|Augment. VoicingRequest = (Quality, PitchClass Root, FretRegion) + ParameterValues; FretRegion defaults [0,15] (mirrors VoicingGridHandler.NeckMaxFret). ParameterValues validates against a ParameterSchema — unknown key / out-of-enum fails loud (C3). No operator instantiated, nothing wired: the solution builds green with zero behavior change.

<!-- step:cagedoperator-emit-trace -->
### Step 2 — CagedOperator + emit trace

Emit-from-deriver (OD-2): the pipeline already computes anchors/stacksUp/window/chosen/anchorFinger locally — surface them as ordered structured RealizationSteps rather than recomputing. ToneSelection = ChordTones.Of(chord) (full chord) as (interval, function, pitch). CagedOperator declares EnumParam(shape: CagedShape) + RegionParam(region). Grip must stay identical (proven in step 5). Parallelizable with step 3.

<!-- step:shelloperator-emit-trace -->
### Step 3 — ShellOperator + emit trace

ToneSelection = root + Third + (Seventh|Sixth) via ChordTones, mirroring the deriver's guide-tone choice. ShellOperator declares EnumParam(form: CagedShape C|E) + RegionParam. Grip identical to today's 12-grip oracle. Parallelizable with step 2.

<!-- step:doubledshelloperator-reduce-composition -->
### Step 4 — DoubledShellOperator (Reduce composition)

The one composition v1 supports (EX4). Inherits the inner CAGED trace, appends a Reduce step (muted the Fifth on string(s), function-based), and its ToneSelection is the inner selection minus the Fifth. Depends on step 2.

<!-- step:registry-grip-shim-regression-gate -->
### Step 5 — Registry + grip shim + regression gate

THE ACCEPTANCE GATE. FamilyVoicing.Derive(...) = VoicingOperators.For(family).Derive(req).Grip. Both consumers read .Grip and are untouched. Nothing downstream proceeds until the 36 CAGED + 12 shell oracles + doubled-shell inherited + coverage + comping + grid tests are all green (C1). Depends on steps 2-4.

<!-- step:trace-tests -->
### Step 6 — Trace tests

Per representative combos across the three families: ToneSelection has the right functions present/absent (caged=all, dshell=no Fifth, shell=root/3rd/7-6); reassembling the strings from the RealizationSteps equals Grip. Depends on step 5.

<!-- step:voicingderive-verb-handler -->
### Step 7 — voicingDerive verb + handler

Reuse RealizedVoicingDiagram.Build (the same path as VoicingGridHandler) + AutomaticVoicingId.For for the id; fail loud on unknown param / ineligible (family,quality) combo (mirrors VoicingGridHandlerTests). The companion voicingOperators catalog verb (IN16, approved) projects each operator's declared ParameterSchema + eligible enum values per quality, so the inspector is a schema-driven dumb view rather than hardcoding the 3 operators in JS. Both verbs' wire contract is documented in the architecture reference §5 at the ref-sync step (9). Depends on step 5.

<!-- step:voicings-engine-inspector-view -->
### Step 8 — Voicings Engine inspector view

Lazy views/onShow mount, sibling of Voicings/Scales/CAGED. A dumb view: controls built from the voicingOperators schema, voicingDerive on any change, render the reply. Reuses the shipped single ChordFlowFretboard box (C7 — dependency already satisfied). Dogfood surface (IN15). Depends on step 7.

<!-- step:ref-sync-voicings-engine-rules-reference -->
### Step 9 — Reference-doc sync

Two gate-excluded refs — edit via loom_patch_doc/loom_update_doc. (a) `voicings-engine-rules-reference` §2/§4 (IN13) — the operator model as built. (b) `chordflow-architecture-reference` §5 (IN16) — the `voicingDerive` + `voicingOperators` wire contract and the new Voicings Engine nav view. NOT `voicings-engine-reference.md` (the engine *architecture* map) — that is EX5, authored at thread end. Depends on steps 5, 7, 8.
