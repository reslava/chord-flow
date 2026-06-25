---
type: chat
id: ch_01KVXVM33HYT2F895RZ15ARW91
title: chromatic-degrees Chat
status: done
created: 2026-06-24
updated: 2026-06-25
version: 2
tags: []
parent_id: null
requires_load: []
last_ai_block: 23
---
# chromatic-degrees Chat

## Rafa:

read idea and global context
is fully grounded?
If so write design

## AI:

Read idea + global ctx + the DSL ref and the kernel sources (`ProgressionParser`, `SongParser`, `RomanDegree`, `Transposer`, `Scale`, `NoteSpeller`, `Chord`) and the jazz-blues bundle.

## Grounding: yes — every claim verified

1. **The wall is real.** `ProgressionParser.ParseDegreeQuality` requires `text[0]` to be a digit 1–7 (`ProgressionParser.cs:186-194`); a leading `#`/`b` throws *"missing a scale degree."* Confirmed.
2. **The borrow source exists.** The Song DSL already parses `#`/`b` in two places — `ParseKey` (`SongParser.cs:245-248`) and `ParseModSpec` (`SongParser.cs:281-292`, `b`/`#` → ∓1 semitone, mod-12). The vocabulary is right there to mirror.
3. **The carrier + resolution point are clear.** `RomanDegree(int Degree, Quality Quality)` is the key-independent token; it resolves in `Transposer.ChordFor` → `Scale.DegreePitchClass(degree)` → tonic + diatonic interval. An accidental is one `Mod12(±1)` at that seam.
4. **The dogfood target is exactly as described.** `jazz_blues_standard.dsl` bar 6 is the second `47` in `17 47 17 17 47 47 17 67 2-7 57 17_67 2-7_57` — the `#IVdim7` stand-in. `jazz_blues_f.dsl` plays it in **F**.
5. **Refs to sync confirmed:** Progression DSL section of `chordflow-dsl-reference.md` + the domain-model ref.

So the idea is implementable. **But** writing the design forces one architecture decision I don't want to silently pick:

## The open decision: does `#`/`b` carry *spelling*, or only *pitch*?

The accidental is two things at once: a **transposition** (±1 semitone on the resolved root) and a **spelling intent** (`#IV` wants F♯, `bII` wants D♭). The transposition is trivial. Spelling is the catch:

`NoteSpeller.Name(pitchClass, key)` picks sharp-table vs flat-table **purely from the key** (`NoteSpeller.cs:37-42, 84-88`), and `Chord` stores only `(PitchClass Root, Quality)` — no per-chord spelling. So a chromatic root gets spelled by the key's convention, *ignoring which accidental you typed*:

- **In F (the actual dogfood key, flat-spelled):** `#4dim7` → IV(B♭)+1 = pc 11 → flat table gives **"B"** ✓; `bII7` → II(G)−1 = pc 6 → **"G♭"** ✓. Both correct *by luck of the key*.
- **In C (flat-spelled by the table's C-default):** `#4dim7` → IV(F)+1 = pc 6 → flat table gives **"G♭"** ✗ — you typed `#IV`, you want **F♯**. Wrong enharmonic.

So the cheap version is correct *for the F jazz blues* but wrong in general. Two ways to go:

**Option A — pitch-class only (minimal).** Add the accidental to `RomanDegree`, apply ±1 in `ChordFor`, spelling stays key-derived. Touches only `RomanDegree` + parser + `Transposer`. Validation passes for the F bundle. Cost: `#IV`/`bII` mis-spell in keys whose table fights the accidental (G♭dim7 instead of F♯dim7 in C). The DSL would silently not mean what it says.

**Option B — accidental carries spelling intent (durable).** Thread the typed direction through so `#` forces sharp-side, `b` forces flat-side for that root. Concretely: `RomanDegree` gains the accidental, `Chord` gains an optional spelling hint (e.g. `Accidental? Spelling`), and `NoteSpeller.Name` gets an overload that honors the hint and falls back to the key table when absent. Bigger blast radius (`Chord` + `NoteSpeller` + every `Chord` construction site / renderer label path), but `#IV` *always* means F♯ and `bII` *always* means D♭, in every key.

**My recommendation: Option B.** It's the only one that makes the feature *mean* what it says, it matches the "correct/durable over minimal" stance, and the spelling-hint seam is reusable the moment we add real tritone-sub / secondary-dominant transforms (the north star in `jazz-blues-design`) — those emit chromatic roots with a known spelling direction too. Option A would have to be torn out and redone then.

One sub-point either way: I'd represent the accidental as a small `Accidental` enum (`Natural`/`Sharp`/`Flat`) rather than a bare `int`, and accept any single `#`/`b` with no semantic range guard (it's chromatic by definition — `#7`, `b1`, etc. just resolve mod-12).

**STOP — which spelling model do you want (A or B) before I write the design?** Everything else (parser grammar, `RomanDegree` field, ref updates, the bar-6 fix) is settled regardless of the answer.

## Rafa:

### does `#`/`b` carry *spelling*?

For curiosity: check what happens if we simple assign 
- for any `#n` same spelling than `n` with `#`. if 4 is F #4 is F# 
- for any `bn` same spelling than `n` with `b`. if 2 is G b2 is bG

## AI:

Good instinct — what you're describing is actually **better than my Option B**, and it's the theoretically correct one. Let me name it and then show the one place the "simple" version bites.

## Your model = spell the chromatic note from its *parent degree's letter*

"`#4` = the 4th, raised" — keep the **letter** of degree 4 and bump its accidental by +1; "`b2` = the 2nd, lowered" — keep the letter of degree 2, accidental −1. That's the real musical rule (an *altered diatonic*), and it's stronger than B because it nails the **letter name**, not just the sharp/flat side.

### But "simple string decoration" only works in C

Your examples are all in C, where degrees 1–7 spell as **natural letters** (C D E F G A B). There, `#4` = "F" + "#" = **F#** ✓ and `b2` = "D" + "b" = **Db** ✓ — clean.

The moment the key has an **accidental diatonic degree**, naive "append the glyph" breaks. The real dogfood key is **F**, and there degree 4 is **Bb**:

| | degree spells | naive decorate | correct (combine accidentals) |
|---|---|---|---|
| `#4` in C | F | F# | **F#** ✓ |
| `b2` in C | D | Db | **Db** ✓ |
| `#4` in **F** | **Bb** | **Bb#** ✗ (nonsense) | **B** ✓ (Bb raised = B natural) |
| `bII7` in **F** | G | Gb | **Gb** ✓ |

So `#IVdim7` in F must come out **Bdim7** — and it does, but only if `#` *combines* with the degree's existing −1 accidental (Bb → B), not by pasting a `#` onto the string. That combine step is the whole trick.

### What that needs

A **letter-aware degree spelling**: degree n's letter = tonic's letter advanced n−1 steps, and its accidental = whatever makes it hit the pitch class. Then `#n`/`bn` = same letter, accidental ±1, rendered as natural / ♯ / ♭ (/ double in the rare edge). We don't have that today — `Scale.DegreePitchClass` returns a bare pitch class and `NoteSpeller` picks a name from a key table. It's a small, clean addition though, and for *diatonic* chords it produces exactly what the current speller does, so it's a strict superset — durable, reusable for the future transforms.

### One wrinkle to decide

Letter-preserving spelling is *pure* but can produce pedantic names at exotic degrees: `b4` in C → **Fb** (not E), `#7` in C → **B#** (not C), and in principle double accidentals. For the actual vocabulary (`#IV`, `bII`, `bVI`, `bVII`, `#V`…) it never gets weird. My call: **keep it letter-pure** — if someone types `b4` they're explicitly asking for the lowered-4 letter; collapsing it to E would throw away the information the accidental carries. Rare and self-inflicted.

So I'd write the design around **your model (letter-preserving altered-diatonic), letter-pure**. It supersedes both A and B.

One small correction on the example, for accuracy: in C, **degree 2 is D**, so `b2` = D♭ — it's **degree 5** that's G (`b5` = G♭). And by convention the glyph trails the letter (`Gb`, not `bG`).

**STOP — confirm "letter-preserving, letter-pure" and I'll write the design.** (Open the door now if you'd rather simplify the rare `Fb`/`B#` cases to their common enharmonic — that's the only knob left.)

## Rafa:

In my opinion preserving the letter is thenatural behavious composer expect. If he wanted other spelling probably he will use other degree number.

I think this also apply to simplify the rare `Fb`/`B#` cases to their common enharmonic, I think it is not what composer wanted.

So what do you you think? Any other beter tweak or approach?

## AI:

Agreed on both counts — and your reasoning *is* the design principle, so let me crystallize it and then give you the one grounding finding that makes this **smaller and cleaner** than I implied two turns ago, plus a couple of tweaks.

## The principle (locked)

> **The written degree — not the key — spells the chord root.** `#4` is the raised 4th's *letter*; `b2` is the lowered 2nd's *letter*. Letter-pure, no enharmonic collapse. If a composer wanted E instead of F♭, they'd have written a different degree.

That's clean because it makes spelling **deterministic from the token**, fully independent of the key's sharp/flat convention. Nice invariant to design against.

## Grounding finding: spelling surfaces in exactly *one* place

I traced where a root's spelling actually becomes visible. It's **only** `ChordSymbol.Format(chord, key)` → `NoteSpeller.Name(root, key)` (`ChordSymbol.cs:35`). That one string feeds three things: the `{ch "…"}` **chord label**, the `\chord` **diagram name**, and the **now/next-fretboard schedule** (`AlphaTexRenderer.cs:390-400, 434`).

What it does **not** touch:
- **Tab** renders `fret.string` — it shows *no letters at all*, so spelling is irrelevant there.
- The **standard-notation staff** noteheads are spelled by **alphaTab itself** from the fret positions — we don't drive those without per-note accidental injection (out of scope).

So we are **not** building staff-spelling infrastructure (what I overestimated earlier). We're making one **chord-symbol label** read correctly. Much smaller blast radius.

## The seam

1. `RomanDegree` gains `Accidental` (enum `Natural`/`Sharp`/`Flat`).
2. Introduce a small durable primitive the domain lacks today: `NoteName(Letter, int Accidental)` — a *spelled* note (the kernel is all pitch-class right now). `PitchClass` stays spelling-free, so constraint **C4** ("spelling is derived, never stored on the pitch class") still holds.
3. `Transposer`, when realizing an accidental'd degree, computes the letter-pure `NoteName` (tonic's letter advanced n−1 steps, accidental combined with the typed `#`/`b`) and hangs it on the realized `Chord` as an **optional** `RootSpelling`.
4. `ChordSymbol.Format` uses `RootSpelling` when present, else falls back to today's `NoteSpeller.Name(root, key)`. **Diatonic chords carry no override → byte-identical output.** The Bb→B combine (the F dogfood case) falls straight out of the accidental arithmetic.

## Two tweaks / judgment calls for you

1. **Symbol vs staff mismatch — accept it for v1.** Because alphaTab spells the staff noteheads on its own, the *symbol* could say `Gb7` while the *standard-staff* notehead renders as `F#`. Cosmetic, staff-only, and only on chromatic chords. Controlling it means injecting accidentals per note — I'd punt that to a later notation pass, not this thread. (Tab + the chord symbol — the things people actually read here — are correct.)
2. **Override-and-fallback, not a rewrite.** Apply the degree-derived spelling *only* to accidental'd degrees, leaving diatonic chords on the existing key-table path (which the key signature needs anyway). Unifying all chord-symbol spelling onto `NoteName` is a tempting cleanup but it's a separate thread — keeping this surgical means zero risk to existing output.

Net: your model, letter-pure, carried as an optional `NoteName` on the realized chord, honored by `ChordSymbol`. If that lands right, **I'll write the design around it.**

## Rafa:

Questions:

1. When and where alphaTab spells the staff noteheads? (I did not see that yet). We need to name the chord `\chord ("Bb7" 1 3 1 3 1 x)` and its used in standard and tab notation.
2. Agree, but why unifying all chord-symbol spelling onto `NoteName` is a tempting cleanup? I think we do not need that. Please briefly explain it.

## AI:

## 1. Where alphaTab spells noteheads

You're right that the **name** is ours everywhere — let me separate the three things our alphaTex emits, because only one of them is alphaTab's to spell:

| What we emit | In **tab** view | In **standard** view |
|---|---|---|
| `\chord ("Bb7" 1 3 1 3 1 x)` — diagram **name** + fret grid | diagram label = **our "Bb7"** | diagram label = **our "Bb7"** |
| `{ch "Bb7"}` on the beat — chord **symbol** text | text above staff = **our "Bb7"** | text above staff = **our "Bb7"** |
| `(8.6 10.5 8.5 …)` — the **note group** (`fret.string`) | **fret numbers** (no letters) | **noteheads on the staff** |

The first two are pure strings *we* write — so the chord **name** is under our control in both views, exactly as you say. The thing we **never** spell is the third row's **noteheads in the standard staff**: we hand alphaTab `8.6` (fret 8, string 6), and alphaTab converts `fret + string + tuning → pitch → a notehead with a line position and accidental` using its **own** enharmonic logic. We never pass it "F♯ vs G♭". That conversion happens inside alphaTab's layout/rendering, only when a **standard (or both)** staff is drawn — there's nothing in *our* code or alphaTex that does it, which is why you didn't see it: it isn't in our source.

So the "mismatch" is narrow: our **`{ch}` label says `Gb7`** while alphaTab's **individual root notehead** on the standard staff *might* draw as `F♯`. It never affects any name/label, never affects tab, and for our actual cases (e.g. `#IVdim7` → `Bdim7`, root B-natural) there's usually no ambiguity at all. That's why I'd accept it for v1 rather than inject per-note accidentals. **Net: the chord name `Bdim7`/`Gb7` is correct in tab and standard alike — only loose noteheads on the standard staff are alphaTab's call.**

## 2. Why unifying was "tempting" — and why we skip it

"Tempting" only in the **DRY** sense: my `NoteName` letter-pure speller would actually produce the *correct diatonic spellings too* (it's a strict superset of the key-table), so you *could* route **every** chord symbol through one path and delete the key-table branch — "one way to spell a chord root" instead of two.

We **don't** need it, and I agree we skip it, because:
- The two paths **already agree** on diatonic chords, so unifying changes **zero output** — it's a pure refactor with no benefit to this feature.
- `NoteSpeller`'s key-table **stays** regardless (it spells the `\ks` key signature and the title's key name) — so we wouldn't even delete it, just swap which path chord *roots* use. Churn for nothing.
- It widens the blast radius across every existing chord and risks the byte-identical guarantee, for a cleanup that has its own separate justification.

So: keep **override-and-fallback** (chromatic degrees get the `NoteName`; diatonic stays on the key-table). The unify idea is a possible *future* tidy-up, not part of this thread.

That settles both. If you're satisfied, say the word and I'll write the design around: *written-degree-spells-the-root, letter-pure, carried as optional `NoteName` on the realized chord, honored by `ChordSymbol` with fallback.*

## AI:

Design written → `chromatic-degrees-design.md` (`de_01KVXYQ0HR654X95B5HCVJC64K`, status **draft**). It captures everything we settled:

- The locked principle (written-degree-spells-the-root, letter-pure) — §2
- Grammar: optional single `#`/`b` before the degree; single-accidental input only — §3
- Domain: `RomanDegree.Accidental`, the new `NoteName` primitive, `Chord.RootSpelling` (PitchClass stays spelling-free, C4 intact) — §4
- The letter-pure combine algorithm with the F→`Bdim7` worked case — §5
- The one-spot seam (`Transposer` → `Chord.RootSpelling` → `ChordSymbol`, override-and-fallback, byte-identical diatonic) — §6
- Both accepted trade-offs (staff-notehead mismatch; no spelling rewrite) — §7
- Tests, ref sync, out-of-scope — §8–10

**Next:** per our flow it's **idea→design→req→plan** — so the move is to mark idea + design done and **generate `req.md`** (the locked include/exclude/constraints anchor) before any plan. I haven't touched any status.

STOP — review the design. Say `go` to proceed to the req, or tell me what to adjust.

## Rafa:

I already marked idea design done
Write req, lock it
Wirte plan

## AI:

Req + plan are in.

- **`req.md`** (`rq_01KVXYXMK1W0NNBCQAM69X718Y`) — **locked**. 10 Included, 6 Excluded, 7 Constraints.
- **`plans/chromatic-degrees-plan-001.md`** (`pl_01KVXYYPDKA2D54RPK600V0X50`) — 8 steps, bottom-up, every step citing its IN/C handles, dependency graph wired to step ids. `loom_verify_req` is structurally clean (no uncovered Included, no excluded-citation violations, no dangling citations).

The 8 steps:
1. `RomanDegree.Accidental` (IN2, C5)
2. `NoteName` primitive (IN3)
3. `Chord.RootSpelling` (IN4, C1)
4. Parser `#`/`b` prefix + tests (IN1, C3, IN10) — needs 1
5. `Transposer` letter-pure spelling (IN5, IN7, C4) — needs 1,2,3
6. `ChordSymbol` override-and-fallback + tests (IN6, IN10, C2, C7) — needs 3,5
7. jazz-blues bar 6 → `#4dim7` (IN8) — needs 4,5,6
8. ref sync (IN9) — needs 7

**Next: Step 1 — `RomanDegree.Accidental`**, touching `src/ChordFlow.Core/Music/Harmony/RomanDegree.cs`.

STOP — say `go` to implement Step 1 (or `do steps 1–N` / `do the whole plan` to authorize a range).

## Rafa:

I started the plan
Do whole plan

## AI:

Implemented **steps 1–6** of the plan — all green (127 passed). Then **step 7 (the `#4dim7` content swap) hit a real blocker**, so I stopped per the design-decision rule.

**Steps 1–6 done & verified**
- `RomanDegree` gained `Accidental {Natural,Sharp,Flat}` (defaulted — call sites still compile).
- `NoteName(char Letter, int Accidental)` primitive with `Symbol` (`#`/`b`, double-acc).
- `Chord.RootSpelling` (nullable, defaulted).
- Parser accepts one leading `#`/`b`; rejects `##4`/`#b4`/bare `#`/`b`/`#8`.
- `Transposer` letter-pure spelling: shifts pitch by the accidental always; sets `RootSpelling` on the key path. Verified F `#4`→B, F `b2`→Gb, C `#7`→B♯, C `b4`→F♭.
- `ChordSymbol` honors `RootSpelling`, else key-table fallback (byte-identical for diatonic).

**The blocker (step 7).** Dropping `#4dim7` into `jazz_blues_standard.dsl` makes the **seed render tests** throw:

> `NotSupportedException: The MVP shell shape covers Dominant7, Minor7, and Major7 only; got Diminished7.`

The *spelling* side is fine — the chord **symbol** renders as `Bdim7`. What's missing is the **voicing**: `BeginnerShellStrategy` (the only generated voicer; `VoicingBook` falls through to it) has no `Diminished7` arm, so the fretboard can't be voiced. The req/plan covered grammar + spelling but never the renderer's voicing table — so IN8 (`#4dim7` in the default pack) can't actually render. That gap is the design decision.

**Options**

1. **(Recommended) Add a `Diminished7` shell shape** — one switch arm in `BeginnerShellStrategy`: `Quality.Diminished7 => (-2, -1)`, voicing **R + ♭3 + ♭♭7** (the ♭♭7 = 9 semitones; consistent 3-note shell, ♭5 omitted like the existing shells omit the 5th) + a unit test. This is a new plan step (slot it as 6b, before step 7). Makes the feature work end-to-end; matches "durable over minimal." Touches the voicing engine → domain/architecture ref gets a line in step 8. *Note:* it widens scope from "spelling" into "voicing," which is why I'm asking rather than just doing it.

2. **Defer the content swap** — back out step 7, ship grammar + spelling now, keep the `47` stand-in in bar 6 until a dim7 voicing lands separately. Suite goes green immediately, but the motivating real-world case (the actual `#IVdim7` in the pack) isn't delivered.

3. ~~Voice it as a supported quality~~ — musically wrong (a `#IVdim7` must be a dim7). Rejected.

I lean hard on **Option 1** — it's a two-line voicing arm and it's the only way IN8 is genuinely satisfied. But it pulls the voicing engine into this thread's scope, so it's your call.

The suite is currently **red** (2 seed-render tests) because step 7's content is in place. Say the word and I either add the dim7 voicing (Option 1) or revert the content (Option 2).

**STOP — which option?**

## Rafa:

Option 1
Do steps 6b,7,8

## AI:

**Done — whole plan complete, 699/699 tests green.** Steps 6b/7/8 landed and the plan auto-closed.

**Step 6b — dim7 voicing.** `BeginnerShellStrategy` gained `Quality.Diminished7 => (-2, -1)`: root + ♭3 + ♭♭7 (the ♭♭7 is a semitone under the minor 7th). B°7 voices as `(s5:2 s4:0 s3:1)` = B / D / A♭ — verified across C/F/B roots, no negative frets. New `BeginnerShellStrategyTests`.

**Step 8 — content.** `jazz_blues_standard.dsl` bar 6 is now `#4dim7`; in F (`jazz_blues_f`) it renders the real **Bdim7** passing chord. Seed render tests pass.

**Step 9 — refs synced.**
- *DSL ref*: a "Chromatic degrees (#/b)" subsection (`#4dim7`→`Bdim7`, `b27`→`Gb7`, the combine rule, letter-pure/no-collapse, single-accidental input), two worked rows, updated error notes.
- *Domain-model ref*: `RomanDegree.Accidental`, the `NoteName` primitive, `Chord.RootSpelling`, `Transposer` letter-pure spelling, `ChordSymbol` override, and the dim7 shell arm.

**Process notes**
- I tightened step 5 while reading the locked req: `ChordFor` now sets `RootSpelling` **only for accidental'd degrees** (was: all chords on the key path). Same rendered output, but it honors **C2** ("diatonic chords carry no RootSpelling") and **IN5** ("for accidental'd degrees") literally. Added `Realize_DiatonicDegree_KeyPath_LeavesRootSpellingNull`.
- The dim7 voicing was genuine new scope (the voicing engine had no `Diminished7` arm — that was the blocker). I amended the req **append-only** with **IN11** and re-locked it (now v2), so the new step 6b is traceable. No existing handle was renumbered.

**Req coverage:** `loom_verify_req` structurally clean — 0 uncovered Included, 0 excluded violations, 0 dangling citations.

Nothing staged. The thread's feature is delivered end-to-end: `#`/`b` degrees parse → resolve to correct pitch → spell letter-pure → render a real voicing.

## Rafa:

commit this thread