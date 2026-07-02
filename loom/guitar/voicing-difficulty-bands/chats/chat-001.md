---
type: chat
id: ch_01KVYP2755CC53SSBM50NYK2HY
title: voicing-difficulty-bands Chat
status: active
created: 2026-06-25
version: 1
tags: []
parent_id: null
requires_load: []
last_ai_block: 5
---
# voicing-difficulty-bands Chat

## Rafa:

read idea and global context
is fully grounded?
what we need before design?

## AI:

Read the idea and global ctx, and checked the idea's claims against the actual code.

## Is it grounded?

Yes — the three factual claims are accurate, I verified each:

1. **Difficulty is a no-op in `VoicingBook`.** `Candidates(chord, difficulty)` never reads `difficulty` (the param's own doc comment: *"reserved for the deferred difficulty-band narrowing (req EX6); it does not filter in slice 1"*). ✓
2. **Authored shadows generated.** `Lookup` returns `candidates[0]` whenever any authored voicing matches; the strategy only runs when there are zero candidates. So for any chord the default pack authored a full CAGED chord for, the shell can never win → the Difficulty control changes nothing. ✓
3. **`BeginnerShellStrategy` throws on dim7.** It covers `Dominant7`/`Minor7`/`Major7` only; everything else hits `throw new NotSupportedException(...)`. dim7 → throw. ✓

One extra thing I found that the idea doesn't call out: only a **Beginner** strategy is registered (`DefaultStrategies` = `{ BeginnerShellStrategy }`). So `Intermediate`/`Advanced` + no authored voicing also throws today. "Richer bands unlock fuller voicings" needs a defined behaviour, not just an unlock.

## What we need before design

The idea names the crux ("difficulty band vs authored-shadows-generated") but a few inputs are still undecided. These are the design questions to settle first:

1. **Where does a voicing's difficulty band come from?** This is the core gap — `VoicingShape` has **no band/difficulty field today**. Three options:
   - (a) **Author** a band tag per voicing (new token in the voicing DSL + field on `VoicingShape`).
   - (b) **Compute** it from the shape (fret span, barre y/n, finger count, muted strings).
   - (c) **Heuristic from CAGED shape** (reuse the existing familiarity ranking).
   The thread is literally named *difficulty-bands*, so this decision is the spine of the design.

2. **Precedence rule (the crux).** Does difficulty *narrow* the authored candidate list, or does it decide *authored vs strategy*? Concretely: at Beginner, should the generated shell beat an authored full-chord voicing? Today authored always wins, which is exactly why Beginner does nothing.

3. **"Beginner = shells" when no authored shell exists.** The default pack authors full CAGED chords, not shells. So either Beginner *filters those out* and lets `BeginnerShellStrategy` generate the shell, or we *author* shell voicings tagged Beginner. Which path?

4. **Intermediate / Advanced behaviour.** No strategy exists for them. Do richer bands simply stop filtering (allow all authored), or do they get their own generated fallbacks too? Defines scope.

5. **Scope split + dependency.** dim7 (Finding 2) is gated on the `domain/chromatic-degrees` thread — a dim7 chord only reaches the voicer once `#IVdim7` parses. But `thread.md`'s `depends_on` is **empty** — the dependency isn't encoded. Two clean choices: (i) set the dep and design both together, or (ii) design findings 5 & 6 (difficulty → shells) as the deliverable now and treat dim7 as an independent follow-up. I'd lean toward (ii): the difficulty-band work is self-contained and the dim7 piece is a small wiring task once the parser lands.

Per our flow (idea → design → **req** → plan), once you pick directions on 1–5 I'd capture them as the design conversation, then lock a `req.md` before any plan.

My recommendation to unblock design: **option (a)** for #1 (author an explicit band — most durable and pack-overridable, matches the content-pack philosophy) and **(ii)** for #5. But these are the architecture-level calls, so I'll stop here for your direction rather than pick.

## Rafa:

5. **Scope split + dependency.
`domain/chromatic-degrees` is almost done 1-6 done and now blocked by this

3. "Beginner = shells" when no authored shell exists.
I think this is easy derived from CAGED chords.
We should spine up a new thread to derive shell voicings before going forward. Agree?
If so, spin up the new thread with an draft an idea

## AI:

Agreed — and done. Deriving shells from CAGED is the right move, and it's the cleaner one:

- The pack already authors **full CAGED chords for every jazz-blues quality** (maj, min, dom7, m7, maj7, m7b5, dim7, aug) in multiple shapes — that's the golden oracle. A shell is just that authored shape with the 5th + doublings dropped, keeping root+3+7.
- Today's `BeginnerShellStrategy` is one hardcoded A/D/G shape for 3 qualities only; it throws on dim7/m7b5/triads. CAGED-derivation gives a shell for *any* quality the pack ships, from a single source of truth — exactly the interval-derivation-engine direction.

So I spun up the new thread and drafted the idea (status: **draft** — review it):

- **Thread:** `guitar/shell-voicing-derivation` (`th_01KVYQ2XJQ0MWYMY02A4PRFHZF`), priority 35 (ahead of this one).
- **Idea:** `shell-voicing-derivation-idea.md` — Goal / Why CAGED-derived / Shape sketch / Scope / 4 open design questions / Validation.
- **Dependency wired:** `voicing-difficulty-bands` now `depends_on` `shell-voicing-derivation` — so the roadmap enforces "shells first, then band selection."

The idea leaves 4 things for its design phase (triad shells?, non-contiguous string-set choice, replace vs. augment `BeginnerShellStrategy`, derive at build-time vs on-demand). One nice property I baked into Validation: the derived dom7/m7/maj7 shells should reproduce the current hand-authored `BeginnerShellStrategy` output — a built-in regression oracle.

On your other two points:
- **chromatic-degrees almost done (1–6) → dim7 unblocks soon.** Good. The dim7 shell rides on the same CAGED-derivation here; once `#IVdim7` parses, the dim7 chord reaches the voicer and the derived `dim7` shell just works. No separate dim7 hack needed.

Next: review the draft idea, then we refine it → design → req for `shell-voicing-derivation`. Want me to walk the 4 open design questions now, or read the idea first?