---
type: design
id: de_01KY06N5VJJC3VN7GTNNFNMMT1
title: Editable genre/subgenre/tags — widening the editor-authoritative override
status: done
created: 2026-07-20
version: 1
idea_version: 1
tags: []
parent_id: id_01KY03B4BMNFT8QKXP39FBZ7XE
requires_load: []
---
# Editable genre/subgenre/tags — widening the editor-authoritative override

## 1. Context

`filter-toggle-buttons` **surfaces** genre/subgenre/tags (`CatalogMetadata`: genre/subgenre/tags/description/tonality) onto `ContentSummary` → the `entityList` wire row, and filters on them client-side. But **authoring** them stays out of the UI — its **EX3**: metadata is edited only by hand-writing the DSL header. This thread **reverses that EX3** for the four metadata-bearing entities (Progression / Song / Voicing / **Drums**), making genre/subgenre/tags first-class editable fields. Rhythm carries no catalog metadata (EX3 of `rhythm-catalog-metadata`) and is untouched.

The plumbing is already in place — verified in code:

- `CatalogMetadata` + `CatalogHeader.Serialize/Parse` round-trip the header (`Persistence/CatalogMetadata.cs`, `CatalogHeader.cs`).
- Every catalog store's `Save` **preserves** the source header but **cannot change** it — the literal EX3 guard (`ProgressionStore.Save` / `DrumGrooveStore.Save`: *"Metadata isn't edited here (EX3) but must NOT be destroyed"*). Metadata carried through is the in-place row's own header, else the forked-from source's.
- `ICatalogEntity` (Progression / Song / Voicing / **Drums** entities) already declares denormalized `Genre` / `Subgenre` / `Tags` columns — but `Save` never populates them and `List()` still re-parses the header (`ProgressionStore.List` line 34, `DrumGrooveStore.List` line 31). The columns are vestigial today. *(The `filter-toggle-buttons` design claimed `List()` would read columns; the shipped code parses headers, so the columns stayed empty.)*
- `ContentItem` / the `entityList` reply **already carries** `genre` / `subgenre` / `tags` per row — so datalist suggestion values are already on the client.

**The decisive precedent — the `tonality` control.** `IContentStore.Save(id, name, dsl, sourceId, Tonality? tonality)` already takes **one editor-authoritative override**, threaded end-to-end: per-entity JS flag (`tonality: true`) → `ccTonality` control → `entitySave` envelope field → `WebMessageRouter` `EntitySaveRequested` → `ContentCrudHandler.ParseTonality` → the store merges it: `meta = preserved; if (tonality is chosen) meta = meta with { Tonality = chosen }` (editor value wins, absent keeps preserved). This design **widens that exact override** to genre/subgenre/tags rather than inventing a new mechanism.

## 2. Goal & non-goals

**Goal:** edit genre/subgenre/tags in the Content CRUD editor — pick an existing value or type a new one — for Progression / Song / Voicing / Drums, so a user classifies their own content and it immediately appears in the Content + Practice filters.

**Non-goals / deferred:**
- **Rhythm** — no catalog metadata (EX3); its editor shows no metadata controls.
- **Switching `List()` to read the denormalized columns** — *scope option A (chosen)*: this thread starts **populating** the columns on `Save` (they exist, empty = a smell), but `List()` keeps parsing headers, which already works. The read-switch (retire header-parse on the list path + backfill existing rows) is a separate perf cleanup — its own thread.
- **A "distinct catalog values across all entities" query verb** — datalist values are discovered **client-side** from the already-loaded `entityList`. The cross-entity verb stays additive if ever wanted.
- **Description / tonality editing** — out of scope; description keeps riding the header untouched, tonality keeps its own existing control.

## 3. Mechanism — widen the override (four fields, one pattern)

### 3.1 Save path (C#)

- **`IContentStore.Save`** gains an optional authoritative metadata argument — a small `CatalogMetadataPatch(string? Genre, string? Subgenre, IReadOnlyList<string>? Tags)` (or three optional args). **When supplied**, it is authoritative for those three fields; when null (rhythm, or any programmatic save), behavior is exactly as today (preserve source header).
- **Merge** (each catalog store, mirroring the tonality merge): start from the preserved `meta` (in-place row's own header, else forked-from source's), then overlay the patch field-wise via `record with`:
  `meta = meta with { Genre = patch.Genre, Subgenre = patch.Subgenre, Tags = patch.Tags }`.
  This **keeps `Description` and `Tonality`** from the preserved meta (tonality still overridden by its own control) — only the three edited fields are replaced.
- **Clear vs. absent:** for a metadata-bearing entity the editor controls are always present and authoritative, so they always send the patch. A **present-but-empty** field (blank genre input, empty tag list) therefore **clears** that field; **absent** (patch == null) only happens for rhythm / programmatic callers and preserves. This is the clean distinction — no "null means both keep and clear" ambiguity.
- **Denormalized columns (option A):** in each catalog store's add + update branch, write `entity.Genre/Subgenre/Tags` from the **final** merged `meta` (tags serialized with the same `CatalogHeader` tag encoding the columns expect). The header stays canonical; the columns finally become truthful. `List()` is **not** changed.

### 3.2 Bridge + handler

- **`entitySave` inbound envelope** (`WebMessageRouter`) gains `genre` / `subgenre` / `tags[]`. `EntitySaveRequested` widens to carry them.
- **`ContentCrudHandler.Save`** passes them into `StoreFor(kind).Save(...)` — the metadata-bearing entities forward the patch; rhythm receives null (its editor never sends them). Analogous to the existing `ParseTonality` hop.

### 3.3 Seeding the editor (no load-path change)

The editor seeds its metadata controls from the **clicked `entityList` row**, which already carries `genre`/`subgenre`/`tags` — exactly how the tonality control seeds from the row's `InitialKeyIsMinor` (`content-crud.js` `pendingSeeds`). So **no change to `Get` / `entityLoaded`** — that path keeps returning the header-stripped body.

### 3.4 UI controls (`content-crud.js`)

- A per-entity **`metadata: true`** flag (mirroring `tonality: true`) gates a metadata block; shown for Progression / Song / Voicing / Drums, hidden for Rhythm.
- **Genre / Subgenre:** `<input list="…">` + `<datalist>`. Datalist options = **distinct present values discovered client-side** from the current `entityList` (across rows); typing a new value is accepted and becomes a suggestion once saved (it's in the next `entityList`).
- **Tags:** a **pill editor** — add several (each from the tags datalist or typed fresh), remove individually.
- On save, the envelope carries the current control values (like `tonality: current.tonality ? tonalityEl.value : undefined`).

### 3.5 Fork-on-edit

Editing metadata on a **package** item forks a user copy (`content-source-model` — same as any edit); the fork carries the *edited* metadata, not the source's. Confirmed intended UX for re-tagging a pack item.

## 4. Doc/ref sync (same unit of work)

- **`chordflow-architecture-reference.md`** — the `entitySave` envelope + `IContentStore.Save` contract now carry editable catalog metadata; update the bridge-contract + persistence notes (ref-sync rule).
- **`ICatalogEntity` XML summary** — currently says "Implemented by `ProgressionEntity`, `SongEntity` and `VoicingEntity`"; add `DrumGrooveEntity` (it implements the interface, correctly). Small nit, fixed in the same change.

## 5. Sequencing

1. **Core store contract** — `IContentStore.Save` + `CatalogMetadataPatch`; each catalog store merges the patch (overlay g/s/t, keep description/tonality) and populates the denormalized columns from the final meta. Rhythm store accepts null inertly. Store-level tests.
2. **Bridge** — `entitySave` envelope + `EntitySaveRequested` + `ContentCrudHandler.Save` carry g/s/t through. Router test.
3. **Editor UI** — the `metadata`-gated block: genre/subgenre datalist inputs (values discovered from `entityList`) + tags pill editor; seed from the clicked row; send on save.
4. **Ref sync** — `chordflow-architecture-reference.md` + the `ICatalogEntity` doc nit.

## 6. Testing

- **Core:** each catalog store's `Save` — (a) an editor patch writes g/s/t into both the stored header *and* the denormalized columns; (b) `Description`/`Tonality` survive a metadata edit; (c) a present-but-empty patch clears; (d) a null patch preserves the source header (today's behavior); (e) rhythm store unaffected. Round-trip: save with genre "Blues"/subgenre "Shuffle"/tags [12-bar] → re-`List()` shows those values.
- **Bridge:** the `entitySave` envelope round-trips g/s/t to the handler.
- **JS/dogfood:** author a new user progression with metadata → it round-trips and immediately shows under those chips in the Content + Practice filters; the datalist offers every value already present; a brand-new value is accepted and then suggested for the next item.
