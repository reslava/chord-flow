---
type: design
id: de_01KTHJD3QTBGRVX3BBRD29PKAW
title: ChordFlow MVP — Design
status: draft
created: 2026-06-07
version: 1
tags: []
parent_id: id_01KTHJ61W7749XVY7AKGZV7F9D
requires_load: []
---
# ChordFlow MVP — Design

Translates `mvp-idea.md` into concrete component design. Architecture baseline: **desktop-first Photino + C# engine + JS/alphaTab**, **vertical slices over a shared Domain kernel**, SQLite persistence. alphaTab is the render/playback layer only.

---

## 1. Solution layout

Single .NET solution, one runnable host project plus a test project.

```
ChordFlow.sln
  src/
    ChordFlow.App/                 ← Photino host (entry point), wwwroot, DI wiring
      Domain/                      ← pure music kernel (no I/O, no UI)
      Rendering/                   ← AlphaTexRenderer (only code that knows alphaTex)
      Features/                    ← vertical slices
        GenerateExercise/
        PracticeSession/
        ExerciseLibrary/
        Progress/
      Infrastructure/              ← SQLite store, Photino bridge, web message router
      wwwroot/                     ← index.html, app.js, alphaTab.min.js, soundfont
  tests/
    ChordFlow.Tests/               ← xUnit, targets Domain + Rendering
```

Rationale: one assembly for the MVP keeps it simple; folders enforce the conceptual boundaries. We can split `Domain`/`Rendering` into their own libraries later if we add a CLI/web front-end (the idea doc's Phase-2). No MediatR — a slice is a class with a method.

---

## 2. Domain kernel (`Domain/`)

Pure, immutable, fully unit-tested. No alphaTex knowledge, no I/O.

```csharp
public enum Quality { Major, Minor, Dominant7 }          // expand later
public enum Difficulty { Beginner, Intermediate, Advanced }

// Pitch class 0..11 with a preferred spelling for the active key.
public readonly record struct PitchClass(int Value);     // 0 = C ... 11 = B

public sealed record Key(PitchClass Tonic, bool IsMinor); // e.g. Bb major

public sealed record Chord(PitchClass Root, Quality Quality);

// A progression is roman-numeral scale degrees, key-independent.
public sealed record Progression(string Id, string Name, IReadOnlyList<RomanDegree> Degrees);
public readonly record struct RomanDegree(int Degree, Quality Quality); // I, IV, V ...

// Where chord hits land within one bar.
public sealed record RhythmPattern(string Id, string Name, IReadOnlyList<Beat> Beats);
public readonly record struct Beat(Duration Duration, bool IsHit);       // hit vs rest

public enum Duration { Whole, Half, Quarter, Eighth, Sixteenth }         // maps to alphaTex :N

// How a Chord becomes specific strings/frets for rendering.
public sealed record Voicing(IReadOnlyList<FretPosition> Positions);
public readonly record struct FretPosition(int String, int Fret);        // 1 = high E ... 6 = low E
```

**Key services (pure functions):**

- `Transposer` — `Chord[] Realize(Progression, Key)`: maps each `RomanDegree` to a concrete `Chord` in the key. 12-bar blues degrees use **Dominant7** quality (blues convention) but MVP renders shell triads; quality is carried for later.
- `VoicingBook` — `Voicing Lookup(Chord, Difficulty)`: for MVP, a hand-authored table of **beginner shell voicings** keyed by chord root/quality. This is the one place literal frets live (verified data, see §6).

The composed **exercise model** the engine emits:

```csharp
public sealed record Exercise(
    Key Key,
    Progression Progression,
    RhythmPattern Rhythm,
    int Tempo,
    Difficulty Difficulty);
```

---

## 3. Rendering seam (`Rendering/AlphaTexRenderer`)

The **only** component that knows alphaTex syntax. Single responsibility: `Exercise → string` (alphaTex). This isolation is what keeps future MIDI/GuitarPro/MusicXML exporters additive.

```csharp
public interface IScoreRenderer { string Render(Exercise exercise); }
public sealed class AlphaTexRenderer : IScoreRenderer { ... }
```

### alphaTex generation rules (verified against the docs)

Confirmed syntax (alphaTex introduction + bar-metadata pages):

- **Header metadata:** `\title "..."`, `\tempo N`, `\ts num den`, `\ks bb` (flat keys accepted: `cb gb db ab eb bb f`). A lone `.` line ends the metadata block.
- **Notes:** `fret.string` (e.g. `3.4` = fret 3 on string 4). alphaTab strings: 1 = highest.
- **Duration:** a leading `:N` token sets the duration for following beats until changed (`:1 :2 :4 :8 :16` = whole/half/quarter/eighth/sixteenth). This is **stateful** across beats and bars.
- **Chord (simultaneous notes):** group in parentheses — `(3.4 3.3 3.2)`.
- **Rest:** `r`.
- **Bar separator:** `|`.

### Worked example — 12-bar blues in Bb, beat-1+3 quarter feel

For a "beat 1 & 3" pattern in 4/4 the bar is four quarter beats: chord, rest, chord, rest.

```alphatex
\title "12 Bar Blues — Bb"
\subtitle "Beginner — Beats 1 & 3"
\tempo 80
\ts 4 4
\ks bb
.
:4 (x.4 x.3 x.2) r (x.4 x.3 x.2) r |   // I  (Bb)  — x = voicing frets from VoicingBook
...
```

> The `x.string` frets are **not hardcoded in the renderer** — the renderer asks `VoicingBook.Lookup(chord, difficulty)` for `FretPosition`s and formats them. The renderer maps `Duration → :N`, `Beat.IsHit==false → r`, and emits `\ks` from `Key`.

**Open verification items (resolve in the playground during implementation, not now):**
- Exact token for **dotted** durations and **ties** (the earlier chat's `h.2` / `(...)h.2` notation is unverified and likely wrong — do **not** copy it). MVP rhythms (beat-1, beat-1+3, quarters) need only `:4` + `r`, so dotted/ties are **not required for v1**.
- Whether `\ks bb` vs `\ks Bb` casing matters; docs show lowercase flats.

---

## 4. Host & bridge (`Infrastructure/`)

### Photino window
`PhotinoWindow` loads `wwwroot/index.html`. No HTTP server, no localhost port. The window *is* the app.

### C# ↔ JS bridge — a narrow string protocol
Photino gives `SendWebMessage(string)` (C#→JS) and a received-message handler (JS→C#). We send small JSON envelopes, but the **payload that matters is just the alphaTex string**.

C# → JS:
```json
{ "type": "loadScore", "tex": "\\title \"...\" ... ", "tempo": 80 }
{ "type": "play" }   { "type": "stop" }   { "type": "setTempo", "bpm": 90 }
```
JS → C#:
```json
{ "type": "ready" }
{ "type": "playbackFinished" }
{ "type": "beatChanged", "bar": 3, "beat": 1 }   // for future progress/accuracy
```

A `WebMessageRouter` deserializes envelopes and dispatches to the active feature slice. Envelope `type` strings are the bridge's only contract surface.

### JS glue (`wwwroot/app.js`)
Thin. Owns the alphaTab instance, translates envelopes to alphaTab API calls:
- `loadScore` → `api.tex(msg.tex)`
- `play`/`stop` → `api.playPause()` / `api.stop()`
- alphaTab events `playerStateChanged` / `activeBeatsChanged` → post `playbackFinished` / `beatChanged` back to C#.

alphaTab config: `core.tex = true` not used (we call `api.tex` imperatively), `player.enablePlayer = true`, `player.soundFont = '/soundfont/sonivox.sf2'`, cursor enabled for the synced highlight.

---

## 5. Feature slices (`Features/`)

Each slice is a class composing Domain + Rendering + Infrastructure. No mediator.

| Slice | Responsibility (MVP) |
|-------|----------------------|
| `GenerateExercise` | Build an `Exercise` (12-bar blues, chosen key, chosen rhythm, tempo, Beginner) → `AlphaTexRenderer.Render` → push `loadScore` to JS. |
| `PracticeSession` | Drive play/stop/tempo via the bridge; receive `playbackFinished`. |
| `ExerciseLibrary` | List saved exercises from SQLite; re-load one. |
| `Progress` | On "mark practiced," write a `PracticeRecord` to SQLite. (No accuracy detection in v1.) |

### Persistence (SQLite)
EF Core (Dapper is a fine alternative; EF chosen for migration tooling). Tables:
```
Exercises(Id, Key, ProgressionId, RhythmId, Tempo, Difficulty, CreatedUtc)
PracticeRecords(Id, ExerciseId, PracticedUtc)
```
Exercises store the **definition** (the `Exercise` record fields), never the alphaTex — alphaTex is regenerated on load so a renderer fix improves all saved exercises.

---

## 6. Seed data (verified, authored during implementation)

- **Progression:** `12bar_blues` = `I I I I  IV IV  I I  V IV I V` (Dominant7 quality).
- **Keys:** all 12, MVP UI exposes Bb first.
- **Rhythm patterns:** `beat_1` (`hit,rest,rest,rest`), `beat_1_3` (`hit,rest,hit,rest`), `quarters` (`hit,hit,hit,hit`) — all quarter `Duration`.
- **Beginner shell voicings:** small authored table (Bb, Eb, F at minimum for the blues). Frets to be confirmed in the alphaTab playground so rendered tab + audio match before locking — this is the one data-correctness gate for v1.

---

## 7. Risks confirmed / carried forward

- **alphaTex dotted/tie syntax** — unverified, but **not needed for MVP** rhythms. Verify before adding shuffle/syncopation (Phase 2).
- **Soundfont** — need a small, redistributable GM `.sf2`; confirm license + size.
- **WebView2** — present on Win11; cross-platform render testing deferred (desktop-first, Windows MVP).
- **alphaTab JS API names** (`api.tex`, `playerStateChanged`, `activeBeatsChanged`, `soundFont`) — confirm exact names against the installed alphaTab version during the bridge step.

---

## 8. Implementation order (feeds the plan)

1. Solution + Domain kernel types + `Transposer` (unit-tested, no UI).
2. `AlphaTexRenderer` + `VoicingBook` seed data (unit-tested: known `Exercise` → expected alphaTex string).
3. Photino host + `wwwroot` + alphaTab wiring; render a hardcoded alphaTex string end-to-end.
4. Bridge protocol + JS glue; `GenerateExercise` pushes a real score; play with synced cursor.
5. SQLite + `ExerciseLibrary` + `Progress` (save / list / mark practiced).
6. Wire the minimal UI controls (key picker, rhythm picker, tempo, generate, play, save).
