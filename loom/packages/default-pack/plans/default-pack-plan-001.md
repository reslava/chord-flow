---
type: plan
id: pl_01KV1NRXEQ3YJWJE6WQDKQE75Z
title: Default pack — author the CAGED voicing content
status: done
created: 2026-06-13
updated: 2026-06-14
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KV1NEPYM2Y7FQ3J7J523Q2BA
requires_load: []
target_version: 0.1.0
actual_release: 0.5.0
steps:
  - id: core-qualities-maj-min-dom7
    order: 1
    status: done
    description: "Author core qualities maj / min / dom7 × CAGED (C·A·G·E·D) into Content/default-pack/voicings/ — complete canonical shapes at the C anchor, {quality}_{shape}shape.dsl files, optional name: header, no genre/tags; fret values checked against a CAGED reference. MVP-critical (covers the shipped blues progressions)."
    files_touched: [src/ChordFlow.Core/Content/default-pack/voicings/]
    blocked_by: []
    satisfies: [IN1, IN2, IN3, C2, C3, C4, C5]
  - id: add-quality-diminished7-dim7-dsl-suffix
    order: 2
    status: done
    description: "Scoped domain addition: add Quality.Diminished7 ({0,3,6,9} = 1 b3 b5 bb7) + QualityIntervals row + dim7/°7 suffix in VoicingDslParser and ProgressionParser + VoicingDslWriter emit; keep the Diminished triad. Ref-sync chordflow-dsl-reference (dim7/°7 rows) and chordflow-domain-model-reference (9th quality). Prerequisite for authoring the dim7 cell."
    files_touched: [src/ChordFlow.Core/Domain/Quality.cs, src/ChordFlow.Core/Domain/QualityIntervals.cs, src/ChordFlow.Core/Domain/ProgressionParser.cs, src/ChordFlow.Core/Domain/Voicings/VoicingDslParser.cs, src/ChordFlow.Core/Domain/Voicings/VoicingDslWriter.cs, loom/refs/]
    blocked_by: []
    satisfies: [IN6]
  - id: extended-qualities-maj7-m7-m7b5-dim
    order: 3
    status: done
    description: Author extended qualities maj7 / m7 / m7b5 / dim7 / aug × CAGED into Content/default-pack/voicings/ — same conventions; m7b5 / dim7 / aug authored only at their playable E(root-6) / A(root-5) / D(root-4) grips, dim7 filled by minor-3rd symmetry; for the symmetric aug/dim7 author only the visibly distinct shapes (Candidates de-dup is a domain/voicings follow-on). Depends on the Quality.Diminished7 addition (step 2).
    files_touched: [src/ChordFlow.Core/Content/default-pack/voicings/]
    blocked_by: [add-quality-diminished7-dim7-dsl-suffix]
    satisfies: [IN2, C3, C4, C5]
  - id: verification-sweep-import-shadow-ref-sync
    order: 4
    status: done
    description: "Verification + close: a Core test that every Content/default-pack/voicings/*.dsl parses and realizes across all 12 roots without throwing (lowest placement fits 0–15); DefaultPack.ImportInto imports them as BuiltIn and a VoicingBook over the stored set returns non-empty Candidates (shadows the shell) for the shipped qualities; a couple of golden-cell assertions; confirm the content rides the existing import path unchanged and that the dim7 ref-sync (chordflow-dsl + chordflow-domain-model) is in place with chordflow-architecture untouched."
    files_touched: [tests/ChordFlow.Core.Tests/]
    blocked_by: [1, 2, 3]
    satisfies: [IN4, IN5, C1]
---
# Default pack — author the CAGED voicing content

## Goal

Author the default pack's voicings/ content — the only piece of the starter bundle that doesn't exist yet (content-catalog Phase 2 already shipped the folder reader, the importer's Voicing arm, and the first-run DefaultPack path). We drop authored .dsl files into Content/default-pack/voicings/ following the C-full matrix (8 qualities × 5 CAGED families, complete canonical shapes authored once at C, real cells only), key-free {quality}_{shape}shape.dsl naming, no catalog metadata. The realizer slides each shape to the 12 roots; the stored-first VoicingBook then shadows the generated shell for the first time in the shipped app. Sequence: author the MVP-critical core qualities first (validates the path end-to-end, covers the shipped blues progressions' dom7/m7), then the extended qualities, then the verification sweep + import-shadow tests and the content-only ref-sync close. Content only — no grammar, domain, or architecture change (C1).

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Author core qualities maj / min / dom7 × CAGED (C·A·G·E·D) into Content/default-pack/voicings/ — complete canonical shapes at the C anchor, {quality}_{shape}shape.dsl files, optional name: header, no genre/tags; fret values checked against a CAGED reference. MVP-critical (covers the shipped blues progressions). | src/ChordFlow.Core/Content/default-pack/voicings/ | — | IN1, IN2, IN3, C2, C3, C4, C5 |
| ✅ | 2 | Scoped domain addition: add Quality.Diminished7 ({0,3,6,9} = 1 b3 b5 bb7) + QualityIntervals row + dim7/°7 suffix in VoicingDslParser and ProgressionParser + VoicingDslWriter emit; keep the Diminished triad. Ref-sync chordflow-dsl-reference (dim7/°7 rows) and chordflow-domain-model-reference (9th quality). Prerequisite for authoring the dim7 cell. | src/ChordFlow.Core/Domain/Quality.cs, src/ChordFlow.Core/Domain/QualityIntervals.cs, src/ChordFlow.Core/Domain/ProgressionParser.cs, src/ChordFlow.Core/Domain/Voicings/VoicingDslParser.cs, src/ChordFlow.Core/Domain/Voicings/VoicingDslWriter.cs, loom/refs/ | — | IN6 |
| ✅ | 3 | Author extended qualities maj7 / m7 / m7b5 / dim7 / aug × CAGED into Content/default-pack/voicings/ — same conventions; m7b5 / dim7 / aug authored only at their playable E(root-6) / A(root-5) / D(root-4) grips, dim7 filled by minor-3rd symmetry; for the symmetric aug/dim7 author only the visibly distinct shapes (Candidates de-dup is a domain/voicings follow-on). Depends on the Quality.Diminished7 addition (step 2). | src/ChordFlow.Core/Content/default-pack/voicings/ | add-quality-diminished7-dim7-dsl-suffix | IN2, C3, C4, C5 |
| ✅ | 4 | Verification + close: a Core test that every Content/default-pack/voicings/*.dsl parses and realizes across all 12 roots without throwing (lowest placement fits 0–15); DefaultPack.ImportInto imports them as BuiltIn and a VoicingBook over the stored set returns non-empty Candidates (shadows the shell) for the shipped qualities; a couple of golden-cell assertions; confirm the content rides the existing import path unchanged and that the dim7 ref-sync (chordflow-dsl + chordflow-domain-model) is in place with chordflow-architecture untouched. | tests/ChordFlow.Core.Tests/ | 1, 2, 3 | IN4, IN5, C1 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
