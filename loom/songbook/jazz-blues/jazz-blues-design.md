---
type: design
id: de_01KVV6HNT0ZNET9NYCA3J1BPYE
title: Jazz Blues — First Real Song (Design)
status: done
created: 2026-06-23
updated: 2026-06-23
version: 3
tags: []
parent_id: id_01KVTQXQWTBNAH7GEKF74XF75Y
requires_load: []
---
# Jazz Blues — First Real Song (Design)

> Design for the first deliberate dogfood: author a real jazz blues, drive it through Practice, and **harvest the gap log** real music exposes. Derived from `jazz-blues-idea.md` and the design conversation in `chats/jazz-blues-chat-001.md`.

## Settled decisions

| Decision | Choice | Why |
|---|---|---|
| **Key** | **F** | Most idiomatic jazz-blues key (bebop blues, "Now's the Time"); guitar-playable. |
| **How many rungs this thread** | **2** — basic 12-bar + standard jazz blues | One control that plays clean end-to-end; one that deliberately breaks to harvest gaps. Quick-change variants and bird-blues are deferred corpus. |
| **Voicing difficulty** | Beginner (shell) | `BeginnerShellStrategy` already voices Dom7/m7/maj7 as root+3+7 — our "shell voicings" pillar, already supported. |
| **Feel** | Swing (`Triplet8th`) | Play-time `\tf` — our "swing" pillar, already supported; straight patterns play swung. |

## The two rungs (key F)

### Rung 1 — basic 12-bar (the control)

Reuse the **existing built-in `12bar_blues`** — no new authoring. Its job is to confirm the whole pipeline works in F: swing feel + shell voicings + now/next boards tracking the cursor, before any jazz complexity.

```
Nashville:  17 17 17 17 47 47 17 17 57 47 17 57
in F:       F7 F7 F7 F7 Bb7 Bb7 F7 F7 C7 Bb7 F7 C7
```

### Rung 2 — standard jazz blues (the gap-harvester)

New authored progression. Standard changes:

| Bar | Chord (F) | Function | Nashville token | Expressible today? |
|----:|-----------|----------|-----------------|:--:|
| 1 | F7 | I7 | `17` | ✓ |
| 2 | Bb7 | IV7 | `47` | ✓ |
| 3 | F7 | I7 | `17` | ✓ |
| 4 | F7 | I7 | `17` | ✓ |
| 5 | Bb7 | IV7 | `47` | ✓ |
| 6 | **Bdim7** | **#IVdim7** | **`#4dim7`** | **✗ — the wall** |
| 7 | F7 | I7 | `17` | ✓ |
| 8 | D7 | VI7 (V7/ii) | `67` | ✓ — secondary dominant on a *diatonic* root |
| 9 | Gm7 | ii7 | `2-7` | ✓ |
| 10 | C7 | V7 | `57` | ✓ |
| 11 | F7 D7 | I7 VI7 | `17_67` | ✓ |
| 12 | Gm7 C7 | ii7 V7 (turnaround) | `2-7_57` | ✓ |

Intended DSL (one token is the wall):

```
17 47 17 17 47 #4dim7 17 67 2-7 57 17_67 2-7_57
```

## The gap, by construction

**Finding #1 (predicted, headline):** the Progression DSL degree is a single digit **1–7 only** — no `#`/`b` accidental prefix on a chord root. So **bar 6's `#IVdim7` cannot be parsed.** Note the surface is *narrow*: every other chord in the standard jazz blues — including the **VI7 secondary dominant** (`67`) — is already expressible, because it lands on a diatonic root and only needs a quality suffix. The only chromatic root is `#IV` (and, later, tritone subs `bII7`). The vocabulary even exists elsewhere: the **Song DSL's `mod` already speaks `bIII`/`#`** — just not on chord degrees.

**Downstream finding #2:** dim7 has no shell voicing (`BeginnerShellStrategy` throws on dim7). The CAGED derivation engine *does* derive dim7 (the "behind-1 reach" case), so this is likely a `VoicingBook` wiring check, not new substrate — but it only bites once `#IVdim7` is expressible, so it's gated behind #1.

### Open decision — how to handle the blocked bar 6 (needs confirm)

A token the parser rejects makes the whole bundle un-loadable, which would stop us from harvesting *other* gaps past bar 6.

- **(A, recommended) Stand-in + log.** Author bar 6 as a placeholder that parses (e.g. keep `47`, with a `# TODO #IVdim7` comment), so bars 1–5 and 7–12 still play end-to-end and surface any further gaps. Log `#IVdim7` as Finding #1. Keeps the dogfood moving.
- **(B) Block on the gap.** Spin a "Progression DSL: accidental degree prefixes" thread immediately and pause rung 2 until chromatic degrees land. Cleaner, but stops the harvest at the first wall.

I lean **A** — the point of the dogfood is to find *all* the gaps in one pass, not just the first.

## Bundle shape (additive data drop — no code change)

Authored as a content bundle on the existing importable-pack path (`progressions/`, `rhythms/`, `songs/`):

- **`progressions/jazz_blues.dsl`** — the rung-2 Nashville progression above (key-independent).
- **`rhythms/`** — a swing **comping** pattern. Proposal: **Charleston** — beat 1 + the "and" of 2 — `:2 X..X....` (eighths), played with `Triplet8th` feel for the jazz lilt. Falls back to the built-in `quarters` (Freddie-Green four-to-the-bar) if we want the simplest possible comp first.
- **`songs/jazz_blues_f.dsl`** — a thin Song that pins the key: `key F` + play the head. Captures "in F" in content rather than as a transient play setting, so the bundle is self-contained and playable as a unit.
- **Play params:** beginner difficulty, swing feel, tempo ≈ 130 (medium swing).

## Deliverable

Per the idea: the real output is **(a)** the playable bundle and **(b)** a written **findings log**, each gap tagged *fix-now* vs *spin-a-thread*.

### Outcome (delivered)

**Bundle** (in `default-pack`, verified — 32/32 seed+pack tests green): `progressions/jazz_blues_standard.dsl` (`17 47 17 17 47 47 17 67 2-7 57 17_67 2-7_57`, bar 6 = `47` stand-in for `#IVdim7`) and `songs/jazz_blues_f.dsl` (`key F` · `head: jazz_blues_standard` · `head x2`). The Charleston rhythm was authored, then **removed** — it can't render in v1 (see Finding 3). Plays end-to-end in F with the built-in **`quarters`** comp + swing.

**Findings log** — from the live app play-through:

| # | Observed | Layer | Root cause | Verdict |
|--:|----------|-------|------------|---------|
| 1 | `#IVdim7` (bar 6) unwritable; `47` stand-in | Harmony / DSL | Progression degree is a single digit 1–7 — no `#`/`b` prefix | spin `chromatic-degrees` |
| 2 | (latent) dim7 has no shell voicing | Voicing | shell strategy throws on dim7 (CAGED engine can derive it) | fold into voicing thread |
| 3 | Charleston comp errors | Rhythm / Render | v1 refuses tie/dotted alphaTex tokens → any **syncopated** comp throws | spin `tie-dotted-rendering` |
| 4 | Song loads in **Bb**, not F | App / UI wiring | Key control default (Bb) is sent as `KeyOverride`, overriding `Song.InitialKey` | spin `play-ui-key-init` |
| 5 | Difficulty control is a no-op | Voicing wiring | `VoicingBook.Candidates` ignores `difficulty` ("does not filter in slice 1" — EX6 deferred) | spin `voicing-difficulty-bands` |
| 6 | Full chords, not shells | Voicing wiring | **same cause as 5** — authored full-chord voicings shadow `BeginnerShellStrategy`; difficulty doesn't narrow | same thread as 5 |
| ✅ | Swing (Triplet8th) on quarters | Rhythm | **works as expected** | confirmed, no gap |

### Follow-on threads (agreed priority order)

1. `play-ui-key-init` (Desktop) — Finding 4. Seed the Key control from the loaded song's `InitialKey`. *Quick win.*
2. `tie-dotted-rendering` (alphaTex ties) — Finding 3. Unlocks real syncopated comping (the heart of jazz rhythm).
3. `chromatic-degrees` (Progression DSL `#`/`b`) — Finding 1; unblocks Finding 2's real test. Borrow the Song DSL `mod` accidental vocabulary.
4. `voicing-difficulty-bands` (EX6) — Findings 2, 5, 6. Make Beginner actually select shells.

## North star (recorded, NOT built this thread)

The chat's ambition: an engine that **derives** the jazz-blues form from a basic blues, the way CAGED derives voicings. Recorded here so it's on the record and so this thread's authored rungs are understood as its seed:

- **A "jazz-up" is a pipeline of harmonic transforms** on the existing `IProgressionTransform` seam (pure, key-independent, composable, below the renderer — today only `@take`). Future transforms: `quickChange`, `iiV-ize`, `secondaryDominant(target)`, `diminishedPassing` (#IVdim7), `tritoneSub`, `turnaround(cell)`.
- **The authored ladder of blues forms is the golden corpus.** Just as 36 hand-authored voicings became the golden oracle proving the CAGED engine (36/36), the hand-written rungs (basic → quick-change → +turnaround → jazz → bird) become the oracle proving a future derivation engine: *basic blues + selected concepts → reproduces the authored rung.*
- **Methodology:** author first, derive later. This thread authors; it does not build transforms.

## Open decisions to confirm before authoring

1. **Bar-6 handling** — A (stand-in + log) vs B (block on chromatic-degrees thread). *Recommend A.*
2. **Comping pattern** — Charleston (`X..X....`, characteristic) vs built-in `quarters` (simplest). *Recommend Charleston.*
3. **Song repeats** — head once, or `head x2` (more realistic play-through for watching the boards). *Recommend x2.*