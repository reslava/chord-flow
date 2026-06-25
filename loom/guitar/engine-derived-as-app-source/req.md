---
type: req
id: rq_01KVZYDFBP16ZTSWA2BD5FVFM0
title: Engine-derived voicings as the app's source (authored → oracle) — Requirements
status: locked
created: 2026-06-25
updated: 2026-06-25
version: 2
tags: []
parent_id: id_01KVYRP94S3FVD9VAGSH81046N
requires_load: []
---
# Engine-derived voicings as the app's source (authored → oracle) — Requirements

Requirements for [[engine-derived-as-app-source]] — the authoritative include/exclude/constraints spec the plan builds against. Derived from the idea + design (decisions D1–D7, D4=(B)) as converged with Rafa in chat-001/chat-002. **Amended (chat-002, plan-002)** with IN12–IN14 — integration fixes the dogfood surfaced.

### ✅ Included

- `IN1` — A pure `ChordShape → Voicing` adapter (`ChordShapeVoicing.ToVoicing`): non-muted strings → `FretPosition`, muted → `MutedStrings`, `FirstFret` = lowest sounding fret. `Derive` keeps returning `ChordShape`. (Design D1.)
- `IN2` — `EngineVoicingSource : IComputedContentSource` whose `List(Voicing)` yields the **36** pinned quality×shape combos as `automatic` `ContentItem` rows (other kinds empty), wired into `ContentCrudHandler` as the `computed:` source so the Content page lists them. (Design D2.)
- `IN3` — Synthetic identity scheme `auto:{qualityToken}:{shape}` (e.g. `auto:dom7:E`) for the automatic voicing families, reusing the voicing-DSL quality suffixes. (Design D3.)
- `IN4` — A Features-layer `CompingResolver` producing a `CompingPlan` (chord-occurrence → `Voicing`): per chord try the **main source**, else fall back `user > package > automatic` (per-chord, so a song may mix sources). Automatic grips come from `Derive → ChordShape → Voicing`. (Design D4=(B).)
- `IN5` — The renderer becomes a **pure formatter**: `AlphaTexRenderer` drops the `VoicingBook` constructor dependency and takes the `CompingPlan` as a `Render(...)` input; `ExerciseRendering` invokes `CompingResolver` and passes the plan. Tab + the now/next chord schedule draw from the one plan. (Design D4=(B).)
- `IN6` — A structured `VoicingSource { Kind, MinFret?, MaxFret?, PackageId?, Ranking? }` replacing the `VoicingStrategy` enum, carried through the `generate` envelope's `renderOptions.voicing` (transient practice knob, not baked into content). Absent ⇒ `automatic` / full neck / Closest. (Design D5.)
- `IN7` — A ranking seam `IVoicingRanking` and the default **Closest** strategy: first chord = lowest-`FirstFret` grip in the region; next = reuse this chord's earlier grip if it appeared, else minimize the **full per-string fret-distance sum** to the previous grip. Ships Closest only. (Design D6.)
- `IN8` — Relocate the 36 authored `.dsl` grips from `Content/default-pack/voicings` to a **test-only fixture** loaded by `CagedDerivationOracleTests` (keep `.dsl`); the default pack ships zero voicings. (Design D7.)
- `IN9` — A coverage structural test asserting every quality×shape `EngineVoicingSource` offers derives a valid, fully-spelled grip (no throw), pinned to the 36-set. (Design D7.)
- `IN10` — Ref + comment updates: `chordflow-architecture-reference`, `chordflow-domain-model-reference` §2/§5/§6, and fix the stale "34" in `CagedDerivation.cs` + the `CagedDerivationOracleTests` comment.
- `IN11` — Dogfood: engine-derived `automatic` comping renders on the now/next fret-boxes for a 12-bar blues.
- `IN12` — **Pack import reconciles** (plan-002, dogfood fix A): after upserting a pack's definitions, `PackImporter` deletes the `Origin.Pack` rows it owns (same `PackId`) whose id is no longer shipped — so emptying the pack's voicings purges the stale rows on next run (a pack is authoritative for its own content; user copies, forked with fresh ids, are untouched).
- `IN13` — **Computed rows are read-only in the Content view** (plan-002, dogfood fix B): clicking an `automatic` (or `package`) voicing no longer errors — it shows a **derived read-only preview** (the grip diagram) with a **"Duplicate to user"** action that mints an editable `user` copy. The C# side resolves an `auto:` id by deriving its grip (lowest valid placement at canonical C) into a voicing DSL the existing preview/duplicate path consumes.
- `IN14` — **Practice voicing-source control** (plan-002, dogfood fix C): a Practice-page control sets the `VoicingSource` — `MinFret`/`MaxFret` inputs (debugging-friendly) for the `automatic` region — sent on `renderOptions.voicing`. Ranking stays Closest (no mode selector — EX2).

### ❌ Excluded

- `EX1` — The additive listing / source tags / filter UI — delivered by [[content-source-model]] (landed).
- `EX2` — The selectable alternative ranking modes (all-CAGED variety; guide-tone voice-leading) + their selection UI — [[voicing-ranking-strategies]]. This thread ships only the seam + Closest.
- `EX3` — Explicit per-chord voicing references in the DSL (`{u: C6}` / `{a: shell-C6}` / `{c: 8 x 7 9 8 x}`) — [[explicit-voicing-reference]]. This thread only leaves the `CompingResolver` override seam open for it.
- `EX4` — Shell / guide-tone derivation ([[shell-voicing-derivation]]) and 6th derivation ([[caged-sixth-voicings]]).
- `EX5` — Barre-arc derivation in the `ChordShape → Voicing` adapter (`BarreFret` stays null in slice 1).
- `EX6` — Root-causing the `Derive` `ArgumentOutOfRangeException` at extreme placements — its own thread [[caged-derive-anchor-edge]]; this thread only catches + skips it in the resolver (defense-in-depth).

### ⛓ Constraints

- `C1` — The relocation (IN8) and the comping re-wire (IN4/IN5) **land atomically** in one unit — otherwise the comping base empties and the app silently regresses to `BeginnerShellStrategy`.
- `C2` — `Derive` stays **fail-loud** (the existing throws) — never emit a silently-wrong grip.
- `C3` — The engine `automatic` source is **computed, un-persisted** — it never flows through `PackImporter`/SQLite (only `package`/`user` do).
- `C4` — Dependency direction holds: the engine source + resolver live in `Features`/`Instruments`, never in `Music`; Core stays UI-agnostic.
- `C5` — Coverage (IN9) is pinned to **exactly the 36** the oracle enumerates (m7b5/dim7 trim to E/A/D; no C/G), matching the `caged-c-full` rule.