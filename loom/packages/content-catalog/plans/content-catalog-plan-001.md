---
type: plan
id: pl_01KTXKNDWB6S0EV90YGPMA8VMZ
title: Content packages & catalog — model then tooling
status: done
created: 2026-06-12
updated: 2026-06-13
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KTXEQWS29T4T2S0GKP7C23AB
requires_load: []
target_version: 0.1.0
actual_release: 0.5.0
steps:
  - id: catalog-metadata-model
    order: 1
    status: done
    description: "Catalog-metadata DSL header (genre/subgenre/tags) — parse into denormalized entity columns; canonical header stays source of truth and round-trips 1:1, across all four content entities"
    files_touched: [ChordFlow.Core/Persistence/, ChordFlow.Core/Domain/ProgressionParser (header parse), EF migration]
    blocked_by: []
    satisfies: [IN1, C1, C3]
  - id: origin-provenance-model
    order: 2
    status: done
    description: Origin provenance model (BuiltIn / UserDefined / Pack{PackId}) stored as an entity column — discriminator + optional PackId
    files_touched: [ChordFlow.Core/Persistence/, EF migration]
    blocked_by: []
    satisfies: [IN2, C1]
  - id: origin-precedence-resolver
    order: 3
    status: done
    description: Id-keyed Origin resolver with precedence UserDefined > Pack > BuiltIn, shadowing non-destructively (lower tiers remain as fallback)
    files_touched: [ChordFlow.Core/Persistence/]
    blocked_by: [2]
    satisfies: [IN3]
  - id: pack-bundle-manifest-format
    order: 4
    status: done
    description: Pack bundle format — manifest.json (id/name/version/kind/provenance/requires) + per-kind folders of .dsl; manifest kind = coarse pack-type, each definition's kind derives from its folder (mixed packs)
    files_touched: [ChordFlow.Core/Features/]
    blocked_by: [1, 2]
    satisfies: [IN4, C4, C5]
  - id: idempotent-pack-import
    order: 5
    status: done
    description: Idempotent import-by-Id — upsert by definition Id (no duplicates), reusing the seed importer; resolve-time fail-loud when a referenced definition is missing
    files_touched: [ChordFlow.Core/Features/]
    blocked_by: [origin-precedence-resolver, pack-bundle-manifest-format]
    satisfies: [IN5, IN8, C2]
  - id: default-pack-from-generalized-seed
    order: 6
    status: done
    description: Default-pack import path — generalize today's SeedData structure into the first bundle (manifest + per-kind .dsl files) and import it at first run via the same idempotent importer; curated content (incl. authored voicings) is the packages/default-pack thread
    files_touched: [ChordFlow.Core/Features/, default content bundle]
    blocked_by: [idempotent-pack-import]
    satisfies: [IN6]
---
# Content packages & catalog — model then tooling

## Goal

Implement the cross-cutting catalog-metadata + provenance model and the pack bundle/import tooling for ChordFlow's open-core content distribution, against the locked req. Two phases: (1) the catalog/provenance MODEL (design §1–2) — the foundation every content entity adopts, landing before song's persistence work; (2) the packages TOOLING (design §3+) — pack format, idempotent import, and the default pack, which needs the entities to pack and is therefore blocked on Phase 1. Constraints throughout: metadata/Origin live on the Entity layer (`Persistence/`), never on pure `Domain/` records (C1); import is a `Features/` concern with Desktop→Core unchanged (C2); tags persist as JSON `TEXT` round-tripped 1:1 with the canonical DSL header (C3); a pack is data-only, zero engine change (C5). Excluded (EX1–EX4): authoring/export tooling, versioning/signing, the import UI surface, and sell/entitlement/licensing.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Catalog-metadata DSL header (genre/subgenre/tags) — parse into denormalized entity columns; canonical header stays source of truth and round-trips 1:1, across all four content entities | ChordFlow.Core/Persistence/, ChordFlow.Core/Domain/ProgressionParser (header parse), EF migration | — | IN1, C1, C3 |
| ✅ | 2 | Origin provenance model (BuiltIn / UserDefined / Pack{PackId}) stored as an entity column — discriminator + optional PackId | ChordFlow.Core/Persistence/, EF migration | — | IN2, C1 |
| ✅ | 3 | Id-keyed Origin resolver with precedence UserDefined > Pack > BuiltIn, shadowing non-destructively (lower tiers remain as fallback) | ChordFlow.Core/Persistence/ | 2 | IN3 |
| ✅ | 4 | Pack bundle format — manifest.json (id/name/version/kind/provenance/requires) + per-kind folders of .dsl; manifest kind = coarse pack-type, each definition's kind derives from its folder (mixed packs) | ChordFlow.Core/Features/ | 1, 2 | IN4, C4, C5 |
| ✅ | 5 | Idempotent import-by-Id — upsert by definition Id (no duplicates), reusing the seed importer; resolve-time fail-loud when a referenced definition is missing | ChordFlow.Core/Features/ | origin-precedence-resolver, pack-bundle-manifest-format | IN5, IN8, C2 |
| ✅ | 6 | Default-pack import path — generalize today's SeedData structure into the first bundle (manifest + per-kind .dsl files) and import it at first run via the same idempotent importer; curated content (incl. authored voicings) is the packages/default-pack thread | ChordFlow.Core/Features/, default content bundle | idempotent-pack-import | IN6 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
