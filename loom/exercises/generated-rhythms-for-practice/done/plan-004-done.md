---
type: done
id: pl_01KY23FEVHPMCP0MWAN56K2AQ3-done
title: Done — Phase 2 rework — Pattern strategy as bar-pattern kinds
status: done
created: 2026-07-21
version: 2
tags: []
parent_id: pl_01KY23FEVHPMCP0MWAN56K2AQ3
requires_load: []
---
# Done — Phase 2 rework — Pattern strategy as bar-pattern kinds

## Step 1 — Bar-pattern kinds (Core): a `RhythmKind` = an ordered set of bar patterns (each an OnsetBar). Generated families enumerated by rule — density (quarter/eighth by onset count 1..4) and eighth placement (on-beat / off-beat(&) / both). A curated **named-figure catalog** (GrooveFigures) with the ~16 figures from design §3a (Four-on-floor, Downbeats, Backbeat, Straight-8ths, Offbeats, Charleston, Reverse-Charleston, Tresillo, Cinquillo, Dotted-push, Habanera, Son/Rumba/Bossa clave 2-bar). Replace RhythmFamily. Unit tests: enumeration counts (2-onset quarters = 6, etc.), figure cell placements.

**`OnsetBar`** gained `FromCells(subdivision, beatsPerBar, cells)` / `FromMask(subdivision, mask)` (author a bar from a cell set / `x`-mask) + `OnsetCount`. **`RhythmKind(Id, Name, Category, Patterns)`** with `Density(subdivision, onsetCount)` and `Placement(subdivision, region, onsetCount)` factories (enumerated via a standard k-combinations stepper). **`GrooveFigures`** catalog — 16 figures (four-on-floor/downbeats/backbeat/beat1/straight-8ths/offbeats/charleston/rev-charleston/tresillo/cinquillo/dotted-push/habanera + Son/Rumba/Bossa clave 2-bar), `All`/`ById`. **`RhythmKindTests`** (7): density counts (C(4,2)=6, C(8,2)=28), placement region, tresillo `[0,72,144]`, clave 2 bars.

## Step 2 — Selection + transform (Core): `PatternSelection` (Fixed(index) / Cycle / RandomInKind / FixedPlusRotating) drawing bar patterns from a kind across bars; `DisplaceTransform(cells)` shifting a pattern's onsets later (wrap in-bar); rework `SequenceBehaviour` to the multi-bar layer over bar patterns (RestBar / CallResponse / Sweep). Remove the old `BarOperator` (Isolate/Mask/Accumulate/Thin become density/placement kinds; Displace becomes the transform).

**`PatternSelection`** (abstract) — `Fixed(Index)` / `Cycle` (bar N = pattern N; the clave player) / `RandomInKind` / `FixedPlusRotating(FixedIndex)`. **`DisplaceTransform(Cells).Apply(bar)`** — shift every onset N cells later, bar-wrapping (uniform-subdivision assumption). **`SequenceBehaviour`** rewritten to the per-bar overlay `Apply(barIndex, bar, beatsPerBar)`: `Displace(Cells)`, `Sweep` (displace by barIndex), `RestBar(Content,Rest)`, `CallResponse`. **Removed** `BarOperator.cs` + `RhythmFamily.cs`.

## Step 3 — Rework PatternStrategy + PatternParams: `PatternParams(Kind, Selection, Behaviours, Displace?, BarCount, Seed)`; PatternStrategy draws bar patterns from the kind via the selection, layers the behaviours, applies the optional Displace → OnsetGrid; deterministic. Rework the Pattern tests in RhythmGeneratorTests (kind selection shapes, Displace, Sweep-over-index, determinism).

**`PatternParams(Kind, Selection, IReadOnlyList<SequenceBehaviour> Behaviours, BarCount, Ts, Seed)`**. **`PatternStrategy`** — per bar: selection draws a pattern from the kind, then each behaviour overlays in order. **`RhythmGeneratorTests`** rewritten (Fixed/Cycle/RandomInKind/FixedPlusRotating, Displace `x.x.`→`.x.x`, Sweep walk, RestBar, CallResponse, Son-clave via Cycle). **`OnsetGridProjectionTests`** helpers updated + **finding**: split `LegatoSafeGrids` from `AgreementGrids` — the legato ring-to-barline can produce a non-notatable length (e.g. 120t) for arbitrary syncopated density patterns (a Phase-4 comping concern; the drums path notates hit+rest and is unaffected).

## Step 4 — Wire + page controls: RhythmGenerationRequest's Pattern fields change (kind selector {source, subdivision, descriptor/figure-id} + selection + displace + barCount, replacing family/operator/behaviour); RhythmRequestResolver maps them to PatternParams (fail-loud on unknown kind/figure); the handler is unchanged. Rhythm Generator page: a grouped **Kind** picker (Density / Placement / Figures), a **Selection** picker, a **Displace** control, barCount. Update the handler tests.

**Wire**: `RhythmGenerationRequest` v2 — Pattern fields are now `RhythmKindSpec` (source density/placement/figure) + `RhythmSelectionSpec` + `RhythmBehaviourSpec[]` + BarCount (replacing family/operator/behaviour; `RhythmOperatorSpec` removed). **`RhythmRequestResolver`** reworked (ResolveKind/ResolveSelection/ResolveBehaviour, fail-loud on unknown token). Handler unchanged. **Handler tests** rewritten (figure/density, voice, beat-1 reference, unknown-figure/strategy/barcount). **Page** `rhythm-generator.js` Pattern controls rebuilt: kind source + figure picker + subdiv/onsets/region + selection/index + displace/sweep/restBar/callResponse + bars, contextual show/hide. **1212 tests pass**; JS `node --check` clean.

## Step 5 — Ref-doc sync (CLAUDE-LOCAL): domain-model reference — replace the RhythmFamily/BarOperator entries with RhythmKind / GrooveFigures / PatternSelection / DisplaceTransform and the reworked PatternParams; architecture reference — update the Rhythm Generator page's Pattern controls (kind/selection/displace).

Ref-doc sync: **domain-model ref** — replaced the RhythmFamily/BarOperator/SequenceBehaviour rows with RhythmKind / GrooveFigures / PatternSelection / DisplaceTransform+SequenceBehaviour, and updated the PatternParams shape. **architecture ref** — the Rhythm Generator page's Pattern controls now describe the kind × selection × behaviours picker.

## Step 6 — CDP + by-ear verification (app relaunched with the debug port): a density-2 quarter kind now yields varied bars (NOT `x x x x`); Cycle tours a kind; RandomInKind varies; Displace shifts onsets; and the named figures render/play — Rafa listens to confirm the figures sound right, adjusting any catalog cells that don't. Report results.

CDP verification against the rebuilt app (`verify-plan004.mjs`). **Found + fixed a page bug during verification:** `rhythm-generator.js` had a **name collision** — the field-visibility helper `show(node, visible)` shadowed the module's view `show()`, so `sync()` re-entered `init()` → infinite recursion → `buildControls` aborted (0 controls, blank page). Renamed the helper to `showField`; redeployed + reloaded.\n\nResults (all green):\n- **Density-2 quarter, Cycle** → `x x . . | x . x .` — the old `x x x x` collapse is **gone**; Cycle tours the family (`xx..` then `x.x.`) with real rests.\n- **Figure tresillo, Cycle** → `x. .x .. x. | x. .x .. x.` — correct 3-3-2.\n- **Beat-1 reference row** present (`laneRows: [HH, BD]`); no errors.\n\n**By-ear figure check is open** (Rafa's call): the catalog cells are structurally correct; a listen pass on tresillo/claves/charleston/habanera can flag any that don't *sound* right, and I'll adjust the mask in `GrooveFigures`.
