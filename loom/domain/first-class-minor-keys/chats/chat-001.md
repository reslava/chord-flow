---
type: chat
id: ch_01KXQP19EYCA02JS0NT5CA6SFS
title: first-class-minor-keys Chat 001
status: done
created: 2026-07-17
version: 1
tags: []
parent_id: null
requires_load: []
last_ai_block: 34
---
# first-class-minor-keys Chat 001

## Rafa:

read idea
is it all grounded for a design + req?

## AI:

Read the idea. Short answer: **yes for the design — not yet for the req**, and that ordering is the point.

**Grounded enough to open the design today:**
- The *problem* is nailed precisely — the double-shift is a real, correctly-diagnosed bug (a major-frame degree like `b7` gets re-shifted a second time through `Scale.ForKey`'s natural-minor offsets), not a vague "minor is flaky."
- The hard invariant is explicit: every existing major-authored progression stays byte-identical.
- Scope is in true dependency order (frame → spelling → UI → renderer) and the non-goals fence out harmonic/melodic minor cleanly.
- Validation names concrete golden cases.

That's everything a design needs to *start*.

**What gates everything downstream — the frame decision (A vs B).** This is genuinely open, and it's not a detail: it changes what a DSL degree *means*, so the req can't be written until it's chosen (design makes the reqs knowable — that's why design precedes req in our flow). Teeing it up:

- **A — major-relative (mode = spelling only):** one frame across the whole DSL. A degree token always means the same pitch; the `-`/quality suffix already supplies the ♭3, and degrees 1/2/4/5 are identical between the two scales — so a natural-minor i–iv–v authored `1- 4- 5-` already realizes correctly, and you only ever spell 3/6/7 with explicit accidentals (`b6`, `b7`), exactly as `chromatic-degrees` already does. A major V in a minor key is just `5`/`57` — the quality's major third *is* the raised leading tone, so we never need a harmonic-minor scale. Realization change ≈ nil (stop consulting `IsMinor` for offsets); the work concentrates in spelling.
- **B — natural-minor-relative:** bare `3/6/7` auto-mean `b3/b6/b7` in a minor key — more intuitive when authoring `1- 4- 5`. Cost: the same token means different pitches by mode, so *every* existing major-frame progression needs a guard against minor realization, and the DSL now carries two frames.

My lean is **A** — single-frame, least-churn, most durable, and the "you're authoring a minor tune relative to its relative major" objection mostly evaporates once you notice 1/2/4/5 are shared and the minor color comes from the *quality*, not the degree. But it's your call, and it's exactly the conversation the design should hold.

**Two smaller items for the design to pin (not blockers, but they belong in it):**
1. Raised-leading-tone spelling — the V's third in A minor must spell G♯, not A♭. Where does `NoteSpeller` get the "raise the 7 where it occurs" rule?
2. `\ks` for a minor tonic — do we emit the relative-major key signature (A minor → `\ks C`), or does alphaTab's `\ks` take a mode? Worth checking the alphatex ref before the renderer step.

Want me to open `design.md` framed around the A/B decision, or settle A vs B here first?

## Rafa:

I must confess I did not understand well the difference between you A and B. Reframe them briefly pls. Write down the nimor diatonic chords with both.

Let me think out loud with you and you reply me with your own ideas...

### (C) Use relative major and IsMinor flag

We could treat minor keys in the Music domain Engine just via using its relative major.
Instead of treating the minor tonic as `1` we treat the relative major key as the reference point.

If a song is in A Minor, the chart is written as if it were in C Major (the relative major), like we curenty do.
The chord that feels like "home" (A Minor) is notated as 6– (six minus), rather than 1–.
Example: A progression in A Minor (Am – Dm – E – Am) would be written as:
6– 2– 3 6– (in the context of C Major) 

Note: In this progresion example (Am – Dm – E – Am) I am using E instead of Em, the song uses a harmonic minor scale (common in rock and metal), the V chord is often major (E Major in A Minor). In this case, we write it as 3 instead of 3– 

Then we could use a king of Conversor Engine <-> Minor ???
6– 2– 3 6–  <-> 1– 4– 5 1–

major diatonic chords: 1 2- 3- 4 57 6- 7dim
starting from 6th we have minor diatonic chords: 6- 7dim 1 2- 3- 4 57 exactly the same, corresponding to
the diatonic chords follow the natural minor scale (I thin this is your A): 1–, 2dim, 3, 4–, 5–, 6, 77

Sorry for the mess!

Resuming we have:
1. A
2. B
3. write as relative major indicating `IsMinor`

Create a table with pros and cons
Tell me what you think

## AI:

No mess at all — you've actually put your finger on a real muddle *in the idea doc*, and clearing it up dissolves most of the confusion.

### First, the thing that's tripping us both up

The idea's option **A** secretly blends **two different ideas** in one bullet:
- "*Transposer keeps using major-scale offsets regardless of IsMinor*" (tonic stays `1`), **and**
- "*a minor tune is authored in its relative-major degrees*" (tonic becomes `6`).

Those aren't the same thing. So let me split A into its two honest halves and line them up with your (C). The real menu is three:

- **A1 — tonic-relative, major offsets.** Key = A minor, home = `1`. Root pitches always come from the **major** scale, so a minor third/sixth/seventh must be written with an explicit flat.
- **B — tonic-relative, minor offsets.** Key = A minor, home = `1`. Root pitches come from the **natural-minor** scale, so bare `3 6 7` already mean ♭III ♭VI ♭VII.
- **C (yours) — relative-major authoring + IsMinor + converter.** Store/author in C major, home = `6`, and a converter shows it back as tonic-relative.

**One correction that matters:** the natural-diatonic reading you wrote —

> 1–, 2dim, 3, 4–, 5–, 6, 7 … "I think this is your A"

— is actually **B**, not A. In A1 those same chords are `1- 2dim b3 4- 5- b6 b7`. That flip (bare `3 6 7` vs `b3 b6 b7`) *is* the whole A-vs-B decision. The idea doc mislabeled it, which is why it read as a mess.

#### The A minor diatonic chords, written three ways

Chords: Am · B° · C · Dm · Em · F · G  (i ii° III iv v VI VII)

| Chord | **A1** (major offsets) | **B** (minor offsets) | **C** (relative to C major) |
|-------|----|----|----|
| Am (i, home) | `1-` | `1-` | `6-` |
| B° (ii°) | `2dim` | `2dim` | `7dim` |
| C (III) | `b3` | `3` | `1` |
| Dm (iv) | `4-` | `4-` | `2-` |
| Em (v) | `5-` | `5-` | `3-` |
| F (VI) | `b6` | `6` | `4` |
| G (VII) | `b7` | `7` | `5` |

(In all three the *quality* suffix is always explicit — only the **root offset** differs. Notice `1 2 4 5` are identical between the scales, so only `3 6 7` are in play.)

### Where your (C) actually lands

Your converter maps `6- 2- 3 6-` ↔ `1- 4- 5 1-`. Look at the right-hand side: `1- 4- 5` **is A1's authoring**. So C = *store as relative-major, but let the user read/write as A1 via a converter.* Which means the converter only exists to reconcile two representations — and we have **no legacy minor content** to preserve, so storing relative-major buys nothing. The good instinct hiding in C is real though: **home should read as `1`, not `6`** — and A1 gives you that directly, no converter.

### Pros / cons

| | **A1** (major offsets) | **B** (minor offsets) | **C** (relative + converter) |
|---|---|---|---|
| Home (i) reads as | `1-` ✅ | `1-` ✅ | `6-` ✋ (home isn't 1) |
| III / VI / VII | `b3 b6 b7` (must flat) | `3 6 7` (bare, intuitive) ✅ | `1 4 5` |
| Frames in the kernel | **one**, absolute ✅ | **two** (a degree changes pitch by mode) ✋ | one stored + a converter view ✋ |
| Transposer change | drop IsMinor from root offsets | keep the mode branch | none |
| Existing major songs | byte-identical, **nothing to guard** ✅ | must guard against minor realization ✋ | byte-identical |
| Double-shift bug | **impossible by construction** ✅ | possible on a frame mismatch ✋ | impossible |
| `Key` object | honest `Key(A, minor)` ✅ | honest `Key(A, minor)` ✅ | tonic A is lost / degree ≠ tonic ✋ |
| Minor-author feel | slightly annoying (flats) ✋ | reads like the minor scale ✅ | home at `6` is unintuitive ✋ |
| Extra machinery | none ✅ | mode-aware degree consumers | a whole converter layer ✋ |

### What I think

**A1**, and I'd frame the whole thread around it:

1. **One absolute degree frame (major offsets). Mode lives on the `Key`, used only for *spelling* — the relative-major key signature (A minor → `\ks C`) and raised leading tones — and for the UI picker.** That's literally what "first-class minor keys" should mean: `Key(A, minor)` stays truthful, the double-shift becomes *impossible* rather than *guarded against*, and every existing song is untouched with nothing new to reason about. It's the most coherent kernel, which is the priority.

2. **B's one advantage — bare `3 6 7` — we can still get later, additively, without B's cost.** Add an optional *minor-relative input mode*: a pure parse-time sugar that lets you type `1- 2dim 3 4- 5- 6 7` and expands to A1's `b3 b6 b7` before realization. That's your converter (C's instinct) — but as a front-end that *produces* A1, not a second realization frame. Ship it only if authoring ever feels annoying.

3. Your **E-major-V** example is the clincher for keeping the scale question simple: the raised leading tone rides on the **quality** (`5`/`57` in A1, `3` in C), not the scale — so *none* of the three frames needs harmonic minor. The less we lean on the scale for offsets, the better → A1.

So: pick **A1** as the kernel truth; treat B as optional future sugar; retire C's converter as unnecessary. If you're good with that, I'll write `design.md` around it (frame = A1, mode-on-Key-for-spelling, the sugar noted as a non-goal-for-now). Want to lock A1, or poke at it more first?

## Rafa:

Yes, now with your fixed A, `A1 — tonic-relative, major offsets` and explain it better I understand well and like it, go with A1

### AlphaTes

Admit minor Keys:

```
\ks key

Description: Specifies the key signature for this and subsequent bars.

Parameters:

Name	Description	Type	Required
key	The key signature	Ident	yes

Example:

\ks Cb | \ks C | \ks C# |
\ks Aminor | \ks Dmajor | \ks Bminor
```

## AI:

**A1 is locked.** ✅ On the record so the design cites it: *one absolute degree frame — degree roots always resolve through the **major** scale offsets, regardless of mode. `Key.IsMinor` is a **spelling + UI** property, never a realization-offset one. Home is `1-`; `b3 b6 b7` are written explicitly; the double-shift is impossible by construction.*

### The `\ks Aminor` find is great — it kills my earlier guess

I'd guessed we'd emit the **relative-major** signature (A minor → `\ks C`). Your docs show alphaTab takes the mode **natively** — `\ks Aminor`, `\ks Bminor`, `{Note}{major|minor}`, no space. That's strictly better and more honest, so:

- `NoteSpeller.KeySignatureToken(key)` emits `{tonicLetter}minor` for a minor key (e.g. `Aminor`) instead of the relative major. One small branch on `IsMinor`.
- We must update `alphatex-syntax-reference.md` (it currently only documents major `\ks`) in the same unit of work when we implement — captured for the plan.

### That also closes both of my open spelling questions

1. **Raised leading tone (G♯ in A minor).** With `\ks Aminor` set and the *real* pitches fed in, alphaTab draws the accidental itself — nothing for us to do on the staff. When the raised 7 is a chord **root** (a `G#dim7` = `#7dim7`), the existing **`Chord.RootSpelling`** machinery from `chromatic-degrees` already gives it the letter-pure `G♯`. No new spelling mechanism.
2. **Diatonic note spelling in a minor key.** One rule: *a minor key spells its diatonic pitch classes from its relative major's table* (A minor ⇒ C's all-naturals). That's the only `NoteSpeller` change beyond the `\ks` token.

### So the design is fully settled — here's the skeleton I'd write

- **Frame:** A1. `Transposer` resolves degree roots against **major** offsets always. Precise surgical point: `Transposer` stops routing root resolution through `Scale.ForKey` (which branches on `IsMinor`) and uses the major scale — **but `Scale.ForKey` itself stays unchanged**, because `HarmonicAnalyzer` legitimately wants natural-minor for its diatonic classification. We only change what *Transposer* uses for roots, not the shared `Scale.ForKey`.
- **Spelling:** `\ks {tonic}minor`; minor-key note spelling via the relative-major table; chromatic roots keep `RootSpelling`.
- **UI:** the Key picker (HarmonyControlsR) offers minor keys; the render path already carries the key.
- **Non-goal (explicit):** the "minor-relative input sugar" (bare `3 6 7`) — captured as a future additive parser front-end, not built now.
- **Invariant / golden tests:** every existing major progression byte-identical; plus new goldens for a natural-minor `i–iv–v` and a `iiø–V–i` realized in a minor key (A1 output).

Shall I write `design.md` around this? Then req → plan.

## Rafa:

write design + req

## AI:

Done — both drafts are written:

- **`design.md`** (`de_01KXQW333KYNQCH1P71VB9E35P`) — the A1 decision + why-over-B/C, realization mechanics (Transposer→major offsets, `Scale.ForKey` left alone), spelling/renderer (`\ks {tonic}minor` + relative-major table + `RootSpelling`), UI, non-goals, and the golden-test set.
- **`req.md`** (`rq_01KXQW3WQJSZWKH8A3H9YN7DYB`, status **draft**) — 7 Included (`IN1`–`IN7`), 3 Excluded (`EX1`–`EX3`), 4 Constraints (`C1`–`C4`).

Quick map of the req handles so the plan can cite them:
- `IN1` A1 frame · `IN2` Transposer→major roots · `IN3` `\ks {tonic}minor` · `IN4` relative-major spelling · `IN5` UI key picker · `IN6` alphatex ref sync · `IN7` goldens.
- `C1` major regression invariant · `C2` no new scale for the leading tone · `C3` `Scale.ForKey` unchanged · `C4` chromatic roots keep `RootSpelling`.

Give them a read. When you're happy, next actions are: **lock `req.md`** (`finalize_req`) and **mark idea + design done**, then I draft the plan citing IN/C. Want me to lock + mark done and go to the plan, or do you want to review/tweak the req first?

## Rafa:

I set req locked and idea + design done
write plan

## AI:

Plan written — **`plan-001.md`** (`pl_01KXQWH3EHRG7JYEG7D5XREV8T`), 5 steps, and `verify_req` is **structurally clean** (no uncovered Included, no stray citations; the semantic AI pass is skipped in a CLI session, so I eyeballed it — every IN is cited by an advancing step and nothing steps on an EX/C).

| # | Step | Satisfies | Blocked by |
|---|------|-----------|-----------|
| 1 | `Transposer` realizes minor via **major offsets** (the A1 core) | IN1 IN2 · C1 C2 C3 | — |
| 2 | Realization goldens (natural-minor `i–iv–v`, `iiø–V–i`; major unchanged) | IN7 · C1 C2 | 1 |
| 3 | `\ks {tonic}minor` token + round-trip inverse + relative-major spelling | IN3 IN4 IN6 · C4 | — |
| 4 | Renderer/spelling golden for a minor tune (raised-7 root ⇒ G♯) | IN7 · C1 | 3 |
| 5 | UI offers minor keys — thread `isMinor` end-to-end | IN5 | 1, 3 |

Shape of the work: **step 1 is tiny** — swap `Scale.ForKey(key)` → `Scale.Major(key.Tonic)` at Transposer's two key-taking entry points (line 67 + 97). That single change *is* A1: major keys stay byte-identical (`ForKey` already returns Major there), minor keys stop double-shifting. Two nice findings while scoping:
- **IN4 is basically already done** — `NoteSpeller.UsesSharps` already maps a minor key to its relative major (tonic + 3), so diatonic notes already spell right. Step 3 just adds a confirming test, plus the `\ks` suffix and its inverse (`KeyFromSignatureToken` currently hardcodes `IsMinor: false`).
- **Step 5 is the real weight** — the key is a bare `keyPitchClass` today (`new Key(pc, false)` everywhere), so minor means threading an `isMinor` flag through the JS controls → bridge DTOs → `GenerateExercise`/`ContentCrud`. Defaulting absent ⇒ `false` keeps every current flow untouched.

Ref updates are folded into the steps that touch each area (domain-model + DSL on step 1, alphatex on step 3), per the same-unit-of-work rule.

Ready to implement. Say `do step 1` (or `do plan`) when you want me to start.

## Rafa:

I started plan
do steps 1-4

## AI:

Steps 1–3 ✅ and step 4 is 90% done — **all 963 Core tests green**. But finishing step 4's renderer golden surfaced a real nuance that trips over a line **I wrote wrong in req IN7**, so I'm stopping before I finalize it (rule 3 — a spec/test-design decision).

**Done so far**
- **Step 1** — `Transposer` resolves roots via `Scale.Major(key.Tonic)` (the A1 core; two-line change). Major output byte-identical.
- **Step 2** — realization goldens (`1- 4- 5-` → Am/Dm/Em; `2ø 57 1-` → Bm7b5/E7/Am; replaced the stale test that pinned the old double-shift).
- **Step 3** — `\ks aminor` token + `minor`-suffix round-trip; confirmed IN4 spelling already works via `UsesSharps`.
- **Step 4 (partial)** — removed the renderer's `EnsureMajorSupported` guard (it threw `NotSupportedException` on any minor key — the MVP wall this thread exists to knock down); rewrote the old `Render_MinorKey_Throws` test into a passing `\ks aminor` render golden.

**The nuance — the raised leading tone as a chord *root***

My IN7 said *"a raised-7-rooted chord spells `G♯`"* and I'd sketched it as `#7dim7`. Both are wrong, and here's why it matters:

- In **A1**, the major-frame degree 7 of A **is already G♯** (the major scale's 7th is the leading tone). So a vii°7 in A minor is authored bare `7dim7`, not `#7dim7`.
- A **bare** (non-accidental) degree gets **no** letter-pure `RootSpelling` — so its root falls to the key table, and A minor's table is its relative major's (**C = flats**). Result: `7dim7` → root spelled **A♭°7**, not G♯°7.
- **Everything else spells correctly.** All seven natural-minor diatonic chords — `1- 2dim b3 4- 5- b6 b7` → Am B° C Dm Em F G — are right. The **V chord is fine too**: `57` → **E7**, root E is diatonic; its G♯ is an *internal* tone that alphaTab spells from `\ks aminor` on the staff. The *only* gap is a chord literally **rooted** on the raised 7 — which is a **harmonic-minor** chord, and the design's non-goal already fences out "harmonic modes beyond natural minor in v1."

So the in-scope natural-minor render is complete and green; the "G♯ root label" was me over-reaching IN7 past the natural-minor scope the design set.

**My recommendation** — option **(a)**:
1. **Amend IN7** to drop the raised-7-root spelling claim and assert the in-scope natural-minor render (already passing).
2. **Capture a follow-up idea** — "raised leading-tone spelling (harmonic-minor chord labels)" — where a bare degree-7 root (and eventually the melodic-minor raised 6) spells as a sharp against a minor key. That's a focused spelling thread, not this one.

Option (b) would be to fix leading-tone spelling *now*, but it's broader spelling work (risk of major-key regressions) and belongs in its own thread per the non-goal.

Amending a **locked** req and creating a follow-up are state actions I won't take unasked. Which way — (a) amend IN7 + capture the follow-up, or (b) pull the spelling fix into this thread? Step 5 (UI) is independent of this and ready either way.

## Rafa:

I think this is not good to skip and go forward. If we go forward we are just delaying the issues, and later will be bigger.
Sometimes you explain barely the concepts or problems and It is hard for me to understand
I can not see what you were doing

First let me write down the concepts becuase my memory is not like yours

### Theory
Key A minor *

1. Chords qualities of the Natural Minor:
            i - ii° - III - iv - v - VI - VII
triads:     Am - Bdim - C - Dm - Em - F - G
sevenths:   Am7 - Bm7b5 - Cmaj7 - Dm7 - Em7 - Fmaj7 - G7

2. Chords qualities of the Melodic Minor scale:
          i - ii - III+ - IV - V - vi° - vii°
triads:   Am - Bm - C+ - D - E - F#dim - G#dim
sevenths: AmMaj7 - Bm7 - Cmaj7#5 - D7 - E7 - F#m7b5 - G#m7b5

3. Chords qualities of the Harmonic Minor scale:
          i - ii° - III+ - iv - V - VI - vii°
triads:   Am - Bdim - C+ - Dm - E - F - G#dim
sevenths: AmMaj7 - Bm7b5 - Cmaj7#5 - Dm7 - E7 - Fmaj7 - G#dim7

### A1 vs C+converter

Let me come back to review previous forks
I am realazing that maybe you overstimnated A1 and understimated C+converter.
For C+converter you analysed C, but did not included the converter as a function block. Let's include it...

### The A minor diatonic chords, written three ways

Chords: Am · B° · C · Dm · Em · F · G  (i ii° III iv v VI VII)

Your previous analysis also did not include the converter like a block. New last column.

| Chord | **A1** (major offsets) | **B** (minor offsets) | **C** (relative to C major) | **C** (relative to C major + converter) |
|-------|----|----|----|----|
| Am (i, home) | `1-` | `1-` | `6-` | `1-` |
| B° (ii°) | `2dim` | `2dim` | `7dim` | `2dim`  |
| C (III) | `b3` | `3` | `1` | `3`  |
| Dm (iv) | `4-` | `4-` | `2-` | `4-`  |
| Em (v) | `5-` | `5-` | `3-` | `5-`  |
| F (VI) | `b6` | `6` | `4` | `6`  |
| G (VII) | `b7` | `7` | `5` | `7`  |


### Pros / cons

Your previous analysis also did not include the converter like a block. New last column.

| | **A1** (major offsets) | **B** (minor offsets) | **C** (relative) | **C** (relative + converter) |
|---|---|---|---|---|
| Home (i) reads as | `1-` ✅ | `1-` ✅ | `6-` ✋ (home isn't 1) | `1-` ✅ |
| III / VI / VII | `b3 b6 b7` (must flat) | `3 6 7` (bare, intuitive) ✅ | `1 4 5` | `3 6 7` (bare, intuitive) ✅  |
| Frames in the kernel | **one**, absolute ✅ | **two** (a degree changes pitch by mode) ✋ | one stored + a converter view ✋ | ? |
| Transposer change | drop IsMinor from root offsets | keep the mode branch | none | ? |
| Existing major songs | byte-identical, **nothing to guard** ✅ | must guard against minor realization ✋ | byte-identical | ?  |
| Double-shift bug | **impossible by construction** ✅ | possible on a frame mismatch ✋ | impossible | ?  |
| `Key` object | honest `Key(A, minor)` ✅ | honest `Key(A, minor)` ✅ | tonic A is lost / degree ≠ tonic ✋ | honest `Key(A, minor)` ✅  |
| Minor-author feel | slightly annoying (flats) ✋ | reads like the minor scale ✅ | home at `6` is unintuitive ✋ | reads like the minor scale ✅  |
| Extra machinery | none ✅ | mode-aware degree consumers | a whole converter layer ✋ | a very simple converter |

### Results

C+Converter (general) = B (natural minor specific)

B is designed for natural minor
C+Converter is just every scale major or minor expressed as relative to its major+small converter -> work with every scale, chords derived
But I think that C+Converter could solve the harmonic and melodic minor issues
Could you do the analysis for them? this is too exhausting

Sorry if I am wrong, I think we need to be sure of this IMPORTANT decision before going forward.

## AI:

You're right to stop, and thank you for pushing — this is the load-bearing decision and getting it wrong compounds. Your meta-point is fair too: I compressed too much. I'll show the full work here, and there's a **"what I changed in code"** list at the end so you can see exactly what steps 1–4 did.

**Your three theory tables are correct.** I checked natural, melodic, and harmonic minor chord-by-chord — no errors. Good reference to anchor on.

---

## 1. C+converter as a real block (you're right — I never defined it properly)

The trick is to separate **three things** that I'd been mushing together:

- **Storage frame** — what the `.dsl` actually stores. C+converter stores everything in the **parent major** (the major key that shares the key signature). A *major* song stores exactly as today. An *A-minor* song stores relative to **C major**: its `i` is stored `6-`, its natural `♭III` is `1`, its leading-tone chord is `#5…`.
- **The converter** — a tiny pure function on degree tokens, a fixed rotation `1↔6 2↔7 3↔1 4↔2 5↔3 6↔4 7↔5` (accidentals pass straight through). It sits at the **edges**: at authoring it turns what you type (tonic-relative minor, `1-`=i, `3`=♭III, `#7`=leading tone) into the stored parent-major form; at display it can turn it back for showing Nashville numbers.
- **The kernel** (Transposer, analysis, transforms, renderer) — sees **only** the one parent-major absolute frame, identical to today's major path. A minor song realizes its parent-major degrees against the chosen minor key's relative major, and spells with the minor key. `Key(A, minor)` stays honest.

So: **to the author, C+converter and B are the *same DSL*** — you type tonic-relative minor either way (`1- 2dim 3 4- 5- 6 7`, and `#7`/`#6` for the raised tones). They differ only under the hood: **B** puts the mode branch *inside* the Transposer; **C+converter** normalizes to one absolute frame *before* the kernel, so the kernel never branches.

---

## 2. The decisive analysis you asked for — spelling harmonic/melodic tones

The chromatic notes that appear beyond natural minor in A minor are **G♯** (raised 7 — in V, vii°, III+, melodic/harmonic) and **F♯** (raised 6 — melodic IV, ii, vi°). The spelling problem only bites when one is a chord **root**: `G♯dim7` (vii°7) and `F♯m7♭5` (vi°). Here's each frame, worked through:

**vii°7 = G♯dim7 (root G♯, pitch class 8):**

| Frame | You author | Stored degree | Why it spells that way | Result |
|---|---|---|---|---|
| **A1** | `7dim7` | degree 7, **A-major** offsets, **bare** | bare degree ⇒ no letter-pure `RootSpelling` ⇒ falls to the key's flat table | **A♭°7** ✗ |
| **B** | `#7dim7` | degree 7 **♯**, A-natural-minor offsets | accidental ⇒ `RootSpelling`: letter G, +1 | **G♯°7** ✓ |
| **C+conv** | `#7dim7` → `#5dim7` | degree 5 **♯**, C-major offsets | accidental ⇒ `RootSpelling`: letter G, +1 | **G♯°7** ✓ |

**vi° = F♯m7♭5 (root F♯, pitch class 6):**

| A1 | `6ø` | degree 6, A-major, **bare** | flat table | **G♭m7♭5** ✗ |
| B | `#6ø` | degree 6 ♯, A-natural-minor | `RootSpelling`: F, +1 | **F♯m7♭5** ✓ |
| C+conv | `#6ø` → `#4ø` | degree 4 ♯, C-major | `RootSpelling`: F, +1 | **F♯m7♭5** ✓ |

**The key insight — A1 is the odd one out, and here's *why*:** A1's frame is the **parallel major** (A major). In that frame the *raised* tones G♯/F♯ happen to be A-major's own diatonic 6 and 7 — so they're written **bare**, and a bare degree gets no letter-pure spelling, so it collapses to the flat table (A♭, G♭). Meanwhile A1 makes the *natural-minor* tones the "chromatic" ones needing flats (`b3 b6 b7`). **A1 optimizes the wrong direction.**

B and C+converter anchor the frame at the **natural minor / relative major** instead. There the natural-minor tones are bare-and-correct (G, F from the plain table) and the raised harmonic/melodic tones are **accidental'd degrees** (`#6`/`#7` → `#4`/`#5`) — which spell letter-pure *by construction*, reusing the exact `chromatic-degrees` `RootSpelling` machinery, **zero new heuristics.** That's your intuition, confirmed: C+converter (and B) solve harmonic/melodic; A1 doesn't.

---

## 3. The table, with the ? filled and the row that actually decides it

| | **A1** (parallel-major) | **B** (natural-minor offsets) | **C+converter** (parent-major + converter) |
|---|---|---|---|
| Author's DSL for i | `1-` ✅ | `1-` ✅ | `1-` ✅ |
| Natural ♭III/♭VI/♭VII | `b3 b6 b7` (must flat) ✋ | `3 6 7` (bare) ✅ | `3 6 7` (bare) ✅ |
| **Harmonic/melodic roots (G♯, F♯)** | **mis-spell A♭/G♭** ✗ | **correct** ✅ | **correct** ✅ |
| Frames the **kernel** sees | one ✅ | **two** (Transposer branches on mode) ✋ | **one** ✅ |
| Mode-handling lives… | nowhere (ignored) | in the Transposer | in a parse-time converter |
| Existing major songs | byte-identical ✅ | byte-identical (major-home) ✅ | byte-identical ✅ |
| Double-shift | impossible ✅ | needs a home-mode tag to avoid ✋ | impossible ✅ |
| `Key` object | honest ✅ | honest ✅ | honest ✅ |
| Generalizes to other modes | no | per-mode offset table in kernel ✋ | **yes — same one frame + a converter rotation** ✅ |
| Extra machinery | none ✅ | one Transposer branch | a small converter + author-form ≠ stored-form |

---

## 4. My honest, revised conclusion

**You've convinced me: A1 is the wrong pick.** It's the *simplest kernel*, but that simplicity buys worse minor behavior on *both* axes — you must flat the natural degrees, *and* the harmonic/melodic roots mis-spell. Under "correct and durable over minimal," A1 loses.

The real fork is now **B vs C+converter**, and — as you said — they're the *same thing to the author*. They differ only in the engine:

- **C+converter** keeps the kernel **single-frame** (its north-star), and is a **uniform mechanism**: *every* scale/mode is stored in its parent major + a converter rotation, so new modes (Dorian, etc.) need **no new kernel offset tables** — just a different rotation. Chromatic tones spell correctly because the stored frame *is* the notational (key-signature) frame. Cost: a small converter, and stored-form ≠ authored-form.
- **B** is simpler to build (one branch in the Transposer, authored-form = stored-form), same user experience, correct spelling — but it bakes mode into the kernel and would need a new offset table per future mode.

**I recommend C+converter.** It's more machinery today, but it's the uniform, single-frame, notation-honest foundation — the kind you've said you optimize for. B is the reasonable "less code now" alternative if you'd rather not add the converter/dual-representation yet.

**Rework cost is small — we're only at steps 1–4 and it's all reversible.** In fact B is *closer to the original code* than A1: the pre-step-1 Transposer already realized minor keys with natural-minor offsets (essentially B without a home-mode tag). My step-1 change moved *away* from B toward A1. So pivoting means reworking step 1 and the step 2–4 tests — an hour or two, not a rebuild.

---

## What I actually changed in steps 1–4 (so you can see it)

All recorded in `plans/.../done/plan-001-done.md`, but in brief:
- **Step 1** — `Transposer.cs`: two lines, `Scale.ForKey(key)` → `Scale.Major(key.Tonic)` (this *is* A1; it's what we'd revert).
- **Step 2** — `TransposerTests.cs`: added A1 minor goldens; replaced the old test that pinned the natural-minor behavior.
- **Step 3** — `NoteSpeller.cs`: `\ks` gains a `minor` suffix + round-trip inverse (this part is **frame-independent — we keep it** whichever way we go).
- **Step 4** — `AlphaTexRenderer.cs`: removed the `EnsureMajorSupported` guard that *threw* on any minor key (**keep this too**), and turned the old "minor throws" test into a passing minor-render test.

So step 3 and the guard removal survive any choice; only the frame (step 1) and its goldens change.

**This is your call — B or C+converter?** Take your time; I won't touch code until you pick. If you want, I can also sketch the concrete converter function and the `Progression` home-mode field for C+converter before you decide.

## Rafa:

Thank you for the analysis, I need time to read and digest, I have a slow mind ;-)

To simplify lets call from now on `C` = `C + converter`

Sketch the concrete converter function and the `Progression` home-mode field for `C` 
Could you do add in the analysis some scales/modes `B` vs `C`, mixolydian, dorian, melodic minor, harmonic minor?

## AI:

No rush at all — and "slow mind" nothing; you caught a real flaw in my reasoning. Here's the sketch and the mode analysis. From here `C` = C+converter.

---

## A. Concrete sketch of `C`

### A.1 — The home-mode field on `Progression`

```csharp
// v1 ships Major + Minor. The 5 remaining diatonic modes are the growth path;
// harmonic/melodic minor need NO new value — they're Minor + accidental'd degrees (see §B).
public enum Tonality { Major, Minor /*, Dorian, Phrygian, Lydian, Mixolydian, Locrian */ }

public sealed record Progression(
    string Id, string Name, IReadOnlyList<HarmonicBar> Bars,
    Tonality Home = Tonality.Major);   // default Major ⇒ every existing progression unchanged
```

Two representations, and it's worth being precise about which is which:
- **The `.dsl` text stays in the author's frame** (tonic-relative: a minor tune reads `1- 2dim 3 4- 5- 6 7`, plus `#7`/`#6` for raised tones) — readable, this is what you type and what's stored on disk.
- **`Progression.Bars` holds the *parent-major* degrees** (post-converter) — the single absolute frame the kernel understands.

The converter is the bridge between the two.

### A.2 — The converter (one small pure function)

```csharp
// Each mode's tonic sits on a fixed degree of its PARENT major (the major sharing its key
// signature): Ionian→1, Dorian→2, Phrygian→3, Lydian→4, Mixolydian→5, Aeolian(minor)→6, Locrian→7.
static int ParentPosition(Tonality m) => m switch { Tonality.Minor => 6, _ => 1 };

// author-frame degree  ->  parent-major degree   (accidental carries through UNCHANGED)
static RomanDegree ToParent(RomanDegree d, Tonality m)
{
    int k = ParentPosition(m);                       // Minor: 6
    int p = ((d.Degree - 1 + (k - 1)) % 7) + 1;      // Minor: 1→6 2→7 3→1 4→2 5→3 6→4 7→5
    return d with { Degree = p };                    // Accidental (# / b) unchanged
}
static RomanDegree ToAuthor(RomanDegree d, Tonality m)   // inverse, for display
{
    int k = ParentPosition(m);
    int a = ((d.Degree - 1 - (k - 1) + 7) % 7) + 1;
    return d with { Degree = a };
}
```

- **Parse** (`ProgressionParser`, given the `Home`): each token → `ToParent`. So `1-`(minor) → `6-`; `3`(minor) → `1`; `#7`(minor) → `#5`.
- **Display** (editor / Nashville view): `ToAuthor` turns the stored parent-major degrees back into what you typed.
- **Accidentals pass straight through** because the author-degree and its parent-degree are the *same physical scale note* (they share the key signature) — a `#`/`b` raises/lowers that one note identically. (Checked: minor `#7`=G♯ ↔ parent `#5`=G♯; minor `b2`=B♭ ↔ parent `b7`=B♭.)

### A.3 — The kernel change is as small as A1's

Because `Bars` are already parent-major, `Transposer` just realizes them against the key's **parent major** instead of the key's tonic:

```csharp
static PitchClass ParentTonic(Key key) =>              // Major: tonic;  Minor: tonic + 3 (relative major)
    new(Mod12(key.Tonic.Value - Scale.MajorIntervals[ParentPosition(key.Mode) - 1]));

// in Realize / RealizeBars:
Scale scale = Scale.Major(ParentTonic(key));           // instead of Scale.Major(key.Tonic)
```

That's it for the kernel — one helper. All the mode logic lives in the **converter** at the parse/display edges; the Transposer, analyzer, transforms, and renderer never branch on mode. `Key(A, minor)` stays honest and drives both the parent major (for realization) and the spelling.

### A.4 — Round-trip, A minor natural

```
You type (A-minor frame):   1-   2dim   3    4-   5-   6    7      (+ #7dim7 for the vii°7)
  --ToParent (Minor)-->      6-   7dim   1    2-   3-   4    5      (+ #5dim7)   ← stored in Bars
  --realize in A minor (parent major = C)-->
                             Am   B°     C    Dm   Em   F    G      (+ G#dim7, spelled via RootSpelling)
  --ToAuthor (Minor)-->      1-   2dim   3    4-   5-   6    7      ← shown back exactly as typed
```

---

## B. `B` vs `C` across the modes you named

The single question that separates them: **does the kernel need a new scale table (and mode-aware spelling) per mode, or not?**

The 5 non-Ionian **diatonic** modes (dorian, mixolydian, …) are just *rotations of a major scale* — every note is diatonic to a parent major. Melodic/harmonic minor are **not** rotations (they add raised tones), so their characteristic notes are accidentals against the parent major.

| Mode | Rotation of a major? | **B** (kernel) | **C** (kernel) | Spelling of the characteristic tone |
|---|---|---|---|---|
| **Mixolydian** (♭7) | yes (tonic = parent's 5th) | needs a mixolydian scale table + branch | just rotation `+4`; **major scale reused** | ♭7 is *diatonic to the parent* → **bare, correct** in both |
| **Dorian** (♭3, ♮6) | yes (tonic = parent's 2nd) | needs a dorian scale table + branch | rotation `+1`; major scale reused | ♮6/♭3 diatonic to parent → **bare, correct** in both |
| **Harmonic minor** (♯7) | **no** | scale table with ♯7 baked in ⇒ **♯7 is *bare* ⇒ mis-spells A♭** (the A1 trap again), *or* fall back to "natural minor + accidental" | Minor home + `#7`→`#5`, **accidental'd** ⇒ RootSpelling ⇒ **G♯ ✓** | **C correct; B correct only if it doesn't table the scale** |
| **Melodic minor** (♯6, ♯7) | **no** | scale table with ♯6/♯7 baked in ⇒ **bare ⇒ mis-spells G♭/A♭**, *or* natural-minor + accidentals | Minor home + `#6`,`#7` → `#4`,`#5`, **accidental'd** ⇒ **F♯, G♯ ✓** | **C correct; B correct only if it doesn't table the scale** |

**Two worked examples:**

*Mixolydian is free in C.* Author G-mixolydian tonic-relative `1 2 3 4 5 6 7`. The ♭7 is degree `7` → `ToParent(Mixolydian)` (rotation +4) → parent degree `4`. Realized in G mixolydian (parent major = C): C-major's degree 4 = **F** (bare, natural — because G-mixolydian's ♭7 *is* C's diatonic F). Correct, no accidental, no new scale table. **B would need a whole mixolydian offset table in the kernel** to get the same F.

*Melodic minor exposes B's real weakness.* A-melodic-minor's raised 6 and 7 are F♯ and G♯. In **C** they're authored `#6`/`#7` → `#4`/`#5` → accidental'd → RootSpelling → **F♯, G♯** ✓. In **B**, if you model melodic minor as its own scale (offsets with the raised 6,7 built in), those tones become *bare* diatonic degrees → they fall to the flat table → **G♭, A♭** ✗ — the exact bug A1 has. B only avoids it by *not* tabling melodic minor and instead treating it as "natural minor + `#6`/`#7`" — at which point B is just doing C's thing without the parent-major normalization.

**The pattern:** `C` needs **one** frame (the major scale) for *every* mode — diatonic modes are a rotation, altered modes are a rotation + accidental'd degrees. The kernel never grows a scale table, and spelling is always right (diatonic tones bare via the parent table, chromatic tones accidental'd via `RootSpelling`). `B` needs a scale table + mode-aware spelling per mode, and re-hits the bare-tone mis-spelling on every altered scale.

**Nice consequence for `C`:** you never need a "harmonic minor" or "melodic minor" *mode* at all — they're just **Minor + `#7`** and **Minor + `#6`/`#7`**. Fewer concepts, and they compose.

---

## Bottom line

- If minor really is the end of the road → `B` is a fine, smaller build.
- If modes/altered scales are coming (your questions suggest they are, and it fits the interval-engine direction) → **`C` is clearly the durable foundation**: one kernel frame forever, spelling correct by construction, new modes are data (a rotation), not kernel code.

My recommendation stays `C`. Digest at your pace — happy to go deeper on any part.

## Rafa:

I think the analysis probes `C` is the way to follow. Simple, complete, allow all scales/modes easily in.
go

## AI:

Locked in **C** and realigned the docs:

- **`design.md`** rewritten — C is the decision; A1 recorded as explored-and-reversed with the reason (parallel-major frame mis-spells harmonic/melodic roots). Includes the converter + `Home` sketch and the modes section.
- **`req.md`** amended → **v2, re-locked.** `IN1`/`IN2`/`IN7` rewritten for C; `IN8` (`Progression.Home`), `IN9` (the converter), `IN10` (parser applies it) appended; `EX1` (the old "sugar excluded") marked `~dropped` since the sugar *is* the design now; `EX3`/`EX4` reframed; `C2` → "single-frame kernel, no per-mode scale table."
- **`plan-001.md`** restructured — 8 steps, `verify_req` clean.

**The plan now:**

| # | Step | Status |
|---|------|--------|
| 1 | Transposer → A1 (`Scale.Major(tonic)`) | ✅ *(reverted by step 4)* |
| 2 | A1 realization goldens | ✅ *(replaced by step 6)* |
| 3 | `\ks {tonic}minor` token + inverse | ✅ *(survives)* |
| 4 | **C pivot** — parent-major realization + `Progression.Home` + the `ToParent`/`ToAuthor` converter | 🔳 |
| 5 | Parser applies the converter at parse (`.dsl` stays author-frame) | 🔳 |
| 6 | C goldens — round-trip; natural minor; **harmonic vii°7→G♯, melodic vi°→F♯** | 🔳 |
| 7 | Renderer golden (minor `\ks` + spelling) | 🔳 |
| 8 | UI offers minor keys — thread the mode end-to-end | 🔳 |

I kept steps 1–3 as honest history (we did A1, then pivoted); step 4 reverts the A1 line and the renderer guard-removal from step 4-old survives. Right now the tree is still the A1 code + `\ks`, all 963 tests green.

**One sub-decision waiting in step 5** (flagging early): *how a progression declares its `Home` mode.* A progression is key-independent and reusable, so a minor tune needs its mode stored on the definition itself — likely a tag in the progression/catalog DSL (and/or inferred from a Song's `key Am`). I'll bring options when we reach it; it doesn't block step 4.

Ready to implement. Say `do step 4` (the C pivot) or `do steps 4-6` (pivot + parser + goldens) to run the kernel work in one go — I'd stop before step 5's Home-declaration decision if you prefer, but 4→6 hangs together. Your call.

## Rafa:

Well done, req, design and plan updated!
note: maybe some source file was touched just for old A1 and you could do `git restore` ro start `C` from fresh code, decide as you consider

On sub-decision waiting in step 5, yes, looks likely progressions will need a tag in the progression/catalog DSL (and/or inferred from a Song's `key Am`). Show me the options when you arrive there, thanks for pre flag it

do 4-6, but stop for the flag

## AI:

**Step 4 (the C pivot) is done — 981 tests green.** I `git restore`d the A1 Transposer first, then built the C kernel cleanly:
- `DegreeFrameConverter` (`Tonality` + `ParentTonic`/`ToParent`/`ToAuthor`), `Progression.Home` (default `Major`), and `Transposer` now realizes against the parent major. The key fix: `SpellRoot` counts letters from the **parent** tonic, so `#5dim7` in A minor spells **G♯** (the whole payoff). Tests prove harmonic vii°7→G♯ and melodic vi°→F♯. Converter round-trip covered.

Now the **step-5 flag: how does a progression declare its `Home` mode?** A progression is *key-independent but not mode-independent* — `1- 4- 5-` only means "i iv v" if something says "minor." Three ways to supply that:

**Option 1 — a `tonality:` catalog-header key (recommended).** Progressions already carry a self-describing header (`genre:`, `tags:`, …). Add one recognized key:
```
genre: Jazz
tonality: minor
1- 2ø 5 1-          ← authored tonic-relative minor: i iiø V i
```
`CatalogHeader` gains `tonality`; the entity layer passes it into `ProgressionParser.Parse(body, ts, home)`, which runs `ToParent`. The DSL body stays metadata-unaware (its existing constraint) — it just gets a mode *param*. **A standalone progression is self-describing** (renders right in the Content preview with no song). Absent ⇒ `Major`, so every existing `.dsl` is untouched.

**Option 2 — infer from the Song's `key Am`.** No per-progression tag; the song's key drives the frame. But a progression is reusable, so the *same* body would mean different pitches in a major vs minor song, and a standalone preview (Content CRUD, no song) can't disambiguate. Fragile — I'd only use the song key at *realization* (it already supplies the tonic), not to decide the parse frame.

**Option 3 — a marker inside the Progression DSL body** (e.g. a leading `minor` / `\tonality minor` line). Makes the body self-describing, but extends the public Progression grammar and puts "mode" into a body that's otherwise key/mode-agnostic. More grammar churn than option 1 for the same result.

**The clean split, whichever we pick:** the progression's `Home` decides the *parse* frame (author→parent, at definition time); the realization `Key` (from the song/UI) supplies the *tonic + parent major* (at play time). They must agree — a `tonality: minor` progression is meant to be realized in a minor key; I'd validate that.

**I recommend Option 1** — least grammar churn, fits the existing header mechanism, keeps the parser body-only, and makes standalone progressions self-describing. Which way do you want to go?

## Rafa:

I like 1. Also, progression should be key agnostic, it is only need the `tonality`. In future, we could expand:
```
...
tonality: minor | mixolydian | phrygian 
...
```

Progressio define `tonality`, song define `key`...
If you agree go

## AI:

Steps 4–6 done (the range you authorized) — **989 Core tests green.** The C engine is live end-to-end at the domain level.

- **Step 4** — the C kernel: `DegreeFrameConverter` + `Progression.Home` + Transposer realizing against the parent major; `SpellRoot` counts from the parent tonic (→ G♯, not A♭).
- **Step 5** — went with your **Option 1**: a `tonality: minor` catalog-header key. `ProgressionParser` applies the converter at parse; `ProgressionStore` and the Content preview pass the tonality through. It rides in the stored DSL (no DB migration). The **song picks the `key`, the progression declares its `tonality`** — exactly as you framed it.
- **Step 6** — the payoff, proven end-to-end: a minor tune `1- 2ø 3 4- 5- 6 7 #7dim7 #6ø` → **Am · Bm7b5 · C · Dm · Em · F · G · G#dim7 · F#m7b5**. Bare naturals, letter-pure raised roots.

**Two things I deliberately deferred (want you aware):**
1. **Inline progressions authored *inside* a Song** don't yet inherit the song's mode (they'd need the song's `key` resolved first). A song that *references* a stored `tonality: minor` progression works fully — only the write-it-inline case is pending. No minor songs exist yet.
2. **User-authored minor via the Content CRUD editor** — `Save` strips the header (metadata editing was scoped out, EX3), so a hand-typed `tonality: minor` wouldn't persist. Pack content + preview both work.

Neither blocks minor content; they're follow-ups if/when we want inline or user-authored minor tunes.

**Remaining in the plan:**
- **Step 7** — renderer golden (a minor tune emits `\ks aminor` + relative-major spelling; major byte-identical). The `\ks` token + guard removal already landed in step 3, so this is mostly the end-to-end test.
- **Step 8** — the UI: the Key picker offers minor keys and threads the mode through the bridge → Features (the biggest remaining chunk — it's currently a bare pitch class everywhere).

Say `do step 7`, `do steps 7-8`, or `do plan` when you want me to continue.

## AI:

Good calls — pausing step 8 and pivoting to the minor content (which is what step 8's dogfood needs anyway, so this is the right order).

**Where first-class-minor-keys stands:** steps 1–7 ✅, **989 Core tests green.** The C engine is complete through the renderer. **Step 8 (UI + mode threading) is paused** — nothing was half-done (I was still reading). It subdivides cleanly for later:
- **8a — bridge + Features mode threading** (`GenerateRequest`/envelope gain `keyIsMinor`; `GenerateExercise.Build` + `ContentCrud.Preview` build `new Key(pc, isMinor)`). Unit-testable, no UI.
- **8b — UI** (a major/minor toggle beside the Key control; seed its mode from a song's key — the latter needs the mode in the content-list payload).

I mapped the whole threading path; it's mechanical, just broad. We resume it after the content exists to dogfood it.

---

**Minor progressions set** — now unblocked (the C frame + realization landed), so I can pin the DSL. Its `minor-progressions` idea already lists the set; here it is authored **tonic-relative + `tonality: minor`** (what the C converter now supports), each destined for `Content/default-pack/progressions/` with the same header shape as the major set (`name:`/`genre:`/`description:`/`tags:`/`tonality:`):

| Progression | DSL body | Realizes in A minor as |
|---|---|---|
| Minor ii–V–i | `2ø 57 1-` | Bm7♭5 · E7 · Am |
| Andalusian cadence (i–♭VII–♭VI–V) | `1- 7 6 5` | Am · G · F · E |
| Natural minor i–iv–v | `1- 4- 5-` | Am · Dm · Em |
| Harmonic minor i–iv–V | `1- 4- 5` | Am · Dm · E |
| Minor turnaround (i–♭VI–iiø–V) | `1- 6 2ø 5` | Am · F · Bm7♭5 · E |
| Aeolian loop (i–♭VI–♭VII–i) | `1- 6 7 1-` | Am · F · G · Am |
| Picardy cadence (…→ major I) | `1- 4- 5 1` | Am · Dm · E · **A** |
| Minor 12-bar blues | `1-7 4-7 1-7 1-7 4-7 4-7 1-7 1-7 57 4-7 1-7 57` | i7–iv7–V7 minor blues |

Note the bare `6 7` (= ♭VI ♭VII) and the major-quality `5`/`1` (the harmonic-minor V and the Picardy major tonic) — exactly the C authoring model.

**Plan for it** (the `minor-progressions` thread — its idea calls it *pure content, no engine/DSL changes*): author these 8 `.dsl` files, wire them into the default-pack manifest + `SeedData`, and add a `ProgressionSeedTests` peer that parses → realizes each in a minor key → checks the sounding chords (the dogfood the idea specifies). It parses/realizes/tests fully now; the UI selection just waits on step 8b.

Want me to author this set as-is, or tweak the list/tokens first? (This turn's already big — happy to start fresh on it with your go.)

## Rafa:

The set looks awesome! Author it

## AI:

Done — the **minor progressions set is authored and green (1005 Core tests).** All 8 landed in `Content/default-pack/progressions/`, and the dogfood test confirms each realizes to the intended chords in A minor:

| File | Realizes (A minor) |
|---|---|
| `minor_ii_v_i` | Bm7♭5 · E7 · Am |
| `andalusian_cadence` | Am · G · F · E |
| `natural_minor_i_iv_v` | Am · Dm · Em |
| `harmonic_minor_i_iv_v` | Am · Dm · E |
| `minor_turnaround` | Am · F · Bm7♭5 · E |
| `aeolian_loop` | Am · F · G · Am |
| `picardy_cadence` | Am · Dm · E · **A** |
| `minor_12bar_blues` | Am7 · Dm7 · … · E7 · Dm7 · Am7 · E7 |

No wiring needed beyond the files — `PackReader` globs `*.dsl` and the `.csproj` copies `Content/**` by wildcard, so they auto-register. Two small correctness touches: synced `PackDefinitionFile.HeaderKeys` with `CatalogHeader` (it was missing `description`/`tonality`), and added a `MinorProgression_RealizesToExpectedChordsInAMinor` dogfood theory that loads each file → confirms `tonality: minor` → realizes → checks the sounding chords (including the letter-pure raised roots). Recorded as a done plan on the `minor-progressions` thread.

**This is the real validation of the whole C pivot** — authored minor content that parses, realizes, and spells correctly end-to-end.

**Two open loose ends** (whenever you want them):
1. **Step 8** of first-class-minor-keys — the UI so a user can *pick* a minor key and select these in the app (8a bridge/Features mode threading, 8b the toggle). The engine's ready; this is the surface.
2. Lots of uncommitted work across both threads — say the word and I'll commit.

## Rafa:

Nice ideam do the quick_ship, thanks
commit
next step 8a — bridge + Features mode threading?
if so, do step 8a

## AI:

Committed the completed C-frame + minor-progressions work (`6e24256`), then split step 8 into **8a (bridge/Features)** and **8b (UI)** and did **8a** — **1008 Core tests green, Desktop builds clean.**

**8a:** the key's mode now threads from the definition all the way into realization:
- `GenerateRequest` + inbound envelope carry `KeyIsMinor`; the router's `generate` and `entityPreview` cases pass it (the preview event grew to 8 args).
- `GenerateExercise.Build` (all overloads) and `ContentCrud.Preview` take `keyIsMinor` and build `new Key(pc, isMinor)`; `Program.cs` forwards it.
- All `false`-defaulted, so every existing major flow is byte-identical.
- Tests: minor `Build` → A-minor override; minor `Preview` → `\ks aminor`; the envelope field parses.

So the whole engine path is ready: a `keyIsMinor` request realizes and renders a minor key correctly. **All that's left is 8b** — the JS toggle beside the Key control + seeding the mode from a song's key (the seed needs the key's mode in the content-list payload; I'll wire that when we do 8b). `loadExercise` re-key mode is a small separate follow-up.

Committing 8a now.