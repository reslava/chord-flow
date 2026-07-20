---
type: done
id: pl_01KY0RS6WV6GN3EGF59FYHXMTA-done
title: Done — Phase 1 — Generation core
status: done
created: 2026-07-20
version: 3
tags: []
parent_id: pl_01KY0RS6WV6GN3EGF59FYHXMTA
requires_load: []
---
# Done — Phase 1 — Generation core

## Step 1 — Onset-grid model: Block (one beat's onsets), OnsetBar (4 blocks in 4/4), OnsetGrid (1–4 bars), plus the cell→tick arithmetic (cell k at subdivision n on beat b → tick b*BeatTicks + k*(BeatTicks/n); n must divide 48).

Created `Music/Rhythm/Generation/` with the onset-grid model:
- **`Block.cs`** — `sealed record Block(int Subdivision, IReadOnlyList<int> Onsets)` via a validating `Of(subdivision, params int[] onsets)` factory (sorts + de-dups, throws on out-of-range/dup) and `Empty(subdivision)`. `RequireSubdivision` enforces `TickGrid.Ppq % n == 0`. `OnsetTicks(beatTicks)` yields intra-beat ticks (`k * beatTicks/Subdivision`).
- **`OnsetBar.cs`** — `sealed record OnsetBar(IReadOnlyList<Block> Beats)`; `IsEmpty`, `Rest(beats)`, and `OnsetTicks(ts)` (the single bar-relative onset stream both projections read, `b*BeatTicks + cellTick`).
- **`OnsetGrid.cs`** — `sealed record OnsetGrid` with a guarded `Of(bars, ts)` (≥1 bar; every bar has `ts.Numerator` beats).

Enclosing-namespace resolution gives access to `TickGrid`/`TimeSignature` (in `ChordFlow.Music.Rhythm`) with no `using`. Instrument-agnostic. Decision: followed codebase precedent (`PatternBar` etc.) of `IReadOnlyList` in records without custom structural equality — the determinism test (step 10) will compare a canonical onset-tick projection rather than record `.Equals`.

## Step 2 — RhythmFamily: a named, ordered palette of non-empty Blocks at one subdivision. Author the v1 families — Quarter (subdivision 1, {[0]}) and Eighth (subdivision 2, {[0], [1], [0,1]} = on-beat / the & / both).

**`RhythmFamily.cs`** — `sealed record RhythmFamily(Name, Subdivision, IReadOnlyList<Block> Blocks)`. v1 statics: `Quarter` (subdivision 1, `{[0]}`) and `Eighth` (subdivision 2, `{[0],[1],[0,1]}` = on-beat/&/both). `All` lists them in offered order. `Primary` = first (strong on-beat) block; `Silence` = an empty beat at the family's subdivision. Triplet/16th families deferred (EX3).

## Step 3 — BarOperator — the (family, beatIndex, rng) → Block dispatch. Implement the six operators: Uniform, Isolate(k), AnchorRotate, Mask(beats), Displace(cells), Accumulate(n)/Thin(n).

**`BarOperator.cs`** — `abstract record BarOperator` with `Apply(family, beatIndex, beatsPerBar, rng) → Block` and a shared `BuildBar(family, beatsPerBar, rng) → OnsetBar`. Six sealed subtypes: `Uniform`, `Isolate(Beat)`, `AnchorRotate` (beat 0 fixed to primary, rest rotate the family list), `Mask(Beats)`, `Displace(Cells)` (shifts the primary's onset cells mod subdivision, wrap-safe), `Accumulate(Count)` / `Thin(Count)` (first-N / drop-last-N beats sound). `rng` is threaded through the signature but unused by these deterministic operators (reserved for later Random-in-family).

## Step 4 — SequenceBehaviour — the (barIndex, operatorConfig, prevBar) → operatorConfig dispatch. Implement Repeat, Cycle, Sweep (bind an operator param to barIndex), RestBar (emit an empty OnsetBar), CallResponse (content bar then empty bar).

**`SequenceBehaviour.cs`** — `abstract record SequenceBehaviour` with `BarAt(barIndex, baseOperator, family, beatsPerBar, rng) → OnsetBar`. Five sealed subtypes: `Repeat`, `Cycle` (bar N plays `family.Blocks[N % count]` on every beat — the family tour, ignores the base op), `Sweep` (pattern-matches the base op: sweeps `Isolate.Beat` by `barIndex % beatsPerBar` or `Displace.Cells` by `barIndex % subdivision`; else repeats), `RestBar(ContentBars=1, RestBars=1)` (content/silence cycle), `CallResponse` (even bar = content, odd = empty). Core builds clean, 0 warnings.

## Step 5 — Pattern strategy: PatternParams(Family, Operator, Behaviour, BarCount, Seed) and the composition loop (behaviour yields per-bar operator config → operator fills 4 beats from the family → OnsetBar → OnsetGrid).

**`PatternStrategy.cs`** + **`PatternParams.cs`** + **`GenerationParams.cs`** (the shared abstract base `GenerationParams(TimeSignature Ts, int Seed)`, defined here so both strategies derive from it). `PatternParams(Family, Operator, Behaviour, BarCount, Ts, Seed) : GenerationParams`. `PatternStrategy.Generate` seeds one `Random(Seed)`, then for each bar index calls `Behaviour.BarAt(i, Operator, Family, beatsPerBar, rng)` → `OnsetGrid.Of`. Validates BarCount ∈ [1,4] (fail loud). Decision: kept `Ts` on the params (design §3 lists it) even though 4/4-only; callers pass `TimeSignature.FourFour`.

## Step 6 — Random strategy: RandomParams(ValuePalette, ContentBars, SilenceBars, Seed). Seeded fill of ContentBars from the note-value palette, then SilenceBars empty bars; produces a (ContentBars+SilenceBars)-bar OnsetGrid.

**`RandomStrategy.cs`** + **`RandomParams.cs`**. `RandomParams(ValuePalette, ContentBars, SilenceBars, Ts, Seed)`. Values are alphaTex denominators (4/8/16) mapped to whole base-cells on a fixed v1 **sixteenth** grid (`ValueToBaseCells`: 16/value, must divide evenly — off-grid/triplet values throw, EX3). `FillBar` walks the 16-cell grid placing an onset then advancing by a random palette value (cell 0 always attacks), then groups landed cells into per-beat `Block`s at subdivision 4. Appends `SilenceBars` `OnsetBar.Rest` bars. Validates ContentBars ∈ [1,4], SilenceBars ∈ [0,4], non-empty palette.

## Step 7 — RhythmGenerator + GenerationParams(BarCount, Ts, Seed, strategy payload). The single entry point that dispatches on strategy and returns an OnsetGrid; deterministic — same {strategy, params, seed} → same grid.

**`RhythmGenerator.cs`** — `Generate(GenerationParams)` switches on the arm (`PatternParams`→PatternStrategy, `RandomParams`→RandomStrategy, else throw). The single deterministic entry point (IN6/C7). Verified by `Generate_DispatchesOnStrategyArm` + the determinism tests.

## Step 8 — OnsetGrid → RhythmPattern projection (legato, ring-to-next-onset): each onset → RhythmEvent(pos, nextOnset−pos); last onset of a bar rings to the barline (no cross-bar tie); empty bar → whole-bar rest. Stays within the verified :N + rest vocabulary.

**`OnsetGridToRhythmPattern.cs`** (Music) — legato `Project(grid, id?, name?)`: per bar, each onset → `RhythmEvent.Hit(pos, nextOnset−pos)`, last onset rings to the barline, empty bar → empty `PatternBar` (whole-bar rest). Verified-vocabulary holds by construction: onset spacings and the ring-to-barline remainder are always a single base/dotted value on this grid (proven by the `Legato_QuantizesWithoutHittingAnUnverifiedTie` theory — no FormatException). Tests confirm ring-to-next-onset, single-onset ring-to-barline (dotted half), and empty-bar.

## Step 9 — OnsetGrid → DrumGroove projection (single voice): each onset → a one-cell RhythmEvent (Hit) on one DrumLane; one DrumBar per generated bar; default voice HiHatClosed. Lives in Instruments/Drums (targets a Drums type).

**`OnsetGridToDrumGroove.cs`** (Instruments/Drums) — single-lane `Project(grid, voice=HiHatClosed, id?, name?)`: each onset → a one-cell `RhythmEvent.Hit(tick, cellTicks)` on one `DrumLane`; one `DrumBar` per bar (empty bar keeps an empty lane so the voice row still draws). Placed under Instruments/Drums (the legal `Instruments→Music` edge; C2 reverse edge never crossed — confirmed green by `MusicLayeringTests`).

## Step 10 — Unit tests: determinism (same {params, seed} → identical OnsetGrid), projection agreement (RhythmPattern event onset ticks == DrumGroove hit onset ticks for one grid), the legato projection stays inside the verified render vocabulary (renders without throwing an unverified-tie), plus per-operator / per-behaviour / per-strategy shape assertions. MusicLayeringTests confirms the Music→Instruments edge is uncrossed.

Two test files under `tests/ChordFlow.Core.Tests/Rhythm/Generation/`:
- **`RhythmGeneratorTests.cs`** — determinism (Pattern + Random, same seed → identical canonical grid), each of the six operators, the five behaviours (Sweep walks the onset, RestBar/CallResponse alternate silence, Cycle tours the eighth family), Random all-quarters + silence bars, off-grid rejection, dispatch, BarCount guard.
- **`OnsetGridProjectionTests.cs`** — legato ring-to-next-onset + ring-to-barline + empty-bar, single-voice default, 1:1 drum onsets, **projection agreement** (RhythmPattern onsets == DrumGroove hits, a `[Theory]` over 4 grids incl. a Random one), and the **verified-vocabulary** theory (each projected bar quantizes without throwing).

Full suite: **1193 passed, 0 failed** (incl. `MusicLayeringTests`).

## Step 11 — Update the domain-model reference doc with the new Music/Rhythm/Generation types and the two projections (the CLAUDE-LOCAL bidirectional ref-sync rule — same unit of work as the code).

Patched `chordflow-domain-model-reference.md` (§3 Rhythm layer): added a **"Rhythm generation (`Music/Rhythm/Generation/`)"** subsection documenting the onset-grid model (`Block`/`OnsetBar`/`OnsetGrid`), `RhythmFamily`, `BarOperator` (6 arms), `SequenceBehaviour` (5 arms), the `GenerationParams`/`PatternParams`/`RandomParams` + `RhythmGenerator`/strategies, and both projections (`OnsetGridToRhythmPattern` in Music, `OnsetGridToDrumGroove` in Instruments/Drums), inserted before "### Composable overlays". Satisfies the CLAUDE-LOCAL bidirectional ref-sync rule (same unit of work as the code).
