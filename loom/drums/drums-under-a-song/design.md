---
type: design
id: de_01KXXQ9ECKBQZP1AN8WYM7KRNT
title: Drums under a song — the drum track (phase 2)
status: done
created: 2026-07-19
version: 1
idea_version: 1
tags: []
parent_id: id_01KXWNH0AD17BA9B918QT6HDM6
requires_load: []
---
# Drums under a song — the drum track (phase 2)

Phase 2 of drums: make a `DrumGroove` play **under** a harmonic exercise (drums under a 12-bar blues, not drums alone). The standalone groove domain, hit-grid DSL, percussion renderer, persistence (5th content kind), and the Drums page all landed in `drums/basic-drums`. This thread **consumes** that groove and layers it beneath a progression/song by remodeling the play-unit. Design settled with Rafa in `chat-001`.

## 1. The core problem

A drum groove has no harmony, but today's play-unit requires one:

```csharp
Exercise(Song Song, RhythmPattern Comping, RhythmPattern? Lead,
         Key? KeyOverride, int Tempo, Difficulty Difficulty, TripletFeel TripletFeel)
```

`Comping`/`Lead` are typed fields threaded by name through the whole pipeline (`AlphaTexRenderer`, `CompingResolver`, `ExerciseRefs`, `ExerciseEntity`, the now/next feed, `HarmonyControlsR`). Adding drums touches the play-unit shape — the deliberate `Exercise` remodel MVP chose not to do by accident.

## 2. Resolved questions (chat-001)

1. **Play-unit shape → typed instrument-parts union (option C).** Not a flat `Drums?` field (defers the problem — a 4th instrument re-remodels), not an untyped bag (loses "exactly one comping" + pushes discrimination everywhere). A **typed union** keeps each part's shape honest AND makes a new instrument a new arm, no remodel.
2. **Tiling → cyclic per bar.** `song bar i → groove bar (i mod m)`, exactly as multi-bar rhythm patterns tile. The groove's bar count is independent of the comping pattern's bar count; both tile independently over the song's total bars.
3. **UI → drums audible + drum staff in the score with an optional show/hide toggle.** A Drums picker + volume in `HarmonyControlsR`; the drum staff is a 3rd `\track`. **No DrumsR-in-Practice in v1** (fast-follow, tracked separately).
4. **Feel/swing → one song-level `\tf`, no per-track feel.** Verified live by Rafa: authored-swing grooves (`:3` triplets, e.g. the shuffle pack) and song-level `\tf` compose correctly — no double-swing. No design task.

## 3. The model — a typed instrument-parts union

```csharp
public sealed record Exercise(
    Song Song,
    IReadOnlyList<InstrumentPart> Parts,   // replaces the flat Comping + Lead fields
    Key? KeyOverride, int Tempo, Difficulty Difficulty, TripletFeel TripletFeel = TripletFeel.None);

public abstract record InstrumentPart
{
    public double Volume { get; init; } = 1.0;   // per-part mix
    public bool Muted { get; init; }
}
public sealed record CompingPart(RhythmPattern Pattern) : InstrumentPart;
public sealed record LeadPart(RhythmPattern Pattern)    : InstrumentPart;
public sealed record DrumPart(DrumGroove Groove)        : InstrumentPart;
// future: BassPart(...) — a new arm, no remodel. NOT modeled in v1.
```

**The clean line this draws:** per-part **mix** (`Volume`/`Muted`) lives on the part; the **shared harmonic + time context** (key/tempo/feel/difficulty) stays on `Exercise` — the split the current track-volume UI already implies. `Difficulty` and the voicing window are guitar-comping-scoped in practice but stay `Exercise`-level params for v1 (the UI treats them globally); pushing them onto `CompingPart` is a later refinement, not a v1 need.

**Invariants (validated at construction):** exactly one `CompingPart` is required (harmony must be comped); at most one `LeadPart` and at most one `DrumPart` in v1. Duplicates/roster expansion (two rhythm guitars, bass) are allowed by the list shape but not exercised in v1.

**Roster is a UI concern (agreed).** The "standard band" (Guitar comping/lead, Drums, future Bass) is what `HarmonyControlsR` *offers*; `Exercise` just holds whichever parts were chosen. No `Roster`/`Band` type in the domain.

**Groove is an `Exercise` part, never `Song` metadata (agreed, v1 boundary).** `Song` stays pure harmony/arrangement; the theory/instrument boundary holds. "This song usually uses this groove" is an Exercise saved-default, not a `Song` field. Rafa's north star — Songs/Progressions/Rhythms/Grooves as composable puzzle pieces — is explicitly **out of v1 scope** (tracked for later).

## 4. Pipeline changes — layer by layer

The union lives at the `Exercise`/Features level; the **renderer stays handed the extracted typed pieces** (its per-instrument logic is genuinely different — comping needs the `CompingPlan`, lead needs dead notes, drums needs the tiled groove — so iterating a union inside the renderer buys nothing).

- **`Exercises/Exercise.cs`** — the union above. Add small accessors (`Comping` → the required `CompingPart`, `Lead` → optional, `Drums` → optional) so callers read intent, not list-scanning.
- **`Rendering/AlphaTexRenderer`** — gains a **drum track** (3rd `\track`) when a `DrumPart` is present. Emitted by **composing the existing concrete `DrumGrooveRenderer`** (see §6), tiling the groove cyclically across the `RealizedSong`'s bar count. Comping/lead paths unchanged. Per-part `Volume` → the alphaTab track volume; `Muted` → volume 0 (staff still emitted so the show/hide toggle can reveal it).
- **`Features/ExerciseRendering.RenderCore`** — extracts the parts, resolves the comping grip plan exactly as today, and passes comping + optional lead + **optional drums** to the renderer. One realization pass; the chord schedule + chord-sheet projection are unchanged (drums add no chords).
- **`Features/ExerciseRefs`** — add `ResolveDrumGroove(string? id)` via `DrumGrooveStore.Find` (optional; null/blank ⇒ no drum part). Mirrors `ResolveOptionalPattern`.
- **`Features/GenerateExercise.Build`** — resolve the optional `drumGrooveId` + drum volume into a `DrumPart`, appended to `Parts`.
- **Persistence — see §5.**
- **Bridge/JS — see §7.**
- **Now/next boards, chord schedule, chord sheet** — untouched; drums are harmony-independent.

## 5. Persistence — durable model, pragmatic mapping (Decision)

The **domain** model is the durable parts-union; **persistence stays flat for v1**. `ExerciseEntity` keeps its typed columns and gains a nullable `DrumGrooveId` + per-part volume columns (comping/lead/drums volume, mute flags). A small `Exercise ↔ ExerciseEntity` mapper translates the fixed v1 part set ↔ columns.

**Why not a child `ExercisePartEntity` table now:** a dynamic-roster child table is the right shape *when the roster is dynamic* (bass, multiple guitars). Until then it is migration cost with no payoff, and — crucially — swapping the flat mapper for a child-table mapper later is an **internal, non-breaking** change to the entity: it does not ripple into the domain `Exercise` or the renderer. So the durable model isn't compromised; only the storage mapping is provisional behind a clean seam. This is "start simple" without "ship X, break later" — the thing that would be expensive to redo (the domain shape) is done right now.

EF migration: add `DrumGrooveId` (nullable) + the volume/mute columns to the `Exercises` table.

## 6. Concrete renderer — no wait on `instrument-rendering` (Decision)

The drum track is emitted by **composing the concrete `DrumGrooveRenderer`** directly — the same call `basic-drums` proved (req C7 there kept it concrete; `IInstrument` deferred). **This thread does NOT depend on `chordflow/instrument-rendering`.** When that thread lands the `IInstrument` seam, the drum-track emission migrates onto it as a mechanical follow-up. Coupling v1 to an unbuilt seam would stall drums-under-a-song for no benefit.

Tiling detail: the groove is a 1–2 bar loop; the renderer walks the song's bars and, for song bar `i`, renders groove bar `i mod m`. The groove and comping pattern tile independently. Time signature is 4/4 for v1 (matches the rest of the engine).

## 7. UI — `HarmonyControlsR` picker + staff toggle

- **`HarmonyControlsR`** gains a **Drums picker** (fed by `entityList` with `entity:"drums"`, the same population pattern as comping/lead) + a **volume slider** (binds to the page engine `setTrackVolume`, like the existing comping/lead volumes) + the part enters `getDefinition()`. A blank selection ⇒ no drum part (drums are optional).
- **`generate` verb** carries an optional `drumGrooveId` + `drumVolume` alongside the existing `harmonyEntity`/`harmonyId`/`compingPatternId`/`leadPatternId`/params. `GenerateExercise.Build` resolves it via `ExerciseRefs`.
- **Drum staff show/hide** is a **display-only** toggle (the `staffProfile` sibling): it flips the drum staff's visibility on the already-emitted track via `api.render()`, no C# re-render. Lives on ScoreR's display strip (or `HarmonyControlsR`, TBD in the plan — a small placement call).
- The **saved-exercise load path** resolves the stored `DrumGrooveId` the same way (`ExerciseRefs.ResolveDrumGroove`), so a saved exercise restores its groove.

## 8. Feel/swing

One play-time `\tf` at render, song-level, applied to the whole score including the drum track — unchanged from today. Per Rafa's live verification, authored-swing grooves and `\tf` compose without double-swinging. No code beyond ensuring the drum track renders under the same `\tf` line the other tracks already share.

## 9. Out of scope (v1 boundaries)

- **Bass** — the union leaves room (`BassPart`), but no bass instrument, content kind, or UI in v1.
- **DrumsR-in-Practice** — the animated grid under the song (synced to playback) is a fast-follow; v1 is the percussion **staff** + audio only.
- **Dynamic-roster persistence** (child `ExercisePartEntity` table) — flat columns + mapper until the roster goes dynamic.
- **Composable puzzle-pieces vision** (Songs/Progressions/Rhythms/Grooves as first-class interchangeable pieces, groove-as-Song-default) — Rafa's future direction, explicitly deferred.
- **Per-part difficulty/voicing scoping** — stays `Exercise`-level in v1.

## 10. Decisions log

- **D1 — Typed instrument-parts union** replaces the flat `Comping`/`Lead` fields (option C). Durable: a new instrument is a new arm. Mix on the part, harmonic/time context on `Exercise`.
- **D2 — Flat persistence + mapper now**, child table deferred behind a non-breaking internal seam (§5).
- **D3 — Concrete `DrumGrooveRenderer` composition**; no dependency on `instrument-rendering` (§6).
- **D4 — Drum staff show/hide is a display-only toggle** over the emitted track (no re-render), audio always emitted.
- **D5 — `Song` stays pure**; groove is an Exercise part, not Song metadata. Composable-pieces vision deferred.

## 11. Validation / dogfood

Drums audible under a real 12-bar blues on the Practice page, tiling across the full form, staying in sync with the comp through the shared playback beat/position bus; the drum staff shows/hides via its toggle; a swung song swings comp + drums together with no double-swing; save → reload restores the chosen groove. Verified live via the CDP harness (render an exercise with a `DrumPart`, hear all tracks together) plus the full Core suite green (the `Music → Instruments` architecture test stays green — the union lives in `Exercises/`, drums stay in `Instruments/Drums`, the renderer edge is the allowed `Rendering → Instruments`).