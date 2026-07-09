---
type: plan
id: pl_01KWSPR473SXWY0XE20FM4BEYE
title: Default triplet feel — Song feel directive + selection seeding
status: done
created: 2026-07-05
updated: 2026-07-05
version: 1
design_version: 2
req_version: 2
tags: []
parent_id: de_01KWSMDGPC8AYX0H26JH8FX792
requires_load: []
target_version: 0.1.0
actual_release: 0.13.0
steps:
  - id: song-defaultfeel-feel-directive-in-songparser
    order: 1
    status: done
    description: Add nullable `DefaultFeel` (TripletFeel?) to the Song record and parse a whole-song `feel <token>` directive in SongParser (peer of `key`).
    files_touched: [src/ChordFlow.Core/Music/Songs/Song.cs, src/ChordFlow.Core/Music/Songs/SongParser.cs]
    blocked_by: []
    satisfies: [IN1, IN2, IN3, IN7, C1, C2, C3]
  - id: parser-round-trip-tests
    order: 2
    status: done
    description: Unit-test feel parsing (valid/unknown/absent/`feel none`/reserved-name) and Song DSL round-trip preserving the directive.
    files_touched: [tests/ChordFlow.Core.Tests/SongParserTests.cs, tests/ChordFlow.Core.Tests/SongModelTests.cs]
    blocked_by: []
    satisfies: [IN6, IN7, C4]
  - id: expose-defaultfeel-on-the-song-read
    order: 3
    status: done
    description: Surface the selected song's DefaultFeel across the bridge on the same path the Key control already seeds from (play-ui-key-init).
    files_touched: [src/ChordFlow.Core/Bridge/WebMessageRouter.cs]
    blocked_by: []
    satisfies: [IN4]
  - id: seed-the-scorer-feel-control-on
    order: 4
    status: done
    description: Initialize the play-time feel control from the selected song's DefaultFeel, mirroring the Key-control seeding; transport stays the override.
    files_touched: [src/ChordFlow.Desktop/wwwroot/app.js, src/ChordFlow.Desktop/wwwroot/score-render-component.js]
    blocked_by: []
    satisfies: [IN4, IN5, C5, C6]
  - id: ref-doc-sync-dsl-domain-model
    order: 5
    status: done
    description: Document the `feel` Song directive and `Song.DefaultFeel` in the two authoritative refs.
    files_touched: [loom/refs/chordflow-dsl-reference.md, loom/refs/chordflow-domain-model-reference.md]
    blocked_by: []
    satisfies: [IN8]
  - id: verify-end-to-end
    order: 6
    status: done
    description: Author a song with `feel triplet8th`, load it, confirm the control seeds + override works + one whole-song `\tf` renders (and none when straight).
    files_touched: []
    blocked_by: []
    satisfies: [IN4, IN5, C5]
---
# Default triplet feel — Song feel directive + selection seeding

## Goal

Give a Song a default triplet feel via a `feel <token>` DSL directive parsed into a nullable `Song.DefaultFeel`, modeled exactly like the existing `key`/`Song.InitialKey` pair: content on the pure domain record, carried inside the Song `Dsl` string, with the ScoreR transport as its play-time override. Selecting a song seeds the feel control from its default (mirroring the play-ui-key-init Key-control seeding); the user can still override, and rendering is unchanged (one whole-song `\tf` when the effective feel ≠ None). No `CatalogHeader`, `CatalogMetadata`, entity-column, or migration changes; progressions, rhythms, and voicings are untouched. Steps are ordered: step 1 is the domain foundation everything else builds on; steps 2/3/5 depend on it; step 4 depends on step 3; step 6 verifies end-to-end after step 4.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add nullable `DefaultFeel` (TripletFeel?) to the Song record and parse a whole-song `feel <token>` directive in SongParser (peer of `key`). | src/ChordFlow.Core/Music/Songs/Song.cs, src/ChordFlow.Core/Music/Songs/SongParser.cs | — | IN1, IN2, IN3, IN7, C1, C2, C3 |
| ✅ | 2 | Unit-test feel parsing (valid/unknown/absent/`feel none`/reserved-name) and Song DSL round-trip preserving the directive. | tests/ChordFlow.Core.Tests/SongParserTests.cs, tests/ChordFlow.Core.Tests/SongModelTests.cs | — | IN6, IN7, C4 |
| ✅ | 3 | Surface the selected song's DefaultFeel across the bridge on the same path the Key control already seeds from (play-ui-key-init). | src/ChordFlow.Core/Bridge/WebMessageRouter.cs | — | IN4 |
| ✅ | 4 | Initialize the play-time feel control from the selected song's DefaultFeel, mirroring the Key-control seeding; transport stays the override. | src/ChordFlow.Desktop/wwwroot/app.js, src/ChordFlow.Desktop/wwwroot/score-render-component.js | — | IN4, IN5, C5, C6 |
| ✅ | 5 | Document the `feel` Song directive and `Song.DefaultFeel` in the two authoritative refs. | loom/refs/chordflow-dsl-reference.md, loom/refs/chordflow-domain-model-reference.md | — | IN8 |
| ✅ | 6 | Author a song with `feel triplet8th`, load it, confirm the control seeds + override works + one whole-song `\tf` renders (and none when straight). | — | — | IN4, IN5, C5 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:song-defaultfeel-feel-directive-in-songparser -->
### Step 1 — Song.DefaultFeel + feel directive in SongParser

**Song.cs** — add `public TripletFeel? DefaultFeel { get; }` beside `InitialKey`; thread it through the private ctor and the `FromSections` factory (new parameter). `OfProgression` passes `DefaultFeel = null` (a lifted bare progression has no feel — EX1). Pure/immutable, no I/O.

**SongParser.cs** — recognize a `feel <token>` line as a whole-song directive, the space-keyword shape like `key <token>`/`mod <spec>` (NOT `feel:` — the colon is reserved for `NAME: id` references, C3). Map idents `none`→`TripletFeel.None`, `triplet8th`→`Triplet8th`, `triplet16th`→`Triplet16th`; an unknown ident throws `FormatException` (IN2). A second `feel` line throws (one whole-song value). Absent directive → `DefaultFeel = null` (IN7); explicit `feel none` → `TripletFeel.None` (distinct from null). Add `feel` to the reserved-keyword guard in `TrySplitDefinition` (`name is KeyKeyword or ModKeyword or FeelKeyword`) so no part can be named `feel`. C1/C2 hold by construction: `CatalogHeader` is untouched and the realized RhythmPattern/tick grid still stores no feel.

<!-- step:parser-round-trip-tests -->
### Step 2 — Parser + round-trip tests

SongParser tests: each offered ident sets the right `DefaultFeel`; an unknown ident throws `FormatException`; no directive → `null`; `feel none` → `TripletFeel.None` (assert distinct from the null case, IN7); `feel` as a part name throws. Round-trip (C4/IN6): parse a DSL carrying `feel triplet8th`, and if a structural Song→DSL emitter exists, assert `parse → serialize → parse` preserves the feel; otherwise assert the authored DSL (with the `feel` line) round-trips verbatim through the Song store load path — which is what gives packs the field for free (IN6).

<!-- step:expose-defaultfeel-on-the-song-read -->
### Step 3 — Expose DefaultFeel on the song read/DTO path

Follow the existing mechanism by which the play UI learns the song's key to seed the Key control (the play-ui-key-init thread). Add `DefaultFeel` to the song-load / summary envelope so the UI can read it at selection time. Serialize as the alphaTab-style ident (or null when absent). No render-path change — the value only feeds the control seed.

<!-- step:seed-the-scorer-feel-control-on -->
### Step 4 — Seed the ScoreR feel control on song selection

On song selection, set the feel control to the song's `DefaultFeel` (null/no-opinion → straight/None), exactly parallel to how the Key control seeds from the song's key. The transport remains the play-time override (IN5); generation passes whatever the control currently shows. Precedence: user transport choice > song default > None (C6). No change to how `\tf` is emitted (C5).

<!-- step:ref-doc-sync-dsl-domain-model -->
### Step 5 — Ref-doc sync (DSL + domain model)

`chordflow-dsl-reference`: add the `feel <token>` **Song** directive (space keyword alongside `key`; idents `none`/`triplet8th`/`triplet16th`; absent vs explicit `feel none`; note it is Song-only — progressions stay directive-free). `chordflow-domain-model-reference`: add `Song.DefaultFeel` (nullable, peer of `InitialKey`), the selection-seeding flow, and a line that C4 is intact because feel mirrors `key` (content on the Song, never baked into the realized pattern). Mandatory in this same unit of work per the reference-doc sync rule.

<!-- step:verify-end-to-end -->
### Step 6 — Verify end-to-end

Drive the real app: load a Song carrying `feel triplet8th` and confirm (1) the feel control seeds to Triplet8th on selection, (2) the user can override it and re-render, (3) the emitted alphaTex carries a single whole-song `\tf triplet8th` when effective feel ≠ None and no `\tf` when None, and (4) a straight song (no directive / `feel none`) is byte-identical to today's output (EX6 regression check).
