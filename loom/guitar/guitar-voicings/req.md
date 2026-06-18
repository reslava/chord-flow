---
type: req
id: rq_01KTZWXSJ6PS9CFX8AJHENNXDB
title: Guitar voicings — the fourth content pillar (authored, stored, movable) — Requirements
status: locked
created: "2026-06-13T00:00:00.000Z"
updated: 2026-06-13
version: 3
tags: []
parent_id: id_01KTXEQ5F1R316J3RCF5CMBDTW
requires_load: []
---
# Voicings — the fourth content pillar (authored, stored, movable) — Requirements

Requirements for the authored-voicing content pillar (the fourth content pillar:
DSL → entity → stored-first lookup → pack-distributable). Anchored on
`voicings-idea.md` / `voicings-design.md` and the decisions resolved in
`voicings-chat-001`.

### ✅ Included

- `IN1` **Voicing DSL + parser** — `voicing <Chord>  shape:<C|A|G|E|D…>  root:<6..1>  frets: <s6 … s1>`; `x` = muted, `0` = open; `shape`/`root` are metadata. Parses onto the existing `Voicing(Positions, BarreFret?, FirstFret?, MutedStrings?)` + `FretPosition`.
- `IN2` **Canonical-C normalization on save** — any declared anchor is normalized to the *lowest non-negative C placement*; one stored record per `(quality, shape)` (no `Dmaj`/`Emaj`/… rows).
- `IN3` **`Realize(entry, targetRoot)`** — transpose a canonical-C voicing to any of the 12 roots; octave-fold into the 0–15 window; return `null` when no placement fits; derive `BarreFret`/`FirstFret` from the lowest fretted fret. Output reuses the existing `Voicing` value type.
- `IN4` **`VoicingBook` — stored-first, exact-quality, instance built with the authored library.** `VoicingBook` is constructed with the stored `VoicingShape` library and exposes two methods: `Candidates(chord, difficulty)` → the exact-quality stored voicings (quality == `chord.Quality`) realized to `chord.Root`, kept playable (0–15), and **ranked** by neck position then CAGED familiarity (may be empty; difficulty-band narrowing is reserved); and `Lookup(chord, difficulty)` → the single voicing to play — the top candidate, else the `BeginnerShellStrategy`-generated shape, throwing when neither covers the chord. Stored authored voicings **shadow** generated ones. (Amended from the original single-`Voicing` `Lookup` per `voicings-chat-001`: a clean break is acceptable; `Candidates` carries the ranked list, `Lookup` the one to play.)
- `IN5` **CAGED familiarity rank** as pack-overridable metadata on the shape (seed order **E A G C D**), used as the ranked-list tiebreak.
- `IN6` **`VoicingEntity(Id, Name, Dsl, Origin, Genre?, CreatedUtc)`** — DSL-only, mirrors `ProgressionEntity`; new `Voicings` EF table + migration.
- `IN7` **~dropped~ — moved to the `ui/content-crud` thread.** Originally "CRUD UI uniform with `Progression`/`Song`/`RhythmPattern`." Discovered at implementation time (`voicings-chat-001`): no shared content-CRUD UI exists yet (`wwwroot/` is still the MVP exercise generator), so there is nothing to be "uniform with." The voicing editor + chord-diagram preview belong to the shared definition-UI effort and now live in `ui/content-crud` (Option 1). This slice delivers the complete engine + DSL + persistence + render wiring; the authoring screen is the new thread's scope.
- `IN8` **Ref-sync** — update `chordflow-domain-model-reference.md` (and DSL ref if the public DSL surface is affected) in the same unit of work as the domain code.

### ❌ Excluded

- `EX1` **No `fixed` flag / non-movable form** — every voicing is movable; "open" is just where a shape lands (open ↔ barre under transpose).
- `EX2` **Open drone/pedal voicings** (open strings that hold pitch under transpose) — deferred; overlaps alternate tunings.
- `EX3` **Alternate tunings** — `Fretboard` is fixed-tuning in v1.
- `EX4` **Pitched lead/target-note voicings** (`domain/intervals` + LeadTargets).
- `EX5` **`QualitySimplifier`** (`maj13→maj7→maj` reduction) — reserved seam, a separate opt-in transform upstream of `Lookup`; not this slice. `Lookup` stays exact-quality.
- `EX6` **Difficulty-band selection heuristics** beyond "lowest fit + next region" / consumer take-N over the ranked list.
- `EX7` **First-class `Interval` type** — deferred to the `domain/intervals` thread; `Realize` reuses `PitchClass` + `Fretboard`.
- `EX8` **Movable-shape abstraction refinement** (re-expressing shapes as interval stacks once `domain/intervals` lands).

### ⛓ Constraints

- `C1` Domain code (parser, normalizer, `Realize`, `VoicingBook`) lives in **`ChordFlow.Core/Domain/`** — pure, reuses `PitchClass` + `Fretboard`, no I/O, no first-class `Interval` type.
- `C2` `VoicingEntity` + migration → **`ChordFlow.Core/Persistence/`**; the CRUD screen (when built in `ui/content-crud`) → **`ChordFlow.Desktop`** (`wwwroot`). Dependency direction **Desktop → Core** unchanged.
- `C3` **DSL-only persistence** — frets regenerated from DSL on load (mirrors `ProgressionEntity`); the stored DSL is the canonical-C form.
- `C4` `Realize` enforces the **0–15 fret** window guard.
- `C5` Adopt **catalog metadata + `Origin` provenance** from the `packages` thread.
