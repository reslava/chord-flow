---
type: done
id: pl_01KY1SNH2YWBR1JPFYADHBCSDP-done
title: Done — Phase 2 polish — Random rests + Beat-1 reference + Loop
status: done
created: 2026-07-21
version: 2
tags: []
parent_id: pl_01KY1SNH2YWBR1JPFYADHBCSDP
requires_load: []
---
# Done — Phase 2 polish — Random rests + Beat-1 reference + Loop

## Step 1 — Random strategy rests: add `RestProbability` (0..1) to RandomParams; in the walk, roll onset-vs-rest per step (a rest advances the drawn value's duration with no onset, so it reads as a quarter/eighth/16th rest); remove the forced beat-1 onset (cell 0 may now rest). Unit tests: rest density trends with probability (0 = solid fill, 1 = empty), determinism with a seed, and the all-rest edge.

**`RandomParams.cs`** — added `double RestProbability = 0.0` (last param, so existing call sites default to solid fill, no churn). **`RandomStrategy.cs`** — `FillBar` now rolls onset-vs-rest each step (`rng.NextDouble() >= restProbability` → onset, else a rest that just advances the drawn value's duration); removed the forced beat-1 onset (cell 0 may rest). Range-validated (0..1). **Tests** (`RhythmGeneratorTests`): restProb 1 → empty bar, restProb 0 → solid fill, monotonic thinning `Count(0) > 0 >= Count(0.5) >= Count(1)=0`, determinism with rests, out-of-range throw.

## Step 2 — Wire rests + Beat-1 reference: add `RestProbability` and `ReferencePulse` ("off"/"beat1") to RhythmGenerationRequest; RhythmRequestResolver maps restProbability onto RandomParams; RhythmGenerateHandler, when referencePulse==beat1, adds a NON-generated reference lane (a hit on beat 1 of each bar) in a distinct voice (Kick, or HiHatPedal if the generated voice is Kick) to the preview DrumGroove — so it shows as its own DrumsR row and plays. Update the handler test (rest density in the reply; the ref row present with hits only on beat 1).

**`RhythmGenerateEnvelope.cs`** — request gains `double? RestProbability` + `string? ReferencePulse` (defaulted, so old shapes deserialize). **`RhythmRequestResolver.cs`** — maps `restProbability` onto RandomParams with fail-loud 0..1 range check. **`RhythmGenerateHandler.cs`** — when `referencePulse=="beat1"`, `WithBeat1Reference` appends a **non-generated** reference lane (a `RhythmEvent.Hit(0, beatTicks)` per bar) in a distinct voice (**Kick**, or **HiHatPedal** if the generated voice is Kick) to the preview DrumGroove; the onset-ASCII still reflects only the generated grid. **Tests**: restProb 1 → no generated hits; beat1 → 2 lanes, the ref lane is Kick and hits only `(0,0)`/`(1,0)`; off → single lane. Core **1208 pass**.

## Step 3 — Loop support in the shared transport: playback-component.js gains `setLooping(on)` (sets `api.isLooping`), re-asserted per loaded score; PlayerControlsR gets a Loop toggle button reflecting/So-toggling it, defaulting ON (applied on ready). ScoreR passes a `loop` create-opt (default true) through to the engine. Default-on affects every render surface (per the request); per-page override stays possible.

**`playback-component.js`** — added `loopOn` state, `api.isLooping = loopOn` re-asserted in the `scoreLoaded` handler (survives synth channel rebuilds), and `setLooping(state)` on the engine handle. **`player-controls-component.js`** — a **Loop** checkbox (shown by default via `opts.loop !== false`), **default ON** (`opts.loopDefault !== false`), toggling `engine.setLooping`; the default is asserted in the `ready` handler once the player exists. Default-on applies to every PlayerControlsR consumer (Practice/Content/Drums too), per the request. JS `node --check` clean.

## Step 4 — Rhythm Generator page: add a Rest % slider to the Random controls (sends `restProbability`); always send `referencePulse:"beat1"` so the visible reference row appears; confirm the Loop toggle (from ScoreR, default on) shows on the page. The beat-1 ref row renders automatically as an extra DrumsR lane.

**`rhythm-generator.js`** — added a **Rest %** range slider (0–80, live value label) to the Random group; `buildRequest` sends `restProbability = restPct/100` (random) and always `referencePulse: "beat1"` (common). The Loop toggle and the Beat-1 reference row appear automatically (ScoreR's PlayerControlsR default-on loop; the ref lane is just another DrumsR row). JS `node --check` clean.

## Step 5 — Ref-doc sync (CLAUDE-LOCAL): domain-model reference gets RandomParams.RestProbability; architecture reference gets the Beat-1 reference lane in RhythmGenerateHandler, the shared Loop toggle on ScoreR/PlayerControlsR, and the Rest % control.

Ref-doc sync: **domain-model ref** — `RandomParams` now shows `RestProbability=0` + the rests note. **architecture ref** — the `rhythmGenerate` handler bullet gains the Beat-1 reference lane; the Rhythm Generator page bullet gains the Loop toggle (default on), the Rest % slider, and the Beat-1 reference (and drops `reference pulse` from "later phase", keeping only `Quarters` + presets deferred); the PlayerControlsR wwwroot descriptor gains `loop`.

## Step 6 — CDP verification (run once the app is relaunched with the debug port): the harness asserts rests appear (grid sparser than solid at a mid probability), the Beat-1 reference row is present with hits only on beat 1, and loop playback repeats continuously. Report results; no code unless a defect surfaces.

CDP verification against the relaunched app (`verify-plan003.mjs`) — all green:\n- **Beat-1 reference row**: `laneRows: [\"HH\",\"BD\"]` — the generated HiHatClosed voice + a distinct Kick reference row (the non-generated beat-1 pulse).\n- **Loop toggle**: present and **checked** (default on).\n- **Rest % slider**: present in the controls.\n- **Rest thins onsets**: random-mode hit circles = **14 at rest 0%** vs **3 at rest 80%** (rest % reduces onsets as designed).\n\nHarness + `cdp.mjs` driver preserved in the session scratchpad.
