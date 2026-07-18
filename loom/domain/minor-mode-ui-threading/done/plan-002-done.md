---
type: done
id: pl_01KXT1CMNQMAV9EAQMS001XYHH-done
title: Done — Minor key mode — thread through content preview, list seeding & loadExercise
status: done
created: 2026-07-18
version: 5
tags: []
parent_id: pl_01KXT1CMNQMAV9EAQMS001XYHH
requires_load: []
---
# Done — Minor key mode — thread through content preview, list seeding & loadExercise

## Step 1 — ContentSummary gains InitialKeyIsMinor; each store's List projection sets it (progression from CatalogMetadata.Tonality, song from its key mode), and the entityList payload carries it.

Added `bool? InitialKeyIsMinor` to `ContentSummary` (IContentStore.cs) and `ContentItem` (ContentCrudEnvelopes.cs), mapped in `ContentCrudHandler.ToItem`, so the `entityList` payload carries the mode.

- `ProgressionStore.List` now reads each row's `CatalogHeader.Parse(Dsl).Metadata.Tonality` (winning tier via `OriginResolver.ResolveOne`, mirroring `SongStore.SeedsOf`) → `InitialKeyIsMinor` (true for `tonality: minor`, false otherwise).
- `SongStore.SeedsOf`/`List` extended to carry `song.InitialKey.IsMinor` (a `key Am` song → true).
- `EngineVoicingSource` (automatic voicings) unaffected — the new param defaults null.

Tests (ContentCrudStoreTests): `ProgressionList_SurfacesInitialKeyIsMinor_FromTheTonalityHeader` (minor=true, major=false) and `SongList_SurfacesInitialKeyIsMinor_FromTheSongsOwnKeyMode` (`key Am`=true, `key C`=false). Full Core suite green — 1012 passed. Updated the `IContentStore` row in the domain-model ref (seed fields incl. `InitialKeyIsMinor`).

## Step 2 — entitySave carries an optional explicit tonality; the store serializes it into the catalog header when present (else the plan-001 preserve-source behavior), so a new minor progression can be authored and a major↔minor flip is written.

Threaded an explicit `tonality` from the editor through the save seam so a new minor progression can be authored (and a major↔minor flip written).

- Inbound envelope gains `string? Tonality`; `EntitySaveRequested` event + `Program.cs` lambda carry it; `WebMessageRouter` passes `envelope.Tonality`.
- `ContentCrudHandler.Save(..., string? tonality = null)` parses the wire `"major"/"minor"` via a fail-loud `ParseTonality` → `Tonality?` and passes it to the store.
- `IContentStore.Save` gains `Tonality? tonality = null`. `ProgressionStore.Save` applies it — `meta = meta with { Tonality = chosen }` before `CatalogHeader.Serialize` — so an explicit value overrides the preserved/source tonality; absent ⇒ the plan-001 preserve-source behavior (C3). Major ⇒ no header ⇒ body verbatim (C1). `SongStore`/`VoicingStore`/`RhythmPatternStore` accept the param inertly (C4).

Tests: `Save_WithExplicitMinorTonality_AuthorsANewMinorProgression`, `Save_WithExplicitMajorTonality_WritesNoHeader_ForByteIdenticalMajor`, `Save_ExplicitTonality_OverridesTheSourceOnFork`; router tests updated to the 6-arg event with a tonality assertion. Full Core suite green — 1015 passed. Updated the `IContentStore.Save` row in the domain-model ref.

## Step 3 — content-crud.js gains a major/minor control (progressions only) seeded from the list item's initialKeyIsMinor; it sends keyIsMinor on entityPreview (→ the reported \ks A becomes \ks Aminor) and the explicit tonality on entitySave.

Added a **major/minor tonality control** to the content editor (`content-crud.js` + `index.html`), shown only for progressions (`tonality: true` on the entity config; a song's mode is its `key`/`mod` stream, EX4).

- New `#ccTonalityRow` (label + `<select>`) placed under the name field; `.cc-tonality` CSS mirrors the existing select styling; hidden for non-progression entities via `tonalityRow.hidden = !current.tonality`.
- **Seeds** from the content: `pendingSeeds.keyIsMinor` captured from the list item's `initialKeyIsMinor` (step 1), applied to the control on `entityLoaded`; `newItem` resets to major.
- **Drives the live preview**: `requestPreview` sends `keyIsMinor` from the control (progressions only); a `change` listener re-previews on a flip. The bridge → `ContentCrudHandler.Preview(keyIsMinor)` path already existed (step 8a), so the preview now emits `\ks {tonic}minor` instead of the reported `\ks A`. ScoreR stays mode-free (EX5).
- **Written on save**: `onSave` sends `tonality` (the control's value) → the step-2 explicit-tonality save path persists it.

Desktop compiles (verified to a scratch output; the running app locks the normal bin). Updated the DSL ref: the editor-control note + `tonality:` added to the catalog-header field list. Note: the live in-app dogfood (visually confirming `\ks Aminor` on a minor preview) is best done in a fresh launch — the JS wiring is complete and the C# preview path is unit-covered from step 8a.

## Step 4 — The loadExercise reply carries keyIsMinor from Exercise.KeyOverride; the load path seeds it via the existing hc.seedKeyMode so a saved minor exercise reopens minor and re-keying keeps mode.

Threaded the key mode through the `loadExercise` reply + the re-key path.

- `LoadScoreEnvelope` gains `bool KeyIsMinor`; `From` sources it from the effective key `(exercise.KeyOverride ?? Song.InitialKey).IsMinor` (the single builder — GenerateExercise + ExerciseLibrary both flow through it).
- `ExerciseLibrary.Load` gains `bool? keyIsMinor = null`; the re-key branch now builds `new Key(pc, keyIsMinor ?? exercise.KeyOverride?.IsMinor ?? false)` — **fixing the hard-wired `IsMinor: false`** so re-keying keeps the mode (IN5).
- `WebMessageRouter.LoadExerciseRequested` event gains a `bool?`; the `loadExercise` case passes `envelope.KeyIsMinor` (already on the inbound envelope from step 8a); `Program.cs` forwards it to `Load`.
- `app.js` `loadScore` handler calls `hc.seedKeyMode(msg.keyIsMinor)` next to the existing `hc.seedKey(msg.key)`.

Tests: `LoadScore_CarriesKeyIsMinor_ForASavedMinorExercise`, `ReKey_PreservesTheModeFromTheRequest_NotHardWiredMajor`; the router loadExercise tests updated to the 5-arg event with keyIsMinor assertions. Full Core suite green — 1017 passed; Desktop compiles. No ref change: the bridge-envelope field addition is below the architecture ref's role-level granularity.

## Step 5 — A golden that a multi-section Song in several keys/modes (via key/mod) realizes each section correctly, plus payload tests that entityList and loadExercise carry the mode.

Added the multi-key/multi-mode song golden (`SongExpanderTests.Expand_MultiKeyMultiMode_RealizesEachSectionInItsOwnKeyAndMode`): a song with `key Am` / `key G` / `key Bm` sections expands to three sections whose `.Key` is (9,minor) / (7,major) / (11,minor) respectively — confirming the Song `key` stream already threads tonic + mode per section (no new modeling, as the design predicted).

Payload coverage (the other half of IN6) was already delivered by the step-1 and step-4 tests — `ProgressionList_SurfacesInitialKeyIsMinor` / `SongList_SurfacesInitialKeyIsMinor` (list `InitialKeyIsMinor`) and `LoadScore_CarriesKeyIsMinor` (loadExercise reply) — so this step only added the multi-key song confirm rather than duplicating them.

Full Core suite green — 1018 passed. Tests-only step, no ref change.
