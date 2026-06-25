---
type: idea
id: id_01KVYRP94S3FVD9VAGSH81046N
title: Engine-derived voicings as the app's source (authored → oracle)
status: done
created: 2026-06-25
updated: 2026-06-25
version: 10
tags: []
parent_id: null
requires_load: []
---
# Engine-derived voicings as the app's source (authored → oracle)

## Goal

Make the **engine** (`CagedDerivation.Derive`) the app's **`automatic` voicing source** — one of three coexisting sources (automatic / package / user, per [[content-source-model]], now **landed**) — and demote the **36 CAGED chord voicings the default pack ships** (today duplicated with the engine) to a **test-only golden-oracle fixture** the app no longer ships. The engine becomes a real runtime source; the authored grips exist only to verify it.

## Origin

Spun off from `guitar/shell-voicing-derivation` (chat-001); scoped here in chat-001. **Depended on [[content-source-model]]**, which has now shipped — the additive, source-tagged listing + the `IComputedContentSource` union seam that lets `automatic` voicings coexist with package/user voicings. (Without it, an engine source would just *hide* the others — the thing we rejected.) chat-002 re-grounded this idea against the post-content-source-model tree.

## Why

The interval-derivation-engine vision: **authored = golden oracle, engine = a real source.** Today the engine is oracle-verified against the authored grips, but the app render path still consumes the authored set — `Program.cs:98` builds `new SwappableRenderer(new AlphaTexRenderer(new VoicingBook(voicingLibrary)))` where `voicingLibrary = new VoicingStore(db).LoadShapes()` (`Program.cs:90`), the DB seeded from `Content/default-pack/voicings/*.dsl` via `DefaultPack.ImportInto`/`PackImporter` (now imported as `Origin.Pack`, `PackId="default"` — `content-source-model` retired `BuiltIn`). `CagedDerivation.Derive` output is **never** in this path (it only feeds the `CagedShapesHandler`/`CagedChordHandler` dogfood UI). So we ship the oracle, not the engine. This thread closes that gap.

> It is **36** grips — `(maj, min, maj7, dom7, min7, aug)×5 + (m7b5, dim7)×3` — not 34. The stale "34" still lives in the `CagedDerivation` class comment (`CagedDerivation.cs:17`) and the `CagedDerivationOracleTests` comment — fix both when this lands.

## Two distinct seams (don't conflate listing with comping)

`content-source-model` built the **listing** seam; this thread also needs the **comping/render** seam. They are different and the design must address both:

1. **Listing (visibility).** `ContentCrudHandler.List` already unions store rows with `IComputedContentSource.List(kind)` (`ContentCrudHandler.cs:70-73`) — an empty seam today. The engine source **implements `IComputedContentSource`** to surface `automatic`-tagged voicing rows (`ContentItem(Id, Name, "automatic", null)`) on the Content page. This is the *catalog* view only — `ContentItem` carries no grip geometry.
2. **Comping/resolution (what actually plays).** Auto-fill across a progression resolves an actual grip per chord — this is the `VoicingBook.Lookup`/`AlphaTexRenderer` path (`Program.cs:98`), **not** the listing. The main-source + fallback rule and the ranking strategy live here. This is the larger architectural change (it changes how a grip is picked for the score).

## Runtime-source nuance (hard ordering constraint)

The app's comping source is the **SQLite DB**, seeded from the pack `.dsl` at first run — *not* the `.dsl` files directly. Relocating the 36 grips out of the pack **severs that seed**, so the engine-as-source wiring and the relocation **must land together** — otherwise `VoicingBook` falls back to `BeginnerShellStrategy` and the app silently regresses to shell voicings. Startup now also runs `ContentSourceMigration.Run(db)` right after `DefaultPack.ImportInto` (`Program.cs:84,87`); the relocation must stay idempotent through that import+migrate path.

## Shape (sketch — design firms this up)

- **Type bridge.** A `ChordShape → Voicing` adapter (`Derive` keeps returning `ChordShape`; it already carries per-string `Fret`/`Muted`, `Semitones`, `Shape`, `AnchorFinger`, `Zone` — `ChordShape.cs` — lossless for both rendering and ranking).
- **Engine as a computed source.** Implement the **existing `IComputedContentSource`** seam for `ContentEntity.Voicing` — synthesizing `automatic`-tagged candidates from `Derive` (over quality×shape within a fret window), **un-persisted, always fresh**, unioned into the listing by `ContentCrudHandler.List`. It does **not** go through `PackImporter`/SQLite.
- **Comping resolution (main-source + fallback).** Bulk voicing auto-fill uses a **main source** — a transient generate-time practice knob, structured `{ kind, region?, packageId? }`, flowing through the `generate` envelope (evolving the existing `renderOptions.voicing` knob; **not** baked into content). Per chord: try the main source, else fall back `user > package > automatic`. Fallback is **per-chord**, so a song may mix sources (the UI may flag fallen-back chords). Within `automatic`, a chord's grip is chosen by a **ranking strategy** — a seam this thread defines and ships **one default** for: **Closest** (1st chord = lowest-fret grip in the region; next = reuse this chord's earlier grip if it already appeared, else the grip closest to the previous chord). Alternative selectable modes (all-CAGED-shapes variety; guide-tone voice-leading) are additive — see [[voicing-ranking-strategies]].
- **Relocate the oracle.** Move the 36 authored `.dsl` grips from `Content/default-pack/voicings` into a **test-only fixture** loaded by `CagedDerivationOracleTests` (keep `.dsl` so the same parser reads them).

## Coverage gating (don't silently regress)

The oracle verifies `Derive` against exactly the **36** grips; the engine can derive other quality×shape combos that are then derived-but-unverified. Once the app sources `automatic` from `Derive`, gate it: (a) `Derive` fails loud when it can't spell a grip (it throws today — `CagedDerivation.cs:36,101`), and (b) a structural test asserts every quality×shape the `automatic` source offers derives a valid, fully-spelled grip — pinned to the 36-grip set (m7b5/dim7 deliberately omit the shapes the oracle omits).

## Voicings stay user-editable

Demotion is *only* of the 36 default-pack CAGED grips to test-only. Voicings remain a first-class editable content kind: users still author voicings (the `user` source), and packages may ship voicings (the `package` source). Engine = the `automatic` base; the other two coexist per [[content-source-model]].

## Docs to update (when this lands)

- `chordflow-architecture-reference.md` — §3 already has the source model; add that the `automatic` voicing source = the engine filling `IComputedContentSource`, plus the main-source/fallback comping resolution.
- `chordflow-domain-model-reference.md` — **§6 Persistence is currently stale** (still describes the retired `BuiltIn`/shadow/collapse model and says "the default pack carries none today"); it needs the `content-source-model` correction *and* this thread's `automatic` voicing-source delta.
- Fix the stale "34" in `CagedDerivation.cs:17` + `CagedDerivationOracleTests`.

## Scope

**In:** the `ChordShape → Voicing` adapter, the engine `IComputedContentSource` implementation (listing union), the main-source/fallback comping resolution + the generate-time main-source knob, the ranking seam + the default Closest strategy, relocating the 36 grips to a test-only oracle, the coverage-gating structural test, the ref-doc updates (incl. the inherited domain-ref §6 correction).
**Out:** the additive listing / source tags / filter UI ([[content-source-model]], landed); the selectable alternative ranking modes + their selection UI ([[voicing-ranking-strategies]]) — this thread ships only the ranking seam + the default Closest strategy; shell derivation ([[shell-voicing-derivation]]); 6th derivation ([[caged-sixth-voicings]]).

## Open design questions (for design)

1. Where the **`IComputedContentSource` implementation** lives, and where the comping main-source/fallback resolution lives (a new type? extend `VoicingBook`? a source-aggregating layer above the stores/engine?). The listing *union point* is already decided (`ContentCrudHandler.List`); open is where the engine impl + the comping resolver sit.
2. The `automatic` synthetic-identity scheme (e.g. `auto:dom7:E`) and how the main-source knob encodes a region.
3. The default **Closest** strategy's specifics: the grip-distance metric, and the "reuse this chord's earlier grip" rule (same chord → same grip). The pluggable seam's full mode set + selection UI live in [[voicing-ranking-strategies]].

## Validation

- The app comps engine-derived (`automatic`) grips end-to-end; the 36-grip pack is not referenced by the app.
- `CagedDerivationOracleTests` passes against the relocated oracle fixture.
- Coverage test: every quality×shape the `automatic` source offers derives a valid, fully-spelled grip, no throw.
- Main-source selection (e.g. `automatic 5–12`) changes the comped grips; the fallback fills chords the main source lacks.
- The Content page lists `automatic` voicing rows alongside package/user (the `IComputedContentSource` union, visible).
- **Dogfood:** render engine-derived comping on the fretboard UI page (now/next fret-boxes) for a 12-bar blues.