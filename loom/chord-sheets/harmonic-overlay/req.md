---
type: req
id: rq_01KXV0K5FC6EF8H1Z0Y2HB11JQ
title: Harmonic-analysis overlay on ChordSheetR — Requirements
status: locked
created: 2026-07-18
updated: 2026-07-18
version: 1
design_version: 1
tags: []
parent_id: de_01KXV0JDNCMH8MJV7Y10A7V8R1
requires_load: []
---
# Harmonic-analysis overlay on ChordSheetR — Requirements

### ✅ Included

- `IN1` **Consume `HarmonicAnalyzer` in `ChordSheetBuilder`** — analyze each `(realized chord, section.Key)` span and carry the result into the sheet model. This thread is the analyzer's first consumer.
- `IN2` **Re-source the honest Roman label** from the analyzer's `Function` and **retire `ChordSheetBuilder.RomanFunction`**, so the inline degree and the analyzer agree by construction (analysis-thread IN9). One function source.
- `IN3` A **Core Rendering formatter** (`Rendering/ChordSheets`) turning `ChordAnalysis` + `Key` into glyph strings; the analyzer stays glyph-free.
- `IN4` `ChordRef` carries pre-formatted `Roman` (honest degree) + `Analysis` (functional glyph) + `Category` (colour-key) **strings** — no raw `ChordAnalysis` struct on the DTO.
- `IN5` **Functional glyphs**: `V7/x` secondary dominant, `vii°/x` / `vii°7/x` secondary leading-tone (target numeral cased by its diatonic quality in the key); `Borrowed` / `TritoneSub` / `Chromatic` use the honest-degree glyph (`iv`, `♭II7`) with colour carrying the signal (so `Analysis == Roman` there).
- `IN6` A **3-state Roman display sub-mode** on the sheet strip: **Diatonic** (honest only, overlay off) / **Analysis** (functional, default) / **Both** (honest→function, paired only where the two differ).
- `IN7` **Category colour** on non-diatonic chords in the ChordSheetR drawer — theme-aware (a light and a dark variant), palette beside FretR's function palette; shown in Analysis + Both, off in Diatonic.
- `IN8` The overlay is a **pure JS re-draw** — the analysis fields ride the existing unified `loadScore` / `entityPreview` reply unconditionally; **no new bridge verb**, no round-trip on toggle.
- `IN9` Appears on **both** Practice and the Content preview via the shared `render-surface-component.js` → ChordSheetR (progression/song; rhythm stays score-only).
- `IN10` **Major and minor** tonic supported (analyzer is minor-symmetric; [[first-class-minor-keys]] is done).
- `IN11` **Builder tests** assert the new fields, including the dominant-blues must-not-over-label rows (`I7 IV7 V7` = blues idiom, not secondary dominants) plus a borrowed-iv and a secondary-dominant fixture.
- `IN12` **Ref-doc sync** in the same unit of work: `chordflow-domain-model-reference` (fields + formatter + builder consuming the analyzer, `RomanFunction` retired) and `chordflow-architecture-reference` (the overlay + 3-state selector, no new verb).

### ❌ Excluded

- `EX1` **No analysis logic** — categories / precedence / detection are the done `domain/harmonic-analysis` thread; this thread only consumes.
- `EX2` **No new music theory** — every label is a projection of the analysis pass.
- `EX3` **No sequence/resolution-aware labels** — the analyzer is context-free per-chord in v1; this thread renders exactly what it returns.
- `EX4` **No export/PDF change** — the overlay is on-screen; PDF pins light as today, the glyph just rides along.
- `EX5` **No score-view (alphaTex/tab) labels** — the overlay is the chord sheet only.
- `EX6` **No new bridge verb** — the fields ride the existing reply.

### ⛓ Constraints

- `C1` **ChordSheetR stays a dumb drawer** — all glyphs and colours resolved in Core; JS only chooses which carried string to paint (chord-sheets-maker C1).
- `C2` **The analyzer stays glyph-free** (its EX2/C3) — glyph formatting is a Rendering concern.
- `C3` **No round-trip on a display toggle** — the analysis fields are carried unconditionally, like the tone strip (chord-sheets-maker C3).
- `C4` **One function source** — the honest Roman and the analysis glyph both derive from the analyzer's output; `RomanFunction(span.Degree)` is retired (no parallel label logic).