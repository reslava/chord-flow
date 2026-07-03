---
type: reference
id: rf_01KWAA8N1THKHZ7K9DB13VR17C
title: Voicings Engine Rules
status: active
created: 2026-06-29
updated: 2026-07-03
version: 4
tags: []
parent_id: null
requires_load: []
slug: voicings-engine-rules
description: "The derivation rules of the guitar Voicings Engine: the operator-library model, the tone-selection (Music) vs realization (Guitar) split, the shared substrate, and the exact atomic rules + order for the CAGED, doubled-shell, and shell families — with their golden oracles and catalog coverage. The recipe for adding a new family."
---
# Voicings Engine Rules

The derivation rules of the **guitar Voicings Engine** (`GuitarVoicingsEngine`) — the live `automatic` voicing source. This is the *rules* map (the operators and the order they run); the *architecture* map (engine structure, how it plugs into the app) will be `voicings-engine-reference.md`. Load this before reasoning about or extending voicing derivation; update it in the same change whenever an operator, its order, or the catalog coverage changes.

> Companion refs: `chordflow-architecture-reference.md` (where this sits in the system) and `chordflow-domain-model-reference.md` (the harmony substrate it consumes).

---

## 1. Mental model — a library of derivation *operators*, not one filter

A voicing **family** is produced by an **operator**: a named, parameterised transform that turns a `(quality, root, neck region)` request into a playable guitar grip (`ChordShape`). The engine is the **library of these operators** plus the shared substrate they stand on. It is *not* a single `CAGED → filter → family` pipe — that flattens three genuinely different operator **kinds**:

| Kind | What it does | Current family | Future examples |
|------|--------------|----------------|-----------------|
| **Derive-from-formula** | Build a grip directly from the quality formula + fretboard geometry | **CAGED**, **Shell** | — |
| **Reduce / filter** | Take an existing grip and mute notes by chord-tone *function* | **Doubled-shell** (mute the 5th) | "no-root", drop-the-3rd |
| **Re-voice** | Rearrange the voices of a grip by octave displacement | — | **Drop2**, **Drop3** |
| **Augment** | Change the chord-tone *set* before voicing it | — | **6/9**, **add9**, **sus** |

"Filter/reduce" is one kind among several. The reference must keep them distinct, or new families get forced into a metaphor that does not fit them.

---

## 2. The two-axis split — tone-selection vs realization

Every operator decomposes into two steps along an axis that maps onto the project's **theory ↔ instrument boundary** (`chordflow-architecture-reference.md` §3, §7):

- **Tone-selection / arrangement — instrument-agnostic (belongs in `ChordFlow.Music`).** *Which* chord tones the family includes and how they are arranged by octave/voice. This is pure harmony: it reads the quality formula via `QualityIntervals` / `ChordTones` / `ChordToneFunction` (all in `Music.Harmony`). Examples: "the full chord", "the full chord minus the 5th", "root + 3rd + (7th|6th)", "drop the 2nd-from-top voice an octave". Piano and flute would share this layer.
- **Realization — guitar geometry (belongs in `ChordFlow.Instruments.Guitar`).** Map the selected tones onto a *playable grip*: octave-correct fret choices, string muting, hand-span and reach limits, anchor finger. This is irreducibly instrument-specific.

**Namespace placement (confirmed against the architecture ref — signed off):**
- New **abstract operators** (tone-selection / arrangement that produces ordered `Pitch`es or intervals) are authored in **`ChordFlow.Music.Harmony`** — `Music/` is provably instrument-agnostic, and ordered `Pitch`es are already inside its output vocabulary.
- **Realization** stays in **`ChordFlow.Instruments.Guitar`** (`VoicingRealizer`, `OctaveShape`, `IntervalLattice`, `HandReach`, `CandidateSelector`, `AnchorFinger`).
- The **three current families are now first-class `IVoicingOperator`s** (`CagedOperator`, `ShellOperator`, `DoubledShellOperator`) behind the `FamilyVoicing` grip shim + the `VoicingOperators` registry. Each declares a typed `ParameterSchema` and emits a `VoicingDerivation` whose **`ToneSelection` carries the tone-selection axis as explicit data** (which chord tones, by function) alongside the ordered `RealizationStep`s and the grip. But this is *introspection in place*: the tone-selection representation still lives in `Instruments/Guitar` — the **physical** namespace move (tone-selection → `Music.Harmony`) becomes real only when the first **re-voice** operator (Drop2) or a **second instrument** forces it, extracted from ≥2 real cases, never guessed from one. A cross-instrument engine interface (`IVoicingsE`) is deferred for the same reason.

---

## 3. The shared substrate

Operators do not author fret tables — they stand on these primitives:

**Theory (Music.Harmony):**
- `QualityIntervals.Intervals(quality)` — the semitone formula of a quality.
- `ChordTones.Of(chord)` → `(Interval, Function)` per tone; `ChordToneFunction` ∈ {Root, Third, Fifth, Seventh, Sixth, …}. Functions are read by **spelling**, never hard-coded semitones — so the ♭5 of m7♭5/dim7 and the ♯5 of augmented are handled correctly.

**Guitar geometry (Instruments/Guitar):**
- `Fretboard` — tuning + `PositionsFor(pitchClass, maxFret)`.
- `OctaveShape` — for a CAGED shape: `AnchorsFor(root, shape, region)`, `Zone(...)`, `RootStrings(shape)` (`.Max()` = the bass/lowest-pitch root string).
- `IntervalLattice.PositionsOfInterval(rootOrigin, semitones, window)` — where a chord tone falls on the neck relative to a root.
- `HandReach` — per-finger ahead/behind reach; `CandidateSelector` — picks one tone per string for a whole-box grip; `AnchorFinger` — the finger that anchors the box.

---

## 4. The current families (exact rules + order)

Each family is a registered `IVoicingOperator` (`VoicingOperators.All`), dispatched behind `FamilyVoicing`: `FamilyVoicing.Derive(...)` returns `operator.Derive(request).Grip` (the byte-identical grip the comping resolver + grid consume), while `FamilyVoicing.Voicing(...)` / `operator.Derive(...)` return the full `VoicingDerivation` (tone selection + realization steps + grip). The derivers below (`CagedDerivation`/`ShellDerivation`/`ShellReduction`) are unchanged in their grip logic — they now additionally *emit* the trace (`DeriveVoicing`), and the `*Operator` types wrap them with the declared `ParameterSchema`.

### 4.1 CAGED — `CagedDerivation.Derive(quality, shape, root, minFret, maxFret)`
**Kind:** derive-from-formula. **Output:** the full chord in a CAGED shape.

Pipeline (the authored order):
1. **Anchor the shape** — `OctaveShape.AnchorsFor` gives the root occurrences + `Zone` for `shape` in the region; throw if none.
2. **Find the bass root** — `RootStrings(shape).Max()` (lowest-pitch root string) and its fret.
3. **Anchor direction** — `stacksUp` = the bass root is the *lowest* anchor (index-anchored, box stacks up) vs the highest (pinky-anchored, box stacks down). Derived from anchors, not authored.
4. **Reach window from the bass root** — extends only in the anchor finger's direction: index reaches *ahead*, pinky reaches *behind*, each capped so the grip spans at most **`MaxChordWidth` = 4 frets** (the 4-finger hand). The width cap is enforced on the *realized* grip in `CandidateSelector`; the window only bounds enumeration.
   - **Stretch-back (behind-1):** when `stacksUp`, the fully-symmetric **dim7** (its nearest 7th sits one fret *below* the bass root) gets the index's one-fret behind reach, which may voice only an *uncovered* tone, never a doubling.
   - **E-shape exception** (`shape == E` and quality ∈ {m7♭5, dim7, maj6, min6} — the "string-5-awkward" qualities): **mute string 5** (it would only re-double the 5th or block the colour tone relocated below the root) and **grant the index stretch-back**. Gated on the E shape so every C/A/G/D derivation stays byte-identical.
5. **Mute below the bass root**; enumerate per played string the chord-tone candidates that land in the window (`IntervalLattice` × distinct tones). The bass-most string is pinned to the **root at its anchor** (every authored grip is root-position).
6. **Select one tone per string** — `CandidateSelector` (B-string tax, octave-zone containment, full chord spelling, tightest grip, width cap).
7. **Anchor finger** — `AnchorFinger.Derive` from the root's rank in the realized box (with the E-shape behind-1 case shifting the anchor up a finger when the index is spent on the stretch-back fret).
8. **Assemble** strings low-E→high-E: muted below the bass root, the chosen tone at/above it.

**Golden oracle:** the **36 authored grips** (test-only fixture). The engine carries no authored fret tables; the oracle proves `Derive`.

### 4.2 Doubled-shell — `ShellReduction.MuteFifth(chordShape)`
**Kind:** reduce / filter. **Output:** a CAGED grip with the **5th muted**, every other sounded string kept (root, 3rd, 7th/6th + doublings) — "a chord minus the 5th".

Rule: mute each sounded string whose chord-tone **function is the Fifth**, read from the quality's formula via `ChordTones` (handles ♭5/♯5 by spelling, not a hard-coded 7). Nothing is re-packed; muted strings stay muted.

**Golden oracle:** none of its own — the surviving notes are already CAGED-oracle-verified, so it **inherits CAGED's trust**.

### 4.3 Shell — `ShellDerivation.Derive(quality, form, root, minFret, maxFret)`
**Kind:** derive-from-formula (a *distinct* 2-form derivation — **not** a reduction of CAGED). **Output:** the compact guide-tone shell, **root + 3rd + (7th|6th), 5th omitted**.

- **Forms** (reusing `CagedShape` as the form label): **C** = 5th-string root (root s5, 3rd s4, guide s3); **E** = 6th-string root (root s6, guide s4, 3rd s3, **s5 skipped**). Throws for any other form.
- **Guide tones** sit on s4 (lower) and s3 (higher); the C form stacks 3rd-then-guide, the E form (root a string lower) stacks guide-then-3rd. Each guide tone takes the occurrence on its string **nearest the root fret** (octave-correct).
- **Anchor** the root at the **lowest *compact* placement** in the region — span ≤ `MaxShellSpan` (5); fall back to the lowest placement if none is compact. (This is why an open-string root whose guide tones would jump ~12 frets away is pushed up an octave — e.g. A maj7 → `x 12 11 13 x x`, not `x 0 11 1 x x` — as a consequence, not a special case.)

**Golden oracle:** the **12 authored shell grips**.

### 4.4 Catalog coverage — `CagedVoicingCatalog.Combos`
The single source of truth for *which* `(family, quality, shape)` combos the engine offers (shared by the listing source, comping resolver, and coverage test so they can't drift). **64 combos:**
- **caged** (full chord): 8 five-shape qualities × 5 shapes (40) + 2 diminished-family qualities (m7♭5, dim7) × {A,E,D} (6) = **46**.
- **doubled-shell**: **C form only**, for dom7 / dim7 / 6 / m6 = **4** (the commonly-played doubled-root voicings).
- **shell**: the 7 shell-eligible qualities (those with a 7th or 6th) × {C, E} = **14**.

Triads carry only the **caged** family (shells need a 7th or 6th).

---

## 5. Golden-oracle principle

Authored grips are the **oracle, not the source**: the engine derives every grip from theory + geometry with no authored fret tables, and a test fixture of hand-authored "correct" grips proves the derivation. A reduce/filter operator may **inherit** an upstream oracle (doubled-shell does); a genuinely new derivation (shell) needs its own authored grips. New families are added test-first against new authored grips.

---

## 6. Recipe — adding a new family

1. **Pick the operator kind** (§1) — derive-from-formula / reduce / re-voice / augment.
2. **Define tone-selection** (instrument-agnostic) in terms of `ChordTones` / `ChordToneFunction` — author it in `Music.Harmony` for any new abstract operator.
3. **Define / reuse realization** — reuse the substrate (§3); only add geometry if the operator needs a new placement rule.
4. **Author the golden oracle** — hand-authored grips for the new derivation (or declare the inherited oracle for a pure reduce).
5. **Register coverage** in `CagedVoicingCatalog` (family × eligible qualities × shapes/forms).
6. **Surface it** — it flows automatically to `EngineVoicingSource` (the `automatic` catalog rows) and the comping resolver; show it on the engine inspector page.

Worked future examples: **Drop2/Drop3** = a *re-voice* operator (drop the 2nd/3rd-from-top voice an octave) — the first operator that forces the tone-selection (agnostic) vs realization split into separate namespaces. **6/9** = an *augment* operator (add the 6th + 9th to the tone set, then voice it).

---

## 7. Status & deferrals

- The engine is **dogfooded through an inspector/playground page** (Voicings Engine page): pick operator + quality + root + parameters, see the abstract voicing *and* the realized grip rendered via the guitar voicings render component — the live form of the golden oracle.
- A **cross-instrument core** (`VoicingsE` / `IVoicingsE`) is **deferred until a second instrument exists** — extracted from two real implementations, not guessed from guitar alone.
