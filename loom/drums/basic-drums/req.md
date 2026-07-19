---
type: req
id: rq_01KXWNSTJJ1JT5Z5YGWC1Z9RX0
title: Basic Drums — standalone groove as a first-class instrument — Requirements
status: locked
created: 2026-07-19
updated: 2026-07-19
version: 2
design_version: 1
tags: []
parent_id: de_01KXWNJTVWB1TKXS0T5CZHRNRQ
requires_load: []
---
# Basic Drums — standalone groove as a first-class instrument — Requirements

Requirements for **basic drums** — a standalone drum groove as ChordFlow's first-class 2nd instrument (MVP). Faithful extraction of the decisions in `chats/chat-001.md` + `design.md`.

### ✅ Included

- `IN1` A **`DrumGroove` domain** in `Instruments/Drums/`: `DrumGroove(Id, Name, Lanes, TimeSignature)` as a **multi-lane rhythm** over the existing 48-PPQ tick grid, `DrumLane(Voice, events)`, and a `DrumVoice` enum mapping each drum voice to its GM alphaTex articulation name.
- `IN2` A **Drums hit-grid DSL** + `DrumGrooveParser`: rows = voices; `x` = hit, `.` = no hit; `:n` subdivision (per-row + per-run); `|` bar separators; `:3`/`:6` triplet beats; the short-token voice vocabulary (`BD`/`SD`/`HH`/`OH`/`PH`/`RD`/`RB`/`CC`/`HT`/`MT`/`FT`) with full-name aliases. Fail-loud parse errors naming the bad token.
- `IN3` **Render a `DrumGroove` → alphaTex** as a percussion track: `\instrument percussion` + `\articulation defaults` + `\ts`/`\tempo`, hits as articulation-name notes, **simultaneous hits grouped in `( )`**, rests where nothing sounds.
- `IN4` A Core **`DrumGrooveDiagram`** spatial producer + a JS **DrumsR** dumb-drawer SVG component (sibling to FretR), **animated off the shared playback beat/position bus**.
- `IN5` A **Drums** dogfood surface (a standalone page): author a groove, preview it, play it, and see DrumsR animate in time.
- `IN6` A **`DrumGrooveStore : IContentStore`** (5th content kind, mirroring `RhythmPatternStore` but **with** catalog metadata: genre/subgenre/tags) wired into the bridge `entity*` CRUD family. **Surfaced as a saved-grooves library on the standalone Drums page** — *not* folded into the harmony-oriented Content editor (a groove has no comping/key/feel/tonality/sheet, so it doesn't fit that editor's chrome). "Shared editor" is satisfied by the shared entity* CRUD family + the uniform `IContentStore`, not the Content-page UI (decision: chat-001, amended after implementation).
- `IN7` **Default-pack starter grooves** shipped as `drums/*.dsl` (rock / blues shuffle / jazz swing / funk), imported through the normal pack path.
- `IN8` **Reference-doc sync** in the same units of work: `chordflow-dsl-reference.md` (the Drums DSL), `chordflow-domain-model-reference.md` (the drum types + store), `chordflow-architecture-reference.md` (`Instruments/Drums/`, DrumsR, the 5th content kind, the percussion render path).
- `IN9` A **soundfont percussion smoke test**: confirm the committed `wwwroot/soundfont/sonivox.sf2` sounds alphaTab's percussion articulations on GM channel 10 (CDP-driven).

### ⛓ Constraints

- `C1` **`Music/` stays instrument-agnostic** — all drum types live under `Instruments/Drums/`; the architecture-test-guarded `Music → Instruments` edge is not crossed. `Drums → Music.Rhythm`, `Rendering → Instruments`, `Persistence → Instruments` are the allowed edges used.
- `C2` **Two DSLs, one model** — a hit compiles to a one-cell `RhythmEvent` on the existing 48-PPQ grid; the guitar Rhythm DSL and the drums hit-grid converge at `RhythmEvent`. **No textual converter** between the two DSLs.
- `C3` **Single hit glyph** (`x` hit / `.` empty) — no sustain/rest/tie distinction; articulation variety is expressed as **separate lanes**, not glyph variants.
- `C4` **Triplets are notation, swing is performance** — shuffle/swing grooves are written with `:3`; no drums-only swing flag; any play-time swing rides the existing `\tf` path.
- `C5` **Stored form = the hit-grid DSL string only**; alphaTex is never stored (regenerated on load), consistent with every other content kind.
- `C6` **Standalone play-unit** — a groove renders and plays with **no `Song`, no `Exercise`, no key/harmony**.
- `C7` The renderer stays the **only alphaTex-aware code**; the drums render path is kept **concrete** (no premature `IInstrument` abstraction).
- `C8` **4/4 only** for this thread, consistent with the v1 render constraint.

### ❌ Excluded

- `EX1` **Drums under a song** — a drum track layered under a progression, and the `Exercise`/`Song` remodel it needs. Deferred → `drums/drums-under-a-song`.
- `EX2` **`IInstrument` extraction / renderer fork** — the polymorphic instrument seam. Deferred → `chordflow/instrument-rendering` (drums is its forcing function).
- `EX3` **Accent / ghost notes** (velocity glyphs beyond the single hit glyph). Deferred → `drums/drums-accent-ghost`.
- `EX4` **Rhythm-pattern → drum-lane import** convenience — an uncommitted nice-to-have, not built.
- `EX5` **Time signatures other than 4/4.**