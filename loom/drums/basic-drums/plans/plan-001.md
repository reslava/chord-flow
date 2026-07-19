---
type: plan
id: pl_01KXWNVK9355V921VP3P8HPX4E
title: Basic Drums — standalone groove vertical slice
status: implementing
created: 2026-07-19
updated: 2026-07-19
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KXWNJTVWB1TKXS0T5CZHRNRQ
requires_load: []
target_version: 0.1.0
steps:
  - id: drums-domain-in-instruments-drums
    order: 1
    status: done
    description: Add DrumVoice (enum → GM articulation), DrumLane, and DrumGroove (multi-lane over the 48-PPQ tick grid); reuse Music.Rhythm RhythmEvent/TickGrid per lane. Update the domain-model ref.
    files_touched: [src/ChordFlow.Core/Instruments/Drums/DrumVoice.cs, src/ChordFlow.Core/Instruments/Drums/DrumLane.cs, src/ChordFlow.Core/Instruments/Drums/DrumGroove.cs, tests/ChordFlow.Core.Tests/Instruments/Drums/DrumGrooveTests.cs, loom/refs/chordflow-domain-model-reference.md]
    blocked_by: []
    satisfies: [IN1, IN8, C1, C2]
  - id: hit-grid-dsl-parser
    order: 2
    status: done
    description: "DrumGrooveParser: rows=voices, x=hit / .=empty, per-row + per-run :n subdivision, | bars, :3/:6 triplet beats, short-token vocabulary + full-name aliases, fail-loud errors. Update the DSL ref."
    files_touched: [src/ChordFlow.Core/Instruments/Drums/DrumGrooveParser.cs, tests/ChordFlow.Core.Tests/Instruments/Drums/DrumGrooveParserTests.cs, loom/refs/chordflow-dsl-reference.md]
    blocked_by: [drums-domain-in-instruments-drums]
    satisfies: [IN2, IN8, C2, C3, C4, C8]
  - id: groove-alphatex-percussion-render
    order: 3
    status: done
    description: "Render a DrumGroove to an alphaTex percussion track: \\instrument percussion + \\articulation defaults + \\ts/\\tempo, hits as articulation-name notes, simultaneous hits grouped in ( ), rests where silent. Keep it concrete (no IInstrument)."
    files_touched: [src/ChordFlow.Core/Rendering/DrumGrooveRenderer.cs, tests/ChordFlow.Core.Tests/Rendering/DrumGrooveRendererTests.cs]
    blocked_by: [drums-domain-in-instruments-drums, hit-grid-dsl-parser]
    satisfies: [IN3, C4, C6, C7]
  - id: soundfont-percussion-smoke-test
    order: 4
    status: done
    description: Confirm the committed sonivox.sf2 sounds alphaTab percussion articulations on GM channel 10, via the CDP harness (render a groove, hear it).
    files_touched: [tests/ChordFlow.Core.Tests/Rendering/DrumGrooveRendererTests.cs]
    blocked_by: [groove-alphatex-percussion-render]
    satisfies: [IN9]
  - id: drumgroovediagram-drumsr-component
    order: 5
    status: done
    description: Core DrumGrooveDiagram spatial producer (drums twin of FretboardDiagram) + a JS DrumsR dumb-drawer SVG component, animated off the shared playback beat/position bus.
    files_touched: [src/ChordFlow.Core/Instruments/Drums/DrumGrooveDiagram.cs, src/ChordFlow.Desktop/wwwroot/drums-render-component.js, tests/ChordFlow.Core.Tests/Instruments/Drums/DrumGrooveDiagramTests.cs]
    blocked_by: [drums-domain-in-instruments-drums]
    satisfies: [IN4, C1]
  - id: content-drums-dogfood-page
    order: 6
    status: done
    description: "Wire a Drums surface into the Content page: author the hit-grid DSL, preview (score-only style), play, and see DrumsR animate in time."
    files_touched: [src/ChordFlow.Desktop/wwwroot/content-crud.js, src/ChordFlow.Desktop/wwwroot/index.html]
    blocked_by: [hit-grid-dsl-parser, groove-alphatex-percussion-render, drumgroovediagram-drumsr-component]
    satisfies: [IN5, C6]
  - id: drumgroovestore-crud-5th-content-kind
    order: 7
    status: pending
    description: "DrumGrooveEntity + migration + DrumGrooveStore : IContentStore (with genre/subgenre/tags catalog metadata), wired into the bridge entity* CRUD family and the shared editor. Stored form is the hit-grid DSL string only."
    files_touched: [src/ChordFlow.Core/Persistence/DrumGrooveEntity.cs, src/ChordFlow.Core/Persistence/DrumGrooveStore.cs, src/ChordFlow.Core/Persistence/ChordFlowDbContext.cs, src/ChordFlow.Core/Features/ContentCrud.cs, tests/ChordFlow.Core.Tests/Persistence/DrumGrooveStoreTests.cs]
    blocked_by: [hit-grid-dsl-parser]
    satisfies: [IN6, C5]
  - id: default-pack-starter-grooves
    order: 8
    status: pending
    description: Ship rock / blues shuffle / jazz swing / funk grooves as drums/*.dsl in the on-disk default pack, imported through the normal PackReader/PackImporter path.
    files_touched: [Content/default-pack/drums/rock.dsl, Content/default-pack/drums/blues-shuffle.dsl, Content/default-pack/drums/jazz-swing.dsl, Content/default-pack/drums/funk.dsl, Content/default-pack/manifest.json, src/ChordFlow.Core/Features/Packs/PackReader.cs]
    blocked_by: [hit-grid-dsl-parser, drumgroovestore-crud-5th-content-kind]
    satisfies: [IN7]
  - id: architecture-ref-sync-end-to-end
    order: 9
    status: pending
    description: Update the architecture ref (Instruments/Drums, DrumsR, the 5th content kind, the percussion render path) and run the full slice end-to-end (author → store → preview → play → animate).
    files_touched: [loom/refs/chordflow-architecture-reference.md]
    blocked_by: [content-drums-dogfood-page, drumgroovestore-crud-5th-content-kind, default-pack-starter-grooves]
    satisfies: [IN8]
---
# Basic Drums — standalone groove vertical slice

## Goal

Deliver drums as ChordFlow's first-class 2nd instrument via one concrete end-to-end vertical slice: a standalone drum groove authored in a new hit-grid DSL, modelled as a multi-lane rhythm over the existing 48-PPQ tick grid in `Instruments/Drums/`, rendered to an alphaTex percussion track, drawn and animated by a new DrumsR SVG component on a Content › Drums dogfood page, and persisted as a 5th content kind with default-pack starter grooves. The render path stays concrete (no premature `IInstrument`); `Music/` stays instrument-agnostic; reference docs are updated in the same units of work. Deferred work (drums-under-a-song, the `IInstrument` extraction, accent/ghost glyphs) is out of scope and already tracked as its own threads.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add DrumVoice (enum → GM articulation), DrumLane, and DrumGroove (multi-lane over the 48-PPQ tick grid); reuse Music.Rhythm RhythmEvent/TickGrid per lane. Update the domain-model ref. | src/ChordFlow.Core/Instruments/Drums/DrumVoice.cs, src/ChordFlow.Core/Instruments/Drums/DrumLane.cs, src/ChordFlow.Core/Instruments/Drums/DrumGroove.cs, tests/ChordFlow.Core.Tests/Instruments/Drums/DrumGrooveTests.cs, loom/refs/chordflow-domain-model-reference.md | — | IN1, IN8, C1, C2 |
| ✅ | 2 | DrumGrooveParser: rows=voices, x=hit / .=empty, per-row + per-run :n subdivision, \| bars, :3/:6 triplet beats, short-token vocabulary + full-name aliases, fail-loud errors. Update the DSL ref. | src/ChordFlow.Core/Instruments/Drums/DrumGrooveParser.cs, tests/ChordFlow.Core.Tests/Instruments/Drums/DrumGrooveParserTests.cs, loom/refs/chordflow-dsl-reference.md | drums-domain-in-instruments-drums | IN2, IN8, C2, C3, C4, C8 |
| ✅ | 3 | Render a DrumGroove to an alphaTex percussion track: \instrument percussion + \articulation defaults + \ts/\tempo, hits as articulation-name notes, simultaneous hits grouped in ( ), rests where silent. Keep it concrete (no IInstrument). | src/ChordFlow.Core/Rendering/DrumGrooveRenderer.cs, tests/ChordFlow.Core.Tests/Rendering/DrumGrooveRendererTests.cs | drums-domain-in-instruments-drums, hit-grid-dsl-parser | IN3, C4, C6, C7 |
| ✅ | 4 | Confirm the committed sonivox.sf2 sounds alphaTab percussion articulations on GM channel 10, via the CDP harness (render a groove, hear it). | tests/ChordFlow.Core.Tests/Rendering/DrumGrooveRendererTests.cs | groove-alphatex-percussion-render | IN9 |
| ✅ | 5 | Core DrumGrooveDiagram spatial producer (drums twin of FretboardDiagram) + a JS DrumsR dumb-drawer SVG component, animated off the shared playback beat/position bus. | src/ChordFlow.Core/Instruments/Drums/DrumGrooveDiagram.cs, src/ChordFlow.Desktop/wwwroot/drums-render-component.js, tests/ChordFlow.Core.Tests/Instruments/Drums/DrumGrooveDiagramTests.cs | drums-domain-in-instruments-drums | IN4, C1 |
| ✅ | 6 | Wire a Drums surface into the Content page: author the hit-grid DSL, preview (score-only style), play, and see DrumsR animate in time. | src/ChordFlow.Desktop/wwwroot/content-crud.js, src/ChordFlow.Desktop/wwwroot/index.html | hit-grid-dsl-parser, groove-alphatex-percussion-render, drumgroovediagram-drumsr-component | IN5, C6 |
| 🔳 | 7 | DrumGrooveEntity + migration + DrumGrooveStore : IContentStore (with genre/subgenre/tags catalog metadata), wired into the bridge entity* CRUD family and the shared editor. Stored form is the hit-grid DSL string only. | src/ChordFlow.Core/Persistence/DrumGrooveEntity.cs, src/ChordFlow.Core/Persistence/DrumGrooveStore.cs, src/ChordFlow.Core/Persistence/ChordFlowDbContext.cs, src/ChordFlow.Core/Features/ContentCrud.cs, tests/ChordFlow.Core.Tests/Persistence/DrumGrooveStoreTests.cs | hit-grid-dsl-parser | IN6, C5 |
| 🔳 | 8 | Ship rock / blues shuffle / jazz swing / funk grooves as drums/*.dsl in the on-disk default pack, imported through the normal PackReader/PackImporter path. | Content/default-pack/drums/rock.dsl, Content/default-pack/drums/blues-shuffle.dsl, Content/default-pack/drums/jazz-swing.dsl, Content/default-pack/drums/funk.dsl, Content/default-pack/manifest.json, src/ChordFlow.Core/Features/Packs/PackReader.cs | hit-grid-dsl-parser, drumgroovestore-crud-5th-content-kind | IN7 |
| 🔳 | 9 | Update the architecture ref (Instruments/Drums, DrumsR, the 5th content kind, the percussion render path) and run the full slice end-to-end (author → store → preview → play → animate). | loom/refs/chordflow-architecture-reference.md | content-drums-dogfood-page, drumgroovestore-crud-5th-content-kind, default-pack-starter-grooves | IN8 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:drums-domain-in-instruments-drums -->
### Step 1 — Drums domain in Instruments/Drums/

A hit is a one-cell RhythmEvent; a lane is a voice + its events on the shared grid. DrumVoice maps BD→KickHit, SD→SnareHit, HH→HiHatClosed, OH→HiHatOpen, PH→HiHatPedal, RD→RideHit, RB→RideBell, CC→CrashHit, HT→HighTomHit, MT→MidTomHit, FT→LowFloorTomHit. Confirm the Music→Instruments architecture test still passes (drum types are under Instruments/, referencing only Music.Rhythm).

<!-- step:hit-grid-dsl-parser -->
### Step 2 — Hit-grid DSL parser

Single hit glyph only — articulation variety is separate lanes. :3 is a literal triplet figure (shuffle/swing as notation), never a swing flag. 4/4 only. Errors name the bad voice token / run / cell.

<!-- step:groove-alphatex-percussion-render -->
### Step 3 — Groove → alphaTex percussion render

Reuse the quantizer machinery to turn per-lane events into duration slots; merge lanes at each tick into ( ) chord groups. Standalone — no Song/Exercise/key. The renderer remains the only alphaTex-aware code.

<!-- step:soundfont-percussion-smoke-test -->
### Step 4 — Soundfont percussion smoke test

De-risk before building DrumsR on top of playback — the risk is alphaTab's articulation path × this specific committed file, not SF2 drum support in general. If it fails, surface it as a finding (may need a different committed bank).

<!-- step:drumgroovediagram-drumsr-component -->
### Step 5 — DrumGrooveDiagram + DrumsR component

Zero music theory in JS — the diagram model is computed in Core. Reuse the existing beat/position bus that the sheet marker and now/next fretboards already ride, so animation is near-free.

<!-- step:content-drums-dogfood-page -->
### Step 6 — Content › Drums dogfood page

Dogfood-first: fast visual+audible confirmation before phase 2 builds on top. Decide at implementation whether Drums is a new nav view or a kind inside Content (lean: a kind in Content).

<!-- step:drumgroovestore-crud-5th-content-kind -->
### Step 7 — DrumGrooveStore + CRUD (5th content kind)

Mirror RhythmPatternStore but keep catalog metadata (grooves are genre-tagged). alphaTex never stored — regenerated on load.

<!-- step:default-pack-starter-grooves -->
### Step 8 — Default-pack starter grooves

Content is data, not code — the drums kind folder joins the existing pack layout. Reuse the idea's researched grooves.

<!-- step:architecture-ref-sync-end-to-end -->
### Step 9 — Architecture ref sync + end-to-end pass

Closes the reference-doc sync obligation and verifies the whole standalone slice works together before the thread is done.
