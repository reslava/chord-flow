---
type: idea
id: id_01KXGQHYZ9WS1YFBQRWYXQWHSJ
title: Chord Sheets — ChordSheetR render component (leadsheet + grid layouts)
status: done
created: 2026-07-14
version: 1
tags: []
parent_id: null
requires_load: []
---
# Chord Sheets — ChordSheetR render component (leadsheet + grid layouts)

## What

Generate **chord sheets** for songs — an **in-app view you can also export** (SVG → PNG/PDF). A new JS render component, **`ChordSheetR`**, a **sibling of ScoreR**, fed a structured `ChordSheet` model over the bridge. Two layouts render from the *same* model:

- **Layout A — flowing engraved leadsheet** (the `chordsheet.com` idiom): 4 bars/row, `|` barlines, boxed section tags, superscript qualities, slash chords as built-up fractions. Reference: `docs/internal/chord-sheets/layout-A.pdf` (+ `-nashville`).
- **Layout B — fixed grid**: one box per bar, 4 columns, bordered section blocks, multi-chord cells. Reference: `docs/internal/chord-sheets/layout-B1/B2/B3.png`.

This is the **reasoner-on-a-page**: a chord sheet that doesn't just show chords but *explains* them, using the engine we already have.

## Architecture

- **`ChordSheet` model lives in `ChordFlow.Core`** (host-neutral): sections → rows → bars → chord(s) + header metadata. Core fills every field it *can* from the existing kernel; the JS component decides which fields to *paint*. All music logic stays in Core, all pixels in JS.
- **`ChordSheetR` (JS)** mirrors ScoreR: owns its render surface, takes an options object, composes **SVG** (SVG is the primitive — **PNG = rasterized SVG, PDF = SVG on a page**). Layout A and B are two render *modes* over one model.
- **Theming = FretR's exact pattern** — CSS custom properties, `light`/`dark`/`auto`; **export pins the light token set** (PDF is light regardless of on-screen theme). Both layouts share the palette so they read as one system.

Model sketch:
```
ChordSheet
  header: { title, artist, key, tempo, feel, timeSig, capo? }
  sections: [ Section { label, rows: [ Row { cells: [ Cell ] } ] } ]
  Cell: { chords: [ ChordRef ], repeatOfPrev? (→ %), span/beats? }
  ChordRef: { concrete (C, Fma7, F/C), degree (1,4,5-,#4), tones[], analysis?, voicing? }
```

## v1 scope (ship the reasoner-on-a-page)

- **Both layouts** A (leadsheet) and B (grid), as render modes over one model.
- **Notation modes**: letter / Nashville / Roman-function — with **primary token + an optional small secondary line** (e.g. `C` big, `I` small), toggleable.
- **Function label = strictly the honest diatonic degree** we already have. No secondary-dominant / borrowed guessing in v1 (that comes from the [[harmonic-analysis]] pass).
- **Key + capo realization** — any key, **song key by default**, via `Transposer`; capo-aware dual display where the Song carries a capo.
- **Below-cell adornment = both, as a per-sheet toggle**:
  - **Tone strip** — the chord's spelled pitch classes, with a **note-names ↔ interval-degrees toggle** (`C E G` ⇄ `R 3 5`), colour-coded by function in FretR's language (dogfoods `NoteSpeller`/`IntervalSpeller`).
  - **Fret diagram** — via FretR, using the **comping / difficulty-band voicing selection** already used elsewhere.
- **`%` simile** derived by comparing a bar to its predecessor (no authoring) — both layouts.
- **Header block** (title/artist/key/tempo/feel/time-sig) + **boxed section tags**.
- **Harmonic-rhythm-aware cell splitting** — a 2-chord bar splits the cell (beat-proportional widths a nice-follow).
- **Export**: SVG → PNG/PDF, light theme pinned on export.

## Deferred (captured, not v1)

- **Animated playback marker** — current-bar highlight (reuse Layout B's lighter-border state) driven by the existing `playedBeatChanged` cursor.
- **Non-diatonic analysis markers** — secondary dominants (`V/ii`), borrowed/mixture (`iv`, `♭VI`), tritone subs — consumed from the [[harmonic-analysis]] thread once it lands.
- **Scale / mode + improv-target overlay** per section/chord (the lead-trainer north star) — own phase.
- **Voice-leading / guide-tone lines** between consecutive chords.
- **Advanced Layout-A engraving** — true repeats `𝄆:𝄇`, 1st/2nd endings, coda/segno, D.C., fermata — once the **Song model carries** that structure (it doesn't yet).

## Non-goals (v1)

- No faked harmonic analysis — v1 labels only what the kernel honestly knows (diatonic degree).
- No new Song-model repeat/ending/coda structure in v1 (renders plain barlines + `%`).
- No standalone export pipeline outside the app — export flows from the in-app rendered SVG.

## Validation / dogfood

- Render the **Jazz Blues** song and a pop song (à la the Elton John reference) in **both layouts**, **both notation modes**, with **both adornments**; export each to **PDF (light)** and confirm it matches the reference idiom.
- Toggle Nashville ⇄ song key ⇄ another key and confirm the realization is correct.
- Confirm light/dark parity on-screen and light-pinned PDF.

## Reference material

- `docs/internal/chord-sheets/` (gitignored): `layout-A.pdf`, `layout-A-nashville.pdf`, `layout-B1.png`, `layout-B2-sections.png`, `layout-B3-sections-notes.png`.
- Origin discussion: this thread's `chat-001`.
