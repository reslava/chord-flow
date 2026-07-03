---
type: done
id: pl_01KWKAX0SHRY28PKX3WSE60V30-done
title: Done — Reify the voicings engine as an introspectable operator library + inspector page
status: done
created: 2026-07-03
version: 8
tags: []
parent_id: pl_01KWKAX0SHRY28PKX3WSE60V30
requires_load: []
---
# Done — Reify the voicings engine as an introspectable operator library + inspector page

## Step 1 — Add the pure operator-model types (no behavior yet): OperatorKind, IVoicingOperator, VoicingRequest (+ FretRegion), VoicingDerivation, ToneSelection, RealizationStep (StepKind + Label), and ParameterSchema/ParameterDef/ParameterValue(s) with validation.

Added the pure operator-model types in `ChordFlow.Instruments.Guitar` (all under `Instruments/Guitar/Caged/`), no behavior wired:

- `OperatorKind.cs` — enum DeriveFromFormula|Reduce|Revoice|Augment (only the first two instantiated).
- `RealizationStep.cs` — `RealizationStepKind` enum + `RealizationStep(Kind, Label, Strings?)` record (OD-1 structured + prose label).
- `VoicingDerivation.cs` — `ToneSelection(Interval, Function)` (abstract voicing) + `VoicingDerivation(Family, Kind, Params, ToneSelection[], Realization[], Grip)`; `Grip` is the backward-compat `ChordShape` field.
- `ParameterSchema.cs` — `ParameterDef`/`EnumParam`/`RegionParam`, `ResolvedParam`, `ParameterValues` (validated name→value bag with `Enum<T>()`), and `ParameterSchema` with `Validate(request)` (fail-loud on unknown key / missing / out-of-enum / region-out-of-bounds) + `Resolve(request)`.
- `VoicingRequest.cs` — `FretRegion(MinFret, MaxFret)` (Default `[0,15]`) + `VoicingRequest(Quality, Root, Region, Params)`.
- `IVoicingOperator.cs` — `Family/Kind/DisplayName/Parameters/Derive(request)`.

Design refinement: region is a universal request field (`FretRegion`) described/bounded by a `RegionParam` in the schema; the enum choice (shape/form) travels in `ParameterValues`. Honors both IN3 (region field) and IN7 (RegionParam). `dotnet build ChordFlow.Core` green, 0 warnings.

## Step 2 — Make CagedDerivation emit its intermediate values (anchors, stacksUp, reach window, per-string picks, anchor finger) as RealizationSteps and build the full-chord ToneSelection, returning a VoicingDerivation. Wrap it in CagedOperator (Kind=DeriveFromFormula, params shape+region).

Refactored `CagedDerivation` to emit the trace without touching the grip computation:
- Split the body into `DeriveVoicing(...) → VoicingDerivation`; `Derive(...) → ChordShape` now just returns `DeriveVoicing(...).Grip`, so every existing caller is untouched.
- Interleaved `RealizationStep`s (Anchor, BassRoot, ReachWindow, E-shape Mute, Select, Mute, AnchorFinger) alongside the unchanged pipeline; `ToneSelection` = full `ChordTones.Of(chord)`. Grip built exactly as before then wrapped in `VoicingDerivation(Caged, DeriveFromFormula, [], toneSelection, steps, grip)`.
- `CagedOperator.cs` — declares `EnumParam(shape: C/A/G/E/D, default E)` + `RegionParam(region, 0..24)`; `Derive` validates, calls `DeriveVoicing`, attaches the resolved-param echo via `with { Params = ... }`.

Full suite green: **767 passed, 0 failed** — grips byte-identical, no regression.

## Step 3 — Make ShellDerivation emit its form-based RealizationSteps (root string, guide tones nearest-fret, compact anchor) and build the shell ToneSelection (root + 3rd + 7th|6th), returning a VoicingDerivation. Wrap it in ShellOperator (Kind=DeriveFromFormula, params form+region).

Refactored `ShellDerivation` the same way: `Derive → DeriveVoicing(...).Grip`; `DeriveVoicing` runs the identical compact-anchor loop (restructured to capture the chosen root fret + compact/fallback flag — same grip result) and `BuildDerivation` assembles the trace. `ToneSelection` = every chord tone except the Fifth (root + 3rd + 7th|6th) via `ChordTones`; steps = GuideTones, Compaction (compact vs fallback anchor), AnchorFinger. Added `ShellOperator.cs` — `EnumParam(form: C/E, default C)` + `RegionParam(0..24)`, delegating to `DeriveVoicing` and attaching the param echo. Core builds clean.

## Step 4 — Model doubled-shell as Reduce(operand: Caged): derive via CagedOperator, mute the fifth (ShellReduction.MuteFifth), append the reduce RealizationStep, and drop the Fifth from ToneSelection. Params baseShape+region, Kind=Reduce.

Added `DoubledShellOperator.cs` — Kind=Reduce, `Reduce(operand: Caged)`: calls `CagedDerivation.DeriveVoicing` for the inner trace+grip, applies `ShellReduction.MuteFifth`, appends a single `Reduce` step listing the newly-muted Fifth strings (computed by diffing inner grip vs reduced), and its `ToneSelection` is the inner selection minus the Fifth. Declares `EnumParam(baseShape: C/A/G/E/D, default C)` + `RegionParam`. `ShellReduction` unchanged (diff approach needs no new API).

## Step 5 — Add the VoicingOperators registry (All / For(family)) and rewrite FamilyVoicing.Derive as a grip shim over it. Run the full existing suite: all oracles, coverage, CompingResolver and VoicingGridHandler must be green with byte-identical grips.

Added `VoicingOperators` registry (`All` ordered by family, `For(family)`, `ShapeParamName(family)` mapping shape/form/baseShape) and rewrote `FamilyVoicing.Derive` as a grip shim: builds a `VoicingRequest` and returns `VoicingOperators.For(family).Derive(request).Grip`; added `FamilyVoicing.Voicing(...)` for the full trace. Safety fix: dropped region-bounds rejection from `ParameterSchema.Validate` (RegionParam is now a descriptive UI hint only) so the shim never adds a rejection the old dispatch lacked. **Acceptance gate (C1/C2): full suite 767 passed / 0 failed** — 36 CAGED + 12 shell oracles, doubled-shell inherited, coverage, CompingResolver and VoicingGridHandler all green; grips byte-identical.

## Step 6 — New tests asserting the trace is correct: ToneSelection matches the quality formula by function per family, and the RealizationSteps end state reconstructs the Grip.

Added `VoicingDerivationTests.cs` (6 tests): CAGED ToneSelection = every chord-tone function (incl. Fifth); shell/doubled-shell omit the Fifth but keep root/3rd/7th; `EveryCombo_ToneSelection_MatchesItsFamilyRule` across all catalog combos; realization consistency across all combos × 12 roots (non-empty, has AnchorFinger step, grip sounds); CAGED Select/Mute steps partition the grip strings; doubled-shell Reduce step is last and mutes only grip-muted strings. Green.

## Step 7 — Add the voicingDerive bridge verb + VoicingDeriveHandler returning id/family/kind/toneSelection/realizationSteps/diagram, plus a voicingOperators catalog verb exposing each operator's ParameterSchema so the page can auto-render controls. Wire in Program.cs; add handler tests.

Added the inspector bridge slice:
- `VoicingDeriveEnvelopes.cs` (Features/Voicings) — outbound `voicingDerivation` (`{id, family, kind, toneSelection[], realizationSteps[], diagram}`), `voicingDeriveError`, and `voicingOperators` (`operators[]` with declared params + eligibleShapesByQuality) DTOs.
- `WebMessageRouter` — inbound `VoicingDeriveRequest` record + `MinFret`/`MaxFret` envelope fields; `VoicingDeriveRequested` / `VoicingOperatorsRequested` events + `voicingDerive` / `voicingOperators` dispatch cases (reusing Family/Quality/Shape/RootPitchClass).
- `VoicingDeriveHandler.cs` — `Derive(request)` (parse+fail-loud → `FamilyVoicing.Voicing` → `RealizedVoicingDiagram` + `AutomaticVoicingId`, tone notes spelled via `NoteSpeller`/`IntervalSpeller`) and `Operators()` (projects `VoicingOperators.All` + schemas + `CagedVoicingCatalog.ShapesFor` coverage).
- `Program.cs` — wired both verbs; `voicingDerive` wraps in try/catch → `VoicingDeriveErrorEnvelope` (UI-safe fail-loud).
- `VoicingDeriveHandlerTests.cs` (5 tests): well-formed CAGED/shell replies, fail-loud on bad family/quality/shape, ineligible shell-of-triad throws, `Operators()` projects all 3 with schemas + eligible coverage.

Full suite **778 passed / 0 failed**. (Solution build shows only pre-existing Desktop infra warnings — SQLite advisory + WindowsBase conflict — not from this change; Core is 0-warning.)

## Step 8 — Add the Voicings Engine top-level nav view: operator + quality + root + declared-params controls, a left column (abstract voicing: function/interval/note + ordered derivation steps) and a right column (realized grip via a single ChordFlowFretboard box).

Added the **Voicings Engine** inspector nav view:
- `index.html` — nav button, `#voicings-engine-view` container (operator/quality/root/shape selects + min/max fret inputs, an error line, and a two-column `#ve-abstract` | `#ve-diagram` layout), and the `voicings-engine.js` script include.
- `app.js` — registered the `voicingsEngine` view in the `views` map (lazy `onShow` → `ChordFlowVoicingsEngine.show()`).
- `voicings-engine.js` (`window.ChordFlowVoicingsEngine`) — a dumb, schema-driven view: on first show fetches the `voicingOperators` catalog (operator list + `eligibleShapesByQuality`), builds the operator/quality/shape selects from it, and issues `voicingDerive` on any change. Renders the reply as the abstract voicing (function · interval label · note table) + the ordered derivation steps (kind tag + label) on the left, and the realized grip via a single `ChordFlowFretboard` (vertical chord-box) on the right; `voicingDeriveError` shows inline.

Verified the JS↔C# wire contract field-by-field and `node --check`'d the JS (syntax OK). **Note:** the visual render was not driven here (WinForms+WebView2 desktop app — can't headlessly drive the WebView); the backend path is fully covered by `VoicingDeriveHandlerTests`, so the dogfood needs a quick eyeball in the running app (open the Voicings Engine tab).

## Step 9 — Reference-doc sync (same unit of work): update voicings-engine-rules-reference §2/§4 (the operator model as built) and chordflow-architecture-reference §5 (the new voicingDerive + voicingOperators wire contract and the Voicings Engine nav view).

Reference-doc sync (same unit of work):
- `voicings-engine-rules-reference.md` — §2 now records that the three families are first-class `IVoicingOperator`s behind the `FamilyVoicing` shim + `VoicingOperators` registry, with `ToneSelection` carrying the tone-selection axis as data *in place* (the physical `Music.Harmony` move still deferred to Drop2 / instrument #2); §4 gained an intro paragraph on the operator dispatch (`Derive`→grip shim, `Voicing`→full trace; derivers unchanged in grip logic, now also emit via `DeriveVoicing`).
- `chordflow-architecture-reference.md` — §5 gained the Voicings Engine inspector paragraph documenting the `voicingOperators` + `voicingDerive`/`voicingDerivation` + `voicingDeriveError` wire contract and the new nav view.
- Also refreshed the `WebMessageRouter` class-doc inbound vocabulary list (code) with the two verbs.

Final gate: full solution build clean (only pre-existing Desktop infra warnings), **778 tests passed / 0 failed**. NOT touched: `voicings-engine-reference.md` (the engine architecture map) — that is EX5, authored at thread end.

## Closing notes

All 9 steps complete. The guitar Voicings Engine is reified as an introspectable operator library (`IVoicingOperator` / `VoicingDerivation` / `ParameterSchema`, three operators behind the `FamilyVoicing` grip shim + `VoicingOperators` registry), with a `voicingDerive` + `voicingOperators` bridge slice and the schema-driven Voicings Engine inspector view. Acceptance gate held throughout: all 64-combo grips byte-identical, existing consumers untouched. Final: 778 tests pass, 0 failures; solution build clean (only pre-existing Desktop infra warnings). Deferred as agreed: any new family, the physical Music.Harmony tone-selection extraction, IVoicingsE, and `voicings-engine-reference.md` (the engine architecture map, EX5 — authored at thread end). Outstanding: a visual eyeball of the inspector page in the running app (the WebView couldn't be driven headlessly here).
