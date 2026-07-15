---
type: design
id: de_01KXJJJYD9XBRYED1F8HYTG74H
title: Chord Sheets playback — animated bar marker over ChordSheetR
status: done
created: 2026-07-15
version: 1
idea_version: 2
tags: []
parent_id: id_01KXJDBW1RZ0QAGD4NNC11KXMM
requires_load: []
---
# Chord Sheets playback — animated bar marker over ChordSheetR

## 1. One sentence

Extract the alphaTab **playback engine** out of `ScoreR` into a shared, headless-capable `ChordFlowPlayback` (proven by ScoreR keeping byte-identical behaviour); have `ChordSheetBuilder` emit a **`cellSchedule`** — a `(bar,beat)→cell` projection of the same realized Song — so a dumb, addressable `ChordSheetR` can **`highlight(cell)`** the sounding bar/chord in time on the Chord Sheets page, which owns its own `ChordFlowPlayback` instance.

## 2. Two layers of work (→ two plans)

- **Plan 1 — extract `ChordFlowPlayback`.** Pure refactor. Lift the alphaTab api + transport + soundfont + beat/schedule out of `score-render-component.js`; `ScoreR` becomes a thin consumer that adds the visible staff + notation controls. **Acceptance gate = ScoreR parity:** Practice (`app.js`) and Content-preview (`content-crud.js`) play/pause/stop/tempo/cursor/soundfont/`onBeat`/schedule behave exactly as today. No new feature.
- **Plan 2 — sheet playback.** `cellSchedule` from the builder; addressable `ChordSheetR` (`<g>` grouping + `highlight`); the Chord Sheets page owns a `ChordFlowPlayback`, plays the song's alphaTex, and drives the marker from `onBeat`.

## 3. `ChordFlowPlayback` (the extracted engine)

alphaTab fuses render + playback in **one** `AlphaTabApi(surface, settings)` — synth, cursor, `activeBeatsChanged`, and the beat schedule are all bound to a rendered score surface. There is no truly headless clock. So `ChordFlowPlayback` **owns that api** and a surface (which the consumer may keep hidden); it is "headless-capable" only in that its surface can be off-screen.

Responsibilities lifted out of today's ScoreR:

- The `AlphaTabApi` + `buildSettings` (player settings, soundfont, scroll modes).
- Transport: `load(tex, {tempo})`, `play()/playPause()`, `stop()`, `setTempo(bpm)`, per-track volumes.
- Soundfont: list + persisted-choice application (`listSoundFonts`/`setSoundFont`).
- Events out: `onBeat(bar, beat)` (from `activeBeatsChanged`, 1-based), `onStateChange(playing)`, `onFinished()`.
- `getApi()`, `dispose()`.

Proposed handle:

```
window.ChordFlowPlayback.create(surfaceEl, {
  soundFont?, scroll?,
  onBeat(bar, beat), onStateChange(playing), onFinished()
}) → { load, play, stop, setTempo, setTrackVolume, setSoundFont,
       setScrollMode, getApi, dispose }
```

**Stays in `ScoreR`:** the visible staff, `scoreLoaded` staff-flag/track-render wiring, notation-display options (chord names, diagrams, staff profile, key/feel pickers, auto-layout), `getRenderOptions`, `onNeedsRerender`, the debug panel. ScoreR composes a `ChordFlowPlayback` internally and **re-exposes its current public handle unchanged** — so Practice + Content-preview need no edits. That handle-stability *is* the parity gate (§2).

## 4. `cellSchedule` (the `(bar,beat)→cell` projection)

The existing NowNext schedule is keyed `(bar,beat)→chord` but carries **no sheet-cell coordinate**, and a `%` `RepeatOfPrev` cell has *no entry of its own* (its beat lands in a bar whose entry points at the previous bar's chord). So the sheet needs its own projection, emitted by the builder that already knows the exact cell↔bar↔chord layout.

- **Shape (recommended):** a **flat array, one entry per (cell, chord-segment) onset**, ordered by time:
  ```
  CellScheduleEntry( int Bar, int Beat,          // 0-based, same origin as the audio timeline
                     int Section, int Row, int Cell,   // address into ChordSheet.Sections[].Rows[].Cells[]
                     int Chord )                        // sub-segment index within a split cell (0 for single-chord / %)
  ```
  Single-chord cell → one entry at the bar downbeat. Split cell → N entries (one per chord onset). **`%` cell → one entry at its own downbeat** with its own `(Section,Row,Cell)` and `Chord=0` — this is what makes similes highlight correctly (Decision D3).
- The JS stays a dumb lookup: `Map "bar:beat" → {section,row,cell,chord}`, exactly NowNext's pattern.

## 5. Where the alphaTex comes from (Decision D1 — sign-off)

For audio, `ChordFlowPlayback` needs the song's **alphaTex**; for mapping, the page needs the **`cellSchedule`**. Both must share **one `(bar,beat)` origin**, which means both must derive from the **same realized/expanded Song** (same id + key). Two ways:

- **D1-a (recommended) — one combined request.** The `chordSheet` handler realizes the Song once and returns `sheet` + `cellSchedule` **+ the alphaTex** in `chordSheetResult`. Alignment is *by construction* (one SongExpander pass feeds both the sheet and the renderer). Cost: the handler additionally calls `AlphaTexRenderer`; the tex is computed even when the user only exports. 
- **D1-b — two requests.** The page fires `chordSheet` (→ sheet + cellSchedule) **and** the existing score/generate verb (→ alphaTex) for the same id+key. Reuses paths as-is, no handler change; alignment relies on both paths expanding the Song identically (true today, but *by coincidence*, not enforced).

I lean **D1-a** — alignment-by-construction beats alignment-by-coincidence, and it keeps the page to one request. Flagging for your call since it changes the bridge contract.

## 6. Addressable `ChordSheetR` (still a dumb view)

Today the component appends flat `<text>`/`<rect>`/`<line>` onto the root `<svg>`. Change: wrap each drawn bar-cell in a `<g>` carrying its address, and each chord segment of a split cell in a nested `<g>`:

```
<g data-section="0" data-row="1" data-cell="2">      … the bar cell …
   <g data-chord="0"> … </g>   <g data-chord="1"> … </g>   (only when split)
</g>
```

New API (the only additions — everything else unchanged):

```
sheet.highlight(section, row, cell, chord?)   // toggle the "cf-playing" state on the addressed <g>
sheet.clearHighlight()
```

- `highlight` **re-queries the current SVG** (`svgElement().querySelector([data-…])`) — it must not hold node references, because `render()` does `innerHTML=""` and `chord-sheets.js` disposes/recreates on every display toggle. If a re-render happens mid-play, the page re-applies the last highlight after `render()`.
- **Highlight visual:** a translucent fill/stroke on the cell `<g>` (a `<rect>` behind the tokens, toggled via a class or attribute), readable in **both themes** (a theme-aware accent from the existing palette). For a split cell, the active **`data-chord` segment** gets the stronger accent while the cell carries a lighter wash. **Screen-only** — never emitted by `toSvgString`/`toPngBlob`/`lightSvg` (export builds a fresh SVG with no highlight state, so it's inert by construction).

## 7. Granularity (Decision D2 — sign-off)

**Recommended:** highlight at **cell (=bar) level always**, and **chord-segment level within a split bar**. The chord *token band* is the primary highlight; the tone-strip / fret-diagram adornments (single-chord, v1) are **not** separately animated — they sit inside the cell `<g>` and simply ride the cell wash. Keeps v1 focused; per-adornment animation is a later polish.

## 8. Page wiring (Chord Sheets view)

`chord-sheets.js` gains a hidden `ChordFlowPlayback` surface + a transport strip:

1. On song-select it already requests the sheet; with **D1-a** the reply also carries `cellSchedule` + `tex`. Feed `tex` to `ChordFlowPlayback.load(tex, {tempo})`; build the `bar:beat→cell` map from `cellSchedule`.
2. `onBeat(bar, beat)` → look up the cell → `sheet.highlight(...)`; unknown `(bar,beat)` → keep the last (sub-bar beats between onsets).
3. `onStateChange`/`onFinished` → `sheet.clearHighlight()` on stop/end.
4. Transport (play/stop/tempo, soundfont) lives on the page and drives the engine. **Reveal-staff (Decision D4):** staff **hidden by default**; an optional "Show tab" collapsible is a cheap nice-follow (the surface exists) — recommend hidden-by-default, toggle as stretch.

Each page owning its own `ChordFlowPlayback` (per idea decision **a**) means no cross-page transport coupling; the shared-bus option (c) stays a captured north-star.

## 9. Open decisions (want sign-off before locking req)

- **D1 — alphaTex delivery:** one combined `chordSheetResult` (sheet+cellSchedule+tex) *(recommended, alignment-by-construction)* vs two requests.
- **D2 — granularity:** cell + sub-chord *(recommended)* vs bar-only.
- **D3 — `%` mapping:** `cellSchedule` carries the repeat cell's own coordinate *(recommended; the reason the projection is builder-side)*.
- **D4 — reveal-staff UX:** hidden-by-default + optional "Show tab" toggle *(recommended)* vs fully headless.

## 10. Scope & phasing

- **Plan 1:** extract `ChordFlowPlayback`; refactor `ScoreR` onto it with an unchanged public handle; ScoreR-parity verified on Practice + Content-preview.
- **Plan 2:** `cellSchedule` (builder + bridge, per D1); addressable `ChordSheetR` + `highlight`/`clearHighlight`; page-owned engine + wiring; start/stop/seek; both layouts; light/dark; dogfood.

## 11. Non-goals (v1)

- No shared global transport bus (option c).
- No export of the animated state (export stays the static light snapshot).
- No new Core music theory beyond the `cellSchedule` projection.
- No accuracy/timing detection; none of the sibling v2 overlays.

## 12. Validation / dogfood

- **Parity (Plan 1):** Practice + Content-preview play/cursor/transport/soundfont identical on the extracted engine.
- **Sheet (Plan 2):** Jazz Blues + a pop song, both layouts — marker tracks the sounding bar/cell, matches the ScoreR cursor beat-for-beat, handles a multi-chord split bar and a `%` bar, clears on stop; reads in light + dark; export unaffected.

## 13. Reference-doc impact (ref-sync rule)

Landing this updates **`chordflow-architecture-reference`** (the new `ChordFlowPlayback` component + `ScoreR` refactored onto it; the `chordSheetResult` contract change for `cellSchedule`/tex) and **`chordflow-domain-model-reference`** (the `cellSchedule` projection off `ChordSheetBuilder`), in the same unit of work as the code.