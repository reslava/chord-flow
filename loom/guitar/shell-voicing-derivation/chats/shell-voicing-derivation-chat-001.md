---
type: chat
id: ch_01KVYQ81FZ5EXQ4MV1K8NN143Y
title: shell-voicing-derivation Chat
status: active
created: 2026-06-25
version: 1
tags: []
parent_id: null
requires_load: []
last_ai_block: 35
---
# shell-voicing-derivation Chat

## Rafa:

Read idea
The current idea you wrote says: 
Derive **shell voicings** (root + 3rd + 7th; 5th dropped) **algorithmically from the authored CAGED chord shapes**

But my idea in fact is:
algorithmically from the derived CAGED chord shapes

First of all I need to ground concepts.

### Vocings we have
- authored voinings, imported from packages and .dsl files
- engine derived voicings

We used authored as a golden oracle to verify engine derivation system 
Now we have duplicated voicings coming from 2 sources
What we do with this duplication?

I think we should keep these authored voicings in a different package: `golden-oracle-voicings` that app does not import and only used to verify engine derived voicings.
Then the app should use engine derived voicings directly.

### Sheel voicings

We could just derive them engine derived CAGED voicings
I do not think we need to add shell voicings to `golden-oracle-voicings` package

### TODO

What you think? Agree? Sugestions, improvements?

## AI:

Agreed on the core direction — it's exactly the interval-derivation-engine vision: **authored = golden oracle, engine = the real source.** But after reading the code I want to separate three things you've folded into one question, because two of them are bigger than "shells" and one is a genuinely open design point.

### What the code actually does today (grounding)

- `CagedDerivation.Derive(quality, shape, root, region)` is the engine — pure, no authored fret tables, builds grips from the substrates (octave shapes × interval lattice × hand reach × candidate selector). Its docstring says *"The 34 authored voicings are the golden oracle for Derive"*, and `CagedDerivationOracleTests` enforces that.
- **But the app pipeline does not use the engine yet.** `VoicingBook` is built over `_stored` (the **authored** `VoicingShape`s from the pack). `Candidates`/`Lookup` realize *authored* voicings; `CagedDerivation` output is never wired in. So right now: oracle and engine exist, app still plays authored.

So "we have duplicated voicings from 2 sources" is real, but the app currently consumes only **one** of them (authored). The dedup you want is: **flip the app to consume `CagedDerivation`, and demote authored to a test-only oracle.**

### My pushback: that flip is its own thread, not part of shell-derivation

What you're describing — *app stops importing authored, authored moves to a `golden-oracle-voicings` package used only by the oracle test* — is an architectural change to the **whole voicing pipeline** (`VoicingBook` re-sourced from `Derive`), not specific to shells. Shell-derivation should sit **on top of** that flip, not contain it. I'd recommend a separate thread (e.g. `guitar/engine-derived-as-app-source`) that this one **depends on**. Otherwise shell-derivation balloons into "rebuild the voicing pipeline + shells."

Three things that flip has to solve (and shells inherit the answers):

1. **Type bridge.** `Derive` returns `ChordShape`; the app speaks `Voicing`/`VoicingShape`. The flip needs a `ChordShape → Voicing` adapter (or `VoicingBook` sourced directly from `Derive`). Shells then become `drop5(ChordShape) → Voicing` — a transform on the *engine* output, exactly as you said.

2. **Coverage gating — don't silently regress.** The oracle verifies `Derive` only against the **34** authored grips. The engine can derive *any* quality×shape, but everything beyond those 34 is **derived-but-unverified**. The moment the app stops importing authored, it starts playing those unverified grips. That's acceptable **only if** (a) `Derive` already fails loud when it can't spell a grip (it does — it throws), and (b) we add a structural test: "every quality×shape the app needs derives a valid, fully-spelled grip." The relocation isn't free — it shifts trust from "authored row exists" onto "engine spells it + the 34-grip oracle anchors the algorithm."

3. **Oracle asset location.** `golden-oracle-voicings` is a **test fixture, not shippable content** — it should NOT live in `Content/default-pack`. Keep it in `.dsl` so the same parser reads it, but put it under tests and load it only from `CagedDerivationOracleTests`. The app's content packs are user-facing; the oracle is not.

### On shells specifically (this thread)

- **Derive shells from engine output, not authored** — yes, agreed, and that's the idea-text fix you flagged (line 16/24 say "authored CAGED" — should be "engine-derived CAGED"). I can update the idea doc.
- **Don't add shells to `golden-oracle-voicings`** — agreed, and here's the principled reason: a shell is a *mechanical* transform (drop 5th + doublings, keep root+3+7) of an already-oracle-anchored CAGED grip, so it **inherits** the CAGED oracle's trust. The only shell-specific regression check is the one the idea already names: derived dom7/m7/maj7 shells must match the current `BeginnerShellStrategy` output (3 qualities). Everything else is "valid root+3+7, no throw" + the dogfood fretboard render. No separate shell oracle package needed.

### The one genuinely-open design question (still unresolved)

Idea open-Q #1: **what is a "shell" for a triad** (maj/min/aug — no 7th)? Root+3 only? Or does "shell" apply to 7th-chords and triads fall through to a different source? This is the real fork for this thread and I'd want to settle it in design.

### So, concretely — what do you want?

1. Split the **app-uses-engine-derived + relocate-authored-to-oracle** flip into its own thread that shell-derivation depends on? (my recommendation) Or keep it all inside this thread?
2. Should I update the idea text now ("authored" → "engine-derived" CAGED, and note the dependency on the flip)?
3. Your call on the triad-shell question, or defer it to design?

## Rafa:

1. spin up a new thread for that purpose
2. yes, update the idea
3. shell voicings will only apply for chords with a 7th or a 6th (not yet done)

Spin up also a new thread for derive CAGED 6th voicings, we will implement it before this

We will need to update docs to reflect:
**authored = golden oracle, engine = the real source.** maybe when we finish 1.

## AI:

Done. Here's what I created and wired:

**Two new threads (each with a seed idea):**

1. `guitar/caged-sixth-voicings` — *Derive CAGED 6th voicings*. Extends `CagedDerivation.Derive` to spell maj6/min6 grips. Idea flags the open Qs: does the domain already have `Quality.Major6/Minor6` (or is it a prereq like dim7/chromatic-degrees), which shapes get authored 6th oracle anchors, and how the selector prioritizes the 6 vs. the 5 in a 4-finger box.
2. `guitar/engine-derived-as-app-source` — *Engine-derived voicings as the app's source (authored → oracle)*. The flip: re-source `VoicingBook` from `Derive`, add the `ChordShape → Voicing` bridge, relocate authored grips to a test-only `golden-oracle-voicings` fixture, add the coverage-gating structural test. I also noted the **doc updates** (domain-model + architecture refs: "authored = oracle, engine = source") as deliverables of *this* thread — so they land when the flip lands, matching your "maybe when we finish 1."

**Dependency graph:** `shell-voicing-derivation` now **depends_on** both new threads (engine-as-source + 6th-voicings). 6th-voicings and the flip are left independent of each other — say if you want 6th to also depend on the flip.

**Shell idea updated** (id_01KVYQ3DY08RT6KGK50X0PPEGR, v2):
- "authored CAGED" → **"engine-derived CAGED"** throughout (derives from `Derive` output, not authored grips).
- Triad question **resolved**: shells apply only to chords with a **7th or a 6th**; triads (maj/min/aug) are out of scope and route to another source. Moved into a "Resolved design decisions" section; the remaining open Qs are now just contiguity/string-set, replace-vs-augment `BeginnerShellStrategy`, and where derivation runs.
- Added the dependencies + the note that shells are **not** added to the golden-oracle package (they inherit trust from the CAGED oracle; only the dom7/m7/maj7 `BeginnerShellStrategy` regression check applies).

Suggested build order: **6th-voicings + the flip first** (parallel), then **shell-derivation** on top. Want me to draft reqs/designs for either new thread next, or leave them as ideas for now?

## Rafa:

These
1. `guitar/caged-sixth-voicings`
2. `guitar/engine-derived-as-app-source`

has been already shipped
so we are ready to continue with this thread, sheel voicings derivation
correct? 

### Open design questions (idea doc line 50)

1. When dropping the 5th leaves a **non-contiguous** shape, how is the playable subset (string set) chosen? mutted strings included, natural sheel voicings grips for E shape
2. Does the derived shell **replace** `BeginnerShellStrategy`, or become a new derivation source the book consults?
3. **Where** does derivation run — at book-build time, or on demand in the strategy?

2 and 3 arise how are we going to use voicings.
we have 3 sources, automatic, user, package
we should have families, genre, difficuly... 
a voicing should have a clear ID and slug ID to be easy and clearly refer by user 

a progression and / or song could be renderer by picking up:
- difficuly loom/guitar/voicing-difficulty-bands/voicing-difficulty-bands-idea.md
- automatic using voicings raking strategies, draft idea loom/guitar/voicing-ranking-strategies/voicing-ranking-strategies-idea.md
- user specific assigned to whole song, a section, or specific chord draft idea for this: loom/guitar/explicit-voicing-reference/explicit-voicing-reference-idea.md

### Families

`family` or whatever better name you suggest

- `doubled shell voicings` the ones define by the idea, just strip 5
- `shell voicings` will be 1 (main root) 3 (7 or 6) no doubled/repeated intervals

### Tags

We could include other classifications by for example using tags

- `common` indicate these vocinings are commonly used, assign to shell derived from CAGED E, C
- `extended` (or alternative) indicate these vocinings are rarely used, assign to shell derived from CAGED A, G, D

### TODO

What do you think?
Agree? suggestions? improvements? better approachs? better approachs for voicings classification, identification? better approachs for voice picking for song, progression render?

## AI:

**Yes — verified in code, both shipped, and this thread is unblocked:**

- `Quality.Major6 / Minor6` exist; `CagedVoicingCatalog` has **46** combos (8 five-shape qualities ×5 incl. 6ths + m7♭5/dim7 ×3).
- The flip is live: `EngineVoicingSource` (lists `automatic` families), `AutomaticVoicingId` (`auto:{token}:{shape}`, e.g. `auto:dom7:E`), and **`CompingResolver`** is now the render-time picker — main source + fallback `user > package > automatic`, `automatic` candidates derived on the fly via `CagedDerivation.Derive` over the catalog, ranked by `ClosestRanking`. The renderer is a pure formatter over the `CompingPlan`.

So we're good to build shells **on top of `CompingResolver` + the catalog**, not on the old path. That reframes your Q2/Q3 — see below.

## Q2 + Q3 are mostly already answered by the flip

The "book-build vs strategy / replace `BeginnerShellStrategy`" framing is **obsolete** — the flip replaced that whole path:

- **Q3 (where derivation runs):** on demand at **resolve time**, region-aware, exactly like full CAGED chords today (`CompingResolver.AutomaticCandidates` → `Derive` → `ChordShapeVoicing.ToVoicing`). A shell is just one more transform on the derived `ChordShape`. Not book-build, not a strategy.
- **Q2 (replace `BeginnerShellStrategy`?):** **Yes, retire it.** `BeginnerShellStrategy`/`IVoicingStrategy`/the old `VoicingBook` are still referenced (`GuitarInstrument`, `VoicingStore`) but they're legacy after the flip — the live render path no longer goes through them. Shells become a derived `automatic` **family**, not a strategy. We keep `BeginnerShellStrategy`'s 3 grips only as the regression-oracle **test fixture** (idea Validation). Confirming nothing live still calls that path is a step in this thread.

## Q1 (non-contiguous) — agreed

Keep muted strings; derive the shell **per CAGED shape**, preserving that shape's natural string set. Dropping the 5th **mutes its string(s)**, it doesn't collapse strings together — so we get the natural shell grips (the classic E/A/C-shape shells). Simple and correct.

## Families — the real design point, and it forces an identity change

Today `auto:dom7:E` implicitly means *the full CAGED chord*. Shells introduce **multiple products per quality×shape**, which is a derivation chain (each a cut point on the same derived `ChordShape`):

```
full CAGED  ⊃  doubled shell (drop 5, keep doublings)  ⊃  shell (R, 3, 7|6 — no doublings)
```

So I agree with your two families, with naming tweaks:
- **`shell`** — pure R + 3 + (7|6), no doublings. Keep this name; it's the standard jazz term.
- **`doubled-shell`** (your "doubled shell voicings") — drop 5 only. Fine; alt names if you dislike it: `drop5` / `guide-tone-CAGED`.
- I'd also make the existing full chord an explicit family **`caged`**, so all three are uniform rather than one being implicit.

**This forces the identity to gain a family segment:** `auto:{family}:{token}:{shape}` → `auto:caged:dom7:E`, `auto:dshell:dom7:E`, `auto:shell:dom7:E`. Note your own [[explicit-voicing-reference]] idea already wrote `{a: shell-C6}` — it *assumed* a shell family in the id. So this is needed and consistent. **Recommendation:** fold the family-id extension into *this* thread (shells are the forcing function; the change is small — extend `AutomaticVoicingId` + add family rows to the catalog/listing), rather than a separate identity thread. Implement the two cut points as **one pipeline**, not two unrelated derivers.

## Tags — agree, with one principle

Tags are the right vehicle for **orthogonal, non-derivable** classification (genre: blues/jazz/bossa; mood; technique). But `common`/`extended` keyed to CAGED shape (E,C vs A,G,D) is a **pure function of the shape** — and `CagedShape` already has `FamiliarityRank()`. Per the codebase's "computed, never stored" rule: **derive** common/extended from shape familiarity; don't stamp it as a stored tag on every voicing. Reserve tags for facts that aren't derivable. (Same spirit as keeping `automatic` voicings computed, never in SQLite.)

## Voice picking for render — they compose into one pipeline, not 3 pickers

The three mechanisms you listed aren't alternatives; they're **layers of one resolution pipeline**, and `CompingResolver` is already the seam:

```
per chord:
  explicit reference  ({u:}/{a:}/{c:})         ← OVERRIDE (highest)   [explicit-voicing-reference]
  else:
    candidates = automatic(source, family)
                 FILTERED BY difficulty band    ← CONSTRAINT          [voicing-difficulty-bands]
    pick = rankingStrategy.Pick(candidates)     ← SELECTION           [voicing-ranking-strategies]
```

Key insight: **difficulty is a *filter on the candidate family*, not a 4th picker** — e.g. Beginner ⇒ "comp with the `shell` family," Advanced ⇒ full `caged`. That's exactly why this thread matters: **the shell family is what makes the Beginner difficulty band meaningful.** Explicit reference is an *override before* ranking. ClosestRanking already exists; difficulty-filter + family-filter slot in as candidate constraints.

## So, concretely — decisions I need from you

1. **Family naming:** `shell` + `doubled-shell` (+ make full chord an explicit `caged` family)? Or different names?
2. **Identity:** extend `auto:{token}:{shape}` → `auto:{family}:{token}:{shape}`, and fold that into *this* thread? (my rec)
3. **common/extended:** agree to **derive** from CAGED-shape familiarity, and keep `tags` for genre/non-derivable only?
4. **Picking pipeline:** agree difficulty = candidate **filter** (Beginner⇒shell family) and explicit = **override**, both around the existing ranking seam?

Once these are settled I'll write this thread's `req` + design.

## Rafa:

Thank you, you’ve really helped me sort out a bunch of ideas!
Agree all.
Write design, req

## AI:

Written and saved.

- **Design** — `shell-voicing-derivation-design.md` (active). Captures the chat decisions as D1–D9, the architecture (shell = a pure `ShellReduction.Reduce(ChordShape, family)` over the existing `Derive` output → `ToVoicing`), the family/identity model, the `CompingResolver` integration with `caged` fallback, the regression oracle, and the picking-pipeline context.
- **Req** — `req.md` (**locked**). IN1–IN12 / EX1–EX7 / C1–C7, each citing the design decisions, ready for the plan to cite handles.

Three calls I made while writing (all derived from your approved principles — flag any you'd change):
1. **D3 is breaking** — `auto:{token}:{shape}` is removed, not shimmed (`auto:caged:dom7:E` etc.). Safe: ids are computed, nothing persists them, and your `explicit-voicing-reference` draft already assumed a family segment.
2. **D7 — family is a preference**: a triad under the `shell` family falls back to `caged` for that chord (since triads have no shell), before the source fallback chain.
3. **D8 — retire the legacy path**: `BeginnerShellStrategy`/`IVoicingStrategy`/dead `VoicingBook` removed from production; the old 3-grip logic survives only as the A-shape-shell regression oracle.

**Next step:** the implementation **plan** (steps citing the locked req handles). Want me to write it?

STOP — waiting for go.

## Rafa:

Write plan

## AI:

Plan written — `plans/shell-voicing-derivation-plan-001.md` (draft). Idea + design marked **done**; req stays the locked anchor. 9 steps, every step citing req handles:

| # | Step | Satisfies | Blocked by |
|---|------|-----------|------------|
| 1 | VoicingFamily + ShellReduction (the pure reducer + unit tests) | IN1–IN4, C1, C3, C5 | — |
| 2 | 4-segment AutomaticVoicingId | IN5 | — |
| 3 | Catalog family dimension | IN3, IN6, C7 | 1 |
| 4 | Resolver family knob + caged fallback | IN7, C2, C4 | 1, 3 |
| 5 | Listing source + derived common/extended | IN8, C7 | 2, 3 |
| 6 | Regression oracle + coverage tests | IN10 | 1, 3, 4 |
| 7 | Retire BeginnerShellStrategy/VoicingBook | IN9 | 6 |
| 8 | Ref-sync (domain-model) | IN11 | 4 |
| 9 | Dogfood fretboard render | IN12 | 4 |

Order rationale: the pure reducer (1) is the keystone everything else hangs off; identity (2) is independent; catalog (3) feeds resolver (4); the oracle (6) preserves the old grip logic **before** we retire it (7); ref-sync (8) + dogfood (9) close out. Step 1 is the natural first slice — pure, no dependencies, fully unit-testable.

**Next step:** start the plan and implement **Step 1 — VoicingFamily + ShellReduction** (`VoicingFamily.cs`, `ShellReduction.cs`, `ShellReductionTests.cs`).

STOP — waiting for go.

## Rafa:

I started the plan
Do 1-8

## AI:

⚠️ **Stopping on a design finding before writing code** — it breaks the regression oracle (step 6) and tests a premise. I verified against the real engine output (the CAGED oracle table in `CagedDerivationOracleTests` records exactly what `Derive` produces), reducing each grip by hand.

## The finding: a "pure reduction" gives *correct* shells, but not the *textbook* shells — and they don't match `BeginnerShellStrategy`

`BeginnerShellStrategy` is the canonical movable shell: **root on A(s5), 3rd on D(s4), 7th on G(s3)** — three adjacent strings. But the engine voices the guide tones on whatever strings the CAGED box puts them, which varies per shape. Dom7 (root C), pure shell = root+3rd+♭7, drop 5th+doublings:

| Shape | Derived full grip (s6→s1) | Pure-shell reduction | vs textbook shell |
|------|---------------------------|----------------------|-------------------|
| C | `x 3 2 3 1 3` | `x 3 2 3 x x` (s5,4,3) | **matches `BeginnerShellStrategy`** |
| G | `8 7 8 5 8 8` | `8 7 8 x x x` (s6,5,4) | clean, but low (root on E string) |
| E | `8 10 8 9 8 8` | `8 x 8 9 x x` (s6,4,3) | the standard *gapped* E-form shell |
| A | `x 3 5 3 5 3` | `x 3 x 3 5 x` (s5,3,2) | gapped, 3rd up on the B string |
| D | `x x 10 12 11 12` | `x x 10 x 11 12` (s4,2,1) | gapped |

Two problems this exposes:

1. **The regression oracle is invalid as specified.** I wrote D8/IN10 as "A-shape shell == `BeginnerShellStrategy`." Wrong on two counts: (a) the grip that matches is the **C-shape**, not the A-shape; and (b) even that only holds for **dom7 and min7** — for **maj7** the C-shape voices the maj7 on the open B string (`x 3 2 x 0 x`), so it does *not* match `BeginnerShellStrategy`'s `x 3 2 4 x x`. So there is no clean "derived shell == old strategy" oracle across the three qualities.

2. **Derived shells vary in ergonomics** — some are clean 3-adjacent grips (C, G shapes), others are gapped with a muted middle string (A, E, D). All have the *right notes* (root + 3rd + 7th/6th), but they are **not** the uniform compact movable shell a player expects. You already blessed gaps for the E shape (Q1); the question is whether you accept them everywhere.

The root cause: a textbook shell is its own *re-voiced* compact grip; it is **not** a subset of the full CAGED chord. Filtering the CAGED chord keeps the guide tones *where the chord already put them*.

## Options

- **A (recommended) — accept derived shells; drop the `BeginnerShellStrategy` oracle.** Shells are the CAGED chord minus 5th/doublings, gaps and all. Validate structurally instead: every shell has exactly {root, 3rd, 7th|6th}, fifth muted, no throw — and they inherit the CAGED golden oracle's trust (the notes are already oracle-verified). Retire `BeginnerShellStrategy` outright (no longer an oracle, just deleted). Honest, matches the "pure reduction" premise, zero new authoring. Cost: shells aren't the uniform compact shape; the Beginner band gets correct-but-sometimes-gapped grips.
- **B — keep a hand-authored "compact shell" as a separate family.** Treat the textbook movable shell as its own authored product (what `BeginnerShellStrategy` is), distinct from the CAGED-derived shell. Contradicts the thread's goal (reintroduces hand-authored shells) — I don't recommend it.
- **C — make shell derivation *re-voice* to a compact guide-tone grip** (pick the octave/strings that yield 3 adjacent strings), not a pure filter. Biggest change: it becomes its own voicing algorithm, not a reduction — but it would produce the shapes players expect and could subsume `BeginnerShellStrategy` for real.

My recommendation is **A** for this thread (keep the reduction pure and honest), with **C** as a possible future "compact-shell" refinement if the gapped grips bother you in dogfood. Either way the plan's step 6 needs rewording (structural validation, not the BeginnerShellStrategy oracle) and D8/IN10 need amending.

How do you want to proceed?

STOP — waiting for your call.

## Rafa:

I can’t even remember `BeginnerShellStrategy oracle`. Please remind it briefly.

I can not see / compare clearly the shell / doubled-shell / BeginnerShellStrategy. Please crate a table comparing the cases when they does not match.

### ABout FamiliarityRank

You said:
>FamiliarityRank orders E,A,G,C,D — but Rafa's common/extended split is specifically {E,C}=common, {A,G,D}=extended (not a familiarity threshold).

My split was justa a proposal related to shell, doubled-shell voicings, if you have better approach, pls tell me

## AI:

### 1. What `BeginnerShellStrategy` is (reminder)

It's the **current hardcoded MVP beginner voicing** — the thing this whole thread replaces. One **movable 3-note shape**: root on the A string (s5), 3rd on the D string (s4), 7th on the G string (s3). It covers **only dom7 / min7 / maj7** and throws on everything else. It slides to any key. It is the only "shell" source in the app today. "The `BeginnerShellStrategy` oracle" just meant: *use its 3 known-good grips as a regression check that the derived shells reproduce them* — which is the idea I'm now saying doesn't hold up.

### 2. Comparison — where they differ (root = C, frets low-E→high-E)

`BeginnerShellStrategy` is a **single fixed grip per quality** (always root-on-A). The only derived shape that even sits in the same place (root on A=s5) is the **C-shape**, so that's the fair head-to-head:

| Quality | `BeginnerShellStrategy` | C-shape **shell** (drop 5th + doublings) | C-shape **doubled-shell** (drop 5th only) | shell == strategy? |
|--------|------------------------|------------------------------------------|-------------------------------------------|--------------------|
| Dom7 | `x 3 2 3 x x` | `x 3 2 3 x x` | `x 3 2 3 1 x` (extra root s2) | ✅ |
| Min7 | `x 3 1 3 x x` | `x 3 1 3 x x` | `x 3 1 3 1 x` (extra root s2) | ✅ |
| Maj7 | `x 3 2 4 x x` | `x 3 2 x 0 x` (maj7 on s2, gap s3) | `x 3 2 x 0 0` | ❌ — 7th lands on a different string |

And for **every other CAGED shape**, the derived shell isn't even in the same register as `BeginnerShellStrategy` (different root string), so "match" isn't meaningful — dom7 examples:

| Shape | derived **shell** | derived **doubled-shell** | shape of grip |
|------|-------------------|---------------------------|---------------|
| C | `x 3 2 3 x x` | `x 3 2 3 1 x` | clean, root-on-A (= strategy) |
| A | `x 3 x 3 5 x` | `x 3 x 3 5 x` | gapped (3rd up on B) |
| G | `8 7 8 x x x` | `8 7 8 x 8 8`… (roots on s3/s1) | clean, root-on-low-E |
| E | `8 x 8 9 x x` | `8 x 8 9 x 8` | gapped (standard E shell) |
| D | `x x 10 x 11 12` | `x x 10 x 11 12` | gapped |

So: **shell vs doubled-shell** differ only by the *doublings* (extra root/3rd strings the doubled-shell keeps). **shell vs `BeginnerShellStrategy`** match only for **C-shape dom7 & min7** — maj7 and all other shapes diverge. That's why the regression oracle as written is dead.

### 3. common / extended — your split was a proposal; here's a more principled one

Agreed it was just a sketch. Two better moves:

- **Better rule (if we keep it):** classify by **root string**, not a hardcoded shape set. Shells whose root sits on the low-E (s6) or A (s5) string are the ones players actually learn first → `common` = E, G, C, A shapes; `extended` = D shape (root on the D string). That's *derivable* from the shape's root string, and musically defensible, unlike `{E,C}`.
- **My actual recommendation: drop common/extended from this thread.** Nothing consumes it yet (the UI/selection that would show it is step 9 / sibling threads). Building a classification with no reader is exactly the "don't add substrate nobody uses" trap. Defer it to whenever the selection UI or a tags thread needs it, then derive it (root-string rule). I'd cut IN8 down to just the listing rows + family-qualified display names.

### Net

The notes are always right (oracle-verified CAGED tones); what varies is *string placement*. My recommendation stands: **Option A** — accept the pure-reduction shells, replace the `BeginnerShellStrategy` regression oracle with structural validation (exactly {root, 3rd, 7th|6th}, fifth muted, no throw), retire `BeginnerShellStrategy`, and defer common/extended. If you'd rather players get uniform compact shells, that's **Option C** (re-voicing) and a bigger, separate piece.

Which way — A (and I fix step 6 + IN8/IN10 + D8), or C?

STOP — waiting for your call.

## Rafa:

### 1. What `BeginnerShellStrategy`

Thanks for the reminder. It looks there are only 3, I think `BeginnerShellStrategy` should include also E shape, and 6, m6 and dim7 to be usefull:
| Shape | Quality | `BeginnerShellStrategy` |
|-------|---------|------------------------|
| C     | Dom7 | `x 3 2 3 x x` |
| C     | Min7 | `x 3 1 3 x x` |
| C     | Maj7 | `x 3 2 4 x x` | <- This is important and actual derived fails
| C     | dim7 | `x 3 1 2 x x` | 
| C     | 6    | `x 3 2 2 x x` | 
| C     | m6   | `x 3 1 2 x x` | <- m6 = dim7
| E     | Dom7 | `8 x 8 9 x x` |
| E     | Min7 | `8 x 8 8 x x` |
| E     | Maj7 | `8 x 9 9 x x` |
| E     | dim7 | `8 x 7 8 x x` |
| E     | 6    | `8 x 7 9 x x` |
| E     | m6   | `8 x 7 8 x x` | <- m6 = dim7

note: I hope I made no mnistakes

### 2. Comparison 

(A) We can refine the current derivation strategy in order to achieve at least C shape Maj shell voicing

Or we can change the strategy:

(B) Try new shell derivation

Instead of drop 5 from the corresponding CAGED derived chords

Derive with the engine the shell shord formula.
1 rule for E shape, skip s5 (new)
1 rule for 6 m6 E, A shapes, 1-behind (I think this is already included)
1 rule for maj7 C shapes, 1-forward (new)

apply having into account `shell` and `doubled-shell`

### 3. common / extended

drop it for now, no added value

### TODO

What do you think?
Agree? (A), (B), both? other you suggest?

## AI:

I verified all 12 of your grips against the fretboard — **correct, no mistakes.** And your "m6 = dim7" note is exactly right: both are C–E♭–A (the m6's `6` and the dim7's `bb7` are the same pitch, semitone 9), so the grips are identical. Nice.

Your table changes my mind about the whole approach. Here's where I land:

## (A) doesn't work as a patch — it *is* (B)

To make C-shape **maj7** come out as `x 3 2 4 x x`, the maj7 must sit on the **G string (s3)** — but the full C-shape CAGED chord voices its maj7 on the **open B string (s2)**. You can't get there by filtering the chord; you have to **re-voice**. So "refine the reduction" collapses into "derive the shell directly." → **(B) is the real answer.** I don't recommend (A).

## (B), reframed — and it actually resolves everything

Your table reveals shells aren't "5 CAGED shapes minus the 5th." They're **2 canonical forms**:
- **5th-string-root** (you call it "C"): root s5, guide tones on s4+s3.
- **6th-string-root** (you call it "E"): root s6, guide tones on s4+s3, **skip s5**.

In both forms the guide tones live on the **same two strings (D+G = s4+s3)**; only the root string moves. That's the whole pattern — plus your reach rules (E skip-s5; maj7 forward-1; dim7/6/m6 behind-1). So **shell = derive {root, 3rd, 7th|6th} onto the form's 3 strings**, and **your 12 grips become the shell golden oracle** — restoring a real oracle (richer than the dead 3-grip one), exactly the way authored CAGED chords are the oracle for `CagedDerivation`. The engine vision holds: we *derive*, your table *verifies*.

## This lets us keep your original "strip-5" idea too — as a different family

The reduction I was worried about isn't wrong; it's just not a *compact shell*. It's a **chord minus the 5th** — a fuller comping voicing. So I propose a clean 3-family model where each family is its own product:

| family | what it is | how it's derived | shapes | oracle |
|--------|-----------|------------------|--------|--------|
| `caged` | full chord | `CagedDerivation` (exists) | 5 | authored CAGED (exists) |
| `doubled-shell` | chord **minus the 5th** (keeps doublings; gapped is fine) | pure reduction of `caged` | 5 | inherits the CAGED oracle (structural check) |
| `shell` | compact 3-note guide-tone shell | **new 2-form shell derivation** (B) | 2 (5th-root, 6th-root) | **your table** (12 grips) |

This honors *both* of your ideas: "drop the 5" lives on as `doubled-shell`; the textbook compact shell is `shell`, derived properly and oracle-verified.

## Two things I need from you

1. **Agree to the 3-family model above?** (`shell` = compact 2-form, derived, your-table oracle; `doubled-shell` = chord-minus-5th reduction; `caged` = full.) Or do you only want `shell` and drop `doubled-shell`?
2. **Shell forms in the id:** reuse `CagedShape` as the segment — `auto:shell:maj7:C` (5th-root) / `auto:shell:dom7:E` (6th-root) — matching your table's labels? Or introduce a distinct `ShellForm {FifthRoot, SixthRoot}`? (I lean: reuse C/E, it reads well and needs no new enum.)

Heads-up: this **revises the just-written design + req + plan** — `shell` becomes a derivation-with-oracle (not a reduction), the catalog/oracle/step-6 change, and IN8's common/extended is dropped. More work than the reduction, but it's the correct thing and gives us a true oracle. I'll redo all three docs once you confirm 1 and 2.

STOP — waiting.

*(tiny English note: "shell shord formula" → "chord"; "usefull" → "useful". Just so you catch them — content was clear.)*

## Rafa:

1. yes, agree
2. reuse C/E

## AI:

Design doc revised to the 3-family / 2-form model (the 12-grip oracle table is in there, and the `ShellDerivation` algorithm — I traced it against all 12 of your grips by hand and it reproduces them exactly). Here's the **revised plan** for your go:

| # | Step | Key files | Oracle/test |
|---|------|-----------|-------------|
| 1 | `VoicingFamily` enum + `ShellReduction.MuteFifth` (doubled-shell) | VoicingFamily.cs, ShellReduction.cs | unit |
| 2 | **`ShellDerivation`** — the 2-form compact shell deriver | ShellDerivation.cs | unit |
| 3 | `AutomaticVoicingId` 4-segment (shell shape ∈ {C,E}) | AutomaticVoicingId.cs | unit |
| 4 | `CagedVoicingCatalog` family dimension (caged×5, dshell×5, shell×2) | CagedVoicingCatalog.cs | unit |
| 5 | `CompingResolver` family dispatch + `Family` knob + caged fallback | RenderOptions.cs, CompingResolver.cs | unit |
| 6 | `EngineVoicingSource` family listing rows | EngineVoicingSource.cs | unit |
| 7 | **Shell golden oracle (your 12 grips)** + doubled-shell structural + catalog coverage + caged no-regression | tests | the spec |
| 8 | Retire `BeginnerShellStrategy`/`IVoicingStrategy`/`VoicingBook` | (deletions) | — |
| 9 | Ref-sync `chordflow-domain-model-reference` | refs | — |
| 10 | Dogfood: render shells on the fretboard UI page | caged-chords.js | visual |

Changes from the old plan: split the reducer (step 1) from the new **`ShellDerivation`** (step 2); step 7's oracle is now your 12-grip table (the old `BeginnerShellStrategy` oracle is gone); `common`/`extended` dropped from step 6.

Your earlier "Do 1-8" was "everything but dogfood" — on this plan that's **steps 1–9** (8 = retire, 9 = ref-sync; 10 = the UI dogfood). 

Go to amend the req, recreate the plan, and implement 1–9? (Or tell me a different range.)

## Rafa:

Yes

## AI:

**Steps 1–9 done — all green (734 tests pass, solution builds).** Step 10 (the fretboard dogfood UI) is the only one left; it was outside your "1–9" range.

What landed:

| # | Step | Result |
|---|------|--------|
| 1 | `VoicingFamily` + `ShellReduction.MuteFifth` (doubled-shell) | ✅ |
| 2 | `ShellDerivation` (2-form compact shell) | ✅ |
| 3 | `AutomaticVoicingId` → 4-segment (`auto:shell:dom7:E`) | ✅ |
| 4 | `CagedVoicingCatalog` family dimension (91 combos) | ✅ |
| 5 | `CompingResolver` family knob + `caged` fallback | ✅ |
| 6 | `EngineVoicingSource` family listing | ✅ |
| 7 | **Shell golden oracle — your 12 grips all reproduced** + coverage | ✅ |
| 8 | Retired `BeginnerShellStrategy`/`IVoicingStrategy`/`VoicingBook` | ✅ |
| 9 | Ref-sync (domain-model) | ✅ |

Highlights:
- **`ShellDerivation` reproduces your 12-grip table exactly** (`ShellOracleTests`, the new golden oracle). The "nearest-to-root" placement gives the maj7-forward / dim7·6·m6-behind grips for free — no special-casing.
- Three families wired end-to-end: `caged` (full), `dshell` (chord-minus-5th), `shell` (compact). Resolver defaults to `caged` (zero render regression) and falls back to `caged` for chords with no shell (triads).
- **Retire** removed the dead `VoicingBook`/strategy cluster. `GuitarInstrument` kept (Diagram + ResolveLead); the old movable-shell logic survives only as a **test fixture** for the renderer's byte-identical formatting tests.
- One thing worth flagging from step 8: the seed-render tests started exercising `dim7` (a build refreshed the on-disk default pack into the test bin), which the retired shell never covered — I made the renderer test helper hybrid (shell for dom7/min7/maj7, engine caged grip otherwise). Not a production change; just test infra.

**Remaining: Step 10 — dogfood the shell families on the fretboard UI page.** That's the guitar-weave dogfood rule (visual confirm in the real app). Want me to do step 10 now, or is that a separate session? Nothing is committed yet — say the word and I can also commit 1–9.

STOP — waiting.

## Rafa:

Excellent!

Problem: CAGED chords page does not include a family selector, I can not verify

## AI:

**Step 10 done — the CAGED Chords page now has a Family selector.** Full solution builds (0 errors), all 734 tests still green.

What I wired (the dogfood path, end to end):
- **`index.html`** — a new **Family** dropdown before Shape.
- **`caged-chords.js`** — `caged` / `doubled shell` / `shell` options; sends `family` in the `cagedChordPreview` message. When you pick **shell**, the Shape selector narrows to **C / E** (the only two shell forms); switching back to caged/doubled-shell restores all five.
- **`WebMessageRouter`** — `cagedChordPreview` now carries `family`; the event widened to `(family, shape, quality, rootPc)`.
- **`Program.cs`** — forwards `family`; also catches the shell's form/eligibility errors → inline error.
- **`CagedChordHandler.Preview(family, quality, shape, root)`** — dispatches through `FamilyVoicing.Derive`, so the page renders whichever family you pick.

**To verify:** reopen the app → CAGED Chords tab. Try:
- **shell · dom7 · C** → `x 3 2 3 x x`; **shell · maj7 · C** → `x 3 2 4 x x`; **shell · dom7 · E** → `8 x 8 9 x x` (your oracle grips).
- **doubled shell · dom7 · C** → the chord with the 5th muted (`x 3 2 3 1 x`).
- **shell · maj** (a triad) → inline error ("no 7th or 6th"); **shell** with shape forced to A/G/D isn't offered (selector hides them).

That completes the whole plan (steps 1–10). Nothing is committed yet — want me to **commit 1–10** (and close the plan), or are you going to eyeball the page first?

## Rafa:

This has been another great thread!
After running the app, these are my conclusions:
1. Keep only shape C of `doubled-shell` family (dom7, dim7, 6, m6), drop/delete others. C has sense becuase some voicings doubling the root are commonly used, like C7 or C6. Agree?
2. Shell Shape C maj 7 root A renders `x 0 11 1 x x` and should be `x 12 11 13 x x`, it looks that there is a bug when root is in fret 0 for `shell` family C and E, check it

## AI:

Both addressed — build clean, **735 tests pass**, app rebuilt (reopen to re-verify).

**2. Open-root bug — fixed.** Root cause: when the root fell on an open string (fret 0), the guide tones couldn't sit near it, so `NearestFret` grabbed the only occurrence ~11 frets up → `x 0 11 1`. Fix: `ShellDerivation` now anchors the root at the **lowest *compact* placement** (it tries each root occurrence ascending and takes the first whose grip spans a hand; an open-string root is pushed an octave up). So:
- `shell · maj7 · C · A` → **`x 12 11 13 x x`** ✓ (your expected)
- `shell · dom7 · C · A` → `x 12 11 12 x x`
- Added a regression test (`OpenStringRoot_AnchorsTheCompactGripUpAnOctave`). The oracle grips (root C, already compact low) are unchanged.

**1. doubled-shell curation — agreed, done.** Now **C form only, for dom7 / dim7 / 6 / m6** (4 combos) — exactly the commonly-played doubled-root voicings; the other shapes/qualities are dropped. Catalog is now **64 combos** (caged 46 + doubled-shell 4 + shell 14). I verified your dim7 case: even though the *caged* family offers dim7 only on A/E/D, the engine **does** derive a clean C-form dim7, so `dshell · dim7 · C` works (confirmed by the coverage test over all 64). The CAGED page also now filters Shape **and** Quality per family, so doubled-shell only offers C + those four qualities.

Docs synced: req v3 (re-locked, IN6 curation + IN13 compact-anchor), design + domain-model ref updated to 64 combos.

Quick re-check list when you reopen: `dshell` shows only C + {dom7,dim7,6,m6}; `shell · maj7 · C` at root A = `x 12 11 13`.

Still nothing committed. Commit everything (the whole thread) to `main` once you're happy with the page?