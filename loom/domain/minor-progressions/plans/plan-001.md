---
type: plan
id: pl_01KXRNQA2Y1JSP109RMMB2BYEE
title: Minor progressions set — 8 default-pack progressions (C frame)
status: done
created: 2026-07-17
updated: 2026-07-17
version: 1
design_version: 1
tags: []
parent_id: null
requires_load: []
target_version: 0.1.0
actual_release: 0.15.0
steps:
  - id: authored-8-minor-home-progression-dsl
    order: 1
    status: done
    description: "Authored 8 minor-home progression .dsl files into Content/default-pack/progressions/ (minor ii-V-i, Andalusian cadence, natural-minor i-iv-v, harmonic-minor i-iv-V, minor turnaround, Aeolian loop, Picardy cadence, minor 12-bar blues) — each with a name/genre/description/tags header + tonality: minor, authored tonic-relative in the first-class-minor-keys C frame."
    files_touched: []
    blocked_by: []
    satisfies: []
  - id: auto-registered-packreader-globs-dsl-and
    order: 2
    status: done
    description: "Auto-registered: PackReader globs *.dsl and the .csproj copies Content/** by wildcard, so the files ship with no manifest or SeedData change."
    files_touched: []
    blocked_by: []
    satisfies: []
  - id: synced-packdefinitionfile-headerkeys-with-catalogheader-s
    order: 3
    status: done
    description: "Synced PackDefinitionFile.HeaderKeys with CatalogHeader's recognized set (added description + tonality) so a name: line is still found when placed after those header lines."
    files_touched: []
    blocked_by: []
    satisfies: []
  - id: added-the-minorprogression-realizestoexpectedchordsinaminor-dogfood-theory
    order: 4
    status: done
    description: "Added the MinorProgression_RealizesToExpectedChordsInAMinor dogfood theory to ProgressionSeedTests — loads each pack file, confirms tonality: minor, realizes in A minor, and asserts the exact sounding chords (incl. letter-pure raised roots). Full Core suite 1005/1005 green."
    files_touched: []
    blocked_by: []
    satisfies: []
---
# Minor progressions set — 8 default-pack progressions (C frame)

## Goal

Quick-ship record of 4 completed changes.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Authored 8 minor-home progression .dsl files into Content/default-pack/progressions/ (minor ii-V-i, Andalusian cadence, natural-minor i-iv-v, harmonic-minor i-iv-V, minor turnaround, Aeolian loop, Picardy cadence, minor 12-bar blues) — each with a name/genre/description/tags header + tonality: minor, authored tonic-relative in the first-class-minor-keys C frame. | — | — | — |
| ✅ | 2 | Auto-registered: PackReader globs *.dsl and the .csproj copies Content/** by wildcard, so the files ship with no manifest or SeedData change. | — | — | — |
| ✅ | 3 | Synced PackDefinitionFile.HeaderKeys with CatalogHeader's recognized set (added description + tonality) so a name: line is still found when placed after those header lines. | — | — | — |
| ✅ | 4 | Added the MinorProgression_RealizesToExpectedChordsInAMinor dogfood theory to ProgressionSeedTests — loads each pack file, confirms tonality: minor, realizes in A minor, and asserts the exact sounding chords (incl. letter-pure raised roots). Full Core suite 1005/1005 green. | — | — | — |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
