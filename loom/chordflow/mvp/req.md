---
type: req
id: rq_01KTHK46HYYPF80HFHGBX8DASN
title: ChordFlow MVP — Requirements
status: locked
created: 2026-06-07
updated: 2026-06-08
version: 2
design_version: 5
tags: []
parent_id: de_01KTHJD3QTBGRVX3BBRD29PKAW
requires_load: []
---
# ChordFlow MVP — Requirements

### ✅ Included

- `IN1` Exercise-generation engine in C#: Domain kernel (Key, Chord, Progression, RhythmPattern, Voicing) with a pure `Transposer` and `VoicingBook`.
- `IN2` One progression for v1: **12-bar blues** (`I I I I  IV IV  I I  V IV I V`, Dominant7 quality), transposable to **all 12 keys**.
- `IN3` Three rhythm patterns: **beat-1**, **beat-1+3**, **quarters** (all quarter-note durations).
- `IN4` **Beginner shell voicings** only — one authored voicing set (frets verified in the alphaTab playground before locking).
- `IN5` `AlphaTexRenderer` (`Exercise → alphaTex string`) as the sole alphaTex-aware component.
- `IN6` Desktop host via **WinForms + the official `Microsoft.Web.WebView2` control** (Windows), serving a local `wwwroot` over a **virtual-host mapping** (`https://chordflow.local/` → folder). No HTTP server, no localhost port, no cloud. *(Photino.NET was the original choice and was retired during Phase 2 — its WebView2 composition-controller renders a black window on the net10 + WebView2-149 stack; the official WinForms control uses the windowed controller and renders. Rationale + investigation: `loom/refs/photino-net-desktop-host-reference.md`, chat `mvp-chat-002`.)*
- `IN7` **alphaTab JS** rendering + playback with a **synchronized beat cursor**, fed alphaTex over the C#↔JS bridge.
- `IN8` Narrow C#↔JS bridge: JSON envelopes (`loadScore`/`play`/`stop` out; `ready`/`playbackFinished`/`beatChanged` in), payload = the alphaTex string. Transport = WebView2 `chrome.webview` messages (`PostWebMessageAsString` / `WebMessageReceived`). The envelope `type` contract is host-agnostic.
- `IN9` **SQLite** persistence storing exercise **definitions** (not alphaTex); regenerate alphaTex on load.
- `IN10` Minimal UI controls: key picker, rhythm picker, tempo, generate, play/stop, save, mark-practiced, exercise list.
- `IN11` xUnit tests over Domain + `AlphaTexRenderer` (known `Exercise` → expected alphaTex string).

### ❌ Excluded

- `EX1` Audio-input accuracy detection / scoring (no real-time listening in v1).
- `EX2` Progressions beyond 12-bar blues.
- `EX3` Intermediate / advanced voicing sets.
- `EX4` Shuffle / syncopated / dotted / tied rhythms (needs unverified alphaTex tokens).
- `EX5` Web / PWA distribution (architecture kept open, not built). The engine stays UI-agnostic (C1), so a web/PWA or cross-platform front-end remains an *additive* future option even though the MVP host (WinForms) is Windows-only.
- `EX6` Exporters: MIDI / Guitar Pro / MusicXML (renderer seam exists, only alphaTex implemented).
- `EX7` Accounts, cloud sync, networking of any kind.
- `EX8` macOS / Linux packaging (Windows-first MVP). Reinforced by the WinForms host choice (IN6), which is Windows-only by design for v1.

### ⛓ Constraints

- `C1` C# wherever possible; engine kept UI-agnostic so a Phase-2 web front-end is additive, not a rewrite.
- `C2` No external web server, no localhost port, fully offline; ~$0 operating cost (solo unemployed dev). The WebView2 **virtual-host mapping** satisfies this — it is an **in-process resource intercept**, not a network server or open port, yet gives the page a real `https` origin (which also un-blocks alphaTab's soundfont fetch that `file://` would CORS-block).
- `C3` Vertical slices over a shared Domain kernel; **no MediatR**, no ceremonial layering.
- `C4` Domain kernel is pure (no I/O) and unit-tested.
- `C5` alphaTex syntax must match the verified reference (`loom/refs/alphatex-syntax-reference.md`); the unverified `h.2` notation is forbidden.
- `C6` Use the **alphaTab JS build** (not the .NET package) for the free synced cursor + playback events.
- `C7` Soundfont must be small and redistributable; confirm license before bundling.
- `C8` ⚠️-flagged API details (alphaTab event subscription shape + soundfont packaging; WebView2 controller/bridge surface) verified against the installed versions before relying on them.