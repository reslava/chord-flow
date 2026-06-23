---
type: req
id: rq_01KVSR1C6A823EM3WKPKK3WNRH
title: Comping picker in the Content preview — Requirements
status: locked
created: 2026-06-23
updated: 2026-06-23
version: 1
tags: []
parent_id: id_01KVRK4WVV6QT6CGS397SRWNBN
requires_load: []
---
# Comping picker in the Content preview — Requirements

A comping-rhythm picker for the Content preview of progressions and songs, replacing the hard-wired
`SeedData.Quarters` in `ContentCrudHandler`. Scope confirmed in `content-preview-comping-chat-001` +
`content-preview-comping-design`.

### ✅ Included

- `IN1` A comping `<select>` picker in the **Content preview toolbar** (`content-crud.js`), shown for **progression** and **song** previews.
- `IN2` The picker is **populated from the rhythm catalog** — fetched over the bridge `entityList` (`entity:"rhythm"`), even while the active entity is progression/song.
- `IN3` The `entityPreview` envelope **carries the chosen `compingPatternId`** → `WebMessageRouter` → `ContentCrudHandler.Preview`, used **instead of the hard-wired `SeedData.Quarters`** for the progression and song preview Exercises.
- `IN4` `Preview` **resolves `compingPatternId` → `RhythmPattern`** via the existing `ExerciseRefs.ResolvePattern` seam, then feeds it to the progression and song preview builders.
- `IN5` **Default `beat_1_3`** (the app's default comping). The picker is **transient** — it resets to the default on each page load; the choice is **not persisted**.
- `IN6` A blank/absent `compingPatternId` falls back to `beat_1_3`; a **non-blank id that does not resolve fails loud** as an `entityParseError` (the existing IN3 inline parse-error surface).

### ❌ Excluded

- `EX1` Any comping knob in the shared **`score-render-component.js` (`ScoreR`)** — it stays content-agnostic.
- `EX2` A **lead-pattern picker** — comping-only in this thread (lead is a later, additive change).
- `EX3` A comping picker on the **rhythm** preview (the rhythm is itself under test) or the **voicing** diagram.
- `EX4` **Persisting** the preview comping choice (no settings round-trip, no per-session storage).
- `EX5` Any change to the **generate / save / library** paths — they already carry `CompingPatternId`.

### ⛓ Constraints

- `C1` The picker lives on the **Content page** (`content-crud.js`), never in `ScoreR` — comping is a content-selection knob (it regenerates), not a fixed-enum render directive.
- `C2` Reuse the existing **`ExerciseRefs.ResolvePattern`** resolution seam — no new id→pattern path.
- `C3` Reuse the existing **`InboundEnvelope.CompingPatternId`** field and the one generic `entity*` bridge family — no new envelope field, no per-entity envelope.
- `C4` Dependency direction **Desktop → Core** unchanged; the engine stays UI-agnostic.
- `C5` **No new build step or framework** in `wwwroot` — vanilla JS modules over the existing virtual host.
- `C6` **Only the preview path** changes; `RhythmPreview` and `VoicingPreview` are untouched.