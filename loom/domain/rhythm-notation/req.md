---
type: req
id: rq_01KVWA1VXAPFTVHK9NWVHHAW5Z
title: Rhythm DSL — Accurate-Notation Redesign — Requirements
status: locked
created: 2026-06-24
updated: 2026-06-24
version: 2
design_version: 4
tags: []
parent_id: de_01KVW7YEZ70AXE0NNAE82DJWTX
requires_load: []
---
# Rhythm DSL — Accurate-Notation Redesign — Requirements

### ✅ Included

- `IN1` — `RhythmPatternParser` grammar refinement: `.` is valid **only when a note is sounding** (FormatException at bar start or after `-`); `-` is one silence cell; add the `_` **tie** token.
- `IN2` — **Note-group rule:** each `X`/`_`+trailing-`.` group's duration must equal **exactly one representable value** — base (1/2/4/8/16) or single-**dotted** (1.5×). Any other count is a FormatException naming the group ("ambiguous duration — tie it with `_`"); the engine never decomposes a note silently.
- `IN3` — `_` is a **tied note**: like `X` it occupies cells and extends with `.`, but ties to the previous note (no re-attack), setting `RhythmEvent.TiedToNext` on the note it closes. `.`-after-`_` is legal (a tie is sounding). A **leading** `_` ties the bar's first note into the previous bar (`PatternBar.StartsTied`). Errors: a leading `_` on the first bar or from a pickup; a cross-bar tie whose previous bar does not end on a sounding note (dangling). *(Amended: was a zero-width boundary that had to precede an `X`.)*
- `IN4` — **Rhythm wins over harmony for ties.** A tied note is **one held slot** that re-states the **previous voicing's strings** (`-.string`), holding the previous chord across any chord change — within-bar or cross-bar. There is **no** chord-boundary tie rejection. *(Amended: was "reject a tie across a chord change.")*
- `IN5` — Model: add `RhythmEvent.TiedToNext` (default false, set by the parser), `RhythmSlot.Dotted` (default false), and `PatternBar.StartsTied` (default false, the leading-`_` cross-bar flag). `Hit(...)` default and `TiedToPrevious` unchanged.
- `IN6` — `RhythmQuantizer`: each note event → **one** slot (its single value; `Dotted` when 1.5×). A non-tied note splits at chord boundaries and re-attacks; a **tied** note (from `TiedToNext`, or the `startTied` cross-bar flag) is one **held** slot that ignores boundaries. **Remove** note beat-line coalescing (the old `LargestAlignedFit`-for-notes path).
- `IN7` — `AlphaTexRenderer`: replace both `throw` sites. `Dotted` slot → base `:N` + chord group + **`{d}`**. `TiedToPrevious` slot → re-state the **last sounding voicing's** strings with **`-.{string}`** (held — `RenderState.LastVoicing`), no `{ch}` label and no schedule entry. Tuplet `{tu N}` unchanged.
- `IN8` — Migrate every seed `.dsl` rhythm pattern to the new grammar; **re-add `charleston.dsl`** to the default pack.
- `IN9` — **Ref sync in the same unit of work:** `alphatex-syntax-reference.md` (mark `-`/`{t}` tie and `{d}`/`{dd}` dot verified; record `{lr}` for the later follow-on); `chordflow-dsl-reference.md` (Rhythm DSL: tokens, the note-group rule, `_`, examples); `chordflow-domain-model-reference.md` (`RhythmEvent.TiedToNext`, `RhythmSlot.Dotted`, `PatternBar.StartsTied`, quantizer changes).
- `IN10` — Tests: parser (`.`-after-rest error; `_` within-bar + cross-bar; dangling-tie error; non-representable group error); quantizer (dotted group → one `Dotted` slot; `_` chain → `TiedToPrevious`; tie over a boundary → held, not thrown; `startTied`; aligned rest coalescing); renderer (golden alphaTex for dotted + tied; cross-bar tie holds the previous chord; tuplet preserved).
- `IN11` — **Visually verify** in the running app: the Charleston (`:2 X.-X----` / `X...--X.--------`) and a genuinely **dotted** comp both render and play; dogfood on the score / fretboard UI page.
- `IN12` — Rests coalesce to the largest **metrically-aligned** value (a rest over beats 3-4 is one `:2 r`, not two `:4 r`); the alignment rule keeps the bar's beat structure readable. Triplet rests stay per-beat. *(Added — score clarity, requested in chat.)*

### ❌ Excluded

- `EX1` — **Let Ring** (`{lr}` playback sustain) — a follow-on; the token is verified but not wired here.
- `EX2` — Quintuplets / 32nds / any tuplet beyond the existing `:3` / `:6`.
- `EX3` — Accents, stroke direction, swing/feel **grammar** — play-time overlays; the Rhythm DSL stays timing-only.
- `EX4` — **Auto** double-dot emission — `{dd}` reserved; a double-dotted duration is authored via a `_` tie.
- `EX5` — The monospace-font and tab-only-staff display work — separate `ui/dsl-monospace-font` and `ui/staff-display-mode` threads.

### ⛓ Constraints

- `C1` — `AlphaTexRenderer` stays the **only** alphaTex-aware code; dot/tie tokens are emitted there, nowhere else.
- `C2` — `Music/Rhythm` stays a pure kernel **sink** (no alphaTex/render knowledge); the Music ← Rendering dependency direction holds (NetArchTest green).
- `C3` — **Notation only:** the grammar describes written durations; sustain is deferred to playback (`EX1`). `.` never extends silence.
- `C4` — A **breaking** grammar change is acceptable (no back-compat); all seed patterns are migrated in the same unit of work (`IN8`).
- `C5` — A passing string assertion is **not** sufficient acceptance — render + play confirmed visually (`IN11`).
- `C6` — Solution builds and **all tests stay green**.