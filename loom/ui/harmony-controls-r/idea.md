---
type: idea
id: id_01KXN9CD4EEAQ6RV5KSGFBF51X
title: HarmonyControlsR + one Practice page (Score ⇄ Sheet views)
status: done
created: 2026-07-16
version: 1
tags: []
parent_id: null
requires_load: []
---
# HarmonyControlsR + one Practice page (Score ⇄ Sheet views)

## Problem

Practice and Chord Sheets are two projections of the same practice definition, but today they are two separate pages that have already drifted:

- The **harmony picker exists twice** with different looks and different population code: Practice's `Harmony` combo (`<optgroup>` Songs/Progressions, `rebuildHarmonyPicker` in `app.js`) vs Chord Sheets' `Sheet` combo (`♪`/`→` prefixes, `mergeHarmony` in `chord-sheets.js`).
- **Key exists twice with different semantics**: Practice's Key on the ScoreR transport (concrete 0–11, seeded from the song) vs Chord Sheets' Key with a blank "Song key" option.
- The **Chord Sheets bridge request (`chordSheet`) carries no comping / lead / difficulty / voicing window** — its playable tex and adornment grips come from a narrower definition than what Practice generates, so the two pages can show/play *different music* for the same selection.
- Chord Sheets owns a **second `ChordFlowPlayback` engine** (with a hidden staff surface) that, under a converged definition, would load the same tex as Practice's engine — pure duplication, and tempo/soundfont/state set on one page doesn't match the other.
- Definition controls (comping, lead, difficulty, voicing frets, Generate/Save/Mark practiced) exist **only on Practice**, so a chord sheet can't be driven by the same exercise definition.

## Idea

Converge fully (chat-001 "Option A"), taken to its logical conclusion (**one page**):

1. **Extract `HarmonyControlsR`** — a shared definition-strip component (the PlayerControlsR precedent): harmony picker, Key, Feel, Comping + Rhythm vol, Lead + Lead vol, Difficulty, Voicing-fret window (min–max), Generate / Save / Mark practiced. Key + Feel move here from ScoreR's transport and keep their Practice behavior: seeded on harmony switch (song's value, else defaults), manual edits survive until the next switch, **always a concrete value, never blank** (song without a key → C).
2. **One Practice page with a Score ⇄ Sheet view toggle** instead of two pages. One HarmonyControlsR + one PlayerControlsR + one playback engine + one Now/Next + one saved-exercise library; only the view surface and its view-specific control strip swap (Score: staff toggles + debug panel · Sheet: layout / chords / + line / below cell / tone labels / theme / marker mode / exports). The standalone Chord Sheets page and nav button are removed.
3. **One definition on the bridge**: `generate` / `loadExercise` become the only render-producing requests; the reply carries **both projections** (score alphaTex + chord-sheet model) plus the shared schedules. The `chordSheet` request retires. Because the reply always carries the comping grips the definition resolved, the sheet's below-cell adornments become pure display toggles (no re-request).

**Why one page wins:** two pages sharing components would still mean two instances of everything plus a cross-page state-sync problem (or accepted incoherence). One page dissolves that — and one engine unlocks toggling Score ⇄ Sheet **mid-playback** without stopping: the music keeps going, only the way you look at it changes. With two pages that's impossible by design (page switches call `stopAll()`).

## Scope

- **In**: `HarmonyControlsR` component; Practice-page view toggle; merge of `chord-sheets.js` (page shell) into a Sheet *view* module; bridge unification (`generate` reply carries score + sheet projections + schedules; `chordSheet` request/reply retired); ScoreR slims on the Practice page (loses key/feel/volume duties there); library + Now/Next shared across both views.
- **Out (unchanged)**: `ChordSheetR` (the pure-SVG sheet renderer) and `PlayerControlsR` internals; the Content-CRUD preview keeps ScoreR's opt-in key/feel/volume controls (it previews single entities — it is not a definition builder); the PDF/PNG/SVG export mechanics.

## Validation

- Pick a song, Generate, play — toggle Score ⇄ Sheet mid-playback: audio continues, the score cursor and the sheet marker both track the same beat, Now/Next boards stay in sync.
- Change comping / difficulty / voicing window: the sheet's playback **and** its below-cell diagrams reflect the new grips (same definition drives both projections).
- Key/Feel seed on harmony switch (song values; no-key song → C), manual edits survive until the next switch — identical behavior in both views because it's the same control.
- Save from Sheet view creates the same saved exercise; loading a library entry restores the definition in either view.