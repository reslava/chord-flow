---
type: plan
id: pl_01KXV8843X046HYG6F8TJGFZPX
title: Reframe README & restructure user docs (+ song content) — v0.15.0
status: done
created: 2026-07-18
updated: 2026-07-18
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KXV812D05P3W0WH994791QZ7
requires_load: []
target_version: 0.1.0
actual_release: 0.15.0
steps:
  - id: author-the-6-showcase-tude-songs
    order: 1
    status: done
    description: Create the 6 Set-1 étude songs under default-pack/songs, each reusing named pack progressions (inline sections only where needed) with key/tempo/feel/genre metadata; build + dotnet test to confirm they parse and render.
    files_touched: [src/ChordFlow.Core/Content/default-pack/songs/]
    blocked_by: []
    satisfies: [IN1, C1, C4]
  - id: author-the-6-grab-bag-songs
    order: 2
    status: done
    description: Create the 6 Set-2 'in the style of' songs (original names, no melodies) under default-pack/songs, reusing progressions + metadata; build + dotnet test.
    files_touched: [src/ChordFlow.Core/Content/default-pack/songs/]
    blocked_by: []
    satisfies: [IN1, C1, C4]
  - id: write-docs-dsl-guide-md
    order: 3
    status: done
    description: "New user-facing, example-first authoring guide: Progressions → Rhythms → Voicings → Songs, each showing the DSL and what it renders; cover capo, whole-song tempo/feel, and per-chord {…} / voice selectors. Teaching voice, self-contained."
    files_touched: [docs/dsl-guide.md]
    blocked_by: []
    satisfies: [IN2, C2]
  - id: reframe-readme-extract-docs-dev-notes
    order: 4
    status: done
    description: Rewrite the README as a shop window (one-line what-it-is, a Highlights section, ~5 grouped feature blocks replacing the flat list, a short 'new this release' line, slim dev section), move project-layout/tests/build into new docs/dev-notes.md, and repoint the README DSL link loom/refs → docs/dsl-guide.md.
    files_touched: [README.md, docs/dev-notes.md]
    blocked_by: []
    satisfies: [IN3, IN4, IN5, IN7, C3]
  - id: refresh-docs-user-guide-md
    order: 5
    status: done
    description: Add Chord Sheets (print + play-along) and the Voicings grid to the user guide and repoint its DSL link loom/refs → docs/dsl-guide.md.
    files_touched: [docs/user-guide.md]
    blocked_by: []
    satisfies: [IN6, IN7, C2]
  - id: wire-in-screenshots-chord-sheet-pdfs
    order: 6
    status: done
    description: Place Rafa-supplied refreshed screenshots (images/screenshots/NN-name.png, incl. Chord Sheets + Voicings-grid) and exported Chord-Sheet PDFs under images/sheets/, and link them from the README highlights and the guides.
    files_touched: [README.md, docs/user-guide.md, docs/dsl-guide.md, images/sheets/]
    blocked_by: []
    satisfies: [IN8]
  - id: changelog-release-v0-15-0
    order: 7
    status: done
    description: Add a v0.15.0 CHANGELOG entry (new songs + doc restructure) and ship via the do-release flow.
    files_touched: [CHANGELOG.md]
    blocked_by: []
    satisfies: [IN9]
---
# Reframe README & restructure user docs (+ song content) — v0.15.0

## Goal

Reframe the public docs into a four-surface model — README as shop window, `docs/user-guide.md` for using the app, a new `docs/dsl-guide.md` for authoring content, and `loom/refs/*` left as the collaborator spec — and populate the song library from 3 to 15 so the app feels full out of the box, then ship it all as release v0.15.0. This is a content + docs release only: no engine, domain, or DSL-grammar change. New songs reuse the existing pack progressions (auto-discovered — no manifest edit) and must parse + render under the build/test gate; the new docs are teaching-voice and route users away from `loom/refs`.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Create the 6 Set-1 étude songs under default-pack/songs, each reusing named pack progressions (inline sections only where needed) with key/tempo/feel/genre metadata; build + dotnet test to confirm they parse and render. | src/ChordFlow.Core/Content/default-pack/songs/ | — | IN1, C1, C4 |
| ✅ | 2 | Create the 6 Set-2 'in the style of' songs (original names, no melodies) under default-pack/songs, reusing progressions + metadata; build + dotnet test. | src/ChordFlow.Core/Content/default-pack/songs/ | — | IN1, C1, C4 |
| ✅ | 3 | New user-facing, example-first authoring guide: Progressions → Rhythms → Voicings → Songs, each showing the DSL and what it renders; cover capo, whole-song tempo/feel, and per-chord {…} / voice selectors. Teaching voice, self-contained. | docs/dsl-guide.md | — | IN2, C2 |
| ✅ | 4 | Rewrite the README as a shop window (one-line what-it-is, a Highlights section, ~5 grouped feature blocks replacing the flat list, a short 'new this release' line, slim dev section), move project-layout/tests/build into new docs/dev-notes.md, and repoint the README DSL link loom/refs → docs/dsl-guide.md. | README.md, docs/dev-notes.md | — | IN3, IN4, IN5, IN7, C3 |
| ✅ | 5 | Add Chord Sheets (print + play-along) and the Voicings grid to the user guide and repoint its DSL link loom/refs → docs/dsl-guide.md. | docs/user-guide.md | — | IN6, IN7, C2 |
| ✅ | 6 | Place Rafa-supplied refreshed screenshots (images/screenshots/NN-name.png, incl. Chord Sheets + Voicings-grid) and exported Chord-Sheet PDFs under images/sheets/, and link them from the README highlights and the guides. | README.md, docs/user-guide.md, docs/dsl-guide.md, images/sheets/ | — | IN8 |
| ✅ | 7 | Add a v0.15.0 CHANGELOG entry (new songs + doc restructure) and ship via the do-release flow. | CHANGELOG.md | — | IN9 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:author-the-6-showcase-tude-songs -->
### Step 1 — Author the 6 showcase-étude songs

Songs (progressions reused): **Turnaround Workout** (`major_turnaround` + `ii_v_i`), **Minor Cadence Study** (`minor_ii_v_i` + `minor_turnaround`), **Aeolian Vamp** (`aeolian_loop`), **Andalusian Descent** (`andalusian_cadence`), **Tritone Turnaround** (`tadd_dameron_turnaround` + `tritone_sub_ii_v_i`), **Minor Blues** (`minor_12bar_blues`). Follow the existing song-DSL shape (see `blues_song_demo.dsl` / `jazz_blues_f.dsl`): metadata header (`name`/`genre`/`tags`/`key`/`tempo`/`feel`), section defs (`name: progression_ref` or inline `name = degrees`), and play lines with `x2`/`mod`. Songs are auto-discovered — no manifest edit. Gate: `dotnet build` + `dotnet test` green; optionally eyeball one in the Content preview.

<!-- step:author-the-6-grab-bag-songs -->
### Step 2 — Author the 6 grab-bag songs

Songs (progressions reused): **Ragtime Circle** (`circle_secondary_dominants`), **Falling Leaves** (`ii_v_i` + `minor_ii_v_i`), **Dameron Lane** (`tadd_dameron_turnaround` + `ii_v_i`), **Northern Lights** (`aeolian_cadence` + `aeolian_loop`), **Wistful** (`borrowed_iv` + `major_turnaround`), **Open Road** (`mixolydian_bvii` + `aeolian_cadence`). Chord changes + arrangement only — no lead lines. Gate: `dotnet build` + `dotnet test` green.

<!-- step:write-docs-dsl-guide-md -->
### Step 3 — Write docs/dsl-guide.md

Load `loom/refs/chordflow-dsl-reference.md` first as the grammar source of truth, then rewrite for users in a teaching voice with runnable examples (lean on the 15-song library for real cases). The guide must be self-contained — it does not send users into `loom/refs`.

<!-- step:reframe-readme-extract-docs-dev-notes -->
### Step 4 — Reframe README + extract docs/dev-notes.md

Highlights order: 1) Voicings Engine — a chord *reasoner* not a viewer, 2) Chord Sheets — print + play along, 3) DSL content packs. Grouped blocks: Practice & playback · Chord Sheets · The Voicings Engine · Author your own content · Guitar shape viewers. Slim dev section frames the C# music kernel as a foundation for an expandable music app and links to `docs/dev-notes.md` for the heavy detail. Keep the README short (C3).

<!-- step:refresh-docs-user-guide-md -->
### Step 5 — Refresh docs/user-guide.md

Teaching voice, consistent with the reframed README. The DSL 'make your own content' link now points at `docs/dsl-guide.md`, not `loom/refs`.

<!-- step:wire-in-screenshots-chord-sheet-pdfs -->
### Step 6 — Wire in screenshots + Chord-Sheet PDFs

Gated on user-supplied assets. Existing screenshot naming convention kept; PDFs live in the new `images/sheets/` folder. Wire links in README + user-guide + dsl-guide.

<!-- step:changelog-release-v0-15-0 -->
### Step 7 — CHANGELOG + release v0.15.0

Use the `do-release` skill: changelog finalize, version bump, build/test, record-release, tag, push, monitor the release workflow.
