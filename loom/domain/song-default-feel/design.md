---
type: design
id: de_01KWSMDGPC8AYX0H26JH8FX792
title: Default triplet feel as Song catalog metadata
status: done
created: 2026-07-05
updated: 2026-07-05
version: 2
idea_version: 1
tags: []
parent_id: id_01KVRK4H1NJAAYE5K377BJXEQK
requires_load: []
---
# Default triplet feel as Song catalog metadata

## Problem

Let a **Song** declare a **default triplet feel** so picking that song pre-selects the swing — a jazz
blues seeds `Triplet8th`, a straight rock tune seeds `None` — instead of the user re-choosing feel every
time. The apparent tension with invariant **C4** ("`TripletFeel` is never baked into content, it's a
play-time choice") dissolves once feel is modeled the same way **`key`** already is (see D-key below).

## Key realizations (ground the whole design)

- **Feel mirrors `key`.** `key G` is a **Song DSL directive** parsed by `SongParser` into `Song.InitialKey`
  on the pure domain record — it never touches `CatalogHeader`, and `KeyOverride` is the play-time override.
  `feel` is the exact same shape: a Song directive → `Song.DefaultFeel`, with the ScoreR transport as its
  play-time override. C4 is about the realized **RhythmPattern / tick grid**, not the Song; a Song carrying a
  suggested default feel is identical to it carrying `InitialKey` — both are content, neither is baked into
  the pattern.
- **`\tf` semantics** (alphaTex ref §"Triplet feel", line 111): `\tf` only reshapes straight 8th/16th
  *pairs*; an explicit `:3` triplet is already a tuplet, so `\tf` leaves it alone — plain 8ths swing while
  `:3` beats render as authored, same bar, no double-swing. So a single whole-exercise `\tf` suffices, and a
  rhythm mixing literal triplets + straight material composes for free (zero special code).

## Decisions

- **D1 — Feel is a content property, not catalog metadata.** `genre`/`subgenre`/`tags` are *discovery*
  fields (filter/search, denormalized to columns). Default feel answers "how should this *sound* by default"
  — a content property, like `key`. `CatalogHeader` is **untouched** (stays genre/subgenre/tags only).
- **D2 — Owner: Song only.** Not Progression (D8), not Rhythm (D9), not Voicing.
- **D3 — Nullable.** `TripletFeel? DefaultFeel`. **Absent** (no `feel` line → no opinion → falls back to
  `None`) is distinct from an explicit **`feel none`** ("this is a straight tune").
- **D4 — A Song DSL directive, parsed by `SongParser`.** `feel <token>` — a space keyword exactly like
  `key <token>` / `mod <spec>`. **Not** `feel:` (the colon is reserved for stored-part references
  `NAME: id`, which `feel: x` would misparse). Parsed into `Song.DefaultFeel`.
- **D5 — No new persistence.** Feel rides *inside the Song `Dsl` string* exactly like `key` does — parsed on
  load into `Song.DefaultFeel`. **No new entity column, no migration, no `CatalogHeader` change.** No
  denormalization because feel is never a filter field.
- **D6 — Carried on the pure domain record.** `Song.DefaultFeel` sits alongside `Song.InitialKey`. Plumbed
  through `Song.FromSections`; `Song.OfProgression` lifts a bare progression with `DefaultFeel = null` (D8).
- **D7 — Seed at selection; the transport is the override.** Selecting a Song pre-fills the ScoreR feel
  control from its `DefaultFeel`; the transport stays the play-time override. Precedence: **user transport
  choice > song default > None**. The read/DTO path exposes `DefaultFeel` so the UI can seed the control.
- **D8 — Progressions stay pure harmony.** The Progression grammar remains space-split bars/chords only —
  no directive syntax, key- and feel-agnostic. A bare-progression drill inherits feel from the transport
  default. (Confirmed with Rafa: progressions are just chords/bars.)
- **D9 — Rhythms carry no default feel.** Intrinsic triplets are authored literally (`:3`, feel-immune).

## Rejected alternatives

- **Feel via `CatalogHeader` / a `CatalogMetadata` field / a new entity column** — rejected (D1/D4/D5). The
  earlier draft's plan; wrong because feel is content, not discovery metadata, and `key` already shows the
  right seam (the entity's own parser + the domain record + the DSL string).
- **Feel on Progressions** — rejected (D8). Keeps progressions a pure harmonic primitive; consistent with
  them having no `key` today. Revisit only if the Progression grammar ever grows directives.
- **Rhythm owns feel** — rejected (D9). Literal `:3` already gives exact, feel-immune triplets.
- **Per-progression `\tf` cascade + restore** — rejected. That is **per-section feel**, a separate bigger
  axis; one `\tf` per exercise avoids it.

## Touchpoints (blast radius)

- `SongParser` — parse the `feel <token>` directive (peer of the `key` handling).
- `Song` — add `DefaultFeel` (nullable `TripletFeel?`); plumb through `FromSections` / `OfProgression`.
- Song DSL round-trip — if a Song→DSL emitter exists, emit `feel <token>` when set (textual round-trip
  otherwise stores the authored DSL verbatim).
- Read/DTO + bridge + ScoreR — expose `Song.DefaultFeel` and seed the feel control on Song selection.
- `TripletFeel` enum, `CatalogHeader`, entities, migrations — **unchanged** (explicitly, by D5).
- **Ref sync (mandatory, same unit of work):** `chordflow-dsl-reference` (new `feel` **Song** directive,
  space-keyword, alongside `key`), `chordflow-domain-model-reference` (`Song.DefaultFeel` + selection
  seeding; note C4 stays intact and feel mirrors `key`).

## Deferred

- **Per-section feel** (a straight bridge inside a swung tune) — separate axis, out of scope.
- **Progression-level feel/key** — only if/when the Progression grammar gains directive syntax.
