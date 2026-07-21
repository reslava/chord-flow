---
type: design
id: de_01KY0RDXS9C7X93BX8Y1HVCMC3
title: Generated Rhythms for Practice
status: done
created: 2026-07-20
updated: 2026-07-21
version: 3
idea_version: 1
tags: []
parent_id: id_01KY0R4KJ4ZKWFVWQJ6T5MCJFR
requires_load: []
---
# Generated Rhythms for Practice

Design for the rhythm **generation engine** and its pedagogy layer (see `idea.md`). This doc settles the domain model, the projection seams, the strategy/operator/behaviour API, where each piece lives, and — the deliverable the thread is organized around — the **phase split** (one plan per phase).

---

## 1. The load-bearing model: an instrument-agnostic onset grid

The generator core knows nothing about durations, sustain, drums, or pitches. It produces an **onset grid** — *which cells carry an attack* — and a **projection** step turns that into the concrete play-unit for the chosen instrument. This is the existing "two DSLs, one 48-PPQ model" seam applied to generation.

### New domain types (`Music/Rhythm/Generation/`)

Pure, immutable, no I/O, instrument-agnostic (references nothing under `Instruments/`).

| Type | Role |
|------|------|
| `Block(int Subdivision, IReadOnlyList<int> Onsets)` | **One beat's** onset pattern. `Subdivision` = cells per beat (the `:n` of the Rhythm DSL: 1 quarter, 2 eighths, 3 triplets, 4 sixteenths). `Onsets` ⊂ `[0, Subdivision)` = which cells inside the beat attack. On-beat eighth = `Block(2, [0])`; the `&` = `Block(2, [1])`; both = `Block(2, [0,1])`; an **empty** beat = `Block(n, [])`. Block **=** one beat is the locked canonical unit. |
| `OnsetBar(IReadOnlyList<Block> Beats)` | One bar = 4 `Block`s in 4/4 (one per beat). Beats may carry different subdivisions (per-beat subdivision runs — the Rhythm DSL already supports this; v1 families keep a bar uniform, but the model is future-proof by construction). |
| `OnsetGrid(IReadOnlyList<OnsetBar> Bars, TimeSignature Ts)` | The whole generated result — 1–4 bars of pure attack positions. No durations, no instrument, no pitch. **This is the generator's only output type.** |

**Absolute cell → tick.** A `Block`'s cell *k* at subdivision *n* on beat *b* sits at tick `b*BeatTicks + k*(BeatTicks/n)` (BeatTicks = 48). `n` must divide 48 (1/2/3/4/6/8/12/16/24) — the same rule the Rhythm DSL enforces. This is the one arithmetic bridge; both projections use it.

---

## 2. The two projections

### 2a. `OnsetGrid → RhythmPattern` (comping / lead) — `Music/Rhythm/Generation/`

Pure Music (both types are Music). The **ring-to-next-onset** (legato) policy is fixed for v1:

- Walk each bar's onsets left→right (across all four beats). Each onset becomes a `RhythmEvent(Position = onsetTick, Length = nextOnsetTick − onsetTick)`.
- The **last onset of a bar rings to the barline** (no cross-bar tie in v1 — keeps every `PatternBar` self-contained and avoids the unverified-tie renderer path). Cross-bar sustain is a deferred option.
- An **empty bar** (no onsets) → a `PatternBar` with no events → the quantizer emits a whole-bar rest.
- Result: `RhythmPattern(id, name, bars, ts, pickup: null)`. The renderer's `RhythmQuantizer` already splits a rung note at `ChordSpan` boundaries and **re-attacks** — so a generated legato pattern comps correctly across chord changes with no extra work.

Because the last onset only rings to the barline (a beat-aligned value) and there are no mid-bar syncopated ties, the projection **never emits a tie/dotted token the renderer would reject** — it stays inside the verified `:N` + rest vocabulary. (A generation whose onset spacing would need a syncopated value across the barline is simply cut at the barline in v1.)

### 2b. `OnsetGrid → DrumGroove` (drums, single voice) — `Instruments/Drums/`

Lives in `Instruments/Drums`, **not** Music (it targets a Drums type; `Instruments → Music` is the legal edge, `Music → Instruments` is not). `Project(OnsetGrid grid, DrumVoice voice) → DrumGroove`:

- Each onset → a **one-cell `RhythmEvent`** (a `Hit`) on a single `DrumLane(voice, …)`; onsets map 1:1, no sustain policy (a drum hit is instantaneous).
- One `DrumBar` with one lane per generated bar. Default voice = `HiHatClosed`.
- Consumed by the existing `DrumGrooveRenderer` (percussion tex) and `DrumGrooveDiagram` → DrumsR.

**Projection agreement** is a unit-test invariant: for the same `OnsetGrid`, the `RhythmPattern`'s event onset ticks and the `DrumGroove`'s hit onset ticks are identical (they only differ in duration handling).

---

## 3. The generator: one engine, two strategies

```csharp
interface IRhythmGenerationStrategy { OnsetGrid Generate(); }   // pure, deterministic

static class RhythmGenerator {
    OnsetGrid Generate(GenerationParams p);   // dispatches on p.Strategy
}
```

`GenerationParams` (shared): `BarCount` (1–4), `Ts` (4/4 v1), `Seed` (int — used by any random draw; present from day one so `{strategy, params, seed}` fully reproduces a generation). Plus a strategy-specific payload.

### 3a. Pattern strategy — bar-pattern kinds (v2, revised — chat-001)

> **Supersedes the original `block = beat` operators.** The first cut atomized to per-beat blocks, which made a *quarter* block a single trivial option — so `Uniform`/`Cycle`/`AnchorRotate` collapsed to `x x x x`. The pedagogically useful unit is a **whole-bar pattern** drawn from an enumerable **kind** (Rafa's original "block kinds", formalized). Reworked accordingly; the **onset-grid model, both projections, and the Random strategy are unchanged** — this reworks only the Pattern strategy's vocabulary/selection layer.

The generation unit is a **bar pattern** (an `OnsetBar` — *which cells across the bar attack*). Three independent knobs compose it:

1. **Kind** — an *ordered set of bar patterns* (a singleton for a named figure). Two sources:
   - **Generated families** (enumerated by rule): *Density* — quarter/eighth bars by **onset count** (1/2/3/4); e.g. 2-onset quarters = {`xx..`,`x.x.`,`x..x`,`.xx.`,`.x.x`,`..xx`}. *Placement* (eighth) — **on-beat only** / **off-beat (`&`) only** / **on-beat + `&`**.
   - **Named figures** (curated data — the catalog below): a figure is a singleton kind that doubles as a preset; adding one is a data edit, no engine change.
2. **Selection** — how bars are drawn from the kind across `BarCount` bars: **Fixed(index)** (one pattern repeated) · **Cycle** (bar N = pattern N) · **RandomInKind** (seeded) · **FixedPlusRotating** (one fixed + one cycling). Layered multi-bar **behaviours**: **RestBar** (insert silent bars) · **CallResponse** (content/empty) · **Sweep** (walk the selection index or a transform param across bars).
3. **Transform** (optional) — **Displace(cells)**: shift the chosen pattern's onsets N cells later (wrap in-bar) → offbeat/pushed variants (`x.x.` → `.x.x`). Kept from v1 as a post-selection transform (chat-001 #5).

Rests are intrinsic — a bar pattern's non-onset cells are the rests (silent on drums), deliberate, not random.

`PatternParams(RhythmKind Kind, PatternSelection Selection, IReadOnlyList<SequenceBehaviour> Behaviours, DisplaceTransform? Displace, int BarCount, int Seed)` — exact C# shape settled at plan time; subdivision comes from the Kind.

#### Named groove figure catalog (cheap curated data — grow freely)

Eighth grid = 8 cells `1 &1 2 &2 3 &3 4 &4`; quarter grid = 4 cells `1 2 3 4`. Authored best-effort — **verified by ear in the app** during plan-004 (Rafa's call), adjusting any that don't sound right.

| Figure | Grid | Cells | Pattern |
|--------|------|-------|---------|
| Four-on-the-floor | Q | 0,1,2,3 | `xxxx` |
| Downbeats (1 & 3) | Q | 0,2 | `x.x.` |
| Backbeat (2 & 4) | Q | 1,3 | `.x.x` |
| Beat-1 anchor | Q | 0 | `x...` |
| Straight eighths | 8 | all | `xxxxxxxx` |
| Offbeats (all `&`s) | 8 | 1,3,5,7 | `.x.x.x.x` |
| Charleston | 8 | 0,3 | `x..x....` |
| Reverse Charleston | 8 | 4,7 | `....x..x` |
| Tresillo (3-3-2) | 8 | 0,3,6 | `x..x..x.` |
| Cinquillo | 8 | 0,2,3,5,6 | `x.xx.xx.` |
| Dotted-quarter push | 8 | 0,3,6,7 | `x..x..xx` |
| Habanera | 8 | 0,3,4,6 | `x..xx.x.` |
| Son clave 3-2 (2-bar) | 8 | [0,3,6][2,4] | `x..x..x.` / `..x.x...` |
| Son clave 2-3 (2-bar) | 8 | [2,4][0,3,6] | `..x.x...` / `x..x..x.` |
| Rumba clave 3-2 (2-bar) | 8 | [0,3,7][2,4] | `x..x...x` / `..x.x...` |
| Bossa clave (2-bar) | 8 | [0,3,6][2,4,7] | `x..x..x.` / `..x.x..x` |

### 3b. Random strategy

`RandomParams(IReadOnlyList<int> ValuePalette, int ContentBars, int SilenceBars, int Seed)`. The value palette = allowed note values expressed as onset spacings (quarter = onset every beat, eighth = every half-beat, etc.; triplets add a `:3` beat). Fill `ContentBars` by randomly placing onsets per the palette, then append `SilenceBars` empty bars; the whole (`ContentBars + SilenceBars`)-bar grid tiles across the progression. It is the Pattern strategy with the family opened up and the behaviour set to "random" — **same OnsetGrid output, same projections**, no separate subsystem.

### 3c. Presets (named trainers)

A preset is a `GenerationParams` factory — a pinned `(strategy, operator, behaviour, family, referencePulse)` tuple. v1 set: **Find the Beat**, **The Backbeat**, **On the &**, **Leave Space** (Fill It In / Echo follow when Accumulate/CallResponse + reference pulse land). They cost nothing (saved tuples) and double as the dogfood page's "load an example" menu.

---

## 4. The pedagogy force-multipliers (not operators)

1. **Reference pulse** — `referencePulse: Off | Beat1 | Quarters`. In **drums** mode it's a second generated lane (a click voice, e.g. `HiHatClosed` or `Kick` on the pulse) merged into the `DrumGroove` — so it needs *no* new engine, just a second single-onset projection unioned in. For **comping/lead** it rides the transport's existing metronome/count-in. A generation param, default `Off`.
2. **Count + emphasis overlay** — a *display overlay* on the rhythm renderer: print `1 e & a` under the grid and highlight the trained beat / downbeats. The pattern stays timing-only (no DSL/model change) — the labels are computed from the subdivision, exactly like the harmonic overlay on ChordSheetR is computed from harmony. Lives entirely in the JS renderer + a small `beatsPerBar/subdivision` hint already carried by `DrumGrooveDiagram`.

---

## 5. Consumption & the dogfood renderer

The **rhythm-generatorR** the idea calls for already exists as **DrumsR** (pure-SVG hit grid, animated off the engine's time-linear `position` clock). A single-lane onset grid *is* a one-row DrumsR. The dogfood page therefore renders the **drums projection** (single voice) — no harmony needed, pure timing, plus the count/emphasis overlay + an audible click. The **RhythmPattern (legato) projection** is validated by unit tests in the core phase and used for real in Practice (comping/lead need a chord to render). This keeps the dogfood page harmony-free and on the DrumsR path.

**Bridge verb (dogfood page):** `rhythmGenerate` (`{strategy, params, seed}`) → `rhythmGenerated` (`{diagram: DrumGrooveDiagram, tex: percussion-tex, dsl?: string}`) or `rhythmGenerateError`. One `RhythmGenerateHandler` (Features) does one generate → project → both projections that can't drift, mirroring `DrumGroovePreviewHandler`.

---

## 6. Where every piece lives (dependency-direction-safe)

- `Music/Rhythm/Generation/` — `Block`, `OnsetBar`, `OnsetGrid`, `RhythmFamily`, `BarOperator`, `SequenceBehaviour`, `RhythmGenerator`, `PatternParams`/`RandomParams`, presets, and the **`OnsetGrid → RhythmPattern`** projection. Pure, instrument-agnostic — the guarded `Music → Instruments` edge is not crossed (frozen by the existing `MusicLayeringTests`).
- `Instruments/Drums/` — the **`OnsetGrid → DrumGroove`** single-lane projection (targets a Drums type).
- `Features/` — `RhythmGenerateHandler` behind the `rhythmGenerate` verb.
- `wwwroot/` — the Rhythm Generator page (reuses `drums-render-component.js` / DrumsR + the count overlay + the transport click).

No new alphaTex-aware code — the drums projection reuses `DrumGrooveRenderer`, the comping/lead projection reuses `AlphaTexRenderer`.

---

## 7. Phase split — one plan per phase

The thread lands as **1 idea + 1 design + N plans**. Each phase is independently shippable and dogfood-verifiable; later phases are additive.

**Phase 1 — Generator core + onset model + projections (headless Core).**
`Music/Rhythm/Generation/` model + `RhythmGenerator` + Pattern & Random strategies + the six operators + the v1 behaviours + both projections + seed. Unit tests: determinism (same `{params,seed}` → same grid), projection agreement (RhythmPattern onsets == DrumGroove hits), and the legato projection stays inside the verified render vocabulary. No UI.

**Phase 2 — Rhythm Generator dogfood page.**
The `rhythmGenerate` verb + `RhythmGenerateHandler` + a new nav page: strategy/preset/param controls → generate → **DrumsR** display with the **count/emphasis overlay** + play. Satisfies the dogfood rule and de-risks the whole onset→projection model visibly.

**Phase 3 — Reference pulse + full preset set.**
`referencePulse` param (drums click lane / transport metronome) + the remaining named trainers (Find the Beat / Backbeat / On the & / Leave Space / Fill It In / Echo). The audible "ground" that makes syncopation learnable.

**Phase 4 — Practice integration.**
Wire the generator as a **comping / lead / drums (single-voice)** source in the Generate flow: HarmonyControlsR gains a "generated rhythm" option feeding `PatternParams`/`RandomParams`; the comping/lead path uses the RhythmPattern projection, the drums path the DrumGroove projection. On-the-fly, ephemeral.

**Phase 5 — Extended families & strategies + save-into-exercise (additive, deferrable).**
Triplet & 16th families, Random-in-family + Ramp behaviours, and persisting a generation as `{strategy, params, seed}` into a saved exercise (store the definition, regenerate — the app's standard pattern).

Phase boundaries are the design's proposal; 3 could fold into 2 and 5 is a clear "later." Confirm before we plan.

---

## 8. Open (resolve at plan time, not blockers)

- Exact control layout of the generator page (strategy/preset selector + param knobs).
- Whether the dogfood page also offers a notation (percussion ScoreR) view of durations, or DrumsR-only for v1 (leaning DrumsR-only).
- Cross-bar sustain for the legato projection (deferred; v1 rings to the barline).
- **Legato projection & syncopation (Phase-4 finding, surfaced in plan-004).** Now that the Pattern strategy emits arbitrary syncopated bar patterns, the legato `OnsetGrid → RhythmPattern` ring-to-barline can produce a **non-notatable** length (e.g. 120 ticks), which the quantizer rejects (C4). The **drums** path is unaffected (it notates hit + rest, never a ring), so the dogfood page is fine. The **comping/lead** legato path needs a notatable-safe policy (snap the ring to the largest notatable value + rest the remainder, or a verified tie) **before Practice integration (Phase 4)**. Until then, C4 is asserted over legato-safe grids only.
- Reference-pulse voice/wiring specifics in comping/lead mode (transport metronome vs a generated click).
