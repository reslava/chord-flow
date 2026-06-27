---
type: chat
id: ch_01KW18NJHFZY248VHW9M7N0WPV
title: caged-sixth-voicings Chat
status: done
created: 2026-06-26
updated: 2026-06-27
version: 2
tags: []
parent_id: null
requires_load: []
last_ai_block: 25
---
# caged-sixth-voicings Chat

## Rafa:

read idea

### 1. CAGED E shape tweak

CAGED derive E shape needs a tweak:
    
skip string 5 for m7b5 dim7  6   m6

#### 1.1 Golden oracle
When we verify resulting grips:
- Update Golden oracle test voicings for E shape m7b5 dim7
- 6 m6 will have not golden oracle authored voicings, we just do a visual check

### 2. 6th voicings

2 qualities: `6` and `m6`

C6  = C7  -> replace b7 for 6  
Cm6 = Cm7 -> replace b7 for 6  

### Plan

When we create plan:
Do 1
Visual check
Do 1.1
Do 2
Visual check

### Questions

What do you think? Agree?
Is it all grounded?

## AI:

Read the idea. Mostly agree on direction — but two parts are grounded and two need a decision before they go in a plan. Let me separate them.

## What's solid (grounded against the code)

**The 6th arithmetic and the "replace b7 → 6" mnemonic — correct.**
- `C6` = `1 3 5 6` = `{0,4,7,9}`; dom7 is `{0,4,7,10}`, so lowering the b7 (10) to the 6 (9) lands exactly on C6. ✓
- `Cm6` = `1 b3 5 6` = `{0,3,7,9}`; m7 is `{0,3,7,10}`, b7→6 lands on Cm6. ✓
- Caveat: that "replace b7" is a nice **oracle-authoring** shortcut, not how the engine works — `Derive` builds from the interval set (`QualityIntervals`), it never edits a 7th grip. So it's a great way for *you* to hand-author/eyeball the anchor grips, but in code the 6 is just a new tone the candidate selector places.

**The two qualities don't exist yet — so there's a prerequisite step (answers idea Q1).**
- `Quality` today tops out at `…HalfDiminished7, Diminished, Diminished7, Augmented` — **no `Major6`/`Minor6`**. So this needs the same precursor the dim7 work did: add `Quality.Major6` / `Quality.Minor6` + their `QualityFormulas` rows (`1 3 5 6` / `1 b3 5 6`), then derivation/authoring can use them. That's **step 0** of the plan, before anything else.
- Collision check (because Major6 is the set-twin of a minor7 a 3rd away): root-relative, `{0,4,7,9}` and `{0,3,7,9}` are distinct from every existing quality, so `QualityIntervals.FromIntervals` stays unambiguous. ✓ No domain hazard there.

## What needs a decision (not yet grounded)

**1. "Skip string 5 for m7b5 / dim7" — as written, this *deletes the chord's defining tone*.** This is the one I want to stop on. In today's authored E-shape grips, the b5 is voiced **only on string 5**:
- `m7b5 E = 8 9 8 8 11 8` → string 5 (fret 9) is the **b5**; strings 4/3/2/1 are b7, b3, b7, root. Skip string 5 ⇒ no b5 left ⇒ the grip is just an m7.
- `dim7 E = 8 9 7 8 10 8` → string 5 again is the **b5**; the rest are bb7, b3, bb7, root. Skip it ⇒ no b5 ⇒ not a dim7.

So "skip string 5" is **benign for 6 / m6** (the perfect 5th is an omittable colour tone there) but **destructive for m7b5 / dim7** (the b5 is their identity). The same one-line rule does opposite things to the two pairs. For m7b5/dim7 it can only work if the re-derivation **re-places the b5 on a remaining string** (4 or 2) — which means the new oracle grips aren't a cosmetic edit, they're a full re-voicing we have to verify tone-by-tone. **Is that your intent — drop string 5 *and* let the b5 move up to string 2/4 — or did you have a different shape in mind?**

**2. The engine has no notion of muting an *interior* string today — only below the bass root.** `Derive` plays every string from the bass root down to 1; the only mutes are strings *below* the root. "Skip string 5" introduces interior muting, and there are two ways to get it:
- **(a) Authored skip-exception** per (quality, shape) — simplest, but it re-introduces an authored fret/skip table, which is exactly what the derivation engine exists to avoid.
- **(b) A derived rule** — e.g. "once every distinct tone is voiced, mute the interior string whose only remaining candidate re-doubles the lowest-weight tone (the 5th)." That keeps it engine-derived and would naturally yield 4-string grips for 6/m6 — but I'd have to confirm it (i) fires for 6/m6, (ii) does **not** wrongly mute string 5 on maj/min/dom7, and (iii) for m7b5/dim7 only fires *after* the b5 has been re-placed elsewhere.
- I lean **(b)** to stay true to the engine, but it's more work and needs validation. Your call.

**3. Scope of the skip rule.** You only named the **E shape**. Is "skip string 5" E-shape-specific, or "any shape where string 5 is an interior, non-root-defining string"? (In A it's the bass root, in D it's already muted, so E is the only CAGED shape where it's even a question — unless G-shape 6/m6 hits the same thing.) And for **6 / m6 you haven't said which CAGED shapes to author/derive** (idea Q2) — all five, or just the practical ones?

**4. 6 / m6 oracle (you said visual-check only).** Fine to derive-then-eyeball on the fretboard page first. But I'd **capture the confirmed grips back into the oracle** once they look right, so a future engine tweak can't silently break them — otherwise 6/m6 are the only qualities with no regression anchor. Permanent oracle-less, or capture-after-confirm?

## On your proposed plan order
`Do 1 → visual → Do 1.1 → Do 2 → visual` is close, but it's missing the **Quality.Major6/Minor6 precursor**, and item 1 (the skip-string-5 tweak for m7b5/dim7) is blocked on decision #1 above. Suggested shape once we resolve the above:
1. **0** — add `Major6`/`Minor6` quality + formulas (domain precursor, mirrors dim7).
2. **1** — implement the skip-string-5 mechanism (a or b) for m7b5/dim7, re-verifying the b5 survives; update the m7b5/dim7 E oracle to the re-derived grips.
3. **visual check.**
4. **2** — derive 6/m6 (which shapes?); visual-check; optionally capture to oracle.

So: agree on the goal, agree the 6th arithmetic and the Quality precursor are right. The **skip-string-5 rule is the real design question** — for m7b5/dim7 it can't be a literal "mute string 5" or you lose the b5. How do you want to handle that — re-voice the b5 (and pick mechanism a vs b)? **STOP — waiting on that.**

## Rafa:

1. 

New authored voicings
- `m7b5 E = 8 x 8 8 7 8` 
- `dim7 E = 8 x 7 8 7 8` 

I think derive engine will produce these new better vocings just by introducing the new tweak rule 

I think these will be the derived 6 and m6 new voicings

- `m6 E = 8 x 7 8 8 8` 
- `6  E = 8 x 7 9 8 8` 

2. 
I think I lean (a) for simplicity, it's just a very small variation. I am almost sure will produce the desire results and no regressions.
I think (b) sounds very unnatural, and could produce regressions. I you want we can try and see, I am not sure at all what kind of results could produce this rule in the engin.
Some tweaks are hard to emulate because of human hand behaviour and the way guitar grips sound.
Example: with root on string 6 the b5 on string 5 sounds very bad (tritone), sounds much better in string 2

Anyway, please explain you (b) rule clearly, I am sorry but I did not understand it very well.

3. 
Scope for now is just the mentioned and needed
If I find more tweaks need we will talk about it

4. 
Capture after confirm
Grips should be:
- `m6 E = 8 x 7 8 8 8` 
- `6  E = 8 x 7 9 8 8` 
for E shape, and I will review the others

## AI:

Good — your grips clear up #1, #3, #4. I checked all four tone-by-tone and then hand-traced them through the actual engine. Two are right *and* the engine reproduces them; but there's one thing the "skip string 5" rule alone won't do, and it's the same reason your m7b5 b5 "sounds better on string 2."

## Your four grips are all valid (verified, C root, string 6→1)

| grip | s6 | s5 | s4 | s3 | s2 | s1 | tones |
|---|---|---|---|---|---|---|---|
| `m7b5 8 x 8 8 7 8` | 1 | – | b7 | b3 | **b5** | 1 | {1 b3 b5 b7} ✓ |
| `dim7 8 x 7 8 7 8` | 1 | – | bb7 | b3 | **b5** | 1 | {1 b3 b5 bb7} ✓ |
| `m6 8 x 7 8 8 8` | 1 | – | 6 | b3 | 5 | 1 | {1 b3 5 6} ✓ |
| `6 8 x 7 9 8 8` | 1 | – | 6 | 3 | 5 | 1 | {1 3 5 6} ✓ |

Note exactly what you intuited: in m7b5/dim7 the **b5 moves to string 2 (fret 7)** — the tritone off the bass, up an octave, instead of the muddy b5-on-string-5. Musically these are clean, complete grips. 

## The catch: "skip string 5" is **not enough by itself** — three of them also need the back-stretch

Here's the thing I traced. Your grips put a colour tone on **fret 7 — one fret *below* the bass root (fret 8)**: the b5 on string 2 (m7b5/dim7), the 6 on string 4 (6/m6). But the engine's reach window for an up-stacking shape like E is **forward-only** — `[bassFret, bassFret+3]` = `[8, 11]` — *except* dim7, which is the one quality that already gets a special "behind-1" stretch-back (`allowStretchBack = stacksUp && quality == Diminished7`, index reaches back 1 fret to 7).

So when I mute string 5 and run the engine as-is:

- **dim7** → already has the back-stretch → derives **`8 x 7 8 7 8`** exactly. ✓ (skip string 5 is all it needs.)
- **m7b5** → *no* back-stretch → the b5 it wants on string 2 sits at fret 7, **outside** the `[8,11]` window. The engine can't reach it, the all-tones pass never places the b5, and you get **`8 x 8 8 11 8`** — an m7 with the b5 **missing** and a fret-11 stretch. Broken. With the back-stretch added → derives your `8 x 8 8 7 8`. ✓
- **6** → the 6 on string 4 is at fret 7 → needs back-stretch → then derives **`8 x 7 9 8 8`** exactly. ✓
- **m6** → same → derives **`8 x 7 8 8 8`** exactly. ✓

So your instinct ("the engine will just produce these") is **right — but the tweak is two toggles, not one**:
1. **mute string 5**, and
2. **grant the behind-1 stretch-back** (the index reaching to fret 7) to m7b5 / 6 / m6 as well (dim7 already has it).

Both gated to the **E shape** for these four qualities. With both, I get all four of your grips on the nose. With only #1, m7b5 silently loses its b5.

## On (a) vs (b) — I now agree with you, (a)

Quick explanation of **(b)** so you know what you're turning down: (b) was "don't author the skip at all — make the engine *figure out on its own* to drop string 5," via a rule like *"after every chord tone is voiced, if an interior string can only re-double the least-important tone (the 5th), mute it."* The problem: (i) it still wouldn't solve the back-stretch (that's separate), (ii) a general "mute the doubling string" rule could mis-fire on maj/min/dom7 where string 5 carries a tone you *want*, and (iii) it's exactly the "hard to emulate human hand/grip-sound choices" thing you mentioned. So **(b) is more complex *and* riskier. (a) wins.** Author the small E-shape exception, keep it scoped, verify against the oracle.

## One scope caution

Granting the back-stretch must stay **E-shape-gated** (or at least gated to these qualities-in-E). m7b5 is also authored at the A and D shapes (`x 3 4 3 4 6`, `x x 10 11 11 11`); if I broaden the back-stretch globally it could shift those and break their oracle. Tying both toggles to the same per-(quality, E-shape) exception keeps A/D untouched → no regression, which is your "small variation, no regressions" instinct made safe.

## So where that leaves the plan

1. **0** — add `Quality.Major6` / `Minor6` + formulas (`1 3 5 6` / `1 b3 5 6`).
2. **1** — E-shape tweak: mute string 5 **+** behind-1 stretch-back for {m7b5, dim7, 6, m6}; update the m7b5 & dim7 **E** oracle to `8 x 8 8 7 8` / `8 x 7 8 7 8`.
3. **visual check** (fretboard page) — m7b5/dim7 against the new oracle; 6/m6 by eye.
4. **2** — 6/m6 across the E shape (you review other shapes), then **capture-to-oracle** the confirmed grips (`6 = 8 x 7 9 8 8`, `m6 = 8 x 7 8 8 8`, plus whatever you bless on A/D/etc.).

Does "skip string 5 **+** behind-1 reach, E-shape-gated, mechanism (a)" match what you want? If yes I'll turn this into the idea's design + a plan. **STOP — confirm and I'll write it up.**

## Rafa:

Yes, good catch, I forgot to mention behind-1 stretch-back
Write design, req, plan

## AI:

Done — design, req (locked), and plan are written.

- **Design** `de_01KW2G8YYBE2QE4M78H519RH2A` — captures the six decisions, incl. the verified tone-by-tone table and the two-toggle E-shape rationale. Idea + design marked **done**.
- **Req** `rq_01KW2G9V1TYQB3PVKYAEFE5ZFG` — **locked**. `IN1–IN9`, `EX1–EX4`, `C1–C5`.
- **Plan** `caged-sixth-voicings-plan-001` (`pl_01KW2GBV0668GBT59RDW1PAX3G`) — 8 steps:

1. `Major6`/`Minor6` quality + formulas *(precursor)* — IN1, IN2, C3
2. `CagedDerivation` E-shape tweak: mute string 5 + behind-1 stretch-back for {m7b5, dim7, 6, m6} — IN3, C1, C2
3. Update m7b5/dim7 **E** oracle → `8 x 8 8 7 8` / `8 x 7 8 7 8` *(proves the tweak)* — IN4
4. Voicing-DSL `6`/`m6` suffixes *(for fixtures)* — IN6
5. Catalog + UI: 6/m6 as five-shape qualities, display names, fretboard quality list, coverage tests — IN5, IN7, C4
6. **Dogfood visual check** — you review 6/m6 across shapes → **STOP for blessing** — C5
7. Capture confirmed 6/m6 grips into the golden oracle — IN8, C5
8. Ref-sync `chordflow-domain-model-reference` — IN9

Step 6 is the built-in review gate; steps 7–8 wait on your call there.

Next step would be **Step 1** — add `Quality.Major6`/`Minor6` + their `QualityFormulas` rows (`src/ChordFlow.Core/Music/Harmony/Quality.cs`, `QualityFormulas.cs`, + `QualityFormulasTests.cs`/`ChordTonesTests.cs`). **STOP — say `go` to start, or tell me to adjust the plan.**

## Rafa:

I started the plan
Do whole plan

## AI:

**Steps 1–5 done — full suite green (735 passed, 0 failed).** Stopping at the Step 6 dogfood gate for your blessing.

### What landed
1. **Quality precursor** — `Quality.Major6`/`Minor6` + `QualityFormulas` rows `1 3 5 6` / `1 b3 5 6`; semitones `{0,4,7,9}` / `{0,3,7,9}` derive automatically. `FromIntervals` stays unambiguous.
2. **E-shape exception** in `CagedDerivation` — for `{m7b5, dim7, 6, m6}` in the **E shape only**: mute string 5 + grant the index's behind-1 stretch-back. Gated on `shape == E`, so every C/A/G/D derivation is byte-identical (m7b5 A/D oracle didn't move).
3. **m7b5 / dim7 E oracle updated** → `8 x 8 8 7 8` / `8 x 7 8 7 8`. The oracle test passes — i.e. the two-toggle tweak **reproduces your grips exactly** (that's the proof for step 2).
4. **Voicing-DSL** `6` / `m6` / `-6` suffixes (+ the writer inverse).
5. **Catalog + UI** — 6/m6 are five-shape qualities (catalog 36 → **46**); display names, `caged-chords.js` quality list, coverage/count tests updated.

### Derived 6 / m6 grips across all five shapes (C root) — for your review
```
Major6  C : x 3 2 2 1 3      Minor6  C : x 3 1 2 1 3
Major6  A : x 3 5 5 5 5      Minor6  A : x 3 5 5 4 5
Major6  G : 8 7 7 5 8 8      Minor6  G : 8 6 7 5 8 8
Major6  E : 8 x 7 9 8 8  ✓   Minor6  E : 8 x 7 8 8 8  ✓
Major6  D : x x 10 12 10 12  Minor6  D : x x 10 12 10 11
```
Both **E** grips match your known values exactly (✓). All ten are fully spelled (every chord tone voiced). The C/A/G/D shapes are derived-by-stacking and want your eyes — a couple are stretchy (Major6 G spans frets 5–8, the full 4-fret hand).

### Findings worth flagging
- **Files touched beyond the plan's filesToTouch** (mechanically required collaborators, all small additive map entries): `VoicingDslWriter` (parser inverse, for the catalog round-trip), `AutomaticVoicingId` (id tokens `6`/`m6`), `ChordSymbol` (display suffix `6`/`m6`, needed so the dogfood page renders the 6/m6 title without throwing).
- **The 6 is tagged as a "Seventh"** — semitone 9 falls in the existing `9/10/11` function band, so `ChordTones`/`ChordShapeDiagram` colour & label the added 6 as a seventh-position tone. No throw, fully derived; but there's no first-class "Sixth" chord-tone function (out of this thread's req scope). On the fretboard page the 6 shows with the seventh's colour/label — cosmetic. Worth a future thread if you want a proper Sixth role.
- **m7b5/dim7 E anchor finger changed Index/Middle → Ring** — a geometric consequence of the relocated box `[7,8]` (root now at the box's high edge in a tight 2-fret span). The anchor-finger oracle confirms it; I updated the two fixtures' `anchor:` letters to `r`.
- *(note)* A pre-existing low-neck tightness drops a tone when **any** quality (e.g. Major7) is re-derived at the cramped open position (`minFret 0`). I scoped the new 6/m6 coverage test to the canonical region + realize-across-roots — the same standard the 36 oracle combos meet — rather than asserting full spelling at the nut. Not a 6th-family issue.

### Step 6 — your move
Run the app → CAGED Chords page → step through 6 and m6 across C/A/G/E/D and a few roots. Bless the grips (or tell me which shapes to adjust), and I'll capture the confirmed set into the golden oracle (step 7) + ref-sync the domain model (step 8). Want me to launch the app for the dogfood?

## Rafa:

I ran the app, visual check, good progress!

Issues:
- The 6 is tagged as a Seventh. The enharmonic bb7 6 need to resolve depending of quality, 6 and m6 should show 6
- anchors
  - Cm7b5 shape:E root:6 frets: 8 x 8 8 7 8 anchor should be m
  - Cdim7 shape:E root:6 frets: 8 x 7 8 7 8 anchor should be m
  Rule could be E shape with `1-behing` rule derive anchor m

## AI:

Good review — thanks. Both are real. Here's where each stands.

### Issue 2 — anchors: **done** ✅
Your rule is in: in the **E shape with the behind-1 stretch active**, the index is spent on the stretch-back fret, so the root one fret up is the **middle** finger. `AnchorFinger.Derive` got a stretch-back-aware branch (index at the low edge → fingers count up one-per-fret), gated to exactly the E-shape behind-1 case so A/D dim7 (which also stretch back) keep their Index anchor. Both fixtures back to `anchor:m`; the anchor-finger oracle + frets oracle + full suite (735) are green.

This also means the 6/m6 E grips anchor sensibly: Major6 E and Minor6 E both derive **m** now too.

### Issue 1 — the 6 shows as a 7th: a domain-model decision, want your go first
The root cause: semitone **9 is enharmonically ambiguous** — it's `bb7` in dim7 but the `6` in 6/m6. Today the engine classifies a chord tone by its *semitone band* (9/10/11 → "Seventh"), which can't tell them apart. The label `bb7` and the purple "seventh" colour both come from that one wrong call.

**The fix has to make classification quality-aware, and the formula already holds the answer** — `QualityFormulas` spells dim7 as `… bb7` (degree 7) and 6/m6 as `… 6` (degree 6). So the clean, durable fix is to classify each chord tone by its **formula degree**, not its semitone band:

1. Add `ChordToneFunction.Sixth` (first-class role).
2. `ChordTones`/`ChordShapeDiagram` classify from the formula's degree number (6 → Sixth, 7 → Seventh) — semitone 9 resolves correctly per quality.
3. `IntervalSpeller.Label`: a Sixth → `"6"`.
4. Fretboard palette (`fretboard-render-component.js`) gains a `sixth` colour + legend.

Blast radius is small and additive (no existing quality changes — dim7 still reads `bb7`), and it sets up future 6/9, add9, etc. correctly. The alternative — a narrow "if Major6/Minor6 show 6" special-case — I'd avoid: it hard-codes around the formula instead of trusting it, and re-breaks on the next added-tone quality.

This is **new scope** past the locked req, so if you're good with the approach I'll: amend the req (the two new requirements — quality-resolved labels + the E-shape-1-behind anchor rule), add the plan steps, then implement before we capture to the oracle (step 7).

**One sub-question:** the 6th wants its own diagram colour (the five buckets are root/3rd/5th/7th/tension). I'd add a distinct one — proposing **amber `#f59e0b`** for "6th". Good, or pick another?

**Go / adjust?**

## Rafa:

Go

## AI:

Both fixes are in — full suite green (**746 passed**, +11 new tests). Req amended to v2 (`IN10`, `IN11`, `C6`); plan grew to 10 steps (1–7 done).

### Issue 1 — the 6 now reads as a 6 ✅
Chord-tone function is now derived from the **formula degree**, not the semitone band, so the enharmonic semitone 9 resolves by quality everywhere:
- `Major6`/`Minor6` → the added tone is a **`6`** (new `ChordToneFunction.Sixth`, amber `#f59e0b` on the fretboard).
- `Diminished7` → still **`bb7`** (a seventh), byte-identical to before. Every other quality is unchanged.

Applied to **both** chord-diagram producers (the CAGED page *and* the Content-page voicing preview) so they stay consistent. One thing I deliberately left alone: `IntervalSetDiagram` (scales/arpeggios) still labels semitone 9 as a seventh by band — that's the scale context, not chord 6ths, and out of this thread's scope. Flag if you want it folded in too.

### Issue 2 — anchors ✅ (recap)
m7b5 E, dim7 E, and now 6/m6 E all anchor **m** (middle), per your 1-behind rule.

### Step 8 — re-bless, please
This re-touches what you see on the CAGED page (the `6` label + amber colour + middle anchors), so it's back to you for the visual gate. Run the app → CAGED Chords → step through **6** and **m6** across C/A/G/E/D (and dim7 to confirm it still shows `bb7`). The grips themselves are unchanged from the table I posted earlier — only the labels/colours/anchors moved.

Bless it and I'll capture the confirmed grips into the golden oracle (step 9) + ref-sync the domain model (step 10). **Want me to launch the app?**

## Rafa:

Good
Need only small tweak in legend label, sort intervals by number 1, 3, 5, 6, 7

## AI:

**Plan complete — all 10 steps done, plan closed.** Full suite **746 passed, 0 failed.**

Captured all 10 confirmed grips into the golden oracle:
```
Major6  C x 3 2 2 1 3   A x 3 5 5 5 5   G 8 7 7 5 8 8   E 8 x 7 9 8 8   D x x 10 12 10 12
Minor6  C x 3 1 2 1 3   A x 3 5 5 4 5   G 8 6 7 5 8 8   E 8 x 7 8 8 8   D x x 10 12 10 11
```
New fixtures `fixtures/caged-oracle/{maj6,m6}_{c,a,g,e,d}shape.dsl` (anchors r/i/p/m), 10 oracle rows, `ExpectedVoicingCount` 36→46 — derived == authored for every cell.

Ref-synced `chordflow-domain-model-reference.md` (11 qualities, the `Sixth` function + formula-degree classification, the E-shape skip-5 / behind-1 / anchor-middle rules, the 46-grip oracle). Done doc: `done/pl_01KW2GBV0668GBT59RDW1PAX3G-done.md`.

Engine now derives 6/m6 across all five CAGED shapes, the m7b5/dim7 E grips match your tone-by-tone spec, the 6 reads as a **6** (amber) everywhere, and the E-shape behind-1 grips anchor **m**.

Nothing is committed yet — say the word and I'll commit to main.

## Rafa:

You said:
> IntervalSetDiagram (scales) still labels semitone 9 as a seventh by band — I left it since it's scale context, not chord 6ths. Say if you want it folded in.

I ran app, scales page, check these:
1 b3 b5 bb7
1 b3 b5 6

both OK, legent OK

so what you meant?

## AI:

You're right — there's **no problem on the Scales page**. I was over-cautious. Two reasons it's already correct:

1. **The label is your typed token.** `IntervalSetDiagram` keeps each marker's interval label as the exact token you wrote (`token` on line 52) — so `6` shows `6` and `bb7` shows `bb7`. It never runs the chord-label speller, so the dim7-vs-6 ambiguity can't even arise here.
2. **The Scales page uses its own palette.** The only place `IntervalSetDiagram` still buckets semitone 9 → `"seventh"` is `FunctionFor` — and that's a *dormant default* "used only if rendered without an override palette." The Scales page supplies its own root-red / rest-black palette, so `FunctionFor` never reaches the screen. The legend in palette mode keys off your tokens too (and now sorts by degree → 1, b3, b5, 6).

So what I "meant" was only that the **internal default-colour bucket** in `FunctionFor` still calls semitone 9 a seventh — but since nothing renders this diagram with the default palette, it's invisible. The band mattered only for **chord** diagrams (CAGED page + voicing preview), which I already fixed. Nothing to fold in.

The one *optional* consistency tweak left: make that dormant `FunctionFor` degree-aware too (it has your token, so it could bucket `6`→sixth/amber, `bb7`→seventh) — purely so a hypothetical future bare-render stays consistent. Zero visible effect today. Want it, or leave the dead default as-is?

## Rafa:

I need to think deeper about scales, arpeggios, etc
Spin up a thread idea for the tweak
Commit

## AI:

Done:
- **Idea spun up** — `guitar/scale-arpeggio-tone-functions` (draft): the degree-aware `FunctionFor` tweak as the concrete step, with the broader "how scales/arpeggios model function/spelling" questions parked there for you to think through.
- **Committed** to `main` as `a6496de` (49 files: engine + diagrams + JS + tests/fixtures + Loom docs). Not pushed — say the word if you want me to push.