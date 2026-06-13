---
type: done
id: pl_01KTZWZPKHS2BHNAR7RABZPDDX-done
title: Done — Voicings — authored content pillar (slice 1)
status: done
created: "2026-06-13T00:00:00.000Z"
version: 6
tags: []
parent_id: pl_01KTZWZPKHS2BHNAR7RABZPDDX
requires_load: []
---
# Done — Voicings — authored content pillar (slice 1)

## Step 1 — Voicing DSL + parser + `VoicingShape` entry type + canonical-C normalizer

**DSL, parser & canonical-C normalize** — all in `src/ChordFlow.Core/Domain/Voicings/` (namespace `ChordFlow.Domain`, matching the flat domain convention even in subfolders).

- **`CagedShape.cs`** — enum `{ C, A, G, E, D }`. Familiarity rank (Step 3) will hang off this.
- **`VoicingShape.cs`** — `record VoicingShape(Quality Quality, CagedShape Shape, int RootString, Voicing Canonical)`. The parsed entry; `Canonical` is the C-anchored `Voicing` (reuses the existing value type — no new downstream type).
- **`VoicingDslParser.cs`** — `Parse(string) → VoicingShape`. Grammar `voicing <Chord> shape:<C|A|G|E|D> root:<6..1> frets: <s6…s1>`; `x`=muted, `0`=open; trailing `#` comment stripped. Note-name+suffix anchor parse (any anchor accepted); quality-suffix vocab mirrors `ProgressionParser` (local copy — a shared suffix table is a noted future cleanup).
  - **Normalize-to-C:** `semisToC = Mod12(-anchorRoot)` (transpose up to the next C, always 0..11 so no negatives arise), then octave-fold uniformly so the lowest fret lands in `[0,11]` — the canonical lowest non-negative placement. Verified: `Gmaj 320003` → `875558` (G-shape at C); E-shape authored at C vs at B dedupe to one canonical.
- **Tests** `tests/ChordFlow.Core.Tests/VoicingDslParserTests.cs` (flat dir, matching existing test layout — deviates from the plan's `Voicings/` subfolder): open-C-at-C identity, open-G→G-shape-at-C normalize, cross-anchor dedupe, quality-suffix mapping, flat anchor, comment strip, 8 malformed-input throws.

Reuses `PitchClass` + `Fretboard.StringCount`; pure, no I/O (C1). No first-class `Interval` type (EX7).

## Step 2 — `Realize(shape, targetRoot)` — movable transpose, octave-fold, 0–15 guard

**`Realize` transpose** — `src/ChordFlow.Core/Domain/Voicings/VoicingRealizer.cs`.

- `static Voicing? Realize(this VoicingShape shape, PitchClass targetRoot)` (extension method → `shape.Realize(root)` ergonomics, own file per the plan). `const MaxFret = 15`.
- `semis = Mod12(targetRoot)` (canonical is C-anchored at pc 0); add to every fretted string; octave-fold uniformly so the lowest fret sits in `[0,11]` (lowest placement); return `null` when the span still exceeds fret 15. Muted strings preserved; `FirstFret` = lowest fret; `BarreFret` left `null` (reliable barre inference deferred). Output reuses the existing `Voicing` type.
- **Correction to a chat napkin value:** the chat wrote `x32010 → x53232 Dmaj`; the mathematically correct uniform transpose is **`x54232`** (D F# A D F# — a real D major; `x53232` would put an F natural on the D string). The implementation + test use `x54232`. This is exactly the "implement & verify rather than trust the sketch" payoff.
- **Tests** `tests/ChordFlow.Core.Tests/VoicingRealizerTests.cs`: C-shape→D barre (`x54232`), G-shape→open-G fold-down (`320003`), realize-at-anchor identity, all-12-roots stay in `[0,15]`, muted-string survival, and the wider-than-window → `null` guard.

0–15 window guard (C4); pure domain (C1). **57 voicing-filtered tests pass** (9 pre-existing `VoicingBook` + 14 new across parser/realizer with theory cases) — existing files untouched.

## Step 3 — `CagedShape` familiarity rank + `VoicingBook.Lookup` (exact-quality, ranked, stored-first over a supplied entry set, strategy fallback)

**`VoicingBook` → instance, stored-first, exact-quality, ranked** (Option A from `voicings-chat-001`; req **IN4 amended → v2** and re-locked).

- **`CagedShape.cs`** — added `CagedShapeRanking.FamiliarityRank(this CagedShape)` (E=0, A=1, G=2, C=3, D=4; barre-roots first). Static default; pack-override is the deferred packs work (IN5).
- **`VoicingBook.cs`** — rewritten from a static strategy-dispatcher to a **sealed instance class** built with the authored library:
  - `Candidates(chord, difficulty)` → exact-quality stored entries (`s.Quality == chord.Quality`) `Realize`d to `chord.Root`, playable ones kept, **ordered by neck position then `FamiliarityRank`**. May be empty. `difficulty` is carried but unused in slice 1 (reserved for the deferred difficulty band, EX6).
  - `Lookup(chord, difficulty)` → `Candidates[0]` if any, else the strategy-generated shape; throws `NotSupportedException` when neither covers the chord (preserves the prior fail-loud contract). Stored **shadows** generated.
  - Default ctor uses the `BeginnerShellStrategy` registry; a second ctor injects a strategy map (test seam).
- **`AlphaTexRenderer.cs`** — the call site `VoicingBook.Lookup(chord, difficulty)` was static, so the instance switch broke it. Minimal, output-preserving fix: a `private readonly VoicingBook _book = new(Array.Empty<VoicingShape>())` field (empty library → strategy-only → byte-identical render), and the three chord-path helpers (`RenderBars`/`RenderBar`/`FormatChord`) de-static'd to reach it. The real repository-backed library is injected in **Step 5** (this empty default is honest scaffolding, not a back-compat shim).
- **`VoicingBookTests.cs`** — the 9 existing tests moved to `new VoicingBook(Array.Empty<VoicingShape>())` (a `StrategyOnly()` helper), behaviour identical. Added 7 authored-voicing tests: stored-resolves-where-strategy-throws, stored-shadows-generated, exact-quality (`maj7` ⊄ `maj` → empty + throw), empty candidates on no match, realize-to-root spelling, neck-position ranking, familiarity-rank ordering.

**Full suite: 303 passed / 0 failed.** Solution builds 0 errors (incl. Desktop). Only callers of the old static `VoicingBook` were the renderer + this test file (verified via grep) — both updated.

## Step 4 — `VoicingEntity` + `Voicings` EF table + migration + repository

**Persistence — `VoicingEntity` + `Voicings` table + migration + read store.**

- **`Persistence/Entities/VoicingEntity.cs`** — `IOriginated`, full **catalog parity with `ProgressionEntity`** (IN6 "mirrors ProgressionEntity"): `Id, Name, Dsl, Origin, PackId?, Genre?, Subgenre?, Tags, CreatedUtc`. DSL-only; the stored `Dsl` is always the **canonical-C** form.
- **`Domain/Voicings/VoicingDslWriter.cs`** — `ToDsl(VoicingShape) → string`, the inverse of the parser. Emits `voicing C<suffix> shape:<X> root:<n> frets: <s6…s1>` from the canonical positions, so saving an authored voicing at any anchor persists one canonical-C line (the IN2/C3 "canonical on save" mechanism). Idempotent + round-trips through `Parse`.
- **`ChordFlowDbContext.cs`** — `DbSet<VoicingEntity> Voicings` + `OnModelCreating` (string PK, `Origin` by name, `Tags` default `"[]"`) — byte-for-byte the Progression/Song config.
- **Migration `20260613091843_AddVoicings`** — generated via `dotnet ef` (tooling 10.0.8); creates `Voicings` with the catalog columns (Genre/Subgenre nullable, Tags default `[]`). Snapshot updated.
- **`Persistence/VoicingStore.cs`** — read-side, mirrors `RhythmPatternStore`: `LoadShapes()` → all rows parsed to `VoicingShape`s (the library handed to a `VoicingBook` at the Step-5 seam) + `Find(id)`. (The save path lands with the UI in Step 6, using `VoicingDslParser` + `VoicingDslWriter`.)
- **Tests** — `VoicingPersistenceTests` (in-memory SQLite, `Migrate()`: LoadShapes round-trip, Find reconstructs canonical shape, missing→null, catalog columns store/read) + `VoicingDslWriterTests` (parse↔write round-trip incl. idempotence, and anchor-normalization on serialize).

**Full suite: 311 passed / 0 failed.** Pure read store (C1); Persistence-only DB touch (C2); DSL-only canonical storage (C3); catalog provenance adopted (C5).

## Step 5 — Wire repository → `VoicingBook` (stored-first end-to-end, stored-shadows-strategy)

**Wire repository → `VoicingBook` → renderer (stored-first end-to-end).**

- **`AlphaTexRenderer.cs`** — replaced the Step-3 empty-library scaffold field with **constructor injection**: `AlphaTexRenderer(VoicingBook book)` (the real seam) + a parameterless `AlphaTexRenderer()` documented as the *no-authored-library / generated-only* renderer (a meaningful default, not a back-compat shim — it's what the song-render tests want). `_book` is now `readonly`, set in the ctor.
- **`ChordFlow.Desktop/Program.cs`** — at startup, after migrate/seed, load the authored library once: `voicingLibrary = new VoicingStore(db).LoadShapes();` (inside the `using` so it's read before the context disposes — the parsed `VoicingShape`s are detached), then `new AlphaTexRenderer(new VoicingBook(voicingLibrary))`. The whole render pipeline (GenerateExercise, ExerciseLibrary, TrySendScore) now resolves chords stored-first. Documented the slice-1 limitation: the library is snapshotted at launch, so voicings authored later apply on next start (live refresh is additive).
- **Tests** `VoicingBookIntegrationTests` — real SQLite: a stored C7 voicing loaded via `VoicingStore` → `VoicingBook` **shadows** the 3-note shell (6 positions vs 3; `Candidates` has 1); and no-stored-voicing falls back to the generated shell with empty `Candidates`.

**Full solution builds 0 errors (incl. Desktop); 313 tests pass.** Renderer stays the sole alphaTex seam; Desktop→Core direction unchanged (C2).

## Step 6 — Ref-sync — update `chordflow-domain-model-reference.md` (+ DSL ref if the public surface changed)

**Reference-doc sync** (IN8). UI step (old #6) was removed from this plan — moved to the new `ui/content-crud` thread; IN7 retired in req v3.

- **`chordflow-domain-model-reference.md`** §2 Voicing layer — `VoicingBook` row rewritten (instance, `Candidates` + `Lookup`, stored-first, shadows generated); added rows for `VoicingShape`, `CagedShape` + `FamiliarityRank`, `VoicingDslParser`/`VoicingDslWriter` (canonical-C normalize), `VoicingRealizer`. §6 Persistence — added `VoicingEntity` / `VoicingStore` (full catalog parity, `AddVoicings` migration, `LoadShapes` → library at the feature seam). §7 pipeline — voicing line now "stored-first: authored ∥ strategy fallback."
- **`chordflow-dsl-reference.md`** — intro updated to "four DSLs"; added a full **Voicing DSL** section (grammar, field table, examples incl. the verified `875558` G-shape, movability note, canonical-C storage, common errors), marked engine-internal-today like the Rhythm DSL.

Both refs edited via `loom_patch_doc` (gate-excluded, frontmatter preserved).
