---
type: req
id: rq_01KXV81KZ8HJNQE9CCQJYXRZ31
title: Reframe README & restructure user docs (+ song content) — v0.15.0 — Requirements
status: locked
created: 2026-07-18
updated: 2026-07-18
version: 1
design_version: 1
tags: []
parent_id: de_01KXV812D05P3W0WH994791QZ7
requires_load: []
---
# Reframe README & restructure user docs (+ song content) — v0.15.0 — Requirements

### ✅ Included

- `IN1` **12 new song content files** under `src/ChordFlow.Core/Content/default-pack/songs/` — the 6 showcase études (Turnaround Workout, Minor Cadence Study, Aeolian Vamp, Andalusian Descent, Tritone Turnaround, Minor Blues) + the 6 grab-bag tunes (Ragtime Circle, Falling Leaves, Dameron Lane, Northern Lights, Wistful, Open Road). Each carries `key` / `tempo` / `feel` / `genre` metadata and reuses existing pack progressions where possible (inline sections only where a shape isn't in the pack).
- `IN2` **New `docs/dsl-guide.md`** — user-facing, example-first authoring guide covering Progressions, Rhythms, Voicings, and Songs, including the newer directives users type: `capo`, whole-song `tempo` / `feel`, and per-chord `{…}` / `voice` voicing selection.
- `IN3` **README reframe** — highlights-led intro (3–4 marquee differentiators, screenshot each) + ~5 grouped feature blocks replacing the flat ~30-item list; version-agnostic feature section with a short "new this release" line.
- `IN4` **Slim inline README dev section** — brief architecture framed as the C# music kernel being a foundation for an expandable music app, with the heavy detail moved out.
- `IN5` **New `docs/dev-notes.md`** — project layout, tests, and build internals moved out of the README and linked from it.
- `IN6` **`docs/user-guide.md` refresh** — cover Chord Sheets + the Voicings grid.
- `IN7` **Repoint the DSL links** in `README.md` and `docs/user-guide.md` from `loom/refs/chordflow-dsl-reference.md` → `docs/dsl-guide.md`.
- `IN8` **Wire in Rafa-supplied screenshots + Chord-Sheet PDFs** — refreshed `images/screenshots/NN-name.png` shots (incl. a Chord Sheets and a Voicings-grid view) and exported PDFs under a new `images/sheets/` folder, linked from the README highlights + the guides.
- `IN9` **CHANGELOG entry + `do-release` v0.15.0** to ship the result.

### ❌ Excluded

- `EX1` **New rhythm sets** — deferred to a future thread.
- `EX2` **Any engine / domain / DSL-grammar code change** — this release is content + docs only.
- `EX3` **Rewrites of `loom/refs/*`** — they remain the collaborator spec, unchanged.
- `EX4` **Authored melodies / lead lines for the songs** — only chord changes + arrangement ship.

### ⛓ Constraints

- `C1` New songs must **parse and render** (build + `dotnet test` gate) before they are "done".
- `C2` Docs are **teaching-voice and example-first**; `loom/refs` stays authoritative and is not copy-pasted — `dsl-guide.md` is user-self-contained and does not send users into `loom/refs`.
- `C3` The README stays **short**; any overflow spins into a `docs/` page rather than growing the README.
- `C4` Songs reuse **existing pack progressions** where possible; **original song names**, no copyrighted titles.