---
type: chat
id: ch_01KXV3G8DMN9YR18720PP7X0ZC
title: reframe-readme-review-docs Chat 001
status: done
created: 2026-07-18
version: 1
tags: []
parent_id: null
requires_load: []
last_ai_block: 12
---
# reframe-readme-review-docs Chat 001

## Rafa:

- README
- docs/user-guide.md

We need to update them with the last features implemented
I do not like actual format of main README
I would like app features, grouped and highlight the most different and interesting
I will generate new updated screenshoots and some PDFs from Chord Sheets export to be showed up

README, docs/ and loom/refs have very different purpose and they should be written different approach

User main entry points are: README and docs/user-guide.md 

- README: show all main features, cleanly, shortly enough. If README become to long we could create a new doc in docs/ and link it
- docs/user-guide.md: show how to use the app

proposed new docs:
- docs/dsl-guide.md show the user how to write rhytms, voicings, progressions and compose songs with clear examples

we already had created a bunch of major and minor progressions
could you add some good songs examples using some of them when possible
src/ChordFlow.Core/Content/default-pack/songs

- loom/refs/ are not for users, they are for us and possible collaborators, they are already well written I think

Suggestions?

when we finish this we will do a release v0.15.0

## AI:

Good split to make — the three surfaces really do have three audiences, and right now they're blurred in one concrete way: **both the README and the user guide currently send users to `loom/refs/chordflow-dsl-reference.md` for the DSL** — i.e. straight into the collaborator docs. Your proposed `docs/dsl-guide.md` is exactly the fix; we repoint those two links to it.

Here's how I'd frame each surface and what changes.

### 1. The four surfaces (purpose → audience)

| Surface | Purpose | Audience |
|---|---|---|
| `README.md` | Shop window — what it is, headline features, screenshots, download | GitHub visitor / prospective user |
| `docs/user-guide.md` | How to *use* the app (task walkthroughs) | Someone who downloaded it |
| `docs/dsl-guide.md` *(new)* | How to *author* content — rhythms, voicings, progressions, songs, with examples | User who wants to write their own |
| `loom/refs/*` | Internal system-of-record | Us + collaborators (leave as-is) |

### 2. README reframe

The current README is one flat ~30-item list under **"Features (v0.14.0)"** — that's the format that reads like a changelog dump. I'd restructure to:

- One-line what-it-is (keep).
- **Highlights** — 3–4 marquee items, the genuinely *different* things, each with a screenshot. My pick for "most different and interesting":
  1. **The Voicings Engine — a chord *reasoner*, not a viewer.** The actual north-star differentiator; it *derives* and *explains* grips instead of showing a frozen dictionary. Nothing comparable in typical chord apps.
  2. **Chord Sheets — print *and* play along.** Newest, most demo-able, and the PDF export you're generating is the perfect visual proof.
  3. **Everything authored in text DSLs + shipped as content packs.** The open-core hook.
- **Feature groups** — fold the 30 bullets into ~5 themed blocks: *Practice & playback · Chord Sheets · The Voicings Engine · Author your own content · Guitar shape viewers*. Each block is 2–4 tight lines, not one bullet per micro-feature.
- Drop the per-version `(v0.14.0)` heading — the grouped section is version-agnostic and the CHANGELOG carries per-release detail. Keep a short **"New this release"** line at the top instead.
- Move the deep **dev** matter (full Project layout, the test-count paragraph, build internals) to the bottom or a short dev doc, so the top stays user-facing. *(Optional — say the word and I keep a slim dev section inline; README also serves devs.)*

If it still runs long after grouping, the overflow candidate to spin into its own `docs/` page is the exhaustive feature catalogue, linked as "full feature list."

### 3. docs/dsl-guide.md

Four sections, each **example-first** (show the DSL, show what it renders): Progressions → Rhythms → Voicings → Songs (arranging the first three). I'd lift the canonical grammar from the loom/refs DSL ref but rewrite it in a *teaching* voice with runnable examples, not the reference's exhaustive-spec voice. It should also cover the newer things a user can actually type: `capo`, whole-song `tempo`/`feel`, and per-chord / `voice` voicing selection.

### 4. Song examples

Worth doing. Today the pack has only **3 songs** (`jazz_blues_f`, `blues_song_demo`, `explicit_voicings_demo`) — all blues — but **21 progressions**, including a rich minor set (`minor_ii_v_i`, `andalusian_cadence`, `aeolian_loop`, natural/harmonic-minor `i–iv–v`, `picardy_cadence`…) and jazz-major set (`ii_v_i`, `tadd_dameron_turnaround`, `tritone_sub_ii_v_i`, `circle_secondary_dominants`…). Those are begging to be shown inside real songs.

Before I write them, two choices are yours:
- **How many + what spread?** e.g. a tight set of ~5–6 that each *showcase a different progression family* (one jazz-major, one minor ii–V, one modal/aeolian, one andalusian, one turnaround étude), or a larger grab-bag.
- **Real-standard-style vs. original études?** We only ship chord changes + arrangement (no melody), so "in the style of" tunes are fine and more motivating — your call on naming.

### 5. Screenshots & PDFs

- New app screenshots keep the existing `images/screenshots/NN-name.png` convention — re-shoot the ones that changed and add a **Chord Sheets** and a **Voicings-grid** shot.
- Put the exported **Chord Sheet PDFs** in their own folder and link them from the README highlight + the guides — a printable PDF is the single best proof of that feature. Folder your call: `images/sheets/` or `docs/exports/`.

### 6. Sequencing → v0.15.0

This spans docs *and* new content (song DSLs are pack content, so they get a build/test run). I'd turn this chat into a small **req → plan** so the release scope is explicit:
1. Author the new songs (content) + verify they parse/render.
2. New `docs/dsl-guide.md`.
3. README reframe + repoint the DSL link.
4. Refresh `user-guide.md` for the new features (Chord Sheets, Voicings grid) + repoint its DSL link.
5. Screenshots/PDFs dropped in, links wired.
6. CHANGELOG + `do-release` v0.15.0.

**Open questions before I draft anything:**
- **A — Songs:** how many, standard-style vs. original étude, and which progression families to prioritise?
- **B — README dev matter:** keep a slim inline dev section, or move Project-layout / tests / build to a separate dev doc?
- **C — Export folder:** `images/sheets/` or `docs/exports/` for the PDFs?

## Rafa:

I like too much your schema to re-structure the docs, thanks!

- **A — Songs:** we need user feel that app have content to be enough appealing, we do not want that user need to write all himself from the beginning, we have to offer enough songs (in other future thread we will create sets of rythms also)
  - how many? do both sets
    - a tight set of ~5–6 that each *showcase a different progression family* (one jazz-major, one minor ii–V, one modal/aeolian, one andalusian, one turnaround étude), 
    - and a larger grab-bag, similar to well know songs
  - mostly standard-style and some original étude, which progression families? you pick
  
- **B — README dev matter:** keep a slim inline dev section mentioning the architecture, how c# music domain build a fundation for expandable music app possibilities and move the details, Project-layout / tests / build to a separate dev doc, add a link in main readme

- **C — Export folder:** `images/sheets/` 

if all is ground...
turn this chat into a small **req → plan** (nide idea)

## AI:

All three locked in:

- **A** — do both sets, mostly standard-style + a few études, families my pick.
- **B** — slim inline dev section in the README (architecture + "the C# music kernel is a foundation for an expandable music app"), with the heavy detail (project layout / tests / build) moved to a separate dev doc, linked.
- **C** — PDFs go in `images/sheets/`.

I read all 21 pack progressions so the songs are grounded in what actually exists. Here's the concrete content proposal — each song reuses existing progressions (inline sections only where a shape isn't in the pack), and each carries `key` / `tempo` / `feel` / `genre` metadata so the library also dogfoods those directives.

#### Set 1 — Showcase études (one progression family each, clearly pedagogical)

| Song | Uses | Genre / feel | Style |
|---|---|---|---|
| **Turnaround Workout** | `major_turnaround` + `ii_v_i` | Jazz, swing | étude |
| **Minor Cadence Study** | `minor_ii_v_i` + `minor_turnaround` | Jazz (minor), swing | étude |
| **Aeolian Vamp** | `aeolian_loop` | Rock, straight | étude |
| **Andalusian Descent** | `andalusian_cadence` | Flamenco, straight | étude |
| **Tritone Turnaround** | `tadd_dameron_turnaround` + `tritone_sub_ii_v_i` | Jazz (advanced), swing | étude |
| **Minor Blues** | `minor_12bar_blues` | Blues, shuffle | étude — complements the two existing major blues |

#### Set 2 — Grab-bag, "in the style of" well-known tunes (original names, no melodies shipped)

| Song | Uses | Genre / feel | Evokes |
|---|---|---|---|
| **Ragtime Circle** | `circle_secondary_dominants` | Jazz / ragtime, swing | "Sweet Georgia Brown" circle-of-5ths |
| **Falling Leaves** | `ii_v_i` + `minor_ii_v_i` | Jazz, swing | major ↔ relative-minor ii–V (Autumn-Leaves shape) |
| **Dameron Lane** | `tadd_dameron_turnaround` + `ii_v_i` | Jazz, swing | Tadd Dameron / "Lady Bird" turnaround |
| **Northern Lights** | `aeolian_cadence` + `aeolian_loop` | Rock / cinematic, straight | epic minor film-cue vamp |
| **Wistful** | `borrowed_iv` + `major_turnaround` | Pop ballad, straight | the borrowed-iv "wistful" lift |
| **Open Road** | `mixolydian_bvii` + `aeolian_cadence` | Classic rock, straight | bVII rock anthem |

That's **12 new songs** (library goes from 3 → 15). Happy to trim either set if 12 feels like too much for one release — say the word and I'll cut.

Names for Set 2 are deliberately original: we only ever ship **chord changes + arrangement, never a melody**, so this stays clear of any copyrighted tune while still teaching the recognisable shape.

---

### Proposed req scope (for your review before I lock it)

**Included**
1. The 12 new song content files (or your approved subset) under `default-pack/songs`, each with `key`/`tempo`/`feel`/`genre` metadata, reusing existing progressions where possible.
2. New `docs/dsl-guide.md` — user-facing, example-first: Progressions → Rhythms → Voicings → Songs (incl. `capo`, `tempo`/`feel`, per-chord/`voice` voicings).
3. README reframe — highlights-led + ~5 grouped feature blocks; slim inline dev section (kernel-as-foundation) linking out to the dev doc.
4. New dev doc (e.g. `docs/dev-notes.md`) — project layout, tests, build internals moved out of the README.
5. `user-guide.md` refresh — Chord Sheets + Voicings grid; repoint its DSL link.
6. Repoint the DSL links in README + user-guide from `loom/refs/…` → `docs/dsl-guide.md`.
7. New/updated screenshots + Chord-Sheet PDFs in `images/sheets/`, links wired.
8. CHANGELOG entry + `do-release` **v0.15.0**.

**Excluded**
- New **rhythm sets** — you've deferred these to a future thread.
- Any engine/domain/DSL-grammar code change — this release is content + docs only.
- Rewrites of `loom/refs/*` — they stay the collaborator spec, as-is.

**Constraints**
- New songs must parse + render (build/test gate before they're "done").
- Docs are teaching-voice and example-first; `loom/refs` remains the authoritative spec, not duplicated.
- README stays short; any overflow spins into a `docs/` page.

---

**Two things before I create anything:**
1. **Song list** — approve all 12, or trim? (This is the one creative call I made for you.)
2. **Flow** — you said "small req → plan", so I'll treat *this chat as the design of record* and go straight to `req` (draft → you review → finalize/lock) → `plan`, skipping separate idea/design docs. Good, or do you want the full idea + design docs too?

STOP — waiting for go.

## Rafa:

all good!
I think we could 
- write a design -> req 
stop if needed
- lock req -> plan 
stop if needed
- do plan

## AI:

On it — full flow: **design → req (stop) → lock → plan (stop) → do plan**, all 12 songs approved.

Writing the **design doc** now — it captures the four-surface doc model, the README information architecture, the dsl-guide ↔ loom/refs split, the dev-doc extraction, and the song-content approach with its trade-offs — then drafting **`req.md`** from it. I'll stop there so you can review both before we lock the req.

*(Skipping a separate idea doc — this chat is the brainstorm of record.)*

## AI:

Both docs are drafted and waiting for your review:

- **`design.md`** — the four-surface doc model, README IA, the dsl-guide ↔ loom/refs split (with the duplication trade-off spelled out), dev-doc extraction, the 12-song plan, and the copyright/naming policy.
- **`req.md`** (draft) — `IN1–IN9` Included · `EX1–EX4` Excluded · `C1–C4` Constraints.

Review both; tell me any edits (I'll amend the req) or say **lock** and I'll finalize `req.md` and draft the step plan.

STOP — waiting for go.

## Rafa:

I marked idea + design done and locked req
write plan

## AI:

**`plan-001.md`** is drafted — 7 steps, verified clean against the locked req (no uncovered Included, no Excluded violations, no dangling citations):

1. **Author the 6 showcase-étude songs** — *(IN1, C1, C4)*
2. **Author the 6 grab-bag songs** — *(IN1, C1, C4)*
3. **Write `docs/dsl-guide.md`** — *(IN2, C2)*
4. **Reframe README + extract `docs/dev-notes.md`** — *(IN3, IN4, IN5, IN7, C3)*
5. **Refresh `docs/user-guide.md`** — *(IN6, IN7, C2)*
6. **Wire in screenshots + Chord-Sheet PDFs** *(gated on your assets)* — *(IN8)*
7. **CHANGELOG + release v0.15.0** — *(IN9)*

Steps 1–2 each end on a `dotnet build` + `dotnet test` gate; songs are auto-discovered so there's no manifest edit. Step 6 waits on the screenshots/PDFs you're generating.

Review the plan; say **do step 1** (or `do steps 1-2`, `do plan`, etc.) and I'll start.

STOP — waiting for go.

## Rafa:

I started plan
do plan

## AI:

Ran `do plan` through steps 1–5. Stopping at step 6 (it needs your assets) and holding step 7 (the release) for your go.

**Done:**
- ✅ **1 — 6 showcase-étude songs** · ✅ **2 — 6 grab-bag songs** → the library is now **15 songs** (was 3), spanning jazz / blues / rock / flamenco / pop. All reuse existing pack progressions; two exercise cross-mode arrangement (Falling Leaves via `mod vi`, Northern Lights via a `key Cm`→`key C` reset). `SongSeedTests` validates every one — full suite **1058/1058 green**.
- ✅ **3 — `docs/dsl-guide.md`** — new example-first authoring guide (Progressions · Rhythms · Voicings · Songs), self-contained, doesn't send users into loom/refs.
- ✅ **4 — README reframe + `docs/dev-notes.md`** — highlights-led (Voicings Engine · Chord Sheets · music-as-text), 6 grouped feature blocks replacing the flat list, slim "How it's built" section; project layout / tests / build moved into dev-notes; README DSL link repointed → `docs/dsl-guide.md`.
- ✅ **5 — `docs/user-guide.md`** — new Chord Sheets + Voicings sections, fixed the stale "no song library" line, DSL link repointed → `docs/dsl-guide.md` (no loom/refs link left for users).

**Blocked / waiting on you:**
- ⏸ **6 — screenshots + PDFs.** When you've generated them: drop the refreshed `images/screenshots/NN-name.png` shots (I'd suggest adding `07-chord-sheets.png` + `08-voicings-grid.png`) and the exported Chord-Sheet PDFs into `images/sheets/`, then tell me and I'll wire them into the README highlights + the guides.
- ⏸ **7 — release v0.15.0.** I'll run `do-release` (CHANGELOG, version bump, build/test, tag, push, monitor) **only on your explicit go** — it's an outward-facing publish, so I won't auto-fire it.

Nothing is committed yet — the songs + docs are sitting in the working tree so you can review them (and so the screenshots land in the same release commit). Want me to **commit steps 1–5 now**, or hold everything until the assets are in and we ship it all together in step 7?

STOP — waiting for go (assets, or a decision on committing 1–5).

## Rafa:

I saved screenshoots:
images/screenshots/01-practice-score.png
images/screenshots/02-practice-chord-sheet.png
images/screenshots/03-content-progressions.png
images/screenshots/04-content-songs.png
images/screenshots/05-content-rhythms.png *
images/screenshots/06-content-voicings.png
images/screenshots/07-scales.png
images/screenshots/08-voicings.png
images/screenshots/09-voicings-engine.png
pdfs:
images/sheets/blues-song-demo.pdf
images/sheets/jazz-blues-in-f.pdf
images/sheets/raftime-circle.pdf
and a gif showing cisual metronome and score animated:
images/chord-flow-demo.gif

* old image

### Issue

content rhythms page is not showing score properly, I think it is a problem, regression after implementing the thread: `loom/ui/content-shared-render-surfaces/plans/plan-001.md`, maybe now is hidding ScoreR

we could end the documentation
commit
but we will need to fix this bug before release
if you find root cause and it is easy to fix
code quick
if not tell me

## AI:

All done bar the release. Two clean commits.

**Docs finished (step 6):** wired your assets in —
- The **demo GIF** leads the README and sits in the user guide's Chord Sheets section.
- Highlights + the screenshot gallery rebuilt onto your new filenames; the user guide got Chord Sheets + Voicings figures.
- **Sample PDFs** linked from the README highlight and the guide.
- Renamed `raftime-circle.pdf` → **`ragtime-circle.pdf`** (matches the Ragtime Circle song).
- Every image + PDF link verified to resolve. `05-content-rhythms.png` is referenced by its stable name — overwrite it in place after the fix and the docs pick it up.

**Bug — root-caused and fixed (`code quick`, committed separately):**
The content **rhythms** preview looked blank because the shared `ChordFlowRenderSurface` **score-only mode** never reset the page-owned score element. When you open a progression/song, switch to the **Sheet** view (which collapses `#ccPreviewScore` via `.view-collapsed`), then switch the entity to **Rhythm**, the composite recreates in score-only mode — which builds **no Score⇄Sheet toggle**, so nothing removed that leftover `.view-collapsed`, and the rhythm score rendered into a `max-height:0` box. One-line fix: score-only create now clears the class. `node --check` clean; you'll confirm it visually when you re-shoot the page.

**Two commits:**
- `fix(ui): show ScoreR in render-surface score-only mode` — the regression fix + its done record.
- *(next)* the docs + 12 songs + assets for this thread.

**Left — step 7, on your go:** `do-release` v0.15.0 (CHANGELOG, version bump, build/test, tag, push, monitor). I'm holding it until (a) you've re-shot `05-content-rhythms.png` on the fixed build, and (b) you say go — since it's the outward-facing publish. Just say **release** when ready.