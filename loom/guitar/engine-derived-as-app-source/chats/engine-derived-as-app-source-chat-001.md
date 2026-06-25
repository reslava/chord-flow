---
type: chat
id: ch_01KVYWBG3WA9VPA7E8XTN0S55S
title: engine-derived-as-app-source Chat
status: done
created: 2026-06-25
updated: 2026-06-25
version: 2
tags: []
parent_id: null
requires_load: [chordflow-architecture]
last_ai_block: 12
---
# engine-derived-as-app-source Chat

## Rafa:

read idea, load global context and `loom/refs/chordflow-architecture-reference.md`

what you think? agree?
is it all grounded for a design?

## AI:

Read the idea, global ctx, and the architecture ref — then traced the actual code to check it's grounded.

**Short answer: I agree with the direction, and the core mechanic is grounded. But it is *not* fully ready for design yet** — there's one missing dimension (the voicing *content/store* story) that's architectural, plus a couple of factual corrections to the idea's wiring claims.

### What's confirmed in the code (the idea is right about these)
- **The duplication + dead engine is real.** The render path is `Program.cs:94` → `new AlphaTexRenderer(new VoicingBook(voicingLibrary))`, and `VoicingBook` realizes `_stored` (`VoicingBook.cs:52`). `CagedDerivation.Derive` output is never in this path. So we ship the oracle, not the engine. ✅
- **`Derive` fails loud.** Two `throw`s (`CagedDerivation.cs:36, 101`) on no-anchor / un-spellable. The coverage-gating premise (a) holds. ✅
- **Type bridge is easy and lossless.** `ChordShape` already carries everything a `Voicing` render needs — per-string fret, muted, `Semitones`, plus `Shape`/`AnchorFinger`/`Zone` (`ChordShape.cs`). A `ChordShape → Voicing` adapter is clean; recommend keeping `Derive` returning `ChordShape` and adapting at the seam (your open-Q1, leaning adapter).
- Precedent already exists: `CagedShapesHandler` + `CagedChordHandler` are wired in `Program.cs:100-101` — the engine is *already* surfaced to a dogfood UI, just not to the render pipeline.

### Corrections to the idea's wiring claims (matters for design)
1. **The runtime source is the SQLite DB, not the pack `.dsl` files.** Flow is: `Content/default-pack/voicings/*.dsl` → `DefaultPack.ImportInto` (PackImporter) → `VoicingStore` → `new VoicingStore(db).LoadShapes()` (`Program.cs:86`). So "move the `.dsl` into a test fixture" doesn't just relocate the oracle — it **severs the seed**, leaving `VoicingBook` empty and falling back to `BeginnerShellStrategy`. The relocation and the re-sourcing-from-`Derive` therefore *must* land atomically, or the app silently regresses to shell voicings. The idea half-acknowledges this in coverage-gating; the design needs to state it as a hard ordering constraint.

2. **The biggest gap — voicings are user-editable *content*, and the idea is silent on it.** There's a whole machinery around voicings beyond rendering: `VoicingStore : IContentStore`, the Content/Voicings CRUD editor, canonicalize-to-C on save, BuiltIn/Pack/UserDefined shadow+revert tiers, and the `VoicingsChanged` → hot-rebuild-renderer path (`Program.cs:116-119`). If authored voicings become *test-only* and the app sources grips from `Derive`, then:
   - What is `VoicingStore` / the Voicings editor *for*?
   - Can a user still author/override a voicing? If yes, the runtime source is **derived ∪ user-authored** (engine as base, user shapes shadowing) — not "engine only." That's a different, larger design than the idea's "one source of truth: the engine."
   - If no, we're deleting a content kind + bridge verbs + a UI page. That's fine if intended, but it's a scope decision, not a footnote.

   This is the one I'd want pinned **before** writing a design — it changes the shape of everything downstream.

### In-scope open questions that are genuinely open
- **Candidates multi-shape (your Q3).** Today `Candidates` returns *all* authored shapes for a quality, ranked by lowest fret then `FamiliarityRank()`. From `Derive` you must derive each CAGED shape at a chosen neck region and reconstruct that ranking — `Derive` needs a `(minFret,maxFret)`, so "where do you place each shape for the list" is a real sub-design.
- **Coverage set must be pinned to exactly the 34.** The oracle deliberately trims m7b5/dim7/aug to A/E/D (no C/G) — matches the `caged-c-full` note. So "every quality×shape the app needs" = exactly the 34 the oracle enumerates; the structural test must assert *that* set, or the app will offer derived-but-unverified shapes (e.g. m7b5 C-shape).

### Verdict
Direction: yes, this is the engine-vision payoff and the right move. Mechanic: grounded — the adapter + re-source + relocate + coverage-test all check out against the code. **Blocker for design:** resolve the *voicing-content / `VoicingStore` / user-authoring* question (gap #2). Once that's decided, I'm at 95% and we can write the design.

One question to start: **after this lands, can a user still author/edit a voicing?** (a) No — voicings stop being editable content, engine is the sole source; or (b) Yes — engine is the base, user-authored shapes shadow it (so the "authored set" that moves to test-only is specifically the *built-in default-pack* grips, and the user tier stays live).

## Rafa:

About your question: after this lands, can a user still author/edit a voicing?
Yes, absolutely
The "authored set" that moves to test-only are the 36 CAGED 
chord voicings only, that are already duplicated with the engine derived.

aug also have 5 voicings, so (min, maj, min7, maj7, dom7, aug) * 5 + (m7b5, dim7) * 3 = 36 .dsl files

We need to clarify this important stuff..
We should have 3 sources for songs, progressions, rhythms, voicings
1. Engine derived (for now just voicings)
2. Packages (default and others)
3. User custom written (**should not hide 1 and 2**, I think current engine hide 2)

User should be able to get:
- voicings for the 3 sources
- songs, progressions, rhythms from sources 2 and 3
UI should tag the sources and implement a filter control. I think current tags are `built-in`, `user`. Maybe the tag should be `{package-name}`, `user` and a name like `automatic` for engine derived.

2 and 3 will persist in .dsl package files and imported to SQLite DB (runtime source)

Agree with what you said line 45: `Derive` needs a `(minFret,maxFret)`

What you think? Agree? Do you like the approach?
Tell me what is not still grounded?
Maybe we will need to spin up some new threads related to this chat but not to this thread

## AI:

Yes — I like this a lot. It's the right generalization: it lines up with the "content is data, additive" principle in the architecture, and the **no-hiding rule is the correct call**. But you've just expanded the problem from "wire the engine into voicings" into "rework the content-source model for *all* kinds" — so let me separate what I agree with, the one design knot the vision doesn't yet resolve, and what I'd grounded-check.

### Count first: it's 36, you're right
The default pack has **36** voicing `.dsl` files and the oracle array has **36** entries — `(maj, min, maj7, dom7, min7, aug)×5 + (m7b5, dim7)×3 = 36`. The "34" in the idea text and in the `CagedDerivationOracleTests` comment (line 21) is **stale** — fix both when this lands.

### Where I fully agree
- **3 sources** (`automatic` = engine-derived · `package` = default + others · `user`) is the right model, and the engine becoming a real *source* (not a hidden oracle) is the whole point.
- **Sources must not hide each other in the listing.** And you're right that the current code *does* hide: `IContentStore.List()` runs `ContentSummaries.Build`, which **collapses all tiers to one winning row per id** via `OriginResolver` (the shadow law — `IContentStore.cs:10-14, 65-83`). So a user edit of a built-in today *replaces* it in the list; a pack item shows only as `Pack`, never "which pack." Your vision means: stop collapsing, list every source's row, tag it, filter it. That's a clean change but it rewrites the `List()` contract.
- **Tag = `{package-name}` / `user` / `automatic`** is better than today's flat `BuiltIn`/`UserDefined`. (Note: today `Origin.Pack` doesn't carry *which* pack — we'd need to thread pack identity through `PackImporter` → store row → `ContentSummary`.)
- `Derive` needs `(minFret, maxFret)` — agreed, that's settled.

### The one knot the vision doesn't resolve yet (this is the important bit)
"No hiding" answers **visibility** but not **resolution**. They're two different questions and the design has to split them:

1. **Visibility (listing):** show all sources side by side, tagged + filterable. ✅ additive, no hiding — exactly what you want.
2. **Resolution (which one actually plays):** when an exercise/song references "the C7 voicing," and `automatic` + a pack + the user all offer one — *which* is used? Today there's a single winner (the shadow). If nothing hides anything, the reference is now ambiguous. Two ways out:
   - **(a) Source-qualified references** — a reference names the source (`user:my-c7`, `auto:dom7:E`, `default-pack:dom7_eshape`). Most explicit; no hidden precedence; but every reference and the `generate`/load path must carry a source.
   - **(b) Precedence at resolve-time only** — still listed un-collapsed (visible), but a fixed precedence (`user > package > automatic`?) picks the winner when a bare id is referenced. Keeps references simple; "no hiding" holds for the *list* but a winner still exists for *play*.

   This is the real decision. My lean: **(b) for now** (precedence at resolve, additive at list) because it's the smallest change to the `generate`/exercise path — but it's genuinely your call and it's architectural, so I'm flagging not picking.

### What's not yet grounded (your explicit question)
1. **The engine isn't a `IContentStore`.** The 3 stores are DB-backed and speak DSL+ids; the engine source is *computed* (`Derive` over qualities×shapes), un-persisted, always fresh. So the listing layer needs a **new seam**: a "computed source" that synthesizes `automatic`-tagged rows and gets *unioned* into the voicing listing — it does **not** go through `PackImporter`/SQLite (only sources 2 & 3 do, as you said). Designing that union (where it lives, how `entityList` merges store rows + computed rows) is the core new architecture.
2. **Cross-source identity.** Engine C-maj-E-shape, a pack's, and a user's are *three* rows now (good) — but each needs a distinct, stable id so you can pick/play a specific one (e.g. `automatic` ids like `auto:maj:E`). Ties directly into the resolution decision above.
3. **`automatic` is voicings-only (for now).** Songs/progressions/rhythms have only sources 2 & 3. The filter UI must handle "this kind has no automatic source." Minor, but the model should say it explicitly.
4. **Editing a built-in.** Once nothing collapses, what does "edit this pack voicing" mean — a separate `user` row that coexists (and wins by precedence), with the pack row still visible? I think yes, and that's *better* than today's silent shadow. Worth stating as the new edit semantics.

### Thread structure — yes, split it
This is too big for `engine-derived-as-app-source`, and most of it isn't even voicing-specific. I'd propose:

- **New foundational thread — `content-source-model`** (cross-cuts all 4 kinds): the additive listing (kill the collapse), the `{package-name}`/`user`/`automatic` tag model, the filter UI, and the **resolution-precedence (or source-qualified-reference) decision**. This is the load-bearing piece.
- **This thread — `engine-derived-as-app-source`** narrows to: add the engine as the `automatic` voicing source (the computed-source adapter + `ChordShape→Voicing`) and relocate the 36 CAGED grips to the test-only oracle. It **depends on** `content-source-model`.
- The filter-control UI is probably the dogfood of the model thread, not its own thread.

So: design `content-source-model` first (especially the visibility-vs-resolution decision), then this thread plugs the engine in as source #1.

**Before I'm at 95% for any design, I need one thing from you:** the **resolution** rule — (a) source-qualified references, or (b) fixed precedence at resolve-time with additive listing? Everything else I can ground from the code.

## Rafa:

Agree all. We are close to ground.

### Resolution

My proposal:
Define a main source for voicings used in the song, or progression.
For example, 
- automatic fret 5-12
- default-package
- swing-package
- user

Engine looks in that source, if a candidate exits, take it, in case does not exits use: `user > package > automatic` (for now we have only default-package)

note: with this approach if User setup main source to `user` then is (b) exactly

Do you like, useful for final users? easy enough to implement?

## AI:

Yes — I like it, and it's useful for real players (more on that below). It also resolves the knot cleanly: **list = all sources (no hiding); resolve = main-source-first, then `user > package > automatic` fallback.** And your note is exactly right — main source = `user` *is* option (b). So (a) and (b) were never really rivals; your "main source" is the dial that spans both. Good.

Let me sharpen it with one framing distinction, then the two things to pin.

### The framing that makes it click: precedence is a *bulk-voicing* concept only
There are two kinds of "resolution," and your main-source+fallback only applies to one:

1. **Hand-picked content** — a song, a progression, a rhythm, or *one specific voicing* the user selects from the list. Here resolution is trivial: they picked that exact row, so it's **source-qualified by the pick itself**. No precedence, no fallback — you chose `swing-package`'s blues or `user`'s C7, you get that one. "No hiding" + tag + pick-directly is the whole story for these.
2. **Auto-filled comping voicings across a progression** — the user did *not* hand-pick a grip per chord; the engine fills all of them. *This* is where main-source + fallback lives, because chords are resolved en masse and any one source may not cover every chord.

So your proposal is precisely the **comping-voicing resolution** rule — not a global one. That's good: it means songs/progressions/rhythms need *zero* precedence machinery (just listing + tags + filter), and the only place the fallback chain runs is voicing auto-fill. Smaller blast radius than I first thought.

### Is it useful for final users? Yes — genuinely
`automatic, frets 5-12` isn't plumbing, it's a **practice mode**: "comp this blues using CAGED shapes in the 5–12 region." That's a real, sellable feature — region-locked shape practice falls straight out of `Derive(minFret, maxFret)`. `main = swing-package` = "play it with the swing pack's voicings." These are things a guitarist actually wants. The fallback keeps it from ever failing loud on a chord the chosen source lacks.

### Two things to pin (small, but the design needs them)
1. **The "main source" value is structured, and it's a practice setting, not content.** For `automatic` it carries a fret window (your 5-12) — and, since 5-12 can hold several shapes per chord, a **within-automatic ranking** (lowest fret? familiarity? voice-leading to the previous chord?). For `package` it carries *which* package. So main-source = `{ kind, region?, packageId? }`. I'd make it a **generate-time parameter** on the Practice page — flowing through the `generate` envelope alongside key/tempo/feel, evolving the existing `renderOptions.voicing` knob — **not** baked into the song/progression content (those stay voicing-agnostic). Confirm: transient practice knob, agreed?

2. **Fallback is per-chord, so a song can mix sources.** If main = `user` and user has C7/G7 but not the passing Dim7, that one chord falls back to automatic. That's correct and necessary — but worth (a) stating explicitly as the rule, and (b) optionally having the UI *flag* a chord that fell back, so the player knows their main source didn't cover it. (And define "package" in the tail for the multi-package future — for now, one pack, moot.)

The within-`automatic` ranking (1) is the only genuinely open sub-question left; everything else is now grounded.

### Easy enough to implement?
The *resolution* itself — yes, it's a small function at the voicing seam: try main source → else walk the fallback chain. The real work is the pieces under it: the additive listing (kill the `ContentSummaries.Build` collapse), the tag/filter model + filter UI, threading the structured main-source param through `generate`, and the engine-as-computed-source union. None hard; spread across Core + bridge + the 4 JS views.

So the thread split still holds:
- **`content-source-model`** — additive listing, `{package}`/`user`/`automatic` tags, filter UI (all 4 kinds). Foundational.
- **this thread** — engine as the `automatic` voicing source + `ChordShape→Voicing` + relocate the 36 grips to the oracle + the **main-source/fallback comping resolution** (it's voicing-specific, so it belongs here, on top of the model thread).

I'm at ~95% on the shape now. If you confirm (1) main-source is a transient generate-time knob, and you have a lean on the within-`automatic` ranking, I think the next move is to **spin up the `content-source-model` thread and write its idea doc**. Want me to create it?

## Rafa:

(1) Agree
within-automatic ranking: 

this is the perfect `voice-leading to the previous chord`, but maybe too ambitious for now?
if not we could use `closest to the previous chord`?

if all is grounded:
spin up the `content-source-model` thread and write its idea doc
update idea of this thread

what's next pending?

## AI:

Got the ranking refinement — and I like it more than plain closest-to-previous, because it's pedagogically better: **it forces the player through different CAGED shapes** instead of parking on one. Captured as:

> **first chord** → lowest-fret grip in the region; **each next chord** → closest to the previous chord **among shapes not yet used**, relaxing the not-yet-used constraint once every shape has been used.

(The "relax when exhausted" bit matters for a 12-bar blues — only 5 shapes, many chords — so the no-repeat rule has to cycle. Flagged that, plus "what does *used* track, shape or grip," as a design-time specific.)

### Done just now
- **Created the new thread** `guitar/content-source-model` + its idea doc (additive listing, `{package}`/`user`/`automatic` tags, filter UI — the foundational piece).
- **Rewrote this thread's idea** to the converged scope: engine = the `automatic` voicing source, `ChordShape→Voicing` adapter, computed-source union, main-source/fallback comping resolution + the generate-time knob, relocate the **36** grips (fixed the stale "34") to a test-only oracle, coverage gating, the runtime-source ordering constraint, and the "voicings stay user-editable" note.
- **Wired the dependency:** `engine-derived-as-app-source` now `depends_on` `content-source-model` on the roadmap.

### What's next pending
The roadmap is now two ordered threads:

1. **`content-source-model` (must go first — this thread depends on it).** Pending: a **design doc**, which has to resolve its 3 open questions — (a) edit/delete/revert semantics once rows coexist (what happens to `OriginResolver`/shadow/`HasLowerTier`), (b) cross-source identity (shared id vs source-qualified ids), (c) the filter-UI shape. Then req → plan → implement.
2. **`engine-derived-as-app-source` (this thread, after the model lands).** Pending: its own design over the same 3 open questions in its idea — where the computed source + union live, the `automatic` synthetic-id scheme, and the ranking specifics above. Then req → plan → implement.

So the **immediate next step is the `content-source-model` design**. Want me to start it (read the content/persistence code in depth and draft the design doc), or pause here?

## Rafa:

Write content-source-model design

### Ranking

I like the interpretaion you made of my ranking refinement, as you said it is useful for force user to know all different CAGED shapes, but it is not what I wanted.

This makes me think we could have different ranking systems:
1. Use all CAGED shapes -> the one you defined
2. Closest -> 1st chord: lowest fret                                        
              nexts: if voicing has been used already then take it (repeat)
                     if not closest to previous chord  
3. Voice leading (3th, 7th) -> future but maybe is not difficult:
              1st chord: lowest fret
              nexts: voice with 3th or 7th same string and closest to previous chord 3th or 7th

Do you like? Agree?

If you think this is too much to fold in, spin up a new thread with an idea for it.

## AI:

Good correction — I over-rotated on variety. What you're really pointing at is that **ranking isn't one rule, it's a family of selectable strategies**, each for a different goal:

1. **All CAGED shapes (variety)** — what I wrote; a *learning* mode (forces you through every shape).
2. **Closest (consistent)** — your actual intent: 1st chord = lowest fret; next = **reuse the grip if this chord already appeared** (muscle memory), else the grip closest to the previous chord. Minimal movement, settle in.
3. **Voice leading (guide tones)** — 1st = lowest fret; next = 3rd/7th on the same string, closest to the previous chord's 3rd/7th. Future, and it needs guide-tone (shell) derivation — so it ties to [[shell-voicing-derivation]].

Yes, I like it — these are genuinely distinct and each musically meaningful. And it's too much to fold in as a single rule, so:

- **Spun up a new thread** `guitar/voicing-ranking-strategies` (idea) for the **pluggable, user-selectable ranking** — the 3 modes + the selection knob.
- **This thread** ships just the **ranking seam + one default**, so the engine-as-source produces grips without waiting on the strategy work. I propose the default = **Closest (mode 2)** — the most natural comping behavior. The other modes are additive on top, in the new thread (which `depends_on` this one).
- Fixed this thread's idea ranking line accordingly.

If you'd rather a different default, say so.

Now writing the **content-source-model design** — reading the persistence/content code in depth first, then drafting the doc.

