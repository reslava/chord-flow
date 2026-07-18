---
type: idea
id: id_01KXSW3V26WPTK3D59E5TJC94D
title: Minor key mode — thread through content preview, list seeding & loadExercise
status: done
created: 2026-07-18
version: 1
tags: []
parent_id: null
requires_load: []
---
# Minor key mode — thread through content preview, list seeding & loadExercise

## What

Finish threading the **key mode** (major/minor) through the three UI/payload surfaces that [[first-class-minor-keys]] deferred as "8b". The kernel, bridge, router, `Preview`/`Build` signatures, and the Practice-page harmony controls already carry `keyIsMinor` end-to-end (delivered in that thread's steps 8a/9). What remains is the surfaces that still drop the mode on the floor, so a minor key silently renders/spells as major (`\ks A` instead of `\ks Aminor`).

This is a plumbing thread, not a new-capability thread: every seam it touches already speaks `keyIsMinor` — the gaps are the JS callers and one C# payload field that don't yet pass it.

## Why — minor mode is honest everywhere except the last mile

`first-class-minor-keys` made `Key(A, minor)` realize + spell correctly and exposed a working major/minor toggle on the Practice page (HarmonyControlsR). But three surfaces were explicitly deferred and still assume major:

1. **Content-editor preview** — the bug Rafa hit. The Content page previews through **ScoreR** (`score-render-component`), whose key control is **pitch-class only** (`getKey()` returns `0..11`, no mode). `content-crud.js` `requestPreview()` sends `entityPreview` with `keyPitchClass` but **no `keyIsMinor`**, so `ContentCrudHandler.Preview` defaults `false` and the alphaTex emits `\ks A`, not `\ks Aminor`. The bridge → router → `Preview(bool keyIsMinor = false)` chain already accepts the flag — only the JS caller (and ScoreR's lack of a mode) are missing.

2. **Content-list seeding of mode** — `harmony-controls-component.js` **already reads** `item.initialKeyIsMinor` when seeding its mode toggle (it's `?minor:major` today), but the **C# content-list payload never emits `initialKeyIsMinor`** / a progression's `Home` tonality. So a minor song can't auto-pick minor mode when selected; the JS seed is wired and waiting for the field.

3. **`loadExercise` re-key mode** — the `loadExercise` reply seeds the key pitch class (`hc.seedKey(msg.key)`) but not the mode, and there's no `seedKeyMode` on that path. A saved minor exercise (its `KeyOverride` already round-trips `IsMinor` after step 3) reloads as major, and re-keying a loaded exercise loses mode.

None of this blocks major-key flows (all default `false`), but until it lands, minor keys are only truly first-class on the Practice generate path — not in the content editor, not on reload.

## The core design decision (settle in design)

**How does the content-editor preview learn its mode?** ScoreR owns the key but has no mode toggle, so:

- **(a) ScoreR grows a key-mode toggle** (the twin of its key `<select>`), `content-crud.js` reads `scoreView.getKeyMode()` and sends it — a live, user-switchable preview mode, symmetric with HarmonyControlsR. More UI + a new ScoreR seam.
- **(b) The preview derives mode from the content's own tonality** — the progression/song's `Home`/`initialKeyIsMinor` flows through `pendingSeeds` into the preview request; no live toggle, mode is a property of the content, not a preview knob.

(b) is smaller and keeps mode a property of the content; (a) matches the Practice page's live toggle and lets you audition a progression in either mode. This choice also decides how much of follow-up #1's payload work (a) vs (b) leans on — settle it in design before the plan.

## Scope (roughly in build order)

1. Content-list payload emits `initialKeyIsMinor` (from a progression's `Home` / a song's key mode).
2. Content-editor preview carries mode → `entityPreview` sends `keyIsMinor` → `\ks Aminor` (per the design decision above).
3. `loadExercise` reply carries the exercise's `KeyOverride` mode; `hc.seedKeyMode` / ScoreR seed it on reload + re-key.

## Non-goals

- No kernel / realization changes — the C frame + converter from [[first-class-minor-keys]] is settled and untouched.
- Not the other diatonic modes (still Major + Minor only, per that thread's EX3).
- No new content authoring UI beyond seeding/threading the existing mode.

## Consumers

- The Content page (progression/song preview) — the primary dogfood surface.
- Exercise reload — a saved minor exercise reopens minor.

## Validation / dogfood

- Select/author a minor progression in Content → preview emits `\ks Aminor` and spells from the relative-major table (the reported bug, fixed).
- Select a minor song → the harmony controls auto-pick minor mode from the list payload.
- Save a minor exercise, reload it → it comes back minor (key + mode), and re-keying keeps mode.

## Related

- Direct continuation of [[first-class-minor-keys]] (its deferred 8b follow-ups).
- Touches the same UI seams as [[harmony-controls-r]] and the content CRUD editor.
