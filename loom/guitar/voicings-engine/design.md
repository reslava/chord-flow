---
type: design
id: de_01KWKA4A1PF30X28FXZE8HWJNT
title: GuitarVoicingsEngine — introspectable operator library + inspector page
status: done
created: 2026-07-03
updated: 2026-07-03
version: 2
idea_version: 1
tags: []
parent_id: id_01KWK9BE5EP699GSDNPC01ZECB
requires_load: []
---
# GuitarVoicingsEngine — introspectable operator library + inspector page

Design for `idea.md` (GuitarVoicingsEngine — introspectable operator library + inspector page). Grounded in the real code (`CagedDerivation`, `ShellDerivation`, `ShellReduction`, `FamilyVoicing`, `CagedVoicingCatalog`, `EngineVoicingSource`, `CompingResolver`, `VoicingGridHandler`) and `voicings-engine-rules-reference.md`. All types live in `ChordFlow.Instruments.Guitar` — this is reify *in place*, no cross-instrument extraction (decision #1).

## 1. The load-bearing fact

`FamilyVoicing.Derive(family, quality, shape, root, minFret, maxFret) → ChordShape` is **already the single dispatch point**. Both live consumers go through it:
- `CompingResolver.DeriveFamily` → `FamilyVoicing.Derive` → `ChordShape` → `ToVoicing`.
- `VoicingGridHandler.TryRealize` → `FamilyVoicing.Derive` → `ChordShape` → `RealizedVoicingDiagram`.

So the operator model grows **behind that seam**. If the new operators keep producing a `ChordShape` as one field, the two consumers never change and every golden oracle stays byte-identical. That is the whole backward-compat story (decision #2: trace first-class, `grip` is one field).

## 2. The three new types

### 2.1 `IVoicingOperator` — the introspectable transform (guitar-scoped)

```
interface IVoicingOperator
{
    VoicingFamily Family { get; }        // stable identity — reuse the existing enum + VoicingFamilies.Token()
    OperatorKind Kind { get; }           // DeriveFromFormula | Reduce | Revoice | Augment  (rules-ref §1)
    string DisplayName { get; }          // "CAGED (full chord)", "Doubled shell (chord − 5th)", "Shell (guide-tone)"
    ParameterSchema Parameters { get; }  // the operator-specific knobs the page renders (§2.3)

    VoicingDerivation Derive(VoicingRequest request);
}
```

`VoicingRequest` carries the **universal axes** every operator takes — `Quality`, `PitchClass Root`, `FretRegion (min,max)` — plus a `ParameterValues` bag validated against `Parameters`. Quality/root/region are *not* operator params (they are the request); the operator's own `ParameterSchema` is only the extra knobs (shape/form/operand).

Three implementations, each a thin wrapper over the existing static deriver — **no derivation logic moves or changes**:
- `CagedOperator` (Kind=DeriveFromFormula) → wraps `CagedDerivation.Derive`.
- `ShellOperator` (Kind=DeriveFromFormula) → wraps `ShellDerivation.Derive`.
- `DoubledShellOperator` (Kind=Reduce) → composes: derive via `CagedOperator`, then `ShellReduction.MuteFifth` (§4 composition).

A static registry `VoicingOperators.All : IReadOnlyList<IVoicingOperator>` (keyed by `VoicingFamily`) is the enumeration surface for the page and the new dispatch home. `FamilyVoicing.Derive` becomes a **grip shim** over it (`VoicingOperators.For(family).Derive(req).Grip`) so callers stay literally unchanged.

### 2.2 `VoicingDerivation` — the trace (first-class)

```
record VoicingDerivation(
    VoicingFamily Family,
    OperatorKind Kind,
    IReadOnlyList<ParameterValue> Params,      // echo of the resolved knobs (for display + the id)
    IReadOnlyList<ToneSelection> ToneSelection, // the ABSTRACT voicing — instrument-agnostic half
    IReadOnlyList<RealizationStep> Realization, // the "show your work" — how tones hit the neck
    ChordShape Grip);                           // the final grip — the ONLY field old consumers read
```

- **`ToneSelection`** = `(int Interval, ChordToneFunction Function, Pitch Pitch)` per selected tone, read from `ChordTones.Of(chord)` filtered by the family's tone-selection rule:
  - CAGED → the full chord tones.
  - DoubledShell → full chord **minus the Fifth** (mirrors `ShellReduction`'s function-based mute).
  - Shell → root + Third + (Seventh|Sixth) (mirrors `ShellDerivation`).
  This is the left column of the page and the seam the future `Music.Harmony` extraction will lift out — represented as *data* now, physically moved only when Drop2 / instrument #2 forces it (rules-ref §2; decision #1).
- **`RealizationStep`** = the ordered narration of the geometry pass (anchor, bass root, reach window, mutes, per-string pick, anchor finger). The right column. See open decision **OD-1** for its shape.
- **`Grip`** unchanged `ChordShape`.

### 2.3 `ParameterSchema` — declared, typed inputs

Each operator *declares* its knobs so any UI auto-renders the right control and stays generic as operators grow. v1 needs exactly two parameter kinds:

- `EnumParam(name, Type enumType, default)` — CAGED `shape` (CagedShape C/A/G/E/D), Shell `form` (CagedShape C/E), DoubledShell `baseShape` (its operand's CAGED shape — C only today).
- `RegionParam(name, min, max default)` — the neck window `[minFret,maxFret]` (default `[0,15]`, mirroring `VoicingGridHandler.NeckMaxFret`).

`ParameterSchema` = `IReadOnlyList<ParameterDef>`; `ParameterValues` validates a request against it (unknown key / out-of-enum → fail loud). The page reads `Parameters` to build its controls; `CagedVoicingCatalog.ShapesFor(family, quality)` still gates *which* enum values are actually offered for a given quality (so the inspector can't request a shell of a triad).

## 3. Reify per family — zero grip regression

The refactor is **structural only**. Each operator's `Derive` calls the *existing* static method for the grip, then decorates it with the trace:
- CAGED/Shell: call the deriver, then build `ToneSelection` from `ChordTones` and `Realization` from the values the pipeline already computes (anchors, `stacksUp`, window, chosen tones, anchor finger — all local in `CagedDerivation` today; surfaced by having `Derive` optionally emit them, or by a parallel narrator that recomputes them from the returned `ChordShape` + inputs — **OD-1** picks which).
- DoubledShell: `CagedOperator.Derive` (its full trace) → append a single `Reduce` step ("muted the Fifth on strings …") → `MuteFifth(grip)`.

**Invariant (the acceptance gate):** for every one of the 64 `CagedVoicingCatalog.Combos` at every root, `operator.Derive(req).Grip` is **byte-identical** to today's `FamilyVoicing.Derive(...)`. The 36 CAGED + 12 shell authored oracles and the doubled-shell inherited-oracle tests pass unchanged. The reify changes structure, never a grip.

## 4. Composition seam (minimal)

Reduce operators take another operator's output as their operand. v1 models **only the one composition that exists** — `DoubledShell = Reduce(operand: Caged)`:

```
DoubledShellOperator.Derive(req):
    inner = cagedOperator.Derive(req)           // full CAGED trace + grip
    grip  = ShellReduction.MuteFifth(inner.Grip)
    return inner with { Family=DoubledShell, Kind=Reduce,
                        ToneSelection = inner.ToneSelection without Fifth,
                        Realization  = inner.Realization + MuteFifthStep,
                        Grip = grip }
```

No arbitrary user-authored pipelines (deferred, idea "Out"). The operand is fixed in code; the page picks the operand's *shape* param, nothing more.

## 5. Deliverable 2 — the Voicings Engine inspector page

R already exists: `guitar-voicings-render-component.js` (the faceted grid) + the shared `fretboard-render-component.js` (single FretR box). The inspector is a **different interaction** from the browse grid — pick *one* operator + params, see *one* derivation with its trace — so:

- **New bridge verb `voicingDerive`** `{ family, quality, root, params:{shape|form, minFret, maxFret} }` → reply **`voicingDerivation`** `{ id, family, kind, toneSelection:[{interval, function, note}], realizationSteps:[…], diagram:FretboardDiagram }`. One handler (`VoicingDeriveHandler`), one round-trip, reusing `RealizedVoicingDiagram.Build` for the grip (same path `VoicingGridHandler` uses). `id` reuses `AutomaticVoicingId.For` — the existing copy-to-clipboard oracle handle.
- **New nav view "Voicings Engine"** (sibling of Voicings/Scales/CAGED, same lazy `views`/`onShow` mount). Two columns: **left** = the abstract voicing (tone-selection list: function · interval · note) + the ordered derivation steps; **right** = the realized grip via a single `ChordFlowFretboard` box. Operator picker + quality + root + the operator's declared params drive it.
- **Dependency:** the page's build is step-level `blockedBy` the single-box FretR (which exists) — so it is effectively **unblocked**; only the new `voicingDerive` verb is net-new. (Decision #3.)

## 6. Settled decisions (signed off in chat-002)

- **OD-1 — `RealizationStep` shape → DECIDED: both.** A structured `RealizationStep(StepKind, data…, string Label)` — structured so tests can assert the pipeline narration and the page can style each kind, with a rendered `Label` for display. This makes "explain this voicing" a real data product, not a formatted string. Consequence: CAGED's pipeline must *emit* its intermediate values (anchors/window/picks) rather than us recomputing them — a modest touch to `CagedDerivation` (see OD-2).
- **OD-2 — where the trace is built → DECIDED: emit-from-deriver.** The deriver emits the trace (CAGED/Shell/reduction each return `VoicingDerivation`), and `FamilyVoicing.Derive` extracts `.Grip`. One source of "why" — no separate narrator that could drift from the real pipeline.
- **OD-3 — page placement → DECIDED: dedicated "Voicings Engine" nav view** (inspector ≠ the browse grid), a sibling of Voicings/Scales/CAGED using the same lazy `views`/`onShow` mount.

## 7. Testing

- **Regression gate (must pass first):** all existing oracle + coverage tests green, grips byte-identical (§3 invariant), `CompingResolver` + `VoicingGridHandler` output unchanged.
- **New trace tests:** for representative combos assert `ToneSelection` matches the quality formula by function (root/3rd/5th/7th present or correctly absent per family) and `RealizationStep`s reconstruct the grip (the steps' end state == `Grip`).
- **New verb test:** `VoicingDeriveHandler` returns a well-formed `voicingDerivation` for a valid request and fails loud on an unknown param / ineligible (family,quality) combo — mirroring `VoicingGridHandlerTests`.

## 8. Reference-doc sync (required)

- **Update `voicings-engine-rules-reference.md`** in the same unit of work once operators become first-class: §2 (operators now carry an explicit tone-selection vs realization split *as data*, still one namespace) and §4 (each family is now an `IVoicingOperator` behind `FamilyVoicing`/`VoicingOperators`).
- **Author `voicings-engine-reference.md`** (the architecture map) at the *end* of this thread, once the operator set has been exercised — then add its row to `CLAUDE-LOCAL.md → Reference-doc sync`.

## 9. Scope guard

In: the 3 types, the reify of the 3 families in place, the trace, the `voicingDerive` verb, the inspector view. Out (deferred): any new family (Drop2/6/9), the physical `Music.Harmony` tone-selection extraction, `IVoicingsE`/cross-instrument core, user-authored pipelines. This thread makes the existing engine *introspectable and explainable* — it adds no new voicing.
