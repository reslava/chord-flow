---
type: design
id: de_01KVXYQ0HR654X95B5HCVJC64K
title: "Progression DSL — chromatic (#/b) chord degrees"
status: done
created: 2026-06-24
updated: 2026-06-24
version: 2
idea_version: 2
tags: []
parent_id: id_01KVVCJAS88SGJ0QHXTGFB5T4S
requires_load: []
---
# Progression DSL — chromatic (#/b) chord degrees

Realizes the idea `id_01KVVCJAS88SGJ0QHXTGFB5T4S`. Decided across `chats/chromatic-degrees-chat-001.md`.

## 1. Goal

Let the Progression DSL express **chromatic chord roots** — `#4dim7` (the jazz-blues bar-6 `#IVdim7` passing chord) and `b27` (the `bII7` tritone sub) — resolving to the correct pitch **and** the correctly-spelled chord symbol, in every key. Retire the `47` stand-in in `jazz_blues_standard.dsl` bar 6.

## 2. The locked principle

> **The written degree — not the key — spells the chord root.** `#4` is the raised 4th's *letter*; `b2` is the lowered 2nd's *letter*. **Letter-pure, no enharmonic collapse**: if a composer wanted E they'd write a different degree, so `b4` is F♭ (never E) and `#7` is B♯ (never C). Spelling is therefore **deterministic from the token**, independent of the key's sharp/flat convention.

This is why naive "spell degree n, then paste the glyph" is wrong: in F, degree 4 already spells **Bb**, so `#4` must *combine* (Bb raised → **B natural**, the real `#IVdim7` root), not produce "Bb#". The combine is the whole mechanism.

## 3. Grammar change (parser)

`ProgressionParser.ParseDegreeQuality` (`Music/Progressions/ProgressionParser.cs:181`) today requires `text[0]` to be a digit 1–7. New token shape:

```
<accidental?> <degree:1..7> <quality?> [:slots]
        ^ optional single '#' or 'b'
```

- Consume an optional **single** leading `#` or `b` before the degree digit; map it to the new `Accidental` (below). Everything after the digit is still the quality suffix (unchanged — the existing greedy-suffix logic is untouched).
- **Input is single-accidental only.** `##4` / `#b4` are rejected ("a degree takes at most one leading # or b"). Double accidentals can only *arise on output* (rare; see §5), never be typed.
- The "missing a scale degree" error now fires only when no digit follows the optional accidental (`#`, `b`, `#dim`).

The Song DSL already lexes `#`/`b` in `ParseKey`/`ParseModSpec` (`Music/Songs/SongParser.cs:245,281`) — same glyph, same "raise/lower a semitone" meaning, so the user's mental model stays consistent. No Song-DSL change in this thread.

## 4. Domain change

**`RomanDegree`** (`Music/Harmony/RomanDegree.cs`) gains a third member:

```csharp
public enum Accidental { Natural, Sharp, Flat }
public readonly record struct RomanDegree(int Degree, Quality Quality, Accidental Accidental = Accidental.Natural);
```

The defaulted positional param keeps every existing `new RomanDegree(d, q)` call site compiling unchanged — clean, not a back-compat contortion.

**New primitive `NoteName`** (`Music/Harmony/`) — a *spelled* note, which the kernel lacks today (everything is pitch-class):

```csharp
public readonly record struct NoteName(char Letter, int Accidental)  // Accidental: …,-1=b,0=natural,+1=#,+2=##
{
    public string Symbol => Letter + (Accidental >= 0 ? new string('#', Accidental) : new string('b', -Accidental));
}
```

`PitchClass` stays spelling-free, so **constraint C4** ("spelling is derived, never stored on the pitch class") still holds — the spelling lives on the realized `Chord`, not the pitch class. (Lighter alternative considered: a bare `string? RootSpelling`. Chose the structured `NoteName` for reuse and because the renderer can format glyphs per target; it is the only new type.)

**`Chord`** (`Music/Harmony/Chord.cs`) gains an optional spelling override:

```csharp
public sealed record Chord(PitchClass Root, Quality Quality, NoteName? RootSpelling = null);
```

Diatonic chords leave it `null` → existing behavior is byte-identical.

## 5. The letter-pure spelling algorithm

Computed in `Transposer` when realizing a degree whose `Accidental != Natural`, in a major key (v1 renders major only — `EnsureMajorSupported` throws on minor, so the tonic letter is unambiguous):

1. **tonic letter** = first char of `NoteSpeller.Name(key.Tonic, key)` (e.g. F major → `'F'`).
2. **degree letter** = tonic letter advanced `degree-1` steps through `C D E F G A B` (wrapping).
3. **diatonic pc** = `Scale.DegreePitchClass(degree)` (the un-accidental'd degree's pitch class).
4. **final pc** = `mod12(diatonicPc + shift)`, `shift = +1 (#) / -1 (b)`.
5. **accidental** = the offset in `[-2..+2]` such that `naturalPc(letter) + accidental ≡ finalPc (mod 12)`.
6. → `NoteName(letter, accidental)`, and the chord root pc is `finalPc`.

Worked: `#4` in **C** → letter F, diatonic pc 5, final pc 6, F natural=5 ⇒ **F♯**. `#4` in **F** → letter B, diatonic pc 10, final pc 11, B natural=11 ⇒ **B** (the Bb→B combine). `b27` in **F** → letter G, diatonic pc 7, final pc 6, G natural=7 ⇒ **G♭7**. Edge: `#7` in C → letter B, final pc 0, B=11 ⇒ accidental +1 ⇒ **B♯**; double accidentals (`#7` in a key whose 7th is already sharp → D𝄪) are supported by `NoteName` and rendered `##`/`bb`, per the letter-pure rule.

## 6. Carry & render seam

Spelling surfaces in exactly **one** place: `ChordSymbol.Format(chord, key)` → `NoteSpeller.Name(root, key)` (`Music/Harmony/ChordSymbol.cs:35`). That string feeds the `{ch "…"}` chord label, the `\chord` diagram name, and the now/next-fretboard schedule (`Rendering/AlphaTexRenderer.cs:390,434`).

- `Transposer.ChordFor` gains the realize-path `Key` (the `RealizeBars(_, Key)` overload already has it) so it can letter-spell and set `Chord.RootSpelling`. The legacy `Scale`-only overloads pass no key → `RootSpelling = null` (they are test/legacy paths and never reach `ChordSymbol`).
- `ChordSymbol.Format` uses `chord.RootSpelling.Symbol` **when present**, else falls back to today's `NoteSpeller.Name(root, key)`. **Override-and-fallback** — diatonic chords are untouched, byte-identical output preserved.

What we do **not** touch: **tab** shows `fret.string` (no letters); the **standard-staff noteheads** are spelled by **alphaTab** internally from `fret+string+tuning`, not by us — so no staff-spelling infrastructure is built.

## 7. Accepted trade-offs

1. **Symbol vs staff notehead mismatch — accepted for v1.** Our `{ch}` label / diagram name are correct in tab *and* standard. The individual root **notehead** on a *standard* staff is alphaTab's call and may show the opposite enharmonic (label `Gb7`, notehead `F♯`). Cosmetic, standard-mode only, no name/tab impact; controlling it means per-note accidental injection — deferred to a future notation pass.
2. **Override-and-fallback, not a spelling rewrite.** Only accidental'd degrees get a `NoteName`; diatonic chords stay on the key-table path (still needed for `\ks` and the title's key name). Unifying all chord-symbol spelling onto `NoteName` is a no-op refactor here — out of scope.

## 8. Validation / tests

- **Parser**: `#4dim7`, `b27`, `#27` parse; `RomanDegree.Accidental` set correctly. Errors: `#` / `b` with no degree, `##4`, `#b4`, `b8`.
- **Spelling**: `Transposer` + `ChordSymbol` produce `F#dim7` (`#4dim7` in C), **`Bdim7`** (`#4dim7` in F — the combine), `Gb7` (`b27` in F); plus the `B#` / double-accidental edges.
- **Regression**: existing diatonic progressions render byte-identically (`RootSpelling` null path).
- **Dogfood**: `jazz_blues_standard.dsl` bar 6 `47` → `#4dim7`; the standard jazz blues (played in F via `jazz_blues_f.dsl`) renders and plays with a real **Bdim7** passing chord, labelled correctly in tab and standard.

## 9. Reference-doc sync (same unit of work)

- `chordflow-dsl-reference.md` — Progression DSL: add the optional `#`/`b` degree prefix to the grammar, a worked row (`#4dim7`, `b27`), and the new error messages.
- `chordflow-domain-model-reference.md` — `RomanDegree.Accidental`, the `NoteName` primitive, the letter-pure `Transposer` spelling, and `Chord.RootSpelling`.

## 10. Out of scope

- Automated tritone-sub / secondary-dominant **transforms** (`IProgressionTransform`) — the north star in `jazz-blues-design`, a later thread.
- Staff-notehead accidental injection (see §7.1).
- Unifying all chord-symbol spelling onto `NoteName` (§7.2).
- Minor-key spelling (v1 renders major only).