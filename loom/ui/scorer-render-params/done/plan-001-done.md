---
type: done
id: pl_01KWY2763RDD6WTSNV1AJ53901-done
title: Done — ScoreR owns render params (key/tempo/feel) — seeded + live
status: done
created: 2026-07-07
version: 1
tags: []
parent_id: pl_01KWY2763RDD6WTSNV1AJ53901
requires_load: []
---
# Done — ScoreR owns render params (key/tempo/feel) — seeded + live

## Step 1 — Add nullable `Song.DefaultTempo` (int?) and parse a `tempo <bpm>` line in SongParser, mirroring the `feel` keyword exactly.

`Song.DefaultTempo` (`int?`, nullable peer of `DefaultFeel`/`InitialKey`) threaded through the private ctor + `FromSections`. `SongParser` gained a `TempoKeyword` + `MinTempo`/`MaxTempo` (40–240) and a `tempo <bpm>` branch mirroring `feel`: at-most-once, position-independent, `NumberStyles.None` (rejects sign/decimal), out-of-range/duplicate/malformed → `FormatException`; also added to the reserved-part-name guard. Tests: `SongParserTests` +7 (valid bpm theory, null-when-absent, position-independent, out-of-range/malformed theory, duplicate, reserved-name).

## Step 2 — Add `DefaultTempo` to the content read DTO (ContentSummary/ContentItem) and populate it from Song, so catalog items carry it (twin of DefaultFeel/InitialKey).

`DefaultTempo` added to `ContentSummary` + `ContentItem` (trailing optional param). `SongStore.SeedsOf` returns a 3-tuple `(Key, Feel, Tempo)` and the list projection carries `DefaultTempo`. `ContentCrudHandler.ToItem` maps it. Test: `ContentCrudStoreTests.SongList_SurfacesDefaultTempo` (132 vs null).

## Step 3 — Update the DSL reference (add a `tempo <bpm>` row + example, mirroring the `feel` row) and the domain-model reference (add Song.DefaultTempo).

DSL ref: added a `tempo <bpm>` table row + example line + a bullet (peer of `feel`), and reconciled the stale 'no feel/swing token' Note to describe `feel`/`tempo` as play-time-control default seeds. Domain ref: noted the `Tempo` seed on the Exercise params line + a new 'Tempo is content-suggested, not baked' bullet mirroring the feel one. Both via `loom_patch_doc` (refs are gate-excluded).

## Step 4 — Add a Key control to ScoreR's transport (opt-in, always shown, default C) and the getKey/seedKey/setKey + seedTempo methods mirroring the feel triad.

score-render-component.js: added `KEY_NAMES`, an opt-in `key` flag + a Key `<select>` in the transport (`keyPicker`), and the methods `getKey`/`seedKey`(control-only)/`setKey`(fires `onNeedsRerender` — transpose) + `seedTempo`(baseTempo+input, no re-render). Usage-doc header updated. `keyEnabled` passed to `buildControls` via `extra`.

## Step 5 — Remove the Practice page's own Key <select>; ScoreR owns it. selections() reads view.getKey(); seedKeyForHarmony→view.seedKey; add seedTempoForHarmony; onNeedsRerender carries keyPitchClass.

app.js: removed the `#key` markup (index.html) + the dead `KEY_NAMES`/key fill; `selections().keyPitchClass` reads `view.getKey()`; `seedKeyForHarmony`→`view.seedKey`; new `seedTempoForHarmony`; both wired on harmony change (seed-only, no re-render — EX2). `onNeedsRerender` now carries `keyPitchClass` beside `tripletFeel`. `key:true` on the Practice ScoreR.

## Step 6 — The loadScore reply carries the persisted render-param triple (KeyOverride/Tempo/TripletFeel); app.js seeds ScoreR from them on load (never via seed*ForHarmony) so the stored override wins.

`LoadScoreEnvelope` gained `Key` (effective = `KeyOverride ?? Song.InitialKey` tonic) + `TripletFeel`; app.js `loadScore` seeds key+feel (tempo already via `load({tempo})`). `ExerciseLibrary.Load` accepts transient `keyOverride`/`tripletFeel` and applies them via `with` (re-voices the displayed piece, stored def untouched). Router `LoadExerciseRequested` → `(id, int?, TripletFeel?, RenderOptions)` with a `ParseNullableEnum` helper (absent ⇒ null ⇒ stored value wins, C2); Program.cs passes them through. Without this a live Key/Feel change on a *loaded* exercise would snap back — a latent gap the seeding exposed. Tests: 2 new router tests (plain vs replayed override).

## Step 7 — In content-crud.js, seed ScoreR Key/Tempo/Feel from the selected item's DTO defaults before requestPreview, and pass {tempo} to load — fixing 'always Straight' and never-seeded tempo/key.

Chose Option 1 (full preview symmetry) — scope bumped beyond the JS-only plan (recorded on the step's files). Threaded `keyPitchClass`+`tempo` through `entityPreview` → router (`EntityPreviewRequested` now 7-arg) → Program.cs → `ContentCrudHandler.Preview` → the 3 preview builders. Key design point: a **null** key lifts progression/rhythm at C but keeps a **Song in its own authored InitialKey** (never forced to C) — `SongPreview` passes `startKey` (nullable) to `SongExpander.Expand`. content-crud.js: `key:true` on the preview ScoreR; `pendingSeeds` captured on `entityLoaded` and applied via `applySeeds()` (now + on lazy ScoreR creation); `requestPreview` sends key/tempo (falling back to `pendingSeeds` pre-ScoreR so the FIRST preview renders seeded); `renderScore(tex, tempo)` loads with `{tempo}`. Tests: +2 router (carries/absent key+tempo), +3 handler (key transposes, tempo drives envelope, Song absent-key keeps its own key).

## Step 8 — Build + test; run the app and confirm the seeded/live behavior on both Practice and the Content preview.

Full solution builds; **813 Core tests pass**; all 4 touched JS files `node --check` clean. App launched (built `ChordFlow.exe`) — boots and runs clean (WebView2 loaded, no crash in the bridge/render path). Visual dogfood confirmed working by Rafa across Practice + Content preview (seed + live transpose/tempo/feel; progression falls back to C/80/Straight). **Out-of-scope observations Rafa noted (potential future threads):** (1) exercise save allows only one exercise per song/progression; (2) no way to delete/update a saved exercise. Both explicitly out of scope for this thread.
