---
type: idea
id: id_01KVYRP94S3FVD9VAGSH81046N
title: Engine-derived voicings as the app's source (authored → oracle)
status: draft
created: 2026-06-25
updated: 2026-06-25
version: 8
tags: []
parent_id: null
requires_load: []
---
# Engine-derived voicings as the app's source (authored → oracle)

## Goal

Make the **engine** (`CagedDerivation.Derive`) the app's **`automatic` voicing source** — one of three coexisting sources (automatic / package / user, per [[content-source-model]]) — and demote the **36 built-in CAGED chord voicings** (today duplicated between the default pack and the engine) to a **test-only golden-oracle fixture** the app no longer ships. The engine becomes a real runtime source; the authored grips exist only to verify it.

## Origin

Spun off from `guitar/shell-voicing-derivation` (chat-001); scoped here in chat-001. **Depends on [[content-source-model]]** — the additive, source-tagged listing that lets `automatic` voicings coexist with package/user voicings (without it, an engine source would just *hide* the others, which is the very thing we're rejecting).

## Why

The interval-derivation-engine vision: **authored = golden oracle, engine = a real source.** Today the engine is oracle-verified against the authored grips, but the app render path still consumes the authored set — `Program.cs:94` builds `new AlphaTexRenderer(new VoicingBook(voicingLibrary))` where `voicingLibrary = new VoicingStore(db).LoadShapes()` (`Program.cs:86`), the DB seeded from `Content/default-pack/voicings/*.dsl` via `PackImporter`. `CagedDerivation.Derive` output is **never** in this path (it only feeds the `CagedShapesHandler`/`CagedChordHandler` dogfood UI). So we ship the oracle, not the engine. This thread closes that gap.

> Correction from chat: it is **36** grips — `(maj, min, maj7, dom7, min7, aug)×5 + (m7b5, dim7)×3` — not 34. The previous idea text and the `CagedDerivationOracleTests` comment said 34 and are **stale**.

## Runtime-source nuance (hard ordering constraint)

The app's voicing source is the **SQLite DB**, seeded from the pack `.dsl` at first run — *not* the `.dsl` files directly. Relocating the 36 grips out of the pack **severs that seed**, so the engine-as-source wiring and the relocation **must land together** — otherwise `VoicingBook` falls back to `BeginnerShellStrategy` and the app silently regresses to shell voicings.

## Shape (sketch — design firms this up)

- **Type bridge.** A `ChordShape → Voicing` adapter (`Derive` keeps returning `ChordShape`; it already carries per-string fret, muted, `Semitones`, `Shape`, `AnchorFinger`, `Zone` — lossless for both rendering and ranking).
- **Engine as a computed source.** A "computed voicing source" that synthesizes `automatic`-tagged candidates from `Derive` (over quality×shape within a fret window) — **un-persisted, always fresh** — unioned into the voicing listing alongside the store-backed package/user sources. It does **not** go through `PackImporter`/SQLite.
- **Comping resolution (main-source + fallback).** Bulk voicing auto-fill across a progression uses a **main source** — a transient generate-time practice knob, structured `{ kind, region?, packageId? }`, flowing through the `generate` envelope (evolving the existing `renderOptions.voicing` knob; **not** baked into content). Per chord: try the main source, else fall back `user > package > automatic`. Fallback is **per-chord**, so a song may mix sources (the UI may flag fallen-back chords). Within `automatic`, a chord's grip is chosen by a **ranking strategy** — a seam this thread defines and ships **one default** for: **Closest** (1st chord = lowest-fret grip in the region; next = reuse this chord's earlier grip if it already appeared, else the grip closest to the previous chord). Alternative selectable modes (all-CAGED-shapes variety; guide-tone voice-leading) are additive — see [[voicing-ranking-strategies]].
- **Relocate the oracle.** Move the 36 authored `.dsl` grips from `Content/default-pack/voicings` into a **test-only fixture** loaded by `CagedDerivationOracleTests` (keep `.dsl` so the same parser reads them).

## Coverage gating (don't silently regress)

The oracle verifies `Derive` against exactly the **36** grips; the engine can derive other quality×shape combos that are then derived-but-unverified. Once the app sources `automatic` from `Derive`, gate it: (a) `Derive` fails loud when it can't spell a grip (it throws today — `CagedDerivation.cs:36,101`), and (b) a structural test asserts every quality×shape the `automatic` source offers derives a valid, fully-spelled grip — pinned to the 36-grip set (m7b5/dim7 deliberately omit the shapes the oracle omits).

## Voicings stay user-editable

Demotion is *only* of the 36 built-in CAGED grips to test-only. Voicings remain a first-class editable content kind: users still author voicings (the `user` source), and packages may ship voicings (the `package` source). Engine = the `automatic` base; the other two coexist per [[content-source-model]].

## Docs to update (when this lands)

- `chordflow-domain-model-reference.md` — the `automatic` voicing source = the engine; the authored 36 = oracle.
- `chordflow-architecture-reference.md` — the oracle fixture is a test asset, not shippable content; the computed voicing-source seam; the main-source/fallback comping resolution.
- Fix the stale "34" in `CagedDerivationOracleTests`.

## Scope

**In:** the `ChordShape → Voicing` adapter, the engine computed-source + its union into the voicing listing, the main-source/fallback comping resolution + the generate-time main-source knob, the ranking seam + the default Closest strategy, relocating the 36 grips to a test-only oracle, the coverage-gating structural test, the ref-doc updates.
**Out:** the additive listing / source tags / filter UI ([[content-source-model]]); the selectable alternative ranking modes + their selection UI ([[voicing-ranking-strategies]]) — this thread ships only the ranking seam + the default Closest strategy; shell derivation ([[shell-voicing-derivation]]); 6th derivation ([[caged-sixth-voicings]]).

## Open design questions (for design)

1. Where the computed source + the union live (a new type? extend `VoicingBook`? a source-aggregating layer above the stores?).
2. The `automatic` synthetic-identity scheme (e.g. `auto:dom7:E`) and how the main-source knob encodes a region.
3. The default **Closest** strategy's specifics: the grip-distance metric, and the "reuse this chord's earlier grip" rule (same chord → same grip). The pluggable seam's full mode set + selection UI live in [[voicing-ranking-strategies]].

## Validation

- The app comps engine-derived (`automatic`) grips end-to-end; the 36-grip pack is not referenced by the app.
- `CagedDerivationOracleTests` passes against the relocated oracle fixture.
- Coverage test: every quality×shape the `automatic` source offers derives a valid, fully-spelled grip, no throw.
- Main-source selection (e.g. `automatic 5–12`) changes the comped grips; the fallback fills chords the main source lacks.
- **Dogfood:** render engine-derived comping on the fretboard UI page (now/next fret-boxes) for a 12-bar blues.
