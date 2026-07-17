---
type: design
id: de_01KXQW333KYNQCH1P71VB9E35P
title: First-class minor keys (end-to-end)
status: done
created: 2026-07-17
updated: 2026-07-17
version: 2
idea_version: 1
tags: []
parent_id: id_01KXQFGY5B575DAS8HPQNBX4T1
requires_load: []
---
# First-class minor keys (end-to-end)

## 1. The decision: C — parent-major storage + a converter (single-frame kernel)

> **Supersedes the earlier A1 pick.** A1 (parallel-major frame) was chosen first, then
> reversed in chat-001 after the harmonic/melodic-minor analysis. The exploration
> (A1 rejected · B vs C) is recorded in §2; the record below is the live decision.

**Every progression is stored in ONE absolute frame — its parent major** (the major
key that shares its key signature). A degree in `Progression.Bars` is always a
parent-major degree; the kernel (Transposer, HarmonicAnalyzer, transforms, renderer)
knows only that one frame and **never branches on mode**.

Minor (and, later, every mode) is an **authoring lens**, applied at the DSL edges by a
small pure **converter**:

- **The `.dsl` text stays in the author's frame** — a minor tune reads tonic-relative:
  home `1-`, the natural degrees `1- 2dim 3 4- 5- 6 7`, and raised tones as accidentals
  (`#6`/`#7`). Readable, and what is stored on disk.
- **`Progression.Bars` holds the parent-major degrees** (post-converter) — the single
  frame the kernel realizes.

`Key(A, minor)` stays honest: it drives both the **parent major** used for realization
and the **spelling** (relative-major note table + letter-pure `RootSpelling` for
accidental'd degrees).

### Why C (and why not A1 or B) — chat-001

Three options, separated cleanly (A1's idea bullet had conflated two):

| | **A1** parallel-major | **B** minor-offset kernel | **C** parent-major + converter |
|---|---|---|---|
| Author's DSL for i / ♭III–♭VI–♭VII | `1-` / `b3 b6 b7` (must flat) | `1-` / `3 6 7` (bare) | `1-` / `3 6 7` (bare) |
| Harmonic/melodic roots (G♯, F♯) | **mis-spell A♭/G♭** | correct | correct |
| Frames the kernel sees | one | **two** (Transposer branches) | **one** |
| Generalizes to other modes | no | per-mode scale table in kernel | **yes — one frame + a rotation** |
| Extra machinery | none | one Transposer branch | a small converter + author-form ≠ stored-form |

**A1 is the odd one out on spelling:** its frame is the *parallel* major, so the
harmonic/melodic raised tones (G♯, F♯) are A-major's own diatonic 6/7 — written **bare**,
and a bare degree gets no letter-pure spelling, so it collapses to the flat table (A♭, G♭).
**B and C anchor at the natural-minor / relative major**, so the raised tones become
**accidental'd degrees** (`#6`/`#7`) that spell letter-pure by construction via the
existing `chromatic-degrees` `RootSpelling`. C is chosen over B because it keeps the
kernel **single-frame** and generalizes: every mode is stored parent-major + a converter
rotation, so new modes are *data*, not kernel code (see §4).

## 2. The converter and the home-mode field

```
enum Tonality { Major, Minor }   // v1; the 5 other diatonic modes are the growth path (§4)

Progression(Id, Name, Bars, Tonality Home = Tonality.Major)   // default Major ⇒ existing progs unchanged
```

Each mode's tonic sits on a fixed degree of its parent major (Ionian→1, … Aeolian/minor→6).
The converter is a pure degree rotation with the **accidental carried through unchanged**
(author-degree and parent-degree are the same physical scale note, so a `#`/`b` moves it
identically):

- `ToParent(degree, Minor)`: `1→6 2→7 3→1 4→2 5→3 6→4 7→5` — used at **parse**.
- `ToAuthor(degree, Minor)`: the inverse — used for **display** (Nashville view).

Round-trip, A minor: you type `1- 2dim 3 4- 5- 6 7` (+ `#7dim7`) → `ToParent` →
stored `6- 7dim 1 2- 3- 4 5` (+ `#5dim7`) → realized in A minor (parent major C) →
`Am B° C Dm Em F G` (+ G♯dim7 spelled via `RootSpelling`) → `ToAuthor` → shown back as typed.

## 3. Realization, spelling & renderer

- **Realization:** `Transposer` realizes the stored parent-major degrees against the key's
  **parent major** — `Scale.Major(ParentTonic(key))` where `ParentTonic` = `key.Tonic` for
  a major key and `key.Tonic + 3` (relative major) for a minor key. `Scale.ForKey` is
  **unchanged** (`HarmonicAnalyzer` keeps its natural-minor classification, C3). For a major
  key the realization is byte-identical (C1).
- **Parse:** `ProgressionParser` applies `ToParent` given the progression's `Home`, so the
  `.dsl` stays author-frame while `Bars` become parent-major.
- **Key signature:** `NoteSpeller.KeySignatureToken(key)` emits `{tonic}minor` for a minor
  key — alphaTab accepts `\ks Aminor` natively. (Already implemented.)
- **Note spelling:** a minor key spells its diatonic pitch classes from its relative-major
  table (already delivered by `UsesSharps(tonic + 3)`); accidental'd (chromatic / raised)
  degrees spell letter-pure via `Chord.RootSpelling` (C4).
- **Renderer:** the old `EnsureMajorSupported` guard (threw on any minor key) is removed.

## 4. Modes (the growth path C unlocks)

- The 5 non-Ionian **diatonic** modes (Dorian … Locrian) are rotations of a major scale, so
  under C they are **one frame + a different rotation** — no new kernel scale table, spelling
  automatic (their notes are diatonic to the parent major).
- **Harmonic / melodic minor need no new mode** — they are `Minor + #7` and `Minor + #6/#7`;
  the raised tones are accidental'd degrees that spell correctly via `RootSpelling`.
- v1 wires **Major + Minor only**; the converter is built to generalize but the other modes
  are not exposed yet.

## 5. Non-goals

- The other 5 diatonic modes as `Tonality` values (Dorian, Phrygian, Lydian, Mixolydian,
  Locrian) — the converter generalizes to them but v1 ships Major + Minor.
- A dedicated editor view for the inverse/display converter (a Nashville tonic-relative
  toggle) — `ToAuthor` exists, but no UI surface for it in v1.
- Harmonic-analysis logic (lives in `harmonic-analysis`; pitch-based, needs no DSL frame).

## 6. Validation / golden tests

- Every existing major progression byte-identical (C1 regression guard).
- Converter round-trip: `ToAuthor(ToParent(d, Minor), Minor) == d` for all degrees + accidentals.
- Realization in A minor: natural-minor `i–iv–v` → Am/Dm/Em; `iiø–V–i` → Bm7♭5/E7/Am.
- **Spelling (now in reach under C):** harmonic-minor vii°7 (`#7dim7`) root spells **G♯**;
  melodic-minor vi° (`#6ø`) root spells **F♯** — via the parent-major `#5`/`#4` + `RootSpelling`.
- Renderer: a minor tune emits `\ks {tonic}minor` and spells diatonic notes from the
  relative-major table.
- Dogfood: pick a minor key in the app → correct realized chords + spelling on Score and Sheet.
