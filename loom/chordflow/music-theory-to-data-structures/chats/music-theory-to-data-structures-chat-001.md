---
type: chat
id: ch_01KTKY2DZ5N60QTG50GFHD2GY9
title: music-theory-to-data-structures Chat
status: active
created: 2026-06-08
version: 1
tags: []
parent_id: null
requires_load: []
---
# music-theory-to-data-structures Chat

## Rafa:

We just build a minimun mvp just for testing purposes, but now is when we need to think deeper to make our app really powerful & flexible.
We need to use music theory, data structures and algoritms properly to avoid having anything hardcoded, everything should be transposable, expandable and bases on music theory.
We have 2 main and different themes to be represented & resolved: 
- (A) harmony: pitch, notes,. chords
- (B) rhythm: time

This is the core engine of the app and the reason why I pick C#

---
---
---

### A. HARMONY

I would like to have data structures to represent:
1. chords (diagram & tablature notes): 
\chord ("D#" 6 8 8 8 6 x) {firstFret 6}

(6.1 8.2 8.3 8.4 6.5){ch "D#"}
2. chord progressions, for example 2 5 1
r.1{ch "Dm7" txt "ii7"} | 
r.1{ch "G7" txt "V7"} | 
r.1{ch "Cmaj7" txt "Imaj7"} |

We should Use a data structure that allow transpose the chords and progression by an algoritm
Ready for scales, diatonic chords, etc

#### A.1 Use music theory to choose proper data structures

We shouldn't store chord names as strings (`"Dm7"`, `"G7"`, `"Cmaj7"`) as primary data model.

Instead, build the model around **music theory objects**, and generate:

* chord names
* Roman numerals
* alphaTex chord definitions
* tablature shapes

from those objects.

---

# Core Principle

Store:

```text
Theory
  ↓
Voicing
  ↓
Rendering
```

not:

```text
Rendered Chord Name
   ↓
Try to reverse engineer later
```

---

# Layer 1: Notes

```csharp
public enum PitchClass
{
    C = 0,
    Db = 1,
    D = 2,
    Eb = 3,
    E = 4,
    F = 5,
    Gb = 6,
    G = 7,
    Ab = 8,
    A = 9,
    Bb = 10,
    B = 11
}
```

Transpose becomes trivial:

```csharp
PitchClass Transpose(
    PitchClass note,
    int semitones)
{
    return (PitchClass)
        (((int)note + semitones + 12) % 12);
}
```

---

# Layer 2: Chord Quality

```csharp
public enum ChordQuality
{
    Major,
    Minor,
    Dominant7,
    Major7,
    Minor7,
    HalfDiminished,
    Diminished,
    Augmented
}
```

---

# Layer 3: Chord Definition

```csharp
public record Chord(
    PitchClass Root,
    ChordQuality Quality
);
```

Examples:

```csharp
new Chord(Bb, Major7);

new Chord(D, Minor7);

new Chord(G, Dominant7);
```

---

# Layer 4: Guitar Voicing

Separate theory from fretboard.

```csharp
public record GuitarVoicing(
    int? String6,
    int? String5,
    int? String4,
    int? String3,
    int? String2,
    int? String1,
    int FirstFret
);
```

Example D# barre:

```csharp
new GuitarVoicing(
    6,
    8,
    8,
    8,
    6,
    null,
    6
);
```

This directly generates:

```text
\chord ("D#" 6 8 8 8 6 x)
```

---

# Layer 5: Chord Shape Library

```csharp
public record ChordShape(
    Chord Chord,
    GuitarVoicing Voicing
);
```

Store many shapes:

```text
Bbmaj7
   Shape 1
   Shape 2
   Shape 3

Dm7
   Shape 1
   Shape 2
```

Then difficulty levels can choose:

```text
Beginner
   Open shapes

Intermediate
   Shell voicings

Advanced
   Random inversions
```

---

# Progressions

Don't store:

```csharp
"Dm7 G7 Cmaj7"
```

Store functions.

---

## Roman Numeral Degree

```csharp
public enum ScaleDegree
{
    I = 1,
    II = 2,
    III = 3,
    IV = 4,
    V = 5,
    VI = 6,
    VII = 7
}
```

---

## Harmonic Function

```csharp
public record ProgressionChord(
    ScaleDegree Degree,
    ChordQuality Quality
);
```

ii-V-I becomes:

```csharp
new[]
{
    new ProgressionChord(II, Minor7),
    new ProgressionChord(V, Dominant7),
    new ProgressionChord(I, Major7)
};
```

This is enormously powerful.

---

# Example

Key = C

Resolution:

```text
II  -> Dm7
V   -> G7
I   -> Cmaj7
```

Key = Bb

Resolution:

```text
II  -> Cm7
V   -> F7
I   -> Bbmaj7
```

Key = Eb

Resolution:

```text
II  -> Fm7
V   -> Bb7
I   -> Ebmaj7
```

Same progression object.

---

# Scale Definition

You'll need this very soon.

```csharp
public record Scale(
    PitchClass Tonic,
    int[] Intervals
);
```

Major:

```csharp
new Scale(
    C,
    new[] {0,2,4,5,7,9,11}
);
```

Minor:

```csharp
new Scale(
    A,
    new[] {0,2,3,5,7,8,10}
);
```

---

# Diatonic Chord Generator

Then:

```csharp
Chord BuildDiatonicChord(
    Scale scale,
    ScaleDegree degree)
```

can generate:

```text
C Major

I     Cmaj7
ii    Dm7
iii   Em7
IV    Fmaj7
V     G7
vi    Am7
vii   Bm7b5
```

automatically.

---

# Exercise Definition

Guitar-flow app's central object could be:

```csharp
public record Exercise(
    Key Key,
    Progression Progression,
    RhythmPattern Rhythm,
    Tempo Tempo,
    Difficulty Difficulty
);
```

Example:

```csharp
new Exercise(
    Key.Bb,
    Progressions.TwoFiveOne,
    RhythmPatterns.Beat1And3,
    80,
    Beginner
);
```

The engine then:

```text
Exercise
   ↓
Resolve progression
   ↓
Choose voicings
   ↓
Generate AlphaTex
   ↓
alphaTab
```

---

We are building a rhythm-training app, I'd define the domain model around:

1. **PitchClass** (notes)
2. **Scale**
3. **ChordQuality**
4. **Chord** (root + quality)
5. **ProgressionChord** (Roman numeral + quality)
6. **Progression** (ii-V-I, 12-bar blues, I-V-vi-IV, etc.)
7. **GuitarVoicing** (fret positions)
8. **RhythmPattern**
9. **Exercise**

With that structure, transposition, diatonic chord generation, scales, modes, arpeggios, chord substitutions, and alphaTex rendering all become straightforward extensions instead of special cases.

---
---
---

### B. Rhythm

About timing & rhythm:
We also need a data structure to easily define rhythmic patterns, like:
1e&a2e&a3e&a4e&a
x                x
We need shuffle and swing feel, triplets, pickup notes before a bar
We need easy way to define, store & combine this patterns
App will have 
- rhythm guitar training, showing chord progressions
- lead guitar training, showing sweet spots (maybe like dead notes) to be played in a solo
Suggestions?

#### B.1 How to approach Rhythm & time

This is where many music apps get into trouble: they model rhythm as **notation**, when they should model it as **time events**.

I'd recommend separating:

```text
Harmony
Rhythm
Fretboard Content
Rendering
```

completely.

---

# The Fundamental Unit: Rhythm Grid

Don't store:

```text
1e&a2e&a3e&a4e&a
x           x
```

Store:

```csharp
public record RhythmEvent(
    int Position,
    int Length,
    Accent Accent = Accent.Normal
);
```

where Position and Length are measured in **ticks**.

For example:

```csharp
public enum GridResolution
{
    Quarter = 4,
    Eighth = 8,
    Sixteenth = 16,
    Triplet8 = 12,
    Triplet16 = 24
}
```

For a 16th-note grid:

```text
1e&a2e&a3e&a4e&a

0 1 2 3
4 5 6 7
8 9 10 11
12 13 14 15
```

Your pattern:

```text
1e&a2e&a3e&a4e&a
x           x
```

becomes:

```csharp
[
    (0,1),
    (12,1)
]
```

---

# Rhythm Pattern Object

```csharp
public record RhythmPattern(
    string Name,
    GridResolution Resolution,
    IReadOnlyList<RhythmEvent> Events
);
```

Example:

```csharp
new RhythmPattern(
    "Beat 1 and 4",
    GridResolution.Sixteenth,
    [
        new RhythmEvent(0,1),
        new RhythmEvent(12,1)
    ]);
```

---

# Separate Rhythm from Notes

This is critical.

The rhythm engine should not know whether it's playing:

```text
Bb
Eb
F7
```

or

```text
G minor pentatonic
```

It only knows:

```text
Attack at tick 0
Attack at tick 12
```

---

# Then Apply Content

Rhythm:

```text
X       X
```

Chord progression:

```text
Bb
Eb
```

Result:

```text
Bb       Bb
Eb       Eb
```

Lead exercise:

```text
Target note
Target note
```

Same rhythm pattern.

---

# Swing / Shuffle

Do NOT bake swing into the pattern.

Store it separately.

```csharp
public enum Feel
{
    Straight,
    Swing,
    Shuffle,
    Triplet
}
```

Example:

```csharp
new Exercise(
    Feel.Shuffle
);
```

The renderer changes playback timing.

Pattern remains:

```text
1 & 2 & 3 & 4 &
X   X   X   X
```

but playback becomes:

```text
long-short
long-short
long-short
```

---

# Pickup Notes

Treat pickup notes as events before bar zero.

```csharp
new RhythmEvent(
    Position: -2,
    Length: 1
);
```

Example:

```text
pickup  1  &  2  &

      x  x
```

or create a dedicated concept:

```csharp
public record PickupMeasure(
    RhythmPattern Pattern
);
```

Either works.

---

# Bars and Measures

```csharp
public record MeasurePattern(
    RhythmPattern Pattern,
    int RepeatCount
);
```

Example:

```text
| Bb | Bb | Eb | Eb |
```

can reuse the same rhythm object.

---

# Strumming Information

You'll likely want:

```csharp
public enum Stroke
{
    Down,
    Up,
    Either
}
```

Event:

```csharp
public record RhythmEvent(
    int Position,
    int Length,
    Stroke Stroke,
    Accent Accent
);
```

---

# Lead Guitar Training

I would create another layer.

Instead of:

```text
play note 8
play note 10
```

store:

```csharp
public record TargetZone(
    ScaleDegree Degree,
    TargetImportance Importance
);
```

Example:

```csharp
Chord: G7

Targets:
3
b7
root
```

Then resolve to fretboard positions.

---

# Sweet Spots

This is where your app can become really interesting.

Example:

```csharp
public record SoloTarget(
    ChordFunction Function,
    ScaleDegree Degree,
    Importance Importance
);
```

For:

```text
ii - V - I
```

you can define:

```text
Dm7
  3rd
  7th

G7
  3rd
  b7

Cmaj7
  3rd
  7th
```

and render:

```text
o = target note
x = other scale notes

----------------
-------o--------
---------------
---o-----------
---------------
---------------
```

instead of full notation.

---

# Pattern Composition

This is the feature I'd build early.

A pattern should be composable.

```csharp
RhythmPattern Beat1And3
```

plus

```csharp
AccentPattern Backbeat
```

plus

```csharp
Feel Shuffle
```

produces:

```text
Final Exercise Pattern
```

---

# A Possible Complete Model

```csharp
Exercise
{
    Harmony
    Rhythm
    Content
    Feel
}
```

Harmony:

```csharp
ii V I
```

Rhythm:

```csharp
Hits on 1 and 3
```

Content:

```csharp
ChordVoicings
```

or

```csharp
SoloTargets
```

Rendering:

```text
AlphaTex
```

---

The key design decision I'd make is:

**Rhythm patterns should contain only timing information. Chords, scales, voicings, and solo targets should be separate layers applied onto that timing grid.**

That one decision will let you reuse the exact same rhythm library for:

* chord comping exercises
* blues rhythm guitar
* funk rhythm guitar
* pentatonic target-note exercises
* arpeggio studies
* jazz guide-tone training

without duplicating pattern definitions.

---
---
---

### Review

Analyze my proposals
What you think? what you like? what you dislike?
Agree? suggestions? improvements?


## AI:

Strong proposal. The spine of it — **Theory → Voicing → Rendering**, key-independent progressions, rhythm-as-timing separate from content — is exactly right, and most of it is *already* the architecture of the MVP engine. So this review is less "should we?" and more "here are 4 corrections to make before they calcify, and 1 real architecture decision I need you to make."

I grounded everything below against the code that already exists (`src/ChordFlow.App/Domain/*.cs`).

---

### What's already true in the codebase (let's not reinvent)

| Your proposal | Already built as |
|---|---|
| `Chord(Root, Quality)` | `Chord(PitchClass Root, Quality Quality)` ✅ identical |
| `ProgressionChord(Degree, Quality)` | `RomanDegree(int Degree, Quality Quality)` ✅ |
| Roman-numeral progression + transpose | `Progression` + `Transposer.Realize(prog, key)` ✅ already does ii–V–I in any key |
| `Scale(Tonic, Intervals)` | partial — interval arrays live inside `Transposer` (`MajorOffsets`/`NaturalMinorOffsets`), not yet a first-class `Scale` type |
| Shape library / difficulty | `Voicing(IReadOnlyList<FretPosition>)` + `VoicingBook` (one **algorithmic** movable shell shape, no hardcoded table) ✅ |

So the "don't store chord names as strings" battle is already won. Good.

---

### Where I'd change your model (the 4 corrections)

**1. Pitch class must NOT bake in spelling.** Your `enum PitchClass { Db=1, Eb=3, Gb=6 ... }` hardcodes *flat* names into the type. But pitch-class 1 is **C#** in D major and **Db** in Ab major — same pitch, different spelling depending on key. The MVP already got this right: `PitchClass` is just an `int 0..11`, and spelling is resolved later per key. Keep it a number. We need a **separate spelling function** `(pitchClass, key) → "C#"/"Db"` — that's the thing that's currently missing (the renderer hardcodes a flat-only array). Don't put accidentals in the enum.

**2. Quality should carry its interval set, not just a label.** Your `enum ChordQuality { Major7, Minor7, ... }` is a name. For the **diatonic generator** and especially for **lead targets (the 3rd, the b7)**, you need the actual notes. Define each quality as intervals from the root: `Maj7 = {0,4,7,11}`, `Dom7 = {0,4,7,10}`, `m7b5 = {0,3,6,10}`. Then *one* source generates chord tones, guide tones, AND your `TargetZone` degrees — "the b7 of G7" becomes `root + 10`, computable. This is the bridge that unifies your Theme A (harmony) and Theme B (lead targets). Keep the enum as a friendly label, but back it with an interval table.

**3. Don't reuse `ScaleDegree` for two different things.** You use `ScaleDegree` for progression roots (I–vii, relative to the **key**) *and* for solo targets (the 3rd of the current chord, relative to the **chord**). Those are different reference frames. The 3rd of G7 in C major is a key scale-degree of 5+... it gets confusing fast. Model two concepts: **`ScaleDegree`** (relative to key tonic, for progressions) and **`ChordTone`/interval** (relative to chord root, for voicings + lead targets). Your `TargetZone` should be chord-relative.

**4. Keep `Voicing` as a list, not 6 fixed nullable fields.** Your `GuitarVoicing(String6..String1, FirstFret)` is rigid — it assumes 6 strings and standard tuning forever. The existing `Voicing(IReadOnlyList<FretPosition>)` already handles partial voicings, drop tunings, 7-string later. **You're right that we need barre/`FirstFret` info** for the `\chord` diagram directive though — add that as optional metadata *on* the voicing (e.g. `BarreFret`, `MutedStrings`), not as positional fields.

---

### Rhythm — you're right, and this is the one real decision

Your central insight is the most valuable thing in the whole doc:

> **Rhythm patterns contain only timing. Chords / scales / voicings / solo-targets are separate layers applied onto the grid.**

100% agree. That single decision is what lets one rhythm library serve comping, blues, funk, and pentatonic target-note drills. **But** it conflicts with the MVP's current model, so this is a genuine fork:

- **Current model:** `RhythmPattern` = ordered `Beat(Duration, IsHit)` list — a *sequential* model. Simple, renders straight to alphaTex (which is itself sequential durations). Can't express "hit on the *a* of beat 2", ties, or polyrhythm.
- **Your model:** `RhythmEvent(Position, Length)` in **ticks** — a *positional grid*. Expresses syncopation, pickups, accents, swing-as-transform, composition. But alphaTex wants sequential durations, so it needs a **tick-grid → duration-token compiler/quantizer** in the renderer. That's real, non-trivial work.

I strongly favor your tick model for everything past the MVP — but two refinements:

- **Drop `GridResolution` as a per-pattern enum.** If each pattern picks Sixteenth *or* Triplet8, you can never compose a 16th groove with a triplet fill. Use **one fixed PPQ tick base** (e.g. 48 ticks/quarter — divisible by both 4→sixteenths=12 and 3→triplets=16) so all subdivisions coexist in one grid. This is what actually makes your "Pattern Composition" feature possible.
- **Pickup as its own anacrusis measure**, not `Position: -2`. Negative positions make bar math and rendering fiddly; a short leading measure with its own length is cleaner.

Everything else in Theme B I'd take as-is: `Feel` as a **playback-time transform** (never baked into the pattern) ✅, `Stroke` (Down/Up) on events ✅, `AccentPattern` as a separate composable layer ✅, `TargetZone`/`SoloTarget` resolved to fretboard late ✅.

---

### Gaps to add to the model

1. **Spelling engine** `(pitchClass, key) → note name` — needed for chord names *and* key signatures (currently hardcoded in the renderer).
2. **First-class `Scale` type** with modes — promote the interval arrays out of `Transposer`.
3. **Time signature in the rhythm model** — bar length in ticks depends on it; `\ts 4 4` is hardcoded today.
4. **Voicing selection as a strategy**, not a table — `Difficulty → shape chooser` so beginner/shell/inversions plug in.

---

### The decision I need from you before any code

**Do we migrate the rhythm model now (sequential `Beat` → tick-grid `RhythmEvent` + a quantizer in the renderer), or keep the sequential model for now and only adopt ticks when we build lead/swing?**

That's an architecture decision (it changes the renderer and the whole rhythm layer), so per our contract I'll **stop here and wait**. My recommendation: commit to the tick grid now while the surface area is tiny — retrofitting it after lead-training and persistence are built is much more expensive. But it does mean writing the grid→alphaTex quantizer up front.

If you agree on direction, the natural next move is to turn this chat into a proper **design doc** for the music-theory domain model, then a plan. Want me to draft that design doc next, or discuss the rhythm fork more first?

## Rafa:

Agreed. Glad we both arrive same spot for the app.
For sure, now it is the time to migrate the rhythm model, as you said, now the surface area is still tiny
If you consider we have enough ground, you could create the design doc


## AI:

## AI:

Drafted the design doc → `music-theory-to-data-structures-design.md` (`de_01KTM0DRF3Q7F4X35RMCBX6DDT`, status **draft**).

It captures everything we converged on:
- **Theme A (Harmony):** `PitchClass` stays a number + a `NoteSpeller`; `Quality` backed by interval sets (the bridge to lead targets); split `ScaleDegree` (key-relative) vs `ChordTone` (chord-relative); `Voicing` stays a list + diagram metadata + a difficulty *strategy*; first-class `Scale` + diatonic generator.
- **Theme B (Rhythm):** migrate to a positional **tick grid** now; **one fixed 48-PPQ base** (not per-pattern `GridResolution`) so 16ths + triplets compose; `Feel`/`Accent`/`Stroke` as overlays; pickup as a leading measure; and a new **grid→alphaTex quantizer** isolated in the renderer.
- **Unifying `Exercise`** + the engine pipeline, plus a lead-training `TargetZone` layer.

I left **4 open questions** at the bottom (§8) that I'd like your call on before turning this into a plan:

1. **PPQ = 48** (covers 16ths + triplets) — good enough, or do you want 32nds/quintuplets (→ 96/240)?
2. **Time signatures in v1** — 4/4 only, or include compound (6/8) and odd (5/4, 7/8) from the start?
3. **Migration** — regenerate from definitions (likely no data migration needed since SQLite stores definitions) — agree?
4. **Quality coverage for v1** — which qualities ship first?

Answer those (or say "your call") and I'll write the implementation plan. **Stopping here for your input.**

## Rafa:

1. **PPQ = 48** (covers 16ths + triplets) — good enough
2. **Time signatures in v1** — 4/4 only for now
3. **Migration** — agreed, even delete all an star over if needed
4. **Quality coverage for v1** — start with Maj/Min/Dom7/Maj7/Min7/m7b5/dim/aug

Tell me if you consider any other approach better
If agree and all clear:
1. generate requireremnts
2. create plan

## AI:

All four answers locked in — they match my recommendations, so no pushback. One micro-decision I baked in: `Diminished`/`Augmented` are **triads** (`{0,3,6}` / `{0,4,8}`), kept distinct from `m7b5` (half-diminished `{0,3,6,10}`).

Generated both docs:

- **Requirements** → `req.md` (`rq_01KTM0Y9B6JMQNJDA8THQG4WCR`) — 15 included / 6 excluded / 7 constraints. PPQ=48, 4/4-only, regenerate-don't-migrate, and the 8 quality interval sets are all pinned as constraints.
- **Plan** → `music-theory-to-data-structures-plan-001.md` (`pl_01KTM0Z0RPTP75F7CWBCZC8AZ1`) — **9 steps**:
  1. Quality interval sets + `ChordTone`
  2. `Scale` + diatonic generator (refactor `Transposer`)
  3. `NoteSpeller` (promote spelling from renderer)
  4. Voicing diagram metadata + `IVoicingStrategy`
  5. Tick-grid rhythm types + quantizer **in parallel** (build stays green)
  6. Atomic switch to tick model + delete old `Beat` + wipe dev DB
  7. Feel / Accent / Stroke overlays
  8. Lead `TargetZone` domain layer
  9. End-to-end wiring + verification

Steps 1–4 are additive (MVP stays green); 5–6 are the real rhythm migration done safely; 7–9 finish overlays, lead targets, and wiring.

Both docs are **draft** — review them and, when you're happy, say the word and I'll finalize the req + start the plan at Step 1.

## Rafa:

I already finalized `reqs`
You created the plan with table steps `Satisfies` column empty so reqs shows 15 gaps
If execute `refine plan` will take reqs into account and will fill `Satisfies` column properly

### Issue

when you reply in chat you write twice headers:

```markdown
## AI:

## AI:
```

why this is happening?

## AI:

Thanks for deleting the dupes — and good catches on both fronts.

**Why the double `## AI:` happened.** `loom_append_to_chat` derives the message header from the `role` argument and prepends `## AI:` itself. I was *also* writing `## AI:` as the first line of the `body` I passed it, so the tool's header and mine stacked. The fix is entirely on my side: pass only the message content in `body` and let the tool own the role header. This very reply is sent with no header line in the body — so if it renders with a single `## AI:`, the fix holds.

**On `refine plan`.** Good instinct, but it won't work in this session: `loom_refine_plan` (like all `refine_*` / `generate_*`) runs via MCP **sampling** (server→client inference), and Claude Code CLI blocks sampling — it returns `MethodNotFound`. So instead I filled the `Satisfies` column directly with `loom_update_doc` (the non-sampling path). Two things came out of it:

- **Found a real gap:** `IN11` (pickup/anacrusis as a leading measure) wasn't covered by any step. Added it to Step 5.
- **Fixed two mis-citations:** Steps 6 and 8 had cited `EX3`/`EX5` in `Satisfies`. A step doesn't *satisfy* an Excluded item — it *respects* it — so I moved those out of the column and into the step prose.

**`loom_verify_req` is now clean:** `uncovered: []`, `excludedViolations: []`, `unknownCitations: []`. (The semantic AI pass is skipped — sampling blocked — but I reviewed it by hand: every `Satisfies` cell matches its step, and constraints C1/C2/C4/C5/C7 are cited where they apply.)

Plan `pl_01KTM0Z0RPTP75F7CWBCZC8AZ1` is updated and verified against the locked req. Ready to `loom_start_plan` and begin **Step 1 — Quality interval sets + ChordTone** whenever you give the go.


## AI:

## Music-Theory Domain Model — implemented steps 1–9

All 9 steps complete; the plan (`pl_01KTM0Z0RPTP75F7CWBCZC8AZ1`) auto-transitioned to **status: done**. Every step built, tested green, and recorded via `loom_append_done` + `loom_complete_step`. Test count grew **39 → 106**, all passing; full solution builds clean.

| # | Step | What landed |
|---|------|-------------|
| **1** | Quality intervals + ChordTone | Expanded `Quality` to the 8 v1 qualities; `QualityIntervals` table (C5); chord-relative `ChordTone`/`ChordToneFunction` + `ChordTones.Of` generator. |
| **2** | Scale + diatonic generator | First-class `Scale` (offsets moved out of `Transposer`); `ScaleDegree`; `DiatonicChord.Build` deriving quality via reverse interval match (C maj7…vii m7b5). `Transposer` now consumes a `Scale`. |
| **3** | NoteSpeller | Pure per-key sharp/flat speller + `\ks` token; renderer's hardcoded key arrays deleted (C2). |
| **4** | Voicing metadata + strategy | `Voicing` gained `BarreFret/FirstFret/MutedStrings`; `IVoicingStrategy` + `BeginnerShellStrategy`; `VoicingBook` now a strategy dispatcher. |
| **5** | Tick rhythm types + quantizer | `TickGrid(Ppq=48)`, `TimeSignature`, `RhythmEvent`, `Stroke`/`Accent`, `PickupMeasure`, parallel tick pattern; `RhythmQuantizer` (beat-line split, tie metadata). Old Beat model untouched (C7). |
| **6** | Atomic migration | Deleted `Beat`/`Duration`/`TickRhythmPattern`; `RhythmPattern` is now the tick model; renderer driven by the quantizer. Existing renderer tests pass **byte-identical**. No DB migration needed (keyed by `RhythmId`, EX3). |
| **7** | Feel/Accent/Stroke overlays | `Feel` + `FeelTransform` (long-short warp), `AccentPattern` (backbeat), `StrokeOverlay` — pure, composable, base never mutated (C4). |
| **8** | Lead TargetZone | `Importance`, `TargetZone`, `Fretboard`, `LeadTargets` — guide tones (3 & 7) derived from interval sets and resolved to fret positions (domain-only, EX5). |
| **9** | End-to-end wiring | `Exercise` gained `Feel` (default Straight); renderer applies feel pre-quantize. Bb 12-bar blues smoke test confirms valid alphaTex through the full path. |

### Notable decisions
- **Quantizer splits at beat lines** — reproduces the MVP `:4 (chord) r r r` exactly and matches the beat-cursor semantics. Ties are modeled as slot metadata but never emitted for MVP patterns, since the alphaTex reference flags tie/dotted tokens as **unverified** (renderer throws defensively if a tie ever reaches it).
- **`ScaleDegree` kept distinct from `RomanDegree`** (per IN4) rather than conflating — `ScaleDegree` derives quality from the scale; `RomanDegree` carries explicit quality for authored progressions.
- **Feel applied at render-time only**, never stored on the pattern (C4); `Straight` is the identity warp so all prior output is unchanged.

No work outside the listed steps; no unlisted prerequisites were needed. The plan is now `done`.

## Rafa:

After this big core domain update review if `loom/ctx.md` need to be updated.
I consider would be good to have a reference doc for a summary of all domain, music theory, data structures used to implement this thread.
This reference doc should be linked in global `loom/ctx.md` requires_load or linked in `## 3. Reference docs...` to be loaded when design, implementing new features or updating domain...

If you agree do it
Then commit all, push, release 0.3.0

## AI:

Agreed — done. Summary of this housekeeping pass:

**Reference doc created** — `loom/refs/chordflow-domain-model-reference.md` (id `rf_01KTM41K36DYJ0CE44FE7TMCGH`). A map of the whole kernel: harmony, the 48-PPQ tick rhythm grid, voicings, feel/accent/stroke overlays, lead targets, the quantizer/render seam, the `Exercise` pipeline, and the C1–C7 invariants. It cross-links the alphaTex/alphaTab refs.

**`loom/ctx.md` updated** (v3 → v4):
- §3 now leads with the domain-model reference and the section is retitled "load when designing/implementing features, the domain, the renderer, or the WebView layer".
- §2 `Domain/` bullet rewritten to describe the music-theory-first kernel (interval-backed Quality, Scale + diatonic, tick rhythm, overlays, lead targets) and point at the new ref; `Rendering/` bullet now mentions the `RhythmQuantizer`.

**Linking decision** — I put the ref in **§3 (on-demand pointer)**, *not* `requires_load`. `requires_load` pulls a doc into *every* session's context; this is a deep reference only needed when working the domain, and your own phrasing ("loaded when designing/implementing/updating domain") is exactly the §3 on-demand pattern. Easy to promote to `requires_load` later if you find you want it always-on.

**Observation (not changed):** ctx §2 still says distribution is "Photino.NET", but per CHANGELOG 0.1.0 the host migrated to WinForms + WebView2. Left it alone to stay scoped to this domain pass — flag if you want it corrected.

**Release 0.3.0** — csproj `<Version>` 0.2.0 → 0.3.0; CHANGELOG `[0.3.0]` "Phase 4 — music-theory-first domain" entry + link. Full suite **106 passed**. Committing all (incl. the new thread docs and your now-deleted `copy.md`), tagging `v0.3.0`, pushing to `main`.