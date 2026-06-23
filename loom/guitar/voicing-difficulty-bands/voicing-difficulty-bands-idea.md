---
type: idea
id: id_01KVVCJNSD76XQXJV5MNJK5YYS
title: Voicing selection by difficulty (Beginner = shells) + dim7
status: draft
created: 2026-06-23
version: 1
tags: []
parent_id: null
requires_load: []
---
# Voicing selection by difficulty (Beginner = shells) + dim7

## Goal

Make the **Difficulty control actually shape voicing selection** — so **Beginner plays shell voicings** (root+3+7), not full chords — and give **dim7** a playable shape.

## Origin

`songbook/jazz-blues` dogfood — **Findings 5, 6** (difficulty no-op; full chords instead of shells) and **Finding 2** (dim7 has no shell voicing). Priority 4 of the four follow-ons.

## Root cause

`VoicingBook.Candidates` **ignores `difficulty`** — its own comment: *"does not filter in slice 1"* (req EX6 deferred). And the default pack's authored **full-chord** voicings *shadow* `BeginnerShellStrategy`, so for any chord with an authored voicing the shell never wins and the Difficulty control changes nothing. Separately, `BeginnerShellStrategy` throws on dim7 (the CAGED engine *can* derive dim7 — the "behind-1 reach" case — so the shape exists, it's a wiring question).

## Shape

- Add **difficulty-band narrowing** to `VoicingBook` (EX6): at Beginner, prefer shells / simpler grips over full authored CAGED chords; richer bands unlock fuller voicings.
- Decide the precedence rule: difficulty band vs the authored-shadows-generated rule (the crux — authored voicings currently always win).
- **dim7 voicing:** wire the CAGED-derived dim7 grip (or a shell) into selection so dim7 chords don't throw.

## Scope

**In:** difficulty-aware voicing selection; Beginner → shells; a dim7 voicing.
**Out:** a full CAGED-shape preference UI (separate); the `RenderOptions.VoicingStrategy` enum expansion beyond what this needs.

## Dependency note

The **dim7** part (Finding 2) is gated behind `domain/chromatic-degrees` — a dim7 chord only reaches the voicer once `#IVdim7` parses. Findings 5 & 6 (difficulty → shells) are independent and can land first.

## Validation

- Changing Difficulty visibly changes the voicings; **Beginner shows shell voicings** for the jazz-blues chords. 
- A dim7 chord renders a playable shape (no throw). 
- Dogfood on the fretboard / Practice surface (guitar-weave dogfood rule).