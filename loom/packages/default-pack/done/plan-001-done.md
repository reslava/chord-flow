---
type: done
id: pl_01KV1NRXEQ3YJWJE6WQDKQE75Z-done
title: Done — Default pack — author the CAGED voicing content
status: done
created: "2026-06-14T00:00:00.000Z"
version: 2
tags: []
parent_id: pl_01KV1NRXEQ3YJWJE6WQDKQE75Z
requires_load: []
---
# Done — Default pack — author the CAGED voicing content

## Step 4 — Verification + close: a Core test that every Content/default-pack/voicings/*.dsl parses and realizes across all 12 roots without throwing (lowest placement fits 0–15); DefaultPack.ImportInto imports them as BuiltIn and a VoicingBook over the stored set returns non-empty Candidates (shadows the shell) for the shipped qualities; a couple of golden-cell assertions; confirm the content rides the existing import path unchanged and that the dim7 ref-sync (chordflow-dsl + chordflow-domain-model) is in place with chordflow-architecture untouched.

## Delivery summary — default-pack voicings content + scoped dim7 domain addition

**34 authored voicings** in `src/ChordFlow.Core/Content/default-pack/voicings/`, all pitch-verified at the C anchor, `{quality}_{shape}shape.dsl`, `name:` header, no catalog metadata:
- **maj / min / dom7 / maj7 / m7** × **full CAGED** (C·A·G·E·D), 5 each = 25. C-full means every real CAGED grip including the stretchy ones (G/C-shape minors and 7ths) — they realize to friendly positions in other keys and the per-position playability hint (deferred) handles the partial-chunk usability. `min_gshape = 8 6 5 5 8 8` per Rafa's Zone/Area fix (keep intervals inside the shape's octave zone).
- **m7b5 / dim7 / aug** at the playable E(root-6)/A(root-5)/D(root-4) grips, 3 each = 9 (the only qualities restricted below full CAGED, per Rafa).

**Scoped domain addition (req IN6, plan step 2)** — keeps the `Diminished` triad, adds the symmetric diminished 7th:
- `Quality.Diminished7` + `QualityIntervals` `{0,3,6,9}` (1 b3 b5 bb7).
- `dim7` / `°7` suffix in both `VoicingDslParser` and `ProgressionParser`; `VoicingDslWriter` emits `dim7`.
- `ChordTones.Classify`: interval **9** (bb7) now maps to `Seventh` (was only 10/11) — surfaced by the failing ChordTones theory; the dim7's bb7 is a guide tone.
- Ref-sync (same unit of work): `chordflow-dsl-reference` (dim7/°7 rows, voicing-suffix note), `chordflow-domain-model-reference` (9th quality, Nashville row, ChordToneFunction 9→Seventh). `chordflow-architecture` untouched.

**Verification** — `tests/ChordFlow.Core.Tests/DefaultPackVoicingsTests.cs`:
- matrix-count guard (34); parse + realize sweep across all 12 roots within 0–15; import → `VoicingBook` shadows the BeginnerShell for dom7 & m7; BuiltIn/null-PackId stamping; golden cells (`maj_cshape` open, `dom7_eshape`, `dim7_ashape`); dim7 parses as `Quality.Diminished7`.
- Added `Diminished7` cases to `ChordTonesTests` and `ProgressionParserTests`.

**Result:** `dotnet test` 345/345 green; full solution builds (0 errors; pre-existing WindowsBase/WebView2 warning only). The stored-first `VoicingBook` is now observable in the shipped app for the first time.

**Deferred (own `domain` thread[s], confirmed in chat):** the intervals / octave-shapes / chord-qualities / caged-system derivation engine — these hand-authored voicings are its golden oracle.
