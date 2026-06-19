---
type: chat
id: ch_01KVGEBB2QN9J13H1YG4TX5JYP
title: interval-lattice Chat
status: done
created: 2026-06-19
updated: 2026-06-19
version: 2
tags: []
parent_id: null
requires_load: []
last_ai_block: 20
---
# interval-lattice Chat

## Rafa:

I did a research and re-think about guitar interval lattice going deeper.

### Semitones distance

In guitar each fretboard step represent a semitone step
We defined intervals as `1 b2 2 b3 3 4 b5 5 b6 6 b7 7 8`

So the index is the semitone distance:

0  -> 1
1  -> b2
2  -> 2
3  -> b3
4  -> 3
5  -> 4
6  -> b5
7  -> 5
8  -> b6
9  -> 6
10 -> b7
11 -> 7
12 -> 8


So:
semitones = intervalIndex


and
interval = intervals[semitones]

very simple

### It matches how guitarists think

Many guitar systems (CAGED, interval training, fretboard visualization) use:

1 b2 2 b3 3 4 b5 5 b6 6 b7 7

rather than:

P1 m2 M2 m3 M3 P4 d5 P5 m6 M6 m7 M7

The latter is music-theory correct but less convenient on a fretboard.

### It naturally extends across octaves

We can continue:

8 b9 9 b10 10 11 b12 12 b13 13 b14 14 15

or normalize everything mod 12 and store octave separately.

### For descending intervals

This notation adapts nicely:

```text
-1
-b2
-2
-b3
...
-7
-8
```

or simply store:

```ts
{
  semitones: -7
}
```

and derive the label when rendering.

---

I think we should treat **semitone distance as the canonical value** and these interval names as labels:

```ts
0  => "1"
1  => "b2"
2  => "2"
3  => "b3"
...
12 => "8"
```

Then all calculations become integer arithmetic, and the interval notation is just a view on top. That's especially attractive for an `interval_lattice`, since fret movement is fundamentally semitone movement.

### Interval-lattice

By using guitar tuning (standard) strings relation 
string X to string X-1 = 5 semitones = interval 4
except string 3 to 2 = 4 semitones = interval 3

And `fretboard steps` = `semitone steps`, we could calculate all intervals across all fretboard

We start from:
0  -> "1"
1  -> "b2"
2  -> "2"
3  -> "b3"
4  -> "3"
5  -> "4"
6  -> "b5"
7  -> "5"
8  -> "b6"
9  -> "6"
10 -> "b7"
11 -> "7"
12 -> "8" 

We define:

1. The tuning relationship between strings.
2. One fret = one semitone.
3. A semitone → interval label mapping.

Then the entire interval lattice emerges automatically.

## Step 1: Define string offsets

Standard tuning:

```
e4
B3
G3
D3
A2
E2
```

Relative semitone offsets between adjacent strings:

```
String 6 -> 5 = +5
String 5 -> 4 = +5
String 4 -> 3 = +5
String 3 -> 2 = +4
String 2 -> 1 = +5
```

Let's define cumulative offsets from string 6:

```text id="mxx3ij"
String 6 = 0
String 5 = 5
String 4 = 10
String 3 = 15
String 2 = 19
String 1 = 24
```

This is the most useful representation.

```ts
const STRING_OFFSET = [
  0,   // string 6
  5,   // string 5
  10,  // string 4
  15,  // string 3
  19,  // string 2
  24   // string 1
]
```

---

## Step 2: Every position has an absolute semitone coordinate

Represent a position as:

```ts
{
  string: number,
  fret: number
}
```

Absolute coordinate:

```ts
absolute =
    STRING_OFFSET[string]
  + fret
```

Examples:

```text
6th string fret 0 = 0
6th string fret 3 = 3

5th string fret 0 = 5
5th string fret 3 = 8

2nd string fret 0 = 19
1st string fret 0 = 24
```

---

## Step 3: Interval between any two positions

```ts
distance =
    absolute(target)
  - absolute(origin)
```

Example:

```text
6th string fret 0
to
5th string fret 0

distance = 5 semitones
```

which maps to: "4"

---

Another:

```text id="pccl1s"
6th string fret 0
to
4th string fret 0

distance = 10 semitones
```

maps to: "b7"

---

## Step 4: Interval names

Our table:

```ts
const INTERVALS = [
  "1",
  "b2",
  "2",
  "b3",
  "3",
  "4",
  "b5",
  "5",
  "b6",
  "6",
  "b7",
  "7"
]
```

Then:

```ts 
function intervalName(semitones: number) {
  const octave = Math.floor(Math.abs(semitones) / 12)
  const index  = Math.abs(semitones) % 12

  return {
    label: INTERVALS[index],
    octaves: octave,
    direction: semitones >= 0 ? "up" : "down"
  }
}
```

---

## Step 5: The lattice interpretation

This is where term `interval_lattice` becomes interesting.

Every position can be viewed as a:
- coordinate: (string, fret)
but also as:
- semitone distance from origin

So the lattice is essentially:

L(string, fret) = stringOffset(string) + fret

Everything else is derived.

For example, root at:

String 6
Fret 0

Then:

String 5 Fret 0 = 4
String 4 Fret 0 = b7
String 3 Fret 0 = b3
String 2 Fret 0 = 5
String 1 Fret 0 = 1 (2 octaves)

Notice something beautiful:

We don't need interval formulas at all.

The entire fretboard becomes:

interval = (semitone coordinate difference) → lookup table

That's why we should model the core lattice as **integer semitone coordinates**, and treat:

1 b2 2 b3 3 4 b5 5 b6 6 b7 7 8

as the rendering layer. 

The mathematics of the fretboard are semitone-based; the interval names are just human-readable projections of that structure. 

This makes transposition, pattern matching, symmetry detection, and alternate tunings much easier later.

## AI:

Read your research — this is the right call, and it's stronger than the original idea framing.

**What I agree with:**
- **Semitone integer as canonical, interval name as a view.** Yes. This is the correct primitive. Transposition, pattern-matching, symmetry, and (later) alternate tunings all fall out for free.
- **Your `STRING_OFFSET = [0,5,10,15,19,24]` is already consistent with the codebase.** `Fretboard`'s open-string table is E A D G B E, and `(E + absolute) mod 12` reproduces every open-string pitch class exactly — string 3 abs 15 → G, string 2 abs 19 → B, string 1 abs 24 → E. So the lattice isn't *new* tuning data; it's the **octave-preserving (non-mod-12)** version of what `Fretboard` already encodes. Build it next to / on top of `Fretboard` — don't duplicate the tuning.

**One improvement on the original idea:** the idea framed this as "enumerate 2 octaves L/R per string." Your absolute-coordinate approach makes that unnecessary:

```
distance = abs(target) − abs(origin)
label    = INTERVALS[ |distance| % 12 ]
octave   = |distance| / 12
```

No pre-enumeration, no L/R boundary bookkeeping, exact at any range. I'd drop the "2 octaves each way" framing in favor of this on-demand function — same data, less to maintain.

**Two cautions:**
- The one irregularity is **string 3→2 = 4 semitones** (the B string). Your cumulative table handles it (the +4 step), but everything downstream must go through `STRING_OFFSET` — no shortcut that assumes a uniform +5 per string, or it silently breaks across the B string.
- **Spelling.** The mod-12 lookup gives the *default* fretboard label (1 b2 2 b3 …). The vocabulary in `[[intervals]]` has context-dependent spellings (e.g. dim7's bb7). That's fine and actually supports your thesis: keep the lattice's output as the **semitone integer + default label**, and let `[[chord-qualities]]` re-spell when it knows the chord context. Spelling is the rendering layer, exactly as you said.

**Dependency correction** (full corrected graph is in the octave-shapes chat): interval-lattice should **drop its dependency on octave-shapes**. I verified all five octave-shape offsets are derivable from this lattice's tuning table + mod 12 — so the lattice is the base and octave-shapes builds on it, not the reverse. interval-lattice depends only on `[[intervals]]` (+ `Fretboard` geometry). You were right that it was blocked backwards.

## Rafa:

I think we are getting better base design in the most important concept in Domain Music Theory: intervals

I would like you review if our current implementation of domain/intervals should be updated any way to fit and support guitar/interval-lattice.

this should be implemented in the source domain/intervals and used in guitar/interval-lattice for fretboard aplication:
semitones -> interval 
0  -> "1"
1  -> "b2"
2  -> "2"
3  -> "b3"
4  -> "3"
5  -> "4"
6  -> "b5"
7  -> "5"
8  -> "b6"
9  -> "6"
10 -> "b7"
11 -> "7"
12 -> "8" 

Tell me what you find. Maybe we need some redesign in domain/intervals

### Side question

I wonder if we should rename `Domain` to `Music`, I see pros and cons, but I lean to `Music` (Not important, no hurry)
What you think?

## AI:

Reviewed `domain/intervals`. **Headline: the mapping you want already exists, and the domain is already built on exactly the "semitone is canonical" principle you're describing. No redesign needed — the lattice just consumes it.**

**1. Your `semitones → interval` table is already implemented — as `IntervalSpeller.Name(int)`.**

The data behind it (`FlatsBase`) is byte-for-byte your table:

```
0→"1" 1→"b2" 2→"2" 3→"b3" 4→"3" 5→"4" 6→"b5" 7→"5" 8→"b6" 9→"6" 10→"b7" 11→"7"
```

…and it already **unfolds past the octave** for free: `Name(12)="8"`, `Name(14)="9"`, `Name(17)="11"`, `Name(24)="15"` (formula: `number = base(s%12) + 7*(s/12)`). So it's a superset of what you asked for — your 12-entry table *plus* the octave extension scales/arpeggios will need.

**2. The whole domain already treats the integer as canonical and the name as a view** — which is your exact thesis:
- `QualityIntervals` stores each quality as a semitone set (`Major = {0,4,7}`, `Diminished7 = {0,3,6,9}`).
- `ChordTone(int Interval, …)` is semitone-relative-to-root; spelling is deferred.
- `IntervalSpeller.Label(semitone, role)` is the context-aware *spelling* layer on top (it's where `bb7` for dim7 comes from).

So the domain is already aligned with the lattice. The lattice should **call `IntervalSpeller.Name`, not define its own `INTERVALS` table** — otherwise we'd have two copies of the same vocabulary that can drift (exactly the duplication we just removed from octave-shapes).

**3. The one real design question — and it's a *lattice* concern, not a domain change: direction.**

`IntervalSpeller.Name` is **non-negative and absolute** (it throws on a negative semitone). The lattice computes `distance = abs(target) − abs(origin)`, which can be negative (positions left/below the root). Two clean ways to use it:
- **Pitch-class labeling** (what you usually want on the fretboard — "what interval is this fret vs. the root"): reduce `distance` mod 12 → always 0–11 → `Name(distance % 12)` gives `1…7`. Direction doesn't matter.
- **Unfolded with octave** (scales/arpeggios): keep `distance ≥ 0` and call `Name(distance)` → `8/9/15`.

Your earlier `intervalName(semitones)` already factored sign out (`abs` + a `direction` flag). I'd **keep `Name` as the pure magnitude→label substrate and let the lattice carry the sign/direction** — don't teach `Name` about negatives. Minimal, and it keeps the descending case a guitar-display concern.

**4. The only genuinely *new* code is guitar-side, not domain:** an **absolute, octave-preserving coordinate** `abs(string, fret) = STRING_OFFSET[string] + fret`. The existing `Fretboard.PitchClassAt` folds to mod-12 and *loses the octave*, so it can't give you signed distances. That absolute-coordinate helper lives in `guitar/interval-lattice`, built next to `Fretboard` — `domain/intervals` doesn't change for it.

**Optional (only if a consumer asks):** a reverse `label → semitone` parser, or promoting intervals to a first-class `Interval` value type. The lattice needs neither (it goes coordinate → semitone → `Name`). I'd not build them speculatively.

**Bottom line:** domain/intervals is already the right shape. Action is just: lattice consumes `IntervalSpeller.Name` + adds the absolute coordinate. The only thing worth a doc tweak is making `IntervalSpeller`'s role explicit as *the* vocabulary the guitar lattice projects (a one-line note in the intervals idea/ref), so nobody re-authors the table on the guitar side.

---

**Side question — `Domain` → `Music`:** I lean the same way you do, with caveats.

*For Music:* it's what the kernel actually is (ctx already calls it the "music-theory-first kernel"), it's less generic than the DDD term "Domain," and it pairs beautifully with the new `Instruments/` weave — **`Music` = pure theory, `Instruments` = how an instrument projects that theory onto frets/strings.** That conceptual pair is genuinely nicer.

*Two cautions:*
- It's a **large mechanical rename** — namespace `ChordFlow.Domain` → `ChordFlow.Music`, the folder, every `using`, the 3 ref docs + `ctx.md`, and the MCP-gate paths. Worth doing as its own isolated commit, not riding along with feature work.
- It surfaces a latent question: `Domain/` today isn't *only* theory — it also holds `Exercise`, `SeedData`, `IProgressionStore`, the Song parser. "Music" invites "why is `Exercise` in Music?" So decide the **scope** first: (a) pure rename of the existing grab-bag, or (b) `Music` means *pure theory* and the feature/persistence types move out. (a) is cheap; (b) is a real reorg.

My recommendation: **agree on `Music`**, but defer it until either we're already doing the theory/guitar reorg or there's a quiet moment — and at that point pick (a) vs (b). Since it's "no hurry," parking it is fine; it doesn't block the lattice.

## Rafa:

Nice to hear good news about domain/intervals
I have many things in my head and I forgot easily, that is another reason why I created loom and roadmap

### About Direction

Could we implement both approachs and use them depending on purpose?
- Pitch-class labeling
- Unfolded with octave

### TODO

I think this is next actions to take:
- re-wire this thread dependencies
- I will fix roadmap order
  1. domain/intervals ✅
  2. guitar/interval-lattice 
  3. guitar/octave-shapes 
  4. domain/chord-qualities
  5. guitar/caged-system 

- update guitar/interval-lattice idea
- update guitar/octave-shapes idea
- spin up a weave/thread idea for rename Domain to Music

then we will be ready to continue with idea -> design -> reqs -> plan for guitar/interval-lattice

Tell me if I miss anything.

### NOTE

I think after creating any new Guitar feature we should test and dogfooding it by:
- creating a UI screen page
- use fretboard render component

For example, after guitar/interval-lattice create a new UI page for intervals/scales showed in fretboard.
This way we have fast visual confirmation we are goind in the right direction before going forward blindly.

### Language learning

I am Spanish, I use English with you to improve it. 
Please, memorize: when I made a very bad use of English, mostly semantic, grammar please ping me a brief note.

## AI:

**On Direction — yes, both, and they aren't even two implementations.** They're two thin *views* over one canonical value, which is exactly your "integer is canonical, names are views" principle:

- The lattice's core returns the **signed semitone distance** `d = abs(target) − abs(origin)` — the canonical value, computed/stored once.
- **Pitch-class label** (fretboard "what interval is this vs. the root"): `Name(((d % 12) + 12) % 12)` → always `1…7`.
- **Unfolded + octave** (scales/arpeggios): `Name(|d|)` → `8/9/11/15…`, plus a direction flag from `sign(d)`.

So "both, depending on purpose" = the same `IntervalSpeller.Name`, called with either the mod-12 reduction or the raw magnitude. One primitive, two projections — no duplication.

**Your TODO — solid and correctly ordered. What I'd add/clarify:**

1. **Identify the 2nd dependency before re-wiring.** Both threads currently carry a *second* `depends_on` besides intervals (`th_…NCYWV7`). Before I drop/re-add edges I want to confirm what it is (the guitar-voicings / default-pack golden-oracle thread? instrument-boundary?) so we don't silently drop a real edge. 30-second check.

2. **The re-wire ripples past these two threads:**
   - interval-lattice: `depends_on` → **[intervals]** only
   - octave-shapes: → **[intervals, interval-lattice]**
   - chord-qualities: → **[intervals]**
   - caged-system: → **[octave-shapes, chord-qualities, interval-lattice, + the 34-voicing oracle thread]**
   - Make sure chord-qualities & caged-system actually have threads/ideas to hang these edges on.

3. **Ref-doc sync** (the contract): when we touch the two ideas, also add the one-liner to `domain-model-reference` (and/or the intervals idea) that **`IntervalSpeller.Name` is THE vocabulary the guitar lattice projects** — so nobody re-authors the table guitar-side. `architecture-reference` gets the Instruments/Guitar geometry note later, when the lattice actually lands (implementation time, not now).

4. **Flow:** you've got idea → design → **req** → plan for interval-lattice — keep the req and lock it before the plan, that's our standard. 👍

Otherwise the list is complete.

**On the dogfooding NOTE — strongly agree, and let's make it a *standing rule*, not ad-hoc.** "Every guitar feature ships with a fretboard UI page that visualizes it" is an excellent forcing function — and we already have a `FretboardDiagram` component to build on. I'd capture it in two places so it survives your "I forget easily": (a) a line in each guitar idea's **Validation** section ("dogfood: render on the fretboard UI page"), and (b) a one-liner in global `ctx.md` as a standing practice for the guitar weave. For interval-lattice specifically: a page that lights up every interval around a chosen root on the fretboard — that's the perfect visual proof the lattice math is right *before* building chord-qualities/caged on top of it.

**Next — say which and I'll go (stopping here for your green light, since these are state changes):**
- (a) identify the 2nd dependency + re-wire all the `depends_on` edges
- (b) update the interval-lattice & octave-shapes ideas (slim octave-shapes to partition-only; lattice consumes `IntervalSpeller`, both Direction views, the on-demand `distance()` framing)
- (c) spin up the `Domain → Music` rename idea/thread
- (d) add the dogfooding rule to ctx + the idea Validation sections

## AI:

Done — all of (a)–(d). Summary:

**(a) Dependencies re-wired** (the mystery 2nd edge was **instrument-boundary**, kept):
- interval-lattice → `[instrument-boundary, intervals]` (dropped octave-shapes)
- octave-shapes → `[instrument-boundary, intervals, interval-lattice]`
- chord-qualities → `[intervals]` (already correct, untouched)
- caged-system → `[instrument-boundary, octave-shapes, chord-qualities, interval-lattice, guitar-voicings]` (added guitar-voicings as the golden-oracle edge)
- `loom_validate`: no cycles / broken edges. (Priorities left alone — that's your "fix roadmap order" task.)

**(b) Ideas updated:**
- `interval-lattice-idea` — lattice is now the **base primitive**: `STRING_OFFSET = [0,5,10,15,19,24]`, `abs(string,fret)`, signed `distance`, both label views via `IntervalSpeller.Name` (no re-authored table), on-demand `distance()` over "2 octaves L/R", the B-string +4 caveat, and the inverted dependency.
- `octave-shapes-idea` — re-parented onto the lattice; slimmed so the **only authored data is the CAGED partition** `{shape → root strings, primary}` (offsets demoted to validation examples); recorded your three answers (string-1 = string-6 same fret, primaries, +12 wrap, query option **c**).

**(c)** New idea: `chordflow/domain-to-music-rename` — captures the `Domain → Music` rename, the case for it, the large-mechanical-rename caution, and the (a)-pure-rename vs (b)-theory-only-reorg scope decision. Deferred, blocks nothing.

**(d)** Dogfood rule added to global `ctx.md` (guitar-weave standing rule) + a "dogfood: fretboard UI page" line in both ideas' Validation sections.

**Your turn:** fix the roadmap priority order. After that we're clear to start **interval-lattice: idea → design → req → plan**. Want me to kick off the design when you're ready?

## Rafa:

I think priority order is already correct for at least 4 next threads, confirm

If so, commit push

## AI:

**Confirmed — priority order is fine, no changes needed.** Now that the dependency graph is corrected, the roadmap order is derived **topologically**, and dependencies dominate priority (priority only breaks ties between threads with no dependency relationship). So the next sequence is enforced by the edges:

- **intervals** ✅ (done)
- **interval-lattice** — deps `[instrument-boundary, intervals]`, both ready → next
- **octave-shapes** — deps include `interval-lattice` → necessarily after it
- **chord-qualities** — deps `[intervals]` only → free to run any time after intervals (parallel slot)
- **caged-system** — deps on all of the above → last

One harmless note: octave-shapes' priority number (30) is *lower* than interval-lattice's (40), which would normally sort it earlier — but the new `octave-shapes → interval-lattice` dependency overrides that, so the real order is still correct. Purely cosmetic if you ever want the numbers to read in order too (e.g. bump octave-shapes to 45), but not required.

Committing + pushing now.

## Rafa:

If all is grounded create design
generate requirements

## AI:

Created both (draft):
- **`interval-lattice-design.md`** — placement (`Instruments/Guitar/Geometry/IntervalLattice.cs`, static, pure), the absolute-coordinate tuning, the API surface (`Absolute` / signed `Distance` / two label views via `IntervalSpeller.Name` / `PositionsOfInterval` / `LabelAt`), reuse-not-duplicate with `Fretboard`, no domain change, validation incl. the octave-shape golden check.
- **`req.md`** — IN1–IN10 / EX1–EX7 / C1–C8. Left **unlocked** pending your call on the three decisions below.

**Three decisions to confirm before I lock the req + plan** (I recommend the first option each):

| # | Decision | Recommend | Alternative |
|---|----------|-----------|-------------|
| **D1** | Tuning source | **Single-source** — derive `Fretboard.OpenPitchClass` from the new absolute base so the two can't drift (small `Fretboard` refactor) | Keep both tables + a consistency unit test |
| **D2** | `PositionsOfInterval` match | **Pitch-class + fret window** (all octaves of the degree in range — matches how chord tones land in a zone) | Exact-octave match only |
| **D3** | `LatticeInterval` record | **Ship it now** (cheap; the dogfood UI will want octave + direction) | Defer — raw `int` + the two label helpers until the UI needs it |

One scope note worth your eye: I put the **dogfood UI page in `ui/intervals-scales`** (the thread you already scaffolded), not in this Core thread — so interval-lattice ships the Core query + tests, and the UI page that renders it lives in the UI thread (`EX6` / `C7`). Tell me if you'd rather fold the UI into this thread.

Confirm D1–D3 (and the UI-scope split) and I'll lock the req, mark idea/design done, and draft the plan.

## Rafa:

D1. I want one source of truth, agree with your lean. But why we have now 2 and where lives the current 2 sources? explain briefly.
D2. Pitch-class + fret window. 
D3. Now

UI in `ui/intervals-scales` correct, I created it for that purpose

## AI:

**D1 — why "two sources," and where they live (brief):**

Right now there's actually **only one** tuning source: `Fretboard.OpenPitchClass = {0,4,11,7,2,9,4}` (in `Instruments/Guitar/Geometry/Fretboard.cs`) — the open-string **pitch class** per string, mod-12.

The *second* table only appears **if we let it**: the lattice needs an **octave-preserving** coordinate (absolute semitones from low E, e.g. `{_,24,19,15,10,5,0}`), and the lazy way is to author that as its own literal in `IntervalLattice`. Then we'd have two tuning tables that could drift — that's the duplication D1 kills.

Direction of derivation matters: the **absolute** table carries strictly *more* information (octave **and** pitch class), and `OpenPitchClass[s] = (4 + absoluteBase[s]) % 12`. You can derive mod-12 from absolute, but **not** the reverse (mod-12 has thrown the octave away). So the fix is: the **absolute base becomes the single source of truth, living in `Fretboard`** (the geometry/tuning home), and `PitchClassAt`/`OpenPitchClass` are *computed* from it. `IntervalLattice` then consumes `Fretboard`'s absolute coordinate — it authors no tuning of its own.

D2 ✓ pitch-class + window · D3 ✓ ship `LatticeInterval` now · UI ✓ stays in `ui/intervals-scales`.

Proceeding: tightening the design/req to say the base lives in `Fretboard`, then **locking the req, marking idea + design done, and drafting the plan**.

## AI:

✅ Locked the req · idea + design marked **done** · plan drafted: **`interval-lattice-plan-001.md`** (6 steps):

1. **Single-source tuning in `Fretboard`** — author the absolute base + `AbsoluteSemitone`, derive `PitchClassAt`/`OpenPitchClass` from it *(IN8, IN2, C3)*
2. **`IntervalLattice` core** — `Absolute` (delegate) + signed `Distance`; B-string + descending tests *(IN1, IN2, IN3, C2, C4, C6)*
3. **Label views + `LatticeInterval`** — `PitchClassLabel` + `Describe`, both via `IntervalSpeller.Name` *(IN4, IN5, C1)*
4. **`PositionsOfInterval` + `LabelAt`** — pitch-class + window, on top of `Fretboard.PositionsFor` *(IN6, IN7, C5)*
5. **Golden octave-shape validation** — lattice reproduces all five octave-shape offsets (the first caged-system oracle slice) *(IN9)*
6. **Ref-sync** — domain-model + architecture references, same unit of work *(IN10)*

Every IN/C handle is covered; the dogfood UI stays in `ui/intervals-scales` (`EX6`).

**STOP — waiting for `go`** to start Step 1. (I'll do one step at a time and stop after each, per the contract — unless you tell me to run a range.)

## Rafa:

I started plan
Do whole plan

## AI:

✅ **Whole plan done — all 6 steps, full Core suite green (491 passed / 0 failed)**, including the `NetArchTest` Domain↛Instruments guard.

1. ✅ **Single-source tuning** — `Fretboard` now authors the octave-preserving `AbsoluteSemitone(string,fret)`; `PitchClassAt`/`PositionsFor` derive from it (the literal `OpenPitchClass` is gone). Test proves the derivation reproduces the old table for every (string, fret).
2. ✅ **`IntervalLattice` core** — `Absolute` (delegate) + signed `Distance` (B-string = 4, descending negative, two-octave = 24).
3. ✅ **Label views + `LatticeInterval`** — `PitchClassLabel` (1…7) and `Describe` (8/9/15 + octave + direction), both via `IntervalSpeller.Name`.
4. ✅ **`PositionsOfInterval` + `LabelAt`** — pitch-class + window on `Fretboard.PositionsFor`.
5. ✅ **Golden octave-shape oracle** — all five offsets (C −2, A +2, G −3 & 6→1 same fret, E +2 & 6→1 same fret, D +3) verified as same-pitch anchors. The lattice math reproduces the octave shapes. 🎯
6. ✅ **Ref-sync** — domain-model + architecture references updated in the same unit of work.

New files: `Geometry/IntervalLattice.cs`, `Geometry/LatticeInterval.cs`, `tests/IntervalLatticeTests.cs`, `tests/FretboardTuningTests.cs`. Done doc written. Plan status → **done**.

**Not committed yet** — say `commit` and I'll push. Next thread up the roadmap is **octave-shapes** (now a thin partition layer on this).