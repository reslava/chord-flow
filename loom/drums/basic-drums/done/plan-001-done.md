---
type: done
id: pl_01KXWNVK9355V921VP3P8HPX4E-done
title: Done — Basic Drums — standalone groove vertical slice
status: done
created: 2026-07-19
version: 9
tags: []
parent_id: pl_01KXWNVK9355V921VP3P8HPX4E
requires_load: []
---
# Done — Basic Drums — standalone groove vertical slice

## Step 1 — Add DrumVoice (enum → GM articulation), DrumLane, and DrumGroove (multi-lane over the 48-PPQ tick grid); reuse Music.Rhythm RhythmEvent/TickGrid per lane. Update the domain-model ref.

Added the `Instruments/Drums/` domain (namespace `ChordFlow.Instruments.Drums`).

**Files**
- `src/ChordFlow.Core/Instruments/Drums/DrumVoice.cs` — `DrumVoice` enum (11 GM voices) + `DrumVoices` static vocabulary (the single source, peer of `VoicingFamilies`): `Articulation()` → GM alphaTex name, `Token()` → canonical short token, `TryParse()` → short-token/full-name alias resolution (case-insensitive).
- `src/ChordFlow.Core/Instruments/Drums/DrumGroove.cs` — `DrumLane(Voice, RhythmEvent[])`, `DrumBar(Lanes)`, `DrumGroove(Id, Name, Bars, TimeSignature)` with `SingleBar(...)` factory + `DistinctVoices()`.
- `tests/ChordFlow.Core.Tests/Instruments/Drums/DrumGrooveTests.cs` — vocabulary (articulation/token/alias/case-insensitivity/unknown) + groove helpers. **35 tests pass.**
- `loom/refs/chordflow-domain-model-reference.md` — new §3b "Drums — multi-lane grooves" (IN8, same unit of work).

**Decisions**
- Kept `DrumLane`/`DrumBar` inside `DrumGroove.cs` (not a separate `DrumLane.cs` as the plan sketched) — matches the existing `PatternBar`-in-`RhythmPattern.cs` convention; tightly-coupled small records.
- **Bar-major** model (`Bars → Lanes → Events`): the renderer walks bar-by-bar and merges lanes; the lane-major DSL is transposed by the parser (step 2).
- Each hit is a one-cell `RhythmEvent` (honors C2 literally) — `Length` = cell width, unused for percussion duration (re-derived from the onset grid at render).
- Music→Instruments architecture test still green (drum types reference only `Music.Rhythm`).

## Step 2 — DrumGrooveParser: rows=voices, x=hit / .=empty, per-row + per-run :n subdivision, | bars, :3/:6 triplet beats, short-token vocabulary + full-name aliases, fail-loud errors. Update the DSL ref.

Added the drums hit-grid DSL parser.

**Files**
- `src/ChordFlow.Core/Instruments/Drums/DrumGrooveParser.cs` — pure `Parse(id, name, dsl, ts) → DrumGroove`. Rows = voice lanes (newline-significant, unlike the Rhythm DSL); `x`=hit / `.`=no-hit; `#` line comments; `|` bars; row-level + in-bar `:n`. Bar-major assembly with duplicate-voice + bar-count-agreement checks. Fail-loud `FormatException` naming the voice/run/cell.
- `tests/ChordFlow.Core.Tests/Instruments/Drums/DrumGrooveParserTests.cs` — rock beat, grid-alignment whitespace, shuffle `:3`, per-run mixing, multi-bar, aliases, comments, one-cell-RhythmEvent, and all fail-loud paths. **18 tests pass.**
- `loom/refs/chordflow-dsl-reference.md` — new "Drums hit-grid DSL" section (IN8, same unit of work).

**Key design decision — whitespace vs `:n` runs.** The locked req IN2 asked for "per-run `:n`", but Rafa's grid needs spaces for column alignment — those pull opposite ways if runs are space-delimited (as the Rhythm DSL does). Resolved by making **whitespace between cells insignificant** and delimiting **runs by `:n` markers** instead: a bare `:n` inside a bar starts a new run. This satisfies per-run subdivision (straight+triplet mix in one bar) AND the aligned-grid look, and is arguably cleaner than the Rhythm DSL's space-delimited runs. `X` is rejected (reserved for the future accent glyph, `drums/drums-accent-ghost`).

## Step 3 — Render a DrumGroove to an alphaTex percussion track: \instrument percussion + \articulation defaults + \ts/\tempo, hits as articulation-name notes, simultaneous hits grouped in ( ), rests where silent. Keep it concrete (no IInstrument).

Added the concrete percussion renderer.

**Files**
- `src/ChordFlow.Core/Rendering/DrumGrooveRenderer.cs` — `Render(DrumGroove, tempo) → string`. Header `\instrument percussion` + `\articulation defaults` + `\tempo`/`\ts` (no `\ks`); per bar it merges lanes into an onset→voices timeline and reuses `RhythmQuantizer` for durations/tuplets/rests; simultaneous hits render as `( )` groups, single hits bare.
- `tests/ChordFlow.Core.Tests/Rendering/DrumGrooveRendererTests.cs` — header/no-`\ks`, rock-beat grouping (8 hi-hats, `:8`), single-voice-no-parens, sparse→rests, shuffle→`{tu 3}`, multi-bar, tempo. **7 tests pass.**
- `loom/refs/chordflow-domain-model-reference.md` — `DrumGrooveRenderer` row in §5 (IN8, same unit of work).

**Design decision — onset notation.** First cut spanned each onset to the next (back-to-back notes); that over-sustains and can throw `NotSupportedException` on a sparse gap that isn't a single representable value. Switched to notating each hit at its **own cell width, capped to the gap**, letting the quantizer coalesce the rest into rests. Result: dense grids stay one clean note per cell (`:8 (KickHit HiHatClosed) …`), sparse grooves read as hits + rests, and it never throws on well-formed single-feel-per-beat bars. Kept it **concrete** (req C7) — no `IInstrument`; that extraction is `chordflow/instrument-rendering`, to be diffed against this once both live.

**Verification:** full Core suite **1118 passed, 0 failed** — the `Music → Instruments` architecture test stays green (drums types reference only `Music.Rhythm`; the renderer sits in the allowed `Rendering → Instruments` edge).

## Step 4 — Confirm the committed sonivox.sf2 sounds alphaTab percussion articulations on GM channel 10, via the CDP harness (render a groove, hear it).

Soundfont/articulation smoke test — **passed** (parse verified programmatically + audible confirmed by Rafa).

**The finding this step caught (before playback was built on top):** the idea's articulation names (`KickHit`…) are alphaTab **2.x/next**; the vendored engine is **1.8.3**, which keys `\articulation defaults` by `toArticulationId(name) = name.replace(/[^a-zA-Z0-9]/g,"").toLowerCase()`. Decision (Rafa): **adapt to 1.8.3** now; a 1.8.4/1.9 bump is a separate later task (2.x is breaking/unstable).

**Fix (corrects steps 1 & 3):** `DrumVoices.Articulation()` now emits the 1.8.3 tokens — `kickhit`/`snarehit`/`hihatclosed`/`hihatopen`/`pedalhihathit`/`ridemiddle`/`ridebell`/`crashhighhit`/`hightomhit`/`midtomhit`/`lowfloortomhit` (verified name+GM-MIDI against the vendored kit table). Only the 11 strings changed — model/DSL/parser/renderer structure untouched. Updated `DrumGrooveTests` + `DrumGrooveRendererTests` expectations + the domain-model ref §3b. **60 drums tests + full suite green.**

**Verification harness** (scratchpad `drums-smoke.mjs`, Node, no deps): attaches via the WebView2 remote-debugging port (launch with `CHORDFLOW_DEVTOOLS=1` + `WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS=--remote-debugging-port=9223`), feeds our exact rock-beat tex to `window.__cfApi`, reports parse result. Result: **`ok:true`, 12 percussion notes, 3 articulations, zero errors** (no `AT209`). Then `--play` (trusted-click gesture for the autoplay gate + loop) → **Rafa confirmed the groove sounds** on the committed default soundfont (channel-10 percussion works; the other 4 fonts in `wwwroot/soundfont` are fallbacks if ever needed).

## Step 5 — Core DrumGrooveDiagram spatial producer (drums twin of FretboardDiagram) + a JS DrumsR dumb-drawer SVG component, animated off the shared playback beat/position bus.

Core spatial producer + JS DrumsR component.

**Files**
- `src/ChordFlow.Core/Instruments/Drums/DrumGrooveDiagram.cs` — `DrumGrooveDiagram` (drums twin of `FretboardDiagram`) + `DrumGrooveLaneRow` + `DrumGrooveHit`. `Build(groove)` transposes the bar-major groove into voice-major rows (one per distinct voice, first-seen order), hits tagged with `(Bar, Tick)`; carries `BarCount`/`BeatsPerBar`/`TicksPerBar`.
- `src/ChordFlow.Desktop/wwwroot/drums-render-component.js` — `window.ChordFlowDrums` (DrumsR), a dumb SVG drawer: `create(container, {theme}) → { render(model), highlightCell(bar, beat), clearHighlight, setTheme, dispose }`. Rows × time-axis grid, per-voice colours, beat/bar gridlines + bar numbers, a playback marker band. Zero music theory in JS (C1).
- `tests/ChordFlow.Core.Tests/Instruments/Drums/DrumGrooveDiagramTests.cs` — 4 tests (voice-major rows, geometry, bar-relative onsets, multi-bar bar tagging). **64 drums tests + full suite green.**
- `loom/refs/chordflow-domain-model-reference.md` §3b — `DrumGrooveDiagram` row (IN8).

**Verified the drawer live (no full rebuild needed):** injected `drums-render-component.js` into the running app via CDP with a rock-beat model → **`hasSvg:true`, 12 hit circles (8 HH + 2 SD + 2 BD), 3 row labels HH/SD/BD, `highlightCell` marker visible**. The dumb drawer produces the correct grid.

**Deferred to step 6:** mounting DrumsR on the Content page and wiring its `highlightCell` to the shared playback beat/position bus.

## Step 6 — Wire a Drums surface into the Content page: author the hit-grid DSL, preview (score-only style), play, and see DrumsR animate in time.

The Drums dogfood page — author a groove, preview (percussion score + grid), play, and see DrumsR animate in time.

**Shape decision:** built as a new **Drums nav view** (like Scales/CAGED), not folded into the Content entity CRUD — the preview is stateless (DSL → tex + grid) so it needs no store, keeping step 6 self-contained. Folding it into Content-as-an-entity-kind is left to step 7 (persistence/CRUD) to revisit; the preview guts stay the authoring surface either way.

**C# (Features/Drums vertical slice + one preview verb)**
- `src/ChordFlow.Core/Features/Drums/DrumGroovePreviewHandler.cs` — `Preview(dsl, tempo)`: one parse → two projections (`DrumGrooveRenderer` tex + `DrumGrooveDiagram.Build`), so score/grid/marker can't drift. Fail-loud `FormatException`.
- `src/ChordFlow.Core/Features/Drums/DrumGrooveEnvelopes.cs` — `DrumPreviewEnvelope(Tex, Diagram)` + `DrumPreviewErrorEnvelope(Message)`.
- `WebMessageRouter.cs` — `DrumPreviewRequested` event + inbound `drumPreview` case (reuses the existing `Dsl`/`Tempo` envelope fields).
- `Program.cs` — handler + wiring (bad DSL → `drumPreviewError`, mirrors the scale/CRUD parse-error path).

**JS**
- `wwwroot/drums.js` (`window.ChordFlowDrumsView`) — DSL editor + tempo, debounced `drumPreview`; on reply renders DrumsR (grid) + loads the tex into a shared **ScoreR** (player); wires the engine's time-linear `"position"` clock (bar/quarterBeat 1-based → DrumsR `highlightCell` 0-based) so the grid marker tracks the audio; stop/finished clears it.
- `index.html` — Drums nav button + `#drums-view` (DSL textarea, tempo, error, grid mount, score mount) + script includes (`drums-render-component.js`, `drums.js`).
- `app.js` — registered the `drums` view in the `views` toggle map.

**Verified live via CDP** (rebuilt Desktop, relaunched, drove the page): switching to Drums fires the round-trip → `err:""`, **12 grid hits (HH/SD/BD)**, ScoreR rendered a **percussion staff (`isPercussion:true`, 12 notes)**; with `--play` the DrumsR marker animated (`x 84 → 124`, moved:true). Full Core suite **1122 passed, 0 failed**.

**Deferred to step 9:** the architecture-ref narrative (DrumsR, the Drums page, the `drumPreview` verb, the percussion render path) — bundled there per the plan.

## Step 7 — DrumGrooveEntity + migration + DrumGrooveStore : IContentStore (with genre/subgenre/tags catalog metadata), wired into the bridge entity* CRUD family and the shared editor. Stored form is the hit-grid DSL string only.

Persistence + CRUD — the 5th content kind, surfaced as a saved-grooves library on the Drums page.

**Surface decision (chat-001):** drums are authored on the standalone Drums page, NOT folded into the harmony-oriented Content editor (a groove has no comping/key/feel/tonality/sheet, so it doesn't fit that editor's chrome). "Shared editor" (req IN6) is satisfied by the shared **entity* CRUD family + the uniform `IContentStore`**, not the Content-page UI. Req IN6 amended (v2) to record this, re-locked.

**C# (persistence core — path-independent)**
- `Persistence/Entities/DrumGrooveEntity.cs` — `ICatalogEntity` (genre/subgenre/tags) + meter columns; hit-grid `Dsl` only.
- `Persistence/DrumGrooveStore.cs` — `IContentStore`: List/Get/Save/Delete + `Find`. Mirrors `ProgressionStore` (catalog-header preserve on save, strip on Get) + `RhythmPatternStore` (meter). Fork-on-edit, fail-loud parse.
- `ChordFlowDbContext` — `DrumGrooves` DbSet + composite (Id,Origin) PK config; EF migration `Persistence/Migrations/…_AddDrumGrooves` (applied on startup via `Migrate()`).
- `ContentEntity.Drums` + `ContentEntities.Parse("drums")` + `ContentCrudHandler.StoreFor(Drums)` → `DrumGrooveStore`. **No new bridge wiring** — the existing generic `entity*` router events already route `drums` to the store.
- `tests/…/DrumGroovePersistenceTests.cs` — 6 tests (round-trip, list source, header strip on Get, header preserved on in-place edit, delete, fail-loud). Full Core suite **1128 passed, 0 failed**.

**JS (library on the Drums page)**
- `drums.js` — a saved-grooves list via the `entity*` family (`entityList`/`entityGet`/`entitySave`/`entityDelete`, entity "drums"): Save/New/Delete + a clickable list (× to delete user rows, pack rows read-only → fork on save). `editingId` tracks the row; a fork returns a new user id.
- `index.html` — Name field + Save/New/Delete + `#drumList` in the Drums view.

**Verified live via CDP:** saved a uniquely-named groove → it listed → loaded back into the editor (name round-tripped) → deleted → list empty. Persists in the real SQLite DB (migration applied on startup).

## Step 8 — Ship rock / blues shuffle / jazz swing / funk grooves as drums/*.dsl in the on-disk default pack, imported through the normal PackReader/PackImporter path.

Default-pack starter grooves — ship rock / blues-shuffle / jazz-swing / funk through the normal pack path.

**Files**
- `src/ChordFlow.Core/Content/default-pack/drums/{rock,blues-shuffle,jazz-swing,funk}.dsl` — `name:` + `genre:` header + the hit-grid body. Copied to output by the existing `Content\**\*` csproj glob.
- `Features/Packs/ContentKind.cs` — `ContentKind.Drums` + `Folder()` → `drums` + added to `All`, so `PackReader` (which walks `ContentKinds.All`) auto-reads the `drums/` folder — no reader change.
- `Features/Packs/PackImporter.cs` — `case ContentKind.Drums` → `UpsertCatalog(_db.DrumGrooves, …, NewDrums)` (drums are a catalog entity; meter defaults 4/4 on the entity) + a `ReconcileOrphans(_db.DrumGrooves, …)` line so the pack stays authoritative.
- `tests/…/DefaultPackDrumsTests.cs` — 6 tests: all 4 grooves import as `Package` source, each `Find` re-parses into a valid groove, and rock lands kick/snare/hi-hat on the right beats. Full Core suite **1134 passed, 0 failed**.

**Verified live via CDP:** the Drums library lists all four with the "ChordFlow Starter" pack badge; loading Jazz Swing populates the editor + renders the grid (RD/PH/BD, 9 hits). Import is idempotent on startup (`DefaultPack.ImportInto`).

**Note:** `name:` is peeled by `PackDefinitionFile`; `genre:`/`subgenre:` stay for denormalization and are stripped again by `CatalogHeader.Parse` on load — the grid rows (`HH :3 …`) are never mistaken for header lines (the key before `:` isn't a recognized header key).

## Step 9 — Update the architecture ref (Instruments/Drums, DrumsR, the 5th content kind, the percussion render path) and run the full slice end-to-end (author → store → preview → play → animate).

Architecture-ref sync + final end-to-end.

**Ref (`chordflow-architecture-reference.md`, IN8):**
- §2 project tree — added `Instruments/Drums/`, `DrumGrooveRenderer` in Rendering, the `Drums` Features slice, and `drums-render-component.js` + `drums.js` in wwwroot; nav now "Practice ⇄ Content ⇄ Drums ⇄ Scales".
- §3 — new **"Drums — the first-class 2nd instrument"** subsection: the standalone slice across Instruments/Drums, the concrete `DrumGrooveRenderer`, the `drumPreview` Features slice, the 5th content kind (`DrumGrooveStore` + `entity*` + default-pack grooves), and the Drums page (editor + library + percussion ScoreR + animated DrumsR).
- §5 bridge — added the `drumPreview` verb + the `entity:"drums"` 5th content kind.

**Final e2e (CDP, fully-integrated page):** Drums page → loaded the **Funk** pack groove → **21 grid hits, `isPercussion: true`** percussion score → play → **marker animated** (`x 84 → 124`). Combined with the step-4 audible confirmation (Rafa) and the step-7 save/list/load/delete round-trip, the whole author → store → preview → play → animate slice works. Full Core suite **1134 passed, 0 failed**.

The basic-drums MVP is complete: drums are ChordFlow's first-class 2nd instrument — a standalone groove authored in a hit-grid DSL, rendered to a percussion track + an animated grid, persisted as the 5th content kind with starter grooves. Deferred work is tracked (drums-under-a-song, instrument-rendering, drums-accent-ghost).
