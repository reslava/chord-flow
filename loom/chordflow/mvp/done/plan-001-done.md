---
type: done
id: pl_01KTHKHSCQE2D3CZFRHQG6KVPD-done
title: Done — Phase 1 — Engine & Renderer
status: done
created: "2026-06-08T00:00:00.000Z"
version: 5
tags: []
parent_id: pl_01KTHKHSCQE2D3CZFRHQG6KVPD
requires_load: []
---
# Done — Phase 1 — Engine & Renderer

## Step 1 — Scaffold the solution: ChordFlow.App host project + ChordFlow.Tests (xUnit), targeting net9.0; create the Domain/ Rendering/ Features/ Infrastructure/ wwwroot/ folders; add .gitignore.

Scaffolded the solution.

**Created:**
- `ChordFlow.sln` (classic .sln format — .NET 10's `dotnet new sln` now defaults to `.slnx`; regenerated with `--format sln` to match the plan's filename).
- `src/ChordFlow.App/ChordFlow.App.csproj` — console host (`OutputType=Exe`, ImplicitUsings + Nullable enabled). Photino wiring deferred to a later phase; for Phase 1 the App is just the host shell.
- `tests/ChordFlow.Tests/ChordFlow.Tests.csproj` — xUnit test project, with a project reference to ChordFlow.App.
- Architecture folders under `src/ChordFlow.App/`: `Domain/`, `Rendering/`, `Features/`, `Infrastructure/`, `wwwroot/` — each seeded with a `.gitkeep` so the empty dirs are tracked.

**Decisions:**
- **Target framework: `net10.0`, not `net9.0`** as the plan/design specify. Only the .NET 10.0.201 SDK is installed (10 is current LTS); targeting net9.0 would require pulling the 9.0 targeting pack for no benefit. User explicitly approved the deviation. Plan/design docs left unchanged (text still reads net9.0) — flagging here rather than silently editing the plan.

**Not changed:** `.gitignore` already existed and is comprehensive + ChordFlow-aware (covers bin/obj, *.db, soundfont *.sf2/.sf3, .claude/settings.local.json). No edits needed.

**Verified:** `dotnet build ChordFlow.sln` → Build succeeded, 0 warnings, 0 errors.

## Step 2 — Define the Domain kernel types as immutable records: PitchClass, Key, Quality, Chord, Progression, RomanDegree, RhythmPattern, Beat, Duration, Voicing, FretPosition, Difficulty, Exercise.

Defined the Domain kernel as immutable types under `namespace ChordFlow.Domain` (file-scoped), one file per type in `src/ChordFlow.App/Domain/`:

**Records / structs:**
- `PitchClass(int Value)` — readonly record struct, 0..11.
- `Key(PitchClass Tonic, bool IsMinor)` — sealed record.
- `Chord(PitchClass Root, Quality Quality)` — sealed record.
- `RomanDegree(int Degree, Quality Quality)` — readonly record struct.
- `Progression(string Id, string Name, IReadOnlyList<RomanDegree> Degrees)` — sealed record.
- `Beat(Duration Duration, bool IsHit)` — readonly record struct.
- `RhythmPattern(string Id, string Name, IReadOnlyList<Beat> Beats)` — sealed record.
- `FretPosition(int String, int Fret)` — readonly record struct (String 1=high E .. 6=low E, alphaTab numbering).
- `Voicing(IReadOnlyList<FretPosition> Positions)` — sealed record.
- `Exercise(Key, Progression, RhythmPattern, int Tempo, Difficulty)` — sealed record.

**Enums:**
- `Quality { Major, Minor, Dominant7 }`
- `Difficulty { Beginner, Intermediate, Advanced }`
- `Duration { Whole, Half, Quarter, Eighth, Sixteenth }`

All signatures match design §2 verbatim. Each type carries an XML-doc summary. Pure — no I/O, no alphaTex knowledge (C4). Removed `Domain/.gitkeep` now that the folder holds real files.

**Verified:** `dotnet build ChordFlow.sln` → Build succeeded, 0 warnings, 0 errors.

## Step 3 — Implement the Transposer (Progression+Key -> Chord[]); add seed data: the 12-bar blues progression and the three rhythm patterns (beat-1, beat-1+3, quarters). Unit-test transposition across all 12 keys.

Implemented transposition + seed data, fully unit-tested.

**`src/ChordFlow.App/Domain/Transposer.cs`** — static pure class. `Realize(Progression, Key) -> Chord[]`: maps each `RomanDegree` to a concrete chord by shifting the key's tonic pitch class by the scale-degree semitone offset, wrapping mod 12. Major scale offsets `{0,2,4,5,7,9,11}`; natural-minor offsets `{0,2,3,5,7,8,10}` selected when `Key.IsMinor`. Degree quality is passed straight through (so blues degrees stay Dominant7). Null-guards args; throws `ArgumentOutOfRangeException` for degrees outside 1..7.

**`src/ChordFlow.App/Domain/SeedData.cs`** — pure constants:
- `TwelveBarBlues` = `I I I I IV IV I I V IV I V`, all Dominant7 (id `12bar_blues`).
- Rhythm patterns, all quarter-note beats: `Beat1` (hit,rest,rest,rest), `Beat1And3` (hit,rest,hit,rest), `Quarters` (hit×4); plus `RhythmPatterns` list in UI order.
- `AllMajorKeys` — the 12 major keys ordered by tonic pitch class 0..11.

**`tests/ChordFlow.Tests/TransposerTests.cs`** — 16 tests:
- `[Theory]` over all 12 tonics asserting the blues realizes to roots `{I,I,I,I,IV,IV,I,I,V,IV,I,V}` (tonic, +5, +7 mod 12) and every chord stays Dominant7.
- Facts: progression length/quality, degree-quality pass-through (ii of C = D minor), minor-key branch (3rd of A natural minor = C, distinguishing +3 from major's +4), and out-of-range degree throws.

Removed the template `tests/ChordFlow.Tests/UnitTest1.cs`.

**Verified:** `dotnet test ChordFlow.sln` → Passed! 16/16, 0 failed.

## Step 4 — Implement VoicingBook.Lookup(Chord, Difficulty) with a hand-authored beginner shell-voicing table (Bb, Eb, F at minimum); unit-test lookups.

Implemented `VoicingBook.Lookup(Chord, Difficulty)` with a hand-authored beginner shell-voicing table, unit-tested.

**`src/ChordFlow.App/Domain/VoicingBook.cs`** — static class wrapping a `Dictionary<(int Root, Quality), Voicing>`. Beginner **dominant-7 shell voicings** (root + major 3rd + minor 7th, 5th omitted) on the A/D/G strings (alphaTab strings 5/4/3):
- Bb7 → (5,1)(4,0)(3,1)
- Eb7 → (5,6)(4,5)(3,6)
- F7  → (5,8)(4,7)(3,8)

`Lookup` throws `NotSupportedException` for non-Beginner difficulty and `KeyNotFoundException` (with a helpful message) for any chord not in the table.

**`tests/ChordFlow.Tests/VoicingBookTests.cs`** — 6 tests:
- `[Theory]` over Bb7/Eb7/F7 computing each fretted note's pitch class from standard tuning and asserting the voicing's note set equals `{root, root+4, root+10}` — i.e. the frets **provably spell the dominant-7 shell**. This is the real data-correctness check, independent of the playground.
- Every chord of the realized Bb 12-bar blues resolves to a 3-note voicing.
- Unauthored chord (C7) throws `KeyNotFoundException`; non-Beginner difficulty throws `NotSupportedException`.

**Decisions / scope:**
- Authored exactly the three chords of the **Bb 12-bar blues** (I=Bb7, IV=Eb7, V=F7) per the step's "Bb, Eb, F at minimum". This fully supports the canonical MVP exercise (Bb blues end-to-end). Other keys are added later as more authored rows, no code change.
- **Pending gate (carried forward):** the design calls for confirming exact frets in the alphaTab playground before locking. Pitch-class *content* is unit-verified here; ergonomic fret positions still warrant a playground pass during the render/playback phase (the Eb7/F7 shells sit higher on the neck, frets 5–8).

**Verified:** `dotnet test ChordFlow.sln` → Passed! 22/22 (16 prior + 6 new), 0 failed.

## Step 5 — Implement AlphaTexRenderer (Exercise -> alphaTex string) following loom/refs/alphatex-syntax-reference.md (stateful :N durations, ( ) chord groups, r rests, \ts/\ks/\tempo). Unit-test: known Exercise -> expected alphaTex string.

Implemented the AlphaTexRenderer — the sole alphaTex-aware component — following the verified syntax reference (C5).

**`src/ChordFlow.App/Rendering/IScoreRenderer.cs`** — `interface IScoreRenderer { string Render(Exercise); }`, the exporter seam.

**`src/ChordFlow.App/Rendering/AlphaTexRenderer.cs`** — `sealed class : IScoreRenderer`. Emits, per loom/refs/alphatex-syntax-reference.md:
- Header: `\title "<ProgressionName> — <KeyName>"`, `\subtitle "<Difficulty> — <RhythmName>"`, `\tempo N`, `\ts 4 4`, `\ks <token>`, then a lone `.`.
- Key signature from a major-key pitch-class table, lowercase flats (Bb → `\ks bb`, matching the worked example); a parallel table supplies the title's key name.
- Body: realizes the progression via `Transposer`, then per chord (bar) per `Beat` emits `(fret.string …)` chord groups for hits (frets from `VoicingBook.Lookup`) and `r` for rests, bars terminated by ` |`.
- **Stateful `:N` durations** — emits `:4` only when the duration changes, persisting across beats *and bars*, so it appears exactly once for an all-quarter piece.
- Deterministic `\n` line endings (not `Environment.NewLine`) for cross-platform stable output. `NotSupportedException` for minor keys (MVP renders major only).

**`tests/ChordFlow.Tests/AlphaTexRendererTests.cs`** — 4 tests:
- **Golden exact-string**: one-bar I-in-Bb + beat-1 → the full expected alphaTex string, char-for-char.
- Full Bb 12-bar blues: header lines, 12 `|` separators, `:4` appears exactly once, and Bb7/Eb7/F7 voicings all present.
- Quarters rhythm ends a bar with four chord groups and no rest.
- Minor key throws.

**Verified:** `dotnet test ChordFlow.sln` → Passed! 26/26 (22 prior + 4 new), 0 failed.

This closes Phase 1: the pure C# engine + renderer is complete and fully unit-tested (IN1–IN5, IN11; C1, C3, C4, C5).
