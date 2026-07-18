---
type: chat
id: ch_01KXTZ0WM0FRM358MKC0J1STTQ
title: harmonic-overlay Chat 001
status: done
created: 2026-07-18
version: 1
tags: []
parent_id: null
requires_load: []
last_ai_block: 13
---
# harmonic-overlay Chat 001

## Rafa:

domain/harmonic-analysis/idea.md has been already implemented

now we need to show up its results

practice and content page share same UI components, so both should show the harmonic-overlay as an option when chord-sheet view is selected

is it all grounded for design?

## AI:

Yes — I read the analyzer, the builder, and the sheet model, and this is unusually well-grounded. The seam is clean, and better than the idea assumed, **every dependency has already landed**. Grounding first, then the three real decisions the design still has to make.

## What's already in place (nothing to build twice)

**The analyzer is a pure, glyph-free pass — exactly the shape a consumer wants.** `HarmonicAnalyzer.Analyze` → `ChordAnalysis { Category, Function (RomanDegree), Target?, SourceMode? }` (`Music/Harmony/ChordAnalysis.cs`). `Category` ∈ Diatonic / SecondaryDominant / SecondaryLeadingTone / Borrowed / TritoneSub / Chromatic. It's **pitch-based** (`chord.Root` + `Key`), so it analyzes the *realized* (transposed) chord correctly. It isn't wired into any consumer yet — this thread is its first.

**The builder already holds exactly the input the analyzer needs.** `ChordSheetBuilder.ToChordRef` (`Features/ChordSheets/ChordSheetBuilder.cs:193`) walks each `RealizedSpan` with `section.Key` in scope — that *is* the `(chord, key)` pair, one call per chord. And the field to retire is right there: line 212 sets `Roman: RomanFunction(span.Degree)`, with a comment already promising "those labels come later from the harmonic-analysis pass." So scope-item-2 is essentially a one-line re-source.

**The carry-to-JS path is the established dumb-drawer pattern.** `ChordRef` already carries every notation as a pre-formatted string (Concrete / Degree / Roman) plus a colour-key on tones (`Function`: root/third/…). Adding analysis is the same move — a formatted glyph string + a category colour-key. The `loadScore` reply already feeds this model to both surfaces via `ExerciseRendering.RenderWithSheet` → `ChordSheetBuilder`, so overlay-on is a pure re-draw, no C# round-trip — matches your validation criterion exactly.

**Your Practice + Content claim is exactly right.** Both pages mount `render-surface-component.js` → ChordSheetR via `ChordFlowSheetView` (`chord-sheets.js`), whose display strip already owns the sheet toggles. One overlay toggle added there shows on both pages for free. (Only caveat: Content renders the sheet for progression/song, not rhythm — same as today.)

**Bonus — the soft dependency is discharged.** The idea gated minor-key display on [[first-class-minor-keys]]; that thread is now `done`, and the analyzer is already minor-symmetric (its D4). So the full major **and** minor overlay ships in this one thread, not two.

## The three decisions the design has to settle

1. **Where the glyph is formatted** (`V7/ii`, `♭II7`, `iv`). The idea left this as "the JS drawer *or* a Rendering formatter." I'd commit to **Core**: a `Rendering` formatter turns `ChordAnalysis` → the glyph string, carried on `ChordRef` alongside a `Category` colour-key — identical to how `ChordSheetTone` carries a formatted `Interval` + colour-key `Function`. That keeps ChordSheetR a dumb drawer (C1); formatting in JS would be the first music-theory-in-JS crack. **Recommend Core.**

2. **Replace vs. add.** When the overlay is on, does the analysis glyph *replace* the Roman row (`ii` → `V7/ii`), or sit as a separate below-cell adornment line (like the tone strip / diagram)? The idea says "upgrades that field," which reads like *replace* — a sub-mode of the Roman notation ("plain diatonic ⇄ full analysis") rather than a fourth adornment. I lean replace/sub-mode, but this is a UX call — **your take?**

3. **Colour vocabulary for non-diatonic.** Category → colour (diatonic neutral; distinct hues for secondary-dominant / borrowed / tritone-sub / chromatic). A small string enum carried to JS, palette lives in the drawer — same shape as FretR's function palette. Just needs the palette picked.

None of these block starting the design doc — they're what the design *decides*. If you're good with my leanings on 1 and 3 and give me a call on 2, this is ready to move to design (then req → plan). Want me to draft the design?

## Rafa:

1. Core. Renderers should keep being dumb. And the info could be reused by other renderer or tool.
2. I also lean replace/sub-mode. Have any sense having both info showed together? Coul apport any useful info for learning/teaching purpose? Maybe yes, not sure... What do you think?
3. You pick some pastel colours, keep in mind the dark and light themes.

## AI:

Good — 1 and 3 are settled (Core formatter; I'll pick theme-aware pastels in the design doc, one hue per non-diatonic category, checked against both the light and dark sheet themes).

On 2 — yes, I think showing both **does** have real teaching value, and the nice part is it costs us almost nothing. Here's the reasoning:

**The two labels carry genuinely different information, and the *gap between them* is the lesson.**
- The honest degree says **where** the chord sits (position + its own quality): in C, `A7` = `VI7`, `Db7` = `♭II7`, `Fm` = `iv`.
- The analysis glyph says **what it does** (function): `A7` = `V7/ii`, `Db7` = tritone sub of V, `Fm` = borrowed iv.

The way this is actually taught is the *equation*: "A7 in C is VI7, but it functions as V7 of ii." Seeing `VI7 → V7/ii` side by side **is** that sentence. The honest degree grounds the ear ("it's built on the 6"), the analysis names the role ("it's a secondary dominant pulling to ii"). For a learner, the bridge is more instructive than either label alone.

**But it only adds information for non-diatonic chords.** For a diatonic chord the two labels are identical (`ii` = `ii`), so a forced always-both view would be redundant noise on 90% of a sheet. That points at the clean answer:

**Make it a 3-state Roman sub-mode, not a binary — and it's free because the model already carries both fields.** Since `ChordRef` will carry *both* the honest `Function` string and the analysis glyph + category, the choice between them is a pure JS draw decision, zero extra Core work, zero round-trip. So we don't have to pick replace *or* both — we offer:
- **Diatonic** — honest degree only (today's `Roman`, the plain view).
- **Analysis** — the functional glyph (replace). *Default* — it's the point of the thread.
- **Both** — the teaching view: the honest degree bridging to the function.

Refinement that makes "Both" feel intentional rather than cluttered: in Both mode, **only pair up when the two differ** (i.e. non-diatonic chords) — a diatonic bar shows its single label as usual. That way the Both view naturally *spotlights* exactly the interesting chords, each shown as `position → function`, and diatonic passages stay clean.

So my recommendation for the design: carry both fields always; Roman label becomes the 3-state sub-mode above; default Analysis; Both pairs only on divergence. That resolves your uncertainty by not forcing the choice on the user — the learner flips to Both when they want the "why," and back to Analysis when they just want the chart.

Sound right? If so, all three decisions are closed and I'll draft the design (then req → plan).

## Rafa:

Yes, sounds right
write design + req

## AI:

Done — both drafted for your read-through:

- **`design.md`** — the seam (analyzer already pure + the builder already holds `(chord, section.Key)`), the three settled decisions (D1 Core formatter · D2 3-state sub-mode · D3 theme-aware category colour), the `ChordRef` model change (re-source `Roman` from the analyzer's `Function`, add `Analysis` glyph + `Category` colour-key strings, retire `RomanFunction`), the Rendering formatter, the builder + JS changes, scope/non-goals, validation, and ref-doc impact.
- **`req.md`** — 12 Included (`IN1`–`IN12`), 6 Excluded (`EX1`–`EX6`), 4 Constraints (`C1`–`C4`), still `draft`.

Two things worth flagging that the design nails down beyond our chat:
- The honest `Roman` and the `Analysis` glyph **only diverge for the two secondary categories** (the `/target` suffix). Borrowed / tritone-sub / chromatic keep the honest glyph (`iv`, `♭II7`) and let **colour** carry the signal — which is why "Both" mode naturally shows a pair *only* on secondary functions, exactly the pedagogically rich ones.
- The old chord-sheets-maker design had sketched a `ChordAnalysis? Analysis` field on `ChordRef` (never actually shipped). The design **reverses** that: carry pre-formatted strings, not the raw struct — keeps the struct off the wire and JS out of formatting.

Nothing is committed and the req is still `draft`. Want me to **lock the req** (`loom_finalize_req`) and draft **`plan-001`** next? Or read them over first and flag edits.

## Rafa:

I marked idea + design done and locked req
write plan

## AI:

`plan-001.md` written — 5 steps, every `IN`/`C` handle covered:

1. **Core Rendering formatter** (`HarmonicAnalysisFormatter`) — `HonestDegree` / `Glyph` / `CategoryKey`; the `RomanFunction` logic moves here. *(IN3, IN5, C2)*
2. **`ChordRef` fields + builder consumes analyzer** — add `Analysis` + `Category`, wire `ToChordRef` to `HarmonicAnalyzer` + formatter, re-source `Roman`, delete `RomanFunction`. *(IN1, IN2, IN4, IN10, C4)*
3. **Builder golden tests** — dominant-blues must-not-over-label, borrowed-iv, secondary-dominant divergence (`VI7` → `V7/ii`), minor-key fixture; hand-reasoned oracle + pasted actual. *(IN11, IN10)*
4. **ChordSheetR overlay + strip selector** — 3-state Roman sub-mode + theme-aware category colour (light/dark); pure re-draw, both pages. *(IN6, IN7, IN8, IN9, C1, C3)*
5. **Reference-doc sync** — domain-model + architecture refs. *(IN12)*

Dependency chain: 1 → 2 → {3, 4} → 5. Steps 3 and 4 are independent once the fields land, so they can go in either order (or parallel).

Ready to implement. `do step 1` and I'll start, or `do plan` to run the whole thing. Want me to `loom_verify_req` first to confirm scope coverage before we begin?

## Rafa:

I verified req: covered
I started plan
do plan

## AI:

`plan-001` is fully implemented — all 5 steps ✅, plan auto-closed to `done`.

**What landed:**
1. **`HarmonicAnalysisFormatter`** (`Rendering/ChordSheets/`) — `HonestDegree` / `Glyph` / `CategoryKey`; 7 unit tests, one per category.
2. **`ChordRef`** gained `Analysis` + `Category`; `ChordSheetBuilder.ToChordRef` now calls `HarmonicAnalyzer.Analyze(chord, section.Key)`, re-sources `Roman` from the analyzer, and `RomanFunction` is deleted.
3. **Builder golden tests** — secondary-dominant divergence (`VI7` vs `V7/ii`), borrowed-iv, tritone-sub, the dominant-blues must-not-over-label, and an A-minor fixture (authored in the C parent-major frame). **Full suite: 1058/1058 green.**
4. **ChordSheetR + strip** — the 3-state **Analysis** selector (Diatonic / Analysis / Both) + theme-aware pastel category tint (light & dark variants). Pure re-draw, no round-trip; shows on Practice and Content alike.
5. **Ref-doc sync** — domain-model + architecture refs updated.

**One thing to flag honestly:** I verified the C# path exhaustively (1058 tests) and node-parsed both JS files clean, but I did **not** drive the running app to eyeball the overlay — the field-name casing is correct by parity (the new fields sit alongside the working `Roman`/`Concrete`), so I'm confident, but a visual check of both themes on a jazz-blues sheet is the natural closing dogfood step. Want me to launch it, or will you eyeball on your next run?

Two small judgment calls I made, flag if you'd differ:
- **Default = Analysis on** (per your D2), so out of the box non-diatonic chords are tinted even in Letter mode — a "highlight the interesting chords" default.
- **Colour tints the primary token** (whatever notation is shown), not just the Roman label — so a borrowed chord reads special even when you're viewing letter names.

Nothing committed yet. Say the word to commit (I'd stage the Core + JS + tests + the two refs together), or `code quick`-style if you want me to run the app first.

## Rafa:

I checked the app, working, well done!
if nothing more is pending commit

## AI:

Great — thanks for the eyeball. Nothing pending: all 5 steps done, plan closed, final build clean and full suite 1058/1058 green.

Committing now — staging the Core formatter + `ChordRef`/builder change, the ChordSheetR + strip JS, the formatter & builder tests, the two updated refs, and the thread's Loom docs (design/req/plan/done + this chat) together as one unit.