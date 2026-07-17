---
type: chat
id: ch_01KXQAY7566SEN5JSWP0S00YJ4
title: harmonic-analysis Chat 001
status: done
created: 2026-07-17
version: 1
tags: []
parent_id: null
requires_load: []
last_ai_block: 29
---
# harmonic-analysis Chat 001

## Rafa:

read idea
is it all clear and grounded for a design?

this will be pure music harmony domain
chord sheets v2 will be the consumer, showing the analysis we do here

future follow up add-ins:
- songs, progressions with multiple keys, DSL should allow multiple `Key`

question:
- I would like to know if you can fill up some interesting progressions and songs to be used here and in future features

## AI:

Read the idea against the domain-model + architecture refs. Short answer: **yes — it's clear and grounded, design-ready.** It lands cleanly as a Harmony sink and there's an existing seam waiting for it. A handful of real *design* decisions remain, but they belong *in* the design, not as blockers to starting it.

## Why it's grounded

- **It fits the kernel as-is.** A pure `Chord (+ Key) → ChordAnalysis` pass in `Music.Harmony` is exactly the shape of the other sinks — immutable, no I/O, no glyphs. The inputs already exist: `Key(Tonic, IsMinor)`, `RomanDegree(Degree, Quality, Accidental)`, `Scale`/`DiatonicChord`, `QualityIntervals`. Secondary-dominant detection ("dominant whose root is a fifth above a diatonic target") is just interval arithmetic you already have.
- **There's a seam already reserved for it.** `ChordSheetBuilder` today computes `ChordRef.Roman` as *honest diatonic only* and the domain ref literally says *"no secondary-dominant/borrowed inference — that awaits the harmonic-analysis thread."* So this pass **subsumes** that inline logic: one source of function, ChordSheetR v2 just reads a richer field. That's the north-star "computed once in Core, shared by every surface" you called out.
- Consumers, non-goals, and validation are all well-drawn. Key is an *input*, not detected — correct for v1 and consistent with Song carrying `InitialKey`.

## Design decisions to settle (in the design, with my leanings)

1. **Context-free vs sequence-aware labeling.** Is `A7`-in-C a `V/ii` from key+quality *alone*, or only when a `ii` actually follows it? Lean: **label context-free per chord** (root a fifth above a diatonic degree + dominant quality ⇒ `V/x`, target recorded), and treat actual resolution as a later *tonicization-span* concern — keeps the pass an order-independent `Chord × Key → ChordAnalysis` map, matching your "resolving or not." The one place this bites: `Bb7`-in-C is genuinely ambiguous (`♭VII7` borrowed **vs** tritone-sub of `E7`) and only neighbors disambiguate — so the API should still *accept* the sequence even if v1 labels each chord largely independently.

2. **Category precedence.** A chord can satisfy several categories at once. We need a deterministic order + a documented tie-break. Proposed: `Diatonic > SecondaryDominant/vii° > TritoneSub > Borrowed > Chromatic`, with resolution context breaking the TritoneSub-vs-Borrowed tie when we have it.

3. **Result shape.** Keep `functionLabel` **structured, not a rendered string** — a `category` enum + optional `target` degree (`ScaleDegree`/`RomanDegree`) + `sourceMode` — and let each render surface format the glyph (`V7/ii`, `♭II7`), honoring the "no glyphs in Music" non-goal. Open sub-question: does this pass also *return* the plain `Diatonic` label so `ChordSheetBuilder` drops its inline diatonic logic entirely (one function source)? I'd say yes.

4. **Major *and* minor tonic in v1.** The diatonic set and the direction of "borrowing" flip with `Key.IsMinor` (Picardy third and minor `iiø–V–i` need a minor tonic). Confirm both modes are in v1, and take the key/scale as an explicit **parameter** — which is precisely what makes your future *multiple-`Key` per song* additive: a multi-key song just calls the same stateless pass per key-region. No API change later.

## Interesting progressions & songs to seed this (and future features)

Here's a starter catalog in the existing Nashville Progression DSL, grouped by the analysis feature each one exercises — they double as real songbook/default-pack content. (Recognizable tunes noted; progressions aren't copyrightable.)

**Diatonic baseline (ii–V–I family)**
| DSL | In C | Exercises |
|-----|------|-----------|
| `2-7 57 1^7` | Dm7 G7 Cmaj7 | the canonical `ii–V–I` |
| `1^7 6-7 2-7 57` | Cmaj7 Am7 Dm7 G7 | diatonic `I–vi–ii–V` turnaround |
| `2ø 57 1-` | Dm7♭5 G7 Cm | minor `iiø–V–i` (minor-key context) |

**Secondary dominants**
| DSL | In C | Exercises |
|-----|------|-----------|
| `27 57 1` | D7 G7 C | `V/V → V → I` |
| `1^7 67 2-7 57` | Cmaj7 A7 Dm7 G7 | `V/ii` inside a turnaround (rhythm-changes flavor) |
| `37 67 27 57 1` | E7 A7 D7 G7 C | full circle of secondary dominants (ragtime / "Sweet Georgia Brown") |

**Borrowed / modal mixture**
| DSL | In C | Exercises |
|-----|------|-----------|
| `1 4- 1` | C Fm C | borrowed minor `iv` — the `Fm` case from the reference sheet ("In My Life") |
| `1 b7 4` | C B♭ F | `♭VII` mixolydian ("Sweet Home Alabama", "Hey Jude" coda) |
| `1 b6 b7 1` | C A♭ B♭ C | `♭VI–♭VII` aeolian cadence (epic-rock) |

**Tritone substitution**
| DSL | In C | Exercises |
|-----|------|-----------|
| `2-7 b27 1^7` | Dm7 D♭7 Cmaj7 | `♭II7` tritone-sub of `V` |
| `1^7 b37 b6^7 b27` | Cmaj7 E♭7 A♭maj7 D♭7 | Tadd Dameron turnaround ("Lady Bird") — stacked subs |

**Diminished passing (secondary vii°)**
| DSL | In C | Exercises |
|-----|------|-----------|
| `1 #1°7 2-7` | C C♯°7 Dm7 | chromatic passing `#i°7` |
| `1 #4ø 57 1` | C F♯ø G7 C | `#iv°` from the reference sheet |

**Stress test — must NOT over-label**
| DSL | In C | Exercises |
|-----|------|-----------|
| `17 47 17 17 47 47 17 17 57 47 17 57` | dominant 12-bar blues | I/IV/V all *dominant7* — the analyzer must read these as the blues idiom, **not** secondary-dominants-of-nothing |

Note we already have a **`jazz-blues`** thread (done — "Jazz Blues — First Real Song") and the Herb Ellis reference sheet the idea cites (`V/ii`, `♭II` tritone sub, `#iv°`); I'd fold that blues in as the flagship golden case rather than re-author it.

If this reads right, the natural next move is a `req.md` — I'd lock decisions 1–4 above as constraints and cite this catalog as the golden-oracle scope. Want me to draft that, or keep chewing on any of the four decisions first?

## Rafa:

I agree all
I really like this idea
Lets talk a little more, when all settle down: design -> req -> plan (design before req)

### Interesting progressions

Very nice set
Could you add them to `src/ChordFlow.Core/Content/default-pack/progressions`? include full descriptions
Need to read `loom/refs/chordflow-dsl-reference.md`

### Design decisions

Agree your reasoning in all 4 points

My concenrs are in points:

1. **Context-free vs sequence-aware labeling.** 
Multiple keys DSL and Music domain support
This will also affects to accepting sequence-aware labeling

4. **Major *and* minor tonic in v1.** 
minor Keys
I think currently Chord Flow only allow Major Keys
- is possible implement this considerig a minor Key just with its relatives major Key 
- maybe we should spin up a thread + idea to support it before implement this
- what do you think?

## AI:

Great — decisions 1–4 locked in principle. Both your concerns are sharp; I dug into the code (`Key`, `Scale`, `Transposer`, `SongParser`, the pack format) so this is grounded, not hand-waving. Two of them need a decision from you before I touch code, so I'm holding.

## Point 4 — minor keys

**What's actually true today** (I checked): minor is *modeled* but not *coherently driven end-to-end*.

- The model already speaks minor: `Key(Tonic, IsMinor)`, `Scale.NaturalMinor` + `Scale.ForKey(key)` (switches on `IsMinor`), `SongParser.ParseKey` accepts `Am`/`Cmin`, and `mod vi` flips mode to minor. So it's **not** true that minor is absent — it's half-wired.
- But `SongParser` literally comments *"Major by default (v1 renders major)"*, and there's a real snag underneath it: the **Progression DSL degrees are authored in a major frame** (chromatic notes written with explicit accidentals — `b27`, `#4dim7`). Meanwhile `Transposer` realizes degrees through `Scale.ForKey`, which for a minor key uses **natural-minor** intervals. So handing an existing major-frame progression a minor key would **double-shift** degrees 3/6/7 (the `b7` becomes b-of-b7). Minor keys aren't wrong in the types — they're just never coherently exercised.

**On your "just use the relative major" idea** — it splits cleanly in two:

- For **pitch realization**: relative major works, and is in fact how you'd notate a minor tune *today* — write it in the relative major's degrees (`6- 2- 3-…` in C gives Am Dm Em) and pick C major. Same 7 notes, correct sounding chords.
- For **harmonic *analysis*** (this thread): the shortcut is **wrong by construction**. Analysis is *tonic-relative* — in A minor, `Am` is **`i`**; as "C major" it's **`vi`**. Every borrowed-chord / secondary-dominant label hangs off which pitch is tonic, so faking minor via its relative major would mislabel the whole point of the feature. This pass must take the *true* tonic + mode.

**So — yes, I agree: spin up a separate thread + idea.** Something like **"First-class minor keys (end-to-end)"**, because it carries its own genuine decision that isn't ours to make here: *what frame does a Progression DSL degree use in a minor key* — major-relative (degrees stay as written, mode is only a spelling/label concern) vs natural-minor-relative (degree 3 = ♭3 automatically). Plus UI key-picker + renderer spelling. That's a real prerequisite with its own design.

**But I'd keep the dependency one-directional and narrow:** the harmonic-analysis *engine* handles `IsMinor` natively from day one (the logic is symmetric and the model already supports it — minor `iiø–V–i`, Picardy, borrowing *into* minor are core cases). Only the ability to *drive* minor analysis through the app UI waits on the minor-keys thread. So the decision for us is:

- **(A, my lean)** analysis v1 includes minor-tonic labeling + golden tests in Core now; the UI surfacing lands when the minor-keys thread does.
- **(B)** scope analysis v1 to major only, add minor once minor-keys ships.

I lean (A): the marginal cost is a few extra golden oracles, and it keeps the analyzer honest (symmetric major/minor) instead of baking in a major-only assumption we'd tear out later.

## Point 1 — multi-key / sequence-aware

You're right that these two are linked, and the good news is the seam already half-exists: **multi-key is already real at the *song* level** — `SongExpander` produces a `RealizedSong` whose sections are *each keyed*, and `mod`/`key` change the running key. So a modulating song already hands us per-region keys for free.

That points at one clean API decision: the analysis pass takes an **ordered sequence where the key can vary per position** — conceptually `IReadOnlyList<(Chord chord, Key key)>` (or it consumes `RealizedSong` sections directly). That single shape is what makes *both* of your futures additive:

- **modulation / multiple keys** → the key just changes mid-sequence, no new concept;
- **sequence-aware labeling** (resolution-based disambiguation, tonicization spans) → we already hold the neighbors, so it's a later pass over the same sequence, not an API change.

That's the "accept the sequence" design you flagged — I'd bake the per-position-key sequence into the API from v1 even while v1 labels each chord largely independently.

## The progressions — I need two calls before I write them

Read the DSL ref and the existing pack (`12bar_blues.dsl`, `jazz_blues_standard.dsl`, `jazz_blues_turnaround.dsl`) so I match conventions. Two things block a clean "add them with full descriptions":

1. **There is no description field in the pack format.** A pack `.dsl` header carries only `name:` / `genre:` / `subgenre:` / `tags:` (`CatalogMetadata`), and a **progression body can't hold `#` comments** — the parser reads every non-header line as bars, so a `# …` line would fail to parse (unlike the Song DSL, which does support `#`). So "full descriptions" has nowhere to live today. Options:
   - **(a, my lean)** add a `description:` field to the catalog header (`CatalogMetadata` + `CatalogHeader.Parse/Serialize`) — a small, additive change that benefits *every* content kind and is genuinely user-facing (it can show in the Content catalog / chord-sheet header). Descriptions become real product data, not dead comments.
   - **(b)** carry the description only in a richer `name:` + `tags:` and skip prose.
   Which do you want? (a) is a tiny thread of its own, really.

2. **The minor-tonic entries.** A few in my set are minor-frame (`2ø 57 1-` minor ii–V–i, Picardy). Given the minor-keys thread, I'd **add the major-frame set now** (secondary dominants, borrowed, tritone subs, diminished passing — they realize correctly today) and **hold the minor-tonic ones** to land with minor-keys. Agree?

Also: genre/tags taxonomy — today's pack uses `Jazz`/`Blues`; my set spans Jazz, Rock/Pop, Ragtime. I'll tag accordingly unless you have a scheme in mind.

**Proposed order, then:** you settle 4A-vs-4B + the description field (1a-vs-1b) → if 1a, I do the tiny `description:` enhancement first → I add the major-frame progressions with descriptions → *then* we go design → req → plan for the analysis pass itself, with the minor-keys thread as a noted dependency. Sound right? I'll wait on your calls before writing anything.

## Rafa:

- 4A analysis v1 includes minor-tonic labeling + golden tests in Core now; the UI surfacing lands when the minor-keys thread does.
- 1A add a `description:` field to the catalog header
- spin up a separate thread + idea. Something like "First-class minor keys (end-to-end)"

We will have these threads:
1. domain/harmonic-analysis (this thread) 
2. chord-sheets/first-class-minor-keys...
3. chord-sheets/harmonic-v2... (depends on 1 and 2 for minor keys)

Any other dependecies?
Which order should we implement 1 and 2? I think we could do 1, then 2, then 3. You?

## AI:

Good — 4A, 1A, and the separate minor-keys thread all locked. One correction on the topology, then the dependency map and my read on order.

## Weave placement — one change

- **1. `domain/harmonic-analysis`** (this thread) — ✅ correct, it's a Music-domain engine capability.
- **2. minor keys — I'd put this in `domain`, not `chord-sheets`.** It's not a sheet feature: it's a **DSL + realization** change (the Progression-degree *frame* decision, `Transposer`, `Scale`, `SongParser`, plus the UI key-picker and renderer spelling). Everything consumes it — Score, Sheet, Now/Next — so it belongs next to `chromatic-degrees` / `progression` / `song` in `domain`. Suggest **`domain/first-class-minor-keys`**.
- **3. `chord-sheets/harmonic-overlay`** (your "harmonic-v2") — ✅ correct, *this* one is genuinely a chord-sheets consumer: it renders thread 1's analysis on ChordSheetR.

## Dependencies — the actual graph

The key insight that changes the graph: **thread 1 is pitch-based**. Per the idea, its input is a concrete `Chord` (+ `Key`), not a `RomanDegree` — it computes function from `(chordRootPc − tonicPc, mode)`. That has two consequences:

- **1 depends on nothing.** It sidesteps thread 2's whole DSL-frame decision, because it never reads a DSL degree — it reads pitches. Its minor-tonic golden tests are hand-built concrete chords (`Dm7♭5, G7, Cm` + `Key(C, minor)`), no `Transposer` realization needed. So **1 is NOT blocked by 2** — that's what makes 4A cheap.
- **2 depends on nothing new** — it's the DSL/realization/UI work, standalone.
- **3 depends on 1 (hard)** — it can't render analysis that doesn't exist — **and on 2 (soft)** — only the *minor-key display* case needs 2; the major-key overlay ships on 1 alone. Exactly as you said.

Two more dependencies you didn't list, both small and both feeding the progressions you asked for:

- **The `description:` catalog field (1A)** is a tiny prerequisite for "add the progressions *with full descriptions*." It's cross-cutting (all content kinds) and independent of 1/2/3 — I'd do it as a small standalone change, not a full thread.
- **The progressions content itself** couples to **thread 1** (they're its golden-oracle scope + real default-pack content); the **minor-tonic** ones additionally wait on **thread 2**.

So the graph:

```
description-field (1A) ─┐
                        ├─► major-frame progressions ─► golden oracles for ▼
domain/harmonic-analysis (1) ──────────────────────────────────────────────┐
                                                                            ├─► chord-sheets/harmonic-overlay (3)
domain/first-class-minor-keys (2) ──────────(soft: minor display)──────────┘
                                    └──► minor-tonic progressions
```

## Order — I agree with 1 → 2 → 3

With one nuance: **1 and 2 are independent**, so the order between them is a *priority* call, not a forced dependency. I land the same place you do — **1 first** — for concrete reasons, not just momentum:

- 1 is the active, already-designed thread and it's genuinely unblocked (pitch-based, self-contained tests).
- 2 carries a real, non-trivial design decision of its own (major-relative vs natural-minor-relative degree frame). Rushing it *just* to unblock 1 would shortchange that design — and 1 doesn't need it. Let 2 get its own unhurried design.
- When 2 lands, we get end-to-end minor realization "for free" to **re-validate** 1 against real `Transposer` output (not just hand-built fixtures) — a nice second check, and the natural hand-off into 3.

So: **1 → 2 → 3**, with the `description:` field + major-frame progressions slotted in as early concrete deliverables alongside 1 (they're 1's golden material anyway).

## Proposed next action

I'd start by writing the two new threads so the roadmap is real: `domain/first-class-minor-keys` (idea) and `chord-sheets/harmonic-overlay` (idea), with `depends_on` edges set (3 → 1, 3 → 2). Then come back here and take *this* thread through design → req → plan (design first, per your rule), citing the progression catalog as golden scope.

Want me to create those two thread stubs + ideas now, or design this thread first and stub the others later? Your call — I'll wait.

## Rafa:

Agree all, sorry for my mistake putting minor keys in chord-sheets weave instead of domain, I got distracted

1. write design + req 
maybe you find something useful before
2. spin up `domain/first-class-minor-keys`, `chord-sheets/harmonic-overlay` threads + ideas

## AI:

Done — roadmap scaffolding + this thread's design & req are written (design before req, per your rule). Nothing locked yet; all await your read.

**New threads + ideas (with dependency edges):**
- `domain/first-class-minor-keys` — the DSL-frame decision (major-relative vs natural-minor-relative), realization coherence, spelling, UI key-picker. *(In `domain`, not chord-sheets — as corrected.)*
- `chord-sheets/harmonic-overlay` — the ChordSheetR v2 consumer. Deps set: **→ harmonic-analysis (hard)**, **→ first-class-minor-keys (soft, minor display only)**.

**This thread:**
- `design.md` — logs the five settled decisions (D1 pitch-based/context-free · D2 precedence · D3 structured output · D4 minor-in-Core · D5 per-position-keyed sequence), the `ChordAnalysis` shape, the per-chord algorithm, type placement (`Music/Harmony` sink; the `RealizedSong→sequence` adapter stays out of Music), the subsumption of `ChordSheetBuilder.RomanFunction`, and the golden-oracle list.
- `req.md` (**draft**) — IN1–IN10, EX1–EX6, C1–C4.

**Something useful I found while grounding it:** `ChordSheetBuilder.RomanFunction` (line 273) already carries the honest-diatonic label *and a comment saying the secondary-dominant/borrowing labels "come later from the harmonic-analysis pass"* — so the subsumption is a seam that's literally reserved for us. And `DiatonicChord.Build` already derives the diatonic quality per degree, so it's the ready-made oracle for the "is this chord diatonic?" step — the analyzer reuses it rather than re-deriving.

**One open sub-decision I need your call on** (it's the only thing not fully settled — everything else follows our chat):

> **Category granularity.** I went with a **merged `SecondaryDominant`** that covers both `V/x` (dominant) and `vii°/x` (leading-tone dim), since the chord's own quality already distinguishes them — smaller enum. The alternative is splitting into `SecondaryDominant` + `SecondaryLeadingTone`. Not architecturally load-bearing (identical algorithm), purely how finely the enum names it. Keep merged, or split?

**Then, on your go, the remaining wrap-up:**
1. Confirm the category-granularity call (above).
2. I lock `req.md` (`finalize`) and mark `idea` + `design` done.
3. The parallel content deliverables you asked for: the small `description:` catalog-header field (1A), then the **major-frame** progressions added to the default pack with descriptions (holding the minor-tonic ones for thread 2).
4. Then `plan-001` for the analyzer itself.

Have a look at the design/req when you get a chance — flag anything you'd reshape before I lock.

## Rafa:

1. category-granularity. I prefer splitted, I think `leading-tone dim` is more accurate and better for teaching/learning purspose. Agree? If not, stop and tell me
2. I already locked req and marked idea + design done
3. add also `tags` and other info when add the new content. Should we include other fields to have a content auto-described and easy to filter, suggestions?
4. write plan

## AI:

Agreed on **split** — `SecondaryLeadingTone` (`vii°/x`) as its own category, exactly for the teaching clarity you called out. Done:

- **req** — amended (v2) to split `IN5` into `SecondaryDominant` + `SecondaryLeadingTone` and reorder `IN6` precedence, then **re-locked**. (Heads-up: the amend re-opened→re-locked it and bumped the version, so Loom will flag idea/design as "stale vs req" — cosmetic, expected after a post-lock refinement.)
- **design** — synced (added D6 = split; enum, algorithm, and the resolved-decisions section all updated).
- **plan-001** — written: *Harmonic analyzer — engine + golden tests*, 5 steps.

## plan-001 steps

1. **Output types** — `HarmonicCategory` (6, split) · `KeyMode` · `ChordAnalysis` record. → IN4, IN5, C1, C3
2. **Diatonic classification + honest Function** — `Analyze(Chord, Key)`, diatonic detection via `Scale.ForKey`+`DiatonicChord` (triad/7th tolerant), `RomanFunction`-compatible `Function`. → IN1, IN2, IN7, IN9, C1
3. **Non-diatonic classifiers (precedence)** — SecondaryDominant · SecondaryLeadingTone · TritoneSub · Borrowed(+SourceMode) · Chromatic. → IN5, IN6, C4
4. **Minor tonic + sequence API** — symmetric major↔minor; `Analyze(IReadOnlyList<(Chord, Key)>)`. → IN3, IN8, C2
5. **Golden-oracle tests** — inline fixtures incl. the dominant-blues stress test; every category, both modes. → IN8, IN10, C4

Steps use **inline fixtures**, so the analyzer + its tests are fully self-contained — they don't depend on the default-pack content or the `description:` field. That keeps plan-001 clean and lets the content work proceed on its own track once you answer this:

## Your question 3 — content metadata for auto-description + filtering

Today a pack `.dsl` header carries `name` / `genre` / `subgenre` / `tags`, plus the new `description` (1A). For *this* app — a harmonic-teaching tool — the single most valuable *filterable* dimension is **the harmonic concepts a progression demonstrates** (it's the whole dogfood: "show me examples of tritone subs"). Two ways to carry that:

- **(a, my lean) — no new schema; use `tags` as a documented concept + level vocabulary.** Ship `description` (prose) + tag each progression with its concepts and difficulty:
  - concept tags: `ii-V-I`, `turnaround`, `secondary-dominant`, `secondary-leading-tone`, `tritone-sub`, `borrowed-iv`, `modal-mixture`, `diminished-passing`, `circle-of-fifths` …
  - level tag: `beginner` / `intermediate` / `advanced`
  This is **immediately filterable** through the existing Content-page tag filter, zero schema change beyond 1A, and "heard-in" examples ("Lady Bird", "Sweet Georgia Brown") live in the `description` prose.
- **(b) — promote `concepts` (and maybe `difficulty`) to first-class header fields** (`CatalogMetadata` + `CatalogHeader` + entity columns). Cleaner for a dedicated "teach me X" filter UI, but it's a real schema change that only pays off once such a UI consumes it — I'd defer it until the `harmonic-overlay` / a learning-filter UI actually needs the structure (YAGNI).

My recommendation: **(a) now** — `description` + a documented concept/level **tag vocabulary** — and promote `concepts` to a first-class field later, when a UI queries it. If you agree, confirm the tag vocabulary (or tweak it) and I'll do the small `description:` field + add the major-frame progressions with descriptions + tags as a **separate** small unit (its own quick change / mini-plan), independent of plan-001.

## Where we are

plan-001 is ready. Per the stop rule I'll wait for your go before implementing — say **`do step 1`** (or `do plan` to run straight through). Separately, tell me your call on the metadata (a vs b + the tag vocabulary) and I'll queue the content work.

## Rafa:

(a)
I started plan
do plan

## AI:

Got **(a)** — I'll queue the description-field + tagged progressions as the separate unit after this.

Before I write code, though, **`do plan` hit a genuine design decision** (stop-rule 3), and it's the one your own stress test was designed to expose. I'd rather settle it than guess, because it changes both the category taxonomy (step 1's types) and what the golden tests assert.

**The problem.** The design's rule says *"a dominant a perfect fifth above a diatonic degree ⇒ V/that-degree."* Taken literally, in a C blues:

- `C7` (I7): C is a fifth above F (= IV, diatonic) ⇒ the rule labels it **V/IV**.
- `F7` (IV7): its down-a-fifth target is B♭ (♭VII, **not** diatonic) ⇒ not a secondary dominant — falls through.
- `G7` (V7): the strict diatonic dominant ⇒ **Diatonic V**. ✓

So the naive rule labels the **tonic I7 as V/IV** — exactly the "secondary-dominant-of-nothing" over-label IN10 forbids. The design and IN10 are quietly in conflict, and I need your call to reconcile them. Two sub-decisions:

**1. Tonic exclusion (I recommend yes).** The **tonic is never a secondary function** — we don't call I7 "V/IV." This is the standard teaching convention and it's what kills the blues over-label. The rare case where `C7` genuinely functions as V/IV (C → C7 → F) is *resolution-dependent* — it belongs to the deferred sequence-aware pass (EX5), not v1's context-free labeling. Adopt it.

**2. Where do the leftover blues chords go?** With tonic-exclusion, `C7` (I7) and `F7` (IV7) are: not the diatonic quality, not functional secondary dominants, not tritone subs, not borrowed. They *do* each contain a non-diatonic note (♭7 in C7, ♭3 in F7), so they're literally chromatic in strict C major. Options:

- **(1, my lean) — `Chromatic`, honest `Function` preserved (`I7`, `IV7`).** No new category (stays at 6), truthful (they *are* chromatic in strict major), and a consumer can still show "I7" from `Function`. The "it's really a blues/mixolydian idiom" reading is a *mode/context* concern that's out of scope here (a future scale-mode analysis, not this pass).
- **(2) — a dedicated 7th category** like `AlteredDiatonic` / `DominantColor`: pedagogically softer ("your I chord, dominant-flavored"), but it's a schema addition and it blurs into borrowed/modal territory.

I lean **1 + tonic-exclusion**: minimal taxonomy, honest labels, and the blues comes out as `I7 (Chromatic) · IV7 (Chromatic) · V7 (Diatonic)` — none mislabeled as secondary dominants, which is what IN10 asks.

I verified 1 holds across the rest of the fixtures (borrowed `iv`/`♭VII` → Borrowed, `♭II7` → TritoneSub of V, `#iv°`/`#i°7` → SecondaryLeadingTone, real `A7`/`D7`/`E7` → SecondaryDominant V/ii, V/V, V/vi). Only the blues chords needed this ruling.

**Your call:** confirm **1 + tonic-exclusion** (my rec) and I'll run the whole plan straight through — or pick **2** and I'll add the extra category to step 1 first. Stopping here.

## Rafa:

Yes, major/dominant blues are very special harmonic case, no diatonic rules at all
I also lean 1 + tonic-exclusion. I think this should comes out:
`I7 · IV7 (Chromatic) · V7 (Diatonic)`
I wonder even if we detect 17 V7 in context then IV7 become excluded also:
`I7 · IV7 · V7 (Diatonic)`

Note: for harmonic functions, we should consider also that:
49 behave as a 47 
59, 5#9 behave as a 57

## AI:

Ruling locked: **option 1 + tonic-exclusion**. v1 blues reads `I7·IV7 (Chromatic) · V7 (Diatonic)`. Both your notes are on point — here's how I'm folding them in:

**(A) "detect I7 + V7 in context → IV7 also diatonic" (`I7·IV7·V7` all diatonic).** Yes — that's the *right* eventual behaviour, and it's exactly the **sequence-aware** case: you can only call it a blues/dominant tonality by *seeing the whole set* (I7 and V7 present ⇒ the tonic is dominant by idiom ⇒ IV7 is idiomatic too). That needs the neighbours, which v1 deliberately doesn't use (D1 / EX5 — context-free per chord). So: **v1 ships `IV7 = Chromatic`**; the "blues/dominant-tonality detector that folds IV7 (and I7) back to diatonic" is a clean **v2 sequence-aware enhancement**. I've recorded it as a deferred item in the design so it isn't lost — the API already accepts the sequence, so it's additive.

**(B) extensions don't change function (`49`→`47`, `59`/`5#9`→`57`).** Strongly agree, and it's a good principle to bake in now: **function is determined by a chord's third + seventh (the guide tones), not its extensions** — dom9/11/13/7#9/7♭9 all function as a dominant 7, maj9 as maj7, m9/m11 as m7. So I'm writing the classifier to key on the **functional core** (via the `QualityFacets`/`ChordTones` third+seventh), not exact `Quality` equality. Two things worth flagging:
- The domain's `Quality` enum has **none** of those extended qualities yet (only the 11 base ones) — so `49`/`59`/`5#9` aren't even representable today. This is **future-proofing**: when a later domain thread adds extended/altered qualities, the analyzer already classifies them correctly, no change needed. In v1 it's behaviour-neutral (base qualities only).
- I've noted "extended chord qualities (9/11/13/altered dominants)" as a future domain capability in the design — say the word and I'll spin it into its own idea when it's time.

Design updated to record all of this. **Proceeding to implement plan-001 now** (your `do plan` stands — the blocker's resolved), running the 5 steps straight through with build + tests. I'll report back when it's green.

## AI:

**plan-001 is done — green.** Full Core suite **933 passed, 0 failed** (incl. the `MusicLayeringTests` architecture check — the analyzer is a clean Harmony sink). All 5 steps recorded in `done/plan-001-done.md`; the domain-model reference is updated with the new types.

**Shipped** (`Music/Harmony/`): `HarmonicAnalyzer` (`Analyze(Chord, Key)` + the per-position-keyed sequence overload), `ChordAnalysis`, `HarmonicCategory` (6, split), `KeyMode`. Golden tests in `HarmonicAnalyzerTests.cs` (10 cases). Verified end-to-end against every fixture — the dominant blues reads `I7·IV7` Chromatic, `V7` Diatonic exactly as ruled.

A few **implementation calls I made while building** (all defensible, but flagging so nothing surprises you — happy to revisit any):

1. **Honest `Function` uses a fixed major-frame degree table** (♭II/♭III/♯IV/♭VI/♭VII). Nice side effect: a minor key reads `i ii° ♭III iv v ♭VI ♭VII` — the *conventional* Roman spelling — for free, and it stays format-compatible with `ChordSheetBuilder.RomanFunction` (IN9).
2. **Minor keys treat the harmonic-minor V/V7 and leading-tone vii°/vii°7 as diatonic** (natural minor alone would've called the `V` in `iiø–V–i` "Borrowed", which is wrong). So minor `iiø–V–i` → `ii°·V·i` all Diatonic. This is the sensible reading of IN8 "handles minor natively," but it's a real choice — shout if you'd rather keep it strict-natural-minor.
3. **Functional-core classification via `QualityFacets`** (your note B) — a dominant is "major 3rd + ♭7", not exact `Quality.Dominant7`, so future dom9/13/7♯9 analyze as dominants with no change.
4. **TritoneSub v1 = `♭II7` only.** The generalization (a tritone sub of a *secondary* dominant, e.g. `Eb7` = tritone-sub-of-V/ii) is deferred — it needs the same care as the sequence-aware pass. Consequence: in the Tadd Dameron turnaround, `Eb7` comes out `Chromatic (♭III7)` in v1, not TritoneSub. Fine for now; noted as a future refinement.

**Not committed** — I don't commit unless you ask. Say the word and I'll commit (main, per usual).

**Still queued** (separate unit, your decision (a)): the small `description:` catalog-header field + the major-frame progressions added to the default pack with descriptions + a harmonic-concept/difficulty tag vocabulary. Want me to pick that up next, or commit this first?

## Rafa:

commit
then the small `description:` catalog-header field + the major-frame progressions added to the default pack with descriptions + a harmonic-concept/difficulty tag vocabulary
code quick

## AI:

Both done, both committed.

**1. Analyzer** — committed as `af3889d` (source + tests + all the Loom docs).

**2. Content unit** (`code quick`, decisions 1A + (a)) — implemented, green, quick-shipped as **plan-002**:
- **`description:` catalog-header field** — added to `CatalogHeader` (parse/serialize) + `CatalogMetadata.Description`. It's a **recognized header key** now (so it never leaks into the pure Domain parser — that was the trap, since `Parse` treats an unknown `key:` as body), round-trips 1:1, and rides in the stored DSL with no dedicated column. Cross-cutting: every content kind gets it.
- **10 major-frame progressions** added to the default pack, each with a description + concept/difficulty tags: `ii-V-I`, `I-vi-ii-V turnaround`, secondary-dominant turnaround, circle of secondary dominants, tritone-sub `ii-V-I`, Tadd Dameron turnaround, borrowed `iv`, Mixolydian `♭VII`, Aeolian cadence, chromatic passing diminished. Tag vocabulary: concepts (`ii-V-I`, `secondary-dominant`, `tritone-sub`, `borrowed-iv`, `modal-mixture`, `diminished-passing`, `circle-of-fifths`, …) + level (`beginner`/`intermediate`/`advanced`) — immediately filterable via the existing Content tag filter.
- **Verified**: full Core suite **944 passing** — `ProgressionSeedTests` drives every new progression DSL→model→render (Bb), `CatalogHeaderTests` covers the description round-trip. DSL reference updated with the new header key.

**Held for `domain/first-class-minor-keys`**: the minor-tonic progressions (minor `iiø–V–i`, Picardy). **Future** (noted in the harmonic-analysis design): the v2 sequence-aware blues-tonality detector, tritone-subs of secondary dominants, and extended/altered qualities.

That closes out everything from this chat. The three-thread roadmap stands: **1 harmonic-analysis ✅ (engine shipped)** → 2 first-class-minor-keys → 3 harmonic-overlay.