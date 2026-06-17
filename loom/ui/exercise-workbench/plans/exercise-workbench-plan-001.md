---
type: plan
id: pl_01KV6HBBY93VRRX8QP7YPGQ3SW
title: Exercise workbench — consumption UI over the canonical Exercise
status: done
created: 2026-06-15
updated: 2026-06-15
version: 1
design_version: 1
req_version: 2
tags: []
parent_id: null
requires_load: []
target_version: 0.1.0
actual_release: 0.5.0
steps:
  - id: core-reference-resolving-generateexercisehandler
    order: 1
    status: done
    description: Generate slice resolves content references from the stores (delete the hard-wired blues)
    files_touched: [src/ChordFlow.Core/Features/GenerateExercise/GenerateExercise.cs, src/ChordFlow.Core/Persistence/SongStore.cs, src/ChordFlow.Core/Persistence/RhythmPatternStore.cs, tests/ChordFlow.Core.Tests/Features/GenerateExerciseTests.cs]
    blocked_by: []
    satisfies: [IN8, C4]
  - id: core-generate-envelope-carries-content-references
    order: 2
    status: done
    description: Widen the generate bridge envelope + router verb + Program.cs wiring
    files_touched: [src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Desktop/Program.cs, tests/ChordFlow.Core.Tests/Bridge/WebMessageRouterContentTests.cs]
    blocked_by: [1]
    satisfies: [IN8, C5]
  - id: fe-definition-selection
    order: 3
    status: done
    description: Definition pickers populated from the content stores (Song/Progression + Comping + optional Lead)
    files_touched: [src/ChordFlow.Desktop/wwwroot/app.js, src/ChordFlow.Desktop/wwwroot/index.html]
    blocked_by: []
    satisfies: [IN2, C2, C4]
  - id: fe-exercise-params
    order: 4
    status: done
    description: Params surface — Difficulty + Feel selects alongside Key (saved defaults)
    files_touched: [src/ChordFlow.Desktop/wwwroot/app.js, src/ChordFlow.Desktop/wwwroot/index.html]
    blocked_by: []
    satisfies: [IN1, C3]
  - id: fe-generate
    order: 5
    status: done
    description: Generate wiring — emit the new reference+params envelope
    files_touched: [src/ChordFlow.Desktop/wwwroot/app.js]
    blocked_by: [2, 3, 4]
    satisfies: [IN3]
  - id: fe-save-library
    order: 6
    status: done
    description: Save + saved-exercise library rewired to the new ExerciseSummary
    files_touched: [src/ChordFlow.Desktop/wwwroot/app.js]
    blocked_by: [3]
    satisfies: [IN6, C6]
  - id: fe-player-settings
    order: 7
    status: done
    description: Player settings — metronome/count-in toggles + per-track volumes
    files_touched: [src/ChordFlow.Desktop/wwwroot/score-render-component.js, src/ChordFlow.Desktop/wwwroot/app.js]
    blocked_by: []
    satisfies: [IN5]
  - id: ref-sync-architecture
    order: 8
    status: done
    description: Sync the architecture reference for the widened generate envelope
    files_touched: [loom/refs/chordflow-architecture-reference.md]
    blocked_by: [2]
    satisfies: [C5]
  - id: verify
    order: 9
    status: done
    description: "Verify end-to-end: select → Generate → two-track play → Save → reload"
    files_touched: [src/ChordFlow.Desktop/wwwroot/app.js]
    blocked_by: [5, 6, 7]
    satisfies: [IN3, IN4, IN7]
---
# Exercise workbench — consumption UI over the canonical Exercise

## Goal

Turn today's hard-wired MVP Practice view into the consumption UI for the canonical `Exercise`: select a stored Song/Progression + Comping + optional Lead, set the params (Key/Tempo/Difficulty/Feel), Generate, and play the two-track score — then Save/Mark-practiced/revisit. The only Core touch is the IN8 generate-path plumbing (widen the `generate` envelope + handler to resolve content references from the stores instead of the hard-wired blues); everything else is front-end on the already-shipped `bridge.js`, `score-render-component.js`, and `content-crud` `entity*` envelopes. Two-track render (IN4) and the Practice⇄Content toggle (IN7) are already delivered by the render component + content-crud and are verified, not rebuilt. Saved exercises are disposable (C6) — no migration. Steps 1–2 are Core (IN8); steps 3–7 are the front-end; step 8 syncs the architecture ref; step 9 verifies end-to-end.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Generate slice resolves content references from the stores (delete the hard-wired blues) | src/ChordFlow.Core/Features/GenerateExercise/GenerateExercise.cs, src/ChordFlow.Core/Persistence/SongStore.cs, src/ChordFlow.Core/Persistence/RhythmPatternStore.cs, tests/ChordFlow.Core.Tests/Features/GenerateExerciseTests.cs | — | IN8, C4 |
| ✅ | 2 | Widen the generate bridge envelope + router verb + Program.cs wiring | src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Desktop/Program.cs, tests/ChordFlow.Core.Tests/Bridge/WebMessageRouterContentTests.cs | 1 | IN8, C5 |
| ✅ | 3 | Definition pickers populated from the content stores (Song/Progression + Comping + optional Lead) | src/ChordFlow.Desktop/wwwroot/app.js, src/ChordFlow.Desktop/wwwroot/index.html | — | IN2, C2, C4 |
| ✅ | 4 | Params surface — Difficulty + Feel selects alongside Key (saved defaults) | src/ChordFlow.Desktop/wwwroot/app.js, src/ChordFlow.Desktop/wwwroot/index.html | — | IN1, C3 |
| ✅ | 5 | Generate wiring — emit the new reference+params envelope | src/ChordFlow.Desktop/wwwroot/app.js | 2, 3, 4 | IN3 |
| ✅ | 6 | Save + saved-exercise library rewired to the new ExerciseSummary | src/ChordFlow.Desktop/wwwroot/app.js | 3 | IN6, C6 |
| ✅ | 7 | Player settings — metronome/count-in toggles + per-track volumes | src/ChordFlow.Desktop/wwwroot/score-render-component.js, src/ChordFlow.Desktop/wwwroot/app.js | — | IN5 |
| ✅ | 8 | Sync the architecture reference for the widened generate envelope | loom/refs/chordflow-architecture-reference.md | 2 | C5 |
| ✅ | 9 | Verify end-to-end: select → Generate → two-track play → Save → reload | src/ChordFlow.Desktop/wwwroot/app.js | 5, 6, 7 | IN3, IN4, IN7 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:core-reference-resolving-generateexercisehandler -->
### Step 1 — Core — reference-resolving GenerateExerciseHandler

Replace `Build(keyPitchClass, rhythmId, tempo)`'s hard-wired `SeedData.TwelveBarBlues` + seed-rhythm lookup with reference resolution. New shape: `Build(harmonyEntity, harmonyId, compingId, leadId?, keyPitchClass?, tempo, difficulty, feel)`.

- **Harmony discriminator mirrors content-crud's `entity`:** `harmonyEntity ∈ {"song","progression"}`. `progression` → `ProgressionStore.Get(id)` → `Song.OfProgression(prog, key)`; `song` → `SongStore` parsed `Song`. One realization path downstream (C4).
- **Store getters:** `ProgressionStore` already returns a parsed `Progression`. Add domain-object getters where missing — `RhythmPatternStore.Get(id) → RhythmPattern` (comping + optional lead) and `SongStore.Get(id) → Song` (parse the stored DSL via `SongParser`/`Song.FromSections`). These are read methods on existing stores — `EX7` plumbing, no new capability.
- **KeyOverride:** `keyPitchClass` (optional) → `KeyOverride`; for a bare progression it's the only persistent key home (as today), for a song it defaults to `Song.InitialKey`.
- Tests: progression-ref build == today's blues build for the seed ids (parity); song-ref build; missing ref → clear failure.

<!-- step:core-generate-envelope-carries-content-references -->
### Step 2 — Core — generate envelope carries content references

Widen the inbound `generate` envelope from `(key, rhythmId, tempo)` to `(harmonyEntity, harmonyId, compingPatternId, leadPatternId?, key?, tempo, difficulty, feel, renderOptions?)`. Update `GenerateRequested` to carry them and `Program.cs` to forward to the step-1 handler (short-lived `DbContext` + stores, as the capstone established). Keep `renderOptions` plumbing intact (score-render-component). Tests: router parses the new fields; absent optional fields default sanely. Dependency direction Desktop → Core unchanged (C5).

<!-- step:fe-definition-selection -->
### Step 3 — FE — definition selection

Replace the hard-wired 3-rhythm `RHYTHMS` block with live pickers fed by `content-crud`'s existing `entityList`/`entityGet` envelopes (C2 — no new bridge): a **harmony** picker (songs + progressions, each tagged with its `entity` so generate sends the right discriminator), a required **comping** rhythm picker, and an optional **lead** rhythm picker. Request the three lists on Practice-view init; cache them (reused for library labels in step 6). index.html: the picker markup in the builder toolbar. UI shows no Progression-vs-Song branch — both are just harmony choices (C4).

<!-- step:fe-exercise-params -->
### Step 4 — FE — exercise params

Add **Difficulty** and **Feel** selects (Key picker already exists; Tempo lives in the component transport). These are params (values), kept distinct from the definition references (C3); persisted as saved defaults so they author the next Generate. Enumerate Difficulty/Feel from their domain enum values (plain labels).

<!-- step:fe-generate -->
### Step 5 — FE — Generate

`selections()` + `sendScoreRequest()` build the widened `generate` envelope (harmonyEntity/harmonyId, compingPatternId, leadPatternId?, key, tempo, difficulty, feel) + the component's `renderOptions`. Update the boot/`onNeedsRerender` replay seed to the new shape. Dev (no-bridge) fallback still renders `SAMPLE_TEX`.

<!-- step:fe-save-library -->
### Step 6 — FE — Save & library

The capstone reshaped `ExerciseSummary` to references (`SongId`/`CompingPatternId`/`LeadPatternId?`/`KeyOverride`/`Tempo`/`Difficulty`/`Feel`); today's `renderLibrary`/`libraryLabel` still read the dropped `ex.key`/`ex.rhythmId` and are broken. Rewire them to the new fields, **resolving ids→display names from the cached entity lists** (step 3) so labels stay human-readable without a Core change. Save posts the current references+params. No data migration — existing rows are disposable; wipe/recreate freely (C6).

<!-- step:fe-player-settings -->
### Step 7 — FE — player settings

Metronome + count-in toggles already ship in the component's `controls:"full"` strip (PLAYER_KIND → `api.metronomeVolume`/`countInVolume`). Remaining IN5: add **per-track volume** controls (rhythm-guitar vs lead-guitar) — alphaTab per-track playback volume — to `score-render-component.js` as PLAYER_KIND options and surface them in the strip. User prefs only; not part of the Exercise (C3 boundary).

<!-- step:ref-sync-architecture -->
### Step 8 — Ref sync — architecture

Reference-doc sync (mandatory, same unit of work): update §5 bridge-protocol to document the widened `generate` envelope (content references + params) and the reference-resolving generate slice. The DSL/domain refs are untouched (no DSL or domain-model change).

<!-- step:verify -->
### Step 9 — Verify

Run the app: pick a Song with a Lead → Generate → confirm the **two-track** staff renders and plays with the synced cursor (IN4, already delivered by the render component — verified not rebuilt); pick a bare Progression → single track; Save → the library label is human-readable → reload restores it; the Practice⇄Content toggle (IN7, delivered by content-crud) still switches cleanly. No data preservation expected (C6).
