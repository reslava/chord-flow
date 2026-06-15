---
type: done
id: pl_01KV6HBBY93VRRX8QP7YPGQ3SW-done
title: Done — Exercise workbench — consumption UI over the canonical Exercise
status: done
created: "2026-06-15T00:00:00.000Z"
version: 9
tags: []
parent_id: pl_01KV6HBBY93VRRX8QP7YPGQ3SW
requires_load: []
---
# Done — Exercise workbench — consumption UI over the canonical Exercise

## Step 1 — Generate slice resolves content references from the stores (delete the hard-wired blues)

**Core — reference resolution for generate (and library load). Build clean; 399 Core tests pass (was 387).**

- **New `Features/ExerciseRefs.cs`** — the shared "ids → Exercise pieces" seam. `ResolveHarmony(entity, id, liftKey, db)` (explicit discriminator, generate path: `"song"`→`SongStore.Find`, `"progression"`→`ProgressionStore.Find`+`Song.OfProgression`), `ResolveHarmonyById(id, liftKey, db)` (no discriminator, load path: try Song then Progression), `ResolvePattern`/`ResolveOptionalPattern`. All **fail loud** (clear `InvalidOperationException`) on a missing row, so a dangling ref surfaces as a status, not a silent wrong render.
- **`Persistence/SongStore.cs`** — added `Find(id) → Song?` (resolves the top tier, strips the catalog header, parses the arrangement grammar). `RhythmPatternStore.Find`/`ProgressionStore.Find` already existed — no change.
- **`Features/GenerateExercise/GenerateExercise.cs`** — `Build`/`Generate` rewritten from `(keyPitchClass, rhythmId, tempo)` hard-wired to `SeedData.TwelveBarBlues` to `(harmonyEntity, harmonyId, compingPatternId, leadPatternId?, keyPitchClass?, tempo, difficulty, feel)` resolved via `ExerciseRefs`. Two `Build` overloads: a `db`-taking one (internal/test) and a host-convenience one that opens a short-lived context. A bare progression with no chosen key anchors at C.
- **`Features/ExerciseLibrary/ExerciseLibrary.cs`** — `ToExercise` was *also* hard-wired to the seed blues (its own comment deferred real resolution to this thread). Now resolves `SongId`/`CompingPatternId`/`LeadPatternId` via `ExerciseRefs.ResolveHarmonyById` + `ResolvePattern`. `Save` unchanged (already reference-shaped).
- **Tests** — new `GenerateExerciseTests` (progression-lift, stored-Song + optional lead + no-override, fail-loud on missing ref); `ExerciseLibraryTests.NewHandler` now `DefaultPack.ImportInto(db)` after migrate so the round-trip resolves real rows.

**⚠️ Design decision flagged for review — load-path harmony discriminator.** `ExerciseEntity.SongId` carries no song-vs-progression kind. Rather than add a schema column (would brush `EX7`'s "no new persistence capability"), the load path tries the Song store first then falls back to a lifted Progression (`ResolveHarmonyById`). Unambiguous in practice (ids don't collide across the two tables); fully reversible under `C6` (exercises are disposable). Chose this over a schema change deliberately.

**Plan-accuracy note:** Step 1 grew beyond its listed files — it necessarily includes `ExerciseLibrary.cs` (load was hard-wired too) and a new `ExerciseRefs.cs`; `RhythmPatternStore.cs` needed no change.

## Step 2 — Widen the generate bridge envelope + router verb + Program.cs wiring

**Core — generate envelope carries content references. Build clean; 399 Core tests pass.**

- **`Bridge/WebMessageRouter.cs`** — new `public record GenerateRequest(HarmonyEntity, HarmonyId, CompingPatternId, LeadPatternId?, KeyPitchClass?, Tempo, Difficulty, Feel)` (carries parsed Domain enums — Bridge is same-assembly as Domain). `GenerateRequested` event changed `Action<int,string,int,RenderOptions>` → `Action<GenerateRequest, RenderOptions>`. `InboundEnvelope` widened with `HarmonyEntity`/`HarmonyId`/`CompingPatternId`/`LeadPatternId`/`Difficulty`/`Feel`; dropped the now-unused `RhythmId`. New generic `ParseEnum<T>` (case-insensitive, fallback) parses Difficulty/Feel like the existing `ParseVoicing`. The `generate` dispatch builds a `GenerateRequest` (defaults: harmony `progression`, comping `beat_1_3`, tempo 80, Beginner/Straight).
- **`Desktop/Program.cs`** — boot now `generate.Build(harmonyEntity:"progression", harmonyId:"12bar_blues", compingPatternId:"beat_1_3", …, key 10/Bb, Beginner, Straight)`; `GenerateRequested` handler destructures the `GenerateRequest` into `generate.Build(...)` wrapped in a try/catch that surfaces a missing-reference throw as a `StatusEnvelope` (no silent no-op). `renderOptions` plumbing unchanged.
- **Tests** — `WebMessageRouterContentTests`: added `Generate_ParsesReferencesAndParams` + `Generate_DefaultsParamsAndDiscriminator_WhenAbsent`; updated the two `renderOptions` generate tests to the 2-arg event + new envelope shape.

Dependency direction Desktop → Core unchanged (C5). Steps 1+2 implemented together since the solution can't compile across the signature change otherwise.

## Step 3 — Definition pickers populated from the content stores (Song/Progression + Comping + optional Lead)

**FE — definition selection. `node --check` clean.**

- **`index.html`** — builder toolbar's hard-wired Key+Rhythm replaced by **Harmony / Comping / Lead** selects (+ Key/Difficulty/Feel from steps 4).
- **`app.js`** — on Practice init (and on returning from the Content view, since the user may have authored content) requests `entityList` for `progression` / `song` / `rhythm` and caches them in `catalog`. `rebuildHarmonyPicker` renders `<optgroup>` **Songs** + **Progressions**, each option value `"<entity>:<id>"` so generate sends the right discriminator (no Progression-vs-Song branch in the model — C4). `rebuildRhythmPickers` fills Comping + a Lead picker with a leading `(none)` option. Rides the existing `entity*` bridge — no new envelope (C2).

## Step 4 — Params surface — Difficulty + Feel selects alongside Key (saved defaults)

**FE — exercise params. `node --check` clean.**

- **`index.html`** — Difficulty + Feel selects added to the builder (Key already present; Tempo lives in the component transport).
- **`app.js`** — `populateStaticPickers` enumerates `DIFFICULTIES` (Beginner/Intermediate/Advanced) and `FEELS` (Straight/Swing/Shuffle/Triplet) from the stable Domain enums (no extra bridge for these). `fillSelect` preserves the current value across rebuilds, giving saved-default behavior. Definition (references) stays distinct from params (values) — C3.

## Step 5 — Generate wiring — emit the new reference+params envelope

**FE — Generate. `node --check` clean.**

- **`app.js`** — `selections()` splits the harmony option's `"entity:id"` value and assembles the widened `generate` envelope: `{ harmonyEntity, harmonyId, compingPatternId, leadPatternId|null, keyPitchClass, tempo (from the component transport), difficulty, feel }`. `sendScoreRequest` attaches the component's `renderOptions` and remembers the request for `onNeedsRerender` replay. The boot/replay seed is `BOOT_REQUEST` (the blues defaults the host renders on ready), so a content-toggle flip before the catalog loads still replays a valid request. Dev (no-bridge) fallback still renders `SAMPLE_TEX`.

## Step 6 — Save + saved-exercise library rewired to the new ExerciseSummary

**FE — Save & library. `node --check` clean.**

- **`app.js`** — `renderLibrary`/`libraryLabel` rewired from the dropped `ex.key`/`ex.rhythmId` to the new `ExerciseSummary` shape (`songId`/`compingPatternId`/`leadPatternId`/`keyOverride`/`tempo`/`difficulty`/`feel`/`practicedCount`). Ids resolve to **display names from the cached catalog** — `harmonyName` tries the Song list then the Progression list (matching the load-path song-then-progression fallback), `rhythmName` for comping/lead, `keyLabel` title-cases the `\ks` token. The library re-labels itself when the catalog arrives/refreshes (`lastLibrary`). Save still posts `{type:"save"}` (host persists the on-screen definition). No data migration — existing rows are disposable (C6); the entity already changed shape in the capstone.

## Step 7 — Player settings — metronome/count-in toggles + per-track volumes

**FE — player settings. `node --check` clean.**

- **`score-render-component.js`** — metronome + count-in toggles already shipped in the `controls:"full"` strip (PLAYER_KIND → `api.metronomeVolume`/`countInVolume`). Added **per-track volume** sliders: a `trackVolumes {rhythm, lead}` state, `applyTrackVolume(which)` mapping rhythm→track 0 / lead→track 1 via `api.changeTrackVolume([track], v)` (guarded for the minified bundle; lead is a no-op on a single-track score), a `handle.setTrackVolume`, two `volumeSlider`s (0..1) appended to the full player strip, and a re-assert of both volumes on `scoreLoaded` (alphaTab rebuilds tracks per load). Player-kind only — never enters `getRenderOptions` (no re-render). Shared component, so the sliders also appear in the Content preview's full strip (harmless).

## Step 8 — Sync the architecture reference for the widened generate envelope

**Ref sync — architecture. Done.**

- **`loom/refs/chordflow-architecture-reference.md`** (`loom_patch_doc`): §5 bridge-protocol now documents the widened `generate` envelope (harmony discriminator + `harmonyId`, `compingPatternId`, optional `leadPatternId`, `key`/`tempo`/`difficulty`/`feel`) and the shared `ExerciseRefs` resolver (incl. the load-path song-then-progression fallback) + the note that the Practice pickers ride the existing `entityList`. §6 data-flow updated from "UI picks key/rhythm/tempo" to the references+params flow through `ExerciseRefs`. Domain-model + DSL refs untouched (no domain or DSL grammar change this thread).

## Step 9 — Verify end-to-end: select → Generate → two-track play → Save → reload

**Verify — machine checks green; live GUI click-through pending a desktop session.**

Automated (done by me):
- Full solution **builds clean** (only the pre-existing Desktop WindowsBase warning).
- **399 Core tests pass** (was 387) — incl. the new `GenerateExerciseTests` (progression-lift / stored-Song+lead / fail-loud) and the round-trip `ExerciseLibraryTests` whose two-track assertion (`\track "Lead"`) covers IN4's render correctness; `WebMessageRouterContentTests` covers the new `generate` envelope parse.
- `node --check` clean on `app.js` + `score-render-component.js`.

**Not done by me — live desktop walkthrough.** The IN3/IN4/IN7 end-to-end click-through (pick a Song-with-Lead → Generate → see the two-track staff play with the synced cursor; bare Progression → single track; Save → human-readable label → reload; Practice⇄Content toggle) runs in the WinForms+WebView2 shell, which needs a display this session doesn't have. Recommended as a final manual smoke test (or via the `run` skill on the user's machine). The render correctness it exercises is otherwise covered by the build + the two-track renderer/library tests; the new surface is wiring over already-shipped components.
