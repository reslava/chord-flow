---
type: plan
id: pl_01KVXYYPDKA2D54RPK600V0X50
title: "Chromatic (#/b) chord degrees — implementation"
status: done
created: 2026-06-24
updated: 2026-06-25
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KVXYQ0HR654X95B5HCVJC64K
requires_load: []
target_version: 0.1.0
actual_release: 0.12.0
steps:
  - id: romandegree-accidental
    order: 1
    status: done
    description: Add `Accidental {Natural,Sharp,Flat}` enum and a defaulted positional `Accidental` member to `RomanDegree`.
    files_touched: [src/ChordFlow.Core/Music/Harmony/RomanDegree.cs]
    blocked_by: []
    satisfies: [IN2, C5]
  - id: notename-primitive
    order: 2
    status: done
    description: "Add the `NoteName(char Letter, int Accidental)` spelled-note primitive with a `Symbol` formatter (`#`/`b`, `##`/`bb`)."
    files_touched: [src/ChordFlow.Core/Music/Harmony/NoteName.cs, tests/ChordFlow.Core.Tests/NoteNameTests.cs]
    blocked_by: []
    satisfies: [IN3]
  - id: chord-rootspelling
    order: 3
    status: done
    description: Add optional `NoteName? RootSpelling = null` to `Chord`.
    files_touched: [src/ChordFlow.Core/Music/Harmony/Chord.cs]
    blocked_by: []
    satisfies: [IN4, C1]
  - id: parse-b-prefix
    order: 4
    status: done
    description: "Parser: accept an optional single leading `#`/`b` in `ParseDegreeQuality`, set `RomanDegree.Accidental`, reject double accidentals; add parser tests incl. error cases."
    files_touched: [src/ChordFlow.Core/Music/Progressions/ProgressionParser.cs, tests/ChordFlow.Core.Tests/ProgressionParserTests.cs]
    blocked_by: [romandegree-accidental]
    satisfies: [IN1, C3, IN10]
  - id: letter-pure-spelling
    order: 5
    status: done
    description: "Transposer: compute the letter-pure root `NoteName` for accidental'd degrees and set `Chord.RootSpelling` in the major-key realize path (`ChordFor` gains the Key)."
    files_touched: [src/ChordFlow.Core/Music/Progressions/Transposer.cs, tests/ChordFlow.Core.Tests/TransposerTests.cs]
    blocked_by: [romandegree-accidental, notename-primitive, chord-rootspelling]
    satisfies: [IN5, IN7, C4]
  - id: chordsymbol-override-and-fallback
    order: 6
    status: done
    description: "ChordSymbol: honor `Chord.RootSpelling.Symbol` when present, else fall back to `NoteSpeller.Name`; add spelling tests (F→Bdim7, Gb7, F#dim7, B# edge)."
    files_touched: [src/ChordFlow.Core/Music/Harmony/ChordSymbol.cs, tests/ChordFlow.Core.Tests/ChordSymbolTests.cs]
    blocked_by: [chord-rootspelling, letter-pure-spelling]
    satisfies: [IN6, IN10, C2, C7]
  - id: diminished7-shell-voicing
    order: 7
    status: done
    description: "Add a `Diminished7` shell arm to `BeginnerShellStrategy` (root + ♭3 + ♭♭7 → offsets `(-2, -1)`) so `#IVdim7` voices; add a unit test."
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Voicings/BeginnerShellStrategy.cs, tests/ChordFlow.Core.Tests/BeginnerShellStrategyTests.cs]
    blocked_by: [chord-rootspelling]
    satisfies: [IN11, IN8]
  - id: fix-jazz-blues-bar-6
    order: 8
    status: done
    description: "Upgrade `jazz_blues_standard.dsl` bar 6 from the `47` stand-in to `#4dim7`."
    files_touched: [src/ChordFlow.Core/Content/default-pack/progressions/jazz_blues_standard.dsl]
    blocked_by: [parse-b-prefix, letter-pure-spelling, chordsymbol-override-and-fallback]
    satisfies: [IN8]
  - id: reference-doc-sync
    order: 9
    status: done
    description: "Sync the reference docs: Progression DSL grammar + worked row + new errors, and the domain-model additions."
    files_touched: [loom/refs/chordflow-dsl-reference.md, loom/refs/chordflow-domain-model-reference.md]
    blocked_by: [fix-jazz-blues-bar-6]
    satisfies: [IN9]
---
# Chromatic (#/b) chord degrees — implementation

## Goal

Add chromatic (#/b) chord roots to the Progression DSL, resolving each to the correct pitch and a letter-pure chord symbol, then retire the bar-6 `47` stand-in in the standard jazz blues. Built bottom-up: the domain carriers first (RomanDegree.Accidental, the NoteName primitive, Chord.RootSpelling), then the parser, then the letter-pure spelling in Transposer, then the ChordSymbol seam, then the content fix, and finally the reference-doc sync. Diatonic output stays byte-identical throughout (override-and-fallback). Per req rq_01KVXYXMK1W0NNBCQAM69X718Y.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add `Accidental {Natural,Sharp,Flat}` enum and a defaulted positional `Accidental` member to `RomanDegree`. | src/ChordFlow.Core/Music/Harmony/RomanDegree.cs | — | IN2, C5 |
| ✅ | 2 | Add the `NoteName(char Letter, int Accidental)` spelled-note primitive with a `Symbol` formatter (`#`/`b`, `##`/`bb`). | src/ChordFlow.Core/Music/Harmony/NoteName.cs, tests/ChordFlow.Core.Tests/NoteNameTests.cs | — | IN3 |
| ✅ | 3 | Add optional `NoteName? RootSpelling = null` to `Chord`. | src/ChordFlow.Core/Music/Harmony/Chord.cs | — | IN4, C1 |
| ✅ | 4 | Parser: accept an optional single leading `#`/`b` in `ParseDegreeQuality`, set `RomanDegree.Accidental`, reject double accidentals; add parser tests incl. error cases. | src/ChordFlow.Core/Music/Progressions/ProgressionParser.cs, tests/ChordFlow.Core.Tests/ProgressionParserTests.cs | romandegree-accidental | IN1, C3, IN10 |
| ✅ | 5 | Transposer: compute the letter-pure root `NoteName` for accidental'd degrees and set `Chord.RootSpelling` in the major-key realize path (`ChordFor` gains the Key). | src/ChordFlow.Core/Music/Progressions/Transposer.cs, tests/ChordFlow.Core.Tests/TransposerTests.cs | romandegree-accidental, notename-primitive, chord-rootspelling | IN5, IN7, C4 |
| ✅ | 6 | ChordSymbol: honor `Chord.RootSpelling.Symbol` when present, else fall back to `NoteSpeller.Name`; add spelling tests (F→Bdim7, Gb7, F#dim7, B# edge). | src/ChordFlow.Core/Music/Harmony/ChordSymbol.cs, tests/ChordFlow.Core.Tests/ChordSymbolTests.cs | chord-rootspelling, letter-pure-spelling | IN6, IN10, C2, C7 |
| ✅ | 7 | Add a `Diminished7` shell arm to `BeginnerShellStrategy` (root + ♭3 + ♭♭7 → offsets `(-2, -1)`) so `#IVdim7` voices; add a unit test. | src/ChordFlow.Core/Instruments/Guitar/Voicings/BeginnerShellStrategy.cs, tests/ChordFlow.Core.Tests/BeginnerShellStrategyTests.cs | chord-rootspelling | IN11, IN8 |
| ✅ | 8 | Upgrade `jazz_blues_standard.dsl` bar 6 from the `47` stand-in to `#4dim7`. | src/ChordFlow.Core/Content/default-pack/progressions/jazz_blues_standard.dsl | parse-b-prefix, letter-pure-spelling, chordsymbol-override-and-fallback | IN8 |
| ✅ | 9 | Sync the reference docs: Progression DSL grammar + worked row + new errors, and the domain-model additions. | loom/refs/chordflow-dsl-reference.md, loom/refs/chordflow-domain-model-reference.md | fix-jazz-blues-bar-6 | IN9 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:romandegree-accidental -->
### Step 1 — RomanDegree.Accidental

`public enum Accidental { Natural, Sharp, Flat }` and `RomanDegree(int Degree, Quality Quality, Accidental Accidental = Accidental.Natural)`. The default keeps every existing `new RomanDegree(d, q)` call site compiling (C5).

<!-- step:notename-primitive -->
### Step 2 — NoteName primitive

`readonly record struct NoteName(char Letter, int Accidental)` with `Symbol => Letter + (Accidental>=0 ? new string('#',Accidental) : new string('b',-Accidental))`. PitchClass stays spelling-free (C1).

<!-- step:chord-rootspelling -->
### Step 3 — Chord.RootSpelling

`Chord(PitchClass Root, Quality Quality, NoteName? RootSpelling = null)`. Diatonic chords leave it null → byte-identical output (C2).

<!-- step:parse-b-prefix -->
### Step 4 — Parse #/b prefix

Consume one optional `#`/`b` before the degree digit; `##4`/`#b4` and `#`/`b`-with-no-digit raise clear FormatExceptions. Suffix parsing after the digit is untouched.

<!-- step:letter-pure-spelling -->
### Step 5 — Letter-pure spelling

Tonic letter from `NoteSpeller.Name(key.Tonic,key)[0]`; degree letter advanced `degree-1` steps through C-D-E-F-G-A-B; `finalPc = mod12(diatonicPc ± 1)`; accidental = offset s.t. `naturalPc(letter)+accidental ≡ finalPc`. Scale-only legacy overloads pass no key → RootSpelling null.

<!-- step:chordsymbol-override-and-fallback -->
### Step 6 — ChordSymbol override-and-fallback

`RootSpelling is { } n ? n.Symbol + suffix : NoteSpeller.Name(root,key) + suffix`. NoteSpeller key-table retained for \ks and the title key name (C7).

<!-- step:fix-jazz-blues-bar-6 -->
### Step 8 — Fix jazz-blues bar 6

`17 47 17 17 47 #4dim7 17 67 2-7 57 17_67 2-7_57`. Played in F via jazz_blues_f.dsl it now renders/plays a real Bdim7 passing chord.

<!-- step:reference-doc-sync -->
### Step 9 — Reference-doc sync

DSL ref: optional `#`/`b` degree prefix, a `#4dim7`/`b27` row, the `##4`/`b8`/no-degree errors. Domain ref: RomanDegree.Accidental, NoteName, Transposer letter-pure spelling, Chord.RootSpelling.
