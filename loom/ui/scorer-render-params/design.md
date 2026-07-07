---
type: design
id: de_01KWY1YTRRPV5XZ1R23GZ7BNG4
title: ScoreR owns render params (key/tempo/feel) — seeded + live
status: done
created: 2026-07-07
version: 1
idea_version: 1
tags: []
parent_id: id_01KWT44ARF3MY1ZQKT472FHYGV
requires_load: []
---
# ScoreR owns render params (key/tempo/feel) — seeded + live

## Decisions locked (chat-001)

Grounded against `score-render-component.js`, `app.js`, `content-crud.js`, `SongParser.cs`.

- **Open questions (idea) — resolved:** weave stays under `ui` (the `tempo` domain add rides here); the Key control is **shown for progressions/rhythms too**, defaulted to C (transposing just realizes the degrees into that key); saved-exercise **override wins** over content defaults.
- **A — Tempo is *not* a re-render param.** `setTempo` already live-scales alphaTab `playbackSpeed` off `baseTempo` with **zero C# round-trip**. Key/feel change the *alphaTex* (realized pitches / `\tf`) so they must re-emit; tempo does not. Only **key joins feel** on `onNeedsRerender`.
- **B — "Live on select" = the Content preview.** The preview auto-renders on select but never seeded feel (→ always Straight) *or* tempo (`load(tex)` passes no `{tempo}`). Practice's harmony is a **definition** param (Generate-gated by design), so it keeps its Generate gate — no auto-render on song-select.

## The seam — two live mechanisms, not one

**Render/interpretation params** (Key, Tempo, Feel — seeded per content, live) vs **definition params** (harmony/comping/lead/difficulty/voicing — Generate). Within the render params there are two live paths:

| Param | Mechanism | Path |
|-------|-----------|------|
| **Key** | re-emit / transpose (changes the alphaTex) | `setKey` → `onNeedsRerender` → C# re-emit (cheap, no regenerate) |
| **Feel** | re-emit (`\tf` line) — *already wired* | `setTripletFeel` → `onNeedsRerender` |
| **Tempo** | local playback-speed — no C# round-trip | `setTempo` → alphaTab `playbackSpeed` |

## Domain — `tempo <bpm>` Song directive → `Song.DefaultTempo`

The exact peer of `feel`/`DefaultFeel` (a **default seed** for the play-time control, never content-baked playback):

- `Song.DefaultTempo` — nullable `int?` (absent vs explicit), peer of `DefaultFeel`/`InitialKey`.
- `SongParser` — parse a `tempo <bpm>` line mirroring the `feel` keyword: at-most-once, position-independent, unknown/duplicate/malformed → `FormatException`; validate bpm in the control's 40–240 range.
- **Read DTO** — `DefaultTempo` on `ContentSummary`/`ContentItem`, so catalog items carry it (twin of `DefaultFeel`/`InitialKey`).
- Absent → ChordFlow default **80**.
- **Ref sync (same unit of work):** DSL ref gains a `tempo <bpm>` row + example (mirroring the `feel` row); domain-model ref gains `Song.DefaultTempo`.

## ScoreR — own the three, seed + live

Mirror the existing feel triad (`getTripletFeel`/`seedTripletFeel`/`setTripletFeel`):

- **Key** control moves into ScoreR's transport (opt-in like `tripletFeel`; always shown, defaults to C). Add `getKey()` / `seedKey(pc)` (control only, no re-render) / `setKey(pc)` (fires `onNeedsRerender`).
- **Tempo** — `getTempo()` exists; add `seedTempo(bpm)` (sets `baseTempo` + control, **no** `onNeedsRerender`). `load(tex,{tempo})` already re-bases `baseTempo`.
- Key/feel stay **first-class envelope fields** (not `renderOptions`): `onNeedsRerender` already sends `tripletFeel`; add `keyPitchClass: view.getKey()`.

## Practice page (`app.js`)

- Remove the page `$("key")` picker — ScoreR owns it. `selections()` reads `view.getKey()`.
- `seedKeyForHarmony` → `view.seedKey(pc)`; add `seedTempoForHarmony` → `view.seedTempo(song.defaultTempo ?? 80)`. Both fire on harmony **switch** only (manual edit survives until the next switch).
- `onNeedsRerender` handler carries `keyPitchClass: view.getKey()` alongside `tripletFeel`.
- **Load path:** the `loadScore` reply carries the persisted render-param triple (`KeyOverride`/`Tempo`/`TripletFeel`) so ScoreR seeds them on a loaded exercise; `load(tex,{tempo})` already carries tempo. The load path never calls the `seed*ForHarmony` functions → stored override wins (C2).

## Content preview (`content-crud.js`)

- On `entityLoaded` (item select), seed ScoreR Key/Tempo/Feel from the item's DTO defaults (`InitialKey`/`DefaultTempo`/`DefaultFeel`) **before** `requestPreview()` — fixes "always Straight" and the never-seeded tempo/key.
- `renderPreview` passes `{tempo}` to `scoreView.load` (currently omitted → playback stuck at 80).

## Precedence & round-trip (contract unchanged)

Seed from content default on switch/select; a manual change overrides until the next content switch. Nullable absent vs explicit: no `tempo` → 80, no `feel` → Straight; key default C. A saved exercise seeds from its persisted `KeyOverride`/`Tempo`/`TripletFeel` — **override wins**.

## Validation / dogfood

- The same ScoreR on Practice **and** the Content preview shows Key/Tempo/Feel, seeded.
- A `feel triplet8th` / `tempo 120` song: pre-selects + renders swung at 120 on **select** (preview) / on **Generate** (Practice); a progression falls back to **C / 80 / Straight**.
- Live **key** change transposes with no regenerate; live **tempo** change re-speeds with no C# round-trip.

Related: [[song-default-feel]], [[play-ui-key-init]], [[score-render-component]], [[chordflow-dsl-reference]], [[chordflow-domain-model-reference]].