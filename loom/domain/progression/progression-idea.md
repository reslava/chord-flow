---
type: idea
id: id_01KTP0SG7G3YR8CRQKZA3YPK6G
title: Multi-chord-per-bar progressions with harmonic-rhythm layer
status: done
created: "2026-06-09T00:00:00.000Z"
updated: 2026-06-12
version: 2
tags: []
parent_id: null
requires_load: []
---
# Multi-chord-per-bar progressions with harmonic-rhythm layer

## Problem

Today the progression model is `1 RomanDegree = 1 chord = 1 bar`. The renderer loops one bar per degree (`AlphaTexRenderer.cs:60`) and applies the same one-bar strum `RhythmPattern` to every bar. This cannot express bars that contain **more than one chord** (e.g. a jazz-blues turnaround `ii-7 V7 | I7 VI7 | ...`), which is table-stakes for the progression trainer.

## Concept

Introduce a **harmonic-rhythm layer** — the rate at which chords change — kept strictly separate from the existing **strum/articulation rhythm** (`RhythmPattern`, "holds only timing"). A bar may hold 1–4 chords. Each chord occupies a span of the bar, measured on the existing 48-PPQ tick grid (`BarTicks = 192`, divisible by 2, 3 and 4 — so 2-, 3- and 4-chord bars are all on-grid).

### Model shape (decision: option B — locked in chat)

```csharp
public sealed record Progression(string Id, string Name, IReadOnlyList<HarmonicBar> Bars);
public sealed record HarmonicBar(IReadOnlyList<ChordSpan> Spans);   // spans sum to BarTicks
public readonly record struct ChordSpan(RomanDegree Degree, int DurationTicks);
```

- `RomanDegree` **stays pure harmony** (key-independent; no timing). This preserves the domain's timing/harmony separation (the same principle as ctx C4: Feel is never stored on the pattern).
- `BarPart {Whole, Half, Quarter}` survives only as **DSL/UI sugar** mapping to ticks (`192/96/48`), never as the storage type.
- Validation is **local**: each `HarmonicBar` requires `sum(span.DurationTicks) == ts.BarTicks`. Bar count = `Bars.Count`.

**Why B over "BarPart on RomanDegree":** the enum-on-degree approach mixes timing into the harmonic atom, makes bar boundaries an implicit/cascading flat list, and cannot represent 3-equal-chord bars or any syncopation. Tick-durations handle all of those now and reach syncopation later with no schema change.

## Input DSL (Nashville-style)

A compact string is the simplest UI and doubles as the v1 serialization:

- ` ` = bar separator, `_` = chord separator within a bar (even split by count → `BarTicks / n` each).
- token = `<degree><quality?>`, quality suffixes mapping onto the existing 8-value `Quality` enum (`-`/`m`, `7`, `-7`, `maj7`/`^7`, `°`/`dim`, `ø`/`m7b5`, `+`/`aug`).
- Example — `jazz blues turnaround`: `2-7 57 17_67 2-7_57` → ii-7 | V7 | I7·VI7 | ii-7·V7.

Parser lives in a pure static `ProgressionParser` (peer of `NoteSpeller`).

## Renderer impact

Chords are no longer 1:1 with bars. `RenderBar` changes from "one chord for the whole bar" to **"for each `RhythmSlot`, look up which `ChordSpan` covers its tick."** That tick→span lookup is the same primitive a future syncopation feature needs — a signal we're modeling at the right altitude.

## Persistence & tiers

- Progressions are **stored in the database** as definitions (re-rendered to alphaTex on load, matching the existing pattern). v1 serialization = the canonical DSL string `{Name, Dsl}`.
- The app ships a **built-in default set** of progressions; **user-added progressions are a pro / pay-tier feature.** This implies an **origin marker** on a stored progression (built-in vs user-defined). The *paywall enforcement* itself is a Features/licensing concern, not domain — see open scope question below.

## Out of scope (this thread)

- Syncopation / anticipations (off-beat and bar-crossing pushes) — the tick model reaches them, but they are a later feature.
- Uneven manual spans via DSL (e.g. Half + Quarter + Quarter) — v1 DSL is even-split only; a duration-suffix syntax is reserved.
- Time signatures other than 4/4 (ctx EX2).

## Open scope question

Does this thread's design + requirements cover (a) the persistence schema and origin/tier marker, or (b) only the domain model + DSL + renderer, leaving DB schema and paywall to a separate persistence/licensing thread? Recommendation: keep the **origin marker** in scope (it shapes the model), but defer paywall *enforcement* to Features.
