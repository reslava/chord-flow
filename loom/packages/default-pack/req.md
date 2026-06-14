---
type: req
id: rq_01KV1NR7T9YGS3VGK94P39BRP0
title: Default pack — the curated free starter content bundle (all entities) — Requirements
status: locked
created: "2026-06-13T00:00:00.000Z"
updated: 2026-06-14
version: 2
tags: []
parent_id: id_01KV06Z7C68HW71XF76ESWT203
requires_load: []
---
# Default pack — the curated free starter content bundle (all entities) — Requirements

### ✅ Included

- `IN1` Author the default pack's **`voicings/` content** — `.dsl` files dropped into `Content/default-pack/voicings/`, one authored voicing per file, flowing through the *existing* `DefaultPack`/`PackImporter` path at first run (no mechanism change).
- `IN2` **The C-full matrix** — qualities `maj · min · dom7 · maj7 · m7 · m7b5 · dim7 · aug` × CAGED families `C · A · G · E · D`, the **complete** canonical shape per real cell, authored once at the **C** anchor. (The diminished cell authors **dim7** — the symmetric diminished 7th — not the dim triad; see `IN6`. m7b5/dim7/aug are authored only at their playable E/A/D grips, dim7 filled by minor-3rd symmetry.)
- `IN3` **Naming/format conventions** — filename = id = `{quality}_{shape}shape.dsl` (key-free, e.g. `dom7_gshape.dsl`); one `voicing …` line per file, optionally preceded by a `name:` human-label header; **no** `genre`/`subgenre`/`tags` header.
- `IN4` Content rides the existing import path **unchanged** — `DefaultPack.ImportInto` imports the voicings as `Origin.BuiltIn`, and the stored-first `VoicingBook` shadows the generated shell for the shipped qualities.
- `IN5` **Verification** — every authored `.dsl` parses and realizes across all 12 roots without throwing (lowest placement fits 0–15); the import → `VoicingBook` shadow is asserted; a couple of golden cells anchor regressions.
- `IN6` **Scoped domain addition (absorbed here):** add `Quality.Diminished7` (1 b3 b5 bb7 = `{0,3,6,9}`) + `QualityIntervals` row + `dim7` / `°7` DSL suffix (both `VoicingDslParser` and `ProgressionParser`) + `VoicingDslWriter` emit, and the `chordflow-dsl` / `chordflow-domain-model` ref-sync. The existing `Diminished` **triad** is retained, not replaced. This narrowly overrides `EX3` / `C1` for this **one** quality so the dim7 cell is authorable; every other Voicing DSL/domain concern stays excluded.

### ❌ Excluded

- `EX1` Progressions / songs / rhythms generalization — already shipped in content-catalog Phase 2.
- `EX2` Pack format / reader / importer / provenance — owned by `content-catalog`.
- `EX3` Any Voicing **DSL or domain change** — playability marks, `Candidates` de-dup, multiple-octave offering — owned by `domain/voicings`. (Exception: the single `Quality.Diminished7` addition absorbed by `IN6`.)
- `EX4` Per-position **playability / partial-voicing hint** — a `domain/voicings` follow-on (a static authored "mark" is rejected: playability is per-position, not per-voicing).
- `EX5` Paid / additional packs; the authoring & import **UI** (`ui` weave).

### ⛓ Constraints

- `C1` **Content only** — no grammar, domain, or architecture change; the `chordflow-dsl/-domain-model/-architecture` reference docs stay unchanged (confirmed at close). (Exception: the scoped `Diminished7` domain + DSL addition and its `chordflow-dsl`/`-domain-model` ref-sync, per `IN6` — the one carve-out this thread makes; `-architecture` is untouched.)
- `C2` **Author once at canonical C; never per-key** — the realizer slides each shape to the 12 roots; there is exactly one `.dsl` per (quality, shape).
- `C3` **Full shapes, real cells only** — author the complete canonical shape (awkward-at-C cells included, since they realize well elsewhere); never fabricate a fingering to fill a non-existent grid cell. Prefer the grip that keeps every interval inside the shape's octave **zone** over a stretch to a lower string for the same note.
- `C4` **Fret values verified, not invented** — each cell's `frets:` is checked against a CAGED reference at authoring time.
- `C5` **No catalog metadata on voicing files** — matches rhythm (`UpsertRhythm` carries none); the voicing's catalog columns stay null.