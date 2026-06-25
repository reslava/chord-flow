---
type: idea
id: id_01KVYQ3DY08RT6KGK50X0PPEGR
title: Derive shell voicings from CAGED chords
status: draft
created: 2026-06-25
updated: 2026-06-25
version: 2
tags: []
parent_id: null
requires_load: []
---
# Derive shell voicings from CAGED chords

## Goal

Derive **shell voicings** (root + 3rd + 7th; 5th dropped) **algorithmically from the engine-derived CAGED chord shapes** (`CagedDerivation.Derive` output, not the authored grips), for every **7th- or 6th-chord** quality the pack ships — replacing the single hardcoded 3-quality shell in `BeginnerShellStrategy`.

## Origin

Spun off from `guitar/voicing-difficulty-bands` (chat-001). That thread needs a shell for every Beginner chord, but the only shell source today is `BeginnerShellStrategy` — **one hardcoded movable shape** on the A/D/G strings covering `Dominant7`/`Minor7`/`Major7` only; it throws on everything else. Deriving shells from the already-derived CAGED chords is the general, single-source way to get a shell for any 7th/6th chord — and a prerequisite for difficulty-band selection.

## Why engine-CAGED-derived

The engine (`CagedDerivation`) derives full CAGED chords across all the jazz-blues qualities from the substrates, oracle-verified against the authored grips (the interval-derivation-engine vision). A shell is just that derived shape with the 5th (and doublings) removed, keeping the guide tones. So the shell is *derived* from the **engine output**, never separately hand-authored: one source of truth that automatically covers every quality and CAGED position the engine derives, and grows for free as the engine gains qualities.

## Shape (sketch — design firms this up)

- Input: an engine-derived CAGED `ChordShape` (full chord, C-anchored).
- Classify each fretted note by **interval-from-root** (root / 3 / 5 / 7 / 6…) using the existing speller.
- Drop the 5th and any doublings; keep root + 3rd + 7th (or 6th) — the guide tones.
- Keep the result contiguous/playable; carry diagram + muted-string metadata.
- Output: a derived shell `Voicing` per CAGED shape, slidable to any root (reuse `VoicingRealizer`).

## Scope

**In:** a CAGED→shell derivation over the engine's derived shapes; covers all derived **7th- and 6th-chord** qualities; produces playable shells with diagram metadata.
**Out:** **triads** (maj/min/aug — no 7th and no 6th) — shells apply only to chords with a 7th or a 6th; triads fall through to another source. Also out: difficulty-band *selection* wiring and the precedence rule (`voicing-difficulty-bands`); UI; broader drop-2 / guide-tone families beyond the shell.

## Dependencies

- **Depends on** `guitar/engine-derived-as-app-source` — shells derive from the engine output, which that thread makes the app's source (authored → oracle).
- **Depends on** `guitar/caged-sixth-voicings` — shells apply to 6th chords, so the engine must derive 6th grips first. (Rafa: implement 6th before this.)
- **Blocks** `guitar/voicing-difficulty-bands` (it consumes the derived shells). The dim7 shell additionally needs `domain/chromatic-degrees` (almost done) so a `dim7` chord can reach the voicer.

## Resolved design decisions

- **Triads have no shell** (Rafa, chat-001): a shell requires a 7th or a 6th. Maj/min/aug triads are out of scope and route to another voicing source.

## Open design questions (for the design phase)

1. When dropping the 5th leaves a **non-contiguous** shape, how is the playable subset (string set) chosen?
2. Does the derived shell **replace** `BeginnerShellStrategy`, or become a new derivation source the book consults?
3. **Where** does derivation run — at book-build time, or on demand in the strategy?

## Validation

- Every derived CAGED 7th/6th quality yields a playable derived shell (root+3+7 or root+3+6) with no throw.
- Derived dom7/m7/maj7 shells match the current hand-authored `BeginnerShellStrategy` output — a **regression oracle** (shells themselves are not added to the golden-oracle package; they inherit trust from the CAGED oracle).
- dogfood: render the derived shells on the fretboard UI page (guitar-weave dogfood rule).
