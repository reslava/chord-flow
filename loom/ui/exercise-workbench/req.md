---
type: req
id: rq_01KV6GKNP1GKE081AZ250YXPD5
title: Exercise workbench UI — generator + practice/player — Requirements
status: locked
created: "2026-06-15T00:00:00.000Z"
updated: 2026-06-15
version: 2
tags: []
parent_id: id_01KV05B8D5VP4T5R37BSJ1NQRR
requires_load: []
---
# Exercise workbench UI — generator + practice/player — Requirements

The consumption-side UI for the now-canonical `Exercise` (capstone landed in
`exercises-definition-ui`): pick the definition + params, Generate, and play the
two-track score with a synced cursor. Sits on the finished `content-crud` bridge +
`score-render-component`. Scope proposed in `exercise-workbench-chat-001`. Amended
(v2): the blanket "no Core changes" `EX6` was reconciled against `IN3` — the
generate-path wiring is now an explicit `IN8`, and `EX6` is narrowed to `EX7`.

### ✅ Included

- `IN1` **Exercise-params surface** — live pickers for the canonical `Exercise` params: **Key** (transpose override, defaults to `Song.InitialKey`), **Tempo**, **Difficulty**, **Feel** — editable before **Generate**, persisted as saved defaults (`exercises-definition-ui` C1: definition = references, params = values).
- `IN2` **Definition selection** — pick a stored **Song** for harmony and a required **Comping** `RhythmPattern` + optional **Lead** `RhythmPattern`, sourced from `content-crud`'s `entityList`/`entityGet` bridge. A bare **Progression** is selectable too (trivially lifted via `Song.OfProgression`), so there is no Progression-vs-Song branch in the UI.
- `IN3` **Generate** — assemble the chosen references + params into the canonical `Exercise` and produce alphaTex through the one realization path (`SongExpander → RealizedSong → render`). (Requires the wiring in `IN8`.)
- `IN4` **Two-track Play / Practice view** — rhythm-guitar (Comping) + lead-guitar (Lead, dead/muted notes in v1) rendered via `score-render-component`, with the synced alphaTab cursor and **Play / Stop / Tempo**. Stays single-track when `Lead` is null.
- `IN5` **Player settings (user prefs)** — count-in on/off, metronome on/off, rhythm-guitar volume, lead-guitar volume (alphaTab *player* config, the home `exercises-definition-ui` EX3 assigned here — "how you listen", not part of the Exercise).
- `IN6` **Save / library** — save an Exercise as the new reference-shaped `ExerciseEntity` (`SongId` / `CompingPatternId` / `LeadPatternId?` + param columns), **Mark-practiced**, and the saved-exercise list, over the existing bridge.
- `IN7` **Practice side of the Practice ⇄ Content toggle** — this thread is the *Practice* view of the single-page toggle `content-crud` introduced; reuse the shared `bridge.js` module.
- `IN8` **Generate-path wiring (the one allowed Core touch)** — widen the `generate` inbound envelope from `(key, rhythmId, tempo)` to carry **content references** (`songId`/`progressionId` + `compingPatternId` + optional `leadPatternId`) + the params, and have `GenerateExerciseHandler.Build` **resolve them from the stores** (`ProgressionStore`/`SongStore`/`RhythmPatternStore`) into the canonical `Exercise`, replacing the hard-wired `SeedData.TwelveBarBlues` + seed-rhythm lookup. Plus the matching `WebMessageRouter` verb and any inbound-envelope field widening. This is plumbing chosen ids into the **existing** pipeline — bounded by `EX7` (no new domain/render/persistence capability).

### ❌ Excluded

- `EX1` **Voicing selection** — surfacing `VoicingBook.Candidates` (the ranked CAGED shape/region list) as a picker. Deferred until the derivation engine (`domain/caged-system`) lands; v1 uses the engine's default voicing. *(Re-open here once CAGED ships.)*
- `EX2` **Chord diagrams above the staff** driven by a *selected* shape — depends on `EX1` / the CAGED derivation output. (The static chord-name-on-change labels from the render component are fine; a shape-driven diagram picker is not.)
- `EX3` **Pitched lead target notes** (scale / chord-tones / guide-tones / arpeggios via `LeadTargets`) — already `exercises-definition-ui` EX1; v1 lead is dead notes only.
- `EX4` **Richer practice loop** — looping, count-in *ramps*, tempo *ramps*, A/B sections — overlaps the `Progress` / `PracticeSession` features; v1 is single play-through + basic Tempo.
- `EX5` **Content authoring/CRUD** — creating/editing Songs, Progressions, RhythmPatterns, Voicings is `content-crud`'s job (done); this thread only *selects* them.
- `EX6` ~~dropped~~ — **superseded by `IN8` + `EX7`.** The blanket "no Core changes; the canonical Exercise / Song.OfProgression / two-track render / ExerciseEntity refactor all shipped" over-broadly forbade *all* Core changes, which blocked the generate-path wiring `IN3` requires. Narrowed to "no new Core *domain/render/persistence capability*" — see `EX7`.
- `EX7` **No new Core domain/render/persistence capability** — the canonical `Exercise`, `Song.OfProgression`, the two-track dead-note render, and the `ExerciseEntity` refactor are shipped (`exercises-definition-ui`) and consumed **as-is**. The only permitted Core edits are the `IN8` generate-path plumbing (envelope + handler + router verb); no new music-theory, rendering, or schema work.

### ⛓ Constraints

- `C1` **No new build step or framework** in `wwwroot` — vanilla JS modules over the existing virtual host (mirrors `content-crud` C6).
- `C2` **Reuse, don't re-implement** — content selection rides `content-crud`'s `entity*` bridge envelopes + `bridge.js`; rendering rides `score-render-component`; no parallel copies.
- `C3` **Definition vs params split** honored in the UI — definition = references (Song + Comping + optional Lead); params = values with saved defaults, editable live before Generate.
- `C4` **One realization path** — a bare Progression is lifted (`Song.OfProgression`); the UI never branches Progression-vs-Song downstream.
- `C5` Dependency direction **Desktop → Core** unchanged; the engine stays UI-agnostic (compile-enforced). All three declared deps (`content-crud`, `score-render-component`, `exercises-definition-ui`) are **done** — this thread is unblocked.
- `C6` **Saved exercises are disposable** — no migration or preservation of existing `ExerciseEntity` rows; the DB may be wiped and re-seeded freely (consistent with the capstone's drop-and-add migration; no users). The library-list rewire targets the new `ExerciseSummary` shape only.
