# Changelog

All notable changes to ChordFlow are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project
adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] — 2026-06-08

First tagged release: the engine generates a 12-bar blues exercise, renders it
as tablature, and plays it back with a synchronized beat cursor.

### Added
- **Music engine (`Domain/`)** — pure, immutable kernel (Key, Chord, Progression,
  RhythmPattern, Voicing) with a `Transposer` and a `VoicingBook`. 12-bar blues
  transposable to all 12 keys; rhythm patterns beat-1, beats-1&3, quarters;
  beginner shell voicings.
- **`AlphaTexRenderer`** — turns an `Exercise` into an alphaTex string; the sole
  alphaTex-aware component (renderer seam for future MIDI/GuitarPro/MusicXML).
- **Desktop host** — WinForms + WebView2, serving the local `wwwroot` over an
  in-process `https://chordflow.local/` virtual host (no web server, no localhost
  port). Renders tablature via [alphaTab](https://www.alphatab.net/) and plays it
  with a bundled GM soundfont.
- **C#↔JS bridge** — narrow JSON-envelope protocol over `chrome.webview`
  (`loadScore`/`play`/`stop`/`setTempo` out; `ready`/`playbackFinished`/
  `beatChanged` in); payload is the alphaTex string.
- **Playback** — play / stop / tempo transport with a synchronized beat cursor,
  current-bar highlight, and active-note highlighting.
- **Tests** — 26 xUnit tests over the Domain kernel and `AlphaTexRenderer`.

### Changed
- **Desktop host migrated from Photino.NET to WinForms + the official
  `Microsoft.Web.WebView2` control.** Photino's WebView2 *composition* controller
  renders a black window on the .NET 10 + WebView2-149 stack; the WinForms
  *windowed* controller renders correctly. Only the host (`Infrastructure/`) and
  the `app.js` transport shim changed — the engine, renderer, feature slices, and
  the bridge envelope contract were untouched. Rationale:
  `loom/refs/photino-net-desktop-host-reference.md`.

### Known limitations
- **Windows-only** (WinForms host). The engine stays UI-agnostic, so a
  cross-platform / web front-end remains an additive future option.
- No persistence or on-screen pickers yet (SQLite save + key/rhythm/tempo UI are
  the next phase).
- No audio-input accuracy detection (out of scope for v1).

[0.1.0]: https://github.com/reslava/chord-flow/releases/tag/v0.1.0
