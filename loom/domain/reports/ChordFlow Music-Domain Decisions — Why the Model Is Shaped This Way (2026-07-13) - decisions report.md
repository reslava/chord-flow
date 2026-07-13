---
type: report
id: rp_01KXEPMWR8ZY1DVRS06S9KGDX2
title: "ChordFlow Music-Domain Decisions — Why the Model Is Shaped This Way"
status: active
created: 2026-07-13
version: 1
tags: []
parent_id: null
requires_load: []
kind: decisions
generated_at: "2026-07-13T21:38:05.194Z"
---
# ChordFlow Music-Domain Decisions — Why the Model Is Shaped This Way

This report captures the **rationale** ("why") behind the design decisions taken across the `domain` weave's music-model threads — progressions, songs, rhythm, intervals, chord qualities, transforms, anacrusis, triplet feel, rhythm-notation, chromatic degrees, multi-bar, and song-default-feel. It records the reasoning, alternatives, and trade-offs that do **not** live in the code. Where a decision's rationale is not recorded in the docs, that is stated explicitly.

---

## 1. The founding split: timing vs. harmony as separate layers

**Decided:** Chord-change timing lives on the tick grid as a distinct *harmonic-rhythm* layer (`HarmonicBar` / `ChordSpan(RomanDegree, DurationTicks)`), never as a field on the harmonic atom `RomanDegree`. (Option B.)

**Alternatives weighed:** (A) A `BarPart {Whole, Half, Quarter}` enum welded onto `RomanDegree` — Rafa's original sketch, fastest to type.

**Why:**
- Putting `BarPart` on `RomanDegree` mixes timing into pure, key-independent harmony — the same mistake the codebase already avoids with Feel ("never stored on the pattern"). The grain of the codebase is timing-as-its-own-layer.
- A flat list with implicit bar boundaries is fragile: one wrong/missing part cascades so every later bar regroups wrong and you can't localize the malformed bar. An explicit `HarmonicBar` wrapper makes validation **local** (`sum(span.DurationTicks) == BarTicks`).
- The `{Whole, Half, Quarter}` enum can't express what the DSL implies (three chords in a bar) and flatly can't express syncopation — you'd re-model the moment either arrived.
- The 48-PPQ grid (`BarTicks = 192`) is divisible by 2, 3 **and** 4, so tick-durations reach further (3-chord bars, dotted placements, later off-beat starts) with zero schema change. `BarPart` survives only as DSL/UI sugar mapping to ticks.
- The renderer's slot→span lookup (which chord covers a slot's tick) is *the same primitive* a future syncopation feature needs — the strongest signal it's modeled at the right altitude.

Rafa: "For sure (B), this way we keep harmony and timing as separated layers, loved it."

*Source: ch_01KTNV0GKNXJ5V3Q0J5TF8AAT5, de_01KTP11T7JCSDK6PN2FEXDR5CW*

### 1a. Quarter-slot model, and the render-vs-model scope correction

**Decided:** A bar is conceptually 4 quarter slots of 48 ticks; a chord occupies contiguous slots (durations ∈ {48,96,144,192}), giving 1/2/3/4 chords per bar — all **v1-renderable**.

**Why:** The renderer's earlier "3-chord bars are render-deferred" caveat only ever applied to three *equal* 64-tick spans (needing tuplets). Rafa never wanted that; he wanted quarter-aligned subdivision, where every boundary lands on a beat line — nothing to defer. The initial over-optimism ("tick-durations handle 3-chord bars now") was corrected, then re-corrected once the quarter-slot intent was clarified.

*Source: ch_01KTNV0GKNXJ5V3Q0J5TF8AAT5, de_01KTP11T7JCSDK6PN2FEXDR5CW*

### 1b. DSL uneven-bar syntax: M1 (even-split + `:slots`) over M2 (4-slot step-sequencer)

**Decided:** Even split when chords divide the bar evenly, plus a `:slots` quarter-count suffix for uneven bars (M1).

**Why M1 over M2:** M2 (always four `_`-separated slots, repeated chord = held) matches "subdivide into 4 equal parts" most literally, but it would break the `17_67` = Half/Half shorthand from Rafa's original examples. M1 keeps the earlier examples valid (backward-compatible).

*Source: de_01KTP11T7JCSDK6PN2FEXDR5CW, ch_01KTNV0GKNXJ5V3Q0J5TF8AAT5*

### 1c. Persistence and origin marker

**Decided:** Store `{Name, Dsl}`; the DSL string *is* the v1 serialization, re-parsed on load. Progressions carry an `Origin {BuiltIn, UserDefined}`; GUID ids for user progressions, human slugs for built-ins (Q1). A guarded factory `Progression.FromBars` throws on malformed bars (Q2).

**Why:**
- Store-the-definition/regenerate-on-load matches the existing `ExerciseEntity` pattern; the DSL string is compact, human-editable, and round-trips. The schema is designed so a future `spans_json` column can supersede it without losing the v1 string form.
- Origin is a *domain* fact (built-in vs user-created); the paywall/tier **enforcement** is a Features/licensing concern that only *reads* Origin — deliberately out of scope.
- A guarded factory makes an invalid `Progression` unconstructable, preferred over a separate `Validate()` — fail-loud house style.

*Source: de_01KTP11T7JCSDK6PN2FEXDR5CW, ch_01KTNV0GKNXJ5V3Q0J5TF8AAT5*

---

## 2. Song = arrangement graph of references, not a container of bars

**Decided:** A `Song` is an ordered stream of *references* to progressions plus arrangement instructions (repeat, modulation), realized by a `SongExpander` that folds a running key left-to-right into a `RealizedSong`. Song sits **above** `Transposer`; nothing below it changes.

**Alternatives weighed:** `Song { IReadOnlyList<Progression> }` (a container of embedded progressions).

**Why:**
- A container of progressions immediately loses repetition, structure, modulation, intros/outros, and navigation. References keep progressions reusable and key-independent.
- The pipeline already is `Progression → Transposer.Realize(key) → RealizedBar[]`; a Song is literally "a sequence of `(progressionRef, key)` realizations" where key is produced by folding modulations. `SongExpander` slots in cleanly without touching the layers below.

*Source: ch_01KTVR6VE608663XSSKMDJXV38, de_01KTVTNZPYS36K23R5Z9MYDB54*

### 2a. Four locked forks (A, C, D, transforms-separate)

**A — modulation stays at the arrangement layer; `transpose` is a future transform.**
*Why:* Modulation (change the realization key going forward — stateful, affects every later section) and transpose-transform (rewrite a progression's degrees, local, key-independent) are architecturally distinct. Musicians almost always want modulation, which the Song fold gives for free; "shift every roman numeral up two degrees" is rarely wanted. `mod` can't be a transform because it depends on running-key state, which doesn't fit a pure `Progression → Progression` signature.

**C — modulation is relative + absolute.**
*Why:* Relative (`mod V`, `mod +2`) is the reusable, musical default. But with a pure fold, modulations **accumulate**, so "return to the home key for the last chorus" has no clean expression — hence absolute (`key G`) as the reset/escape hatch. Internally `Modulation` carries `(Semitones, ModeChange?)` so mode-flip forms (`vi` → minor) are modelable even if v1 ships only `+n`/roman.

**D — Song stays pure harmony+arrangement.**
*Why:* This is "the one I'd most want your decision on, because it determines what the first slice's domain types even are." Rhythm/voicing/tempo/feel attach at play time via a `SongExercise` (the analog of today's `Exercise`), keeping a Song reusable across rhythm settings. The alternative (rhythm per-section) is more expressive but couples arrangement to timing.

**Transforms — their own thread.**
*Why:* Getting Song realizing correctly first keeps the first slice small/shippable; transforms are a clean *additive* layer that slots into the reserved `@op` DSL slot later without reworking the timeline.

*Source: ch_01KTVR6VE608663XSSKMDJXV38, de_01KTVTNZPYS36K23R5Z9MYDB54*

### 2b. Minor Song decisions (settled in song-chat-002)

- **`Part` is a dictionary, not a list** — the stream references parts by reused name (`A x2 … A`); a dict makes the stream cheap references and "locals shadow stored names" a one-line lookup.
- **Locals shadow stored progressions** — bare names resolve local-first then store.
- **Referential integrity enforced at resolution, not schema** — a missing stored reference fails loud ("reference 'blues' not found"), never silently dropping a section; no DB-level FK because built-ins are seeded by slug and user songs may inline everything.
- **`InitialKey` defaults to C major** when `key` is omitted.
- **`RealizedSong`/`RealizedSection` live in `Domain/Song/`** — they hold no alphaTex, only pure keyed bar data, so they belong in the domain, not `Rendering/`.
- **`RealizedSection.Key` is an output of the fold, never an input** (decision E) — a section shouldn't own the key it starts in.

*Source: de_01KTVTNZPYS36K23R5Z9MYDB54, ch_01KTXPJ0ZQNQEKDCB5A9EATHNZ*

### 2c. Section-aware rendering: single renderer entry point, not a Features orchestrator

**Decided:** A single `Render(RealizedSong, rhythm, tempo, difficulty, feel)` entry point in the renderer owns the whole walk; the Features orchestrator only runs `SongExpander.Expand` (because it holds `IProgressionStore`) and never touches alphaTex.

**Alternatives weighed:** A Features-layer orchestrator iterates `RealizedSection`, calls per-section render, and string-joins fragments.

**Why the orchestrator option was rejected:**
- It would make the orchestrator alphaTex-aware (stripping all-but-first headers, injecting inline `\ks`, inserting markers) — violating the invariant that `AlphaTexRenderer` is the *only* alphaTex-aware component.
- Per-section calls reset `currentDuration = null` at every seam, forcing redundant `:N` re-emission and leaking renderer state out.
- Header duplication — each fragment would carry its own header.
- A Song is one score with one header and N sections at possibly different keys; `currentDuration` must flow across section seams. That walk belongs in the renderer.
- Confirmed from alphaTex docs that `\ks` is legal mid-score ("specifies the key signature for this and subsequent bars"), so a key-changing Song renders as a single score — no per-key splitting needed.

The per-bar loop is extracted into a shared private `RenderBars(...)` so `Render(Exercise)` stays byte-identical.

*Source: ch_01KTXPJ0ZQNQEKDCB5A9EATHNZ, de_01KTVTNZPYS36K23R5Z9MYDB54*

---

## 3. Rhythm DSL: the durable-design pivot

### 3a. Initial minimal decisions (rhythm-chat-001)

**Decided (initially):** `.` = sustain (a hit rings to the next onset), `-` = rest/mute, `X` = attack; single lane (no drum `K/S/H` notation); onset-only DSL (stroke/accent stay overlays); defer `Velocity`; no `SONG:` arrangement in the rhythm DSL.

**Why:**
- **`.` = sustain, not fixed-length staccato:** for a strum trainer a hit almost always rings until the next onset; the original "every X = one 16th" would make everything staccato. This required a *separate* rest glyph (`-`) so a muted strum is distinguishable from a ringing one.
- **Single lane:** `K/S/H` is drum-machine notation; ChordFlow renders one guitar strumming voice — there is no drum staff to render lanes onto, and a hit can't be simultaneously kick and snare.
- **Onset-only glyphs:** `X/./x/A/U/D` overloads one character with four orthogonal axes (onset, velocity, accent, stroke) and can't express "accented upstroke." The domain already separates these via `RhythmEvent` + `AccentPattern`/`StrokeOverlay`.
- **Drop `SONG:` arrangement:** it duplicated the harmonic Song thread's timeline — two "Song" concepts and two arrangement parsers. Arrangement belongs to the harmonic Song.
- **`Velocity` deferred:** `Accent` already covers "louder hit"; a speculative `Velocity=100` field is exactly the speculative surface the minimal-domain ethos avoids, and records make it trivial to add later.

*Source: ch_01KTVSYS2RQK5FQXT2R78F8B90, de_01KTVVTS9HG5X2C39TC1X1KP94*

### 3b. The pivot: "robust, ready to grow" over "minimal" — and the design rules

**Decided:** Adopt the durable shape now — `RhythmPattern` becomes **multi-bar** (`Bars: PatternBar[]`) from the start; design the full DSL grammar (`:n` subdivisions, per-beat mixed subdivision, `|` multi-bar, dotted-via-sustain); and put **triplet rendering in-scope**.

**Why (the reasoning that reconciles minimal with durable):**
- The rhythm model is **positional** (`RhythmEvent` stores absolute ticks, not grid cells), so triplets, mixed subdivisions, and dotted values need *no* event-level change — the 48-PPQ grid makes `÷3` (16t) and `÷4` (12t) both integer.
- The **only** structural corner was single-bar `RhythmPattern`. Shipping single-bar and "breaking it later" directly violated Rafa's design rules #1 (durable over breaking) and #2 (no legacy churn). Fixing it now is cheap because only the progression thread depends on it.
- The principle that reconciles the two: **"Design for all of it; implement in additive slices; never require a breaking change to adopt a deferred part."** "Minimal v1" means minimal *implemented surface*, not a cornered design.
- Triplets are common in lead guitar and the domain already supports them; `{tu 3}` makes the renderer change small — so triplet rendering earns "support the commonly-used, done durably."

This crystallized Rafa's four **design rules** (saved as a durable feedback memory): (1) correct/robust/clean/durable/expandable design is max priority, prefer it over breaking changes; (2) clean over legacy; (3) always choose the correct durable way, not the faster; (4) for no-users apps, only architecture matters.

*Source: ch_01KTVSYS2RQK5FQXT2R78F8B90, de_01KTVVTS9HG5X2C39TC1X1KP94*

### 3c. Grammar model B (space separates subdivision-runs) and sustain-literal seeds

**Decided:** Model B — a space-delimited token is a maximal same-`n` run whose cells split into beats by count, so `X...X...X...X...` (one run) and `X... X... X... X...` (four runs) are equivalent. Seeds re-expressed sustain-literally.

**Why B over A (space-mandatory, one beat per token):** Every example in the idea/design wrote the contiguous form; Model A would make `X...X...X...X...` invalid. B matches all examples and is a ~3-line relaxation (`== n` → `% n == 0`). The feared `XXX:3X...` ambiguity is resolved the same way in B — require a space before a subdivision change, not forbid inner spaces.

**Why sustain-literal seeds:** The `.` = sustain decision implies guitar rings (not staccato). The seed DSLs (`X...............` etc.) are the ringing reinterpretation; using them as-is is exactly the correction the sustain rule implies. Beat1 rings the whole bar, Beat1And3 = two halves, Quarters round-trips identically.

*Source: ch_01KTXSJBKZYTW64A9E11J2X9JV, de_01KTVVTS9HG5X2C39TC1X1KP94*

### 3d. Quantizer coalescing (Option A) for the tie problem

**Decided (rhythm slice 2):** The quantizer coalesces a beat-aligned straight note into a single note value (`LargestAlignedFit`) — whole note across the bar, half note on beat 1/3 — instead of tied quarters.

**Alternatives weighed:** (B) implement tie rendering; (C) revert the sustain seed migration.

**Why Option A:** Migrating the seeds to ringing produced multi-beat notes, but the quantizer split every note at beat lines into *tied* continuations and the renderer throws on ties (C4, tie token unverified). Option A gives correct notation (a clean whole/half note, not four tied quarters), keeps ties/dotted deferred, and no built-in seed hits a genuinely syncopated ring that would still tie-split. B pulled a slice-1 deferral into scope and rendered visually worse; C abandoned the locked sustain decision.

*Source: ch_01KTY8G498TJ4ZNNVEY4420R8K, de_01KTVVTS9HG5X2C39TC1X1KP94*

### 3e. Deferral tracking: append-only requirements (the "5 gaps" lesson)

**Decided:** Fix deferral-drift by making `loom_refine_req` **append-only / immutable** — new slices append fresh handles, never renumber/reuse/drop a cited handle; superseded requirements get a status marker, not removal.

**Alternatives weighed:** (#1) `verify_req` ignores done/closed plans; (#3) a slice prefix (`S2-IN1`).

**Why #2 alone:** Refining a req in place re-used `IN1–IN6` for different things and dropped `IN7/IN8/C6`, so the completed slice-1 plan showed 5 phantom gaps and silent mismatches. #1 was rejected because skipping done plans would suppress the tool's core value — a genuinely deferred IN would silently disappear (Rafa's objection). #3 is just readability sugar. Append-only keeps deferrals visible and citations resolving with no format change. The existing rhythm thread was left as a "pre-fix scar" (history) rather than retroactively cleaned.

*Source: ch_01KTY8G498TJ4ZNNVEY4420R8K*

---

## 4. Intervals — the theory substrate

### 4a. Role-keyed spelling, owning both label spaces

**Decided:** `IntervalSpeller` (pure static, peer of `NoteSpeller`) with `Name(semitone)` (plain octave-degree vocabulary) and `Label(semitone, role)` (chord-context, role-keyed). It owns **both** label spaces.

**Alternatives weighed:** A flat `degree → semitone + spelling` dictionary.

**Why:** A flat table can't produce the overrides because spelling collides on pitch: semitone 3 is `b3` or `#9`, 8 is `#5` or `b6/b13`, 9 is `bb7` or `6/13`. The live `VoicingDiagram.IntervalLabel(semitone, role)` already resolves this by taking the chord-tone function as a second argument — so **spelling is a function of (semitone, role)**, not a flat map. The immediate oracle is reproducing `VoicingDiagram`'s labels byte-for-byte (it's the de-facto spec).

*Source: ch_01KVBNHXQWD1RM6TT3M6NGRR93, de_01KVF92S3CBV2XT4B8N9SF3038*

### 4b. `Name` computed and unfolded; diagram tensions stay conventional

**Decided:** `Name` is computed by formula (`number = base(sem%12) + 7·(sem/12)`, flats-only, octave-extensible), not a hand-written table. Diagram tensions stay conventional (`#9/#11/b13`), while the substrate `Name` stays flats-regular (`b10/b12`).

**Why:**
- Rafa's insight — a tension is just a chord tone an octave up — makes the extension series fall out of *not* folding mod-12. But the two-octave flats array (`b10`, `b12`) contradicts jazz convention (`#9`, `#11`) that `VoicingDiagram` already emits and pins with tests. So they genuinely differ: `Name` is indexed by absolute semitone (octaves real), `Label` by `(pc mod-12, role)` (octaves folded by function — a tension reads `9` regardless of register). Different questions, by design — that's *why* the layer keeps two methods.
- A computed `Name` is strictly better than a literal array: one 12-entry base table yields every octave for free and avoids transcription slips (Rafa's hand-written list had `b13` twice and a non-standard `#13`).
- **Ship `Name` now (A) even without a consumer:** the octave vocabulary is the thread's headline deliverable — tiny, pure, self-tested (its own golden oracle) — matching the durable-over-minimal stance.

*Source: ch_01KVBNHXQWD1RM6TT3M6NGRR93, de_01KVF92S3CBV2XT4B8N9SF3038*

---

## 5. Chord qualities — formula authoritative, semitones derived

**Decided:** `QualityFormulas` stores each quality's interval formula as an authored degree+accidental **string** (`"1 b3 b5 bb7"`), parsed via `IntervalSpeller.ParseSet` (Option A). `QualityIntervals` becomes a *derived* projection. Semitones are the **unit-test oracle**, never stored runtime data.

**Alternatives weighed:** (B) a structured `IntervalFormula` type of `(degree, accidental)` pairs; storing a Quality/Formula/Semitones three-column table at runtime.

**Why Option A:**
- The intervals thread already shipped `IntervalSpeller` — a degree+accidental authority. Option A reuses it (max DRY, zero new value type; the authored form *is* the spelling), cutting toward reuse per the durable-over-minimal philosophy.
- Option B's win — deriving chord-tone *function* from the degree number — isn't needed for `take`/this slice; the `ChordTones` band classifier stays. B partially duplicates what `IntervalSpeller.Parse` already decodes.

**Why not store semitones alongside the formula:**
- It caches a derived value, creating **two sources of truth** free to drift (fix a formula typo but not the semitone column). The derivation makes drift *impossible*; the codebase rule is "everything derived, never hand-authored per case."
- There's nothing to optimize — a 9-row static table parsed once at startup.
- Rafa's instinct (an independent cross-check) is fully captured by moving the semitone column into `QualityFormulasTests` as the golden oracle — human-authored expected values that break the test if a formula or `IntervalSpeller` changes, without a second runtime source. The full 3-column table still belongs in the design doc/ref as documentation.

*Source: ch_01KVJX7BV4HHDCXYQY6Q616GHA, de_01KVJYPHF2G4CHGNEN9ZMFPGAY*

---

## 6. Progression transforms — base + `take` (slice 1)

### 6a. Scope: base + one proof transform, and NOT `repeat`

**Decided:** Build the base (`IProgressionTransform` + composition + the `@op` DSL hook) plus one proof transform, **`take`** — not `repeat`.

**Why:**
- `repeat` is the *least* valuable transform — `@repeat(n)` (bar-expansion inside a progression) mostly duplicates the already-shipped Song `A x2` (section repeat), and the idea's own taxonomy flags "repeat (section)" as arrangement, not a transform.
- Transforms are **not a prerequisite for dogfooding** — real tunes are already authorable via inline/stored references + `x<n>` + modulation + `KeyOverride`. A transform layer is "more engine substrate," cutting against the stated next direction (real content + derived voicings). Dogfooding should surface which transforms matter rather than pre-building speculatively.
- `take`/`skip` (drill bars 1–4 of a standard) are the genuinely useful first ones; `take` was chosen as the proof transform.

*Source: ch_01KVQ0EWQFKBSSNKCTRN951QWV, de_01KVQ23HY2X7VM6JY2S51F0NCH*

### 6b. The four transform decisions

- **D1 — defer the key-aware interface.** `take` needs no key; define only `Apply(Progression)` now (YAGNI). The `SongExpander` dispatch point is already key-aware, so a sibling `IKeyAwareProgressionTransform` can be added when the first key-aware transform actually lands.
- **D2 — attach transforms to `PartPlay`, not `Part`.** Transforms are an *application-site* choice — the same defined part may be played plain in one spot and `@take(8)` in another, mirroring how `x<n>` lives on the play. Attaching to the `Part` definition would bind a transform to every play and break the "same part, different drill" use.
- **D3 — lexical token order, fixed semantics.** `A @take(8) x2` ≡ `A x2 @take(8)` (transform the progression, then repeat the section); allow either order rather than legislate one.
- **D4 — out-of-range `take` fails loud.** `take(0)`/negative/`> bar count` throws, consistent with the house fail-loud style (`Progression.FromBars`); clamping was rejected because asking for 8 bars of a 4-bar progression is an authoring error worth surfacing.

The `@op` name→transform mapping lives in a `ProgressionTransform.Parse` factory (the transform catalog's home), so `SongParser` recognizes the `@name(args)` shape without hard-coding `take`.

*Source: de_01KVQ23HY2X7VM6JY2S51F0NCH, ch_01KVQ0EWQFKBSSNKCTRN951QWV*

### 6c. `BeginnerShellStrategy` gains Major7 — the dogfood-surfaced gap

**Decided:** Add a `Major7` arm to `BeginnerShellStrategy` (root + maj3 + maj7 shell) as a **quick fix**, not a proper voicing thread.

**Why:** A ii-V-Imaj7 dogfood tune revealed the MVP shell voiced Dominant7/Minor7 only and threw on Major7 — a pre-existing voicing gap, not a transforms bug. Option 1 (a one-method addition) unblocks rendering real maj7 tunes immediately; the bigger authored/derived voicing engine is its own arc. The Major triad and richer qualities still throw — that stays the voicing engine's job.

*Source: ch_01KVQ0EWQFKBSSNKCTRN951QWV*

---

## 7. Anacrusis — render `PickupMeasure` as a true `\ac` pickup

**Decided:** Emit `\ac` before the pickup bar of both tracks (comping + lead), by prepending `"\ac "` at the two pickup call-sites, keeping `RenderBar`/`RenderLeadBar` as pure formatters. It gets its **own thread**, homed in `domain/anacrusis`.

**Alternatives weighed:** Adding a `bool anacrusis` parameter to the renderers; folding the work into `multi-bar` or Pickup-into-section.

**Why:**
- A `bool` param adds surface for a constant prefix the other bars never use; the "this bar is the anacrusis" fact already lives in the two pickup branches, so the prefix lives there too.
- It belongs to neither `multi-bar` (which owns alignment/fills) nor Pickup-into-section (the *interaction* of a pickup with alignment) — it's a self-contained rendering-correctness slice, logically upstream of both.
- Confirmed facts made it small: `\ac` is bar metadata at bar start, and a pickup's length follows its actual beats (not padded to a full bar), so the existing short-bar emission is already the right shape — only the marker is added.

**Accepted limitation (C6):** alphaTab numbers the anacrusis as bar 1 (first full bar shows as bar 2). `\ac` makes the short bar legal but doesn't suppress the number, and alphaTex exposes no renumber directive (only visibility). A real but accepted gap, recorded rather than worked around.

*Source: ch_01KVQ3X698E9RDYZ5FBNQD1QJR, de_01KVQ4HM9ZV5SSRR86P1TGZ9H2*

### 7a. Weaves are workstreams, not namespaces

**Decided:** New music-model threads (including `anacrusis`) stay under the `domain` weave rather than spawning a new `music` weave to match the code's `ChordFlow.Music.*` namespaces.

**Why:** The one arrangement to avoid is fragmenting a single workstream across two weaves with no principle for which goes where. Since Rafa wouldn't rename the existing `domain` weave, consistency forces all music-model work under `domain`. Weaves group *work* (like `ui/`, `guitar/`, `release/`) — they were never 1:1 with code namespaces, so the name not matching `Music.*` is a non-issue. A gradual drift (new work in `music`, old in `domain`) is strictly worse than either consistent option.

*Source: ch_01KVQ0QFCDAA5NN379SHHGX7EX*

### 7b. Defer `multi-bar`

**Decided:** Keep `multi-bar` deferred (priority lowered to 1000); freeze its idea as a captured spec.

**Why:** After carving out anacrusis, its remaining items (section-anchored fills, divisibility validation, first-class fills, per-section phase) are *refinements of a layer that already works* (cyclic tiling renders a bar-4 fill fine over any clean multiple), not bug fixes like anacrusis was. They're "more engine substrate," cut against the dogfood direction, and are speculative until real multi-bar content reveals the right rule (truncate vs require-divisible vs stretch). Pickup-into-section also `depends_on` anacrusis. When re-picked up (chat 002), the idea was found *more* grounded — its gate (rhythm slice landed) is satisfied and anacrusis shipped — but grounded ≠ due; it stayed deferred.

*Source: ch_01KVQ0QFCDAA5NN379SHHGX7EX, ch_01KX0TANYXS75WWAHJCZBNHMDE*

---

## 8. Triplet feel — delegate whole-song swing to alphaTex `\tf`

### 8a. Delegate to `\tf`; retire the tick-warp from the alphaTex path

**Decided:** Emit a native `\tf` directive; stop calling `FeelTransform` in the alphaTex path (but keep the class for a future export seam).

**Alternatives weighed:** (a) keep self-computed tick-warp; coexistence of both.

**Why:**
- The warp swings *playback only* — the notation stays straight, so the score reads wrong; and it only swings the off-beat 8th, so a quarter-note comping showed no audible difference for any Feel (why the combo "felt dead"). Delegating makes the score *read* swung and lets authors write plain 8ths.
- Keeping both would double-swing, so it's a replacement, not coexistence.
- `FeelTransform` is kept (unused by alphaTex) because a future MIDI/GuitarPro exporter has no alphaTab to swing playback and would need to bake the groove into ticks itself — a real future consumer, and keeping a pure, tested class costs nothing.

*Source: ch_01KVR2BPBMHM8SF34070D2SASD, de_01KVR89QNHC6NE2XHTJ6EM9MDQ*

### 8b. `Feel` → `TripletFeel` (alphaTab vocabulary); play-time only; C4 intact

**Decided:** Replace `Feel {Straight, Swing, Shuffle, Triplet}` with `TripletFeel` mirroring alphaTab's enum (`none/triplet8th/triplet16th` wired now; `dotted8th`/Scottish reserved). Feel is a single whole-song play-time choice — no per-section, no new grammar.

**Why:**
- alphaTab vocabulary (option 1) is clearer and one-to-one — no lossy "does Swing mean triplet8th or dotted8th?" mapping; the old names implied distinctions the tick-warp didn't honor.
- Whole-song play-time only **sidesteps** C4 rather than bending it: if feel is a render param (like tempo/difficulty) never written into a Progression/Song/Rhythm, C4 ("feel never baked into content") stays literally true. Per-section feel is the *only* thing that would force feel into content/grammar, so dropping it (rare, complex-rhythm-only) keeps the model clean.
- `\tf` and `{tu 3}` coexist for free: `\tf` reshapes only straight 8th/16th *pairs*, leaving explicit `:3` tuplets alone — so mixing straight and triplet in one bar (common in lead) needs zero special code, and `3: X.X` becomes redundant under `\tf triplet8th` while explicit `:3` is still needed for `XXX`/`.XX`/`XX.` and for `X.X` under straight feel.

*Source: ch_01KVR2BPBMHM8SF34070D2SASD, de_01KVR89QNHC6NE2XHTJ6EM9MDQ*

### 8c. Control moves into the score component; persisted; re-render not regenerate

**Decided:** Move the feel control out of the page into `ChordFlowScore` ("tempo's twin"); changing it triggers a cheap re-render (re-emit `\tf`, harmony unchanged) via the existing `onNeedsRerender` seam; feel stays a persisted Exercise param.

**Why:**
- Once feel is a pure render directive it's the same *kind* of knob as tempo (already in the component). The dividing line: component = render/playback knobs (tempo, render options, feel); page = content-selection knobs (harmony/key/difficulty, which change the notes).
- Feel changes the alphaTex string (unlike tempo's local `playbackSpeed`), so it's content-kind → a re-render, reusing the chord-diagram-toggle replay path.
- **Persisted (recommended, confirmed):** a saved swung blues should remember it's swung, exactly as it remembers tempo. The control's *location* moving doesn't change whether the *value* is saved; the drop-column alternative was rejected as a real behavior change breaking parity with tempo/difficulty.
- Default feel as *content metadata on a Song* was deliberately deferred (it collides with C4 and needs a catalog-metadata carve-out) to a follow-up thread — bolting it on would balloon scope and muddy a clean C4.

*Source: de_01KVR89QNHC6NE2XHTJ6EM9MDQ, ch_01KVR2BPBMHM8SF34070D2SASD*

---

## 9. Rhythm DSL — accurate-notation redesign

### 9a. Root-cause reframe: notation vs. sustain are separate concerns

**Decided:** The grammar describes **notated durations only**; sustain ("let ring") becomes a deferred *playback* overlay. `.` may no longer extend a rest; a note lasts exactly its drawn cells; ties come only from an explicit `_`.

**Alternatives weighed:** (A) keep ring-semantics + build dots/ties; (B) keep ring, author accuracy with `-`, defer dots/ties; (C) change the default model (X = one-cell stab, explicit extend, no auto-ring).

**Why the auto-ring model was the real culprit:**
- The Charleston "tie" problem had a different cause than assumed — the Charleston uses no ties/dots at all (`:2 X.-X-...`), so the thread's origin premise dissolved. The actual defect was that `.` conflated *notated duration* with *sustain*: any note ringing across a syncopation was forced into a tie/dot the renderer threw on.
- Guitar Pro / standard notation **separate** notated duration from sustain — a note is written as its value; "let ring" is a playback overlay that doesn't change the written duration. That's the correct model, and it makes the score always show the true value.
- Rafa's space/`_` proposal was simplified: dropping auto-ring means `X` and `-` already delimit every boundary, so **space stays insignificant** (readability only) and `_` carries ties. This is smaller than making space load-bearing.

**Why not (C):** it trades one inaccuracy for the biggest rewrite (reinterpreting every existing seed) and is arguably *less* guitar-true.

The renderer still must emit dots (`{d}`) and ties (`-.string`) — what changes is that the grammar decides *when* they appear (explicitly, accurately) instead of auto-ring producing them by surprise; the quantizer actually gets simpler (no coalescing heuristics).

*Source: ch_01KVVY9WZ0FN3016WRT5YQM7HQ, de_01KVW7YEZ70AXE0NNAE82DJWTX*

### 9b. Glyph rules and the note-group rule

**Decided:** `.` requires a sounding note (error at bar start / after `-` / after `_`); `-` = one cell of silence; each note-group must equal exactly one representable value (base or single-dotted) or be an error naming the group ("ambiguous duration — tie it with `_`").

**Why:** `.` implies sound, `-` implies silence — no overlap (Rafa's reasoning), so `X...----........` is forbidden (the `........` follows a rest → nothing sounding) and must be `X...------------`. The grammar self-enforces the distinction. Forcing one representable value per group removes engine guessing and makes the score 1:1 with the DSL. Rests, by contrast, decompose automatically (largest-fit, aligned) because rest grouping is a rendering detail with no author burden.

*Source: ch_01KVVY9WZ0FN3016WRT5YQM7HQ, de_01KVW7YEZ70AXE0NNAE82DJWTX*

### 9c. The `_`-as-tied-note redesign, and "rhythm wins over harmony"

**Decided (revised during implementation):** `_` is a **tied note** — it behaves like `X` (starts a note, occupies cells, extends with `.`) but ties to the previous note instead of re-attacking; a leading `_` ties across the barline. When a tie spans a chord change, **rhythm wins**: the tied note re-states the *previous* voicing's strings (holds the old chord).

**Alternatives weighed:** zero-width `_` that must be followed by `X` (the first design); for chord-clash: (defer clashes) / (prioritize rhythm) / (prioritize harmony).

**Why the `_`-as-note reframe:** it's cleaner — it deletes the awkward zero-width + "must be followed by X" + dangling-trailing-tie rules, makes `.`-after-`_` legal, and simplifies cell-count math (a `_` is a real cell).

**Why rhythm wins (option b):**
- A tie over a chord change is the *same* problem within-bar and cross-bar, so it needed one rule.
- `_` literally means "hold the previous note" — honoring that is the literal meaning of a tie; if the author wanted the new chord they'd write `X`.
- It's uniform (deletes the IN4 "reject tie across a chord change" special case) and trivial in alphaTex/guitar (tie the same strings the last note used).
- The cost is musically honest: a tie held into a new chord sounds the old chord late — which is what a tie *is*.

**Why cross-bar ties were initially deferred then adopted:** the first pass rejected bar-final `_` because tying into a new chord conflicted with IN4; the "rhythm wins" decision removed that conflict, so within-bar + cross-bar landed in one pass.

Related fix (surfaced by dogfooding): rests now coalesce to the largest metrically-aligned value (`:2 r` not `:4 r r`), a defect found by ear in the app that unit tests didn't catch.

*Source: ch_01KVVY9WZ0FN3016WRT5YQM7HQ, de_01KVW7YEZ70AXE0NNAE82DJWTX*

### 9d. Coverage judged sufficient; alphaTab tokens resolved

**Decided:** The grammar covers all important notation (bases, single-dotted directly, everything else via `_` tie chains, all rests, triplets via `:n`); accents/strokes/swing stay play-time overlays, quintuplets/32nds/grace notes/pitch stay out. Dot = `{d}`/`{dd}`, tie = `-.{string}` (over the `{t}` alternative), let-ring = `{lr}` (deferred).

**Why:** Ties are a *universal escape hatch* — any duration standard notation can express is a tie chain, so the model is complete for the trainer without further primitives. The `-.string` tie form was chosen over `{t}` because ChordFlow always voices on known strings (terser, no fret re-derivation), and the "non-stringed tricky" caveat in the docs doesn't bite.

*Source: ch_01KVVY9WZ0FN3016WRT5YQM7HQ, de_01KVW7YEZ70AXE0NNAE82DJWTX*

---

## 10. Chromatic (#/b) chord degrees

### 10a. The written degree — not the key — spells the root (letter-pure)

**Decided:** `#`/`b` on a degree carries **spelling intent**, spelled letter-pure from the parent degree's letter (`#4` = the 4th's letter raised), with no enharmonic collapse (`b4` is F♭, never E; `#7` is B♯, never C). Carried as an optional `NoteName` on the realized `Chord`.

**Alternatives weighed:** (A) pitch-class only (accidental applies ±1, spelling stays key-derived); (B) accidental carries a sharp/flat-side hint; naive "spell degree n then paste the glyph."

**Why letter-pure (Rafa's model) over A and B:**
- **A is correct only by luck of the key** — in C, `#4dim7` (F+1 = pc 6) would spell G♭ from the flat table, but you typed `#IV` and want F♯. The DSL would silently not mean what it says.
- **Naive glyph-pasting breaks on accidental diatonic degrees** — in F, degree 4 is B♭, so `#4` must *combine* (B♭ raised → B natural, the real `#IVdim7` root), not produce "Bb#". The combine step is the whole mechanism; it only looked simple because Rafa's examples were in C.
- **Letter-pure is what a composer expects** — preserving the letter is the natural behavior; if they wanted E instead of F♭ they'd write a different degree. Same reasoning rejects collapsing rare `Fb`/`B#` to enharmonics — that would throw away the information the accidental carries.
- It makes spelling **deterministic from the token**, independent of the key's convention, and the spelling-hint seam is reusable for future tritone-sub/secondary-dominant transforms. A (option) would have to be torn out and redone then.

*Source: ch_01KVXVM33HYT2F895RZ15ARW91, de_01KVXYQ0HR654X95B5HCVJC64K*

### 10b. Where spelling surfaces; accepted staff mismatch; no rewrite

**Decided:** Spelling surfaces in exactly one place — `ChordSymbol.Format` → the `{ch}` label / `\chord` diagram name / fretboard schedule — via override-and-fallback (accidental'd degrees use the `NoteName`; diatonic stays on the key table). PitchClass stays spelling-free (C4). Accept that the standard-staff *notehead* may show the opposite enharmonic.

**Why:**
- Tab shows `fret.string` (no letters), and standard-staff noteheads are spelled by **alphaTab itself** from fret+string+tuning — ChordFlow never passes it "F♯ vs G♭." So the feature only needs to make the chord *symbol/name* correct (which it fully controls in both views); building staff-spelling infrastructure was correctly scoped out as an overestimate.
- The symbol-vs-notehead mismatch is cosmetic, standard-mode only, with no name/tab impact — controlling it means per-note accidental injection, deferred to a later notation pass.
- Unifying all chord-symbol spelling onto `NoteName` was rejected as tempting-but-pointless: the two paths already agree on diatonic chords (zero output change), `NoteSpeller`'s key table stays anyway (for `\ks` and the title's key name), so it's churn with byte-identical risk for no benefit.
- Input is single-accidental only; double accidentals can only *arise on output* (rare), never be typed.

### 10c. dim7 voicing gap (blocker → Option 1)

**Decided:** Add a `Diminished7` arm to `BeginnerShellStrategy` (root + ♭3 + ♭♭7) as a new plan step, appending IN11 to the locked req.

**Why:** Dropping the real `#4dim7` into the pack made seed render tests throw — the shell voiced only Dominant7/Minor7/Major7. The *spelling* was fine (`Bdim7`); the *voicing* was missing. Adding the arm was the only way IN8 (`#4dim7` in the default pack) is genuinely satisfied end-to-end, matching durable-over-minimal. Voicing it as a supported quality was musically wrong (a `#IVdim7` must be a dim7). The scope-widening into the voicing engine was surfaced and tracked append-only (IN11) rather than done silently.

*Source: de_01KVXYQ0HR654X95B5HCVJC64K, ch_01KVXVM33HYT2F895RZ15ARW91*

---

## 11. Song default feel — content property, not catalog metadata

### 11a. Feel mirrors `key`, owned by Song only

**Decided:** Default feel is a Song DSL directive `feel <token>` (space keyword, like `key`) parsed into `Song.DefaultFeel` on the pure record — no `CatalogHeader` change, no new column, no migration. Owned by **Song only**; progressions and rhythms stay feel-agnostic.

**Alternatives weighed (and rejected):** feel via `CatalogHeader`/a `CatalogMetadata` field/a new entity column; feel on progressions; rhythm-owned feel; a per-progression `\tf` cascade with restore.

**Why:**
- **Feel is a content property, not catalog metadata.** Genre/subgenre/tags are *discovery* fields (filter/search, denormalized to columns). Default feel answers "how should this *sound* by default" — like `key`, which is already a Song directive parsed into `Song.InitialKey` and never touches `CatalogHeader`. Modeling feel identically means no persistence change (it rides inside the `Dsl` string) and no denormalization (feel is never a filter field). This corrected the earlier draft that had planned a catalog column + migration.
- **C4 stays intact** — it's about the realized RhythmPattern/tick grid, not the Song; a Song carrying a suggested default is identical to it carrying `InitialKey`.
- **Progressions stay a pure harmonic primitive** (just bars/chords) — they have no `key` today and adding `feel` would give the space-split progression grammar its first directive. Keeping them pure matches the existing boundary; a bare-progression drill inherits feel from the transport.
- **Rhythms carry no default feel** because literal `:3` already gives exact, feel-immune triplets — so a rhythm needing swing writes it literally, and the song/exercise feel handles the interpretive case. This avoided a song-vs-rhythm precedence conflict.
- **The per-progression `\tf` cascade + restore is per-section feel** — a separate, bigger axis; one `\tf` per exercise avoids it.
- **`feel <token>` not `feel:`** — the colon is reserved for stored-part references (`NAME: id`), which `feel: x` would misparse.
- **Nullable, with `feel none` distinct from omission** — explicit "this is a straight tune" (seeds the control to None deliberately) vs "no opinion" (falls back to None).

*Source: ch_01KWSD1TBFGAJ318Z11027TQ34, de_01KWSMDGPC8AYX0H26JH8FX792*

### 11b. Superseded by the ScoreR redesign

**Decided:** After the feel-domain slice shipped, a follow-up surfaced that key/tempo/feel are all *render params* that should live in and live-render from the score component. The feel-domain work (`Song.DefaultFeel`, the `feel` directive, read-DTO) stays valid; only the UI seeding wiring is superseded, moved to a new `ui/scorer-render-params` thread.

**Why:** The bugs (feel not live on Practice; content preview always Straight) shared one root — render params seeded per-content but not live-rendered. The durable seam is "render/interpretation params (key/tempo/feel) owned by ScoreR, live on change, seeded per content; definition params (harmony/comping/difficulty) on the page, need Generate." That split subsumes both bugs by construction and is bigger than the feel thread, so it earned its own thread. Progressions/rhythms get ChordFlow defaults (C / 80 / Straight); `tempo` becomes a new Song directive there.

*Source: ch_01KWSD1TBFGAJ318Z11027TQ34*

---

## 12. Cross-cutting principles that recur (the meta-rationale)

Several decisions above are instances of standing principles the docs repeatedly invoke:

- **Design for all of it; implement in additive slices; never require a breaking change to adopt a deferred part.** (Multi-bar type, triplet-feel enum, transforms.) *Source: ch_01KTVSYS2RQK5FQXT2R78F8B90*
- **Everything derived, never hand-authored per case; one authored source of truth.** (Quality formulas → semitones; interval `Name` by formula; DSL string as the single persisted form.) *Source: de_01KVJYPHF2G4CHGNEN9ZMFPGAY, de_01KVF92S3CBV2XT4B8N9SF3038*
- **`AlphaTexRenderer` is the only alphaTex-aware component.** (Song section rendering; feel `\tf`; anacrusis; rhythm-notation dots/ties.) *Source: ch_01KTXPJ0ZQNQEKDCB5A9EATHNZ*
- **Fail loud over silent.** (Guarded factories; out-of-range `take`; missing references; ambiguous durations; cross-boundary ties in the harmonic path.) *Source: de_01KTP11T7JCSDK6PN2FEXDR5CW, de_01KVQ23HY2X7VM6JY2S51F0NCH*
- **Dogfooding surfaces the real requirements** — maj7/dim7 voicing gaps, rest coalescing, and the ScoreR render-param split were all found by using the app on real tunes, not by unit tests. *Source: ch_01KVQ0EWQFKBSSNKCTRN951QWV, ch_01KVVY9WZ0FN3016WRT5YQM7HQ, ch_01KWSD1TBFGAJ318Z11027TQ34*
- **Reference-doc sync is part of the change** — every domain/DSL change updates its matching `loom/refs/` doc in the same unit of work. *Source: multiple design docs.*

---

## Note on completeness

Every decision above cites the doc where its rationale is recorded. The docs are unusually rich in "why," so there were few gaps; where a decision was a simple confirmation with the reasoning captured in a prior turn (e.g. some minor Song settlements), the reasoning is attributed to that turn's doc. No rationale has been invented beyond what the cited docs state.

## Provenance

- **Kind:** decisions
- **Scope:** weaves: domain; threads: all; from: —; to: —
- **Sources:** ch_01KTNV0GKNXJ5V3Q0J5TF8AAT5, ch_01KTQ8NE2DQCHXCMGA6GWSWNJT, de_01KTP11T7JCSDK6PN2FEXDR5CW, ch_01KTQWTHWQ83ZXAB0SZRZANXTS, ch_01KTVR6VE608663XSSKMDJXV38, ch_01KTVSYS2RQK5FQXT2R78F8B90, de_01KTVTNZPYS36K23R5Z9MYDB54, de_01KTVVTS9HG5X2C39TC1X1KP94, ch_01KTXPJ0ZQNQEKDCB5A9EATHNZ, ch_01KTXQEDNMEV8EPE2MK4T4T33D, ch_01KTXSJBKZYTW64A9E11J2X9JV, ch_01KTY8G498TJ4ZNNVEY4420R8K, ch_01KVBNHXQWD1RM6TT3M6NGRR93, de_01KVF92S3CBV2XT4B8N9SF3038, ch_01KVJX7BV4HHDCXYQY6Q616GHA, de_01KVJYPHF2G4CHGNEN9ZMFPGAY, ch_01KVQ0EWQFKBSSNKCTRN951QWV, ch_01KVQ0QFCDAA5NN379SHHGX7EX, ch_01KVQ3X698E9RDYZ5FBNQD1QJR, de_01KVQ23HY2X7VM6JY2S51F0NCH, de_01KVQ4HM9ZV5SSRR86P1TGZ9H2, de_01KVR89QNHC6NE2XHTJ6EM9MDQ, ch_01KVR2BPBMHM8SF34070D2SASD, ch_01KVVY9WZ0FN3016WRT5YQM7HQ, de_01KVW7YEZ70AXE0NNAE82DJWTX, de_01KVXYQ0HR654X95B5HCVJC64K, ch_01KVXVM33HYT2F895RZ15ARW91, ch_01KWSD1TBFGAJ318Z11027TQ34, de_01KWSMDGPC8AYX0H26JH8FX792, ch_01KX0TANYXS75WWAHJCZBNHMDE
- **Generated:** 2026-07-13T21:38:05.194Z
