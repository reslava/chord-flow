---
type: plan
id: pl_01KWY2763RDD6WTSNV1AJ53901
title: ScoreR owns render params (key/tempo/feel) — seeded + live
status: done
created: 2026-07-07
updated: 2026-07-07
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KWY1YTRRPV5XZ1R23GZ7BNG4
requires_load: []
target_version: 0.1.0
actual_release: 0.13.0
steps:
  - id: domain-song-defaulttempo-directive
    order: 1
    status: done
    description: Add nullable `Song.DefaultTempo` (int?) and parse a `tempo <bpm>` line in SongParser, mirroring the `feel` keyword exactly.
    files_touched: [src/ChordFlow.Core/Music/Songs/Song.cs, src/ChordFlow.Core/Music/Songs/SongParser.cs, src/ChordFlow.Core.Tests]
    blocked_by: []
    satisfies: [IN1, C1, C4]
  - id: read-dto-carries-defaulttempo
    order: 2
    status: done
    description: Add `DefaultTempo` to the content read DTO (ContentSummary/ContentItem) and populate it from Song, so catalog items carry it (twin of DefaultFeel/InitialKey).
    files_touched: [src/ChordFlow.Core]
    blocked_by: []
    satisfies: [IN1, IN6]
  - id: ref-sync-dsl-domain
    order: 3
    status: done
    description: Update the DSL reference (add a `tempo <bpm>` row + example, mirroring the `feel` row) and the domain-model reference (add Song.DefaultTempo).
    files_touched: [loom/refs/chordflow-dsl-reference.md, loom/refs/chordflow-domain-model-reference.md]
    blocked_by: []
    satisfies: [IN9]
  - id: scorer-owns-key-seed-methods
    order: 4
    status: done
    description: Add a Key control to ScoreR's transport (opt-in, always shown, default C) and the getKey/seedKey/setKey + seedTempo methods mirroring the feel triad.
    files_touched: [src/ChordFlow.Desktop/wwwroot/score-render-component.js]
    blocked_by: []
    satisfies: [IN2, IN4, IN5, IN8, C3]
  - id: practice-page-move-key-in-seed
    order: 5
    status: done
    description: Remove the Practice page's own Key <select>; ScoreR owns it. selections() reads view.getKey(); seedKeyForHarmony→view.seedKey; add seedTempoForHarmony; onNeedsRerender carries keyPitchClass.
    files_touched: [src/ChordFlow.Desktop/wwwroot/app.js, src/ChordFlow.Desktop/wwwroot/index.html]
    blocked_by: []
    satisfies: [IN3, IN4, IN6, C2]
  - id: saved-exercise-load-round-trip
    order: 6
    status: done
    description: The loadScore reply carries the persisted render-param triple (KeyOverride/Tempo/TripletFeel); app.js seeds ScoreR from them on load (never via seed*ForHarmony) so the stored override wins.
    files_touched: [src/ChordFlow.Core, src/ChordFlow.Desktop/wwwroot/app.js]
    blocked_by: []
    satisfies: [IN6, C2]
  - id: content-preview-seeds-the-triple-tempo
    order: 7
    status: done
    description: In content-crud.js, seed ScoreR Key/Tempo/Feel from the selected item's DTO defaults before requestPreview, and pass {tempo} to load — fixing 'always Straight' and never-seeded tempo/key.
    files_touched: [src/ChordFlow.Desktop/wwwroot/content-crud.js, src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudEnvelopes.cs, src/ChordFlow.Desktop/Program.cs, tests/ChordFlow.Core.Tests]
    blocked_by: []
    satisfies: [IN7, C2]
  - id: dogfood-verify
    order: 8
    status: done
    description: Build + test; run the app and confirm the seeded/live behavior on both Practice and the Content preview.
    files_touched: []
    blocked_by: []
    satisfies: [IN2, IN7]
---
# ScoreR owns render params (key/tempo/feel) — seeded + live

## Goal

Make ScoreR the single owner of the three render/interpretation params — Key, Tempo, Feel — seeded per content and live on change, uniformly across the Practice page and the Content preview. Built bottom-up: first the `tempo <bpm>` Song directive → `Song.DefaultTempo` on the read DTO, then ScoreR grows the Key control + `seedKey`/`seedTempo`/`setKey` methods (mirroring the banked feel triad), then the two consumers wire seeding + the load round-trip. Per the locked decisions: **Key** and **Feel** re-emit through `onNeedsRerender` (transpose / `\tf` — cheap, no regenerate); **Tempo** stays a local alphaTab `playbackSpeed` knob with no C# round-trip. Harmony/comping/lead/difficulty/voicing stay Generate-gated definition params; the "live-on-select" fix is the Content preview's seeding, not auto-render in Practice.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add nullable `Song.DefaultTempo` (int?) and parse a `tempo <bpm>` line in SongParser, mirroring the `feel` keyword exactly. | src/ChordFlow.Core/Music/Songs/Song.cs, src/ChordFlow.Core/Music/Songs/SongParser.cs, src/ChordFlow.Core.Tests | — | IN1, C1, C4 |
| ✅ | 2 | Add `DefaultTempo` to the content read DTO (ContentSummary/ContentItem) and populate it from Song, so catalog items carry it (twin of DefaultFeel/InitialKey). | src/ChordFlow.Core | — | IN1, IN6 |
| ✅ | 3 | Update the DSL reference (add a `tempo <bpm>` row + example, mirroring the `feel` row) and the domain-model reference (add Song.DefaultTempo). | loom/refs/chordflow-dsl-reference.md, loom/refs/chordflow-domain-model-reference.md | — | IN9 |
| ✅ | 4 | Add a Key control to ScoreR's transport (opt-in, always shown, default C) and the getKey/seedKey/setKey + seedTempo methods mirroring the feel triad. | src/ChordFlow.Desktop/wwwroot/score-render-component.js | — | IN2, IN4, IN5, IN8, C3 |
| ✅ | 5 | Remove the Practice page's own Key <select>; ScoreR owns it. selections() reads view.getKey(); seedKeyForHarmony→view.seedKey; add seedTempoForHarmony; onNeedsRerender carries keyPitchClass. | src/ChordFlow.Desktop/wwwroot/app.js, src/ChordFlow.Desktop/wwwroot/index.html | — | IN3, IN4, IN6, C2 |
| ✅ | 6 | The loadScore reply carries the persisted render-param triple (KeyOverride/Tempo/TripletFeel); app.js seeds ScoreR from them on load (never via seed*ForHarmony) so the stored override wins. | src/ChordFlow.Core, src/ChordFlow.Desktop/wwwroot/app.js | — | IN6, C2 |
| ✅ | 7 | In content-crud.js, seed ScoreR Key/Tempo/Feel from the selected item's DTO defaults before requestPreview, and pass {tempo} to load — fixing 'always Straight' and never-seeded tempo/key. | src/ChordFlow.Desktop/wwwroot/content-crud.js, src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudEnvelopes.cs, src/ChordFlow.Desktop/Program.cs, tests/ChordFlow.Core.Tests | — | IN7, C2 |
| ✅ | 8 | Build + test; run the app and confirm the seeded/live behavior on both Practice and the Content preview. | — | — | IN2, IN7 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:domain-song-defaulttempo-directive -->
### Step 1 — Domain: Song.DefaultTempo + `tempo` directive

`DefaultTempo` is the nullable peer of `DefaultFeel`/`InitialKey` — absent vs explicit is meaningful (absent → the 80 default is applied downstream, never stored on Song). `SongParser`: a `tempo <bpm>` line, at-most-once, position-independent (a whole-song default, not a stream item); duplicate / malformed / non-integer / out-of-range (outside 40–240) → `FormatException`, matching the `feel` handling. Unit tests: parses a bpm, rejects duplicate/garbage/out-of-range, omitted → null.

<!-- step:read-dto-carries-defaulttempo -->
### Step 2 — Read DTO carries DefaultTempo

Mirror how `DefaultFeel`/`InitialKey` are surfaced from the song-default-feel work. Absent DefaultTempo serializes as null → the JS side coalesces to 80.

<!-- step:ref-sync-dsl-domain -->
### Step 3 — Ref sync — DSL + domain

Same unit of work as the domain change (CLAUDE-LOCAL ref-sync rule). Frame `tempo` as a default-seed directive like `feel` — a play-time-control default, not content-baked playback (reconcile with the DSL ref's 'tempo chosen at play time' line). Edit via loom_patch_doc / loom_update_doc (refs are gate-excluded).

<!-- step:scorer-owns-key-seed-methods -->
### Step 4 — ScoreR owns Key + seed methods

`setKey(pc)` fires `onNeedsRerender` (key is a re-emit/transpose param — the twin of `setTripletFeel`); `seedKey(pc)` updates the control only (twin of `seedTripletFeel`). `seedTempo(bpm)` sets `baseTempo` + the tempo input WITHOUT `onNeedsRerender` (tempo is local playback-speed — EX3). Key/feel stay first-class envelope fields, not renderOptions. The Key <select> is shown for progression/rhythm too, defaulted to C (IN8).

<!-- step:practice-page-move-key-in-seed -->
### Step 5 — Practice page: move Key in, seed tempo, carry key on re-render

Delete the `#key` picker markup + `populateStaticPickers` key fill; `selections().keyPitchClass` reads `view.getKey()`. `seedKeyForHarmony` → `view.seedKey(song.initialKey ?? 0)`; new `seedTempoForHarmony` → `view.seedTempo(song.defaultTempo ?? 80)`, both on harmony switch only. The `onNeedsRerender` handler adds `keyPitchClass: view.getKey()` beside `tripletFeel`. Harmony stays Generate-gated — no auto-render on switch (EX2).

<!-- step:saved-exercise-load-round-trip -->
### Step 6 — Saved-exercise load round-trip

Host side: include KeyOverride (or the effective key), Tempo, and TripletFeel on the `loadScore` envelope. JS side: on `loadScore`, `view.seedKey`/`seedTempo`/`seedTripletFeel` from the envelope (load already passes `{tempo}`); the load path must NOT call the `seed*ForHarmony` functions, so a stored override survives the seed-on-switch precedence (C2).

<!-- step:content-preview-seeds-the-triple-tempo -->
### Step 7 — Content preview seeds the triple + tempo

On `entityLoaded`, seed `view.seedTripletFeel(item.defaultFeel ?? 'None')`, `view.seedKey(item.initialKey ?? 0)`, `view.seedTempo(item.defaultTempo ?? 80)` BEFORE `requestPreview()`. `renderPreview` passes `{ tempo: msg.tempo }` to `scoreView.load` (currently omitted → stuck at 80). Enable the Key control on the preview's ScoreR.

<!-- step:dogfood-verify -->
### Step 8 — Dogfood + verify

A `feel triplet8th` / `tempo 120` song pre-selects Triplet8th + 120 and renders swung at 120 (on select in the preview, on Generate in Practice); a progression falls back to C / 80 / Straight. Live key change transposes with no regenerate; live tempo change re-speeds with no C# round-trip. Run the verify skill on the affected flow.
