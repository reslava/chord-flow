---
type: plan
id: pl_01KX0J0W78NGPRB7TMH8TM1P6S
title: Explicit per-chord voicing references in the DSL
status: done
created: 2026-07-08
updated: 2026-07-08
version: 1
design_version: 5
req_version: 2
tags: []
parent_id: de_01KWZAEXXHM8K1PB5J71G3T800
requires_load: []
target_version: 0.1.0
steps:
  - id: voicing-spec-value-type-shared-parser
    order: 1
    status: done
    description: "Add a `VoicingSpec` value type (a literal grip with an optional `root:<string>[@<fret>]` anchor clause, OR a source-qualified reference `<source>:<id>` with source ∈ u|a|<package>) and a small shared parser + writer. Reuse the existing fret-token parsing; add the `root:` clause (voiced `root:6`, phantom `root:6@8`). Bare grip == `c:` grip; a reference is anything carrying a non-`c` `source:` word."
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingDslParser.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingDslWriter.cs, tests/ChordFlow.Core.Tests/VoicingDslParserTests.cs]
    blocked_by: []
    satisfies: [IN2, IN3, C2, C9]
  - id: progressionparser-annotation-purity-guard
    order: 2
    status: done
    description: "Lex a trailing `{ voicing-spec }` token that binds to the immediately preceding chord (whitespace-optional; `{` can't start a bar/chord) into an optional annotation field on the parsed chord/span. Purity guard: accept annotations only in inline-in-Song parse context; a standalone/stored progression parse rejects them (fail-loud). Errors: orphan `{`, annotation on a stored progression."
    files_touched: [src/ChordFlow.Core/Music/Progressions/ProgressionParser.cs, tests/ChordFlow.Core.Tests/ProgressionParserTests.cs]
    blocked_by: [voicing-spec-value-type-shared-parser]
    satisfies: [IN1, IN7]
  - id: songparser-directive-selectors
    order: 3
    status: done
    description: "Parse `voice <selector> = <voicing-spec>` in the Song definitions section into a selector→spec map on the Song. Selectors: quality-scoped `*<quality>` (`*7`, `*m7`, `*` = major triad) and degree-scoped `[accidental]<degree><quality>` (`17`, `#4dim7`, `2-7`). Errors: duplicate selector ('defines voicing for … more than once'). Coexist with the `feel`/`tempo` space-keywords; canonical minor-7 token `6-7`."
    files_touched: [src/ChordFlow.Core/Music/Songs/SongParser.cs, tests/ChordFlow.Core.Tests/SongParserTests.cs]
    blocked_by: [voicing-spec-value-type-shared-parser]
    satisfies: [IN4, C1, C4, C6, C7]
  - id: thread-annotation-voice-map-through-realization
    order: 4
    status: done
    description: Carry the per-chord annotation onto `RealizedSpan` and the song `voice` map onto `RealizedSong` via `SongExpander`. Ensure each realized span exposes its **scale degree** (for degree-scoped matching) alongside the transposed `Chord`, since the concrete post-transpose chord no longer carries its degree.
    files_touched: [src/ChordFlow.Core/Music/Songs/SongExpander.cs, src/ChordFlow.Core/Music/Songs/RealizedSong.cs, tests/ChordFlow.Core.Tests/SongExpanderTests.cs]
    blocked_by: [progressionparser-annotation-purity-guard, songparser-directive-selectors]
    satisfies: [IN1, IN4, IN5]
  - id: grip-realization-anchoring-voicingrealizer
    order: 5
    status: done
    description: "Realize a grip `VoicingSpec` to a movable `Voicing`: (1) harmony-fixed anchor (per-chord / degree-scoped — root from degree+key), (2) inferred anchor (quality-scoped voiced — spell the shape), (3) declared `root:` / phantom `@fret` for rootless or ambiguous shapes. Canonicalize-to-C and transpose to the sounding root. First-class rootless (`@fret`) support. Error when a muted root lacks `@<fret>` or an ambiguous grip has no `root:`."
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingRealizer.cs, tests/ChordFlow.Core.Tests/VoicingRealizerTests.cs]
    blocked_by: [voicing-spec-value-type-shared-parser]
    satisfies: [IN3, IN11, C3, C9]
  - id: reference-resolution-fail-loud
    order: 6
    status: done
    description: "Add a new `IVoicingReferenceSource` port (Features) that resolves a source-qualified reference to a `Voicing` at the chord's root by id: `u:` = an origin-aware user-row lookup, `<packageId>:` = that pack's row, `a:` = the engine `auto:shell:dom7:E` id (`AutomaticVoicingId`) derived on the fly. Add `VoicingStore.FindBySource(id, source, packageId)` (origin-strict, unlike the tier-collapsing `Find`). Return null on a miss/filtered-out source so the cascade (step 7) can fail loud (IN6). The `a:` id format is the engine's structured `auto:<family>:<quality>:<shape>`."
    files_touched: [src/ChordFlow.Core/Features/Voicings/IVoicingReferenceSource.cs, src/ChordFlow.Core/Features/Voicings/VoicingReferenceSource.cs, src/ChordFlow.Core/Persistence/VoicingStore.cs, tests/ChordFlow.Core.Tests/VoicingReferenceSourceTests.cs]
    blocked_by: [voicing-spec-value-type-shared-parser]
    satisfies: [IN2, IN6]
  - id: compingresolver-override-cascade
    order: 7
    status: done
    description: "In `CompingResolver.Resolve`, before the ranking fill, apply per span the most-specific-wins cascade: per-chord `{}` annotation (steps 5/6) › degree-scoped `voice <deg><qual>` › quality-scoped `voice *<qual>` › existing candidate/ranking fill. Override resolution keys per-span (not the by-`Chord` candidate cache, since two identical chords may differ in annotation). The fill path is unchanged — pure addition in front of it."
    files_touched: [src/ChordFlow.Core/Features/Voicings/CompingResolver.cs, tests/ChordFlow.Core.Tests/CompingResolverTests.cs]
    blocked_by: [thread-annotation-voice-map-through-realization, grip-realization-anchoring-voicingrealizer, reference-resolution-fail-loud]
    satisfies: [IN5, C5, C8]
  - id: round-trip-serialization
    order: 8
    status: done
    description: "Serialize per-chord `{}` annotations and `voice` directives back through the Song/Progression writers so parse → serialize → parse is a fixed point. Bare-grip is the canonical grip output; references serialize as `{source: id}` / `voice sel = source: id`; `root:` clause preserved."
    files_touched: [src/ChordFlow.Core/Music/Songs/SongParser.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingDslWriter.cs, tests/ChordFlow.Core.Tests/SongParserTests.cs]
    blocked_by: [progressionparser-annotation-purity-guard, songparser-directive-selectors]
    satisfies: [IN8]
  - id: update-the-dsl-reference-doc
    order: 9
    status: done
    description: "Document both placements in `chordflow-dsl-reference`: the shared voicing-spec grammar (grip, `root:`/phantom anchor, rootless, references), the per-chord `{}` annotation with the purity rule, the `voice` default with `*` wildcard + degree selectors, the resolution cascade, and the new error messages. Use canonical tokens (`6-7`)."
    files_touched: [loom/refs/chordflow-dsl-reference.md]
    blocked_by: [compingresolver-override-cascade]
    satisfies: [IN9, C7]
  - id: dogfood-annotated-12-bar-blues-on
    order: 10
    status: done
    description: "An annotated 12-bar blues — mixing a per-chord `{}` pin, a `voice *7` default, a `voice #4dim7`, and one rootless grip (`root:…@…`) — renders the pinned grips on the now/next fret-boxes of the fretboard UI page. Visual confirmation each cascade tier resolves to the intended shape (guitar-weave dogfood rule)."
    files_touched: [src/ChordFlow.Desktop/wwwroot]
    blocked_by: [compingresolver-override-cascade]
    satisfies: [IN10, IN11]
---
# Explicit per-chord voicing references in the DSL

## Goal

Implement explicit per-chord voicing references: one **voicing-spec** grammar — a movable literal grip with an optional `root:<string>[@<fret>]` anchor, or a source-qualified reference (`u:`/`a:`/`<package>:`) — usable both as a per-chord `{…}` annotation on an inline-in-Song chord and as a Song-level `voice <selector> = …` default, resolved through a most-specific-wins cascade (per-chord `{}` › degree-scoped `voice` › quality-scoped `voice` › ranking fill). It is a purely additive override layered on the existing `CompingResolver` seam from the (done) [[engine-derived-as-app-source]] thread — the ranking fill is untouched. Progressions stay pure harmony (annotations are honored only inline in a Song), and rootless jazz voicings are first-class via the phantom-root anchor. Builds against the locked req (v2, IN1–IN11 / C1–C9).

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add a `VoicingSpec` value type (a literal grip with an optional `root:<string>[@<fret>]` anchor clause, OR a source-qualified reference `<source>:<id>` with source ∈ u\|a\|<package>) and a small shared parser + writer. Reuse the existing fret-token parsing; add the `root:` clause (voiced `root:6`, phantom `root:6@8`). Bare grip == `c:` grip; a reference is anything carrying a non-`c` `source:` word. | src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingDslParser.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingDslWriter.cs, tests/ChordFlow.Core.Tests/VoicingDslParserTests.cs | — | IN2, IN3, C2, C9 |
| ✅ | 2 | Lex a trailing `{ voicing-spec }` token that binds to the immediately preceding chord (whitespace-optional; `{` can't start a bar/chord) into an optional annotation field on the parsed chord/span. Purity guard: accept annotations only in inline-in-Song parse context; a standalone/stored progression parse rejects them (fail-loud). Errors: orphan `{`, annotation on a stored progression. | src/ChordFlow.Core/Music/Progressions/ProgressionParser.cs, tests/ChordFlow.Core.Tests/ProgressionParserTests.cs | voicing-spec-value-type-shared-parser | IN1, IN7 |
| ✅ | 3 | Parse `voice <selector> = <voicing-spec>` in the Song definitions section into a selector→spec map on the Song. Selectors: quality-scoped `*<quality>` (`*7`, `*m7`, `*` = major triad) and degree-scoped `[accidental]<degree><quality>` (`17`, `#4dim7`, `2-7`). Errors: duplicate selector ('defines voicing for … more than once'). Coexist with the `feel`/`tempo` space-keywords; canonical minor-7 token `6-7`. | src/ChordFlow.Core/Music/Songs/SongParser.cs, tests/ChordFlow.Core.Tests/SongParserTests.cs | voicing-spec-value-type-shared-parser | IN4, C1, C4, C6, C7 |
| ✅ | 4 | Carry the per-chord annotation onto `RealizedSpan` and the song `voice` map onto `RealizedSong` via `SongExpander`. Ensure each realized span exposes its **scale degree** (for degree-scoped matching) alongside the transposed `Chord`, since the concrete post-transpose chord no longer carries its degree. | src/ChordFlow.Core/Music/Songs/SongExpander.cs, src/ChordFlow.Core/Music/Songs/RealizedSong.cs, tests/ChordFlow.Core.Tests/SongExpanderTests.cs | progressionparser-annotation-purity-guard, songparser-directive-selectors | IN1, IN4, IN5 |
| ✅ | 5 | Realize a grip `VoicingSpec` to a movable `Voicing`: (1) harmony-fixed anchor (per-chord / degree-scoped — root from degree+key), (2) inferred anchor (quality-scoped voiced — spell the shape), (3) declared `root:` / phantom `@fret` for rootless or ambiguous shapes. Canonicalize-to-C and transpose to the sounding root. First-class rootless (`@fret`) support. Error when a muted root lacks `@<fret>` or an ambiguous grip has no `root:`. | src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingRealizer.cs, tests/ChordFlow.Core.Tests/VoicingRealizerTests.cs | voicing-spec-value-type-shared-parser | IN3, IN11, C3, C9 |
| ✅ | 6 | Add a new `IVoicingReferenceSource` port (Features) that resolves a source-qualified reference to a `Voicing` at the chord's root by id: `u:` = an origin-aware user-row lookup, `<packageId>:` = that pack's row, `a:` = the engine `auto:shell:dom7:E` id (`AutomaticVoicingId`) derived on the fly. Add `VoicingStore.FindBySource(id, source, packageId)` (origin-strict, unlike the tier-collapsing `Find`). Return null on a miss/filtered-out source so the cascade (step 7) can fail loud (IN6). The `a:` id format is the engine's structured `auto:<family>:<quality>:<shape>`. | src/ChordFlow.Core/Features/Voicings/IVoicingReferenceSource.cs, src/ChordFlow.Core/Features/Voicings/VoicingReferenceSource.cs, src/ChordFlow.Core/Persistence/VoicingStore.cs, tests/ChordFlow.Core.Tests/VoicingReferenceSourceTests.cs | voicing-spec-value-type-shared-parser | IN2, IN6 |
| ✅ | 7 | In `CompingResolver.Resolve`, before the ranking fill, apply per span the most-specific-wins cascade: per-chord `{}` annotation (steps 5/6) › degree-scoped `voice <deg><qual>` › quality-scoped `voice *<qual>` › existing candidate/ranking fill. Override resolution keys per-span (not the by-`Chord` candidate cache, since two identical chords may differ in annotation). The fill path is unchanged — pure addition in front of it. | src/ChordFlow.Core/Features/Voicings/CompingResolver.cs, tests/ChordFlow.Core.Tests/CompingResolverTests.cs | thread-annotation-voice-map-through-realization, grip-realization-anchoring-voicingrealizer, reference-resolution-fail-loud | IN5, C5, C8 |
| ✅ | 8 | Serialize per-chord `{}` annotations and `voice` directives back through the Song/Progression writers so parse → serialize → parse is a fixed point. Bare-grip is the canonical grip output; references serialize as `{source: id}` / `voice sel = source: id`; `root:` clause preserved. | src/ChordFlow.Core/Music/Songs/SongParser.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingDslWriter.cs, tests/ChordFlow.Core.Tests/SongParserTests.cs | progressionparser-annotation-purity-guard, songparser-directive-selectors | IN8 |
| ✅ | 9 | Document both placements in `chordflow-dsl-reference`: the shared voicing-spec grammar (grip, `root:`/phantom anchor, rootless, references), the per-chord `{}` annotation with the purity rule, the `voice` default with `*` wildcard + degree selectors, the resolution cascade, and the new error messages. Use canonical tokens (`6-7`). | loom/refs/chordflow-dsl-reference.md | compingresolver-override-cascade | IN9, C7 |
| ✅ | 10 | An annotated 12-bar blues — mixing a per-chord `{}` pin, a `voice *7` default, a `voice #4dim7`, and one rootless grip (`root:…@…`) — renders the pinned grips on the now/next fret-boxes of the fretboard UI page. Visual confirmation each cascade tier resolves to the intended shape (guitar-weave dogfood rule). | src/ChordFlow.Desktop/wwwroot | compingresolver-override-cascade | IN10, IN11 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:voicing-spec-value-type-shared-parser -->
### Step 1 — Voicing-spec value type + shared parser/writer

One grammar, reused by both placements (steps 2 and 3). The `@<fret>` phantom form is what makes rootless voicings expressible; `root:` is optional (only required for a muted/ambiguous root — the realizer in step 5 enforces that).

<!-- step:grip-realization-anchoring-voicingrealizer -->
### Step 5 — Grip realization & anchoring (VoicingRealizer)

Reuses the Voicing DSL normalize-to-C. Anchor inference (case 2) rides the derivation-engine chord-spelling; the tritone-shell ambiguity is why case 3's explicit `root:` exists.

<!-- step:compingresolver-override-cascade -->
### Step 7 — CompingResolver override cascade

This is the seam from the done [[engine-derived-as-app-source]] thread (C8). The current cache-by-Chord must become override-aware: check the span annotation first, then the song voice-map by degree then quality, then fall through to today's CandidatesFor + ranking.
