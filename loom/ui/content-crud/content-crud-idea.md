---
type: idea
id: id_01KV05AZ7T77CMGM86X7T7GZRB
title: Content-definition CRUD UI — the shared editor for DSL-backed entities
status: draft
created: 2026-06-13
version: 1
tags: []
parent_id: null
requires_load: []
---
# Content-definition CRUD UI — the shared editor for DSL-backed entities

## The idea

One **uniform CRUD screen** for every DSL-backed content entity — `Progression`,
`Song`, `RhythmPattern`, and `Voicing` — grouped here in the `ui` weave with all
other front-end/UX work. The engine + persistence for these entities already
exist in `ChordFlow.Core` (`ProgressionStore`, `RhythmPatternStore`,
`VoicingStore`, the EF entities); what's missing is the front-end. Today
`wwwroot/` is still only the MVP exercise generator — there is **no** content CRUD
screen yet, which is exactly why the voicings slice deferred its UI to here.

## Why a shared screen (not per-entity one-offs)

Every content entity is the same shape: a **canonical DSL string** + a **name** +
catalog/provenance metadata, parsed to a domain object and previewed. So the CRUD
surface should be **one component** parameterized by entity type, not four
divergent screens. Building it once, uniformly, is the durable move
([[design-philosophy-durable-over-minimal]]).

## Shape (to design)

- **Editor:** DSL textarea + name field; live parse on edit.
- **Live preview:** parsed → rendered. Per-entity preview differs:
  - Progression / Song / RhythmPattern → alphaTab **score** snippet.
  - Voicing → a **chord diagram** (from `VoicingShape` → `Realize` → `Voicing`
    metadata). **Open design call:** alphaTab's native `\chord (...)` diagram vs a
    custom SVG fret-box renderer.
- **Parse-error surface:** the parser's `FormatException` message inline (every
  parser already throws a located message).
- **List + create / edit / delete** via new C#↔JS bridge envelopes (peers of the
  existing `save` / `loadScore` envelopes).
- **Save path:** for voicings, `VoicingDslParser.Parse` → `VoicingDslWriter.ToDsl`
  to store the **canonical-C** form (the engine side is ready).

## Carries the deferred voicing CRUD

This thread **owns the voicing CRUD UI** deferred from `domain/voicings`
(req `IN7`). The engine-side stored-first `VoicingBook` is fully wired; only the
authoring/preview screen remains, and it belongs to the shared pattern here.

## Open questions (for the design doc)

1. Chord-diagram preview: alphaTab native vs custom SVG.
2. Bridge envelope shapes for list/create/update/delete (one generic
   `entityCrud` envelope vs per-entity).
3. One generic editor component vs a thin per-entity wrapper.
4. Live-refresh of the in-memory stores after a save (voicings are snapshotted at
   launch today — `Program.cs`).

Related: [[design-philosophy-durable-over-minimal]], the `domain/voicings` thread
(engine + persistence done), the `exercises-definition-ui` thread (origin of these
decisions), [[exercise-workbench]].