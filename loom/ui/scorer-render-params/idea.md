---
type: idea
id: id_01KWT44ARF3MY1ZQKT472FHYGV
title: ScoreR owns render params (key/tempo/feel) — seeded + live
status: draft
created: 2026-07-05
version: 1
tags: []
parent_id: null
requires_load: []
---
# ScoreR owns render params (key/tempo/feel) — seeded + live

## The idea

Make **ScoreR** (the score-render-component) own the three **render/interpretation params** — **key, tempo, feel** — as live controls on its transport. Each **seeds** from the content and **re-renders immediately** on change:

- **Song** → its DSL defaults (`key`, `feel`, and a new `tempo`); a param the song omits → the ChordFlow default.
- **Progression / Rhythm** → ChordFlow defaults: **Key C, Tempo 80, Feel Straight** (they're pure content, no render-param defaults).

Changing any of the three re-renders the current piece **immediately** (a cheap re-emit / transpose — no regenerate). The other Practice-page controls (harmony, comping, lead, difficulty, voicing) stay **definition params** — they still need the **Generate** button.

## Why — the seam

This draws the honest line the feel work exposed: **render/interpretation params** (how to voice the *same* piece — key/tempo/feel, live) vs **definition params** (*what* the piece is — harmony/comping/lead/difficulty/voicing, Generate). Putting all three render params in ScoreR, seeded per content and live-rendering, is one uniform mechanism across the Practice page **and** the Content preview.

## Scope

1. **Move the Key control** from the Practice page into ScoreR. Generate reads key from ScoreR (not the page `$("key")`); the saved-exercise **load** path seeds ScoreR's key from the stored `KeyOverride`; a live key change is a transpose re-render.
2. **`tempo <bpm>` Song directive** → **`Song.DefaultTempo`** (nullable), the exact peer of `key`/`feel`. Parsed by `SongParser`; seeds the tempo control; absent → 80.
3. **Live re-render** for all three params on change — extend the existing `onNeedsRerender` path (it already carries feel) to also carry key + tempo.
4. **Seed on content select/load**, uniformly in ScoreR, on both the Practice page and the Content preview.

## Bugs this subsumes (found in [[song-default-feel]] testing)

- **Feel not live on song-select** — picking a song seeds the feel control but the score doesn't re-render (only Generate applied it). Fixed by live-render.
- **Content preview always Straight** — the content-crud preview's feel control was never seeded from the song's `DefaultFeel`. Fixed by ScoreR seeding uniformly.

## Builds on (banked — don't redo)

The feel **domain** work is shipped + green in [[song-default-feel]]: `Song.DefaultFeel`, the `feel` directive, and `DefaultFeel` on the read DTO (`ContentSummary`/`ContentItem`). This thread reuses those and adds the UI ownership + `tempo` + the Key move. The page-seed JS from that thread's step 4 is a stepping stone this thread replaces.

## Precedence (unchanged)

Seed from the content default; a manual change **overrides** and survives until the next content switch. Nullable "absent vs explicit": no `tempo` → 80, no `feel` → straight — both distinct from an explicit value.

## Open questions

- **Weave placement** — filed under `ui` (ScoreR is the subject); the `tempo` directive is a small domain add carried here. OK, or split the domain bit into its own thread?
- **Key/rhythm applicability** — a progression/rhythm is key-independent; does its ScoreR hide/disable the Key control, or show C? Feel/tempo apply to all three.
- **Saved-exercise round-trip** — confirm loading a stored exercise seeds ScoreR's key/tempo/feel from its persisted params (`KeyOverride`/`Tempo`/`TripletFeel`) so the override still wins over content defaults.

## Validation

- dogfood: the same ScoreR on both the Practice page and the Content preview shows the three seeded controls and live-renders on change; a `feel triplet8th` / `tempo 120` song pre-selects and renders swung at 120 on select; a progression falls back to C / 80 / Straight.

Related: [[song-default-feel]], [[chordflow-dsl-reference]], [[chordflow-domain-model-reference]], [[score-render-component]], [[play-ui-key-init]].