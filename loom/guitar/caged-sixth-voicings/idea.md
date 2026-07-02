---
type: idea
id: id_01KVYRNSY7JPFKF3TWYKJYWH6V
title: Derive CAGED 6th voicings
status: done
created: 2026-06-25
updated: 2026-06-26
version: 2
tags: []
parent_id: null
requires_load: []
---
# Derive CAGED 6th voicings

## Goal

Extend the CAGED **derivation engine** (`CagedDerivation.Derive`) to cover **6th chords** (major 6 and minor 6) — `1 3 5 6` / `1 b3 5 6` — so the engine can spell a full CAGED grip for a 6th chord in any shape/root, with no authored fret tables.

## Origin

Spun off from `guitar/shell-voicing-derivation` (chat-001). Shell voicings will apply to chords with a **7th or a 6th**; the 6th case has no derivation today. Rafa: implement this **before** shell-voicing-derivation.

## Why

The interval-derivation-engine vision: grips are derived from substrates (octave shapes × interval lattice × hand reach × candidate selector), with the authored voicings as the golden oracle. 6th chords are a new quality the engine must spell — the major/minor 6 add a `6` (9 semitones... maj6 = 9) as a chord tone the candidate selector must place. Adding it here keeps 6ths a first-class derived quality, feeding both full-chord rendering and (later) 6th-chord shells.

## Scope

**In:** add Major6 / Minor6 to the quality formulas the engine derives; ensure `Derive` spells valid CAGED grips for them across shapes; oracle-anchor with authored 6th grips (golden oracle) where they exist.
**Out:** shell derivation (that's `shell-voicing-derivation`); the app-source flip (that's `engine-derived-as-app-source`).

## Open design questions (for design)

1. Does the domain already have `Quality.Major6` / `Quality.Minor6`, or is that a prerequisite (cf. the `dim7`/chromatic-degrees pattern)?
2. Which CAGED shapes get authored 6th grips as oracle anchors (vs. derived-only, verified structurally)?
3. The 6th competes with the 7th and 5th for limited strings — how does the candidate selector prioritize the 6 vs. the 5 in a 4-finger box?

## Validation

- Every CAGED shape yields a playable, fully-spelled derived 6th grip with no throw.
- Derived 6th grips match any authored 6th oracle grips.
- dogfood: render the derived 6th chords on the fretboard UI page.
