---
type: chat
id: ch_01KWZ4PECMGNB7PZDYXCG57A4F
title: explicit-voicing-reference Chat 001
status: done
created: 2026-07-07
version: 1
tags: []
parent_id: null
requires_load: []
last_ai_block: 23
---
# explicit-voicing-reference Chat 001

## Rafa:

read idea

### Open design questions 

This are my leans:
1. Token placement + escaping in the DSL 
   1. does `{…}` collide with anything? check it
   2. per-chord vs per-span? both per-chord, per-song
2. The literal `{c:}` grip's exact syntax: fret string only
3. Reference resolution semantics when the named id is missing or its source is filtered out: fail loud
4. Whether annotations live: on Song DSL
Agree? Have other suggestions?

### CHORDs DLS 

Thinking outloud, draft proposal
Chords should be defined over key C to be transposables, similar to voicings. 

Song example:
```
\17 3 3 2 3 1 x        // all 17 in song and progressions inside will use this custom voice 
\#4dim7 8 x 7 8 7 x    // all #4dim7 in song and progressions inside will use this custom voice, transposed to 4

head: jazz_blues_standard
end = 17 {3 3 2 3 x x} 6min7 17 17 // First 17 used custom voice per-chord
```

What do you think?

## AI:

Read the idea. Broadly agree with your four leans — but your CHORDs draft quietly introduces a **new tier** the idea didn't have, and it forces two real decisions. Let me confirm the easy ones, then push on those.

## Your four leans

1. **`{…}` collision — checked, clean.** I swept all four DSLs (Progression, Song, Rhythm, Voicing). Separators/sigils in use: space, `_`, `:`, `#`, `@`, `x`, `|`, `\`, `=`. Curly braces are **unused everywhere** — no collision. ✅
2. **`{c:}` = fret string only.** Agree. Same 6-value low-E→high-E grammar as the Voicing DSL `frets:` field (`x`=muted, `0`=open). No barre/finger hints — the derivation engine already owns finger inference. ✅
3. **Missing / filtered reference → fail loud.** Agree, and it's the *consistent* choice — content-pack references already "fail loudly when the song is realized." A silent fallback to ranking fill would hide a typo'd id. ✅
4. **Live on the Song DSL** — agree in spirit, but this is the one that needs unpacking (see *Fork C* below), because a per-chord `{}` is lexically glued to a chord, and chords are Progression-DSL.

Per-chord vs per-span: agree — **per-chord + per-song, no per-span.** A "span" is a bar, which can hold several chords, so per-span voicing is ambiguous. Skip it.

## What your draft actually proposes

You've gone from **two tiers** to **three**:

```
per-chord  17 {3 3 2 3 x x}     ← override this ONE chord      (highest priority)
song       \17  3 3 2 3 1 x     ← default for ALL matching     (new tier)
—          (no annotation)      ← automatic ranking fill        (default)
```

This is a clean cascade and architecturally cheap — all three ride the same `CompingResolver` override seam from [[engine-derived-as-app-source]]; the song-default is just a bulk pre-fill checked before the ranking fill, and the per-chord `{}` overrides even that. I like it. But it opens three forks.

### Fork A — grip semantics: movable shape vs absolute frets

Your `\#4dim7 8 x 7 8 7 x // transposed to 4` comment says **movable** — you author the shape once and it slides to the root. I strongly agree, and it should be the rule for *both* forms: **a literal grip is a movable shape authored WYSIWYG at the current key, normalized to a C anchor, and re-transposed at realization — identical to the Voicing DSL** ("whatever anchor you type, stored normalized to C"). 

Why this matters: ChordFlow is a *transposing trainer*. If grips were absolute frets, a `{3 3 2 3 x x}` annotation would silently become wrong the instant you change key or hit a `mod V`. Movable-shape is the only choice that survives modulation, and it answers your open-Q2 "does it canonicalize?" → **yes, exactly like authored voicings.** One mental model for every grip in the app.

### Fork B — what does `\17` key on: quality, or degree+quality?

You wrote degree-specific tokens (`\17`, `\#4dim7`). But "similar to voicings" cuts the other way — voicings are keyed by **quality** and movable to any root. So which is the song default?

- **(a) Quality-keyed:** `\7 <grip>` = every dominant-7 chord (I7, IV7, V7) uses this shape, transposed per root. Concise, matches the voicings model — *one* line voices a whole blues.
- **(b) Degree+quality-keyed:** `\17 <grip>` = only the I7. More control (e.g. a special `#4dim7` passing grip), more lines.
- **(c) Cascade:** allow both — `\7` sets the quality default, `\17` overrides just that degree. Powerful but a third layer inside the song tier.

My lean: **(a) as the primary**, because it's the voicings-consistent model and covers the common case in one line — with **(b) permitted as a specificity override** (which is really just (c), but framed as "more-specific token wins"). Your `#4dim7` example is exactly the case where degree-specificity earns its keep. Do you want the cascade, or keep it single-level?

### Fork C — the "progressions are pure harmony" invariant (your Q4)

You said annotations live "on the Song DSL." Agreed as the intent — but the per-chord `{}` is lexically part of a **chord token**, and chords are Progression-DSL. The idea's own sketch says "the parsed chord carries an optional voicing annotation." Those two pull apart. We've been strict that *"progressions carry no feel/key"* — pure harmony. By that same logic a stored progression should carry **no voicing** either.

Cleanest reconciliation:
- **`ProgressionParser` lexes the `{}` token** (it's glued to the chord) into an optional annotation field — but the annotation is **arrangement metadata**, consumed only by the Song/`CompingResolver`.
- A **standalone stored progression** stays pure: it **rejects** `{}` annotations (fail-loud: "voicing annotations are a Song-level concern").
- An **inline progression inside a Song** (`end = 17 {3 3 2 3 x x} 6m7 …`) accepts them — that's arrangement context.
- The `\` song-default is unambiguously Song-DSL and needs no such guard.

That keeps the invariant intact while allowing your inline authoring. Agree, or do you want stored progressions to carry voicings too (looser, but breaks the purity rule)?

## Smaller calls

- **Sigil `\` vs a keyword.** The Song DSL is otherwise all **space-keywords** (`key C`, `feel triplet8th`, `tempo 120`, `mod V`) — no sigils. A bare `\` would be a new lexical style. Consider `voice 17 = 3 3 2 3 1 x` or `voice #4dim7 = 8 x 7 8 7 x`, which reads like the existing `NAME = …` definitions. Purely your aesthetic call — flagging it for consistency.
- **`{c:}` prefix optional?** Your draft dropped it (`{3 3 2 3 x x}`). The colon disambiguates cleanly: `{3 3 2 3 x x}` (no colon → literal grip) vs `{u: C6}` (colon → source-qualified reference). We *can* allow bare `{grip}` as sugar, but I'd keep `{c:}` explicit so the grammar is uniformly `{source: id}` — one shape to learn. Your pick.
- **The source-qualified reference form** (`{u: C6}`, `{a: shell-C6}`, `{swing: C6}`) from the idea isn't in your draft — only the literal grip. Still in scope, or are we shipping literal-grip first and references later?
- **Nit:** `6min7` in your example isn't a valid suffix — it's `6m7` (or `6-7`).

Net: I agree with your leans and like the three-tier cascade. Before I can firm this into a design doc I need your calls on **A** (movable — I think we already agree), **B** (quality vs degree keying), and **C** (progression purity). Those three decide the grammar and the resolver contract.

## Rafa:

### Fork A — grip semantics: movable shape vs absolute frets

Yes, movable frets, normalized to key C

### Fork B — what does `\17` key on: quality, or degree+quality?

Your lean

### Fork C — the "progressions are pure harmony" invariant (your Q4)

Yes, progressions are pure harmony

- ✅ An **inline progression inside a Song** (`end = 17 {3 3 2 3 x x} 6m7 …`) accepts them — that's arrangement context. Yes.

## Smaller calls
- use `voice 17 = 3 3 2 3 1 x`
- `{c:}` always prefix c for custom
- The source-qualified reference form** (`{u: C6}`, `{a: shell-C6}`, `{swing: C6}`, yes they are included
- `6-7` is the correct canonical

is it all grounded?
if so write design + req

## AI:

Almost — **one gap in Fork B** I have to close before writing, plus one micro-call I'll flag. Everything else is locked.

### The gap: the quality-only selector collides with a degree

You endorsed "quality-primary + degree-override" for the `voice` default. But in the chord grammar a bare `7` is **degree-VII, major** — not "the dominant-7th quality." So `voice 7 = …` is ambiguous: degree VII, or all dom7s? Your own examples are all degree+quality (`voice 17`, `voice #4dim7`), which never hit this — the ambiguity only appears the moment we add the quality-wide form.

Most qualities are safe (`m7`, `maj7`, `dim7`, `m7b5`, `aug` can't be read as a bare degree), but **dominant-7 `7`** and the **major triad (empty)** both collide with degree tokens. So we need one unambiguous marker for "any degree."

**Recommendation — a `*` wildcard degree** (`*` is unused in the Song/Progression DSL):

```
voice *7   = 3 3 2 3 1 x      # every dominant-7 chord (I7, IV7, V7 …), transposed per root
voice *m7  = x 3 1 3 1 x      # every minor-7 chord
voice 17   = 8 x 7 9 8 x      # overrides just the I7  (most-specific wins)
voice #4dim7 = 8 x 7 8 7 x    # just the #IVdim7
```

Two clean forms: `*<quality>` = quality-scoped (any degree), `<degree><quality>` = degree-scoped. Explicit degree beats wildcard in the cascade. Confirm `*` (or pick another marker) and I'll write.

### Micro-call (flagging, not blocking)

The **`voice` default RHS** takes the same voicing-spec as the brace form — a literal grip *or* a reference. Since a bare grip after `=` can't be mistaken for a reference (a reference has a `source:` word), I'll allow the bare grip as sugar there (`voice *7 = 3 3 2 3 1 x`), with explicit `c:` also accepted (`voice *7 = c: 3 3 2 3 1 x`). References use their source prefix (`voice *7 = u: C6`). The **braced per-chord** form still always requires the prefix, per your rule (`{c: …}`, `{u: C6}`). Shout if you'd rather force `c:` everywhere for strict uniformity.

Locked so far: movable grips normalized to C · cascade with most-specific-wins · progressions stay pure (inline-in-song accepts `{}`, stored progression rejects) · `voice` keyword not `\` · `{c:}` always-prefixed · reference forms `{u:}`/`{a:}`/`{pkg:}` in scope · `6-7` canonical.

Give me the `*` nod and I'll write the design + req.

## Rafa:

use `*`
allow the bare grip as sugar. why braced per-chord can not allow that sugar also?
in `voice` default RHS, what means RHS?

## AI:

**RHS** = "right-hand side" — the part after the `=` in `voice *7 = 3 3 2 3 1 x`. The left-hand side (LHS) is the selector `voice *7`; the right-hand side is the voicing it maps to.

**Why the brace can't have the sugar too — it can.** There's no technical reason it couldn't; the `c:` requirement was just the rule *you* stated earlier ("always prefix c"). The disambiguation is identical in both places: a **reference** always carries a `source:` word before the colon (`u:`, `a:`, `swing:`), so anything *without* that leading source word is a literal grip. So I'll make it **uniform**: bare grip allowed everywhere, `c:` accepted-but-optional everywhere, references always source-prefixed.

```
17 {8 x 7 9 8 x}          # per-chord literal grip (bare)
17 {c: 8 x 7 9 8 x}       # same, explicit c:
17 {u: C6}                # per-chord reference (source-prefixed)
voice *7 = 3 3 2 3 1 x    # song default, bare grip
voice *7 = u: C6          # song default, reference
```

One grammar, two placements. Cleaner than the asymmetry — good catch.

**One thing the movable-grip contract surfaced** (writing it into the design, flagging so it's not buried): a bare grip needs an *anchor root* to be movable. For a **per-chord** grip and a **degree-scoped** `voice 17`, the harmony fixes the root (degree + key), so transposition is just "shift the shape by the root's semitone motion" — clean, no inference. For a **quality-scoped** `voice *7` there's no fixed root, so the engine infers the shape's anchor by spelling it (the same normalize-to-C the Voicing DSL already does), with an optional `root:6` hint available for ambiguous shapes, and references (`voice *7 = a: shell-7`) as the naturally-movable alternative. I'm treating that inference as the one detail that leans on the voicings engine — captured as an open impl note in the design, not a blocker.

Writing **design + req** now.

## AI:

Written both docs.

- **`design.md`** (`de_01KWZAEXXHM8K1PB5J71G3T800`, draft) — decisions D1–D7, the shared voicing-spec grammar, both placements, the `CompingResolver` cascade, the grip-anchoring/movability section (with the quality-scoped inference flagged as the one voicings-engine-leaning open impl note), parsing/domain changes, round-trip, errors, dogfood, out-of-scope.
- **`req.md`** (`rq_01KWZAFJ3NSWGZNTDSJFWSAP6K`, **locked**) — 10 Included (`IN1`–`IN10`), 7 Excluded (`EX1`–`EX7`), 8 Constraints (`C1`–`C8`). This is the anchor the plan will cite.

Left the **design at draft** and the **idea untouched** — didn't want to flip statuses you didn't ask for. When you're ready for the plan I'll finalize the design, mark the idea done, and build the plan citing the `IN`/`C` handles.

Two things worth your eye before then:
1. The **grip-anchoring open note** in the design (quality-scoped `voice *7` bare grips need engine shape-inference) — it's the one piece that genuinely depends on [[engine-derived-as-app-source]] landing first (which this thread already declares as a dependency).
2. `req` `C8` records that hard dependency — the plan can't really be *implemented* until that sibling thread's `CompingResolver` exists, though the design/req/plan can all be authored now.

Want me to go on to the plan, or review anything in the two docs first?

## Rafa:

Thinking outloud...

We could include `root:6` in the grip, but I am think in rootless voicings, so we should include a fake root for them `root:6-8` being 8 the fret.

Or use: 
voice *7 = 3 3r 2 3 1 x // r = root
voice *7 = x 3f 2 3 1 x // r = fake root for a rootless voicing

## AI:

Good instinct — **rootless voicings are exactly the case that forces an explicit anchor**, and they're worth doing well (they're the bread and butter of jazz comping, and the [[dogfood-real-jazz-derived-voicings]] direction wants them).

Here's *why* they force it, because it sharpens the choice: for a voiced-root grip the engine can infer the anchor by spelling the shape. But a **rootless shell (3rd + 7th only) is genuinely ambiguous** — `{3, b7}` is a tritone that belongs to *two* dom7 roots a tritone apart. That ambiguity is literally why tritone subs work. So the engine **cannot** always recover the root of a rootless shape — you have to declare it. Inference stays the default for the easy cases; the declaration is the escape hatch for rootless/ambiguous ones.

### The two ways you floated

**(A) A separate `root:` clause — my recommendation.** Reuse the Voicing DSL's existing `root:` concept and extend it with an optional fret for the phantom case:

```
voice *7 = x 3 2 3 1 x  root:6@8     # rootless: phantom root = string 6, fret 8 (sounds C → the anchor)
voice *7 = 8 x 7 9 8 x  root:6       # voiced: root on string 6 (fret read from the grip)
17 {c: x 3 2 3 1 x root:6@8}         # same inside a per-chord annotation
```

- `root:<string>` — voiced root, fret inferred from the grip (identical to today's Voicing DSL).
- `root:<string>@<fret>` — a **phantom root** on a *muted* string: the `@<fret>` supplies the pitch the grip can't, and it doubles as the normalize-to-C reference. The phantom fret can sit anywhere (even below the played notes — an implied bass root).
- **Optional by default**: omit it and the engine infers; supply it only when the root is muted (rootless) or inference is ambiguous. So the common case stays clean.

**(B) Inline fret-token suffix (`3r` / `3f`).** I'd **decline** this one:
- It breaks the clean "grip = 6 fret tokens" grammar — every token could now carry a suffix.
- The rootless case doesn't fit: a true rootless voicing has the root on a **muted** string, and you can't suffix `x`. Your `3f` sidesteps that by marking a *sounded, non-root* note as a "fake root" — but then the marker no longer means "root," and to make it unambiguous you'd *also* have to say which interval that note is. That's more info than `root:6@8`, not less.

### Naming nits

- I'd write it `root:6@8` ("string 6 **at** fret 8") rather than `root:6-8` — the `-` reads like a range ("strings 6–8"). Small; your call.
- "phantom" / "implied" root rather than "fake" — same idea, clearer in the docs.

### Consequence

This promotes **rootless voicings to a first-class Included item** (a new `IN`), and the anchoring section of the design gets the `root:<string>[@<fret>]` rule. Good addition — it's the thing that makes these annotations useful for real jazz, not just triads.

If you're good with **(A)** + `@` separator, I'll amend `design.md` and re-open/amend the locked `req.md` (append-only) to add the rootless requirement. STOP — waiting for your go.

## Rafa:

A
go

## AI:

Done — Option A with the `@` separator is in both docs.

**`design.md`** (draft):
- New **D8** — rootless voicings via a declared `root:<string>[@<fret>]` anchor.
- Grammar gains the `root-clause` (`root:<string 6..1> [ @<fret> ]`).
- The *Grip anchoring & movability* section is rewritten as three escalating anchor sources — **harmony-fixed → inferred → declared** — with the phantom-root rule and the tritone-shell ambiguity spelled out (no longer an "open impl note"; it's decided).
- New validation error for a muted-root grip missing its `@<fret>`.

**`req.md`** (re-locked, **v2**):
- **`IN11`** — rootless voicings as a first-class requirement.
- **`C9`** — the `root:` clause grammar (`@<fret>` required for a muted root; `@` separator).

Handles are append-only, so `IN1`–`IN10` / `C1`–`C8` kept their numbers. Bumping the req to v2 marks the design "behind the req" until it's re-anchored — I'll clear that when we build the plan (finalize design → mark idea done → plan citing the handles).

Ready for the plan whenever you say go.

## Rafa

I marked design done
write plan

## AI:

**`plan-001` written** (`pl_01KX0J0W78NGPRB7TMH8TM1P6S`) — 10 steps, each citing the req handles; `loom_verify_req` is structurally clean (all of `IN1`–`IN11` covered, no `EX`/dangling issues).

Grounding note: the dependency [[engine-derived-as-app-source]] is **done**, so the `CompingResolver` seam already exists — this plan is fully implementable now, not blocked. I read `CompingResolver.Resolve` to shape the cascade step against the real code (it iterates sections → bars → spans and picks per `Chord`; the override slots in per-span ahead of the ranking fill, and the by-`Chord` candidate cache has to become override-aware since two identical chords can differ in annotation).

The steps, in dependency order:

1. **Voicing-spec type + shared parser/writer** — grip + `root:`/phantom anchor, or reference *(IN2, IN3, C2, C9)*
2. **ProgressionParser `{}` + purity guard** — lex annotation, reject in stored progressions *(IN1, IN7)* · after 1
3. **SongParser `voice` directive** — `*` wildcard + degree selectors, dup error *(IN4, C1, C4, C6, C7)* · after 1
4. **Thread through realization; expose degree** — annotation→`RealizedSpan`, voice-map→`RealizedSong` *(IN1, IN4, IN5)* · after 2,3
5. **Grip realization & anchoring** — 3 anchor sources, rootless `@fret` *(IN3, IN11, C3, C9)* · after 1
6. **Reference resolution (fail-loud)** — `u:`/`a:`/`<package>:` *(IN2, IN6)* · after 1
7. **CompingResolver override cascade** — per-chord › degree › quality › fill *(IN5, C5, C8)* · after 4,5,6
8. **Round-trip serialization** *(IN8)* · after 2,3
9. **DSL-reference doc update** *(IN9, C7)* · after 7
10. **Dogfood: annotated blues on the fretboard page** *(IN10, IN11)* · after 7

Plan is at draft; not started. **Next step when you say go: Step 1** — add `VoicingSpec` + the shared parser/writer (`VoicingDslParser.cs`, `VoicingDslWriter.cs`, tests).

STOP — waiting for go.

## Rafa:

I started the plan
do the whole plan

## AI:

**Step 1 done** (voicing-spec type + shared parser/writer, 43/43 green). Then Step 2 surfaced an architecture decision I want your nod on before I thread it through the pure Music kernel.

### Finding

`InstrumentBoundaryTests` enforces (NetArchTest, IL-level) that **`ChordFlow.Music` must never depend on `ChordFlow.Instruments`**. `VoicingSpec` is in `Instruments.Guitar` (it's `FretPosition`-based). So the design's "the parsed chord carries an optional `VoicingSpec`" can't be literal — `ChordSpan` / `Song` are pure Music and can't hold a typed guitar voicing. This is the same wall the "progressions are pure harmony" rule already implies, now showing up in the type system.

### Recommended resolution (keeps the boundary intact)

**Carry the annotation as an opaque string at the Music boundary; parse it into a `VoicingSpec` in the Features layer** (which legitimately sees both Music and Instruments — that's where `CompingResolver` already lives):

- `ChordSpan` gains `string? VoicingAnnotation` — the raw inner text of `{…}`, uninterpreted. Music just carries it.
- `Song` gains the `voice` map as `selector → raw-spec-string`. The **selector** *is* pure harmony vocab (degree/quality/`*`), so `SongParser` validates it into a small `VoiceSelector` Music type and does the duplicate-selector check (C6) at parse time. Only the **spec value** (frets) stays opaque.
- `RealizedSpan` carries the opaque annotation + its `RomanDegree` (for degree-scoped matching); `RealizedSong` carries the voice map.
- `CompingResolver` (Features) parses the opaque strings via `VoicingDslParser.ParseSpec`, matches selectors, realizes grips / resolves references.

**One consequence:** a malformed grip inside `{…}` is caught at **realization**, not at progression-parse time (Music can't call the Instruments parser). That's consistent with how references already "fail loudly when the song is realized" (IN6) — same fail-loud, one layer later.

Net: no change to the DSL, the resolution semantics, or any req handle — only the internal representation shifts from "typed spec on the chord" to "opaque string on the chord, typed in Features." I'll update `design.md` + the domain-model ref to record it.

Good to proceed on that basis? (It's the only boundary-preserving option, so unless you object I'll take this and continue steps 2–10.)

## Rafa:

Yes, keeps the boundary intact, Music.Songs just admit DSL strings for voicings, no more
go

## AI:

Steps **1–5 are done and green** (voicing-spec grammar · ProgressionParser `{}` + purity · SongParser `voice` directive · realization threading · grip realizer with rootless/phantom anchors). Full suite 860+ passing.

Now at **Step 6 (reference resolution)** I hit the one part the design under-specified, and it needs your call before I wire it.

### The problem

The three reference forms resolve a voicing **by id** — but nothing in the comping path does a by-id lookup:
- The comping seam (`IStoredVoicingSource`) only lists candidates **by chord/quality**, and the stored `VoicingShape`s it hands out **carry no id at all**.
- `VoicingStore.Find(id)` exists but **collapses tiers** (highest of user/package wins) — it can't honor `{u: id}` vs `{pkg: id}` as *distinct* sources (needed for the "source filtered out → fail loud" rule, IN6).
- The `a:` (automatic) id scheme in your idea (`a: shell-C6`) was illustrative — the engine's real id format is the structured **`auto:shell:dom7:E`** (`AutomaticVoicingId`).

So references need a **new by-(source,id) resolver port**, threaded into `CompingResolver` from `ExerciseRendering` (the composition root) — a genuinely new cross-layer surface, not just more of the same.

### Two ways forward

**(A) Build references now.** Add an `IVoicingReferenceSource` port: `u:`/`<pkg>:` resolve by (origin, id) via an origin-aware `VoicingStore` lookup; `a:` parses the engine `auto:…` id → derives the grip; all realized at the chord's root, fail-loud on a miss (IN6). Wire it through `ExerciseRendering`. This keeps the thread whole but is ~1½ extra steps of port + DI wiring, and locks in **`a:` = the `auto:…` id format** (not `shell-C6`).

**(B) Ship the literal-grip cascade now, references as a short follow-up.** Do steps 7–10 with the per-chord `{c:}`/bare grips + `voice` grips fully working (that's the primary value and it's all built), and split `{u:}/{a:}/{pkg:}` into its own thread. This means amending the locked req to move IN2/IN6 into that follow-up. It's the cleaner scope boundary and matches your original "literal-grip first, references later" instinct.

My lean: **(B)** — the reference-by-id resolver is a separable, self-contained piece with its own id-scheme decisions (esp. `a:`), and deferring it lets the cascade + dogfood land clean now without a rushed cross-layer port. But you explicitly wanted references in, so it's your call.

Which way? (If A, confirm `a:` = the `auto:shell:dom7:E` format.)

## Rafa:

(A) i do not like middle way features

do option you consider better
- insert new step in plan-001 
- new plan-002 
- loom_quick_ship if it is small enough

## AI:

(A) it is — and no new step/plan/quick-ship needed: **reference resolution is already Step 6** of plan-001 (with Step 7 the cascade that consumes it). The scope didn't grow into new territory, it just got *more specific* than the plan first assumed. So I widened Step 6 in place (`loom_update_step`) to name the real shape:

- new `IVoicingReferenceSource` port + `VoicingReferenceSource` impl (Features)
- `u:` → origin-strict user-row lookup · `<packageId>:` → that pack's row · `a:` → the engine `auto:<family>:<quality>:<shape>` id (`auto:shell:dom7:E`) derived on the fly
- `VoicingStore.FindBySource(id, source, packageId)` — the origin-aware lookup the tier-collapsing `Find` couldn't do
- returns null on miss/filtered-out → the Step 7 cascade fails loud (IN6)

The `CompingResolver` wiring (new dependency threaded from `ExerciseRendering`) stays in **Step 7**, where the resolver is already being modified for the cascade — so no half-wired seam in between. Building Step 6 now.

## AI:

**plan-001 is done — all 10 steps, 883/883 tests green, req verify clean.** The whole explicit-voicing feature is in.

**What shipped**
- **One voicing-spec grammar** (`VoicingSpec` = `GripSpec`/`ReferenceSpec` + `GripAnchor`) parsed/written by `VoicingDslParser.ParseSpec` / `VoicingDslWriter.SpecToDsl`.
- **Two placements**: per-chord `{…}` on inline-Song chords (`ProgressionParser`, whitespace-optional binding, purity guard) and the Song `voice <selector> = …` default (`SongParser`, `*`-wildcard + degree selectors, dup guard).
- **Movable grips** (`VoicingRealizer.RealizeGrip`): bass/explicit-`root:`/phantom-`@` anchor, normalize-to-C, slide to root — **rootless voicings** first-class.
- **References** (`IVoicingReferenceSource`): origin-strict `u:`/`pkg:` + engine `a: auto:…`, fail-loud.
- **Most-specific-wins cascade** in `CompingResolver`: `{…}` › degree `voice` › quality `voice` › fill. Per-**occurrence** via a `RealizedSpan`-keyed override layer on `CompingPlan` (an annotation never leaks to identical chords) — the renderer now looks up by span.
- **Dogfood**: `songs/explicit_voicings_demo.dsl` in the default pack + an end-to-end test asserting the pinned grips reach the now/next-fretboard schedule.

**Decisions made along the way** (all captured above): opaque-string annotation at the Music↔Instruments boundary (D9), option-A references with `a: = auto:shell:dom7:E`, and the per-occurrence override key. Design + domain-model + DSL refs all updated.

**Not committed** — I didn't stage/commit anything (you didn't ask). Say the word and I'll commit the lot to `main`. Two small English notes from earlier still stand if you want them; otherwise this is ready for a look.

## Rafa:

tell me the English notes pls
commit