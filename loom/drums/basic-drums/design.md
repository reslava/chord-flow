---
type: design
id: de_01KXWNJTVWB1TKXS0T5CZHRNRQ
title: Basic Drums — standalone groove as a first-class instrument
status: done
created: 2026-07-19
version: 1
idea_version: 1
tags: []
parent_id: id_01KXWJV6XD6K54AZP4D7K8NXVR
requires_load: []
---
# Basic Drums — standalone groove as a first-class instrument

Design for **basic drums** in ChordFlow. Distills the decisions reached in `chats/chat-001.md`. Load the DSL / domain-model / architecture refs before implementing — this thread touches all three.

## 1. North star & the two-phase split

Drums are ChordFlow's **first-class 2nd instrument**, not an internal drum machine — the concrete first caller that lights up the long-deferred `IInstrument` seam.

- **This thread (MVP) = a standalone drum groove.** A groove is its own play-unit: hit-grid DSL → `DrumGroove` → alphaTex percussion track → play + animate. **No harmony, no key, no `Exercise`, no `Song`.** Delivered as a new **first-class content kind** (a 5th entity alongside progression / song / rhythm / voicing).
- **Phase 2 (deferred → `drums/drums-under-a-song`) = drums under a progression.** A groove becomes a 3rd `\track` layered under a harmonic exercise; that is where the `Exercise`/`Song` remodel gets designed *on purpose*.

**Sequencing decision (locked):** *concrete slice first, extract the interface after.* We build the drums vertical slice end-to-end, get it playing, and only **then** extract the shared `IInstrument` / renderer seam by diffing the guitar renderer against the drums renderer — never extract an abstraction ahead of its second implementation. The extraction itself is the existing active `chordflow/instrument-rendering` thread; drums is its forcing function.

## 2. The governing insight — a groove is a multi-lane rhythm

The strongest asset we already own is the **48-PPQ tick grid** (`TickGrid` / `RhythmEvent`). A drum groove is just **N lanes** of that grid, one lane per drum voice. Every hit compiles to a one-cell `RhythmEvent` — the drums notation is a *view*, the tick grid is the *model*.

```
DrumGroove(Id, Name, IReadOnlyList<DrumLane> Lanes, TimeSignature)   // Instruments/Drums/
DrumLane(DrumVoice Voice, <RhythmEvent[] on the shared 48-PPQ grid>)
DrumVoice (enum) → GM articulation name
```

**Placement (keeps `Music/` provably instrument-agnostic):**

- `DrumGroove` / `DrumVoice` / `DrumGrooveParser` / `DrumGrooveDiagram` → **`Instruments/Drums/`** — the voice→articulation vocabulary is GM-percussion-specific, an instrument concern.
- Per-lane *timing* **reuses** `Music/Rhythm` primitives. `Drums → Music.Rhythm` is a legal edge; the architecture-test-guarded `Music → Instruments` edge stays clean.
- The renderer consumes `DrumGroove` via the allowed `Rendering → Instruments` edge; persistence via the allowed `Persistence → Instruments` edge.

## 3. The Drums hit-grid DSL (a 5th DSL)

**Decision: a drums-specific hit-grid, NOT the guitar Rhythm DSL — and no converter between them.**

The blocker for reusing the Rhythm DSL is one glyph: there `.` = **sustain** and `-` = rest. Drum hits are instantaneous — nothing rings — so a drummer's `.` naturally means **no hit**. Two DSLs, **one model**: both parse into `RhythmEvent`s, which *is* the shared canonical form. A textual Rhythm-DSL ↔ hit-grid converter was rejected as fundamentally lossy (durations vs onsets can't round-trip) and musically pointless.

### Grammar

- **Rows = voices.** Each row: `<VOICE> [:n] <cells>`.
- **Glyphs:** `x` = hit · `.` = no hit. **Single hit glyph** — a hit is instantaneous; there is no sustain/rest/tie distinction.
- **`:n` subdivision** (required — a grid still declares cell width): reuse the Rhythm DSL's meaning — `:1` quarters, `:2` eighths, `:3` eighth-triplets, `:4` sixteenths (default), `:6` 16th-triplets. Per-row leading `:n`; per-run `:n` for mixing straight & triplet beats in one bar.
- **`|` separates bars.** A multi-bar groove tiles cyclically onto a longer form (phase 2).
- **Triplets are notation, swing is performance (locked, matches C4):** shuffle/swing grooves are written with `:3` (a literal triplet figure); we do **not** add a drums-only swing flag. If a play-time swing feel is chosen it rides the same `\tf` path as everything else.

### Voice vocabulary — short token canonical, full-name aliases

Articulation variety is expressed as **separate lanes, not glyph variants** (open hi-hat is its own `OH` row) — which is exactly right for GM percussion, where each is a distinct MIDI note.

| Short (canonical) | Full aliases | alphaTex articulation |
|-------------------|--------------|-----------------------|
| `BD` | `Kick`, `KD` | `KickHit` |
| `SD` | `Snare` | `SnareHit` |
| `HH` | `HiHat`, `CH` | `HiHatClosed` |
| `OH` | `OpenHat` | `HiHatOpen` |
| `PH` | `FootHat`, `HF` | `HiHatPedal` |
| `RD` | `Ride` | `RideHit` |
| `RB` | `RideBell` | `RideBell` |
| `CC` | `Crash` | `CrashHit` |
| `HT` | `HighTom` | `HighTomHit` |
| `MT` | `MidTom` | `MidTomHit` |
| `FT` | `FloorTom` | `LowFloorTomHit` |

### Example — basic rock beat

```text
HH :2 x x x x x x x x
SD :2 . . x . . . x .
BD :2 x . . . x . . .
```

**Deferred (→ `drums/drums-accent-ghost`):** accent / ghost notes are *velocity*, not a distinct GM note, so a single hit glyph can't express them. Add later as an optional glyph (e.g. `X` accented / `g` ghost).

## 4. Rendering — groove → alphaTex

A `DrumGrooveRenderer` (or a percussion branch of the render path — decided in code) emits:

- staff header `\instrument percussion` + `\articulation defaults` + `\ts` + `\tempo`;
- per tick position, the set of voices hitting there as articulation-name notes — **simultaneous hits grouped in `( )`** (`(HiHatClosed KickHit)`), `r` where nothing sounds;
- durations from the tick grid via the existing quantizer machinery (a hit is one cell; longer gaps coalesce to rests).

The renderer stays the **only alphaTex-aware code** (the existing seam). We keep the drums path concrete for now; the `IInstrument` extraction (diffing this against the guitar renderer) is `chordflow/instrument-rendering`.

**De-risk (first slice, not a blocker):** confirm our committed `wwwroot/soundfont/sonivox.sf2` routes alphaTab's `\instrument percussion` + `\articulation defaults` notes to **GM channel-10 percussion** — a ~5-minute CDP smoke test (render a percussion groove, hear it). The risk is alphaTab's articulation path × *this specific file*, not "do SF2s support drums" (they do — confirmed).

## 5. DrumsR — the SVG render component (sibling to FretR)

Core produces a spatial **`DrumGrooveDiagram`** (the drums twin of `FretboardDiagram`); the JS **DrumsR** is a **dumb drawer** (zero music theory in JS) that draws the lane grid / a simple kit and **animates it off the shared playback beat/position bus** — the same infrastructure the sheet marker and now/next fretboards already ride. This gives the animated HH/SD/BD for near-free.

Dogfood (guitar-weave rule, applied to drums): the groove renders + animates on a **Content › Drums** page — fast visual+audible confirmation before phase 2 builds on top. Add this line to the idea's Validation.

## 6. Persistence & CRUD — the 5th content kind

- **`DrumGrooveStore : IContentStore`** mirroring `RhythmPatternStore`, but **with catalog metadata** (`genre`/`subgenre`/`tags`) — grooves are genre-tagged (rock / blues / funk / jazz). Stored form = the **hit-grid DSL string** (the only persisted form; alphaTex never stored), consistent with every other kind storing its own DSL.
- A **5th entity discriminator** in the bridge `entity*` CRUD family + the shared Content editor, with a **score-only-style preview** (like rhythm patterns) plus the DrumsR view.
- **Default pack:** ship a starter set of grooves as `drums/*.dsl` in the on-disk default pack (rock / blues shuffle / jazz swing / funk from the idea's research) — content is data, not code.

## 7. Reference-doc sync owed by this thread (required)

Per `CLAUDE-LOCAL.md`, these ship *in the same units of work* as the code:

- **`chordflow-dsl-reference.md`** — the new Drums hit-grid DSL (grammar, glyphs, voice table).
- **`chordflow-domain-model-reference.md`** — `DrumGroove` / `DrumLane` / `DrumVoice` / `DrumGrooveParser` / `DrumGrooveDiagram`, and the `DrumGrooveStore` row.
- **`chordflow-architecture-reference.md`** — `Instruments/Drums/`, DrumsR, the 5th content kind, the percussion render path.

## 8. Scope summary

**In (this thread):** `Instruments/Drums/` domain · hit-grid DSL + parser · groove → alphaTex · DrumsR + Content › Drums page · `DrumGrooveStore` + CRUD + default-pack grooves · the three ref updates · the soundfont smoke test.

**Out (tracked as threads):** drums-under-a-song / the drum track / `Exercise` remodel → `drums/drums-under-a-song` · `IInstrument` extraction + renderer fork → `chordflow/instrument-rendering` · accent/ghost velocity glyphs → `drums/drums-accent-ghost`.

## Open questions to settle at req time

- Is **Content › Drums** a new top-level nav view, or a kind inside the existing Content page? (Lean: a kind in Content, like the others.)
- Exact hit-grid edge cases for the parser (empty lanes, a row shorter/longer than the bar, unknown voice token → fail-loud message shape).
- Minimum starter-groove set for the default pack.
