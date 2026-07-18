---
type: design
id: de_01KXV0JDNCMH8MJV7Y10A7V8R1
title: Harmonic-analysis overlay on ChordSheetR
status: done
created: 2026-07-18
version: 1
idea_version: 1
tags: []
parent_id: id_01KXQFHDBQC1NNZMC15CNK8TPW
requires_load: []
---
# Harmonic-analysis overlay on ChordSheetR

## 1. One sentence

The chord sheet's inline Roman label is re-sourced from the already-shipped pure `HarmonicAnalyzer` pass and upgraded into a **functional** label — `VI7 → V7/ii`, `♭II7` tritone sub, borrowed `iv` — formatted **in Core** (Rendering) and carried as pre-formatted strings on `ChordRef`, so ChordSheetR stays a dumb drawer and the overlay is a pure re-draw on **both** the Practice and Content surfaces.

## 2. What already exists (the seam is clean)

- `HarmonicAnalyzer.Analyze(chord, key) → ChordAnalysis { Category, Function, Target?, SourceMode? }` — a pure Harmony sink, glyph-free, pitch-based, minor-symmetric. Not yet consumed anywhere; this thread is its first consumer.
- `ChordSheetBuilder.ToChordRef(span, section.Key, comping)` (`Features/ChordSheets/ChordSheetBuilder.cs:193`) already walks every `RealizedSpan` with the section's `Key` in scope — the exact `(chord, key)` the analyzer needs — and today sets `Roman: RomanFunction(span.Degree)`, whose own comment defers the real labels "to the harmonic-analysis pass."
- `ChordRef` carries every notation as a pre-formatted string plus a colour-key on tones (`Function`), and rides the unified `loadScore` / `entityPreview` reply (`ExerciseRendering.RenderWithSheet` → `ChordSheetBuilder`) to **both** Practice and the Content preview. So the overlay is a pure JS re-draw with no new plumbing.
- Soft dependency [[first-class-minor-keys]] is **done** and the analyzer is minor-symmetric (its D4) — the major **and** minor overlay ship together in this one thread.

## 3. Decisions (settled in chat-001)

**D1 — Glyph formatting lives in Core.** A new `Rendering/ChordSheets/` formatter turns a `ChordAnalysis` (+ the key) into the glyph string(s). The analyzer stays glyph-free (its EX2/C3); ChordSheetR stays a dumb drawer (chord-sheets-maker C1). Formatting in JS is explicitly rejected — it would be the first music-theory-in-JS crack, and the formatted label is reusable by any future renderer / exporter / tool.

**D2 — The Roman label becomes a 3-state display sub-mode, not a binary.**
- **Diatonic** — the honest key-relative degree only (`VI7`, `iv`, `♭II7`). This is the overlay-**off** state (no analysis colour).
- **Analysis** *(default)* — the functional glyph (`V7/ii`, `vii°/V`, …); colour on.
- **Both** — the teaching view: honest degree bridging to the function, shown **only where the two differ** (i.e. the secondary functions), so diatonic bars stay clean and the interesting chords are spotlighted as `position → function`.

Because `ChordRef` carries both the honest string and the analysis string, this is a pure JS draw choice — zero extra Core work, zero round-trip.

**D3 — Category colour: a small colour-key carried to JS, theme-aware pastels in the drawer.** `ChordRef` carries a `Category` key string; the palette (one pastel hue per non-diatonic category, diatonic neutral) lives in ChordSheetR next to FretR's function palette, with a **light and a dark variant**. Colour shows in Analysis + Both, off in Diatonic.

## 4. What `ChordRef` carries (the model change)

Add two pre-formatted, glyph-level fields; **re-source** one existing field; carry **no** raw `ChordAnalysis` on the DTO (reversing the old chord-sheets-maker §3 sketch of a `ChordAnalysis? Analysis` field — that was never shipped, and it would push the struct onto the wire and tempt JS formatting):

```
ChordRef(
  string Concrete,
  string Degree,
  string Roman,        // RE-SOURCED: the analyzer's honest Function, formatted (was RomanFunction(span.Degree))
  string Analysis,     // NEW: the functional glyph — "V7/ii", "iv", "♭II7"; == Roman for diatonic chords
  string Category,     // NEW: colour-key — "diatonic"/"secondaryDominant"/…/"chromatic"
  int DurationTicks,
  IReadOnlyList<ChordSheetTone> Tones,
  FretboardDiagram? Diagram )
```

- `Roman` and `Analysis` differ **only** for `SecondaryDominant` / `SecondaryLeadingTone` (the `/target` suffix). For `Borrowed` / `TritoneSub` / `Chromatic` the honest degree *is* the conventional glyph (`iv`, `♭II7`) and the **colour** carries the "special" signal — so `Analysis == Roman` there and Both-mode correctly shows a single (unpaired) label.
- The honest-Roman formatter is today's `RomanFunction` body, moved into the Rendering formatter and fed the analyzer's `Function` instead of `span.Degree` — same output shape (analysis-thread IN9's "agree by construction"), now pitch-authoritative.

## 5. The formatter (Rendering/ChordSheets)

`HarmonicAnalysisFormatter` (name TBD) — pure, glyph-only, may reference `Music.Harmony`:
- `HonestDegree(RomanDegree) → string` — the moved `RomanFunction` logic (numeral + case by quality + quality suffix + accidental prefix).
- `Glyph(ChordAnalysis, Key) → string` — Diatonic / Borrowed / TritoneSub / Chromatic ⇒ `HonestDegree(Function)`; SecondaryDominant ⇒ `"V7/" + targetRoman`; SecondaryLeadingTone ⇒ `("vii°" | "vii°7") + "/" + targetRoman` (° vs °7 read off `Function.Quality`). `targetRoman` is the target degree's numeral **cased by its own diatonic quality in the key** (ii lowercase, V uppercase) — resolved via `DiatonicChord.Build(Scale.ForKey(key), target)`. Formatting only, no new theory.
- `CategoryKey(HarmonicCategory) → string` — the colour-key vocabulary.

## 6. Builder change

`ChordSheetBuilder.ToChordRef`: call `HarmonicAnalyzer.Analyze(chord, key)` once per span, then fill `Roman` / `Analysis` / `Category` from the formatter. Delete the private `RomanFunction` (its `Numerals` / suffix / accidental helpers move into the formatter). Everything else in the builder is unchanged. `ChordSheetBuilderTests` gains assertions for the new fields — including the dominant-blues must-not-over-label rows (`I7 IV7 V7` all read as blues, not secondary dominants) plus a borrowed-iv and a secondary-dominant fixture.

## 7. JS drawer

- `chord-sheets.js` (ChordFlowSheetView) display strip: the existing Roman/notation control gains the **3-state analysis selector** (Diatonic / Analysis / Both). A pure re-render; no round-trip.
- `chord-sheet-render-component.js` (ChordSheetR): when drawing the Roman line, choose `Roman` vs `Analysis` vs the paired form per the state; tint the chord token (or a small marker) by `Category` via the theme-aware palette, with a light and a dark variant.
- Appears on **both** Practice and the Content preview automatically (shared `render-surface-component.js` → ChordSheetR); Content shows it for progression/song (rhythm is score-only, as today).

## 8. Scope & phasing

- **v1 (this thread):** the model fields + Core formatter + builder re-source + `RomanFunction` retirement + the 3-state selector + category colour. Major and minor. Overlay is a pure re-draw.
- **Non-goals:** no analysis *logic* (the done `harmonic-analysis` thread owns categories/precedence/detection); no new music theory; no sequence/resolution-aware labels (analyzer is context-free per-chord in v1); no export/PDF change (the overlay is on-screen; the glyph just rides along, PDF pins light as today); no score-view (tab/alphaTex) labels — the overlay is the chord sheet only.

## 9. Validation / dogfood

- Jazz Blues (with the Herb Ellis substitutions) + a borrowed-iv ballad render correct functional labels; the dominant-blues `I7 IV7 V7` reads as the blues idiom (not secondary dominants) — the analyzer's must-not-over-label case, now visible on the sheet.
- Toggle Diatonic ⇄ Analysis ⇄ Both: pure re-draw, no C# round-trip (parity with the sheet's other display toggles).
- Both light and dark sheet themes legible.
- Appears identically on Practice and the Content preview.

## 10. Reference-doc impact (per the ref-sync rule)

Landing this updates **`chordflow-domain-model-reference`** (the new `ChordRef` fields + the Rendering analysis formatter + `ChordSheetBuilder` now consuming `HarmonicAnalyzer`; `RomanFunction` retired) and **`chordflow-architecture-reference`** (ChordSheetR's analysis overlay + the 3-state selector; **no new bridge verb** — the fields ride the existing `loadScore` / `entityPreview` reply), in the same unit of work as the code.