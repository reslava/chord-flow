---
type: done
id: pl_01KW4G5AP5V1K4P17M0CB03118-done
title: Done — Shell voicing derivation — implementation (v2, 2-form derivation)
status: done
created: 2026-06-27
version: 1
tags: []
parent_id: pl_01KW4G5AP5V1K4P17M0CB03118
requires_load: []
---
# Done — Shell voicing derivation — implementation (v2, 2-form derivation)

## Outcome

Shell voicings shipped as engine-derived `automatic` families. All 10 steps done; 735 tests green; full solution builds; committed to `main` as `00834b9`. The mid-thread pivot (chat-001) reframed shells from a *reduction* to a *2-form derivation with an authored golden oracle* (req v2/v3); the locked req IN1–IN14 / C1–C7 are all satisfied.

## Steps

1. **VoicingFamily + ShellReduction** — `VoicingFamily` enum (`Caged`/`DoubledShell`/`Shell`) + tokens; `ShellReduction.MuteFifth(ChordShape)` mutes the fifth (via `ChordTones`), keeps doublings. (IN1–4)
2. **ShellDerivation** — the 2-form compact shell deriver (C = 5th-string root, E = 6th-string root; guide tones on s4+s3, nearest the root, anchored at the lowest *compact* placement so an open-string root is pushed an octave up). (IN13)
3. **AutomaticVoicingId** — 4-segment `auto:{family}:{token}:{shape}`; old 3-segment form removed. (IN5)
4. **CagedVoicingCatalog** — `(VoicingFamily, Quality, CagedShape)`, 64 combos: caged 46 + doubled-shell 4 (C-form dom7/dim7/6/m6, curated) + shell 14 (C,E × 7th/6th). (IN6)
5. **CompingResolver + RenderOptions** — `VoicingSource.Family` (default caged); per-family dispatch via `FamilyVoicing.Derive`; caged fallback for shell-less chords. (IN7)
6. **EngineVoicingSource** — family-qualified listing rows; common/extended dropped (no consumer). (IN8)
7. **Oracle + coverage** — `ShellOracleTests` (the 12 authored grips), catalog coverage over all 64, doubled-shell structural, open-root regression, Family=caged no-regression. (IN14)
8. **Retire** — deleted `VoicingBook` / `IVoicingStrategy` / `BeginnerShellStrategy`; `GuitarInstrument` kept (Diagram/ResolveLead); old shell logic preserved only as the `ShellGripFixture` test helper for renderer formatting tests. (IN9)
9. **Ref-sync** — `chordflow-domain-model-reference` updated (family pipeline, ShellDerivation/ShellReduction/FamilyVoicing, 64 combos). (IN11)
10. **Dogfood** — CAGED Chords page gains a Family selector that filters Shape + Quality per family. (IN12)

## Dogfood refinements (post-implementation, chat-001)

- **doubled-shell curated** to the C form × {dom7, dim7, 6, m6} (the commonly-played doubled-root voicings); verified the dim7 C-form derives even though caged offers dim7 only on A/E/D.
- **Open-root bug fixed** — `shell · maj7 · C` at root A now anchors the compact grip up an octave (`x 12 11 13`, was the unplayable `x 0 11 1`).

## Notes

- m7♭5's shell equals the min7 grip (its ♭5 is the dropped fifth) — derived, validated structurally, no oracle row.
- Breaking: the 3-segment `auto:` id form was removed (no back-compat shim; nothing persisted it).
- Depends on the shipped `[[engine-derived-as-app-source]]` + `[[caged-sixth-voicings]]`; unblocks `[[voicing-difficulty-bands]]` (Beginner ⇒ the shell family).
