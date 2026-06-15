---
type: design
id: de_01KTWE8B7WKRX7M681PM4P9JFP
title: Exercise definition & UI — Design
status: done
created: "2026-06-11T00:00:00.000Z"
updated: 2026-06-15
version: 9
tags: []
parent_id: id_01KTWE71HWW2GWFHQQ0QMR84K6
requires_load: []
---
# Exercise definition & UI — Design

Design for the **Exercise** keystone and its UI — the integration layer over the
Harmony (`Song`/`Progression`) and Rhythm (`RhythmPattern`) pillars. Builds on
[[chordflow-domain-model-reference]]; decisions settled in
`exercises-definition-ui-chat-001`.

> **Stance** ([[design-philosophy-durable-over-minimal]]): one realization path,
> references over embedding, definition vs params cleanly separated, and the
> Exercise refactor done **now** (no users) rather than carried as legacy.

---

## 1. Domain — `Exercise`

```csharp
public sealed record Exercise(
    // ── Definition (references — what to play) ──
    Song Song,                  // harmony + arrangement (bare progression = single-section song)
    RhythmPattern Comping,      // rhythm-guitar track (required)
    RhythmPattern? Lead,        // lead-guitar track (optional; v1 = dead notes)

    // ── Params (values — saved defaults, user-editable at play) ──
    Key? KeyOverride,           // null → Song.InitialKey; else transpose the whole song
    int Tempo,
    Difficulty Difficulty,
    Feel Feel = Straight);      // + Swing% later
    // TargetNotes — postponed (LeadTargets seam already exists)
```

**Reconciliation (decision (a), `…-chat-002`).** This record **supersedes both**
today's `Exercise(Key, Progression, RhythmPattern, Tempo, Difficulty, Feel)` *and*
the `song` thread's already-shipped `SongExercise(Song, RhythmPattern, Tempo,
Difficulty, Feel)`. `SongExercise` already made the Song-as-harmony pivot; this
refactor **evolves it into the one canonical `Exercise`** (adding `Lead?` +
`KeyOverride?`) and **deletes both** the old `Exercise` *and* the `SongExercise`
name — one play-unit, not three. `AlphaTexRenderer.Render(Exercise, …)` (the old
overload) is rewritten against the new shape; the `Render(RealizedSong, …)` overload
(shipped by `score-render-component`) stays and is the internal target the Exercise
path expands into. Pure/immutable, no I/O (C3).

### `Song.OfProgression` — the trivial lift

```csharp
// in the Song domain (song thread)
static Song OfProgression(Progression p, Key initialKey)
    => Song.FromSections(p.Id, p.Name, initialKey,
                         parts: { ["A"] = new InlineProgression("A", p) },
                         items: [ new PartPlay("A", 1) ]);
```

So a single-progression drill is a one-section Song — **no `Progression` branch
downstream**. The Exercise UI calls this when the user picks a bare progression.

### Key as a transpose override

The Song owns harmonic key (`InitialKey` + relative modulations). `KeyOverride`
re-anchors the **whole** song:

```csharp
var baseKey = exercise.KeyOverride ?? exercise.Song.InitialKey;
int offset  = baseKey.Tonic.Value - exercise.Song.InitialKey.Tonic.Value;   // semitones, mod 12
// SongExpander folds modulations from baseKey instead of InitialKey
var realized = SongExpander.Expand(exercise.Song, store, startKey: baseKey);
```

`SongExpander.Expand` gains an optional `startKey` (defaults to `Song.InitialKey`)
— additive. Mode follows the song; a mode-changing override is a deferred edge.

---

## 2. Realization & rendering pipeline

```
Exercise
  → baseKey = KeyOverride ?? Song.InitialKey
  → SongExpander.Expand(Song, store, startKey: baseKey) → RealizedSong (sections, each keyed)
  → per RealizedSection:
       Comping track: VoicingBook(Difficulty) → chords → AlphaTexRenderer (rhythm staff)
       Lead track   : Lead pattern → dead notes  (or future LeadTargets → pitches)
  → FeelTransform (Feel/Swing) → RhythmQuantizer → AlphaTexRenderer
  → 2-track alphaTex → alphaTab
```

### Two-track alphaTex (the only renderer work owed here)

Most of the original renderer scope **already shipped** in `score-render-component`:
the `Render(RealizedSong, rhythm, tempo, difficulty, feel, RenderOptions)` overload,
chord **names on change** (`{ch}`), chord **diagrams** (`\chord (…)`, on-top /
over-staff), and the `RenderOptions` seam. Track 1 (comping) is therefore **done**.

What remains is **Track 2 (lead) as a second staff** — `AlphaTexRenderer` emits one
track today; it gains a **two-track** mode (rhythm staff + lead staff; alphaTab
supports multiple tracks/staves) where track 2 emits the `Lead` pattern. Per §7.4,
when `Lead` is null the renderer stays **single-track** (no empty lead staff). So §2
is **additive to** the shipped renderer, not a from-scratch build.

### Dead-note lead track (v1)

The `Lead` `RhythmPattern` renders as **dead/muted notes** — confirmed alphaTex
syntax **`x.3`** (dead note on string 3) or **`3.3{x}`** (note with the dead-note
effect). Pitched targets later swap the dead notes for `LeadTargets`-resolved
pitches without changing the track plumbing.

> Render-token status: `{tu N}` (tuplets) and `x.3` (dead notes) are now
> **confirmed**; ties/dotted tokens remain unverified and the renderer still
> throws on them.

---

## 3. Persistence — `ExerciseEntity`

Refactor (clean, now) from `ProgressionId` to references + params:

```csharp
record ExerciseEntity(
    string Id,
    string SongId,              // was ProgressionId
    string CompingPatternId,
    string? LeadPatternId,
    string? KeyOverride,        // key token; null → song key
    int Tempo,
    string Difficulty,          // stored by name
    string Feel,                // stored by name (+ SwingPercent column later)
    DateTime CreatedUtc);
```

- Definition is stored as **references** (`SongId`/`CompingPatternId`/
  `LeadPatternId`) to rows in the `Songs` / `RhythmPatterns` tables; alphaTex is
  never stored (regenerated on load) — consistent with `Progression`/`Song`.
- **Referential integrity:** resolve-time fail-loud if a referenced Song/pattern
  is missing (same rule as Song→Progression refs).
- **EF migration:** drop `ProgressionId`, add the new columns. No data
  preservation needed (no users); seed exercises re-expressed against Songs.
- **Catalog metadata / provenance:** the Exercise *references* content entities
  (`Song` / `RhythmPattern` / `Voicing`) that each carry genre + provenance per
  the `packages` thread — so `ExerciseEntity` adds **no** catalog columns; an
  exercise is filterable through its `Song`. (Voicings are now an authored content
  pillar — see the `domain/voicings` thread.)

---

## 4. UI — delegated to the `ui` weave (not built here)

The three UI layers this thread originally scoped now live in dedicated `ui`
threads; **this thread is Core (domain / persistence / render) only.** Pointers:

- **Layer 1 — Definition / CRUD** (`Progression` / `Song` / `RhythmPattern` /
  `Voicing` authoring — edit a DSL string + name, live preview/parse-error) →
  **`ui/content-crud` — done.**
- **Layers 2–3 — Exercise params + Play/Practice** (Key / Tempo / Difficulty /
  Feel pickers → Generate; the two-track play view; player settings persisted as
  user prefs) → **`ui/exercise-workbench`** (idea only — *consumes* this thread's
  `Exercise`, so it `depends_on` it).
- **alphaTex → alphaTab display + transport** (render toggles, synced cursor,
  metronome, count-in, chord names + the chord-diagram display modes) →
  **`ui/score-render-component` — done** (`ChordFlowScore`); Exercise-Workbench is
  its third consumer.

So the only rendering work owed *here* is the **2-track alphaTex + dead-note lead
staff** (§2). Chord names, chord diagrams (on-top / over-staff), the synced cursor,
and the JS player are **already shipped** by `score-render-component` — §2 must be
read as *additive to* that renderer, not a from-scratch build.

---

## 5. Placement & dependency direction

- **This thread is Core-only.** The merged `Exercise` record, `Song.OfProgression`,
  and the `SongExpander.startKey` param live in **`ChordFlow.Core`** (`Domain/` /
  `Features/`); `ExerciseEntity` + the 2-track-lead renderer change live in Core
  (`Persistence/` / `Rendering/`).
- **UI + JS render/transport are out of this thread** — they live in
  `ui/content-crud` (done), `ui/exercise-workbench`, and `ui/score-render-component`
  (done) per §4. Player config stays a host concern; the engine just emits alphaTex.
- Desktop → Core unchanged.

---

## 6. Explicitly deferred (each additive)

- **Pitched target notes** — `LeadTargets`-driven scale / chord-tones / guide-
  tones / arpeggios; the dead-note lead track is the forward-compatible placeholder.
- **Per-section rhythm**; continuous `SwingPercent` (needs `FeelTransform` to take
  a ratio — enum is the v1 simplification).
- **Multi-track beyond 2** (e.g. bass, percussion guide).

---

## 7. Resolved implementation decisions (`…-chat-002`)

1. **`KeyOverride` representation** — store a **`Key` token** (UI-friendly "practice
   in G"); the semitone offset is derived at expand time. Persisted as the key-token
   column in §3 (`null` → song key).
2. **Mode-changing override** (major song practiced in minor) — **deferred**; v1 is
   **tonic transpose only**. A mode-changing override maps to a modulation later.
3. **Player-prefs scope** — **global user prefs in v1**; and this now lives in
   `ui/exercise-workbench` / `ui/score-render-component`, **not** on the Exercise.
4. **2-track when `Lead` is null** — **single track** (no empty lead staff).

---

## Design conversation

Origin and the locked decisions (Song-only + trivial lift, two fixed tracks,
key-as-transpose, dead-notes-now, player-settings-as-prefs) plus the confirmed
`x.3` / `3.3{x}` dead-note syntax are in
`loom/exercises/exercises-definition-ui/chats/exercises-definition-ui-chat-001.md`.
The **`SongExercise` → `Exercise` merge (decision (a))**, the §7 resolutions, the
§4 UI delegation, and the §2 renderer-scope reconciliation with
`ui/score-render-component` are in `…/chats/exercises-definition-ui-chat-002.md`.

Related: [[chordflow-domain-model-reference]], [[chordflow-architecture-reference]], [[design-philosophy-durable-over-minimal]], the `song` & `rhythm` threads.