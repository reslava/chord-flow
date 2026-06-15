---
type: req
id: rq_01KV5D9MXERR51XQ7A84YDEV2E
title: Score Render Component — Requirements
status: locked
created: "2026-06-15T00:00:00.000Z"
updated: 2026-06-15
version: 3
tags: []
parent_id: null
requires_load: []
---
# Score Render Component — Requirements

One reusable JS component that renders an **alphaTex string → alphaTab notation + optional playback** with a declarative on/off option set, consumed by every score-showing screen (Practice, Content-CRUD preview, future Exercise-Workbench). Centralizes the **alphaTex → alphaTab** half only; alphaTex *generation* stays in C# `AlphaTexRenderer`. Scope confirmed in `score-render-component-chat-001` + `score-render-component-design`.

### ✅ Included

- `IN1` A **single reusable render component** (`wwwroot/score-render-component.js`, `window.ChordFlowScore`) that turns an alphaTex string into rendered notation, replacing the two divergent `AlphaTabApi` instantiations (`app.js`, `content-crud.js`).
- `IN2` **One source of truth for alphaTab settings** (`fontDirectory`, `useWorkers`, `soundFont`, `scrollMode`, cursor/highlight flags) living inside the component — no per-consumer drift.
- `IN3` A **`create(container, opts)` factory** returning a handle with `load(tex, {tempo})`, `play()`, `stop()`, `setTempo(bpm)`, `setOption(name, value)`, `getRenderOptions()`, `dispose()`.
- `IN4` A **`player` mode flag**: `player:true` = full player (soundfont + transport + cursor); `player:false` = lite, render-only (no soundfont load).
- `IN5` A **`controls` profile** (`"full"` | `"mini"` | `"none"`) — the component owns its control strip (transport, toggles, metronome slider) rendered per profile, consistent across screens.
- `IN6` **Player-kind options applied locally via the alphaTab API** (no round-trip): metronome on/off + volume, count-in, cursor, playback speed.
- `IN7` **Content-kind options** (chord names, diagram modes, voicing strategy) flip via a **C# re-render**: `setOption` fires an `onNeedsRerender(renderOptions)` callback the consumer turns into a fresh render request.
- `IN8` **Event callbacks** to the consumer: `onBeat(bar, beat)` (1-based), `onStateChange(playing)`, `onFinished()` — preserving the existing `beatChanged` / `playbackFinished` bridge sends.
- `IN9` A C# **`RenderOptions`** record threaded as an **optional** parameter through `IScoreRenderer.Render(...)` (both overloads); absent ⇒ today's behavior (backward-compatible).
- `IN10` **Chord-name** and **chord-diagram** alphaTex emission in `AlphaTexRenderer`, gated by `RenderOptions`. `{ch "Name"}` labels at chord changes; `\chord` diagram definitions emitted in the **metadata header** (before the `.`) + `\chordDiagramsInScore` over-staff toggle — confirmed in the running app.
- `IN11` **Render-time voicing strategy** carried in `RenderOptions` to `VoicingBook.Lookup`; v1 ships the difficulty-backed value (`ByDifficulty`) reusing the existing selection.
- `IN12` An **optional `renderOptions` field** on the render-producing request envelopes — `ready`, `generate`, `entityPreview`, `loadExercise` — mapped to `RenderOptions` on the C# side (absent ⇒ defaults). `ready` carries the component's default options so the boot score reflects the checked toggles.
- `IN13` **Retrofit Practice** (`app.js`) onto the component in `player:true` / `controls:"full"` mode; transport + tempo move into the component, the bespoke `AlphaTabApi` block is removed. (A content-toggle change replays the last render request — including the boot exercise — with the new options.)
- `IN14` **Retrofit Content-CRUD score preview** (`content-crud.js`) onto the component. **Shipped as a full player** (`player:true` / `controls:"full"`) so progression/song/rhythm previews get transport + metronome/count-in/chord-name/diagram options (smoke-test feedback) — supersedes the original `player:false`/`controls:"mini"` plan. The voicing fret-box (`chord-diagram.js`) is untouched.
- `IN15` **Reference-doc updates** in the same unit of work: architecture ref (component as the JS render/transport layer + `renderOptions` on the bridge envelopes) and domain-model + alphaTex-syntax refs (`RenderOptions`, chord emission, the two diagram-display modes).
- `IN16` **Three independent chord-display toggles** (smoke-test refinement, supersedes `EX6`): **Chord names** (`{ch}` over staff), **Diagrams over staff** (`\chordDiagramsInScore`), **Diagrams on top** (the chord-diagram list — shown for defined+used chords, with no alphaTex directive; visibility via the `globalDisplayChordDiagramsOnTop` stylesheet flag set in JS). Defaults: **Chord names + Diagrams on top** selected. Coupling: turning on *Diagrams on top* while *over staff* is off auto-selects *Chord names*. (`\chordDiagramsOnTop` is **not** a valid alphaTex directive — confirmed; emitting it breaks the parse.)

### ❌ Excluded

- `EX1` **Inline voicing in the DSL** (`@shape` pin / inline voicing literal in a progression or song) — a separate DSL thread (touches `chordflow-dsl-reference`, the parsers, the domain).
- `EX2` **CAGED-shape-specific voicing preference** at render time — deferred to the `caged-system` / `voicings` domain threads; only `ByDifficulty` ships in v1.
- `EX3` The **voicing fret-box preview** — stays the SVG `chord-diagram.js` path, not alphaTab; untouched by this thread.
- `EX4` **Persisting render options** with the exercise definition — they are a view/session preference, never stored.
- `EX5` **Exercise-Workbench** itself — the future third consumer; this thread only builds the seam it will sit on.
- `EX6` ~dropped — **implemented as `IN16`.** (Was: two independent chord-diagram display modes deferred as a follow-up.)

### ⛓ Constraints

- `C1` alphaTex is **generated only in C# `AlphaTexRenderer`** — the component never builds alphaTex in JS (the exporter seam stays intact). The one exception is the on-top **display** flag (`globalDisplayChordDiagramsOnTop`), a stylesheet property with no alphaTex directive, set in JS — it changes display, not alphaTex content.
- `C2` Player-kind options (metronome, count-in) **never reach C#** — pure alphaTab API calls inside the component.
- `C3` `RenderOptions` is **optional everywhere**; its absence reproduces today's render exactly (backward-compatible).
- `C4` Dependency direction **Desktop → Core** unchanged; the engine stays UI-agnostic (compile-enforced).
- `C5` **No new build step or framework** in `wwwroot` — vanilla JS modules over the existing virtual host.
- `C6` **No dependency** on the derivation-engine threads (`intervals`, `octave-shapes`, `chord-qualities`, `caged-system`, `voicings`) — insulated by the `VoicingBook` seam; this thread can proceed in parallel.
