---
type: req
id: rq_01KXXQAMV3CJ1BPQSPXQJTPGSY
title: Drums under a song — the drum track (phase 2) — Requirements
status: locked
created: 2026-07-19
updated: 2026-07-19
version: 1
design_version: 1
tags: []
parent_id: de_01KXXQ9ECKBQZP1AN8WYM7KRNT
requires_load: []
---
# Drums under a song — the drum track (phase 2) — Requirements

Requirements for **drums under a song** — a `DrumGroove` playing beneath a harmonic exercise, via a play-unit remodel. Settled with Rafa in `chat-001`; built on the `design.md` decisions D1–D5.

### ✅ Included

- `IN1` — Remodel the play-unit to a **typed instrument-parts union**: `abstract InstrumentPart { Volume, Muted }` with arms `CompingPart(RhythmPattern)` / `LeadPart(RhythmPattern)` / `DrumPart(DrumGroove)`. `Exercise` holds `IReadOnlyList<InstrumentPart> Parts` (replacing the flat `Comping`/`Lead` fields) plus the shared context `Song`/`KeyOverride`/`Tempo`/`Difficulty`/`TripletFeel`. Add intent accessors (`Comping`/`Lead`/`Drums`).
- `IN2` — A `DrumPart` layers a `DrumGroove` **under** the harmonic exercise: rendered as a 3rd `\track` percussion staff, audible alongside comping (and lead when present).
- `IN3` — The groove **tiles cyclically per bar** (`song bar i → groove bar i mod m`), independent of the comping pattern's bar count; both tile over the song's total bars.
- `IN4` — `HarmonyControlsR` gains a **Drums picker** (populated via `entityList` with `entity:"drums"`) + a **volume slider** bound to the page engine; the part enters `getDefinition()`. The `generate` verb carries an optional `drumGrooveId` + `drumVolume`; a blank selection ⇒ no drum part.
- `IN5` — A **display-only show/hide toggle** for the drum staff (flips staff visibility via `api.render()`, no C# re-render). Audio is always emitted; `Muted`/volume control audio independently of visibility.
- `IN6` — One **song-level `\tf`** applies to the whole score including the drum track; authored-swing grooves and `\tf` compose without double-swing.
- `IN7` — Persistence: `ExerciseEntity` gains a nullable `DrumGrooveId` + per-part volume/mute columns via a **flat mapper** (`Exercise ↔ ExerciseEntity`) + an EF migration; **save → reload restores** the chosen groove.
- `IN8` — `ExerciseRefs.ResolveDrumGroove(id)` resolves the selected/stored groove via `DrumGrooveStore.Find` (optional; blank ⇒ null). Both `GenerateExercise.Build` and the saved-exercise load path use it.
- `IN9` — **Reference-doc sync (same unit of work):** update `chordflow-domain-model-reference.md` (the `Exercise` pipeline + parts union) and `chordflow-architecture-reference.md` (the play-unit remodel, the drum track in the render path, the `HarmonyControlsR` picker).

### ⛓ Constraints

- `C1` — The parts union lives in `Exercises/`; drum types stay in `Instruments/Drums`; the drum-track emission sits on the allowed `Rendering → Instruments` edge. The `Music → Instruments` architecture test **stays green**.
- `C2` — `AlphaTexRenderer` stays **pure/store-free** (the `Exercise → RealizedSong` I/O expansion stays in Features). The renderer receives the **extracted typed pieces** (comping plan + optional lead + optional drums), not the `InstrumentPart` union.
- `C3` — The drum track uses the **concrete `DrumGrooveRenderer`** — **no dependency on `chordflow/instrument-rendering`**, no `IInstrument` introduced here.
- `C4` — Exactly **one `CompingPart` required**; **at most one** `LeadPart` and one `DrumPart` in v1. Per-part mix on the part; key/tempo/feel/difficulty on `Exercise`.
- `C5` — `Song` stays **instrument-agnostic** — no groove or instrument reference on `Song`.
- `C6` — **4/4 only** for v1 (matches the engine).
- `C7` — Persistence stays **flat (columns + mapper)**; a later swap to a child part table must remain a **non-breaking internal** change (no ripple into the domain `Exercise` or the renderer).

### ❌ Excluded

- `EX1` — **Bass** — the union leaves room (`BassPart`) but no bass instrument, content kind, or UI ships in v1.
- `EX2` — **DrumsR-in-Practice** — the animated drum grid synced under the song; v1 is the percussion **staff + audio** only (fast-follow).
- `EX3` — **Dynamic-roster persistence** — a child `ExercisePartEntity` table.
- `EX4` — **Composable puzzle-pieces vision** — interchangeable Songs/Progressions/Rhythms/Grooves as first-class pieces; groove-as-`Song`-default.
- `EX5` — **Per-part difficulty/voicing scoping** — stays `Exercise`-level in v1.
- `EX6` — **Multiple same-role parts** (e.g. two rhythm guitars) — allowed by the list shape, not exercised in v1.