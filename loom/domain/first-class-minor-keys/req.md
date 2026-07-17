---
type: req
id: rq_01KXQW3WQJSZWKH8A3H9YN7DYB
title: First-class minor keys (end-to-end) — Requirements
status: locked
created: 2026-07-17
updated: 2026-07-17
version: 2
design_version: 2
tags: []
parent_id: de_01KXQW333KYNQCH1P71VB9E35P
requires_load: []
---
# First-class minor keys (end-to-end) — Requirements

### ✅ Included

- `IN1` **C frame.** A Progression DSL degree is authored **tonic-relative** (minor home `1-`, natural `1- 2dim 3 4- 5- 6 7`, raised tones as accidentals `#6`/`#7`); a **converter** normalizes it to the single **parent-major** absolute frame stored in `Progression.Bars`. *(Supersedes the A1 wording — parent-major storage, not parallel-major.)*
- `IN2` **Transposer realizes against the parent major.** `Transposer` resolves stored parent-major degrees against `Scale.Major(ParentTonic(key))` — `key.Tonic` for major, `key.Tonic + 3` (relative major) for minor. The kernel never branches on mode.
- `IN3` **Minor `\ks` token.** `NoteSpeller.KeySignatureToken(key)` emits `{tonicLetter}minor` (alphaTab-native `\ks Aminor`) + round-trip inverse. *(Implemented.)*
- `IN4` **Minor note spelling.** A minor key spells its diatonic pitch classes from its relative-major table; chromatic/raised degrees spell letter-pure via `Chord.RootSpelling`.
- `IN5` **UI key picker.** The Key picker (HarmonyControlsR) offers minor keys and the render path honors the mode end-to-end (realized chords + spelling on Score and Sheet).
- `IN6` **Ref sync.** `chordflow-dsl-reference.md` documents tonic-relative minor authoring + the converter; `chordflow-domain-model-reference.md` documents `Progression.Home`, the converter, and parent-major realization; `alphatex-syntax-reference.md` documents the `{Note}minor` `\ks` form. All in the same units of work.
- `IN7` **Golden tests.** Converter round-trip (`ToAuthor(ToParent(d)) == d`); natural-minor `i–iv–v` (`1- 4- 5-`) and `iiø–V–i` (`2ø 57 1-`) realized in a minor key; **harmonic-minor vii°7 (`#7dim7`) root spells `G♯`** and **melodic-minor vi° (`#6ø`) root spells `F♯`** (via parent-major `#5`/`#4` + `RootSpelling`); renderer emits `\ks {tonic}minor`.
- `IN8` **`Progression.Home` field.** `Progression` gains `Home : Tonality` (`Tonality { Major, Minor }`, default `Major` so existing progressions are unchanged), recording the author frame the converter presents/parses.
- `IN9` **The converter.** A pure `ToParent`/`ToAuthor` pair on degree tokens — a fixed degree rotation with the accidental carried through unchanged; exact round-trip.
- `IN10` **Parser applies the converter.** `ProgressionParser` applies `ToParent` at parse (given `Home`), so the stored `.dsl` stays author-frame while `Bars` are parent-major.

### ❌ Excluded

- `EX1` `~dropped` — the "minor-relative input sugar" is no longer excluded; under C it **is** the design (the converter, `IN9`/`IN10`).
- `EX2` **Harmonic-analysis logic** — lives in the `harmonic-analysis` thread (pitch-based, needs no DSL-frame decision).
- `EX3` **The other 5 diatonic modes as `Tonality` values** — Dorian/Phrygian/Lydian/Mixolydian/Locrian. The converter generalizes to them but v1 ships **Major + Minor only**. *(Note: harmonic/melodic-minor **chords** are in reach as Minor + accidentals, so they are not excluded.)*
- `EX4` **Inverse-converter editor UI** — a Nashville tonic-relative display toggle. `ToAuthor` exists, but no dedicated editor surface in v1.

### ⛓ Constraints

- `C1` **Major regression invariant.** Every existing (major-authored) progression's realized output and byte-level render is unchanged; only minor-key behavior is new.
- `C2` **Single-frame kernel.** No per-mode scale table in the kernel: every mode is the one major frame + a converter rotation (+ accidental'd degrees for altered scales). The raised leading tone / major V rides the chord **quality** or an accidental'd degree, never a new kernel scale.
- `C3` **`Scale.ForKey` unchanged.** `HarmonicAnalyzer` and other consumers keep the natural-minor mode switch; only what `Transposer` uses for realization changes.
- `C4` **Chromatic/raised roots keep `RootSpelling`.** Accidental'd degrees continue to spell letter-pure via the existing `Chord.RootSpelling` path from `chromatic-degrees`.
