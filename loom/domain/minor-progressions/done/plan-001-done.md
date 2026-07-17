---
type: done
id: pl_01KXRNQA2Y1JSP109RMMB2BYEE-done
title: Done — Minor progressions set — 8 default-pack progressions (C frame)
status: done
created: 2026-07-17
version: 1
tags: []
parent_id: pl_01KXRNQA2Y1JSP109RMMB2BYEE
requires_load: []
---
# Done — Minor progressions set — 8 default-pack progressions (C frame)

Quick-shipped — recorded already-completed work:

1. Authored 8 minor-home progression .dsl files into Content/default-pack/progressions/ (minor ii-V-i, Andalusian cadence, natural-minor i-iv-v, harmonic-minor i-iv-V, minor turnaround, Aeolian loop, Picardy cadence, minor 12-bar blues) — each with a name/genre/description/tags header + tonality: minor, authored tonic-relative in the first-class-minor-keys C frame.
2. Auto-registered: PackReader globs *.dsl and the .csproj copies Content/** by wildcard, so the files ship with no manifest or SeedData change.
3. Synced PackDefinitionFile.HeaderKeys with CatalogHeader's recognized set (added description + tonality) so a name: line is still found when placed after those header lines.
4. Added the MinorProgression_RealizesToExpectedChordsInAMinor dogfood theory to ProgressionSeedTests — loads each pack file, confirms tonality: minor, realizes in A minor, and asserts the exact sounding chords (incl. letter-pure raised roots). Full Core suite 1005/1005 green.
