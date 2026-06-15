---
type: done
id: pl_01KV5DBCQS2W7AGFTRG3D3F8AZ-done
title: Done — Score Render Component — Implementation
status: done
created: "2026-06-15T00:00:00.000Z"
version: 7
tags: []
parent_id: pl_01KV5DBCQS2W7AGFTRG3D3F8AZ
requires_load: []
---
# Done — Score Render Component — Implementation

## Step 1 — Add `RenderOptions` record and thread it as an optional param through `IScoreRenderer.Render` (both overloads) — no behavior change when absent.

**RenderOptions seam — done.**

- New `src/ChordFlow.Core/Rendering/RenderOptions.cs`: `sealed record RenderOptions(bool ShowChordNames=false, bool ShowChordDiagrams=false, VoicingStrategy Voicing=ByDifficulty)` with a static `Default`, plus the `VoicingStrategy` enum (only `ByDifficulty` in v1).
- `IScoreRenderer.Render` (both overloads) gained an optional `RenderOptions? options = null` param; `null` ⇒ `RenderOptions.Default` (today's render — backward-compatible).
- `AlphaTexRenderer` threads `opts` from both public `Render` methods down through `RenderBars` → `RenderBar` → `FormatChord`. `FormatChord` now fails loud on any non-`ByDifficulty` strategy (deferred CAGED preference). No emission logic yet — that's step 2; output is byte-identical when options are absent.
- `dotnet build ChordFlow.Core` clean (0 warnings, 0 errors).

## Step 2 — Honor `RenderOptions` in `AlphaTexRenderer`: emit chord names + chord diagrams at chord changes when enabled; carry the voicing strategy into the `VoicingBook` lookup.

**Renderer emission — done.** Chord syntax verified against the alphaTab docs Rafa linked (score-metadata + Chord model).

- New `src/ChordFlow.Core/Domain/ChordSymbol.cs`: `Format(Chord, Key)` → display symbol (`C`/`Am`/`G7`/`Cmaj7`), root spelled via `NoteSpeller` against the key.
- `AlphaTexRenderer`:
  - Header gained `\chordDiagramsInScore true|false` — emitted only when a chord toggle is on (`true` for diagrams, `false` for names-only); omitted entirely otherwise so default output is byte-identical.
  - Replaced the `ref string? currentDuration` threading with a per-render `RenderState` (duration + active chord label + define-once `\chord` set).
  - `{ch "Name"}` attached at each **chord change only** (not every strum); tuplet `{tu N}` now shares the same single brace group (`{ch "…" tu N}`) — tuplet-only output unchanged.
  - `\chord ("Name" f1…f6)` definition built from the realized `Voicing`, frets ordered string 1 (high E) → 6 (low E), `x` for unplayed strings; emitted inline once before first use when `ShowChordDiagrams`.
  - `Key` threaded through `RenderBars`/`RenderBar` (per-section key in the song path) for correct name spelling.
  - Non-`ByDifficulty` voicing strategy fails loud.
- Tests: +5 in `AlphaTexRendererTests` (null==Default, names-once-per-change, names-per-distinct-chord, diagram-defined-once, unimplemented-strategy-throws). **383 pass** (was 378).
- Updated `loom/refs/alphatex-syntax-reference.md` with the verified chord-name/diagram section + renderer-mapping rows.
- ⚠️ Residual inference flagged in the ref: the `x` muted-string token and inline `\chord` placement come from the single documented example — smoke-test in the playground/app when the diagrams toggle is first exercised (step 5 verify).

## Step 3 — Carry an optional `renderOptions` on the render-producing request envelopes (`generate`, `entityPreview`, `loadExercise`); map to `RenderOptions` in the router and pass through the features.

**Bridge renderOptions — done.**

- `WebMessageRouter`: added an optional nested `renderOptions` to the inbound envelope (`InboundRenderOptions(ShowChordNames?, ShowChordDiagrams?, Voicing?)`), mapped to Core `RenderOptions` via `ToRenderOptions` (absent ⇒ `RenderOptions.Default`; `Voicing` parsed case-insensitively, unknown ⇒ `ByDifficulty`). The three render-producing events now carry it: `GenerateRequested(int,string,int,RenderOptions)`, `LoadExerciseRequested(int,RenderOptions)`, `EntityPreviewRequested(string,string,RenderOptions)`.
- `LoadScoreEnvelope.From(exercise, renderer, RenderOptions? = null)` → `renderer.Render(exercise, options)`.
- `ExerciseLibraryHandler.Load(id, RenderOptions? = null)` threads it to the regenerated score.
- `ContentCrudHandler.Preview(entity, dsl, RenderOptions? = null)` → `ScorePreview`/`SongPreview` pass it to the renderer; the voicing diagram path ignores it. All new params optional ⇒ existing callers/tests unaffected.
- `Program.cs` wiring: `TrySendScore(exercise, options)`; boot uses `RenderOptions.Default`; `generate`/`loadExercise`/`entityPreview` handlers forward the parsed options.
- `SwappableRenderer` (Desktop) updated to the new `IScoreRenderer` signatures.
- Tests: +4 in `WebMessageRouterContentTests` (generate parses options, generate without ⇒ Default, entityPreview flags, loadExercise flags); fixed the existing entityPreview test for the new arity. **387 pass.** Full solution builds (only the pre-existing WindowsBase version-conflict warning in Desktop).

## Step 4 — Build `score-render-component.js` (`window.ChordFlowScore`): settings source of truth, `create/load/play/stop/setTempo/setOption/dispose`, `player` mode, `controls` profiles, player- vs content-kind option handling, event + `onNeedsRerender` callbacks.

**ChordFlowScore component — done.**

- New `wwwroot/score-render-component.js` exposing `window.ChordFlowScore.create(container, opts)`:
  - **Single alphaTab settings source of truth** (`buildSettings`) — player settings (soundfont, cursor, highlight, workers, scrollMode) added only when `player:true`, so a lite preview pays no soundfont cost.
  - Handle: `load(tex, {tempo})`, `play()`, `stop()`, `setTempo(bpm)` (playbackSpeed multiplier off the authored tempo), `setOption(name, value)`, `getRenderOptions()`, `getApi()`, `dispose()`.
  - **Option taxonomy:** `PLAYER_KIND` {metronome, countIn} → `api.metronomeVolume`/`api.countInVolume` locally; `CONTENT_KIND` {chordNames, diagrams, voicing} → fire `onNeedsRerender(getRenderOptions())`. `getRenderOptions()` maps to the envelope shape `{showChordNames, showChordDiagrams, voicing}`.
  - **Callbacks:** `onBeat(bar,beat)` (1-based, from `activeBeatsChanged`), `onStateChange(playing)` + `onFinished()` (from `playerStateChanged`, defensive `PlayerState` resolution reused from app.js).
  - **Component owns its control strip** per `controls` profile: `full` = transport (play/stop/tempo) + player toggles + content toggles; `mini` = transport only (player) ; `none`/lite-no-player = render-only. Transport enables on `soundFontLoaded`.
- `index.html`: registered `<script src="score-render-component.js">` (after bridge.js, before chord-diagram/content-crud/app) + added `.cf-controls` / `.cf-toggle` / `.cf-score-surface` styles.
- `node --check` clean. Not yet consumed — steps 5 & 6 retrofit app.js (full) and content-crud preview (lite) onto it.

## Step 5 — Retrofit Practice (`app.js`) onto `ChordFlowScore` in `player:true` / `controls:"full"`; move transport + tempo into the component, remove the bespoke `AlphaTabApi` block, send `renderOptions` on generate/loadExercise.

**Retrofit Practice — done.**

- `app.js` rewritten onto `ChordFlowScore.create($("score-pane"), {player:true, controls:"full", …})`:
  - Removed the bespoke `AlphaTabApi` setup, `wirePlaybackEvents`, `applyTempo`, `setTransportEnabled`, `reflectPlayState`, and the local `PlayerState` (all now in the component).
  - Transport (play/stop/tempo + chord-name/diagram/metronome/count-in toggles) is the component's strip; the builder toolbar (key/rhythm/generate/save/practiced) stays.
  - `loadScore` → `view.load(tex, {tempo})`. Position seam preserved via callbacks: `onBeat`→`beatChanged`, `onFinished`→`playbackFinished`.
  - **renderOptions plumbed:** `sendScoreRequest()` attaches `view.getRenderOptions()` to every `generate`/`loadExercise` and remembers the envelope; `onNeedsRerender` replays it with the new options when a content toggle flips. `selections().tempo` now comes from `view.getTempo()`.
  - Dropped the host-echo `play`/`stop`/`setTempo` handling — the component owns alphaTab transport directly (the C# `PracticeSession` echo was a dormant seam; its position tracking still works via the beat/finished echoes). Dev fallback renders `SAMPLE_TEX` through the component.
- `index.html`: removed the `#transport` toolbar section; `#score-pane` is now the component's container (empty); dropped the dead `#score` CSS rule. Added `getTempo()` to the component.
- `node --check` clean on app.js + component.

## Step 6 — Retrofit the Content-CRUD score preview (`content-crud.js`) onto `ChordFlowScore` in `player:false` / `controls:"mini"`; remove `previewApi`/`renderScore`; send `renderOptions` on `entityPreview`. Voicing fret-box untouched.

**Retrofit Content preview — done.**

- `content-crud.js`: replaced the standalone `previewApi` (`new alphaTab.AlphaTabApi(scoreEl, …)`) with a lazy `scoreView = ChordFlowScore.create(scoreEl, {player:false, controls:"mini"})` — render-only, no soundfont/transport (the no-soundfont behavior is now explicit, not accidental). `renderScore(tex)` → `scoreView.load(tex)`.
- The `diagram` preview branch (voicing → `chord-diagram.js` SVG) is untouched. Header comment updated to reference the shared component.
- Both alphaTab instantiations are now gone — Practice and the Content preview share the one `ChordFlowScore` integration (the original goal: no drift). renderOptions omitted on `entityPreview` (no toggles in the lite preview ⇒ server defaults; the optional contract covers it).
- `node --check` clean.

## Step 7 — Update the architecture + domain-model reference docs in the same unit of work.

**Ref sync — done.** Plan auto-completed (all 7 steps).

- `chordflow-architecture-reference.md`: added `score-render-component.js` to the `wwwroot` file map (§2); documented the shared render component + the `renderOptions` bag on `generate`/`entityPreview`/`loadExercise` in the bridge-protocol section (§5).
- `chordflow-domain-model-reference.md`: updated the `AlphaTexRenderer` + `IScoreRenderer` rows for the optional `RenderOptions`/`RenderState`/chord-name+diagram emission; added a `RenderOptions`/`VoicingStrategy` row (Rendering seam) and a `ChordSymbol` row (harmony).
- (DSL ref untouched — inline voicing deferred.) `alphatex-syntax-reference.md` was updated in step 2.

**Final state:** full solution builds (only the pre-existing Desktop WindowsBase version-conflict warning); **387 Core tests pass**. Both standalone alphaTab instances are gone — Practice and the Content preview share one `ChordFlowScore`.
