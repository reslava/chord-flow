---
type: idea
id: id_01KXJDBW1RZ0QAGD4NNC11KXMM
title: Chord Sheets playback — animated bar marker over ChordSheetR
status: done
created: 2026-07-15
updated: 2026-07-15
version: 2
tags: []
parent_id: null
requires_load: [id_01KXGQHYZ9WS1YFBQRWYXQWHSJ, de_01KXGRA16DWE7FKJJEX3A2ZAXH, pl_01KXGSCVXQEEK70B1ZTMH4QY34]
---
# Chord Sheets playback — animated bar marker over ChordSheetR

## What

Bring the ChordFlow **playback cursor** onto the chord sheet: while a song plays, the **currently-sounding bar (and, where a bar splits, the currently-sounding chord cell) lights up in time** on `ChordSheetR`, the way alphaTab's cursor highlights the tab in `ScoreR`. This is the first item from the [[chord-sheets-maker]] v1 "Deferred (captured, not v1)" list, promoted to its own thread.

## Why

A chord sheet you can *follow while it plays* is the difference between a printout and a practice tool. It closes the loop with the rest of ChordFlow (ScoreR already highlights in time) and makes the chord sheet a first-class practice view, not just an export target.

## Grounding — what the shipped v1 actually gives us (checked against code, chat-001)

The maker v1 **design intended** the render surface to be ready for this: §6 says "compose an `<svg>` DOM … `<g>` per bar/cell" and the implementation note says the shell drives "playback highlighting (toggling `<g data-bar>` attributes)," reusing a Layout-B highlight state. **The shipped code diverged.** Grounding this thread against the actual code (not the design's promises) found three gaps this thread must close — none fatal, but they move work from "wiring" to "build":

1. **No addressable render surface.** `chord-sheet-render-component.js` appends **flat** `<text>`/`<rect>`/`<line>` straight onto the root `<svg>` — **no `<g>` grouping, no `data-bar`/`data-cell` attributes, no highlight state.** And `render()` clears `innerHTML`; `chord-sheets.js` disposes/recreates the component on every display toggle. So a marker can't hold external node references. → **In scope:** add `<g data-bar data-cell>` grouping + a `highlight(bar, cell)` method that re-queries the current SVG + a highlight visual (both themes).
2. **No player on the Chord Sheets page.** `chord-sheets.js` is render + export only — no alphaTab, no transport, no soundfont, no schedule. "While a song plays" has **no clock on that page** today. → **In scope:** give the page a playback source (see Architecture).
3. **Time-source facts.** ScoreR uses alphaTab's **`activeBeatsChanged`** surfaced as **`onBeat(bar, beat)` (1-based)** — not `playedBeatChanged`. A real **chord schedule** exists (sent on score load, `{bar,beat,…}` 0-based, one entry per chord change), consumed today by **NowNext** (`now-next-fretboards.js`) — the proven template for "mount a component, feed it schedule + a beat signal."

## Architecture & settled decisions (chat-001)

- **Extract a shared `ChordFlowPlayback`** (the playback engine): the alphaTab `AlphaTabApi(player)` + transport + soundfont + `onBeat`/schedule. alphaTab fuses render+play in one api, so `ChordFlowPlayback` owns that api (render surface hideable). **`ScoreR` = `ChordFlowPlayback` + visible staff + notation-display controls** — ScoreR is refactored to sit *on* the engine.
- **(a) Each page owns its own `ChordFlowPlayback`.** No shared global transport bus — a page's Play button driving a player that lives on another page is the "weird" we're avoiding. (The shared-bus option (c) is captured as a north-star refactor; (a) doesn't block it.)
- **(ii) `ChordSheetBuilder` emits a `cellSchedule`** — `(bar,beat)→cell` coordinates, guaranteed consistent with the drawn cells (handles multi-chord split cells and `%` `RepeatOfPrev` correctly, which a JS-side `(bar,beat)` map would get wrong since a `%` cell has no schedule entry of its own). The JS stays a dumb `(bar,beat)→cell` lookup, same dumb-view contract as v1.
- **`ChordSheetR` stays a dumb view (C1):** it exposes `highlight(bar, cell)` and nothing more; export stays one self-contained SVG with **no** alphaTab dependency. The **page** (`chord-sheets.js`) owns the `ChordFlowPlayback` and calls `sheet.highlight(...)` on each beat — the NowNext pattern verbatim.
- **Naming:** `ChordFlowPlayback` is JS + a transport/controller, so it takes neither the `R` (JS render) nor `E` (C# engine) shorthand — a plain descriptive name.

## Scope (v1 of this thread)

- **Extract `ChordFlowPlayback`** and refactor `ScoreR` onto it, with **ScoreR parity** (Practice + Content-preview: play/pause/stop, tempo, soundfont, cursor, schedule/`onBeat` all identical).
- **`cellSchedule` from `ChordSheetBuilder`** + its bridge field, mapping `(bar,beat)→(section,row,cell,chord)` including multi-chord split cells, `%` similes, and section boundaries.
- **Addressable `ChordSheetR`:** `<g data-bar data-cell>` groups + `highlight(bar, cell)` + a highlight visual, in **both layouts** (A flowing leadsheet, B fixed grid), driven purely by attribute toggling — no re-request to Core.
- **Wire it on the Chord Sheets page:** the page owns a `ChordFlowPlayback`, requests the score's alphaTex+schedule alongside the sheet, and drives `sheet.highlight` from `onBeat`. Transport lives on the page (optional: a toggle to reveal the underlying staff).
- **Start / stop / seek** — marker appears on play, clears on stop, lands on the right cell on reposition.
- **Light/dark parity on-screen** — the highlight reads in both themes (export stays a static light snapshot; the marker is screen-only, non-exported).

## Plans (two, per chat-001)

1. **Extract `ChordFlowPlayback`, prove it with ScoreR** — pure refactor; acceptance gate is ScoreR parity on Practice + Content-preview. No new feature rides on it yet.
2. **Chord-sheet playback on the proven `ChordFlowPlayback`** — `cellSchedule`, addressable `ChordSheetR` + `highlight`, page wiring, start/stop/seek, dogfood.

## Open design questions (for the design session)

1. **`cellSchedule` shape** — a flat `[{bar, beat, sectionIndex, rowIndex, cellIndex, chordIndex}]`, or nested to mirror the model? How it rides the bridge (part of `chordSheetResult`, or its own message).
2. **Highlight granularity** — bar-level always, and cell-level only within a split bar? Does the tone-strip / diagram adornment highlight too, or only the chord token?
3. **`%` / repeats mapping** — a `RepeatOfPrev` cell highlights on *its* beat while the schedule entry points at the previous bar's chord; confirm the `cellSchedule` carries the repeat cell's own coordinate.
4. **`ChordFlowPlayback` API surface** — the exact handle ScoreR and the sheet page both consume (`load`, `play/stop`, `setTempo`, `onBeat`, schedule access, soundfont), and how much of today's ScoreR handle stays stable for Practice/Content-preview.
5. **Reveal-the-staff UX** — does the sheet page expose the hidden staff (collapsible), or keep the engine fully headless?

## Siblings (rest of v2 — captured, each its own future thread)

From the [[chord-sheets-maker]] Deferred list, promote each when next:

- **Non-diatonic analysis markers** (`V/ii`, borrowed, tritone subs) — *blocked on* `domain/harmonic-analysis` filling `ChordRef.Analysis`.
- **Scale / mode + improv-target overlay** — the lead-trainer north star; its own phase.
- **Guide-tone / voice-leading lines** between consecutive chords.
- **Advanced Layout-A engraving** (true repeats `𝄆:𝄇`, endings, coda/segno, D.C., fermata) — *blocked on* the Song model carrying that structure.
- **Shared transport bus (option c)** — one `ChordFlowPlayback` many views subscribe to; the north-star refactor once multi-view sync is wanted.

## Non-goals (v1 of this thread)

- No new Core music theory — the marker is a screen-only presentation layer; the only Core addition is the `cellSchedule` projection over data the v1 model already carries.
- No export of the animated state — export remains the static light snapshot from v1.
- No shared global transport bus (option c) — each page owns its own `ChordFlowPlayback`.
- None of the sibling v2 items above.

## Validation / dogfood

- **ScoreR parity (Plan 1):** Practice + Content-preview play/cursor/transport behave byte-for-byte as before on the extracted `ChordFlowPlayback`.
- **Sheet playback (Plan 2):** play **Jazz Blues** and a pop song in **both layouts**; the marker tracks the sounding bar/cell in time, matches the ScoreR cursor beat-for-beat, handles a multi-chord split bar and a `%` bar correctly, and clears on stop.
- Highlight reads in **light and dark** on-screen; **export unaffected** (static light snapshot).

## Reference material

- Parent: [[chord-sheets-maker]] v1 — the `ChordSheet` model, `ChordSheetBuilder`, `chordSheet` bridge verb, and the `ChordSheetR` SVG component (its **flat** SVG is what this thread makes addressable). Idea/design/plan are in this thread's `requires_load`.
- **`ChordFlowPlayback` extraction target:** `wwwroot/score-render-component.js` (today's ScoreR — the alphaTab api + transport + `activeBeatsChanged`→`onBeat` + soundfont to lift out).
- **Pattern to copy:** `wwwroot/now-next-fretboards.js` (NowNext) — mounts once, consumes `loadScore.schedule` + the beat signal via a `(bar,beat)→index` lookup; `app.js` wires beat→NowNext.
- Origin discussion: `chord-sheets-maker/chat-002` and this thread's `chat-001`.