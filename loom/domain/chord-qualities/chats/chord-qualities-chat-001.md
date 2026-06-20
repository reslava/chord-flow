---
type: chat
id: ch_01KVJX7BV4HHDCXYQY6Q616GHA
title: chord-qualities Chat
status: done
created: 2026-06-20
updated: 2026-06-20
version: 2
tags: []
parent_id: null
requires_load: []
last_ai_block: 7
---
# chord-qualities Chat

## Rafa:

Is idea 100% grounded?
If so create design

## AI:

**Yes — the idea is 100% grounded.** I verified every factual claim against the live code, not just the refs:

| Idea claim | Verified against | Status |
|---|---|---|
| 9 qualities, formula table | `Domain/Quality.cs` (Major…Augmented, 9 members) | ✅ exact match |
| `QualityIntervals` is the single source of truth | `Domain/QualityIntervals.cs` — `Intervals(q)` / `FromIntervals(set)` | ✅ |
| Every formula = the semitone set already in `QualityIntervals` | maj `0,4,7` · min `0,3,7` · maj7 `0,4,7,11` · 7 `0,4,7,10` · m7 `0,3,7,10` · m7b5 `0,3,6,10` · dim `0,3,6` · dim7 `0,3,6,9` · aug `0,4,8` | ✅ all 9 line up |
| `dim7 = 1 b3 b5 bb7`, bb7 = 9 semitones | `Quality.Diminished7 = {0,3,6,9}` | ✅ |
| `ChordTones`' function classifier exists | `Domain/ChordTones.cs` — `Classify` semitone→`ChordToneFunction` band switch | ✅ |
| Spelling follows the intervals thread (`#5`, `bb7`) | `Domain/IntervalSpeller.cs` — `Label(semitone, role)` already emits `#5`/`bb7`; `Parse`/`ParseSet` do token→semitone | ✅ shipped |
| Golden oracle = 34 hand-authored CAGED voicings | `Content/default-pack/voicings/` — maj/min/dom7/maj7/m7 × 5 shapes + m7b5/dim7/aug × 3 (A/D/E) = **34** `.dsl` files | ✅ exact |

So nothing in the idea is hand-wavy — it describes the code as it actually is.

**Before I author the design, there's one genuine fork to settle** (it sets the API shape, so I don't want to pick it silently). The idea says the formula must be *"degree+accidental, not raw semitones, with canonical spelling per degree,"* and that `QualityIntervals` + the `ChordTones` classifier should **derive from** it. The intervals thread already shipped `IntervalSpeller`, which is exactly a degree+accidental authority — so *how* we represent the formula is now a real choice:

**Option A — authored string formula, parsed via `IntervalSpeller` (recommended).**
The authoritative data is Rafa's table verbatim: `[Quality.Diminished7] = "1 b3 b5 bb7"`. `QualityIntervals.Intervals(q)` becomes `IntervalSpeller.ParseSet(formula[q])` — semitones are the *derived projection*, exactly as the idea asks. Maximally DRY: reuses the just-shipped speller, zero new value type, the authored form *is* the spelling.
- *Cost:* a raw string loses the degree number after parsing → the `ChordTones` classifier would still infer function from the semitone band (today's logic), not from the degree.

**Option B — a structured `IntervalFormula` type (ordered `(degree, accidental)`).**
A small new value type. Most literal reading of "degree+accidental." Lets the `ChordTones` classifier derive function straight from the **degree number** (3rd→Third, 5th→Fifth, 7th→Seventh), retiring the `6/7/8→Fifth, 9/10/11→Seventh` band heuristic and its "9 = bb7 of dim7" special-case comment.
- *Cost:* partially duplicates what `IntervalSpeller.Parse` already decodes; more surface for what's still a fixed 9-row table.

My lean is **A** — it satisfies the idea's "formula is authoritative, semitones derived" precisely while leaning on `IntervalSpeller` instead of adding a parallel type ([[design-philosophy-durable-over-minimal]] cuts toward reuse here). The only thing A gives up is degree-driven function classification; if you want *that* cleanup too, B earns its keep.

A hybrid also exists (A's authored strings + a tiny parse that keeps the degree number only for the classifier) — but that's a detail I'd fold into whichever direction you pick.

Which way do you want the formula represented? Once you confirm, I'll write the design.

## Rafa:

A

Curiosity, what if we instead of having table with only quality, formula
we store table with quality, formula, semitones:

## Interval formulas table

| Quality | Formula       | Semitones |
|---|---|---|
| maj     | 1  3  5       | 0,4,7
| min     | 1 b3  5       | 0,3,7
| maj7    | 1  3  5   7   | 0,4,7,11
| 7       | 1  3  5  b7   | 0,4,7,10
| m7      | 1 b3  5  b7   | 0,3,7,10
| m7b5    | 1 b3 b5  b7   | ...
| dim     | 1 b3 b5       |    
| dim7    | 1 b3 b5 bb7   |    
| aug     | 1  3 #5       | 

Could be useful?



## AI:

Good instinct, but I'd say **no — not in the stored table.** Storing the semitones next to the formula quietly undoes the one thing this thread exists to do.

The whole point of A is: **formula = the single source of truth, semitones = a derived projection** (`IntervalSpeller.ParseSet(formula)`). The moment the table also *stores* `0,3,6,9`, you have **two sources of truth for the same fact**, and nothing stops them drifting — someone fixes a formula typo but not the semitone column (or vice versa) and now `dim7` says one thing in one column and another in the next, with no error. The derivation is precisely what makes drift *impossible*: there's one authored value, the other is computed every time. That's the codebase's standing rule — *"everything derived, never hand-authored per case"* (ctx §C4, and the same reason `Feel`/spelling are never stored).

So storing it = caching a derived value. The only reason to cache is performance, and here it's a 9-row static table parsed once at startup — there is nothing to optimize.

**But your instinct points at something genuinely useful — just move it one layer over: the semitones column becomes the unit-test oracle, not stored data.**

- Production table: `[Quality.Diminished7] = "1 b3 b5 bb7"` — formula only.
- Test: a hand-written expected-semitones table (exactly your third column), asserting `QualityIntervals.Intervals(q) == {0,3,6,9}` for all 9 qualities.

That captures 100% of the value you're after — an independent, human-readable cross-check that the formula really decodes to the right notes — **without** a second runtime source of truth. If a formula or `IntervalSpeller` ever changes, the test's hand-authored semitones catch it. It's the same move the intervals thread used: the formula's golden oracle is a unit test, not duplicated data. (This also lets us drop today's hard-coded `QualityIntervals` arrays entirely — they become the *test's* expectation, which is where a hand-authored constant belongs.)

And to be clear: the full 3-column table (Quality · Formula · Semitones) absolutely belongs **in the design doc and the ref** as documentation — it's a great at-a-glance map. The rule is only about what's *stored as runtime data*: formula authoritative, semitones derived (and independently pinned by the test).

So: **A, formula-only stored, semitones as the test oracle.** If that lands, I'll write the design.

## Rafa:

Design marked done
Generate req
Create plan

## Rafa:

Plan started
Do whole plan

## AI:

**No error — the work completed cleanly.** Flagging the confusion first: there was no API/server error. Every step ran and passed; I'd just been reporting in the terminal instead of here in the chat, so this doc went quiet while the work happened. Catching it up now.

## Plan complete — `chord-qualities-plan-001` (status: done)

| # | Step | Result |
|---|------|--------|
| 1 | `Domain/QualityFormulas` — 9 formulas as degree+accidental strings | ✅ created |
| 2 | `QualityIntervals` derives via `IntervalSpeller.ParseSet`; arrays deleted | ✅ public surface unchanged |
| 3 | `QualityFormulasTests` golden oracle | ✅ **575 passed / 0 failed** |
| 4 | Ref-sync `chordflow-domain-model-reference` §1 | ✅ patched (same unit of work) |

**What changed:** the interval formula is now the single authoritative form of every chord quality (`QualityFormulas.Formula(q)` → `"1 b3 b5 bb7"`), and the semitone set is a derived projection parsed once via the existing `IntervalSpeller`. The hand-authored `new[]{0,3,6,9}` arrays are gone — one authored source per quality, no drift possible. Your "store semitones too" instinct landed where it belongs: the expected-semitone table lives in `QualityFormulasTests` as the cross-check oracle, not as duplicated runtime data.

**Parity held:** the existing `QualityIntervals`/`ChordTones`/`Diatonic` tests pass unchanged — behavior is byte-for-byte identical, just sourced differently.

Thread state: idea ✅ done · design ✅ done · req 🔒 locked · plan ✅ done. Committing the work now.