---
type: design
id: de_01KV1NEPYM2Y7FQ3J7J523Q2BA
title: Default pack — authored CAGED voicing content (the voicings/ bundle)
status: done
created: "2026-06-13T00:00:00.000Z"
updated: 2026-06-14
version: 6
tags: []
parent_id: id_01KV06Z7C68HW71XF76ESWT203
requires_load: []
---
# Default pack — authored CAGED voicing content (the voicings/ bundle)

## 1. What this delivers

The default pack's **`voicings/` content** — the one piece of the starter bundle
that does not exist yet. content-catalog **Phase 2 already shipped** everything
*around* it: `Content/default-pack/{progressions,songs,rhythms}/` as real `.dsl`
files, a `PackReader` that **already walks a `voicings/` folder**, a `PackImporter`
with a **live `ContentKind.Voicing` arm** → `VoicingEntity` (upsert by `(Id, Origin)`),
and `DefaultPack.ImportInto` (first-run import, stamped `Origin.BuiltIn`).

So there is **no mechanism work here.** We drop authored `.dsl` files into
`Content/default-pack/voicings/` and they ride the existing import path. This makes
the stored-first `VoicingBook` **observable in the shipped app for the first time** —
today the system has zero authored voicings, so every lookup falls to the generated
`BeginnerShellStrategy`. Authored voicings *shadow* that shell.

## 2. Scope — the authoring matrix (C-full)

**Qualities × CAGED families**, one canonical shape per cell, authored once at the
**C** anchor (the parser normalizes any declared anchor to its lowest C placement).

- **Qualities (8):** `maj`, `min`, `dom7`, `maj7`, `m7`, `m7b5`, `dim`, `aug`.
- **Families (5):** C, A, G, E, D.

**C-full** = author the *complete* canonical shape for **every cell that is a real
movable CAGED form**, including cells that are an awkward stretch *at the C anchor*.
Rationale (decided in chat): a shape that's unplayable in full at C is often the same
shape that realizes to an easy, open voicing in another key —
`C7 G-shape = 8 7 5 5 5 6` is impossible at C but is the open
`G7 = 3 2 0 0 0 1` four keys down. Authoring the **full** shape preserves those good
realizations; `VoicingRealizer.Realize` slides each shape to all 12 roots and drops
any octave that won't fit the **0–15** window.

Cells with no standard movable grip are simply **not authored** — we never fabricate a
fingering just to fill the grid. So the matrix is "up to 40," realistically fewer.
The two shipped blues progressions + the demo song already require **`dom7`** and
**`m7`**; the full matrix covers them and gives a rich ranked list everywhere else.

## 3. Conventions (decided in chat)

- **Filename = id = `{quality}_{shape}shape.dsl`**, key-free (storage normalizes to C,
  so the id must **not** imply a key). E.g. `maj_eshape.dsl`, `dom7_gshape.dsl`,
  `m7b5_ashape.dsl`. Quality tokens: `maj min dom7 maj7 m7 m7b5 dim aug`; shape token =
  the CAGED letter lowercased + `shape`. The filename stem becomes the `VoicingEntity.Id`.
- **One `voicing` line per file**, optionally preceded by a `name:` header (human label,
  e.g. `name: Dominant 7 — G shape`). `PackDefinitionFile.Read` strips `name:` and hands
  the `voicing …` line to the parser; the stem is the id.
- **No `genre`/`tags`/`subgenre` header** on voicing files — matches rhythm (`UpsertRhythm`
  carries no catalog metadata). The voicing's catalog columns stay null.
- **Zone/Area authoring principle (decided in chat):** prefer the grip that keeps every
  interval inside the shape's octave **zone** over a grip that stretches to a lower string for
  the same note. E.g. G-shape min = `8 6 5 5 8 8` (zone-local), not `8 6 5 5 4 8` (stretches
  the b3 down to the B string for no gain). This subsumes CAGED chord shapes from octave shapes.

## 4. Fret-value accuracy is an implementation concern, not a design guess

This design fixes the **matrix, method, and conventions** — it deliberately does **not**
hand-fabricate 40 fingerings. The DSL reference's verified examples are the templates
(open-C `x 3 2 0 1 0`, E-shape C `8 10 10 9 8 8`, G-shape C `8 7 5 5 5 8`, A-shape Cm).
Each cell's exact `frets:` is authored and checked against a CAGED reference **during
implementation**, cell by cell behind step gates — that's where fingering correctness
earns the attention it needs, not in a design doc written from memory.

## 5. Verification

- **Parse + realize sweep (Core test):** every `Content/default-pack/voicings/*.dsl`
  parses via `VoicingDslParser`, and each realizes across all 12 roots without throwing;
  the lowest placement fits 0–15.
- **Import path (Core test):** `DefaultPack.ImportInto` imports the voicings as
  `Origin.BuiltIn`; a `VoicingBook` over the stored set returns non-empty `Candidates`
  for the shipped qualities (`dom7`, `m7`) and now shadows the generated shell.
- **A couple of golden cells:** the DSL-ref example shapes realize to known frets
  (regression anchor).
- Idempotent re-import is already proven by the importer's own tests — not re-proven here.

## 6. The symmetric-quality note — right-sized

On inspecting the realizer, the earlier "ambiguous normalization" flag was **overstated**.
`VoicingRealizer.Realize` octave-folds by 12 to the lowest non-negative placement —
fully **deterministic** for the symmetric qualities in scope, **`aug`** (period 4 frets)
and now **`dim7`** (period 3 frets — see the domain note below; `dim` the *triad* stays
out of the authored matrix). The fold picks one placement unambiguously. There is **no
canonical-rule ambiguity to resolve.**

**Domain addition (decided in chat — absorbs req `IN6`):** the authored diminished cell is
**`dim7`** (1 b3 b5 bb7), not the dim triad. That required one scoped domain change —
`Quality.Diminished7` + `QualityIntervals` `{0,3,6,9}` + `dim7`/`°7` suffix in both DSL
parsers + the writer emit — landed in the same unit of work. The `Diminished` triad is
retained. dim7 is authored at E/A/D grips and **filled by minor-3rd symmetry** to cover 0–15.

The only residual: `VoicingBook.Candidates` does **not** de-duplicate, so a symmetric
quality *could* surface two CAGED labels that realize to an identical grip — a cosmetic
dupe in the offered list. That's a small **domain/voicings** hardening (add `Distinct`),
**not** a content rule and **not** a blocker. Authoring discipline: for `aug`, author only
the visibly distinct shapes.

Directly answering the open question: there is **no "max 2 shapes" cap** in the current
realizer/book to relax. `Realize` returns the **single lowest placement** per (shape, root);
`Candidates` already offers **one per CAGED family**, every one that fits 0–15. Offering
*multiple octave positions of the same shape* would be a realizer enhancement — out of
scope for this content thread.

## 7. Follow-ons (domain/voicings — not this thread)

- **Per-position playability hint** — the realizer computes, per realized position, a
  fret-span / barre-width signal and a usable-subset hint ("here, play strings 4–1").
  This is the durable home for the *partial-voicing* idea: a static authored "mark" would
  be wrong because playability is **per-position, not per-voicing**. Every C-full voicing
  benefits for free when it lands.
- **`Candidates` de-dup** for symmetric qualities (§6).
- **Multiple-octave offering** per shape, if ever wanted.

## 8. Out of scope

- Progressions / songs / rhythms generalization — **already shipped** (content-catalog Phase 2).
- Pack format / reader / importer / provenance — **content-catalog**.
- Any Voicing **DSL or domain change** (playability marks, dedup, multi-octave) — **domain/voicings**. *Exception:* the single `Quality.Diminished7` addition (req `IN6`), absorbed here because the dim7 cell can't be authored without it.
- The **intervals / octave-shapes / chord-qualities / caged-system** derivation engine — Rafa's durable direction (derive CAGED shapes from interval formulas instead of hand-typed frets). **Deferred to its own `domain` thread(s)**; these hand-authored voicings become its golden oracle. Not this thread.
- Paid / additional packs; the authoring & import **UI** (`ui` weave).

## 9. Notes

- **Reference-doc sync (done):** the scoped `Diminished7` addition (req `IN6`) *does* touch
  the DSL + domain, so `chordflow-dsl-reference` (new `dim7`/`°7` suffix rows) and
  `chordflow-domain-model-reference` (9th quality + suffix row) were updated in the same unit
  of work. `-architecture` is untouched. (The original "content-only, no ref change" held until
  dim7 was pulled in.)
- **Implementation shape (for the plan):** (1) author `maj`/`min`/`dom7` across CAGED
  (MVP-critical, validates the path end-to-end) → (2) author `maj7`/`m7`/`m7b5`/`dim`/`aug`
  → (3) the verification tests in §5 → (4) confirm §9 ref-sync.