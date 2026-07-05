---
type: done
id: pl_01KWSPR473SXWY0XE20FM4BEYE-done
title: Done — Default triplet feel — Song feel directive + selection seeding
status: done
created: 2026-07-05
version: 2
tags: []
parent_id: pl_01KWSPR473SXWY0XE20FM4BEYE
requires_load: []
---
# Done — Default triplet feel — Song feel directive + selection seeding

## Step 1 — Add nullable `DefaultFeel` (TripletFeel?) to the Song record and parse a whole-song `feel <token>` directive in SongParser (peer of `key`).

`Song.DefaultFeel` (nullable `TripletFeel?`) added to the `Song` record as the peer of `InitialKey`, plumbed through the private ctor + `FromSections` (trailing optional) and `OfProgression` (null — a lifted bare progression has no feel). `SongParser` parses a whole-song `feel <token>` directive (space keyword like `key`): idents `none`/`triplet8th`/`triplet16th`, unknown ident throws, a duplicate `feel` throws, `feel` added to the reserved-keyword guard. Files: `Song.cs`, `SongParser.cs`.

## Step 2 — Unit-test feel parsing (valid/unknown/absent/`feel none`/reserved-name) and Song DSL round-trip preserving the directive.

Parser tests (idents / case-insensitive / unknown-throws / absent→null / `feel none`→None-not-null / position-independent / duplicate-throws / reserved-name-throws / textual round-trip) in `SongParserTests.cs`; model tests (`FromSections` carries/defaults DefaultFeel, `OfProgression`→null) in `SongModelTests.cs`. Green.

## Step 3 — Surface the selected song's DefaultFeel across the bridge on the same path the Key control already seeds from (play-ui-key-init).

`DefaultFeel` threaded `SongStore.List()` → `ContentSummary` → `ContentItem` so it rides the `entityList` JSON exactly like `InitialKey`. `SongStore` now parses each song once for both seeds (`SeedsOf` → `(key, feel)`); feel serialized as the enum-name ident or null. Store-integration test added in `ContentCrudStoreTests.cs`. Files: `SongStore.cs`, `IContentStore.cs`, `ContentCrudEnvelopes.cs`, `ContentCrudHandler.cs`.

## Step 4 — Initialize the play-time feel control from the selected song's DefaultFeel, mirroring the Key-control seeding; transport stays the override.

SUPERSEDED. Implemented as a page-seed (`app.js seedFeelForHarmony` + a non-rendering `view.seedTripletFeel` in `score-render-component.js`, wired to the harmony `change`). Testing showed this seeds the control but doesn't live-render (Bug 1), and the content preview was never seeded at all (Bug 2). Both are folded into the new **ScoreR render-params** thread, which replaces the page-seed with ScoreR owning key/tempo/feel and live-rendering. The JS edits remain in the tree as a stepping stone (revisited there).

## Step 5 — Document the `feel` Song directive and `Song.DefaultFeel` in the two authoritative refs.

Refs synced via `loom_patch_doc`: `chordflow-dsl-reference` gained the `feel <token>` Song directive (space keyword, idents, absent-vs-`feel none`, Song-only); `chordflow-domain-model-reference` gained `Song.DefaultFeel` + selection-seeding + a C4-clarifying invariant (feel mirrors `key` — content on the Song, never baked into the realized pattern).

## Step 6 — Author a song with `feel triplet8th`, load it, confirm the control seeds + override works + one whole-song `\tf` renders (and none when straight).

Domain + store path verified by tests: full suite 793 green (incl. the new store-integration test). `\tf` emission is unchanged pre-existing behaviour driven by `Exercise.TripletFeel` (EX6). GUI-interactive verify surfaced two UI bugs (feel not live on song-select; content preview always Straight) — deferred to the ScoreR render-params thread, which fixes both by construction.

## Closing notes

Feel **domain** shipped and green: `Song.DefaultFeel` + the `feel <token>` Song directive + `DefaultFeel` on the read DTO (`ContentSummary`/`ContentItem`) + ref sync; full suite 793 pass. The **UI wiring** (page-seed, step 4) is superseded by the new `ui/scorer-render-params` thread, which moves key/tempo into ScoreR, live-renders all three params, and fixes the two feel bugs found in testing (feel not live on song-select; content preview always Straight). Nothing committed at close.
