---
type: done
id: pl_01KXV8843X046HYG6F8TJGFZPX-done
title: Done — Reframe README & restructure user docs (+ song content) — v0.15.0
status: done
created: 2026-07-18
version: 7
tags: []
parent_id: pl_01KXV8843X046HYG6F8TJGFZPX
requires_load: []
---
# Done — Reframe README & restructure user docs (+ song content) — v0.15.0

## Step 1 — Create the 6 Set-1 étude songs under default-pack/songs, each reusing named pack progressions (inline sections only where needed) with key/tempo/feel/genre metadata; build + dotnet test to confirm they parse and render.

Authored the 6 Set-1 showcase-étude songs under `src/ChordFlow.Core/Content/default-pack/songs/`, each as a reference-form Song (`NAME: progression_id`) reusing existing pack progressions, with `key`/`tempo`/`feel`/`genre`/`subgenre`/`tags`/`description` metadata:

- `turnaround_workout.dsl` — `major_turnaround` + `ii_v_i`, key C, 140, triplet8th (Jazz)
- `minor_cadence_study.dsl` — `minor_turnaround` + `minor_ii_v_i`, key Am, 120, triplet8th (Jazz)
- `aeolian_vamp.dsl` — `aeolian_loop` x4, key Em, 100, straight (Rock)
- `andalusian_descent.dsl` — `andalusian_cadence` x4, key Am, 100, straight (Flamenco)
- `tritone_turnaround.dsl` — `tadd_dameron_turnaround` + `tritone_sub_ii_v_i`, key C, 140, triplet8th (Jazz)
- `minor_blues.dsl` — `minor_12bar_blues` x2, key Am, 90, triplet8th (Blues)

Songs are auto-discovered from the folder — no manifest edit. Minor songs use minor keys (`key Am`/`key Em`) over the `tonality: minor` progressions, consistent with the first-class-minor-keys model.

**Verification (C1):** `SongSeedTests.EveryDefaultSong_Parses_Expands_AndRenders` enumerates every default-pack song and asserts parse → expand → render, so it validated the 6 new songs with no test edit needed. Targeted run green (2/2), then the full `dotnet test` suite green (**1058/1058**).

## Step 2 — Create the 6 Set-2 'in the style of' songs (original names, no melodies) under default-pack/songs, reusing progressions + metadata; build + dotnet test.

Authored the 6 Set-2 grab-bag songs (original names, chord changes + arrangement only — no melodies) under `src/ChordFlow.Core/Content/default-pack/songs/`:

- `ragtime_circle.dsl` — `circle_secondary_dominants` x2, key C, 160, triplet8th (Jazz/Ragtime)
- `falling_leaves.dsl` — `ii_v_i` then `mod vi` → `minor_ii_v_i`, key G, 110, triplet8th (Jazz) — demonstrates the relative major↔minor ii-V pairing via `mod vi` (relative-minor modulation)
- `dameron_lane.dsl` — `ii_v_i` head x2 + `tadd_dameron_turnaround`, key C, 150, triplet8th (Bebop)
- `northern_lights.dsl` — `aeolian_loop` (minor vamp) then `key C` reset + `aeolian_cadence` (parallel-major lift), key Cm→C, 90, straight (Rock/Cinematic) — demonstrates a mid-song absolute key reset across modes
- `wistful.dsl` — `major_turnaround` verse x2 + `borrowed_iv` tag, key C, 80, straight (Pop ballad)
- `open_road.dsl` — `mixolydian_bvii` riff + `aeolian_cadence` lift, key D, 120, straight (Classic Rock)

Two songs exercise cross-mode arrangement: **Falling Leaves** via `mod vi` (major → relative minor) and **Northern Lights** via an absolute `key Cm`→`key C` reset — both confirmed to parse/expand/render.

**Verification (C1):** targeted `SongSeedTests` green (2/2), full `dotnet test` suite green (**1058/1058**). Library is now 15 songs (was 3).

## Step 3 — New user-facing, example-first authoring guide: Progressions → Rhythms → Voicings → Songs, each showing the DSL and what it renders; cover capo, whole-song tempo/feel, and per-chord {…} / voice selectors. Teaching voice, self-contained.

Created `docs/dsl-guide.md` — a user-facing, example-first authoring guide with four sections: **Progressions** (degrees, splits, qualities, chromatic #/b, even-split vs `:n` slots, minor tonic-relative authoring), **Rhythms** (the four glyphs `X . - _`, `:n` subdivision, bars/pickups, feel-is-play-time note), **Voicings** (the movable-shape line + CAGED examples), and **Songs** (definitions, arrangement stream, `mod` specs, `@take`, and the `capo`/`tempo`/`feel` play-time seeds + per-chord `{…}` / `voice` selectors).

Decisions:
- Teaching voice with runnable examples; grounded in real progression ids (`12bar_blues`, `ii_v_i`, `minor_ii_v_i`, `major_turnaround`) and the shipped **Falling Leaves** `mod vi` example (IN2).
- **Self-contained (C2):** the guide does not send users into `loom/refs`; a single closing line points *contributors* there. loom/refs is not duplicated — this is an independent teaching rendering.
- Header image + relative links follow the existing `docs/user-guide.md` convention (`../images/icon.png`), and it cross-links to `user-guide.md`.

Docs-only change — no build/test; verified by read-through.

## Step 4 — Rewrite the README as a shop window (one-line what-it-is, a Highlights section, ~5 grouped feature blocks replacing the flat list, a short 'new this release' line, slim dev section), move project-layout/tests/build into new docs/dev-notes.md, and repoint the README DSL link loom/refs → docs/dsl-guide.md.

**Reframed `README.md`** (IN3, IN4, IN7, C3) and **extracted `docs/dev-notes.md`** (IN5).

README changes:
- One-line what-it-is + a **"What makes it different"** callout + a short **"New in v0.15.0"** line (song library + rebuilt docs).
- New **Highlights** section — 3 marquee differentiators (Voicings Engine reasoner · Chord Sheets print+play · music-as-text DSLs), with two screenshots.
- The flat ~30-bullet list replaced by **grouped feature blocks**: Practice & playback · Chord Sheets · The Voicings Engine · Author your own content · Guitar shape viewers · Save & sound. Version-agnostic; "full history in CHANGELOG".
- **Slim "How it's built" dev section** framing the C# `Music/` kernel as a durable foundation an expandable music app grows on, linking to dev-notes + loom/refs.
- **DSL link repointed** loom/refs → `docs/dsl-guide.md` (both the Documentation list and the inline "Progression & Song DSL" link); added a **Developer Notes** doc link.
- Kept Screenshots, Download & install, Soundfonts (the `#soundfonts` anchor the user guide links to is preserved), Developed-with-Loom, and licenses.

`docs/dev-notes.md` (new) holds the moved developer matter: Tech stack, build Requirements, Build & run, Tests (with the default-pack seed-test note), Project layout (added the `Content/` line), and a content-authoring pointer to the DSL guide.

Verified all README/dev-notes relative link targets exist (docs, CHANGELOG, screenshots, loom/refs). Docs-only — no build/test.

## Step 5 — Add Chord Sheets (print + play-along) and the Voicings grid to the user guide and repoint its DSL link loom/refs → docs/dsl-guide.md.

Refreshed `docs/user-guide.md` (IN6, IN7, C2):

- **New §4 "Chord Sheets — print and play along"** — layouts (leadsheet / bar-grid), any key, letter/Nashville/Roman names, chord-tone strip + fret diagram, `%` similes, SVG/PNG/PDF export, and the play-along marker (visual metronome / per-chord).
- **New §5 "Explore chords & voicings"** — the Voicings grid + filter stack, CAGED Chords page, Scales / CAGED Shapes, colour = interval, light/dark theme.
- Renumbered the following sections (Make your own content → §6, Soundfonts → §7, Known limits → §8) and updated the intro paragraph to mention chord sheets + voicings.
- **Fixed a now-false claim:** §1 previously said "there's no song library to browse"; updated to describe the 15-song starter library (songs are now real content).
- **Repointed the DSL link** loom/refs → `docs/dsl-guide.md` (IN7); the top "developer docs" pointer now goes to `dev-notes.md` instead of loom/refs, so **no loom/refs link remains for users** (C2). The `README.md#soundfonts` anchor is preserved and still resolves.

Verified links via grep. Docs-only — no build/test.

## Step 6 — Place Rafa-supplied refreshed screenshots (images/screenshots/NN-name.png, incl. Chord Sheets + Voicings-grid) and exported Chord-Sheet PDFs under images/sheets/, and link them from the README highlights and the guides.

Wired Rafa's supplied assets into the docs (IN8).

**Assets received:** 9 new screenshots (`images/screenshots/01-practice-score` … `09-voicings-engine.png` — a renamed/expanded set that **replaced** the old `01-practice.png`/`02-content-progressions.png`/… names my step-4/5 drafts referenced), a demo GIF (`images/chord-flow-demo.gif`, visual metronome + animated score), and 3 exported chord-sheet PDFs under `images/sheets/`.

**README.md:**
- Added the demo **GIF** centered under the intro (shop-window animation).
- Highlights image row → `08-voicings.png` + `02-practice-chord-sheet.png`.
- Added **sample-PDF** links to the Chord Sheets highlight (Blues Song Demo / Jazz Blues in F / Ragtime Circle).
- Rebuilt the Screenshots gallery onto the new filenames (8-shot spread).

**docs/user-guide.md:**
- §3 Practice figure → `01-practice-score.png`.
- §4 Chord Sheets → added a chord-sheet figure, the demo GIF, and the 3 sample-PDF links.
- §5 Voicings → added `08-voicings.png` + `09-voicings-engine.png`.
- §6 Content figures → new filenames + a 4th shot (voicings); shows all four content types.

**Decisions:**
- Renamed the mistyped PDF `raftime-circle.pdf` → **`ragtime-circle.pdf`** (matches the Ragtime Circle song).
- `docs/dsl-guide.md` was in the step's file list but kept **text-only** (it's a grammar reference; screenshots would distract) — no change needed there.
- `05-content-rhythms.png` is Rafa's *old* (pre-fix) shot; it's referenced in the user guide by its stable name and Rafa will **overwrite it in place** with a fresh capture once the rhythms-page bug is fixed.

Verified every image and markdown-PDF reference in README + docs resolves to a real file.

## Step 7 — Add a v0.15.0 CHANGELOG entry (new songs + doc restructure) and ship via the do-release flow.

Authored the **v0.15.0** CHANGELOG entry (Highlights → Added / Changed / Fixed) covering the full unreleased backlog — the 24 Unreleased done plans (first-class minor keys end-to-end; chord-sheet play-along with the metronome-true visual marker; Now/Next boards on sheets; the harmonic-analysis overlay; pickup/anacrusis lead-in cells; the unified Score⇄Sheet Practice page; metronome/count-in + several regression fixes) **plus** this thread's 15-song library, the new DSL/dev guides, and the rhythms-preview fix — synthesized in-session from `loom report release-notes`.

Brought the docs current with the full release scope (per Rafa: "update anything in documentation you catch"): README's "New in v0.15.0" callout + the Practice / Chord Sheets / Authoring feature blocks (minor keys, play-along marker, analysis overlay, Now/Next, Score⇄Sheet toggle); user-guide (major/minor Key note + the Chord-Sheets analysis/Now-Next line); dev-notes test count → 1058. Added the `[0.15.0]` link reference.

Coverage-net clean: every commit since v0.14.0 maps to an Unreleased done plan; no stale-leak (all done docs dated after the v0.14.0 tag). Shipping via the do-release flow (csproj → 0.15.0, `build -c Release` + `test -c Release`, `record-release`, annotated tag `v0.15.0`, push + workflow watch).
