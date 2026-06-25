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
last_ai_block: 3
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