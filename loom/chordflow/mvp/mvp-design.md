---
type: design
id: de_01KTHJD3QTBGRVX3BBRD29PKAW
title: ChordFlow MVP — Design
status: draft
created: "2026-06-07T00:00:00.000Z"
updated: 2026-06-08
version: 2
tags: []
parent_id: id_01KTHJ61W7749XVY7AKGZV7F9D
requires_load: []
---
# ChordFlow MVP — Design

Translates `mvp-idea.md` into concrete component design. Architecture baseline: **desktop-first WinForms + WebView2 + C# engine + JS/alphaTab**, **vertical slices over a shared Domain kernel**, SQLite persistence. alphaTab is the render/playback layer only.

> **Host decision (Phase 2):** the host was migrated from **Photino.NET → WinForms + the official `Microsoft.Web.WebView2` control**. Photino's WebView2 **composition controller** renders a black/blank window on this stack (.NET 10 + WebView2 runtime 149); the WinForms control uses the **windowed controller** (the same path Edge uses) and renders correctly. The C# engine/renderer/feature slices and the bridge *envelope contract* were unaffected — only `Infrastructure/` + the `wwwroot/app.js` bridge shim changed. Full investigation: `loom/refs/photino-net-desktop-host-reference.md`, chat `mvp-chat-002`.

---

## 1. Solution layout

Single .NET solution, one runnable host project plus a test project.

```
ChordFlow.sln
  src/
    ChordFlow.App/                 ← WinForms + WebView2 host (entry point), wwwroot, DI wiring
      Domain/                      ← pure music kernel (no I/O, no UI)
      Rendering/                   ← AlphaTexRenderer (only code that knows alphaTex)
      Features/                    ← vertical slices
        GenerateExercise/
        PracticeSession/
        ExerciseLibrary/
        Progress/
      Infrastructure/              ← SQLite store, WebView2 bridge, web message router
      wwwroot/                     ← index.html, app.js, alphaTab.min.js, font/, soundfont/
  tests/
    ChordFlow.Tests/               ← xUnit, targets Domain + Rendering
```

Rationale: one assembly for the MVP keeps it simple; folders enforce the conceptual boundaries. We can split `Domain`/`Rendering` into their own libraries later if we add a CLI/web front-end (the idea doc's Phase-2). No MediatR — a slice is a class with a method. The host project targets `net10.0-windows` (`UseWindowsForms`); the Domain/Rendering/Features code is plain C# and stays portable.

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

**Resolved during Phase 2 implementation:** MVP rhythms (beat-1, beat-1+3, quarters) need only `:4` + `r`; dotted/tie tokens stay out of scope (EX4). `\ks bb` lowercase confirmed.

---

## 4. Host & bridge (`Infrastructure/`)

### WinForms + WebView2 window
A WinForms `Form` hosts a dock-filled `WebView2` control (`Microsoft.Web.WebView2.WinForms`) — the window *is* the app. After `EnsureCoreWebView2Async`, the host maps the local `wwwroot` to a virtual host and navigates to it:

```csharp
web.CoreWebView2.SetVirtualHostNameToFolderMapping(
    "chordflow.local", wwwrootPath, CoreWebView2HostResourceAccessKind.Allow);
web.CoreWebView2.Navigate("https://chordflow.local/index.html");
```

This gives the page a real **`https` origin** with **no HTTP server and no localhost port** (it's an in-process resource intercept — satisfies C2). The real origin also un-blocks alphaTab's soundfont fetch, which a `file://` (null-origin) page would CORS-block. WinForms' WebView2 uses the **windowed controller** (the path that renders on this stack; WPF/Photino's composition controller does not — see the host-decision note above).

### C# ↔ JS bridge — a narrow string protocol
WebView2 gives `CoreWebView2.PostWebMessageAsString(string)` (C#→JS) and the `CoreWebView2.WebMessageReceived` event (JS→C#). We send small JSON envelopes; the **payload that matters is just the alphaTex string**.

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

A `WebMessageRouter` deserializes envelopes and dispatches to the active feature slice. Envelope `type` strings are the bridge's only contract surface — **unchanged across the Photino→WebView2 migration** (only the transport call sites changed). `PhotinoBridge` → `WebView2Bridge`; the router is host-agnostic.

### JS glue (`wwwroot/app.js`)
Thin. Owns the alphaTab instance, translates envelopes to alphaTab API calls:
- `loadScore` → `api.tex(msg.tex)`
- `play`/`stop` → `api.playPause()` / `api.stop()`; `setTempo` → `api.playbackSpeed = bpm / baseTempo`
- alphaTab events `playerStateChanged` / `activeBeatsChanged` → post `playbackFinished` / `beatChanged` back to C#.

Transport on the JS side: `window.chrome.webview.postMessage(json)` (JS→C#) and `window.chrome.webview.addEventListener('message', e => …)` (C#→JS). The `Bridge` module feature-detects, so opening `index.html` with no host still works (renders a `SAMPLE_TEX` fallback). alphaTab config: `player.enablePlayer = true`, `player.soundFont = 'soundfont/sonivox.sf2'` (relative; same-origin under the virtual host), `scrollMode = Off`, cursor enabled for the synced highlight. (`core.useWorkers` can be `true` now that the origin is real; it was `false` only for the `file://` null-origin era.)

---

## 5. Feature slices (`Features/`)

Each slice is a class composing Domain + Rendering + Infrastructure. No mediator.

| Slice | Responsibility (MVP) |
|-------|----------------------|
| `GenerateExercise` | Build an `Exercise` (12-bar blues, chosen key, chosen rhythm, tempo, Beginner) → `AlphaTexRenderer.Render` → push `loadScore` to JS. |
| `PracticeSession` | Drive play/stop/tempo via the bridge; receive `playbackFinished`/`beatChanged`. |
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
- **Beginner shell voicings:** small authored table — Bb7 `(1.5 0.4 1.3)`, Eb7 `(6.5 5.4 6.3)`, F7 `(8.5 7.4 8.3)` confirmed; rendered tab + audio match.

---

## 7. Risks confirmed / carried forward

- **Host rendering (RESOLVED):** Photino's WebView2 composition controller renders black on .NET 10 + WebView2 149. Migrated to WinForms + WebView2 (windowed controller). See the host-decision note + `loom/refs/photino-net-desktop-host-reference.md`.
- **Soundfont origin (RESOLVED):** `file://` CORS-blocks alphaTab's soundfont fetch; the virtual-host `https` origin fixes it.
- **alphaTex dotted/tie syntax** — unverified, **not needed for MVP** (EX4). Verify before adding shuffle/syncopation.
- **Soundfont** — Sonivox GM `sonivox.sf2`, **Apache-2.0**, ~1.35 MB — small + redistributable (C7 satisfied).
- **Windows-only host** — WinForms is Windows-only; aligned with EX8 (Windows-first). Engine stays UI-agnostic, so a cross-platform/web front-end remains additive (C1, EX5).
- **alphaTab JS API names** — verified against the installed alphaTab 1.8.3 (`api.tex`, `playerStateChanged`, `activeBeatsChanged`, `playbackSpeed`, `soundFont`, `ScrollMode`).

---

## 8. Implementation order (feeds the plans)

1. Solution + Domain kernel types + `Transposer` (unit-tested, no UI). — *Phase 1*
2. `AlphaTexRenderer` + `VoicingBook` seed data (unit-tested: known `Exercise` → expected alphaTex string). — *Phase 1*
3. Desktop host + `wwwroot` + alphaTab wiring; render a hardcoded alphaTex string end-to-end. — *Phase 2*
4. Bridge protocol + JS glue; `GenerateExercise` pushes a real score; play with synced cursor. — *Phase 2*
5. Host migration Photino → WinForms + WebView2 (virtual-host origin; `chrome.webview` bridge). — *Phase 2b*
6. SQLite + `ExerciseLibrary` + `Progress` (save / list / mark practiced).
7. Wire the minimal UI controls (key picker, rhythm picker, tempo, generate, play, save).