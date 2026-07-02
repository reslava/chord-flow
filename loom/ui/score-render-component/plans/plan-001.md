---
type: plan
id: pl_01KV5DBCQS2W7AGFTRG3D3F8AZ
title: Score Render Component — Implementation
status: done
created: 2026-06-15
updated: 2026-06-15
version: 1
design_version: 5
req_version: 1
tags: []
parent_id: de_01KV5CZF197BYKYJGS4W3NTQDY
requires_load: []
target_version: 0.1.0
actual_release: 0.5.0
steps:
  - id: renderoptions-seam
    order: 1
    status: done
    description: Add `RenderOptions` record and thread it as an optional param through `IScoreRenderer.Render` (both overloads) — no behavior change when absent.
    files_touched: [src/ChordFlow.Core/Rendering/RenderOptions.cs, src/ChordFlow.Core/Rendering/IScoreRenderer.cs, src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs]
    blocked_by: []
    satisfies: [IN9, C3]
  - id: renderer-emission
    order: 2
    status: done
    description: "Honor `RenderOptions` in `AlphaTexRenderer`: emit chord names + chord diagrams at chord changes when enabled; carry the voicing strategy into the `VoicingBook` lookup."
    files_touched: [src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs, src/ChordFlow.Core/Domain/Voicings/VoicingBook.cs, tests/ChordFlow.Core.Tests/AlphaTexRendererTests.cs, loom/refs/alphatex-syntax-reference.md]
    blocked_by: [1]
    satisfies: [IN10, IN11]
  - id: bridge-renderoptions
    order: 3
    status: done
    description: Carry an optional `renderOptions` on the render-producing request envelopes (`generate`, `entityPreview`, `loadExercise`); map to `RenderOptions` in the router and pass through the features.
    files_touched: [src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Core/Features/GenerateExercise/GenerateExerciseHandler.cs, src/ChordFlow.Core/Features/ExerciseLibrary/ExerciseLibraryHandler.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs, tests/ChordFlow.Core.Tests/WebMessageRouterContentTests.cs]
    blocked_by: [1]
    satisfies: [IN12]
  - id: chordflowscore-component
    order: 4
    status: done
    description: "Build `score-render-component.js` (`window.ChordFlowScore`): settings source of truth, `create/load/play/stop/setTempo/setOption/dispose`, `player` mode, `controls` profiles, player- vs content-kind option handling, event + `onNeedsRerender` callbacks."
    files_touched: [src/ChordFlow.Desktop/wwwroot/score-render-component.js, src/ChordFlow.Desktop/wwwroot/index.html]
    blocked_by: []
    satisfies: [IN1, IN2, IN3, IN4, IN5, IN6, IN7, IN8]
  - id: retrofit-practice
    order: 5
    status: done
    description: "Retrofit Practice (`app.js`) onto `ChordFlowScore` in `player:true` / `controls:\"full\"`; move transport + tempo into the component, remove the bespoke `AlphaTabApi` block, send `renderOptions` on generate/loadExercise."
    files_touched: [src/ChordFlow.Desktop/wwwroot/app.js, src/ChordFlow.Desktop/wwwroot/index.html]
    blocked_by: [3, 4]
    satisfies: [IN13]
  - id: retrofit-content-preview
    order: 6
    status: done
    description: "Retrofit the Content-CRUD score preview (`content-crud.js`) onto `ChordFlowScore` in `player:false` / `controls:\"mini\"`; remove `previewApi`/`renderScore`; send `renderOptions` on `entityPreview`. Voicing fret-box untouched."
    files_touched: [src/ChordFlow.Desktop/wwwroot/content-crud.js]
    blocked_by: [3, 4]
    satisfies: [IN14]
  - id: ref-sync
    order: 7
    status: done
    description: Update the architecture + domain-model reference docs in the same unit of work.
    files_touched: [loom/refs/chordflow-architecture-reference.md, loom/refs/chordflow-domain-model-reference.md]
    blocked_by: [2, 3, 4, 5, 6]
    satisfies: [IN15]
---
# Score Render Component — Implementation

## Goal

Build the shared `ChordFlowScore` component (alphaTex → alphaTab notation + optional playback with a declarative option set) and the C# `RenderOptions` seam it needs, then retrofit Practice and the Content-CRUD score preview onto it — removing the two drifted `AlphaTabApi` instances. Built C#-seam-first (RenderOptions → renderer emission → bridge envelopes), then the JS component, then the two retrofits, then the ref updates. Content-kind toggles (chord names, diagrams, voicing strategy) re-render through C#; player-kind toggles (metronome, count-in) stay client-side. Inline voicing DSL and CAGED-shape preference are out of scope (deferred threads).

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add `RenderOptions` record and thread it as an optional param through `IScoreRenderer.Render` (both overloads) — no behavior change when absent. | src/ChordFlow.Core/Rendering/RenderOptions.cs, src/ChordFlow.Core/Rendering/IScoreRenderer.cs, src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs | — | IN9, C3 |
| ✅ | 2 | Honor `RenderOptions` in `AlphaTexRenderer`: emit chord names + chord diagrams at chord changes when enabled; carry the voicing strategy into the `VoicingBook` lookup. | src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs, src/ChordFlow.Core/Domain/Voicings/VoicingBook.cs, tests/ChordFlow.Core.Tests/AlphaTexRendererTests.cs, loom/refs/alphatex-syntax-reference.md | 1 | IN10, IN11 |
| ✅ | 3 | Carry an optional `renderOptions` on the render-producing request envelopes (`generate`, `entityPreview`, `loadExercise`); map to `RenderOptions` in the router and pass through the features. | src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Core/Features/GenerateExercise/GenerateExerciseHandler.cs, src/ChordFlow.Core/Features/ExerciseLibrary/ExerciseLibraryHandler.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs, tests/ChordFlow.Core.Tests/WebMessageRouterContentTests.cs | 1 | IN12 |
| ✅ | 4 | Build `score-render-component.js` (`window.ChordFlowScore`): settings source of truth, `create/load/play/stop/setTempo/setOption/dispose`, `player` mode, `controls` profiles, player- vs content-kind option handling, event + `onNeedsRerender` callbacks. | src/ChordFlow.Desktop/wwwroot/score-render-component.js, src/ChordFlow.Desktop/wwwroot/index.html | — | IN1, IN2, IN3, IN4, IN5, IN6, IN7, IN8 |
| ✅ | 5 | Retrofit Practice (`app.js`) onto `ChordFlowScore` in `player:true` / `controls:"full"`; move transport + tempo into the component, remove the bespoke `AlphaTabApi` block, send `renderOptions` on generate/loadExercise. | src/ChordFlow.Desktop/wwwroot/app.js, src/ChordFlow.Desktop/wwwroot/index.html | 3, 4 | IN13 |
| ✅ | 6 | Retrofit the Content-CRUD score preview (`content-crud.js`) onto `ChordFlowScore` in `player:false` / `controls:"mini"`; remove `previewApi`/`renderScore`; send `renderOptions` on `entityPreview`. Voicing fret-box untouched. | src/ChordFlow.Desktop/wwwroot/content-crud.js | 3, 4 | IN14 |
| ✅ | 7 | Update the architecture + domain-model reference docs in the same unit of work. | loom/refs/chordflow-architecture-reference.md, loom/refs/chordflow-domain-model-reference.md | 2, 3, 4, 5, 6 | IN15 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:renderoptions-seam -->
### Step 1 — RenderOptions seam

New `public sealed record RenderOptions(bool ShowChordNames=false, bool ShowChordDiagrams=false, VoicingStrategy Voicing=VoicingStrategy.ByDifficulty)` plus a `VoicingStrategy` enum (only `ByDifficulty` for v1). Add an optional `RenderOptions? options = null` to both `Render` overloads; default-coalesce to `RenderOptions` defaults so existing callers and tests are unchanged.

<!-- step:renderer-emission -->
### Step 2 — Renderer emission

Verify the exact chord-name / chord-diagram directives against `alphatex-syntax-reference.md` first. Gate emission on `ShowChordNames` / `ShowChordDiagrams`. Pass `options.Voicing` to `VoicingBook.Lookup` (v1: `ByDifficulty` = today's difficulty-keyed selection — a real, working toggle). Tests: default options reproduce current output byte-for-byte; flags add the expected tokens.

<!-- step:bridge-renderoptions -->
### Step 3 — Bridge renderOptions

Parse an optional `renderOptions` object on the inbound `generate` / `entityPreview` / `loadExercise` envelopes → `RenderOptions` (absent ⇒ defaults). Thread it into `GenerateExercise`, `ExerciseLibrary` reload, and `ContentCrudHandler` preview builders so the rendered tex reflects the toggles. Confirm exact feature/handler file names while implementing.

<!-- step:chordflowscore-component -->
### Step 4 — ChordFlowScore component

One module owning all alphaTab settings. `player:false` skips soundfont + transport. Component renders its own control strip per `controls` profile. Player-kind `setOption` → alphaTab API locally; content-kind `setOption` → fire `onNeedsRerender(renderOptions)`. Wire alphaTab events to `onBeat`/`onStateChange`/`onFinished`. Add the script tag + a container in index.html.

<!-- step:retrofit-practice -->
### Step 5 — Retrofit Practice

Replace the `AlphaTabApi` init + `wirePlaybackEvents` + `applyTempo` with a `ChordFlowScore` handle. `loadScore` handler → `view.load(tex, {tempo})`. Callbacks post the existing `beatChanged` / `playbackFinished` envelopes; transport buttons drive the component. Simplify the now-duplicated practice controls in index.html.

<!-- step:retrofit-content-preview -->
### Step 6 — Retrofit Content preview

`renderPreview`'s score branch → `view.load(msg.tex)`. Delete `previewApi` + `renderScore`. The `diagram` branch (voicing → `chord-diagram.js`) stays as-is. Make the no-soundfont preview behavior explicit via `player:false`.

<!-- step:ref-sync -->
### Step 7 — Ref sync

Architecture ref: `score-render-component.js` as the shared JS render/transport layer feeding alphaTab + `renderOptions` on the `generate`/`entityPreview`/`loadExercise` envelopes. Domain-model ref: `RenderOptions` on `IScoreRenderer.Render` and the render-time voicing strategy.
