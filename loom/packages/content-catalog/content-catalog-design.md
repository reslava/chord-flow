---
type: design
id: de_01KTXEQWS29T4T2S0GKP7C23AB
title: Content packages & catalog — open-core content distribution
status: done
created: "2026-06-12T00:00:00.000Z"
updated: 2026-06-12
version: 4
tags: []
parent_id: id_01KTXEPV11EAA4DDY2A8MWTCB4
requires_load: []
---
# Content packages & catalog — open-core content distribution

Design for the cross-cutting **catalog-metadata** + **provenance** model and the
**pack** bundle format. Every content entity (`Progression`, `Song`,
`RhythmPattern`, `Voicing`) adopts this; packs are additive data drops. Decisions
settled in `exercises-definition-ui-chat-001`.

> **Stance** ([[design-philosophy-durable-over-minimal]]): pack-readiness baked
> into the content model now; tooling deferred. A pack adds rows, never code.

---

## 1. Catalog metadata

Self-describing DSL header on every content `.dsl`:

```
genre: Blues
subgenre: Shuffle
tags: [12-bar, beginner]
```

- **Parsed → entity columns** (`Genre`, `Subgenre`, `Tags`) for efficient filter
  queries; the DSL header stays the canonical source (denormalized, round-tripped).
- **Never on pure `Domain/` records** — theory stays pure (C3/C4). Metadata lives
  on the *Entity* layer only.
- Applies to **all four** content entities — filter songs by genre, rhythms by
  feel/genre, voicings by family, etc.
- **`Song.OfProgression`** copies the source progression's genre/subgenre/tags —
  the lift inherits, never empties.

## 2. Provenance — `Origin`

```csharp
abstract record Origin;
record BuiltIn()         : Origin;   // shipped in the default/starter pack
record UserDefined()     : Origin;   // authored locally by the user
record Pack(string PackId): Origin;  // imported from a content pack
```

- Stored as an entity column (discriminator + optional `PackId`).
- **Locals shadow imported shadow built-in.** Lookup precedence:
  `UserDefined` > `Pack` > `BuiltIn` — implemented as an `Origin` rank enum in a
  single Id-keyed resolver; consistent with song's locals-shadow-stored and
  voicings' stored-first rules. (Settled — see §6.1.)

## 3. Pack format

```
my-pack/
  manifest.json
  progressions/*.dsl
  songs/*.dsl
  rhythms/*.dsl
  voicings/*.dsl
```

```json
{
  "id": "blues-essentials",
  "name": "Blues Essentials",
  "version": "1.0.0",
  "kind": "content",
  "provenance": "ChordFlow",
  "requires": []
}
```

- **Import = idempotent by Id.** Re-importing upserts by definition Id; no
  duplicates. Same mechanism as today's seeding.
- **Default pack** = today's `SeedData` generalized into the first bundle (the
  free starter set), imported at first run.
- **Referential integrity:** a pack's Songs may reference its Progressions;
  resolve-time **fail-loud** if a referenced definition is missing (same rule as
  Song→Progression refs).

## 4. Placement & dependency direction

- Catalog-metadata columns + `Origin` live on the **Entity** layer
  (`ChordFlow.Core/Persistence/`), never on `Domain/`.
- Pack import = a **Feature** (`ChordFlow.Core/Features/`), reusing the seed
  importer.
- The DSL-header parse lives with each entity's DSL parser.
- Desktop → Core unchanged; import UI (deferred) will be a host concern.

## 5. Explicitly deferred (additive — closer to release)

- **Authoring/export tooling**, pack **versioning** + dependency resolution,
  **signing**/integrity.
- **Import UI** surface (the model + headless idempotent import land now).
- **Sell flow + entitlement/licensing** — a Features/licensing concern, never
  paywalls the core.

## 6. Settled decisions (was: open implementation questions)

1. **`Origin` precedence — `UserDefined` > `Pack` > `BuiltIn`.** The same `Id`
   may exist across origins simultaneously (a built-in re-shipped by a pack, then
   edited locally). Lookup resolves the highest-ranked copy — locals shadow
   imported shadow built-in — **non-destructively**: lower tiers stay on disk as
   fallback, so removing a local restores the next tier down. Implemented as an
   `Origin` rank enum in one Id-keyed resolver. Same shadowing law as the song
   (locals-shadow-stored) and voicings (stored-first) threads.
2. **Tags → JSON column (v1).** `Tags TEXT` holding a JSON array, round-tripped
   1:1 with the canonical DSL header. Sufficient for MVP-scale in-memory
   filtering; `json_each()` is there if SQL-side tag queries are ever needed. A
   later move to a join table is an **additive read-model migration** — it never
   touches the canonical DSL header.
3. **`kind` — manifest pack-type + per-definition kind (mixed packs).** The
   manifest declares `kind: "content"` — the coarse *pack-type* discriminator
   (vs. future `soundfont`/`theme`/etc.). Each **definition's** kind comes from
   its folder (`progressions/`, `songs/`, `rhythms/`, `voicings/`), not a
   redundant per-file field. This supports **mixed packs** — one bundle carrying
   progressions + songs + rhythms together, the way a real genre pack ships.

Related: [[chordflow-architecture-reference]], [[design-philosophy-durable-over-minimal]], the `voicings` / `song` / `rhythm` threads.