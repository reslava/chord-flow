---
type: idea
id: id_01KXQNT33MS3EBFXYRY1J01188
title: Minor progressions set (default-pack content)
status: done
created: 2026-07-17
version: 1
tags: []
parent_id: null
requires_load: []
---
# Minor progressions set (default-pack content)

## What

The **minor-tonic twin** of the major-frame progression set already shipped to the default pack (see [[harmonic-analysis]] plan-002). A curated batch of **minor-key progressions** — authored in the Progression DSL, added to `Content/default-pack/progressions/`, each with a `description:` blurb and a harmonic-concept + difficulty **tag vocabulary** — that double as **real content** and as **minor-key golden dogfood** for the harmonic analyzer.

Same format and quality bar as the major set: `name:` / `genre:` / `description:` / `tags:` header + a one-line-per-bar Nashville body.

## Why deferred (the hard dependency)

These were **deliberately held back** when the major set landed: a minor progression's **DSL spelling depends on the degree-frame decision** that [[first-class-minor-keys]] must settle first — *major-relative* (write `1- b7 b6 5` with explicit accidentals) vs *natural-minor-relative* (write `1- 7- 6- 5`, where the mode drives the offsets). The same tune has two different DSL forms depending on that ruling, and realizing a minor-frame progression correctly (no double-shift on degrees 3/6/7) needs the coherent minor realization that thread delivers. So this thread **must land after** `domain/first-class-minor-keys`.

## Scope — candidate progressions (finalize DSL once the frame is set)

A representative minor set (the exact tokens are pinned to the chosen frame):

- **Minor `iiø–V–i`** — the minor jazz cadence (half-diminished ii, harmonic-minor V).
- **Andalusian cadence** — `i–♭VII–♭VI–V` (the descending minor tetrachord; flamenco/rock).
- **Natural-minor `i–iv–v`** — the plain modal minor.
- **Harmonic-minor `i–iv–V`** — the raised-leading-tone dominant.
- **Minor turnaround** — `i–♭VI–iiø–V`.
- **Aeolian `i–♭VI–♭VII–i`** — the minor "epic" loop (the minor-key peer of the major-set `aeolian_cadence`).
- **Picardy cadence** — a minor progression resolving to a **major I** (the borrowed-major tonic).
- **Minor 12-bar blues** — `i7 … iv7 … V7 …` in a minor key.
- *(optional)* line-cliché / `i–i(maj7)–i7–i6` if the needed qualities exist.

## Non-goals

- No engine or DSL changes — pure content authored against the (by-then) working minor-key DSL.
- Not the analyzer's minor *logic* — that already ships in [[harmonic-analysis]] (its minor golden tests use hand-built fixtures); this thread adds the **authored, realizable** minor content the app can actually select and play.

## Validation / dogfood

- Every progression parses → realizes (in a **minor** key) → renders, the peer of the major set's `ProgressionSeedTests` coverage.
- Each realizes to the intended sounding chords under the chosen frame (no double-shift).
- Analyzed by the harmonic analyzer, the minor set produces the expected functional labels (minor `iiø–V–i`, borrowed, Picardy) — visible once [[harmonic-overlay]] lands.

## Related

- Twin of the major-frame set in [[harmonic-analysis]] (plan-002).
- **Hard dependency:** [[first-class-minor-keys]] (frame + realization).
- Feeds the minor-key dogfood of [[harmonic-overlay]].
