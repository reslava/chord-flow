---
type: design
id: de_01KTWE8B7WKRX7M681PM4P9JFP
title: Exercise definition & UI — Design
status: draft
created: "2026-06-11T00:00:00.000Z"
updated: 2026-06-12
version: 2
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

Replaces today's `Exercise(Key, Progression, RhythmPattern, Tempo, Difficulty,
Feel)`. Pure/immutable, no I/O (C3).

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

### Two-track alphaTex (additive renderer enhancement)

`AlphaTexRenderer` currently emits one track; it gains a **two-track** mode
(rhythm staff + lead staff — alphaTab supports multiple tracks/staves). Track 1
(comping) emits chord groups with chord **names on change** and chord
**diagrams** (`\chord (...)` from the existing `Voicing` diagram metadata). Track
2 (lead) emits the lead pattern.

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

## 4. UI — three layers, three screens

### Layer separation (don't blur)

1. **Definition / CRUD** — `Progression`, `Song`, `RhythmPattern` (Target notes
   postponed). Each screen = edit a **DSL string + name** (uniform, since all
   three are DSL-backed entities) with a live preview/parse-error surface.
2. **Exercise params** — Key / Tempo / Difficulty / Feel(+Swing%); saved as the
   exercise's defaults, editable live.
3. **Player settings** (persisted as **user prefs**, not on the Exercise) —
   count-in, metronome on/off, rhythm-guitar volume, lead-guitar volume → alphaTab
   player config.

### Screens

- **Exercises** (current screen) — pick/edit an exercise's params, then
  **Generate** → alphaTex → render. Player toggles (count-in/metronome/volumes)
  live here as playback controls.
- **CRUD** — `Progression`, `Song`, `RhythmPattern`.
- **Play / Practice** — two tracks: (1) rhythm guitar with chord names on change +
  chord diagrams above; (2) lead guitar when `Lead` is defined. Synced cursor via
  the existing alphaTab playback events.

---

## 5. Placement & dependency direction

- `Exercise`, `Song.OfProgression`, the `SongExpander.startKey` param live in
  **`ChordFlow.Core`** (`Domain/` / `Features/`). `ExerciseEntity` +
  the 2-track renderer change live in Core (`Infrastructure/` / `Rendering/`).
- UI screens + player-settings prefs live in **`ChordFlow.Desktop`** (`wwwroot` +
  the bridge). Player config is a host concern; the engine just emits alphaTex.
- Desktop → Core unchanged.

---

## 6. Explicitly deferred (each additive)

- **Pitched target notes** — `LeadTargets`-driven scale / chord-tones / guide-
  tones / arpeggios; the dead-note lead track is the forward-compatible placeholder.
- **Per-section rhythm**; continuous `SwingPercent` (needs `FeelTransform` to take
  a ratio — enum is the v1 simplification).
- **Multi-track beyond 2** (e.g. bass, percussion guide).

---

## 7. Open implementation questions (non-blocking, decide at plan time)

1. **`KeyOverride` representation** — store a `Key` token vs a signed semitone
   transpose. (Leaning `Key` token: UI-friendly "practice in G"; derive the offset.)
2. **Mode-changing override** (major song practiced in minor) — defer or map to a
   modulation? (Leaning defer — tonic transpose only in v1.)
3. **Player-prefs scope** — global vs per-exercise overrides. (Leaning global
   prefs in v1.)
4. **2-track when `Lead` is null** — single track (no empty lead staff).

---

## Design conversation

Origin and the locked decisions (Song-only + trivial lift, two fixed tracks,
key-as-transpose, dead-notes-now, player-settings-as-prefs) plus the confirmed
`x.3` / `3.3{x}` dead-note syntax are in
`loom/exercises/exercises-definition-ui/chats/exercises-definition-ui-chat-001.md`.

Related: [[chordflow-domain-model-reference]], [[chordflow-architecture-reference]], [[design-philosophy-durable-over-minimal]], the `song` & `rhythm` threads.