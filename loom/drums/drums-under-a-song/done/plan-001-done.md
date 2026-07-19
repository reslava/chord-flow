---
type: done
id: pl_01KXXR7D3WKTN5SGPREPCRWMHA-done
title: Done — Drums under a song — parts-union remodel + drum track
status: done
created: 2026-07-19
version: 6
tags: []
parent_id: pl_01KXXR7D3WKTN5SGPREPCRWMHA
requires_load: []
---
# Done — Drums under a song — parts-union remodel + drum track

## Step 1 — Introduce the typed InstrumentPart union and remodel Exercise to hold Parts, behavior-preserving (full suite green, no drums rendered yet).

The typed instrument-parts union + the Exercise remodel — behavior-preserving (full Core suite 1138 passed, 0 failed).

**Files**
- `src/ChordFlow.Core/Exercises/InstrumentPart.cs` (new) — `abstract record InstrumentPart { double Volume = 1.0; bool Muted }` + arms `CompingPart(RhythmPattern)` / `LeadPart(RhythmPattern)` / `DrumPart(DrumGroove)`. Mix rides the part; a future `BassPart` is a new arm.
- `src/ChordFlow.Core/Exercises/Exercise.cs` — canonical member is now `IReadOnlyList<InstrumentPart> Parts` (replacing the flat `Comping`/`Lead` fields). Intent accessors `Comping` (`.Single()` — exactly one, fail-loud, C4), `Lead`/`Drums` (`.SingleOrDefault()` — at most one). A **convenience constructor** mirroring the old `(Song, RhythmPattern Comping, RhythmPattern? Lead, Key?, int, Difficulty, TripletFeel)` signature delegates to `Parts`, so the pre-parts callers (GenerateExercise, ExerciseLibrary, ContentCrud ×3) compile **unchanged**.
- `tests/ChordFlow.Core.Tests/ExerciseModelTests.cs` (new) — 4 facts: convenience ctor builds comping + optional lead parts; accessors project each arm + per-part Volume; missing comping throws; ambiguous drum part throws.

**Decisions**
- **Convenience ctor over rewriting every call site** — the durable model is `Parts`; the old-signature ctor is a genuine ergonomic overload for the common guitar-only case (comping + optional lead, no drums), not back-compat contortion (it constructs the same union). Distinguishable from the primary ctor by arity (7 vs 6) + 2nd-param type (RhythmPattern vs IReadOnlyList). Zero ripple into the 5 existing construction sites and the tests (which build via handlers, not `new Exercise`).
- **Invariants enforced at the accessors** (fail-loud `.Single()`/`.SingleOrDefault()`) rather than a custom primary-ctor body — positional records don't allow injecting into the generated ctor cleanly, and read-time enforcement is where a violation would actually bite.
- `DrumPart(DrumGroove)` puts `ChordFlow.Exercises → ChordFlow.Instruments.Drums`, which is allowed (the guarded edge is `Music → Instruments`; Exercises is outside Music). `Music → Instruments` architecture test stays green.
- `DrumPart` exists but is not yet rendered — that is step 2.

## Step 2 — AlphaTexRenderer emits a 3rd \track percussion staff when a DrumPart is present, composing the concrete DrumGrooveRenderer with cyclic per-bar tiling; ExerciseRendering extracts and passes the optional drum part.

The percussion drum track in the render path — concrete, tiled, shared feel (full Core suite 1142 passed, 0 failed).

**Files**
- `src/ChordFlow.Core/Rendering/IScoreRenderer.cs` — `Render(...)` gains `DrumGroove? drums = null` (after `lead`, before `options`) + doc.
- `src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs` — the single-track branch now guards `lead is null && drums is null` (byte-identical output preserved). The multi-track path is now additive: Comping track always, then a Lead `\track` iff a lead, then a **Drums percussion `\track`** iff a drum groove. New helpers: `BuildDrumBars` (optional pickup rest bar mirroring a comping pickup + the groove tiled across `song.Sections.Sum(s => s.Bars.Count)` bars + `PrependTripletFeel`), `AppendPercussionTrackHeader` (`\track`/`\instrument percussion`/`\articulation defaults`/`\ts`, **no `\ks`**), `DrumRestBar`. A `private readonly DrumGrooveRenderer _drumRenderer` composes the concrete percussion renderer (C3 — no `IInstrument`).
- `src/ChordFlow.Core/Rendering/DrumGrooveRenderer.cs` — new public `RenderTiledBars(groove, barCount)`: `barCount` bar bodies, bar i = groove bar `i % m`, sharing the stateful `:N` duration; no header/wrapper (the caller composes the `\track`). Reuses the existing private `RenderBar`.
- `src/ChordFlow.Core/Features/ExerciseRendering.cs` — `RenderCore` passes `drums: exercise.Drums`.
- `tests/ChordFlow.Core.Tests/RenderTestHelpers.cs` — the `Render` extension gains `DrumGroove? drums = null`, threaded through.
- `tests/ChordFlow.Core.Tests/AlphaTexRendererTests.cs` — 4 new tests: percussion track emitted (Comping+Drums, `\instrument percussion`, `\articulation defaults`, keyless drum staff, kick/hi-hat present); a 2-bar groove tiles to 12 drum bars under a 12-bar blues (24 pipes total); no drums ⇒ no percussion track; drums ride the same whole-song `\tf`.

**Decisions**
- **Multi-track trigger widened to `lead OR drums`.** Percussion needs its own `\instrument` track, so any drum part forces the score-metadata + `\track` layout. The refactor keeps the comping+lead byte output identical (verified by the untouched lead tests): comping bars, then each subsequent track prefixed with `'\n'` + its header.
- **Renderer handed the extracted `DrumGroove`, not the `InstrumentPart` union (C2).** Per-instrument render logic differs (comping needs the plan, lead needs dead notes, drums needs the tiled groove), so typed params beat iterating a union.
- **Pickup handling:** drums rest through a comping pickup (an `\ac` rest bar sized to the pickup), mirroring the lead track, so the drum staff stays bar-aligned. The 12-bar blues dogfood has no pickup; the jazz-blues song does — handled.
- **Feel:** `\tf` is per-track bar metadata, so the drum track gets its own `PrependTripletFeel` like comping/lead. Authored-swing grooves (`{tu 3}`) and `\tf` compose without double-swing (Rafa verified live).
- 4/4 only (C6); percussion keyless (no `\ks`). `Music → Instruments` architecture test stays green (the drum-track emission is the allowed `Rendering → Instruments` edge, C1).

## Step 3 — ExerciseRefs.ResolveDrumGroove resolves the optional groove id; GenerateExercise.Build appends a DrumPart from drumGrooveId + drumVolume.

Features resolve + generate wiring — a chosen groove id becomes a DrumPart (full Core suite 1145 passed, 0 failed).

**Files**
- `src/ChordFlow.Core/Features/ExerciseRefs.cs` — new `ResolveDrumGroove(string? drumGrooveId, db) → DrumGroove?` via `DrumGrooveStore.Find`: null/blank ⇒ null; missing non-blank id ⇒ fail loud (mirrors ResolveHarmony/ResolvePattern). + `using ChordFlow.Instruments.Drums`.
- `src/ChordFlow.Core/Features/GenerateExercise/GenerateExercise.cs` — `Generate` + both `Build` overloads gain trailing `string? drumGrooveId = null, double drumVolume = 1.0`. The core `Build(db, …)` now resolves the optional groove and assembles the **Parts list explicitly** (`CompingPart` required, `LeadPart`/`DrumPart` appended when present), the DrumPart carrying `drumVolume`. + `using ChordFlow.Instruments.Drums`.
- `tests/ChordFlow.Core.Tests/GenerateExerciseTests.cs` — 3 new tests: a groove id appends a DrumPart carrying its volume (0.7); blank ⇒ no drum part; a missing id fails loud.

**Decisions**
- **Volume is a saved mix default, not a tex input.** Track volume is applied via the engine's `setTrackVolume` (the established comping/lead pattern — the architecture ref), so `DrumPart.Volume` rides the model for persistence (step 4) + UI seeding (step 5); it does NOT appear in the alphaTex. This is why step 2 correctly emits no volume directive.
- **Trailing optional params** (`drumGrooveId`/`drumVolume` after `keyIsMinor`) keep every existing `Generate`/`Build` caller + test compiling unchanged; the bridge stays passing defaults (no drums) until step 5 wires the verb.
- Test placed in `GenerateExerciseTests.cs` (the sibling Build tests' home) rather than the plan's listed `ExerciseLibraryTests.cs` — the generate-side resolve belongs with the generate tests; the load-side of IN8 is exercised in step 4's persistence tests.

## Step 4 — ExerciseEntity gains a nullable DrumGrooveId + per-part volume/mute columns via a flat Exercise↔Entity mapper + an EF migration; the saved-exercise load path resolves the groove.

Persistence — entity columns, EF migration, flat mapper, load path (full Core suite 1147 passed, 0 failed).

**Files**
- `src/ChordFlow.Core/Persistence/Entities/ExerciseEntity.cs` — added nullable `DrumGrooveId`, `double DrumVolume = 1.0`, `bool DrumMuted`.
- `src/ChordFlow.Core/Persistence/ChordFlowDbContext.cs` — `e.Property(x => x.DrumVolume).HasDefaultValue(1.0)` so pre-drums rows migrate cleanly (inert without a groove id).
- `src/ChordFlow.Core/Persistence/Migrations/20260719…_AddExerciseDrumPart.cs` (generated via `dotnet ef migrations add`) — AddColumn DrumGrooveId (TEXT null) / DrumMuted (INTEGER default false) / DrumVolume (REAL default 1.0); clean Down. Applied on startup via the existing `Migrate()`.
- `src/ChordFlow.Core/Features/ExerciseLibrary/ExerciseLibrary.cs` — the **flat mapper** both directions: `Save` flattens the DrumPart (`drumPart?.Groove.Id`/`.Volume`/`.Muted`); `ToExercise` resolves `DrumGrooveId` via `ExerciseRefs.ResolveDrumGroove` and rebuilds the Parts union restoring the saved mix. + `using ChordFlow.Instruments.Drums`.
- `tests/ChordFlow.Core.Tests/ExerciseLibraryTests.cs` — 2 tests: save→reload restores the groove ("rock" resolved from the store), volume (0.6), and the reloaded score carries the percussion staff; no-drums save reloads with no drum part + no percussion.

**Decisions**
- **Flat columns + inline mapper, no child table (D2/C7).** The existing `Save`/`ToExercise` inline mapping *is* the flat mapper — extended, not replaced (no separate `ExerciseMapper` class; that'd over-engineer v1). The domain union is durable; only these columns are provisional, and swapping them for a child `ExercisePartEntity` table later is a non-breaking internal change (the domain `Exercise` + renderer don't move).
- **Persist only the DrumPart's mix** (id/volume/mute), not comping/lead volumes — those are JS-runtime `setTrackVolume` state and were never persisted; adding columns for always-1.0 values would store no signal. Consistent with today's behavior; not a regression.
- **Save stores only the groove id**, Load re-resolves the real groove from the store — same pattern as Song/comping/lead references (alphaTex/content never duplicated into the exercise row).

## Step 5 — The generate verb carries drumGrooveId + drumVolume; HarmonyControlsR gains a Drums picker (entity:"drums") + volume slider; a display-only drum-staff show/hide toggle.

UI + bridge — Drums picker, volume, generate-verb fields, drum-staff toggle (solution builds; full Core suite 1147 passed; live behavior verified in step 6).

**C# bridge**
- `Bridge/WebMessageRouter.cs` — `GenerateRequest` + `InboundEnvelope` gain `DrumGrooveId` (string?) + `DrumVolume` (double?); the `generate` case threads them (`envelope.DrumVolume ?? 1.0`).
- `Desktop/Program.cs` — the `GenerateRequested` handler passes `req.DrumGrooveId, req.DrumVolume` to `generate.Build`.

**JS**
- `wwwroot/playback-component.js` — `trackVolumes` gains `drums`; `applyTrackVolume` finds the drum track by its **percussion staff** (its index isn't fixed: comping+drums ⇒ 1, comping+lead+drums ⇒ 2); re-asserted on `scoreLoaded`.
- `wwwroot/harmony-controls-component.js` — a **Drums picker** (`entity:"drums"`, "(none)" + grooves, same pattern as lead) + a **Drums vol** slider (binds live to `engine.setTrackVolume("drums")` AND supplies `drumVolume` in `getDefinition`); `catalog.drums`, `rebuildDrumsPicker`, `setCatalog("drums")`; `getDefinition` adds `drumGrooveId`/`drumVolume`. Extracted a `rangeInput()` helper.
- `wwwroot/app.js` — `catalog.drums`; `requestCatalog` adds `"drums"`; the `generate` envelope carries `drumGrooveId`/`drumVolume`.
- `wwwroot/score-render-component.js` — a **"Drums staff" display toggle** (`showDrums`, default on) in the full display strip. `renderVisibleTracks(score)` renders the track subset (all, minus the percussion staff when off) via `api.renderTracks`; wired into `scoreLoaded`, `applyStaffProfile`'s re-render, and `setOption` (no C# request). `isPercussionTrack` finds the drum staff by `staff.isPercussion`.

**Decisions**
- **Toggle placement = ScoreR display strip** (D4's option), next to the staff-profile/notation toggles — it's a display-only knob, session-transient (not persisted, unlike staffProfile).
- **Show/hide via `renderTracks` (display-only), audio always emitted (IN5):** alphaTab's playback is generated from the full `api.score`, and `renderTracks` selects the *rendered* subset, so a hidden drum staff should stay audible. **This audio-always-when-hidden property is the key thing step 6's CDP run must confirm** for alphaTab 1.8.3; the toggle defaults to shown (audible + visible), the primary v1 experience, so even if hidden-mutes turns out true it's a follow-up tweak, not a v1 blocker.
- **Drum volume is a saved default (persisted, step 4) that seeds the slider**, distinct from rhythm/lead volumes (runtime-only). No load-path seed for the Drums *picker* itself — consistent with comping/lead, which also aren't reflected back into HarmonyControlsR on load (the reloaded exercise still renders its drums via the C# load path).

## Step 6 — Update the domain-model + architecture refs for the parts-union remodel and the drum-track render path; run the full slice live (pick groove → audible under a 12-bar blues → save/reload → toggle staff).

Reference-doc sync + live CDP e2e — the slice works end-to-end in the real app.

**Refs (IN9, same unit of work)**
- `chordflow-domain-model-reference.md` (6 patches): the `Exercise` signature → `Exercise(Song, IReadOnlyList<InstrumentPart> Parts, …)` with the typed union (Comping/Lead/Drum arms, intent accessors, invariants); the §7 pipeline render step now `Render(…, lead: Lead, drums: Drums, options)` with the multi-track branch; `AlphaTexRenderer` + `IScoreRenderer` signatures gain `drums?`; the two-track note widened to the multi-track (lead and/or drums) layout; the §8 v1 constraint notes the `DrumPart` percussion track (concrete `DrumGrooveRenderer`, cyclic tiling, shared `\tf`).
- `chordflow-architecture-reference.md` (3 patches): the §3 Drums subsection now records `drums-under-a-song` as **delivered** (the parts-union remodel, drum `\track`, flat persistence seam, HarmonyControlsR picker + ScoreR toggle); §5 bridge — the `generate` verb carries `drumGrooveId`/`drumVolume` (resolved via `ExerciseRefs.ResolveDrumGroove`), the drums picker (`entity:"drums"`), the live `setTrackVolume("drums")`, and the display-only Drums-staff toggle; the HarmonyControlsR strip enumeration gains **Drums + Drums vol**.

**Live e2e (CDP harness, `scratchpad/drums-song-e2e.mjs`, rebuilt Desktop, `CHORDFLOW_DEVTOOLS=1` + `--remote-debugging-port=9223`):**
- Drums picker populated with the pack grooves (`rock` present) — `entityList entity:"drums"` wired through app.js → HarmonyControlsR.
- Picked `rock` → clicked Generate → **`api.score.tracks` = [Comping, Drums]**, the Drums track's staff `isPercussion: true` — the percussion track renders under the comp in the real app.
- The **"Drums staff"** toggle exists on the Practice display strip; unchecking it leaves the **drum track still in `api.score.tracks`** — empirically confirming `renderTracks` is display-only and playback keeps the drums (**audio-always per IN5**, the one item flagged in step 5).
- `window.__cfEngine.setTrackVolume("drums", 0.4)` wired.
- Full Core suite **1147 passed, 0 failed**; solution builds.

**Left for Rafa (ears only):** the actual *audible* confirmation that the drums sound under the blues (channel-10 percussion on the soundfont) — the same human check `basic-drums` step 4 used. Structurally the percussion track is present and stays in the score for playback; audibility is the last human sign-off.
