---
type: done
id: pl_01KY0T38YXA9XPVE84GT6XHVVQ-done
title: Done — Phase 2 — Rhythm Generator dogfood page
status: done
created: 2026-07-21
version: 2
tags: []
parent_id: pl_01KY0T38YXA9XPVE84GT6XHVVQ
requires_load: []
---
# Done — Phase 2 — Rhythm Generator dogfood page

## Step 1 — Bridge contract for the verb: an inbound `rhythmGenerate` envelope carrying the wire request ({strategy discriminator, per-strategy params as token+args, seed, drum voice}) and the outbound `rhythmGenerated` ({diagram: DrumGrooveDiagram, tex, dsl?}) / `rhythmGenerateError` ({message}) replies. Mirror the drumPreview envelope family.

**`Bridge/RhythmGenerateEnvelope.cs`** — the inbound request payload: `RhythmGenerationRequest(Strategy, Seed, Voice?, Tempo, Family?, Operator?, Behaviour?, BarCount?, Palette?, ContentBars?, SilenceBars?)` + `RhythmOperatorSpec(Kind, Args?)` + `RhythmBehaviourSpec(Kind, Args?)` (token+int-args shape). Placed in Bridge as the peer of `GenerateRequest`/`VoicingDeriveRequest` (event-payload records live with the router). **`Features/Rhythm/RhythmGeneratedEnvelope.cs`** — `RhythmGeneratedEnvelope(Tex, DrumGrooveDiagram Diagram, Grid, Type="rhythmGenerated")` + `RhythmGenerateErrorEnvelope(Message, Type="rhythmGenerateError")`. `Grid` is a plain onset-ASCII debug string.

## Step 2 — Wire→Core params resolver: map the JSON request (strategy + operator/behaviour/family/palette tokens + args) onto the Core GenerationParams / PatternParams / RandomParams discriminated unions. One place that knows the token vocabulary; unknown token → a clean parse error surfaced as rhythmGenerateError.

**`Features/Rhythm/RhythmRequestResolver.cs`** (named RhythmRequestResolver, not the plan's placeholder `RhythmGenerationRequest.cs` — that name is the DTO from step 1). `Resolve(request) → GenerationParams`: strategy → Pattern/Random; family token → RhythmFamily; operator/behaviour token+args → the `BarOperator`/`SequenceBehaviour` unions; palette passthrough. **Fail-loud** `FormatException` on any unknown token or out-of-range count (BarCount 1–4, ContentBars 1–4, SilenceBars 0–4) — so the host's catch maps it to rhythmGenerateError instead of the strategy throwing a raw ArgumentException later.

## Step 3 — RhythmGenerateHandler (Features): resolve request → RhythmGenerator.Generate → OnsetGridToDrumGroove.Project(voice) → DrumGrooveRenderer (percussion tex) + DrumGrooveDiagram.Build (the DrumsR model) in one pass (projections that can't drift). Bad input → rhythmGenerateError. Reuses existing renderers (no new alphaTex code, C3).

**`Features/Rhythm/RhythmGenerateHandler.cs`** — `Generate(request)`: resolve → `RhythmGenerator.Generate` → `OnsetGridToDrumGroove.Project(grid, voice)` → `DrumGrooveRenderer.Render(groove, tempo)` (percussion tex) + `DrumGrooveDiagram.Build(groove)` + an inline onset-ASCII grid string — all from the one generated grid (can't drift). Voice resolved via `DrumVoices.TryParse` then enum name, default `HiHatClosed`; unknown → FormatException. Reuses the existing renderer (req C3 — no new alphaTex code). Stateless/pure.

## Step 4 — Handler unit test: a valid Pattern request and a valid Random request each return a non-empty tex + a DrumGrooveDiagram whose hit ticks match the generated grid's onsets; an invalid request (bad token / out-of-range count) returns rhythmGenerateError, not a throw.

**`tests/…/Rhythm/Generation/RhythmGenerateHandlerTests.cs`** — 7 tests: Pattern Uniform returns tex + diagram hits `[0,48,96,144]` + grid text `"x x x x"` + default HiHatClosed; honours a chosen voice (SD→Snare); Mask uses operator args; Random all-quarters fills the bar; unknown operator / unknown strategy / out-of-range BarCount each throw FormatException. Full suite **1200 passed, 0 failed**.

## Step 5 — Router wiring: WebMessageRouter parses the inbound rhythmGenerate envelope and raises a typed event; the host wires it to RhythmGenerateHandler and posts the reply. Follows the existing drumPreview inbound path.

**`Bridge/WebMessageRouter.cs`** — added `RhythmGenerateRequested` event, a `rhythmGenerate` dispatch case, and a nested `RhythmGenerationRequest? RhythmGenerate` field on `InboundEnvelope` (STJ binds the nested strategy/operator/behaviour records by constructor with the existing Web options). **`Desktop/Program.cs`** — instantiated `RhythmGenerateHandler`, added `using ChordFlow.Features.Rhythm;`, and subscribed: `try { bridge.Send(rhythmGenerate.Generate(request)); } catch (FormatException or ArgumentException) { bridge.Send(new RhythmGenerateErrorEnvelope(…)); }`. Full solution builds (0 errors; only the pre-existing WindowsBase/WebView2 warning).

## Step 6 — Rhythm Generator JS page: strategy selector; Pattern controls (family / operator / behaviour / barCount) and Random controls (value palette / contentBars / silenceBars); seed field + reroll. On change, issue rhythmGenerate; render the returned DrumGrooveDiagram on a reused DrumsR (drums-render-component.js) and play the tex through the shared playback engine. Show the raw dsl for debug.

**`wwwroot/rhythm-generator.js`** — `window.ChordFlowRhythmGen` with `show()`. Builds the controls in JS: strategy (pattern/random); pattern group (family, operator, contextual operator Arg / mask-beats text, behaviour, contextual restBar content/rest, barCount); random group (palette checkboxes 4/8/16, contentBars, silenceBars); common (voice, tempo, seed, Reroll, Generate). `sync()` shows only the relevant contextual fields. `buildRequest()` assembles the `rhythmGenerate` payload (operator/behaviour as `{kind, args}`). `onHostMessage` handles `rhythmGenerated` (DrumsR render + onset-ASCII gridText + ScoreR `load(tex)`) and `rhythmGenerateError`; `ensureScore()` reuses ScoreR and drives the DrumsR marker off the engine `position` clock. Ephemeral — no save/library (EX1). `node --check` clean.

## Step 7 — Count + emphasis overlay on DrumsR: print 1 e & a beat-position labels under the grid (from subdivision/beatsPerBar) and highlight the downbeats / trained beat. Display-only — no change to the rhythm model or DSL (C5). Add as an opt-in flag so the Drums page is unaffected.

**`wwwroot/drums-render-component.js`** — added an **opt-in `countLabels`** create option (default off, so the Drums page is unchanged). Added a `COUNT_H` bottom band; refactored the height/gridline math to a `laneBottom` so the grid ends above the label band; draws the **`1 e & a`** counting row per beat with the downbeat number bolded and the `&` emphasized. Display-only — no rhythm-model/DSL change (req C5).

## Step 8 — Nav entry + lazy mount: add the Rhythm Generator top-level view to index.html (the views/onShow pattern used by Scales/CAGED), lazily create the page on first tab show, and register its envelope types in bridge.js fan-out.

**`wwwroot/index.html`** — added the `navRhythmGen` nav button, the `rhythm-generator-view` container (`rgControls`/`rgError`/`rgGrid`/`rgGridText`/`rgScore`), and the `rhythm-generator.js` script include (after drums.js, so DrumsR/ScoreR are already loaded). **`wwwroot/app.js`** — added the `rhythmGen` entry to the `views` registry (nav auto-wires via the generic `Object.entries(views)` click loop). **No bridge.js change** — the fan-out is generic (`onReceive` hands every message to every view), so registering the view's receiver in its `init()` is all that's needed (deviation from the plan's listed bridge.js file; recorded here).

## Step 9 — Update the architecture reference with the new rhythmGenerate/rhythmGenerated verb (its wire shape + one-pass handler) and the Rhythm Generator page + DrumsR count-overlay reuse (CLAUDE-LOCAL architecture ref-sync, same unit of work).

**`chordflow-architecture-reference.md`** — added `Rhythm (rhythmGenerate)` to the Features list and a new **"Rhythm generation — the generation-engine dogfood page"** subsection: the `rhythmGenerate` verb + `RhythmGenerateHandler` (resolve → generate → single-voice DrumGroove projection → percussion tex reusing `DrumGrooveRenderer` + `DrumGrooveDiagram` + onset-ASCII; fail-loud `rhythmGenerateError`; ephemeral) and the Rhythm Generator page (raw param controls → reused DrumsR with the opt-in `countLabels` overlay + ScoreR playback; presets/reference-pulse deferred). CLAUDE-LOCAL architecture ref-sync. Full solution builds (0 errors).
