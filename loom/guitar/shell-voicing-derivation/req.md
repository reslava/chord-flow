---
type: req
id: rq_01KW482RXX4XAKW061344SK9PN
title: Derive shell voicings from CAGED chords — Requirements
status: locked
created: 2026-06-27
updated: 2026-06-27
version: 3
tags: []
parent_id: id_01KVYQ3DY08RT6KGK50X0PPEGR
requires_load: []
---
# Derive shell voicings from CAGED chords — Requirements

Authoritative scope for `shell-voicing-derivation` — amended after chat-001's design pivot (3-family / 2-form model) and the dogfood refinements (doubled-shell curation + the open-root anchor fix). Shells are **derived** as compact 2-form voicings verified by an authored golden-oracle table; the "strip the 5th" idea survives as a small curated `doubled-shell` family. Builds on the shipped `[[engine-derived-as-app-source]]` + `[[caged-sixth-voicings]]`. (Handles are append-only: superseded ones are marked `~dropped`, not removed.)

### ✅ Included

- `IN1` New `Instruments/Guitar/Caged/ShellReduction.cs` — static, pure, unit-tested: `MuteFifth(ChordShape) → ChordShape`, the **doubled-shell** reduction. Mutes the strings whose chord-tone function is the **fifth** (via `ChordTones`/`QualityFormulas`), keeps root/3rd/7th/6th including doublings.
- `IN2` New `VoicingFamily` enum (`Caged, DoubledShell, Shell`) + token mapping (`caged`/`dshell`/`shell`).
- `IN3` `shell` & `doubled-shell` apply **only** to qualities with a 7th/6th; triads have only `caged`.
- `IN4` Reduction **mutes** dropped strings — never repacks (doubled-shell, idea Q1).
- `IN5` `AutomaticVoicingId` → 4-segment `auto:{family}:{token}:{shape}`; for `shell` the shape segment ∈ {`C`,`E`} (the 5th-root / 6th-root forms); the 3-segment form is removed; `TryParse` requires 4 segments (breaking).
- `IN6` `CagedVoicingCatalog.Combos` carries `(VoicingFamily, Quality, CagedShape)` with `ShapesFor(family, quality)` — **64 combos**: `caged` over all 46; `shell` over the 7 shell-eligible (7th/6th) qualities × {`C`,`E`} (14); **`doubled-shell` over a curated common doubled-root set — the C form only, for `Dominant7`/`Diminished7`/`Major6`/`Minor6` (4)** (dogfood call, chat-001: only the C-shape doubled-root voicings like C7/C6 are commonly played; the other shapes/qualities were dropped). The engine derives a clean C-form grip for each, including the dim7 C-form (verified by coverage, though caged offers dim7 only on A/E/D).
- `IN7` `VoicingSource` (RenderOptions) gains `Family` (default `caged` ⇒ unchanged behaviour). `CompingResolver` dispatches per family (`caged`→`Derive`; `doubled-shell`→`Derive`+`MuteFifth`; `shell`→`ShellDerivation`) and **falls back to `caged`** for a chord whose quality has no shell, before the source fallback chain.
- `IN8` `EngineVoicingSource` lists the family rows with family-qualified display names (e.g. `Dominant 7 (shell) — E shape`). *(`common`/`extended` `~dropped` — no consumer yet.)*
- `IN9` Retire the legacy strategy path: remove `BeginnerShellStrategy` + `IVoicingStrategy` from production and delete `VoicingBook` if dead, rewiring `GuitarInstrument`/`VoicingStore`.
- `IN10` `~dropped` — the `BeginnerShellStrategy` regression oracle is obsolete; replaced by the authored shell-table oracle (IN14).
- `IN11` Ref-sync in the same unit of work: add `ShellDerivation` + `ShellReduction` + `VoicingFamily` and the family dimension to `chordflow-domain-model-reference.md`.
- `IN12` Dogfood: the derived shell families render on the fretboard UI page — the **CAGED Chords page gains a Family selector** (caged/dshell/shell), narrowing Shape + Quality to each family's offered set.
- `IN13` New `Instruments/Guitar/Caged/ShellDerivation.cs` — static, pure, unit-tested: `Derive(Quality, CagedShape form /* C|E */, PitchClass root, int minFret, int maxFret) → ChordShape`, the 2-form compact-shell deriver. Root on s5 (`C`) / s6 (`E`); guide tones on s4+s3 — `C`: (s4=3rd, s3=7th|6th), `E`: (s4=7th|6th, s3=3rd); each guide tone at the occurrence on its string **nearest the root fret**. The root is anchored at the **lowest *compact* placement** in the region — an open-string root whose guide tones would land ~12 frets away is pushed up an octave (e.g. A maj7 C-form → `x 12 11 13 x x`, not `x 0 11 1 x x`). Reuses `IntervalLattice`/`Fretboard`/`QualityFormulas` — no authored frets.
- `IN14` **(new)** The shell **golden oracle**: the 12 authored grips (`C`,`E` × `dom7/min7/maj7/dim7/6/m6`, root C) as a **test-only** fixture `ShellDerivation` must reproduce. `m7♭5` is derived too (its shell = the min7 grip), validated structurally. Plus: doubled-shell structural validation, catalog coverage (every offered `(family,quality,shape)` resolves, no throw), the open-root compaction regression, and the `Family=caged` no-regression check.

### ❌ Excluded

- `EX1` `~dropped` — shells are now a **derivation** (`ShellDerivation`), not solely a reduction.
- `EX2` Shipping **authored shell grips as runtime content** — excluded; the 12-grip table is a **test-only** oracle fixture (IN14).
- `EX3` **Difficulty-band selection** + the Beginner⇒shell mapping — `[[voicing-difficulty-bands]]`.
- `EX4` **Ranking modes** beyond the existing Closest default — `[[voicing-ranking-strategies]]`.
- `EX5` **Explicit per-chord voicing references** — `[[explicit-voicing-reference]]`.
- `EX6` **Authored `tags`** (genre/mood) + the `common`/`extended` classification — deferred.
- `EX7` **Triad "power-shell"** (root+3 only) and barre modelling for shells.

### ⛓ Constraints

- `C1` **Source-of-truth:** `doubled-shell` reduces the engine `ChordShape`; `shell` derives from the quality formula + octave anchors; the authored 12-grip oracle is **test-only**, never a runtime grip table.
- `C2` **Fail-loud preserved:** resolution throws only when no source yields any grip; a family with no candidate for a quality is a fallback, not a failure.
- `C3` **Pure & deterministic:** `ShellReduction`/`ShellDerivation` have no I/O/UI, fully unit-tested; reuse `QualityFormulas`/`ChordTones`/`IntervalLattice` (no magic semitone).
- `C4` **No regression:** `Family` defaults to `caged`; existing renders unchanged.
- `C5` **Dependency direction:** lives in `Instruments`/`Features`, may depend on `Music`, never the reverse — NetArchTest guards stay green.
- `C6` **Sequencing:** depends on `[[engine-derived-as-app-source]]` + `[[caged-sixth-voicings]]`; depended upon by `[[voicing-difficulty-bands]]`.
- `C7` **Computed, never stored:** shell families never flow through SQLite.
