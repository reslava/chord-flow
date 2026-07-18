---
type: design
id: de_01KXV812D05P3W0WH994791QZ7
title: Reframe README & restructure user docs (+ song content) — v0.15.0
status: done
created: 2026-07-18
version: 1
tags: []
parent_id: null
requires_load: []
---
# Reframe README & restructure user docs (+ song content) — v0.15.0

## Problem

The public docs have drifted out of shape as features piled up:

- **The README is a flat ~30-item feature list** under a version-stamped `Features (v0.14.0)` heading — it reads like a changelog dump, not a shop window. There is no grouping and no highlighting of what actually makes ChordFlow *different*.
- **User-facing docs point users into collaborator docs.** Both `README.md` and `docs/user-guide.md` link users to `loom/refs/chordflow-dsl-reference.md` for the DSL — i.e. straight into the internal system-of-record, which is written for us and collaborators, not end users.
- **There is no user-facing authoring guide.** The DSLs (rhythms, voicings, progressions, songs) have no teaching-voice, example-first home for a downloader who wants to write their own content.
- **The content library feels empty.** The pack ships **3 songs** (all blues) over **21 progressions** — a user opening the app sees almost nothing to play and is pushed to author everything from scratch.

These are three surfaces with three audiences that have blurred together. This thread untangles them and lands the result as release **v0.15.0**.

## The core decision — a four-surface doc model

Separate the docs by **audience + purpose**, and route every cross-link to the surface that matches the reader:

| Surface | Purpose | Audience |
|---|---|---|
| `README.md` | Shop window — what it is, headline features, screenshots, download | GitHub visitor / prospective user |
| `docs/user-guide.md` | How to *use* the app (task walkthroughs) | Someone who downloaded it |
| `docs/dsl-guide.md` *(new)* | How to *author* content — rhythms, voicings, progressions, songs, example-first | User who wants to write their own |
| `loom/refs/*` | Internal system-of-record / spec | Us + collaborators (unchanged) |

The one concrete bug this fixes: user paths currently dead-end in `loom/refs`. After this change, users stay inside `README` + `docs/`, and `loom/refs` is reachable only as an explicit "for contributors" pointer.

## README information architecture

Restructure from a flat list into:

1. **One-line what-it-is** (keep).
2. **Highlights** — 3–4 marquee items, the genuinely *different* things, each with a screenshot. Ordered by differentiation:
   1. **The Voicings Engine — a chord *reasoner*, not a viewer** (the north-star differentiator: derives + explains grips).
   2. **Chord Sheets — print *and* play along** (newest, most demo-able; the exported PDF is the proof).
   3. **Everything authored in text DSLs + shipped as content packs** (the open-core hook).
3. **Feature groups** — the current ~30 bullets folded into ~5 themed blocks (*Practice & playback · Chord Sheets · The Voicings Engine · Author your own content · Guitar shape viewers*), each 2–4 tight lines. Version-agnostic; a short "new this release" line sits at the top and the CHANGELOG carries per-release detail.
4. **Screenshots** (refreshed).
5. **Download & install** (keep).
6. **Slim dev section** — architecture in brief, framed as *the C# music kernel is a foundation for an expandable music app*, then a link out to the new dev doc for the heavy detail.

If the README still runs long after grouping, the overflow candidate to spin into a `docs/` page is the exhaustive feature catalogue.

## docs/dsl-guide.md (new)

Four sections, each **example-first** — show the DSL, show what it renders — in a teaching voice: **Progressions → Rhythms → Voicings → Songs** (arranging the first three). It covers the practical grammar a user actually types, including the newer directives: `capo`, whole-song `tempo` / `feel`, and per-chord `{…}` / `voice <selector> = …` voicing selection.

**dsl-guide ↔ loom/refs relationship (trade-off).** Both describe the grammar, so there is intentional overlap. The split is by *voice and audience*, not by content ownership: `loom/refs/chordflow-dsl-reference.md` stays the exhaustive, edge-case-complete **spec** for contributors; `dsl-guide.md` is the **self-contained teaching** version for users and does **not** send the reader back into `loom/refs`. They are kept consistent but written independently — not copy-pasted. (Accepting a little duplication is the right call here: forcing users into the spec doc to "avoid duplication" would re-introduce the exact audience-blur we're removing.)

## Dev-doc extraction

Move the deep developer matter — full **Project layout**, the **test-count** paragraph, and **build internals** — out of the README into a new `docs/dev-notes.md`, linked from the README's slim dev section. The README keeps only enough architecture to convey the "expandable foundation" story.

## Song content — populate the library

Add **12 songs** (library 3 → 15), each reusing existing pack progressions where possible (inline sections only where a shape isn't in the pack), and each carrying `key` / `tempo` / `feel` / `genre` metadata so the library also dogfoods those directives.

**Set 1 — Showcase études** (one progression family each): Turnaround Workout (`major_turnaround` + `ii_v_i`), Minor Cadence Study (`minor_ii_v_i` + `minor_turnaround`), Aeolian Vamp (`aeolian_loop`), Andalusian Descent (`andalusian_cadence`), Tritone Turnaround (`tadd_dameron_turnaround` + `tritone_sub_ii_v_i`), Minor Blues (`minor_12bar_blues`).

**Set 2 — Grab-bag** ("in the style of", original names): Ragtime Circle (`circle_secondary_dominants`), Falling Leaves (`ii_v_i` + `minor_ii_v_i`), Dameron Lane (`tadd_dameron_turnaround` + `ii_v_i`), Northern Lights (`aeolian_cadence` + `aeolian_loop`), Wistful (`borrowed_iv` + `major_turnaround`), Open Road (`mixolydian_bvii` + `aeolian_cadence`).

**Naming / copyright policy.** Songs ship **chord changes + arrangement only — never a melody**. Set 2 uses deliberately original names that evoke, but do not reproduce, known tunes. Chord progressions are not copyrightable and no melodic content is authored, so this is clean.

## Screenshots & PDFs

Rafa supplies the assets; this thread wires them in. New/updated app screenshots keep the existing `images/screenshots/NN-name.png` convention (re-shoot changed views + add a Chord Sheets and a Voicings-grid shot). Exported **Chord-Sheet PDFs** go in a new `images/sheets/` folder, linked from the README highlight and the guides — a printable PDF is the strongest single proof of the Chord Sheets feature.

## Sequencing → v0.15.0

1. Author the 12 songs (content) + verify they parse and render.
2. Write `docs/dsl-guide.md`.
3. Reframe the README + repoint its DSL link; extract `docs/dev-notes.md`.
4. Refresh `docs/user-guide.md` (Chord Sheets, Voicings grid) + repoint its DSL link.
5. Wire in Rafa's screenshots + Chord-Sheet PDFs (`images/sheets/`).
6. CHANGELOG entry + `do-release` v0.15.0.

## Trade-offs settled

- **README length vs completeness** → grouped highlights win; the exhaustive list can overflow into `docs/`.
- **dsl-guide vs loom/refs duplication** → intentional, audience-driven overlap; teaching doc stays user-self-contained.
- **Content vs substrate** → this release deliberately spends its effort on *content the user sees* (songs) and *docs*, not more engine substrate. Rhythm sets are explicitly deferred to a future thread.
- **Scope discipline** → content + docs only; **no** engine/domain/DSL-grammar code change in this release.