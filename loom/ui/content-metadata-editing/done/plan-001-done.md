---
type: done
id: pl_01KY07MWVRATS2GFKWACQR79J1-done
title: Done — Editable genre/subgenre/tags — implementation
status: done
created: 2026-07-20
version: 4
tags: []
parent_id: pl_01KY07MWVRATS2GFKWACQR79J1
requires_load: []
---
# Done — Editable genre/subgenre/tags — implementation

## Step 1 — Add an authoritative `CatalogMetadataPatch(Genre, Subgenre, Tags)` and thread it through `IContentStore.Save`; each catalog store overlays the patch onto the preserved header via `record with` (keeping Description + Tonality) and writes the final genre/subgenre/tags into the denormalized `ICatalogEntity` columns; the Rhythm store accepts a null patch inertly. A present-but-empty patch clears; a null patch preserves as today. Store-level tests.

**Store contract + merge + column population — done.**

- **`Persistence/CatalogMetadata.cs`** — added `CatalogMetadataPatch(Genre, Subgenre, Tags)` with `ApplyTo(preserved)`: overlays the three fields via `record with`, keeping Description + Tonality (C4); blank genre/subgenre normalize to null (present-but-empty clears — IN9).
- **`Persistence/IContentStore.cs`** — `Save` gains `CatalogMetadataPatch? metadata = null` (optional, so existing 6-arg callers are untouched) + doc paragraph. Updated the `ContentSummary` note that used to say columns "aren't populated on user saves".
- **`ProgressionStore` / `SongStore` / `VoicingStore` / `DrumGrooveStore`** — each `Save` now: `preserved = in-place row's header ?? forked-from source's`; `meta = metadata is not null ? patch.ApplyTo(preserved) : preserved` (Progression still applies the `tonality` override on top); writes the header via `CatalogHeader.Serialize` **and** populates the denormalized `Genre`/`Subgenre`/`Tags` columns from the final `meta` (option A) in both the add and update branches. Tonality stays inert on Song/Voicing/Drums (EX4).
- **`RhythmPatternStore.Save`** — accepts the `metadata` param inertly (`_ = (sourceId, tonality, metadata)`); no columns (rhythm carries no catalog metadata — EX1).
- Refreshed the stale "metadata isn't edited here (EX3)" comments in each store to describe the patch overlay.

**Tests** — `tests/ChordFlow.Core.Tests/ContentCrudStoreTests.cs`: 7 new — patch writes header + columns (Progression); keeps preserved tonality + description (C4); empty patch clears fields + columns (IN9); null patch preserves the source header **and** now populates the fork's columns from it; plus header+columns round-trips for Song, Voicing, Drums.

**Verification:** `dotnet build` clean (1 pre-existing warning); full Core suite green — **1156 passed, 0 failed**.

## Step 2 — Extend the `entitySave` inbound envelope + the `EntitySaveRequested` event + its `Program.cs` subscription + `ContentCrudHandler.Save` to carry genre/subgenre/tags into the store patch — mirroring the existing `tonality` hop. Router test asserting the envelope round-trips the fields to the store.

**Bridge threads genre/subgenre/tags through — done.**

- **`Bridge/WebMessageRouter.cs`** — `EntitySaveRequested` widened to `Action<…, string? genre, string? subgenre, IReadOnlyList<string>? tags>`; the `entitySave` dispatch passes `envelope.Genre/Subgenre/Tags`; the `InboundEnvelope` record gains `string? Genre, string? Subgenre, IReadOnlyList<string>? Tags` (array field deserializes like the existing `Sources`/`Families` filter fields).
- **`Features/ContentCrud/ContentCrudHandler.cs`** — `Save` gains `genre/subgenre/tags` params + a `MetadataPatch(...)` builder: returns a `CatalogMetadataPatch` when any field is non-null (the JS sends all three — even empty — for metadata entities, so an all-empty patch clears, IN9), else `null` (rhythm sends none → no patch, EX1). Mirrors the existing `ParseTonality` hop.
- **`Desktop/Program.cs`** — the `EntitySaveRequested` subscription lambda widened to forward the three fields into `contentCrud.Save(...)`.

**Tests** — `WebMessageRouterContentTests.cs`: updated the two existing `EntitySave` lambdas to the 9-arg shape; added 2 — `genre/subgenre/tags` round-trip through the envelope (IN5), and rhythm/absent-metadata dispatches null (EX1).

**Verification:** build clean; router + content-CRUD tests green — **83 passed, 0 failed**.

## Step 3 — Add a `metadata`-gated editor block (per-entity flag mirroring `tonality: true`): genre/subgenre `<input>`+`<datalist>` whose options are distinct present values discovered client-side from the current `entityList`, and a tags pill editor (add from datalist or type; remove individually). Seed the controls from the clicked list row (no load-path change); send the values on save. Shown for Progression/Song/Voicing/Drums, hidden for Rhythm.

**Editor UI — done across both editors (scope decision A: Drums is a separate page).**

**Discovery:** the four metadata-bearing entities aren't all in one editor — Progression/Song/Voicing live in `content-crud.js`, but Drums is a separate CRUD page (`drums.js` + its `index.html` markup). Rafa chose **A** (fold drums in now), so step 3 expanded to both editors + a shared component.

- **NEW `wwwroot/metadata-editor-component.js`** — `ChordFlowMetadataEditor`, a dumb reusable control (the editing twin of FilterR): genre/subgenre `<input>`+`<datalist>` + a tags pill editor (add via Enter/comma/datalist-pick/blur, case-insensitive de-dupe, Backspace-on-empty removes last, per-pill ×). API: `setSuggestions(items)` (client-side distinct discovery — IN4), `seed({genre,subgenre,tags})` (IN7), `getValues()→{genre,subgenre,tags}` (IN5; commits a half-typed tag; empty = clear per IN9), `clear()`, `setEnabled(on)`.
- **`content-crud.js`** — `metadata:true` on progression/song/voicing (not rhythm — EX1); mounts the component in a gated `#ccMetadata` block; feeds suggestions from each `entityList`; seeds from the clicked row; disables it for package/automatic items; sends genre/subgenre/tags on save (omitted for rhythm).
- **`drums.js`** — mounts the same component in `#drumMetadata`; keeps a local `items` list so `entityLoaded` seeds from the clicked groove row; suggestions from the drums `entityList`; sends the patch on save; clears on New.
- **`index.html`** — `<script src="metadata-editor-component.js">` (before content-crud.js); a `#drumMetadata` mount in the drums markup; scoped `.cf-metadata*` / `.cf-tag-*` styles beside the tonality-control styles.

**Verification:** `node --check` clean on all three JS files; full solution build clean. **Visual dogfooding of the two pages is the remaining check** (typing/removing tags, datalist suggestions, round-trip into the filters) — flagged for a run-through.

## Step 4 — Update `chordflow-architecture-reference.md` for the editable-metadata contract (the `entitySave` envelope + `IContentStore.Save` now carry an authoritative metadata patch; columns populated on save). Fix the `ICatalogEntity` XML summary to list all four implementers (add `DrumGrooveEntity`).

**Ref sync + doc nit — done.**

- **`Persistence/Entities/ICatalogEntity.cs`** — XML summary now lists all four implementers (`ProgressionEntity`, `SongEntity`, `VoicingEntity`, `DrumGrooveEntity`) and notes the columns are populated on save while `List()` still reads the header (read-switch deferred).
- **`loom/refs/chordflow-architecture-reference.md`** (via `loom_patch_doc`) — the bridge §5 content-CRUD passage now documents the editable-metadata contract: `entitySave` carries the authoritative genre/subgenre/tags patch for progression/song/voicing/drums (not rhythm), `WebMessageRouter → ContentCrudHandler.Save` builds a `CatalogMetadataPatch` overlaid onto the header + written to the denormalized columns (List() still header-reads; deferred), present-but-empty clears / absent preserves, and the shared `ChordFlowMetadataEditor` dumb component mounted by both the Content editor and the Drums page.

**Verification:** full solution build clean; full Core suite green — **1158 passed, 0 failed**.
