---
type: req
id: rq_01KTP12K0BP250TBP9EHWAXRQ2
title: Multi-chord-per-bar progressions with harmonic-rhythm layer — Requirements
status: locked
created: 2026-06-09
updated: 2026-06-09
version: 2
design_version: 4
tags: []
parent_id: de_01KTP11T7JCSDK6PN2FEXDR5CW
requires_load: []
---
# Multi-chord-per-bar progressions with harmonic-rhythm layer — Requirements

### ✅ Included

- `IN1` `Progression` becomes `IReadOnlyList<HarmonicBar>`; `HarmonicBar` holds an ordered `IReadOnlyList<ChordSpan>`.
- `IN2` `ChordSpan(RomanDegree Degree, int DurationTicks)` — harmony + tick duration on the existing 48-PPQ grid.
- `IN3` A bar may hold **1–4 chords** placed on the 4 quarter slots (each span duration ∈ {48, 96, 144, 192}, summing to 192) — including 3-chord layouts like `[96,48,48]`.
- `IN4` Per-bar validation: `sum(DurationTicks) == TimeSignature.BarTicks`, every span `DurationTicks > 0`, and (v1) each `DurationTicks` a multiple of `BeatTicks` (48); enforced by a guarded factory so a malformed `Progression` is unconstructable.
- `IN5` `BarPart {Whole, Half, Quarter}` exists only as DSL/UI sugar mapping to ticks (192/96/48), never as a domain field.
- `IN6` Nashville-style `ProgressionParser` (pure, in `Domain/`): space = bar separator, `_` = chord separator, token = `<degree><quality?>[:<slots>]`. Even split for n ∈ {1,2,4}; the `:slots` quarter-count suffix (summing to 4) expresses 3-chord and other uneven quarter-aligned bars. (Syntax M1 — pending Q3 confirmation.)
- `IN7` `RhythmSlot` gains `StartTick`; `RhythmQuantizer` splits at `ChordSpan` boundaries; `AlphaTexRenderer` maps each slot to the chord covering its start tick (`SpanCovering`).
- `IN8` At a chord-span boundary, a sounding note **re-attacks** (`TiedToPrevious = false`); a rest spanning the boundary stays a rest.
- `IN9` Persist progressions in SQLite via a new `ProgressionEntity(Id, Name, Dsl, Origin, CreatedUtc)`; `Dsl` (canonical Nashville string) is the v1 serialization; load = parse `Dsl` then render.
- `IN10` `ProgressionOrigin {BuiltIn, UserDefined}` recorded on every stored progression (stored by name, like `Difficulty`).
- `IN11` Built-in default set seeded on first run from `SeedData` with `Origin = BuiltIn`, including the migrated `12-Bar Blues` (`Dsl = "17 17 17 17 47 47 17 17 57 47 17 57"`) and the new example progressions.
- `IN12` `ExerciseEntity.ProgressionId` references a `ProgressionEntity.Id` row; one EF migration adds the `Progressions` table.

### ❌ Excluded

- `EX1` Syncopation / off-beat and bar-crossing anticipations (pushes) — deferred; the tick model reaches them later.
- `EX2` Tuplet rendering, and therefore **sub-quarter equal divisions** (e.g. three equal 64-tick spans). The quarter-slot 3-chord layouts (`[96,48,48]` etc.) are **included**, not excluded.
- `EX3` Sub-quarter / non-quarter-aligned span durations of any kind (boundaries not on a 48-tick multiple).
- `EX4` Paywall / pro-tier **enforcement** — `Origin` is recorded; gating belongs to a separate Features/licensing layer.
- `EX5` Time signatures other than 4/4 (ctx EX2); minor keys (renderer is major-only today).

### ⛓ Constraints

- `C1` `RomanDegree` stays pure harmony — no timing field — preserving the timing/harmony layer separation (cf. ctx C4).
- `C2` Durations use the existing 48-PPQ `TickGrid` / `TimeSignature.BarTicks`; no new per-pattern grid resolution.
- `C3` v1 render supports only **quarter-aligned** span boundaries (durations multiples of 48 ⇒ span durations ∈ {48,96,144,192} summing to 192); non-aligned boundaries raise a clear parse/render error rather than emitting unparseable alphaTex.
- `C4` A single-chord bar equals one full-bar `ChordSpan(degree, 192)` — backward compatible with today's `12bar_blues`.
- `C5` The DSL string is the v1 persistence form; the schema must allow a future richer form (`spans_json` / normalized tables) without losing the v1 string.
- `C6` `AlphaTexRenderer` remains the only alphaTex-aware code; the slot→span lookup and boundary splitting live in the `Rendering/` seam, the parser/model in `Domain/`.
