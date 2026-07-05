---
type: chat
id: ch_01KWSD1TBFGAJ318Z11027TQ34
title: song-default-feel Chat 001
status: done
created: 2026-07-05
version: 1
tags: []
parent_id: null
requires_load: []
last_ai_block: 37
---
# song-default-feel Chat 001

## Rafa:

read idea

### Open questions

- Is a per-content default, songs, rhythm
- Does the default feel travel in content packs. I reframe to: it belongs to songs, rhytnm.

is all clear?
is all grounded?

## AI:

Read. Short answer: **clear, mostly grounded — with one real gap in the "songs + rhythm" reframe.**

## Is it clear?

Yes. The spine reads cleanly:

- Default feel = **catalog metadata** (a *suggestion*), same family as `genre:` / `subgenre:` / `tags:`.
- It seeds the initial `Exercise.TripletFeel`; the play-time transport control still overrides it.
- The **realized rhythm stays feel-free** — swing still happens at render via `\tf`. So C4 isn't broken, it's *carved out*: the invariant forbids feel in the **body/realized pattern**, and a header suggestion never touches that. Coherent.

## Is it grounded?

Grounded for **Song** and **Progression**, not for **Rhythm** as written.

The idea leans on "it's just another catalog-metadata field, so packs carry it by construction." That's true only for entities that *have* a catalog header. Per `chordflow-dsl-reference`:

> `genre:` / `subgenre:` / `tags:` follow as usual (the catalog header) … **Rhythm files carry no catalog metadata.**

So per entity:

1. **Song** — has a catalog header. Natural home. Packs carry it for free. Fully grounded.
2. **Progression** — has a catalog header. Grounded, same mechanism.
3. **Rhythm** — has **no catalog header at all** today. So "belongs to rhythm" is *not* "just another field" — it means **giving Rhythm a catalog header it doesn't have**. That's a new model surface, not a metadata drop, and the pack-format claim doesn't hold for it yet.

## The reframe's live tension

Your reframe ("it belongs to songs, rhythm") is interesting *because* feel is arguably most native to **Rhythm** — a shuffle pattern *wants* swing more than a Song does. So there's a genuine pull to put the default there. But that collides head-on with the current rule that Rhythm carries no catalog metadata.

That's the one thing to settle before design: **does Rhythm grow a catalog header (feel + eventually genre/tags), or does the default live only on Song/Progression and a swung Rhythm just relies on the Song's default?** Everything else in the idea is clear and grounded.

The two idea open-questions are fine as-is: per-section feel is already scoped **out** (correct), and the pack question is answered **yes for Song/Progression** — the Rhythm caveat above is the only asterisk.

## Rafa:

I think we have to clarify some concepts.
It is little difficul to design this correcly. 

Thinking out loud with you...

Catalog is a set of related songs, progressions, rhythms and voicings. Is correct?
Define catalog.

Default feel has nothing to do with catalog.
Default feel belongs to songs, progressions, rhythms (comping, lead). Currently this feel is a UI control in ScoreR and always need to be picked up by user.
Example, rendering a song, hhen we generate the alphaTex:
If the song have default feel:
  write `tf {song-feel}` in the header (ScoreR feel is override)
If the song have no default feel:
  write `tf {score-feel}` (this become `song-fell`)
If the next progression have default feel and it is different of `song-feel` then
  write `tf {progression-feel}` in the header, after progression write `tf {song-feel}` again

The problem is this is not direct, a little complicated.

### Rhythm

This is difficult

(a) `:3 X.X X.X X.X X.X` no need default feel
(b) `X.X.X.X.X.X.X.X.` default feel triplet8th 
(c) `:3 --X XXX X.X X.X` no need default feel 
(d) `--X:3 XXX:3 X.X. X.X.` default feel triplet8th 

(a) = (b)
(c) = (d)
but (b) and (d) can be straight or triplet8th applying feel
(a) and (c) are always triplet8th no matter the feel you apply to the rhythm, progression or song 

(c) with straight feel is mixing triplet8th with straight 8th notes, very usual in lead

### TODO

I stop here to know what do you think?
suggestions? simplification?

## AI:

Good — you caught a real conflation in my earlier framing. Let me untangle it, then I think most of the "complicated" dissolves.

## Define catalog — and you're right, feel isn't catalog

**Catalog** = the aggregated, browsable *index* of content entities (songs, progressions, rhythms, voicings) across all installed packs — the thing you scroll to *find and pick* content. **Catalog metadata** = `genre` / `subgenre` / `tags`: fields whose only job is **discovery/organization** (filter, group, search).

Default feel is **not** that. Its job is not "help me find this" — it's "how this should *sound* by default." So it's a different *kind* of field even though it rides in the same header block. Call it a **content property** (a rendering/interpretation default), not catalog metadata. My "same family as genre/tags" line was wrong; drop it. This also kills the earlier worry that it's a "mere suggestion" — it's a genuine content property.

## Does that reopen C4? No.

Read C4 precisely (domain ref, invariant list): *"TripletFeel never stored in a **pattern**."* The **pattern** = the realized tick grid. That stays feel-free. Default feel is a **separate sibling field on the entity** that *seeds the play-time `\tf` choice*. The grid still carries no feel; ScoreR still overrides. So C4-on-the-pattern holds exactly — we're adding a default for a play-time param, not baking feel into content data. Clean.

## Your rhythm (a)–(d): confirmed, and the scary part is free

I checked alphaTex ref line 111 — this validates your whole analysis:

> `\tf` only reshapes straight 8th/16th **pairs**; an explicit `:3` triplet beat is already a tuplet (no straight pair to warp), so `\tf` **leaves it alone**. Plain-8th beats swing while `:3` beats render as authored — no double-swing.

So exactly as you said:

1. **(a) `:3 X.X …` / (c) `:3 --X …`** — triplets written **literally**. Feel-immune. No default feel needed; applying straight vs swing does nothing to them.
2. **(b) `X.X.X.X…` / (d)** — written **straight**. Feel-sensitive: they *become* the swing when `\tf triplet8th` is applied.
3. **(c) with straight feel = triplets mixed with straight 8ths** — and this **composes for free**: `\tf` swings the straight pairs and leaves the literal `:3` alone in the same bar. We write **zero** special code for the mix — alphaTab handles it.

So there are two authoring philosophies for swing in a rhythm:

- **Literal** (`:3`): exact, WYSIWYG, feel-immune, verbose, locks the groove. Right for **lead lines / precise figures** where the swing is compositional (your "(c) with straight feel, very usual in lead").
- **Interpretive** (straight + feel): compact, flexible (same pattern straight or swung), swing is a render decision. Right for **comping** where "swing the 8ths" is a global style knob.

C4 is the app *betting on interpretive as the default*. Default feel just gives the interpretive path a good starting value.

## Simplification 1 (do this): drop the per-progression cascade

Your `\tf {song}` → `\tf {progression}` → restore `\tf {song}` sketch is the "little complicated" part — and it's **per-section feel**, which the idea already scoped **out**. A swung tune with a straight bridge is real, but it's a separate, bigger axis. Kill the nested-restore for now. Then the whole thing collapses to **one feel per exercise**, resolved once at generation into a **single `\tf` on bar 1** (exactly what the renderer already emits). No restores, no mid-song scope juggling.

## Simplification 2 (bigger — your call): who owns the default?

The literal-triplet insight lets us shrink ownership. My lean:

- **Default feel lives on the harmonic content — Song (and Progression when practiced standalone).** That's the "this is a swing tune" statement, and it seeds the one exercise-wide `\tf`.
- **Rhythms stay feel-free** and just encode any intrinsic triplet groove **literally** (`:3`). A rhythm that *needs* triplets writes them (feel-immune, exact); a straight rhythm inherits the exercise feel.

That removes the song-vs-rhythm precedence conflict entirely. The trade-off vs your "rhythms carry it too": a straight-authored comping pattern you personally want swung must either rely on the song's feel or be written literal — it can't self-declare "I default to swing" in isolation.

If you *do* want rhythms to self-declare (your (b)/(d) case), then we need a precedence chain — I'd propose **user-override > content(song/prog) default > rhythm default > None** — and rhythm default is only meaningful for straight-authored patterns.

## The one decision to make

**Does a Rhythm carry its own default feel, or does default feel live only on Song/Progression (rhythms encode triplets literally)?**

- **Only Song/Prog** → simplest, no precedence, matches "feel = one play-time global for the exercise." Straight rhythms that want swing rely on the exercise feel or go literal.
- **Rhythm too** → honors your instinct; costs a precedence rule and a "this field is meaningless for literal-`:3` rhythms" caveat.

Everything else (drop the cascade, C4 intact, mix composes for free, feel = content property not catalog) I think is settled. Which way on rhythm ownership?

## Rafa:

Only Song/Prog
Awesome! we converged to a very sweet point I think

if all grounded write design, req
if not tell me

## AI:

First — noted on replying/asking **in the chat doc**, including questions. My earlier terminal reply + popup were both wrong surfaces; I've saved that as a standing rule. (The API-shape question I asked in the popup, you answered "separate field, not CatalogMetadata" — that's D4/D5 below.)

Written both docs as **drafts** for your review:

- `design.md` — decisions D1–D8.
- `req.md` — `IN1–IN9` / `EX1–EX6` / `C1–C5`.

The spine:

1. **D1** feel is a *content property*, not catalog metadata — off `CatalogMetadata`, off the discovery record.
2. **D2** owners = **Song + Progression only** (your call). Rhythm/Voicing excluded (`EX1`/`EX2`).
3. **D4** the leading-header parser learns `feel:`, strips it, returns it **separately** — signature becomes `Parse → (CatalogMetadata, TripletFeel? DefaultFeel, Body)`.
4. **D5** `DefaultFeel` column on `SongEntity` + `ProgressionEntity` only (not the shared `ICatalogEntity`, so it never lands on Voicing) + one migration.
5. **D3** nullable — absent (no opinion → `None`) is distinct from explicit `feel: none`.
6. **D7** seed the ScoreR feel control at **selection**; the transport stays the override. Precedence: user > content default > None. `GenerateExerciseHandler` unchanged; the read/DTO path gains `DefaultFeel`.
7. **D8** rhythms stay feel-free; intrinsic triplets go literal `:3` (feel-immune; mixing composes for free).

Deferred: per-section feel (`EX3`).

Two things I want your read on before I go further:

- **A — the seam rename question.** `CatalogHeader` will now parse a *non*-catalog field. Fine functionally, but the class name gets slightly misleading. Leave the name (least churn) or rename it to something like `DefinitionHeader`? My lean: **leave it**, add a doc-comment noting it now surfaces feel separately. Rename is scope creep.
- **B — next action.** If the design/req read right: **lock `req.md`**, mark `idea`/`design` done, and I'll write the plan against these `IN`/`C` handles. Or tell me what to revise first.

Which way on A, and are the drafts good to lock?

## Rafa:

### Confirm

Please confim me this:
1. genre, subgenre and tags are catalog metadata for filtering, part of songs and progressions 
2. key and feel are not catalog metadata, they are content properties of songs and progressions 

### A

feel should be treated as key and key was already suppported, they are part of songs and progressions, default playback
why you said:
>`CatalogHeader` will now parse a *non*-catalog field

currently is parsing also `key` and it is a *non*-catalog field also

CatalogHeader should parse only genre, subgenre and tags
Song should parse key and feel by its own

Sorry i need to clarily my ideas

## AI:

You're right, and it makes the design cleaner. Confirmations first, then the correction, then one new question your grouping raises.

## Confirm

1. **Yes** — `genre` / `subgenre` / `tags` are **catalog (discovery) metadata**: their job is filter/search, they're denormalized into columns for `WHERE genre = …` queries, and they apply to songs, progressions **and voicings** (rhythm excluded).
2. **Yes** — `key` and `feel` are **content properties**, not catalog metadata. One nuance from the code: today `key` is a **Song-only** directive; a **bare Progression has no key** (it anchors at C / takes the play-time key). So "content property of songs and progressions" is the *target*, but for progressions it's new ground — see the question below.

## A — you're right, feel mirrors key (not CatalogHeader)

Grounded in the code:

- `key G` is parsed by **`SongParser`** (`key <token>` line) into **`Song.InitialKey`** on the pure domain record. It **never touches `CatalogHeader`**. My "CatalogHeader will parse a non-catalog field" was the wrong plan.
- So `feel` mirrors `key` exactly: `SongParser` parses a `feel` directive → **`Song.DefaultFeel`** (nullable `TripletFeel?`) on the domain record. **`CatalogHeader` stays untouched** (genre/subgenre/tags only).

This **simplifies** the whole design — three things drop out:

- **No `CatalogHeader` signature change** (old D4 — gone).
- **No new entity column, no migration** (old D5 — gone). Feel rides *inside the DSL string* exactly like `key` does; it's parsed on load into `Song.DefaultFeel`. No denormalization because feel is never a filter field.
- **D6 flips**: the pure domain record *does* carry feel — `Song.DefaultFeel` alongside `Song.InitialKey`. C4 still holds: C4 is about the realized **RhythmPattern/tick grid**, not the Song; the Song carrying a suggested default is identical to it carrying `InitialKey`.

**Syntax nuance:** in the Song grammar `:` already means a stored-part reference (`NAME: id`), so `feel: triplet8th` would misparse as "define part `feel` → reference `triplet8th`." The directive must be **`feel triplet8th`** (space keyword, like `key G`) — not the colon form the idea sketched.

## One question your grouping raises — Progressions

The Song grammar is line-oriented and already hosts `key`/`mod`, so adding `feel` there is trivial and symmetric. But the **Progression grammar is pure space-split bars** (`17 17 47 …`) — it has **no directive syntax at all**, and no `key`. Putting `feel` on a progression means giving the Progression grammar its **first directive** (a leading `feel` line + making the parser line-aware).

So:

- **(a) Add it to progressions now** — Progression grammar grows a `feel` directive (and this naturally opens the door to `key` on progressions later).
- **(b) Song-only for now** — a bare-progression drill inherits feel from the transport/exercise default; revisit progression-level feel if/when the Progression grammar gains directives. Consistent with progressions having no `key` today.

My lean is **(b)** — lighter, and it matches the current progression grammar (zero directives, no key). But you explicitly grouped progressions with songs, so your call. Which way — (a) or (b)? Then I'll revise `design.md` + `req.md` in one clean pass and we lock.

## Rafa:

(b) lets keep progressions pure and key, feel agnostic. Progressions should be just chords/bars. Agree? 
revise `design.md` + `req.md`

## AI:

Agreed — Progression stays a **pure harmonic primitive** (just bars/chords), key- and feel-agnostic. Key and feel are arrangement/play concerns that belong at the Song (and Exercise) layer, which matches the code already (a bare progression has no key; the Song supplies `InitialKey`; the Exercise supplies play params). Clean boundary.

Revised both docs to **Song-only**:

**`design.md`** — D1–D9 now:

- **D4** feel is a Song DSL directive `feel <token>` (space keyword like `key`), parsed by `SongParser`.
- **D5** no new persistence at all — no `CatalogHeader` change, no column, no migration; feel rides in the `Dsl` string like `key`.
- **D6** carried on the pure `Song` record (`DefaultFeel` beside `InitialKey`).
- **D8** progressions stay pure harmony (new, your call).
- Old CatalogHeader/column/migration plan moved to **Rejected alternatives**.

**`req.md`** — `IN1–IN8` / `EX1–EX7` / `C1–C6`. Notably `EX1` (progressions feel/key-agnostic), `EX5` (feel never touches CatalogHeader/CatalogMetadata/entity columns), `C1` (C4 preserved — feel mirrors `key`).

Two small things I decided while revising, flag if you disagree:

1. **`feel none` is allowed** and is distinct from omitting the directive (`IN7`) — an explicit "this is a straight tune" vs "no opinion." Both render straight, but the explicit one seeds the control to None deliberately.
2. **Song→DSL round-trip** (`C4`): I noted the emitter only *if* one exists — CRUD stores the authored DSL text verbatim, so textual round-trip is free; a structural Song→DSL serializer (if there is one) must emit `feel`. The plan will confirm which.

If these read right: say the word and I'll **lock `req.md`**, mark `idea` + `design` done, and write the plan against these `IN`/`C` handles.

## Rafa:

I already locked req.md, marked idea + design done
write plan

## AI:

`plan-001.md` written — 6 steps, and `loom_verify_req` is **clean** (no uncovered Included, no Excluded violations, no dangling citations).

| # | Step | Satisfies |
|---|------|-----------|
| 1 | `Song.DefaultFeel` + `feel <token>` directive in `SongParser` (peer of `key`) | IN1, IN2, IN3, IN7, C1, C2, C3 |
| 2 | Parser + round-trip tests | IN6, IN7, C4 |
| 3 | Expose `DefaultFeel` on the song read/DTO path | IN4 |
| 4 | Seed the ScoreR feel control on song selection (mirrors play-ui-key-init) | IN4, IN5, C5, C6 |
| 5 | Ref-doc sync (dsl + domain model) | IN8 |
| 6 | Verify end-to-end (control seeds, override works, one `\tf`; straight = byte-identical) | IN4, IN5, C5 |

Ordering: step 1 is the foundation; 2/3/5 build on it; 4 needs 3; 6 verifies after 4. No renderer changes (EX6) — feel just originates from the seeded control now.

**Step 1** touches `Song.cs` (+`DefaultFeel`, plumb `FromSections`/`OfProgression`) and `SongParser.cs` (parse `feel <token>`, reserved-keyword guard, unknown-ident throw).

STOP — waiting for `go` to implement step 1.

## AI:

Steps 1–5 done and green; step 6 is partly blocked by your running app. Status:

**✅ Step 1** — `Song.DefaultFeel` (nullable, peer of `InitialKey`) + `feel <token>` directive in `SongParser` (reserved keyword, unknown-ident throw, duplicate throw). Core builds.
**✅ Step 2** — parser + model tests (idents / unknown / absent-null / `feel none` / reserved-name / textual round-trip). Green.
**✅ Step 3** — `DefaultFeel` threaded `SongStore.List()` → `ContentSummary` → `ContentItem` (rides the `entityList` JSON like `InitialKey`; single parse now yields both seeds).
**✅ Step 4** — `seedFeelForHarmony()` in app.js + a non-rendering `view.seedTripletFeel()` in ScoreR, wired to the harmony `change` event next to the key seed.
**✅ Step 5** — both refs synced via `loom_patch_doc` (DSL: the `feel` Song directive; domain-model: `Song.DefaultFeel` + a C4-clarifying invariant).

**Step 6 — automated part green, GUI part blocked:**

- Full suite **793 pass** (792 + the new store-integration test).
- New integration test proves the whole server path: a `feel triplet8th` Song surfaces `DefaultFeel="Triplet8th"`, `feel none` → `"None"` (not null), no directive → `null` (IN7).
- `\tf` emission is unchanged pre-existing behaviour driven by `Exercise.TripletFeel` (EX6) — the feel value just originates from the seeded control now.
- **I did not drive the GUI interactively:** the app is running (PID 19272) on the *old* build and holds the DLL lock, so a full rebuild can't overwrite it. The JS seed is wired exactly parallel to the proven key-seed, but I haven't *observed* the control pre-selecting.

Nothing is committed. How do you want to close step 6:

1. **You test it** — I leave your instance alone; you rebuild/restart when ready and confirm the Feel control seeds on song select.
2. **I restart it** — I close PID 19272, rebuild, relaunch, and drive it (select a `feel triplet8th` song, confirm the control + `\tf`).

Your call. Once step 6 is confirmed I'll close the plan (and commit only if you ask).

## Rafa:

App issues:
Practice page -> song with `feel triplet8th` -> ScoreR auto-select feel triplet8th but it is not passed to alphaTex -> not renderer -> not feel playback
Content -> Song page -> song with `feel triplet8th` -> ScoreR always feel `Straight`

## AI:

Good catches — traced both. They're two different pages / two different render paths.

## Practice page (Bug 1) — the seed works, but nothing re-renders

The control seeds correctly (you saw Triplet8th). The gap: **selecting a song doesn't re-render the score** — the practice builder only renders when you click **Generate**. `selections()` reads `view.getTripletFeel()` at Generate time, so *after* you click Generate the `\tf` **is** emitted. This is the same as the key picker today: picking a song updates the Key control, but the score only changes on Generate.

So: does it render `\tf` **after you click Generate**? If yes, the real issue is just that the seed is silent (no live update). My `seedTripletFeel` deliberately does **not** re-render — because at the moment you switch songs, the last render is still the *old* harmony, so re-rendering then would apply the new feel to the old song.

The clean fix, if you want it live: **switching harmony auto-regenerates** (so the new song renders immediately in its own key **and** feel — one regenerate carries both seeds). That's a small builder-UX change and it fixes the key-picker's identical "applies only on Generate" lag too. **Question: do you want harmony-switch to auto-regenerate, or is "applies on Generate" fine (then this isn't a bug, just expected)?**

## Content → Song page (Bug 2) — a real bug, no seed at all

Confirmed defect. My step 4 only wired the **practice** picker (`app.js`); the **content preview** (`content-crud.js`) has its own feel control that is **never** seeded from the song's `DefaultFeel`, and its preview renders from that control (default `None`) — so it's always Straight. The preview *does* auto-re-render on change, so the fix is clean: when a song loads into the content editor, seed the preview feel from the item's `defaultFeel` (already carried on the catalog item from step 3) and re-preview.

I'll fix Bug 2 regardless. For Bug 1 I need your call on auto-regenerate vs Generate-applies before I touch it — that's a builder-UX decision, and it also governs the key picker. Which way?

## Rafa:

### ScoreR design

We need to tweak ScoreR behavior. ScoreR renders songs, progressions and rhythms. Its has controls for: tempo, feel and key (new, move it from page).
In songs that parameters can be set by default and initial render pick them
In progressions, rhythms that parameters are set to Chord Flow defaults: Key C, tempo 80, Feel: Straight.

When this 3 params are changed ScoreR render them inmediately
notes: 
- Key control should be included in ScoreR. 
- Tempo: should have also a `default tempo` in Song DSL

Practice other page controls need to click Generate button to render

agree, like this tweak?

weekly limit = 98% so reply before doing 

## Bug 1

Current behaviour:
ScoreR does not render feel when pick a song but it does when:
- change `feel` combo box value of ScoreR
- click generate button

## AI:

Yes — I agree, this is the cleaner model. No code this turn (noted the limit); here's my read.

## Why it's right

It draws the honest seam: **render/interpretation params vs definition params.**

- **Render params — owned by ScoreR, live-render on change, seeded per content:** **key, tempo, feel.** Song → its DSL defaults; progression/rhythm → ChordFlow defaults (C / 80 / Straight).
- **Definition params — page controls, need Generate:** harmony, comping, lead, difficulty, voicing.

That's exactly the split that was fighting us: key/tempo/feel are "how to voice this same piece" (a cheap re-render / transpose — no regenerate), while harmony/comping/etc. are "what the piece is." Putting all three render params in ScoreR with live re-render, seeded from the content, is durable and consistent.

**It subsumes both bugs by construction:** Bug 1 (feel not live) → the 3 params live-render on seed/change; Bug 2 (content preview always Straight) → same ScoreR seeds identically on the content page. One mechanism, both pages.

## Flags before we build

1. **`tempo` becomes a 3rd Song directive** — `tempo <bpm>` → `Song.DefaultTempo` (nullable), the exact peer of `key`/`feel`. Symmetric and easy, but it's **new scope** (a second directive beyond this thread's feel work).
2. **Moving Key into ScoreR touches more paths** than feel did: Generate must read key from ScoreR (not the page `$("key")`); the saved-exercise **load** path must seed ScoreR's key from the stored `KeyOverride`; and the live key change is a **transpose re-render** carried on the same re-render path as feel/tempo. Manageable, but it's the biggest piece.
3. **Precedence unchanged:** seed from the content default, a manual change overrides and survives until the next content switch (our C6). Same nullable "absent vs explicit" for tempo (no `tempo` → 80).

## What's already banked (don't redo)

The **feel domain** work is done + green and stays valid: `Song.DefaultFeel`, the `feel` directive, and the `DefaultFeel` on the read DTO (steps 1–3, 5). Only the **UI wiring** (my step 4 page-seed + step 6) gets superseded by the ScoreR redesign.

## Scope question for next session

This is bigger than "song-default-feel." I'd suggest: mark the feel-domain piece done here, and open a new thread — **"ScoreR owns render params (key/tempo/feel) — seeded + live"** — that carries the Key move, the `tempo` directive (+`Song.DefaultTempo`), the live-render wiring, and both bug fixes. Or fold it all into this thread renamed. Your call next time — nothing's committed, tests green.

## Rafa:

Ok. 
Plan step 6 is not done. Close it to done.
Open new thread, idea.

## AI:

Done:

- **`plan-001` closed** — step 6 marked done, done-doc written (`done/plan-001-done.md`), plan finalized. Steps 4/6 recorded as *superseded* by the new thread (the page-seed JS stays in the tree as a stepping stone).
- **New thread + idea** — `loom/ui/scorer-render-params/` → **idea.md**: "ScoreR owns render params (key/tempo/feel) — seeded + live." It carries the render-vs-definition seam, the Key move into ScoreR, the `tempo` directive (`Song.DefaultTempo`), live re-render, and both bugs (which it fixes by construction). Filed under `ui`; the `tempo` domain bit is flagged as an open question (split out or keep here).

Banked and green: `Song.DefaultFeel` + `feel` directive + read-DTO + refs (793 tests pass). Nothing committed. Next session: read the new idea and we design from there.