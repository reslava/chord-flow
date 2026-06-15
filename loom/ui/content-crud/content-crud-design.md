---
type: design
id: de_01KV54ENW26AVDKP72VKY39ZEK
title: Content-definition CRUD UI — Design
status: done
created: "2026-06-15T00:00:00.000Z"
updated: 2026-06-15
version: 3
tags: []
parent_id: id_01KV05AZ7T77CMGM86X7T7GZRB
requires_load: []
---
# Content-definition CRUD UI — Design

One uniform CRUD surface for every DSL-backed content entity — **Progression, Song,
RhythmPattern, Voicing** — built once and parameterized by type. Engine + persistence
are mostly read-only today; this thread adds the **write path** and the **front-end**.

> Refs consulted: `chordflow-architecture-reference` (bridge contract, where code belongs),
> `chordflow-domain-model-reference` (entities, stores, OriginResolver, voicing types),
> `chordflow-dsl-reference` (all four DSLs + catalog header). Decisions came out of
> `content-crud-chat-001`.

---

## 1. Decisions locked (from the chat)

| # | Decision | Rationale |
|---|----------|-----------|
| 1 | **Custom SVG fret-box** for the voicing diagram (no alphaTab `\chord`) | alphaTab's Chord model is `name`/`firstFret`/`strings`/`barreFrets` + 3 booleans — **no per-dot color, interval, or note labels**, and it's score-coupled. It can't reach the target (intervals-in-color, note names), so the "fast first pass" would be throwaway ([[design-philosophy-durable-over-minimal]]). |
| 2 | **One generic bridge envelope family** (`entity*`) with an `entity` discriminator | Fits the existing flat-typed-envelope style; avoids ~20 envelope types (5 entities-ops × 4). |
| 3 | **One generic editor component** + a per-entity **preview strategy** | The only real divergence between entities is the preview (score vs diagram). |
| 4 | After a successful **voicing** save/delete, **rebuild the in-memory snapshot + renderer** | Voicings are snapshotted once at launch (`Program.cs`); without a rebuild the book is stale. |
| + | **Store write path is in-scope** (there's no CRUD without it), incl. `(Id, Origin)` tier-shadowing | The idea understated this — persistence is read-only today. |

---

## 2. What actually exists today (grounding)

- **Stores are read-only.** `ProgressionStore.Find`, `RhythmPatternStore.Find`,
  `VoicingStore.LoadShapes`/`Find`. **No Create/Update/Delete anywhere.**
- **No `SongStore` at all.** `SongEntity` + `SongParser`/`SongExpander` exist, but nothing
  loads/saves a Song by id. Song CRUD = a new store from scratch.
- **The exercise generator is hardcoded.** `GenerateExercise` uses `SeedData.TwelveBarBlues`
  + the 3 seed rhythms — it does **not** read the progression/rhythm stores. So authored
  progressions/rhythms are **stored but not yet consumed** by the generator (wiring them in
  is a separate thread — exercise-workbench / exercises-definition-ui). **Voicings are the
  exception:** they already feed `AlphaTexRenderer` via the launch snapshot → hence decision 4
  applies to voicings specifically.
- **Front-end is one monolithic `app.js`** (IIFE) + `index.html`: a single "practice" view
  (key/rhythm pickers, transport, saved-exercise list). No router, no build step, vanilla JS,
  WebView2 `chrome.webview` transport feature-detected in a `Bridge` module.
- **Catalog header** (`genre:`/`subgenre:`/`tags:`) is split off the DSL body by
  `CatalogHeader.Parse` before the pure parser sees it (Progression/Song/Voicing; Rhythm has none).

---

## 3. Architecture & data flow

```
[Content view in wwwroot]
  editor (name + DSL textarea)
    → entityPreview(entity, dsl)  ──► Core: parse → preview payload | parse error
                                         · score entities → alphaTex (preview defaults)
                                         · voicing        → DiagramModel (theory in Core)
    ← preview payload / error
  Save → entitySave(entity, id?, name, dsl) ──► Core: validate → write UserDefined row
                                         ← saved id + refreshed list
  list → entityList(entity) ──► Core: rows (id, name, origin) per resolved tier
  Delete → entityDelete(entity, id) ──► Core: drop UserDefined row (delete | revert)
```

All theory stays in Core (intervals, spelling, canonicalization); JS is a dumb renderer —
consistent with "the engine knows the music, the host renders it."

New code lands in: a **`ContentCrud` Features slice** (Core), **store write methods** +
a new **`SongStore`** (Persistence), one **outbound DTO set** + **router verbs** (Bridge),
and a **content view** in `wwwroot`.

---

## 4. The generic bridge protocol

Five inbound verbs, each carrying an `entity` discriminator
(`progression|song|rhythm|voicing`). Inbound envelope (extends the `WebMessageRouter`
`InboundEnvelope` record / switch):

| `type` | Payload | C# event | Returns (outbound) |
|--------|---------|----------|--------------------|
| `entityList` | `entity` | `EntityListRequested(entity)` | `entityList` `{entity, items:[{id,name,origin}]}` |
| `entityGet` | `entity, id` | `EntityGetRequested(entity,id)` | `entityLoaded` `{entity, id, name, dsl}` |
| `entityPreview` | `entity, dsl` | `EntityPreviewRequested(entity,dsl)` | `entityPreview` (below) **or** `entityParseError {entity, message}` |
| `entitySave` | `entity, id?, name, dsl` | `EntitySaveRequested(...)` | `entitySaved {entity, id}` + an `entityList` refresh; or `entityParseError` |
| `entityDelete` | `entity, id` | `EntityDeleteRequested(entity,id)` | `entityDeleted {entity, id}` + an `entityList` refresh |

`entityPreview` outbound payload is strategy-shaped:
- **score** (progression/song/rhythm): `{entity, kind:"score", tex, tempo}` — preview alphaTex.
- **voicing**: `{entity, kind:"diagram", diagram: DiagramModel}` (§6).

The discriminator keeps it one flat family; the `InboundEnvelope` record gains
`Entity`, `Name`, `Dsl` (`Id` already exists, widened to `string?` since content ids are
slugs/GUIDs, not the int exercise id — **note:** today `Id` is `int?` for `loadExercise`;
we add a separate `EntityId` string field rather than overload it).

---

## 5. Engine: store write path + tier-shadowing

Each store gains `List()`, `Save(...)`, `Delete(id)` (+ build `SongStore` new). The **canonical
storage form differs by entity** and the design must respect it:

- **Voicing** — `Save` runs `VoicingDslParser.Parse` → `VoicingDslWriter.ToDsl` to store the
  **canonical-C** form (any anchor the user typed collapses to one row). Validation = the parse.
- **Progression / Song / Rhythm** — no writer; the **typed DSL is the canonical form**. `Save`
  validates by parsing (`ProgressionParser` / `SongParser` / `RhythmPatternParser`), then stores
  the string as-is (after `CatalogHeader.Parse` round-trip for the three that carry one).

### Tier semantics under the composite `(Id, Origin)` PK

`OriginResolver`: `UserDefined > Pack > BuiltIn`. CRUD only ever writes the **`UserDefined`** tier:

| User action on entity X | DB effect |
|-------------------------|-----------|
| Create new | insert `(GUID, UserDefined)` |
| Edit a user-authored X | update the `(id, UserDefined)` row |
| Edit a BuiltIn/Pack X | **insert/update a `(id, UserDefined)` shadow** — never touch the lower row |
| Delete a user-only X | hard-delete `(id, UserDefined)` → gone |
| Delete an X that shadows a lower tier | delete `(id, UserDefined)` → **lower tier resurfaces** (a *revert*) |

So "delete" has two meanings depending on whether a lower tier exists — see open question §10.1.
`entityList` returns the **resolved** rows (one per id, top tier) plus the `origin` so the UI can
badge BuiltIn/Pack/UserDefined and choose the right verb label.

---

## 6. Engine: the voicing DiagramModel (theory stays in Core)

The SVG needs intervals-in-color + note names — that is music theory, so Core computes a
**`DiagramModel`** DTO and JS only draws it:

```
DiagramModel {
  firstFret: int,            // diagram window nut position
  barreFret: int?,           // barre across, if any
  strings: [                 // 6 entries, low-E(6) → high-E(1)
    { string: int,
      state: "muted" | "open" | "fretted",
      fret: int?,            // absolute fret when fretted
      note: string?,         // spelled note name at C anchor (NoteSpeller)
      intervalLabel: string?,// "R","b3","5","b7"… relative to root
      function: "root"|"third"|"fifth"|"seventh"|"tension" // → color key in JS
    } ]
}
```

Computed from the canonical-C `VoicingShape`: each `FretPosition` → pitch class via
`Fretboard`/standard tuning → interval vs the root pitch class →
`ChordToneFunction`/`QualityIntervals` for the label + function; `NoteSpeller` for the note name.
**v1 shows the shape at its canonical C anchor** (movability is real but a root-picker is a later
add). The function→color map and the legend live in JS.

---

## 7. Front-end: generic editor + SVG fret-box

**Modest, durable refactor** of the monolith: extract the shared `Bridge` module, keep the
existing practice screen as one view, add the content screen as another, and gate them behind a
small view toggle in the header (Practice ⇄ Content). Files:

```
wwwroot/
  index.html          + a nav toggle + a #content-view container
  bridge.js           extracted shared transport module
  app.js              practice view (unchanged behavior, now imports bridge)
  content-crud.js     the generic editor component
  chord-diagram.js    the SVG fret-box renderer (DiagramModel → SVG)
```

**`content-crud.js`** — one component, configured by an `ENTITIES` table
`{ key, label, previewKind:"score"|"diagram", placeholder, helpText }`. It renders: an entity
picker, a list (with origin badges), a name field, a DSL textarea, a live parse-error line, a
preview pane, and Save / Delete (label = "Delete" or "Revert to default" per origin + lower tier).
On textarea input (debounced) → `entityPreview`; the preview strategy switches on `previewKind`:
- **score** → a small dedicated alphaTab instance renders the returned `tex`.
- **diagram** → `chord-diagram.js` draws the `DiagramModel`.

**Score preview defaults.** Progressions/rhythms aren't self-contained scores, so Core builds a
minimal preview `Exercise` with fixed defaults (key C, a default rhythm for progressions / a
single sustained chord for rhythms, default tempo) purely to visualize — these defaults are a
preview concern, never persisted.

**`chord-diagram.js`** — pure presentation: 6 strings × N frets grid, colored dots by
`function`, label toggle (interval ⇄ note name), barre bar, open `○` / muted `✕` markers, a
first-fret indicator, and an interval-color legend.

---

## 8. Live-refresh after save (decision 4)

Only voicings feed a live engine object (the launch snapshot →
`VoicingBook` → `AlphaTexRenderer`). Today `Program.cs` captures `renderer`/`generate`/`library`
in closures built once. To rebuild on a voicing change without leaking persistence into the host:

- The `ContentCrud` slice raises a **`VoicingsChanged`** signal after a successful voicing
  save/delete.
- `Program.cs` holds the voicing-backed renderer behind a **swappable holder** (a small mutable
  field the handlers read, instead of a captured local). On `VoicingsChanged` it re-runs
  `VoicingStore.LoadShapes()` → new `VoicingBook` → new `AlphaTexRenderer` and swaps the holder.
- Progression/Song/Rhythm need **no** rebuild — they're read per-use (and not yet consumed by the
  generator at all), so saving them just updates the DB.

---

## 9. Scope boundaries

**In scope:** the four-entity CRUD surface (list/create/edit/delete), live parse + preview,
the SVG voicing diagram, the store write path (+ new `SongStore`), the generic bridge family,
and voicing live-refresh. Carries the **voicing CRUD UI deferred from `domain/voicings` (req IN7)**.

**Out of scope (explicit):**
- Wiring stored **progressions/rhythms into the exercise generator** (it stays on `SeedData`) —
  that's exercise-workbench / exercises-definition-ui.
- A **root-picker** on the voicing diagram (canonical-C anchor only in v1).
- **Catalog-metadata editing** (genre/subgenre/tags) — see §10.2.
- **Pack import/authoring UI** (packs remain a file drop).

---

## 10. Resolved sub-decisions

All three confirmed (`content-crud-chat-001`):

1. **Delete vs Revert.** When a `UserDefined` edit shadows a BuiltIn/Pack row, "delete" reverts to
   the lower tier rather than removing the entity. **Decision:** contextual label — "Delete" for
   user-only entities, "Revert to default" when a lower tier exists; both map to the same
   `entityDelete` (drop the `UserDefined` row).
2. **Catalog metadata in v1.** **Decision: defer.** Editor = name + DSL body only; genre/subgenre/
   tags are not surfaced in v1 (the header round-trips on disk, so nothing is lost; user-authored
   rows simply have none).
3. **UI placement.** **Decision:** a single-page Practice ⇄ Content view toggle (above), not a
   second HTML page — shared bridge module, no second alphaTab bootstrap.

---

## 11. Test plan

- **Core (xUnit):** store `Save`/`Delete`/`List` per entity incl. tier-shadowing (edit BuiltIn
  → new UserDefined row, lower tier intact; delete shadow → lower resurfaces); voicing `Save`
  canonicalizes to C (round-trip); `SongStore` parity; `DiagramModel` interval/function/spelling
  correctness for known shapes; preview-default `Exercise` builds for each score entity; invalid
  DSL → the parser's `FormatException` surfaces as `entityParseError`.
- **Bridge:** `WebMessageRouter` dispatches each `entity*` verb with the discriminator; malformed
  envelope is dropped (existing contract).
- **Manual:** author a voicing → diagram colors/labels correct → save → it appears in the
  generated score on the next render (live-refresh); edit a built-in progression → shadow badge →
  revert restores the default.

---

## 12. Build slices (provisional — for the plan)

1. **Store write path + `SongStore`** (Core/Persistence) + unit tests — the foundation.
2. **`ContentCrud` Features slice + generic bridge envelopes + router verbs** (Core) + tests.
3. **`DiagramModel` computation** (Core) + tests.
4. **Front-end refactor**: extract `bridge.js`, add the Practice⇄Content toggle.
5. **`content-crud.js`** generic editor + score-preview strategy.
6. **`chord-diagram.js`** SVG fret-box + voicing-preview strategy.
7. **Voicing live-refresh** wiring in `Program.cs`.

Each is an independent, testable seam; 1–3 are pure Core (no UI) and land first.

Related: [[design-philosophy-durable-over-minimal]], `domain/voicings` (engine done, UI deferred
here), exercise-workbench / exercises-definition-ui (consume stored content later).