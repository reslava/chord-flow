---
type: idea
id: id_01KTXEQADETHX82GXAV8T5A5GT
title: Intervals — the theory substrate (deferred, captured)
status: draft
created: 2026-06-12
version: 1
tags: []
parent_id: null
requires_load: []
---
# Intervals — the theory substrate (deferred, captured)

## The idea (captured, not yet scheduled)

A first-class **`Interval`** type (quality + number, spelling-aware — `P5`, `M3`,
`m7`, `A4`…) is the substrate under most music theory: scales, triads, arpeggios,
chord construction, and **interval mapping on the fretboard** (the most useful
practice lens of all).

> **Why this is a stub, not a thread we build now:** nothing in the current
> slices needs it. Movable voicings + the exercise pipeline need only
> **`PitchClass` mod-12 arithmetic + the `Fretboard`** (both present), and
> `Quality` is already *interval-backed* internally. Keeping minimal, the build
> is **postponed**.

## What will need it (the dependency this stub records)

- **Pitched target notes** — scale / chord-tones / guide-tones / arpeggios via
  the existing `LeadTargets` seam (deferred in the `exercises` thread).
- **Fretboard interval-overlay** — showing intervals (not just notes) under a
  key/chord.
- A **refactor** of `Scale` / triads / arpeggios to *derive from* a shared
  `Interval` formula rather than each computing semitones ad hoc.

## When we build it

When the deferred pitched-target-notes work is scheduled, design
`domain/intervals` first as its substrate, then refactor scales/triads/arpeggios
onto it. Until then this idea exists so the dependency is explicit and not lost.

Related: [[chordflow-domain-model-reference]], the `voicings` & `exercises-definition-ui` threads, [[design-philosophy-durable-over-minimal]].