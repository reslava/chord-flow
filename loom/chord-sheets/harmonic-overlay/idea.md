---
type: idea
id: id_01KXQFHDBQC1NNZMC15CNK8TPW
title: Harmonic-analysis overlay on ChordSheetR
status: draft
created: 2026-07-17
version: 1
tags: []
parent_id: null
requires_load: []
---
# Harmonic-analysis overlay on ChordSheetR

## What

The **chord-sheet v2 analysis overlay**: render the [[harmonic-analysis]] pass's functional labels and non-diatonic markers on **ChordSheetR**, so the sheet *explains* the harmony instead of only showing the honest diatonic degree. That `Fm` in a C-major tune shows as a **borrowed iv**; the `A7` in a C blues shows as a **secondary dominant V/ii**; a `Db7` shows as a **♭II7 tritone sub**.

## Why

- ChordSheetR v1 already carries an honest-diatonic `ChordRef.Roman` (from `ChordSheetBuilder.RomanFunction`) — deliberately *no* secondary-dominant / borrowing inference. This thread is the promised consumer that upgrades that field with real analysis.
- It's the visible dogfood of the reasoner north star applied to *function*: the same sheet that draws chords now names their role.

## Scope

1. **Consume the analysis pass** — the Features `ChordSheetBuilder` calls [[harmonic-analysis]] over the realized `(chord, key)` sequence and carries the structured `ChordAnalysis` into the `ChordSheet` model (per chord: category + honest function + optional target + source mode).
2. **Subsume the inline diatonic label** — `ChordSheetBuilder.RomanFunction` is retired in favour of the analyzer's `Diatonic`-category output (one function source), so the sheet's Roman field and the engine agree by construction.
3. **Present the label + non-diatonic colour** — ChordSheetR draws the formatted glyph (`V7/ii`, `♭II7`, `iv`) and a colour/marker for non-diatonic chords. Presentation (glyph formatting, colour) lives in the JS drawer / a Rendering formatter — the analyzer stays glyph-free.
4. A display toggle for the analysis overlay (on/off), like the other sheet adornments.

## Dependencies

- **Hard:** [[harmonic-analysis]] (thread 1) — there is no analysis to render without it.
- **Soft:** [[first-class-minor-keys]] (thread 2) — the **major-key** overlay ships on thread 1 alone; only the **minor-key display** case needs first-class minor keys (a minor tonic driven through the app).

## Non-goals

- No new music theory — every label is a projection of the analysis pass (like the rest of `ChordSheetBuilder`).
- No analysis *logic* here — that's thread 1; this thread is purely the consumer/presentation.

## Validation / dogfood

- The reference songs (jazz blues with the Herb Ellis substitutions, a borrowed-iv ballad) render with correct functional labels on the sheet.
- Toggling the overlay is a pure re-draw (no C# round-trip), consistent with the sheet's other display toggles.

## Related

- Consumes [[harmonic-analysis]]; minor display gated on [[first-class-minor-keys]].
- Extends the `chord-sheets-maker` sheet model + ChordSheetR.
