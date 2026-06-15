---
type: req
id: rq_01KV5509VZ77PSNX4ADHYMVEBB
title: Content-definition CRUD UI — the shared editor for DSL-backed entities — Requirements
status: locked
created: "2026-06-15T00:00:00.000Z"
updated: 2026-06-15
version: 1
tags: []
parent_id: id_01KV05AZ7T77CMGM86X7T7GZRB
requires_load: []
---
# Content-definition CRUD UI — the shared editor for DSL-backed entities — Requirements

One uniform CRUD surface for the four DSL-backed content entities (Progression, Song,
RhythmPattern, Voicing), plus the engine write path it requires. Scope confirmed in
`content-crud-chat-001` + `content-crud-design`.

### ✅ Included

- `IN1` A **uniform CRUD surface** — list / create / edit / delete — for all four entities: Progression, Song, RhythmPattern, Voicing.
- `IN2` **One generic editor component**, parameterized by entity type, with a per-entity **preview strategy** (not four divergent screens).
- `IN3` **Live parse on edit**, surfacing the parser's `FormatException` message **inline** as a parse-error.
- `IN4` **Live preview**: an alphaTab **score** snippet for Progression/Song/Rhythm; an **SVG chord diagram** for Voicing.
- `IN5` A **custom SVG fret-box** voicing diagram showing frets/string, **intervals-in-color**, **note names**, fingering/barre, muted/open markers, first-fret indicator, and an interval-color legend.
- `IN6` The voicing **`DiagramModel` is computed in Core** (intervals/functions/spelling); JS only draws SVG.
- `IN7` The **engine write path**: `Save` / `Delete` / `List` on each content store, including a **new `SongStore`** (none exists today). Carries the voicing-CRUD UI deferred from `domain/voicings`.
- `IN8` **Tier-shadowing semantics** under the `(Id, Origin)` PK: CRUD writes the **`UserDefined`** tier only; editing a BuiltIn/Pack entity creates a shadow; delete = hard-delete (user-only) **or revert** (a lower tier resurfaces).
- `IN9` **Voicing save canonicalizes to C** (`VoicingDslParser.Parse` → `VoicingDslWriter.ToDsl`); the other three persist the validated DSL as typed.
- `IN10` A **generic bridge envelope family** — `entityList` / `entityGet` / `entityPreview` / `entitySave` / `entityDelete` — with an `entity` discriminator, plus the matching `WebMessageRouter` verbs and outbound DTOs.
- `IN11` **Voicing live-refresh**: after a successful voicing save/delete, rebuild the in-memory `VoicingBook` + `AlphaTexRenderer` so the generated score reflects it without a restart.
- `IN12` **Front-end refactor**: extract a shared `bridge.js` module and add a single-page **Practice ⇄ Content** view toggle.
- `IN13` The list shows **origin badges**; the destructive action is labelled **"Delete"** or **"Revert to default"** per the entity's origin/lower tier.

### ❌ Excluded

- `EX1` Wiring stored **progressions/rhythms into the exercise generator** (it stays on `SeedData`) — a later thread (exercise-workbench / exercises-definition-ui).
- `EX2` A **root-picker** on the voicing diagram — v1 shows the canonical-C anchor only.
- `EX3` **Catalog-metadata editing** (genre / subgenre / tags) — editor is name + DSL body only in v1.
- `EX4` **Pack import / authoring UI** — packs remain a file drop.

### ⛓ Constraints

- `C1` `Domain/` stays pure and I/O-free; store **write methods live in `Persistence/`**; all music theory (intervals, spelling, canonicalization, the `DiagramModel`) lives in Core.
- `C2` CRUD writes the **`UserDefined` tier only** — never mutate a BuiltIn or Pack row.
- `C3` **One generic** bridge envelope family with an `entity` discriminator — not per-entity envelopes.
- `C4` **DSL is the only persisted form** (alphaTex/realized frets never stored); the voicing's stored DSL is canonical-C.
- `C5` Dependency direction **Desktop → Core** unchanged; the engine stays UI-agnostic (compile-enforced).
- `C6` **No new build step or framework** in `wwwroot` — vanilla JS modules, served over the existing virtual host.