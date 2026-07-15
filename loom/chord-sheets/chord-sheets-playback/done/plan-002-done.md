---
type: done
id: pl_01KXJM98ZZMPCYW4XNH3KY6B6K-done
title: Done — Plan 2 — Chord-sheet playback (marker on ChordSheetR)
status: done
created: 2026-07-15
version: 5
tags: []
parent_id: pl_01KXJM98ZZMPCYW4XNH3KY6B6K
requires_load: []
---
# Done — Plan 2 — Chord-sheet playback (marker on ChordSheetR)

## Step 1 — Add a pure CellScheduleEntry(Bar, Beat, Section, Row, Cell, Chord) record + have ChordSheetBuilder emit a per-bar BAR-MAP while it walks bars: one entry per bar (global 0-based bar index -> Section/Row/Cell) at the bar downbeat (Beat=0, Chord=0), covering % RepeatOfPrev AND sustained bars. The builder does NOT compute rhythm-slot beats (it has no rhythm layout) — split-cell sub-chord onset beats are overlaid in step 2 from the render schedule (approach A). Test the bar-map covers every bar incl. % and multi-chord cells.

Added `CellScheduleEntry(int Bar, int Beat, int Section, int Row, int Cell, int Chord)` to `Rendering/ChordSheets/ChordSheet.cs` (0-based, alphaTab master-bar/beat.index units; docs the builder-downbeats + handler-overlay split).

`ChordSheetBuilder.Build` now returns **`ChordSheetBuildResult(ChordSheet Sheet, IReadOnlyList<CellScheduleEntry> BarSchedule)`**: while walking bars it emits one **downbeat** entry per bar (`Beat=0, Chord=0`) carrying a running global 0-based bar index + (section, row, cell) — covering `%` similes and sustained bars. Tracks `globalBar` across all sections (lines up with `AlphaTexRenderer.BarIndex`/alphaTab master-bar index) and `rowIndex`/`cellIndex` for the addressing. The builder does not compute rhythm-slot beats (approach A); split-bar sub-chord onsets are the handler's overlay (step 2).

`ChordSheetHandler` adapted to `built.Sheet` (minimal; step 2 will consume `built.BarSchedule`). Tests: `Build` helper now returns `.Sheet`, three direct call-sites use `.Sheet`, and added 2 bar-map tests (one-entry-per-bar incl. a `%` bar with its own coordinate; a split bar → a single downbeat entry). Build 0 errors; 22/22 ChordSheets tests pass.

## Step 2 — Realize the Song ONCE in ChordSheetHandler and feed the builder (sheet + bar-map) AND the alphaTex renderer (tex + its ChordChange schedule) from that single expansion. ASSEMBLE the final cellSchedule in the handler: per-bar downbeat entries from the bar-map (covers %/sustained), overlaid with sub-chord onset beats from RenderResult.Schedule for split bars, so (bar,beat) aligns with the audio timeline by construction (D1-a). Extend chordSheetResult to { sheet, cellSchedule, tex } + the envelope DTOs + serialization.

`ChordSheetHandler.Build` now realizes the song once and produces the full playback payload:
- **Comping resolved always** (was diagram-only) — the same `CompingPlan` feeds the fret diagram (when the adornment is on) AND the audio render, so the drawn grip is what sounds.
- **Renders playable alphaTex** via `new AlphaTexRenderer().Render(realized, SeedData.Quarters, tempo, Difficulty.Beginner, comping, feel)` — the approved neutral quarter-note comp (every beat an attack, so split-bar mid-bar onsets land on a real beat); `tempo = song.DefaultTempo ?? 100`, `feel = song.DefaultFeel ?? None`. One pass → `RenderResult { Tex, Schedule }`, aligned with the bar-map by construction (D1-a).
- **`BuildCellSchedule`** overlays the render schedule onto the builder's per-bar downbeats: keeps one downbeat entry per bar (bar-level, incl. `%`/sustained), and for each mid-bar `ChordChange` (beat>0) adds a sub-chord entry mapped to chord-segment index j+1 (beat-ordered; segment 0 is the downbeat). Sorted by (Bar, Beat).

`ChordSheetResultEnvelope` extended to `{ Sheet, CellSchedule, Tex }`. Handler tests: `SeededConnection` parameterized by DSL; added `Build_ReturnsPlayableTexAndPerBarCellSchedule` (non-empty tex + a downbeat per bar incl. the `%` bar) and `Build_SplitBar_GetsSubChordOnsetEntry` (split bar → segment-1 entry at beat>0). Build 0 errors; 24/24 ChordSheets tests pass.

## Step 3 — Wrap each drawn bar-cell in <g data-section data-row data-cell> and each chord segment of a split cell in a nested <g data-chord>, in BOTH layouts (A flowing, B grid). Pure structural change — same pixels, now addressable. ChordSheetR stays a dumb view with zero alphaTab dependency.

ChordSheetR made addressable in `chord-sheet-render-component.js`. `buildSheetSvg` loop now passes a section index; `drawSection(...,si)` threads a row index; `drawRow(...,si,ri)` wraps each bar-cell in `<g class="cf-cell" data-section data-row data-cell>` (barlines/border + a `cf-cell-hl` backdrop rect + the cell content all move inside it). `drawCell(parent,...)` draws into that group and wraps every chord token in `<g class="cf-chord" data-chord=j>` (single-chord = one `data-chord=0`; split = one per segment; `%` cell has none — it highlights at the cell level). Same pixels — the backdrops are `fill:none` in a fresh build, so export is unchanged. Indices match Core's cellSchedule because the page requests barsPerRow=4 and ChordSheetR chunks at 4 (Layout B uses Core rows directly; Layout A re-chunks identically). node --check clean.

## Step 4 — Add sheet.highlight(section,row,cell,chord?) and sheet.clearHighlight(): toggle a screen-only 'cf-playing' state on the addressed <g> by RE-QUERYING the current SVG (no held node refs). Cell-level highlight always + the active chord segment within a split bar (D2); adornments ride the cell wash. A light/dark-safe accent (translucent rect behind the tokens) from the shared palette. Never present in toSvgString/toPngBlob/lightSvg output.

Added the screen-only marker API to ChordSheetR: `highlight(section,row,cell,chord?)` and `clearHighlight()`. `applyHighlight` re-queries the CURRENT svg (`.cf-cell[data-…]` → `.cf-chord[data-chord]`) and toggles a `cf-playing` class — no held node refs. A `lastHighlight` is stored and re-applied at the end of `render()`, so a layout/notation/theme rebuild mid-play keeps the marker. Visual = one injected `<style>` (`HIGHLIGHT_CSS`): `.cf-playing > .cf-cell-hl` = translucent amber `rgba(245,158,11,.18)` (bar wash), `.cf-playing > .cf-chord-hl` = `rgba(245,158,11,.42)` (active split-segment) — translucent amber reads on both light + dark, no per-theme table. Export stays inert: `toSvgString`/`toPngBlob`/`lightSvg` build a fresh SVG with no `cf-playing`, and the style rule only fires on that class. `highlight`/`clearHighlight` exposed on the handle; header doc updated. node --check clean.

## Step 5 — chord-sheets.js gains a hidden ChordFlowPlayback surface + a transport strip (play/stop/tempo, soundfont). On chordSheetResult: load tex into the engine, build a 'bar:beat'->cell map from cellSchedule. onBeat(bar,beat) -> lookup -> sheet.highlight(...); unknown sub-onset beats keep the last cell. onStateChange/onFinished -> clearHighlight on stop/end. Start/stop/seek land on the correct cell. Staff hidden by default with an optional 'Show tab' collapsible (D4).

`chord-sheets.js` now owns its own `ChordFlowPlayback` (option a) and drives the marker.

- **`setupEngine()`** creates one `ChordFlowPlayback` rendering its staff into a collapsed `scoreWrapEl` (`overflow:hidden;max-height:0`, full width so alphaTab still lays out). **`buildTransport()`** adds play/stop/tempo + soundfont picker + a "Show tab" checkbox (D4 — reveals the staff by clearing max-height).
- **On `chordSheetResult`**: `buildSchedule(msg.cellSchedule)` groups entries by 0-based bar (sorted by beat); `renderNow()`; `clearHighlight()`; `engine.stop()` + `engine.load(msg.tex, { tempo: header.tempo || 100 })`.
- **`onBeat(bar,beat)`** (engine 1-based → 0-based like NowNext): picks the last entry in the bar with `beat <= current` (covers sub-chord onsets + sustain), dedupes via `lastMarkerKey`, calls `view.highlight(section,row,cell,chord)`. `onFinished`/stop → `clearHighlight`; `onReady` → enable transport; `onSoundFontsListed` → fill picker.
- **Marker survives display toggles**: `renderNow` now creates the ChordSheetR once and reuses it; the toolbar's layout/notation/tone-label/theme handlers call the component's setters (each re-renders + re-applies `lastHighlight`) instead of rebuilding. `requestSheet`'s no-harmony branch stops the engine + clears the schedule.
- No `index.html`/`bridge.js` change needed (engine surface + transport built in JS; existing bridge fan-out delivers `soundFontsListed`/`chordSheetResult`). `playback-component.js` already loads before `chord-sheets.js`. node --check clean.

## Step 6 — Play Jazz Blues + a pop song in both layouts: confirm the marker tracks the sounding bar/cell, matches the ScoreR cursor beat-for-beat, handles a multi-chord split bar and a % bar, clears on stop, reads in light + dark, and that export is unaffected. Update chordflow-domain-model-reference (cellSchedule projection off ChordSheetBuilder) and chordflow-architecture-reference (chordSheetResult now carries cellSchedule + tex).

**Ref-sync done.** Updated `chordflow-domain-model-reference` (new "Playback projection" para: `ChordSheetBuildResult`/`BarSchedule`, the handler's one-realized-song render with `SeedData.Quarters` + always-resolved comping, the `ChordChange`→cellSchedule overlay, and `chordSheetResult = { sheet, cellSchedule, tex }`) and `chordflow-architecture-reference` (the `chordSheetResult` contract change + addressable `ChordSheetR` `<g>` groups + `highlight()`/`clearHighlight()`).

Full suite green: **920/920** tests pass; all JS `node --check` clean.

**Dogfood pending** — live-app visual check (Rafa): build + run, Chord Sheets tab → pick Jazz Blues + a pop song, Play, confirm the marker tracks the sounding bar/cell beat-for-beat (matches the ScoreR cursor), a split bar sub-highlights, a `%` bar highlights, clears on stop, reads in light + dark, and export is unaffected. Step left open until confirmed.
