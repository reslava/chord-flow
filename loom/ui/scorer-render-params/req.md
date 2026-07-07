---
type: req
id: rq_01KWY1Z5X8B257D62JTRQ38NTN
title: ScoreR owns render params (key/tempo/feel) — seeded + live — Requirements
status: locked
created: 2026-07-07
updated: 2026-07-07
version: 1
design_version: 1
tags: []
parent_id: de_01KWY1YTRRPV5XZ1R23GZ7BNG4
requires_load: []
---
# ScoreR owns render params (key/tempo/feel) — seeded + live — Requirements

### ✅ Included

- `IN1` — `tempo <bpm>` Song directive → `Song.DefaultTempo` (nullable `int?`), parsed by `SongParser` (at-most-once, position-independent, malformed/duplicate/out-of-range → `FormatException`), surfaced on the read DTO (`ContentSummary`/`ContentItem`) as the peer of `DefaultFeel`/`InitialKey`.
- `IN2` — ScoreR owns the three render params — **Key, Tempo, Feel** — as transport controls, seeded from content; Key and Feel live re-render on change.
- `IN3` — Move the **Key control** from the Practice page into ScoreR; Generate reads key from `view.getKey()`, not the page `$("key")`.
- `IN4` — A **key** change is a live re-emit/transpose via `onNeedsRerender` (no regenerate); the replayed envelope carries `keyPitchClass` from ScoreR. Feel already rides this path.
- `IN5` — A **tempo** change stays a local alphaTab `playbackSpeed` adjustment (no C# re-render); tempo is seeded from `Song.DefaultTempo` (else 80) and re-bases `baseTempo`.
- `IN6` — Seed the render-param triple uniformly in ScoreR: on content **select** (song → `InitialKey`/`DefaultTempo`/`DefaultFeel`; progression/rhythm → C/80/Straight) and on saved-exercise **load** (persisted `KeyOverride`/`Tempo`/`TripletFeel`).
- `IN7` — The **Content preview** seeds Key/Tempo/Feel from the selected item's defaults before its auto-render, and passes `{tempo}` to `load`.
- `IN8` — The Key control is **shown for progression/rhythm** too, defaulted to C (not hidden or disabled).
- `IN9` — Update the DSL reference (`tempo <bpm>` row + example) and the domain-model reference (`Song.DefaultTempo`) in the same unit of work.

### ❌ Excluded

- `EX1` — Harmony / comping / lead / difficulty / voicing remain **definition** params — still Generate-gated, not made live.
- `EX2` — **No auto-render on harmony switch** in Practice; song-select does not auto-generate. Practice keeps its Generate gate.
- `EX3` — Tempo does **not** trigger a C# re-render / `onNeedsRerender`.
- `EX4` — No baking of key/tempo/feel into content playback — they stay play-time render/interpretation params (defaults only).

### ⛓ Constraints

- `C1` — Nullable **absent vs explicit** preserved: no `tempo` → 80, no `feel` → Straight; both distinct from an explicit value.
- `C2` — **Precedence:** content default seeds on switch/select; a manual change overrides until the next content switch; the saved-exercise load path never re-seeds from content (stored override wins).
- `C3` — **Reuse the banked [[song-default-feel]] domain** (`Song.DefaultFeel`, the `feel` directive, `DefaultFeel` on the DTO) and the existing `onNeedsRerender`/`seedTripletFeel` pattern — key/tempo mirror feel, not new machinery.
- `C4` — ChordFlow defaults: **Key C, Tempo 80, Feel Straight**.