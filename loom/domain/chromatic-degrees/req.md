---
type: req
id: rq_01KVXYXMK1W0NNBCQAM69X718Y
title: "Progression DSL — chromatic (#/b) chord degrees — Requirements"
status: locked
created: 2026-06-24
updated: 2026-06-25
version: 2
tags: []
parent_id: id_01KVVCJAS88SGJ0QHXTGFB5T4S
requires_load: []
---
# Progression DSL — chromatic (#/b) chord degrees — Requirements

### ✅ Included

- `IN1` `ProgressionParser` accepts an **optional single leading `#`/`b`** before the degree digit (`<accidental?><degree:1..7><quality?>[:slots]`); the suffix-parsing after the digit is unchanged.
- `IN2` `RomanDegree` gains `Accidental Accidental` (enum `{Natural, Sharp, Flat}`) as a **defaulted positional member**, set by the parser from the token's `#`/`b`.
- `IN3` New spelled-note primitive `NoteName(char Letter, int Accidental)` in `Music/Harmony`, with a `Symbol` formatter that renders `#`/`b` (and `##`/`bb` for double accidentals).
- `IN4` `Chord` gains an optional `NoteName? RootSpelling` (default `null`).
- `IN5` `Transposer` computes the **letter-pure** root spelling for accidental'd degrees and sets `Chord.RootSpelling`, in the major-key realize path: degree letter = tonic letter advanced `degree-1` steps; accidental = the offset making `letter+accidental ≡ finalPc`; `finalPc = mod12(diatonicPc ± 1)`.
- `IN6` `ChordSymbol.Format` uses `RootSpelling.Symbol` when present, else falls back to the existing `NoteSpeller.Name(root, key)` key-table path.
- `IN7` Letter-pure spelling **without enharmonic collapse**: `#4` in F → `B` (Bb raised), `b27` in F → `Gb7`, `#4dim7` in C → `F#dim7`, `b4` in C → `Fb`, `#7` in C → `B#`; double accidentals are emitted, not simplified.
- `IN8` `jazz_blues_standard.dsl` bar 6 upgraded from the `47` stand-in to `#4dim7`.
- `IN9` Reference docs synced in the same unit of work: `chordflow-dsl-reference.md` (Progression DSL grammar + worked row + new errors) and `chordflow-domain-model-reference.md` (`RomanDegree.Accidental`, `NoteName`, `Transposer` spelling, `Chord.RootSpelling`).
- `IN10` Tests: parser accidental cases incl. errors (`#`/`b` with no degree, `##4`, `b8`); spelling cases incl. the F→`Bdim7` combine, `Gb7`, `F#dim7`, and the `B#`/double-accidental edge.
- `IN11` `BeginnerShellStrategy` gains a `Diminished7` shell shape (root + ♭3 + ♭♭7 → A/D/G-string offsets `(-2, -1)`) so the IN8 `#IVdim7` actually voices and renders rather than throwing; a unit test covers the new arm, and the domain-model ref notes the added quality. *(Scope grew during implementation: the voicing engine had no `Diminished7` arm, blocking IN8's render — see chat-001.)*

### ❌ Excluded

- `EX1` Automated tritone-sub / secondary-dominant **transforms** (`IProgressionTransform`) — the north star in `jazz-blues-design`, a later thread.
- `EX2` Staff-**notehead** accidental injection — alphaTab spells the standard-staff noteheads from `fret+string+tuning`; we accept its enharmonic choice there (only the chord *symbol* is ours).
- `EX3` Unifying **all** chord-symbol spelling onto `NoteName` — diatonic chords stay on the key-table path; a no-op refactor here.
- `EX4` Double-accidental **input** (`##4`, `#b4`) — input is single-accidental only; double accidentals arise on output only.
- `EX5` Minor-key spelling — v1 renderer is major-only (`EnsureMajorSupported`).
- `EX6` Song-DSL changes — it already lexes `#`/`b` in `key`/`mod`; out of scope here.

### ⛓ Constraints

- `C1` `PitchClass` stays spelling-free; the spelling lives on the realized `Chord`, preserving ctx **C4** ("spelling is derived, never stored on the pitch class").
- `C2` **Override-and-fallback**: diatonic chords carry no `RootSpelling`, so existing rendered output is **byte-identical**.
- `C3` The parser accepts **at most one** leading `#`/`b`; double accidentals can only arise on output.
- `C4` Spelling is **deterministic from the token**, independent of the key's sharp/flat convention (the locked principle).
- `C5` The defaulted `Accidental` member keeps every existing `new RomanDegree(d, q)` call site compiling unchanged.
- `C6` `AlphaTexRenderer` remains the only alphaTex-aware code; the accidental parsing, `NoteName`, and the spelling algorithm live in the `Music/Harmony` kernel + `Transposer`.
- `C7` `NoteSpeller`'s key-table is retained — it still spells the `\ks` key signature and the title's key name.