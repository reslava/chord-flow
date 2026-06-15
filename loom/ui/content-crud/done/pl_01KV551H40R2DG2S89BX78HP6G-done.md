---
type: done
id: pl_01KV551H40R2DG2S89BX78HP6G-done
title: Done — Content-definition CRUD UI — Plan
status: done
created: "2026-06-15T00:00:00.000Z"
version: 7
tags: []
parent_id: pl_01KV551H40R2DG2S89BX78HP6G
requires_load: []
---
# Done — Content-definition CRUD UI — Plan

## Step 1 — Store write path + new SongStore (List/Save/Delete) with (Id, Origin) tier-shadowing + canonicalization

**Store write path + new SongStore — done.** All 358 Core tests pass (13 new in `ContentCrudStoreTests`).

Files:
- **New `Persistence/IContentStore.cs`** — the uniform CRUD surface (`List`/`Get`/`Save`/`Delete`) + DTOs `ContentSummary(Id, Name, Origin, HasLowerTier)`, `ContentDoc(Id, Name, Dsl)`, `DeleteOutcome {NotFound, Deleted, Reverted}`, and the internal `ContentSummaries.Build` helper (collapses tiered rows to one winner per id via `OriginResolver`, flagging a lower tier).
- **`ProgressionStore` / `RhythmPatternStore` / `VoicingStore`** — now implement `IContentStore` alongside their existing read methods.
- **New `Persistence/SongStore.cs`** — none existed (only `SongEntity` + `SongParser`); implements `IContentStore`.

Design decisions made while implementing:
- **Tier law (C2):** writes only ever target the `(id, UserDefined)` row. New = fresh GUID id; editing a BuiltIn/Pack id writes a UserDefined **shadow** (lower row untouched); `Delete` removes only the UserDefined row → `Deleted` (user-only) or `Reverted` (a lower tier resurfaces). `HasLowerTier` on the summary lets the UI pick the "Delete" vs "Revert to default" label (IN13).
- **Validate-by-parse:** `Save` parses first and writes nothing on failure (`FormatException`). For songs, `Song.FromSections` raises `ArgumentException` for structural errors — `SongStore.Validate` normalizes that to `FormatException` so the CRUD parse-error surface (IN3) sees one exception type for all entities.
- **Voicing canonicalization (IN9):** `VoicingStore.Save` runs `VoicingDslParser.Parse → VoicingDslWriter.ToDsl`, so any authoring anchor folds to canonical-C before storage (test: a D-anchored open-C shape stores as `voicing Cmaj … x 3 2 0 1 0`).
- **EX3 (no metadata editing):** `Get` returns the header-stripped body as the editable DSL; `Save` stores the body only (catalog columns left empty). A consequence: editing a built-in that carried genre/tags produces a shadow without them — acceptable under EX3 (metadata is deferred; user content has none anyway).

## Step 2 — ContentCrud Features slice + generic bridge envelope family (entityList/Get/Preview/Save/Delete) + router verbs

**ContentCrud slice + generic bridge protocol — done.** 374 Core tests pass (16 new across `ContentCrudHandlerTests` + `WebMessageRouterContentTests`).

Files:
- **New `Features/ContentCrud/ContentEntity.cs`** — `ContentEntity` enum + `ContentEntities.Parse` (wire string → enum; unknown → `FormatException`).
- **New `Features/ContentCrud/ContentCrudEnvelopes.cs`** — the outbound family: `EntityListEnvelope`/`ContentItem`, `EntityLoadedEnvelope`, `EntityPreviewEnvelope` (kind `score`/`diagram`, `Diagram` field reserved for step 3), `EntityParseErrorEnvelope`, `EntitySavedEnvelope`, `EntityDeletedEnvelope`.
- **New `Features/ContentCrud/ContentCrudHandler.cs`** — maps entity→`IContentStore`; `List`/`Get`/`Save`/`Delete`/`Preview`. Raises `VoicingsChanged` on voicing save/delete (for step 7 / IN11).
- **`Bridge/WebMessageRouter.cs`** — added the five `entity*` inbound verbs + events; widened `InboundEnvelope` with `Entity`/`EntityId`/`Name`/`Dsl` (kept the existing `int? Id` for `loadExercise`).

Decisions while implementing:
- **Serialization constraint drove the API shape.** `IBridge.Send<T>` serializes by the static type `T`, so a slice method returning `object` would emit `{}`. Every slice method therefore returns a single concrete envelope type; the fail-able ones (`Save`, `Preview`) **throw `FormatException`** and the host builds the `entityParseError` envelope (mirrors the existing `TrySendScore` catch idiom). Wired in step 7.
- **Preview defaults (score entities):** progression → `Exercise(C, prog, Quarters, 80)`; rhythm → same on a single `1` chord (focus the timing); song → `SongExpander.Expand` (resolving refs against `ProgressionStore`) → `Render(RealizedSong, …)`. `Preview` wraps the whole build/render so any failure (parse, missing song ref, unrenderable chord, structural error) becomes one uniform `FormatException` (IN3).
- **Voicing preview** validates + canonicalizes now and returns `kind:"diagram"`; the `DiagramModel` is populated in step 3.
- **Host wiring deferred to step 7.** Step 2 is Core-only (slice + protocol + router). Subscribing `router.Entity*Requested` → handler → `bridge.Send` (with the `FormatException`→`entityParseError` catch) lands in `Program.cs` alongside the voicing live-refresh, keeping all host changes in one step.

## Step 3 — Voicing DiagramModel computed in Core (intervals/functions/spelling) + wire it into the voicing preview payload

**Voicing DiagramModel in Core — done.** New tests pass (4 diagram + the voicing-preview handler assertion).

Files:
- **`Domain/Fretboard.cs`** — added `PitchClassAt(string, fret)` (inverse of `PositionsFor`), the geometry needed to label a voicing's notes.
- **New `Domain/Voicings/DiagramModel.cs`** — `DiagramModel(FirstFret, BarreFret?, Strings)` + `DiagramString(String, State, Fret?, Note?, Interval?, Function?)`. `State` = muted/open/fretted; `Function` = root/third/fifth/seventh/tension (the color key); all as strings so STJ Web defaults serialize them readably (no enum-as-number).
- **New `Domain/Voicings/VoicingDiagram.cs`** — `Build(VoicingShape)`: per string, pitch class via `Fretboard.PitchClassAt`, interval = pitch class (canonical anchor is C), function by tertian position in `QualityIntervals` (or `tension` if outside the quality), role-aware label, spelled note via `NoteSpeller`.
- **`Features/ContentCrud/`** — `EntityPreviewEnvelope.Diagram` retyped `object?` → `DiagramModel?`; `VoicingPreview` now returns `VoicingDiagram.Build(shape)`.

Decisions:
- **Theory stays in Core (IN6/C1):** the JS renderer (step 6) draws the precomputed model; no music theory in JS.
- **Role-aware enharmonics:** chord tones get precise labels (dim7's pc 9 = `bb7`, aug's pc 8 = `#5`, m7b5's pc 6 = `b5`) by mapping the quality's tertian interval set to root/third/fifth/seventh; a note outside the quality falls back to a generic label and is colored `tension`.
- **Canonical-C anchor only (EX2):** the diagram is computed at C; a root-picker is a later add.

Engine side (steps 1–3) is complete and fully unit-tested; front-end is next.

## Step 4 — Front-end refactor: extract shared bridge.js module + add the single-page Practice⇄Content view toggle

**Front-end split + Practice⇄Content toggle — done.** `node --check` passes on bridge.js + app.js.

Files:
- **New `wwwroot/bridge.js`** — `window.ChordFlowBridge`: the shared WebView2 transport extracted from app.js. `send()` + `onReceive()` with **fan-out** (every registered handler sees every inbound message; each view ignores envelope types it doesn't own) — needed because both the Practice and Content views share one message channel.
- **`wwwroot/index.html`** — header gained a segmented `Practice | Content` nav; existing UI wrapped in `#practice-view`; added an empty `#content-view` (filled in step 5); loads `bridge.js` before `app.js`. Added nav + `[hidden]` CSS.
- **`wwwroot/app.js`** — dropped its inline `Bridge` IIFE for `window.ChordFlowBridge`; added `setupViewToggle()` exposing `window.ChordFlowViews.show(view)` (and lazily calling `window.ChordFlowContent.show()` on first Content view); its `onHostMessage` default case no longer warns (other views' envelopes now fan out to it).

No behavior change to the Practice view; vanilla JS, no build step (C6). No JS test runner in the repo — verified by `node --check`.

## Step 5 — Generic content editor (content-crud.js): entity picker, list with origin badges, name+DSL fields, live parse error, score-preview strategy, Save + Delete/Revert

**Generic editor + score preview — done.** `node --check` passes.

Files:
- **New `wwwroot/content-crud.js`** — `window.ChordFlowContent`, one component driven by an `ENTITIES` config table (key/label/previewKind/placeholder/help). Renders entity tabs, the definitions list (origin badges), name + DSL fields, inline parse-error line, Save / Delete / + New, and a preview pane. Lazy-inits on first Content view show.
- **`wwwroot/index.html`** — added the `#content-view` editor CSS and the `content-crud.js` + `chord-diagram.js` script tags.

Behavior:
- Debounced (300 ms) DSL input → `entityPreview`; **score strategy** renders the returned alphaTex in a lazily-created preview alphaTab instance; **diagram strategy** delegates to `window.ChordFlowDiagram` (step 6), guarded for absence.
- `entityList` → list with origin badges; `entityLoaded` → populate + preview; `entityParseError` → inline message + clear preview; `entitySaved`/`entityDeleted` → status + list refresh.
- **IN13 Delete/Revert:** the destructive button reads the selected item's summary — enabled only for a `UserDefined` row, labelled "Revert to default" when a lower tier exists, else "Delete"; disabled for BuiltIn/Pack or a new unsaved item.

`chord-diagram.js` is referenced now (created in step 6).

## Step 6 — SVG fret-box renderer (chord-diagram.js) + the voicing diagram-preview strategy

**SVG voicing fret-box — done.** `node --check` passes.

Files:
- **New `wwwroot/chord-diagram.js`** — `window.ChordFlowDiagram.render(container, model)`. Pure presentation (no theory — IN6): maps `function` → color, draws a fret-box (nut for open position or an `Nfr` position label otherwise), colored dots with the interval/note label inside for fretted strings, colored `○` rings for open strings, grey `✕` for muted, a function-colored bottom label for every sounding string (so open strings are labeled too), an optional barre bar, and an interval-color **legend**.
- **`wwwroot/content-crud.js`** — already delegates the voicing `diagram` preview strategy here (guarded on `window.ChordFlowDiagram`).

Behavior: an **Intervals ⇄ Notes** toggle re-renders the labels; colors: root=red, 3rd=blue, 5th=green, 7th=purple, tension=grey. Consumes the `DiagramModel` JSON straight from `entityPreview`.

Front-end (steps 4–6) is complete; only the host wiring (step 7) remains to make it run end-to-end.

## Step 7 — Voicing live-refresh wiring in Program.cs (rebuild VoicingBook + AlphaTexRenderer on voicing save/delete)

**Host wiring + voicing live-refresh — done.** Full solution builds (only the pre-existing WindowsBase warning); 378 Core tests pass.

Files:
- **New `Desktop/WebHost/SwappableRenderer.cs`** — an `IScoreRenderer` wrapper with a hot-swappable inner. The generator, library, and content-CRUD preview all share this one instance.
- **`Desktop/Program.cs`** — the renderer is now a `SwappableRenderer`; added a `ContentCrudHandler`; subscribed `contentCrud.VoicingsChanged` to reload `VoicingStore.LoadShapes()` and `Swap` in a fresh `AlphaTexRenderer` (IN11); wired the five `entity*` router events → handler → `bridge.Send`, with `FormatException` → `entityParseError` (preview/save/delete) or a status line (list/get), and save/delete echoing a list refresh.

Decisions:
- **One swappable renderer** (vs reassigning multiple captured locals): generate/library/contentCrud hold the same instance, so a single `Swap` updates every consumer's voicing book without a restart — only voicings are snapshotted, so only they need it (EX1: progression/song/rhythm are read per-use and not yet consumed by the generator).
- **`EntityGet` uses two concrete `Send` calls** (loaded vs not-found status) rather than a ternary-to-`object`, keeping serialization unambiguous.

All 7 steps complete. Reference docs (architecture + domain-model) updated next in the same unit of work.
