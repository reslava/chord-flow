---
type: design
id: de_01KVZVJC1A52Q0GSJGRHXQJZ8B
title: Engine-derived voicings as the app's source (authored → oracle)
status: done
created: 2026-06-25
updated: 2026-06-25
version: 8
idea_version: 10
tags: []
parent_id: id_01KVYRP94S3FVD9VAGSH81046N
requires_load: []
---
# Engine-derived voicings as the app's source (authored → oracle)

Design for the [[engine-derived-as-app-source]] idea. Builds on the **landed** [[content-source-model]] (additive listing, `Pack`/`user` tags, the `IComputedContentSource` union seam). Resolves the idea's three open questions and proposes a concrete architecture for the **two seams** (listing + comping). **Status: decisions locked** — D4 = (B) confirmed by Rafa (chat-002); D5/D6 confirmed; D1/D2/D3/D7 settled. Requirements in `engine-derived-as-app-source-req` (locked).

Refs loaded: `chordflow-architecture-reference` (v52 — §3 Persistence/source model, §5 bridge), `chordflow-domain-model-reference` (v71 — §2 Voicing layer, §5 Rendering, §7 pipeline; §6 Persistence is being corrected as part of this thread's ref work).

---

## 1. Problem

The engine (`CagedDerivation.Derive`) is oracle-verified against 36 authored grips but is **dead weight in the app**: the render path consumes the authored set (`Program.cs:98` → `new AlphaTexRenderer(new VoicingBook(voicingLibrary))`, `voicingLibrary = new VoicingStore(db).LoadShapes()` at `:90`), seeded from `Content/default-pack/voicings/*.dsl`. `Derive`'s output reaches only the CAGED dogfood UI. We ship the oracle, not the engine. This thread makes `Derive` the `automatic` voicing source and demotes the 36 default-pack grips to a test-only oracle.

`content-source-model` already built the **listing** half (it added `IComputedContentSource` and the union point in `ContentCrudHandler.List`). What remains splits in two:

- **Listing seam (small):** implement `IComputedContentSource` so `automatic` voicing rows appear in the catalog.
- **Comping seam (the real work):** make an exercise actually *play* engine grips — the main-source/fallback resolution + ranking, which changes how a grip is picked during render.

## 2. Current state (grounded)

- **Engine output.** `CagedDerivation.Derive(quality, shape, root, minFret, maxFret) → ChordShape`. `ChordShape` (`ChordShape.cs`) carries `IReadOnlyList<ChordShapeString>` (per string: `String`, `Fret?`, `Semitones`; `IsMuted` when `Fret is null`), `Quality`, `Shape`, `AnchorFinger`, `Zone`. Lossless for render + ranking. `Derive` throws on no-anchor (`:36`) / un-spellable (`:101`).
- **Render-time voicing selection lives in the renderer.** `AlphaTexRenderer` is constructed with a `VoicingBook` and calls `VoicingBook.Lookup(chord, difficulty)` per chord during the body pass (domain-ref §7). `Lookup` = top of `Candidates` (exact-quality stored shapes realized to root, ranked lowest-fret then familiarity) else `BeginnerShellStrategy`, else throw. **Stateless per chord** — no previous-chord context.
- **The render knob.** `RenderOptions.Voicing` is a `VoicingStrategy` enum with only `ByDifficulty` live (others fail loud). Carried from the bridge `renderOptions.voicing`.
- **The listing union point.** `ContentCrudHandler.List` does `items.AddRange(_computed.List(kind))` when an `IComputedContentSource` is injected (`:70-73`); none wired today. `ContentItem(Id, Name, Source, PackName, InitialKey)` — a catalog row, **no grip geometry**.
- **The persistence model (post content-source-model).** `Origin = { UserDefined, Pack }` (BuiltIn removed); composite PK `(Id, Origin)`; `OriginResolver` `UserDefined > Pack` used only for single-item reads + the voicing-book load; `List` additive per `(id, source)`, fork-on-edit, `DeleteOutcome = { NotFound, Deleted }`. `DefaultPack.ImportInto` imports the 36 voicing `.dsl` as `Origin.Pack`/`PackId="default"`; `ContentSourceMigration.Run` follows (`Program.cs:84,87`).

## 3. Target architecture

```
LISTING (catalog view — content-source-model seam, fill it)
  EngineVoicingSource : IComputedContentSource
    List(Voicing) → 36 ContentItem rows  id = auto:{quality}:{shape}, source "automatic", name "Dominant 7 — E shape"
    (root-independent catalog entries; no geometry — the page just lists/tags them)
  → unioned by ContentCrudHandler.List (already wired)

COMPING (what actually plays — the new resolution, in FEATURES under D4=(B))
  generate envelope.renderOptions.voicing  →  VoicingSource { kind, minFret?, maxFret?, packageId?, ranking? }
  Features/ExerciseRendering realization:
    CompingResolver.Resolve(realizedChords, mainSource, fallback, ranking) → CompingPlan (chord-occurrence → Voicing)
       per chord: [future: explicit ref override] else main source, else fallback user > package > automatic
       automatic grips: Derive(quality, shape, root, minFret, maxFret) → ChordShape → Voicing (adapter), picked by ranking
  → AlphaTexRenderer.Render(RealizedSong, …, compingPlan) : a PURE FORMATTER — emits tab + chord schedule from the given grips
```

The **type bridge** `ChordShape → Voicing` is the shared primitive feeding both the dogfood diagrams and the comping plan.

## 4. Key decisions

### D1 — `ChordShape → Voicing` adapter (recommended: small static adapter in `Instruments/Guitar/Caged`)
A pure `ChordShapeVoicing.ToVoicing(ChordShape) → Voicing`: non-muted `ChordShapeString`→`FretPosition(String, Fret)`, muted strings → `Voicing.MutedStrings`, `FirstFret = min sounding fret`. **`BarreFret` left null in slice 1** — `ChordShape` doesn't model a barre; the grip still renders/plays correctly, only the diagram's barre arc is absent (deriving barre from `AnchorFinger`+repeated-fret is a later refinement). `Derive` keeps returning `ChordShape`. *Alternative:* a `ToVoicing()` method on `ChordShape` — rejected, it would pull `Voicing` into the `Caged` core type for no gain.

### D2 — `EngineVoicingSource : IComputedContentSource` (the listing impl)
A new small slice (`Features/Voicings/EngineVoicingSource`) whose `List(ContentEntity.Voicing)` → the **36 pinned** quality×shape combos as `automatic` `ContentItem`s; every other kind → empty. Wired in `Program.cs` as the `computed:` arg to `ContentCrudHandler`. Root-independent catalog entries (mirroring canonical-C authored `VoicingShape`s). Purely additive, low-risk.

### D3 — Synthetic identity: `auto:{qualityToken}:{shape}` (recommended)
e.g. `auto:dom7:E`, `auto:maj7:A`, `auto:m7b5:D`. Stable, unique, human-readable; the same string the comping main-source uses to name an automatic family — and the same shape the **future** explicit-ref `{a: …}` would use. The 36-set = exactly the pinned coverage set (m7b5/dim7 trim to E/A/D, matching the oracle + `caged-c-full`). Quality-token vocabulary reuses the voicing-DSL suffixes (`dom7`/`maj7`/`m7`/`m7b5`/`dim7`/`aug`/`maj`/`min`).

### D4 — Comping resolution is a **Features-layer pre-render pass producing a `CompingPlan`** — **RESOLVED: (B)** (Rafa, chat-002)
The default ranking (**Closest**) needs previous-chord context + a "reuse this chord's earlier grip" rule, so a stateless per-chord `VoicingBook.Lookup` cannot express it. Two restructurings (full analysis + table in chat-002):

- **(A) Plan consumed by the renderer.** Resolver sits at the render boundary; renderer consumes a plan but still owns the voicing seam.
- **(B) Resolve in Features (recommended).** A `CompingResolver` runs in the `ExerciseRendering` realization seam — *where references already resolve* — and produces a `CompingPlan` (chord-occurrence → `Voicing`) from main-source → fallback → ranking. `AlphaTexRenderer` takes the plan as an explicit `Render(...)` input and becomes a **pure formatter**: it no longer holds a `VoicingBook` and never chooses a grip. The tab and the now/next chord schedule both draw from the one plan (no drift).

**Why (B):** it gives a *single* place that answers "which grip does this chord get?" — whatever the reason. That directly enables the noted future **explicit per-chord voicing references** (`{u: C6}` / `{a: shell-C6}` / `{swing: C6}`), which are reference-resolution and belong in the same Features seam: per chord, an explicit ref overrides, else ranking fills. (A) would fragment that resolution across two layers. Effort delta over (A) is small (drop `VoicingBook` from the renderer ctor, add a `CompingPlan` param, move the resolver call into `ExerciseRendering`); the architectural payoff (one resolution seam absorbing ranking + fallback + future explicit picks) is the durable end-state.

### D5 — Main-source knob: structured `VoicingSource` — **RESOLVED (Rafa, chat-002)**
Replace the `VoicingStrategy` enum with `VoicingSource { string Kind ("automatic"|"package"|"user"), int? MinFret, int? MaxFret, string? PackageId, string? Ranking }`, carried on the `generate` envelope as `renderOptions.voicing`. Transient practice knob, **not** baked into content. **Absent ⇒ `automatic`, full neck, Closest** (a deliberate behaviour change from today's `ByDifficulty`/authored-shadow default — the engine becomes the base; durable-over-minimal, no back-compat contortion). Fallback chain `user > package > automatic` fixed (not user-facing) for now.

### D6 — Ranking seam + default **Closest** — **distance metric RESOLVED (Rafa, chat-002)**
`IVoicingRanking.Pick(candidates, context) → Voicing` where `context` carries the previous chosen grip + the per-chord history. **Closest:** first chord → lowest-`FirstFret` grip in the region; each next → if this *same chord* already appeared, reuse its grip (muscle memory); else the candidate minimizing **the full per-string fret-distance sum** to the previous grip — `Σ |fretᵢ(prev) − fretᵢ(curr)|` over strings sounding in both grips. *Detail to settle at impl:* how to treat strings sounding in only one grip (skip vs a fixed penalty) — recommend skip in slice 1, revisit if it picks visibly jumpy grips. "Used" tracks **per-Chord → grip**. Ship **only Closest**; variety + voice-leading modes and the selection UI are [[voicing-ranking-strategies]].

### D7 — Relocate the 36 grips + coverage gating (atomic with the comping re-wire)
Move `Content/default-pack/voicings/*.dsl` → a test fixture under `tests/` loaded by `CagedDerivationOracleTests` (keep `.dsl`, same parser). The default pack then ships **zero** voicings; the app's voicing base becomes `automatic`. Add a **coverage structural test**: every quality×shape `EngineVoicingSource` offers `Derive`s a valid, fully-spelled grip (no throw), pinned to the 36-set. Fix the stale "34" in `CagedDerivation.cs:17` + the oracle-test comment. **Ordering constraint:** relocation + comping re-wire land in one unit — else `VoicingBook` empties and the app regresses to `BeginnerShellStrategy`.

## 5. Scope

**In:** D1 adapter · D2 `EngineVoicingSource` (listing) · D3 synthetic ids · D4 `CompingResolver`+`CompingPlan` in Features (B) · D5 `VoicingSource` knob through `generate` · D6 ranking seam + Closest (per-string-sum distance) · D7 relocate + coverage gate · ref updates (arch §3/§5 delta, domain §6 correction + voicing-source delta, the "34" fixes) · dogfood on the now/next fret-boxes.
**Out:** the listing/tag/filter UI ([[content-source-model]], landed) · alternative ranking modes + selection UI ([[voicing-ranking-strategies]]) · shell ([[shell-voicing-derivation]]) / 6th ([[caged-sixth-voicings]]) derivation · barre-arc derivation in the adapter.

## 5a. Noted future direction (NOT this thread) — explicit per-chord voicing references
A Song/Progression DSL annotation naming a voicing per chord — either **source-qualified** (`{u: C6}` user, `{a: shell-C6}` automatic, `{swing: C6}` package *Swing*) or a **fully explicit custom grip with no source** (`{c: 8 x 7 9 8 x}` — a literal fret string, low-E→high-E). E.g. `2m7_V7 I6 {u: C6}` or `2m7_V7 I6 {c: 8 x 7 9 8 x}`, letting a user author multiple explicit voicing versions of one progression. This is **reference (or literal) resolution**, which (B) places in the same Features `CompingResolver`: per chord, an explicit annotation overrides the ranking fill. It touches the DSL grammar (a per-chord `{source: voicing-id}` / `{c: frets}` token), the DSL ref, and the resolver's override path. Now a dedicated thread — [[explicit-voicing-reference]] (spun up chat-002) — so it stays on the roadmap. Captured here because it is the deciding factor for D4 = (B).

## 6. Blast radius (files)

- `Instruments/Guitar/Caged/ChordShapeVoicing.cs` (new) — D1 adapter.
- `Features/Voicings/EngineVoicingSource.cs` (new) — D2, `IComputedContentSource`.
- `Features/Voicings/CompingResolver.cs` + `CompingPlan.cs` (new) — D4 resolution + ranking dispatch, called from `ExerciseRendering`.
- `Instruments/Guitar/Voicings/IVoicingRanking.cs` + `ClosestRanking.cs` (new) — D6.
- `Features/ExerciseRendering.cs` — invoke `CompingResolver`, pass the `CompingPlan` to `Render` (D4-B).
- `Rendering/AlphaTexRenderer.cs` — drop the `VoicingBook` ctor dependency; take `CompingPlan`; format-only (D4-B). `RenderOptions.Voicing` type change (D5).
- `Rendering/RenderOptions.cs` + `Bridge/*` + `wwwroot` (Practice voicing picker) — `VoicingStrategy`→`VoicingSource` (D5).
- `Program.cs` — inject `EngineVoicingSource` as `computed:`; build the `CompingResolver`; the relocation severs the voicing seed, so the renderer no longer wires `VoicingStore.LoadShapes` as the comping base.
- `Content/default-pack/voicings/*` → `tests/.../fixtures/` (D7); `CagedDerivationOracleTests`, `DefaultPackVoicingsTests`, new coverage test.
- `loom/refs/chordflow-architecture-reference.md`, `loom/refs/chordflow-domain-model-reference.md`.

## 7. Decision status

1. **D4 — comping restructure:** **RESOLVED — (B) resolve-in-Features** (Rafa, chat-002).
2. **D5 — absent-knob default:** **RESOLVED** — `automatic`/full-neck/Closest (behaviour change, intended).
3. **D6 — distance metric:** **RESOLVED** — full per-string fret-distance sum.
4. **Side items:** **RESOLVED (do both)** — correct domain-ref §6 now; postfill the content-source-model done-doc.

## 8. Validation

- App comps `automatic` grips end-to-end; the 36-grip pack is unreferenced by the app.
- `CagedDerivationOracleTests` passes against the relocated fixture; the coverage test pins the 36-set, no throw.
- `renderOptions.voicing = { automatic, 5–12, Closest }` changes the comped grips; a chord the source lacks falls back per-chord.
- The Content page lists 36 `automatic` voicing rows alongside package/user.
- The renderer, given a `CompingPlan`, emits tab + chord schedule from the same grips (D4-B: no `VoicingBook` in the renderer).
- **Dogfood:** engine-derived comping renders on the now/next fret-boxes for a 12-bar blues.
