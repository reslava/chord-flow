---
type: req
id: rq_01KWSME2WS1SBYA5YJBF78RQGN
title: Default triplet feel as Song catalog metadata — Requirements
status: locked
created: 2026-07-05
updated: 2026-07-05
version: 2
design_version: 1
tags: []
parent_id: de_01KWSMDGPC8AYX0H26JH8FX792
requires_load: []
---
# Default triplet feel as Song catalog metadata — Requirements

### ✅ Included

- `IN1` A **Song** may declare a default triplet feel via a `feel <token>` directive in its DSL — a space keyword like `key <token>` (e.g. `feel triplet8th`).
- `IN2` The value is a `TripletFeel` (offered idents: `none`, `triplet8th`, `triplet16th`); parsing accepts the offered idents and rejects unknown ones with a `FormatException`.
- `IN3` `SongParser` parses the directive into a nullable `Song.DefaultFeel` on the pure domain record (peer of `Song.InitialKey`).
- `IN4` Selecting a Song **pre-fills** the play-time feel control from its `DefaultFeel` (seed-at-selection); the read/DTO path exposes `DefaultFeel`.
- `IN5` The user can still **override** feel at play time via the existing ScoreR transport control; the song default only seeds the initial value.
- `IN6` Default feel **round-trips** through the Song DSL and is preserved across content packs by construction (it lives in the `Dsl` string).
- `IN7` "No default declared" (directive absent) is distinguishable from an explicit `feel none`.
- `IN8` Ref docs updated in the same unit of work: `chordflow-dsl-reference` (the `feel` Song directive) and `chordflow-domain-model-reference` (`Song.DefaultFeel` + selection seeding).

### ❌ Excluded

- `EX1` **Progressions** carry no feel (and no key) — the Progression grammar stays pure space-split bars/chords, directive-free.
- `EX2` **Rhythm** patterns carry no default feel — intrinsic triplets are authored literally with `:3`.
- `EX3` **Voicings** carry no default feel.
- `EX4` No **per-section / per-progression-within-a-song** feel and no `\tf` cascade/restore (deferred separate axis).
- `EX5` Feel does **not** touch `CatalogHeader`, `CatalogMetadata`, `ICatalogEntity`, or any new entity column/migration — it lives in the Song DSL like `key`.
- `EX6` No change to swing rendering — still a single whole-song `\tf` at bar 1 via alphaTab; the realized pattern stays feel-free.
- `EX7` No new `TripletFeel` enum members.

### ⛓ Constraints

- `C1` Invariant **C4** preserved: `TripletFeel` is never stored in the realized `RhythmPattern`/tick grid; `Song.DefaultFeel` is a suggested play-time default, exactly as `Song.InitialKey` is content without baking feel into the pattern.
- `C2` `CatalogHeader` is unchanged (genre/subgenre/tags only); feel is Song body grammar parsed by `SongParser`, mirroring `key`.
- `C3` The directive is the space-keyword form `feel <token>` (the colon form `feel:` is reserved for stored-part references and is not used).
- `C4` The Song DSL round-trips 1:1 including the `feel` directive (test-enforced).
- `C5` The renderer contract is unchanged: emit `\tf` once (whole-song) only when the effective feel ≠ `None`.
- `C6` Override precedence at play time: **user transport choice > song default > None**.
