---
type: idea
id: id_01KXQFGY5B575DAS8HPQNBX4T1
title: First-class minor keys (end-to-end)
status: done
created: 2026-07-17
version: 1
tags: []
parent_id: null
requires_load: [rf_01KTM41K36DYJ0CE44FE7TMCGH, rf_01KTSAQ6990GY3J4CZ7HPVPW6K]
---
# First-class minor keys (end-to-end)

## What

Make **minor keys a coherent, first-class citizen** across ChordFlow — DSL, realization, UI, and spelling — instead of the half-wired state they're in today. The tonic's *mode* (major/minor) becomes something you can author, realize, pick in the app, and see spelled correctly, everywhere a key is used (Score, Sheet, Now/Next).

## Why — minor is modeled but not coherently driven

The type model already speaks minor, but nothing exercises it end-to-end:

- **Modeled:** `Key(PitchClass Tonic, bool IsMinor)`, `Scale.NaturalMinor` + `Scale.ForKey(key)` (switches on `IsMinor`), `SongParser.ParseKey` accepts `Am`/`Cmin`, and `mod vi` flips the running mode to minor.
- **Not coherently driven:** `SongParser` carries the caveat *"Major by default (v1 renders major)"*, and there is a real frame snag underneath it — the Progression DSL degrees are authored in a **major frame** (chromatic notes written with explicit accidentals: `b27`, `#4dim7`), while `Transposer` realizes degrees through `Scale.ForKey`, which for a minor key uses **natural-minor** intervals. So feeding an existing major-frame progression a minor key would **double-shift** degrees 3/6/7 (a written `b7` lands a further semitone low). Minor keys aren't *wrong* in the types — they've just never been made coherent.

This blocks nothing that ships today (everything is authored in major), but it is a hard prerequisite for driving **minor-key harmonic analysis** through the app (see [[harmonic-analysis]] decision 4A) and for **minor-key chord sheets** ([[harmonic-overlay]]).

## The core design decision (settle in design)

**What frame does a Progression DSL degree use in a minor key?**

- **(A) Major-relative frame** — degrees stay exactly as written (`b3`, `b6`, `b7` are explicit); the key's *mode* is only a spelling/label concern, and `Transposer` keeps using the major-scale offsets regardless of `IsMinor`. A minor tune is authored in its relative-major degrees. Least churn; the DSL stays one frame.
- **(B) Natural-minor-relative frame** — in a minor key, degree 3 = ♭3 automatically (the natural-minor scale drives the offsets), so `1- 4- 5` in a minor key reads as the natural-minor i–iv–V. More intuitive to a minor-key author; but it makes the *same* degree token mean different pitches depending on mode, and every existing (major-frame) progression must be guarded against being realized minor.

This is the crux and it is *not* the analysis thread's to make — hence a dedicated thread.

## Scope (roughly in build order)

1. **Settle the frame** (decision above) and make `Transposer` realization coherent with it — no double-shift; every existing progression's byte-output unchanged in major.
2. **Spelling** — `NoteSpeller` / key-signature emission correct for minor keys (relative-major key signature; raised leading tones spelled where they occur).
3. **UI key-picker** offers minor keys (the Practice/HarmonyControlsR Key control), and the render path honors mode.
4. **Renderer** — `\ks` / chord spelling for a minor tonic.

## Non-goals

- Not harmonic modes beyond natural minor in v1 (harmonic/melodic minor, other modes are later scale sets).
- No harmonic-analysis logic here — that lives in [[harmonic-analysis]] and is pitch-based, so it needs no DSL-frame decision.

## Consumers

- [[harmonic-analysis]] — its engine already handles `Key.IsMinor`; this thread is what lets the **app** drive a minor-key analysis (its UI/realization surface).
- [[harmonic-overlay]] — minor-key chord sheets.
- Score / Now/Next — any surface that realizes a key.

## Validation / dogfood

- A minor tune authored end-to-end (pick a minor key in the app → correct realized chords + spelling on Score and Sheet).
- Golden tests: the chosen frame's realization for a known minor progression (e.g. a minor `iiø–V–i`, a natural-minor i–iv–v), and every existing major progression byte-identical.

## Related

- Prerequisite-sibling of [[harmonic-analysis]] (independent; implemented after it per the roadmap order 1→2→3).
- Builds on the `chromatic-degrees` / `progression` / `song` DSL + `Transposer` / `Scale` work.
