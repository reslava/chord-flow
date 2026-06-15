---
type: design
id: de_01KV5CZF197BYKYJGS4W3NTQDY
title: Score Render Component
status: done
created: "2026-06-15T00:00:00.000Z"
updated: 2026-06-15
version: 5
tags: []
parent_id: null
requires_load: []
---
# Score Render Component

A reusable JS component that turns an **alphaTex string into rendered notation + optional playback**, with a declarative option set, consumed by every screen that shows a score (Practice, Content-CRUD preview, future Exercise-Workbench). It centralizes the **alphaTex → alphaTab** half of the pipeline; alphaTex *generation* stays in C# `AlphaTexRenderer` (the exporter seam is untouched).

> Origin discussion: `chats/score-render-component-chat-001.md`.

---

## 1. Why

Today there are **two independent alphaTab instances with drifted settings**:

- `wwwroot/app.js:331` — full player: soundfont, cursor, element highlighting, `useWorkers`, `scrollMode`, transport wiring, playback→bridge events.
- `wwwroot/content-crud.js:298` — render-only preview: just `fontDirectory`. It dodges the soundfont cost only *by accident* (it never sets a player).

`exercise-workbench` would be the third consumer. Three divergent copies of "new alphaTab + settings + transport" is the smell. One component removes the drift and makes the on/off render options (count-in, metronome, chord names, chord diagrams, …) work **identically everywhere**.

**Terminology guard:** "DSL → alphaTex" is *already* centralized in C# `AlphaTexRenderer` (the only alphaTex-aware code — architecture rule). This component centralizes the **JS display + transport** layer. It must **never** build alphaTex in JS, or the exporter seam breaks.

---

## 2. The option taxonomy (the load-bearing distinction)

Options split into two kinds by **where the work happens**:

| Kind | Options | Mechanism | Round-trip? |
|------|---------|-----------|-------------|
| **Player-kind** | metronome on/off + volume, count-in, cursor, playback speed | alphaTab JS API (`api.metronomeVolume`, `api.countInVolume`, …) | No — applied locally, instant |
| **Content-kind** | show chord names over staff, show chord diagrams on top, voicing strategy | alphaTab can only display what's in the tex → requires C# to emit **different alphaTex** | Yes — re-request from C# |

This is decided (Q1): content-kind toggles go through a **C# re-render**, keeping a single alphaTex authority. The component holds toggle state; flipping a player-kind option applies locally, flipping a content-kind option fires a `onNeedsRerender(renderOptions)` callback the consumer turns into a fresh render request.

---

## 3. Component contract (`wwwroot/score-render-component.js`)

Exposes `window.ChordFlowScore`:

```js
const view = ChordFlowScore.create(containerEl, {
  player:   true,          // false = lite render-only (no soundfont, no transport) — fast preview
  controls: "full",        // "full" (transport + toggles + metronome) | "mini" (play [+ metronome]) | "none"
  options: {               // initial option state
    metronome: false, countIn: false,        // player-kind
    chordNames: true, diagrams: false,        // content-kind
    voicing: "byDifficulty",                  // content-kind (see §5)
  },
  onBeat:        (bar, beat) => {},   // active-beat changed (1-based)
  onStateChange: (playing)   => {},   // play/pause transitions
  onFinished:    ()          => {},   // playback ended/stopped
  onNeedsRerender: (renderOptions) => {},  // a content-kind toggle flipped → consumer re-requests from C#
});

view.load(tex, { tempo });   // render an alphaTex string (sets base tempo)
view.play();                 // play/pause
view.stop();
view.setTempo(bpm);          // playbackSpeed = bpm / baseTempo (no re-render)
view.setOption(name, value); // player-kind → applied locally; content-kind → fires onNeedsRerender
view.dispose();              // tear down the alphaTab instance
```

- **Single source of truth** for alphaTab settings (`fontDirectory`, `useWorkers`, `soundFont` path, `scrollMode`, cursor/highlight flags) lives *inside* the component — no more per-consumer drift.
- The component **owns its control strip** (transport buttons, toggle checkboxes, metronome slider) rendered into `containerEl` per the `controls` profile, so chrome is consistent across screens. Consumers stop hand-rolling Play/Stop/Tempo markup.
- `player:false` skips soundfont load and the player entirely — render-only, cheap (makes the CRUD preview's current accidental behavior explicit).

---

## 4. Consumers & retrofit

**Practice (`app.js`)** — `create(scoreEl, {player:true, controls:"full", …})`.
- `loadScore` envelope handler → `view.load(tex, {tempo})`.
- Transport buttons + tempo input move *into* the component; `index.html` practice controls simplify accordingly. In host mode the callbacks post the existing bridge envelopes (`play`/`stop`/`setTempo` are still echoed by the host; `onBeat`→`beatChanged`, `onFinished`→`playbackFinished`).
- The bespoke `AlphaTabApi` block + `wirePlaybackEvents` + `applyTempo` (`app.js:268–360`) are replaced by the component.

**Content-CRUD preview (`content-crud.js`)** — `create(scoreEl, {player:true, controls:"full", onNeedsRerender:…})`.
- `renderPreview`'s `score` branch → `view.load(msg.tex)`; `previewApi`/`renderScore` deleted.
- **Shipped as a full player** (not the originally-planned `player:false`/`mini`): smoke-test feedback wanted transport + metronome/count-in/chord-name/diagram toggles in the progression/song/rhythm previews. `onNeedsRerender` re-requests `entityPreview` with the new `renderOptions` (req IN14, amended).
- **Voicing** preview stays the SVG fret-box (`chord-diagram.js`) — a non-alphaTab path, out of scope.

**Exercise-Workbench (future)** — third consumer, `player:true`, built *on* the component from day one (Q2: build this seam first).

---

## 5. C# seam — `RenderOptions`

Content-kind options need the renderer to emit different alphaTex, so we thread an options bag through the **only** alphaTex-aware code.

```csharp
public sealed record RenderOptions(
    bool ShowChordNames    = false,
    bool ShowChordDiagrams = false,
    VoicingStrategy Voicing = VoicingStrategy.ByDifficulty);
```

- `IScoreRenderer.Render(...)` gains an optional `RenderOptions options = default/null` parameter on both overloads (`Render(Exercise)` and `Render(RealizedSong, rhythm, tempo, difficulty, feel)`); absent ⇒ today's behavior, so it's backward-compatible.
- `ShowChordNames` / `ShowChordDiagrams` add alphaTex chord annotations / chord-diagram directives at chord changes (verify exact syntax against `alphatex-syntax-reference.md` during implementation).
- **Voicing strategy (render-time only).** Scoped per Q4: v1 surfaces the *existing* `VoicingBook.Lookup(chord, difficulty)` selection — `VoicingStrategy.ByDifficulty` reuses the difficulty tier that already drives voicing complexity. The CAGED-shape-specific preference (e.g. "prefer the C shape") is **deferred** to coordinate with the `caged-system` / `voicings` domain threads; the field exists so the seam is ready, but v1 ships only the difficulty-backed value. `RenderOptions` is carried *to* `Lookup`; the resolver honors what the book offers.

**Player-kind options (metronome, count-in) never reach C#** — they're pure alphaTab API calls and stay entirely in the component.

---

## 6. Bridge envelopes

Every **render-producing request** envelope gains an optional `renderOptions` object (absent ⇒ defaults; backward-compatible):

- `generate` (Practice) — already carries key/rhythm/tempo; add `renderOptions`.
- `entityPreview` (Content) — add `renderOptions`.
- `loadExercise` (Practice reload) — add `renderOptions` so toggles apply to reloaded exercises.

Outbound replies (`loadScore`, the `entityPreview` reply) are unchanged — they already carry just the tex (options were applied server-side). When a content-kind toggle flips, the component's `onNeedsRerender` hands the consumer the new `renderOptions`, and the consumer re-sends the matching request.

The C# side maps `renderOptions` → `RenderOptions` and passes it to `_renderer.Render(...)` / the `ContentCrudHandler` preview builders.

**Persistence:** render options are a **view/session preference, not part of the exercise definition** — they are *not* persisted. Consistent with "persist definitions, regenerate alphaTex on load."

---

## 7. Out of scope / deferred

- **Inline voicing in the DSL** (`1@C` shape-pin, or an inline voicing literal) — a separate **DSL thread** (touches `chordflow-dsl-reference`, `ProgressionParser`/`SongParser`, domain). The renderer already resolves per-chord, so a future pin is a per-chord *override* of `Lookup` — seam noted, not built. (Authored shell/custom voicings already work via `VoicingBook`; only *inline declaration* is deferred.)
- **CAGED-shape voicing preference** at render time — deferred to the `caged-system`/`voicings` threads (§5).
- **Two chord-diagram display modes** — *shipped* (req IN16, supersedes EX6): three toggles — Chord names (`{ch}`), Diagrams over staff (`\chordDiagramsInScore`), Diagrams on top (the chord-diagram list, no alphaTex directive — driven by `\chord` defs + the `globalDisplayChordDiagramsOnTop` stylesheet flag set in JS). Defaults: names + on top. Note: `\chordDiagramsOnTop` is **not** a valid alphaTex directive (confirmed) — on-top is stylesheet-only.
- **Voicing fret-box preview** stays SVG (`chord-diagram.js`).
- No dependency on the derivation-engine threads (`intervals`, `octave-shapes`, `chord-qualities`, `caged-system`, `voicings`) — the component is insulated from them by the `VoicingBook` seam and can proceed in parallel.

---

## 8. Reference-doc updates (same unit of work)

- `chordflow-architecture-reference.md` — note the shared `score-render-component.js` as the JS render/transport layer feeding alphaTab; update the bridge-protocol section for `renderOptions` on `generate`/`entityPreview`/`loadExercise`.
- `chordflow-domain-model-reference.md` — document `RenderOptions` on `IScoreRenderer.Render` and the render-time voicing-strategy knob.
- (DSL ref untouched — inline voicing is deferred to its own thread.)

---

## 9. Open questions / risks

1. **alphaTex chord-annotation + diagram syntax** — confirm the exact directives for chord names over the staff and chord diagrams on top against `alphatex-syntax-reference.md` before implementing §5.
2. **Control-strip styling** — the component owning its transport markup means a small CSS migration out of the current practice/CRUD styles into a component stylesheet.
3. **`controls:"mini"` exact set** — play + metronome assumed for CRUD preview; trivially adjustable.
