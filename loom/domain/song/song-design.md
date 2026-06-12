---
type: design
id: de_01KTVTNZPYS36K23R5Z9MYDB54
title: Song — Design
status: draft
created: "2026-06-11T00:00:00.000Z"
updated: 2026-06-12
version: 3
tags: []
parent_id: id_01KTVTKHDVQYNXYB33HXH0HXAS
requires_load: []
---
# Song — Design

Design for the **Song** layer: an arrangement of Progressions. Builds on the
`progression` thread (v0.4.0) and the [[chordflow-domain-model-reference]].
Decisions here are settled in `song-chat-001`.

> **Principle inherited:** `Song → arrangement` sits above `Progression → harmony`.
> A Song composes **references**; it never holds bars or chords directly. A new
> `SongExpander` slots in **above** `Transposer`; nothing in `Domain/`,
> `Rendering/`, or the bridge below it changes.

---

## 1. Domain model (`Domain/`, pure + immutable)

```csharp
// A modulation: a tonic shift, optionally flipping mode.
readonly record struct Modulation(int Semitones, bool? ModeChange)
{
    // Absolute reset is modelled as a separate item (see ArrangementItem), not here.
    Key Apply(Key current);   // shift tonic by Semitones; flip IsMinor if ModeChange is set
}

// A named part: either a reference to a stored progression, or an inline one.
abstract record Part(string Name);
record ProgressionReference(string Name, string ProgressionId) : Part(Name);
record InlineProgression(string Name, Progression Progression) : Part(Name);

// One entry in the arrangement stream.
abstract record ArrangementItem;
record PartPlay(string PartName, int Repeat) : ArrangementItem;     // Repeat >= 1
record RelativeMod(Modulation Modulation) : ArrangementItem;        // mod V / mod +2
record AbsoluteKey(Key Key) : ArrangementItem;                      // key G  (reset / escape hatch)

record Song(
    string Id,
    string Name,
    Key InitialKey,
    IReadOnlyDictionary<string, Part> Parts,   // local definitions (A = …, verse: blues)
    IReadOnlyList<ArrangementItem> Items);     // the ordered stream
```

### Guarded factory

`Song.FromSections(id, name, initialKey, parts, items)` — the only constructor,
paralleling `Progression.FromBars`. Validates:

- every `PartPlay.PartName` resolves to a `Part` in `Parts`;
- every `ProgressionReference` is left as a *name to resolve later* (resolution
  against the store happens in `SongExpander`, which has the store; the factory
  only checks the name is non-empty);
- `Repeat >= 1`;
- `Modulation.Semitones` is finite (any int; folded mod 12 at realization);
- at least one `PartPlay` exists.

Throws `FormatException` (parser path) / `ArgumentException` (programmatic path)
naming the offending item — same convention as `Progression.FromBars`.

> **Why `Part` is a dictionary, not a list:** the arrangement stream references
> parts **by name**, and names are reused (`A x2 … A`). A dictionary makes the
> stream a flat list of cheap references and makes "locals shadow stored names"
> a one-line lookup rule.

---

## 2. Realization pipeline — `SongExpander`

```csharp
RealizedSong Expand(Song song, IProgressionStore store);
```

`IProgressionStore` is the lookup seam for **stored** progressions
(`ProgressionReference.ProgressionId` → `Progression`); inline parts skip it. The
existing persistence layer provides the concrete implementation; the domain only
sees the interface (keeps `Domain/` I/O-free, **C3**).

Algorithm — a left-to-right fold carrying a running key:

```csharp
var key = song.InitialKey;
var sections = new List<RealizedSection>();

foreach (var item in song.Items)
{
    switch (item)
    {
        case AbsoluteKey a:  key = a.Key;                  break;  // reset
        case RelativeMod m:  key = m.Modulation.Apply(key); break; // accumulating shift
        case PartPlay p:
            var prog = Resolve(p.PartName, song, store);   // local-first, then store
            for (var i = 0; i < p.Repeat; i++)
                sections.Add(new RealizedSection(
                    Label: p.PartName,
                    Key:   key,
                    Bars:  Transposer.Realize(prog, key)));
            break;
    }
}
return new RealizedSong(sections);
```

Key properties:

- **Modulations accumulate** (`mod V` twice = up two fifths). `AbsoluteKey`
  is how you return home — this is exactly why decision **C** keeps both.
- **`Repeat` expands at realization**, never in the parser — the `Song` stays a
  compact stream; the timeline is derived.
- `RealizedSection.Key` is an **output** of the fold, never an input
  (decision **E**).

### Output types (`Rendering/` adjacent — see §5 on placement)

```csharp
record RealizedSection(string Label, Key Key, IReadOnlyList<RealizedBar> Bars);
record RealizedSong(IReadOnlyList<RealizedSection> Sections);
```

`RealizedBar` is the **existing** type from `Transposer` — reused unchanged. The
renderer already knows how to turn a `RealizedBar` list into alphaTex per bar; a
Song is rendered by walking sections and concatenating, with the `Label` and
per-section `Key` available for section markers / `\ks` changes.

---

## 3. The DSL (`SongParser`, peer of `ProgressionParser`)

`SongParser.Parse(id, name, dsl, ts) → Song`. Pure static, no I/O. Two regions,
order-free for definitions but order-significant for the stream:

```text
key C                  # optional; sets InitialKey (default C major)

A = 17 17 47 17        # inline definition: NAME = <progression-DSL>
B = 2-7 57 1maj7       # RHS is parsed by the existing ProgressionParser verbatim
verse: blues           # reference definition: NAME: <stored-progression-id>

A x2                   # arrangement stream begins
B x2
mod V                  # relative modulation (stream token)
key Eb                 # absolute key reset (stream token)
C x3
```

### Grammar (informal)

| Form | Meaning |
|------|---------|
| `key <token>` (first, definitions region) | `InitialKey` |
| `NAME = <prog-dsl>` | inline `InlineProgression`; RHS → `ProgressionParser.Parse` |
| `NAME: <stored-id>` | `ProgressionReference` to a stored progression |
| `NAME` / `NAME x<n>` (stream) | `PartPlay(NAME, n)`; `n` defaults to 1 |
| `mod <spec>` (stream) | `RelativeMod`; spec ∈ `+n` / `-n` / roman (`V`,`bIII`) / `vi` (mode flip) |
| `key <token>` (stream) | `AbsoluteKey` reset |

### Resolution & namespacing

- A bare `NAME` in the stream resolves **local-first** (the `Parts` dict), then
  the store. Locals **shadow** stored progressions of the same name.
- Unknown name in the stream → `FormatException` naming it.

### Modulation spec parsing

`mod` sugar maps to `Modulation(Semitones, ModeChange)`:

| Token | Semitones | ModeChange |
|-------|-----------|------------|
| `+2` / `-3` | ±n | null |
| `V` | +7 | null |
| `IV` | +5 | null |
| `bIII` | +3 | null |
| `vi` | +9 | `true` (to minor) |

v1 ships `+n`/`-n` and the plain roman degrees; the mode-flip lowercase form can
land with the model already in place.

---

## 4. Persistence (`Infrastructure/`)

Mirror `ProgressionEntity` exactly:

```csharp
record SongEntity(string Id, string Name, string Dsl, Origin Origin,
                  string? Genre, string? Subgenre, string Tags, DateTime CreatedUtc);
```

- `Dsl` (the canonical Song DSL string) is the **only** persisted form;
  `RealizedSong` / alphaTex are **never** stored (regenerated on load).
- `ChordFlowDbContext.Songs` `DbSet`; `Origin` `HasConversion<string>()`.
- **Adopts the catalog-metadata + provenance model from the `packages` thread**:
  `Origin` extends `ProgressionOrigin` (`BuiltIn`/`UserDefined`) with
  `Pack{PackId}`; `Genre`/`Subgenre`/`Tags` are denormalized from the DSL header
  for filtering (the DSL stays canonical). **`Song.OfProgression` inherits** the
  source progression's genre/subgenre/tags — never empty.
- Idempotent first-run seeding of built-in songs by `Id`
  (`SeedBuiltInSongs()`), same pattern as `SeedBuiltInProgressions()`.
- Round-trip on load: row → `SongParser.Parse(Dsl)` → `Song` →
  `SongExpander.Expand` → render.

### Referential integrity

A stored `ProgressionReference` points at a progression `Id`. If that row is
deleted, `SongExpander.Resolve` raises a clear domain error (`reference 'blues'
not found`) — **fail loud, never silently drop a section**. Inline parts are
self-contained in the Song's own `Dsl` and are immune. (A DB-level FK is *not*
added in v1 — built-ins are seeded by slug and user songs may legitimately
inline everything; we enforce at resolution, not at the schema.)

---

## 5. Placement & dependency direction

- `Song`, `Part`, `ArrangementItem`, `Modulation`, `Song.FromSections`,
  `SongParser`, `SongExpander`, `RealizedSong`/`RealizedSection`, and
  `IProgressionStore` all live in **`ChordFlow.Core`** (engine, zero UI/host refs).
- `SongEntity` + `DbContext` wiring + the concrete `IProgressionStore` live in
  `Infrastructure/` (still inside Core).
- Dependency direction unchanged: Desktop → Core. The web/cross-platform host
  stays additive.

---

## 6. The play target — `SongExercise` (decision D)

A Song is pure harmony+arrangement and **cannot be played on its own**. The play
unit is the direct analog of today's `Exercise`:

```csharp
record SongExercise(Song Song, RhythmPattern Rhythm, int Tempo, Difficulty Difficulty, Feel Feel = Straight);
```

Render path: `SongExpander.Expand` → for each `RealizedSection`, run the existing
`Exercise`-style pipeline (`VoicingBook`/`LeadTargets` → `FeelTransform` →
`RhythmQuantizer` → `AlphaTexRenderer`) with the section's `Key`. The renderer
gains a section-aware entry point; the per-bar logic is untouched.

> **Scope note:** the first slice ends at `RealizedSong` + `SongExercise` *model*
> and a single rendered example. Wiring `SongExercise` into the UI / library is a
> follow-up once the harmony layer is proven.

---

## 7. Explicitly deferred

- **Progression transforms** — `domain/transforms` thread. The Song DSL reserves
  the `@op` slot on a `PartPlay` (e.g. `A @transpose(2) x2`) so transforms are an
  additive parser change, not a model change.
- Repeat endings (1st/2nd), D.C./D.S. al coda, coda jumps.
- Per-section time signatures / multi-meter songs (v1 inherits one 4/4 TS).
- Per-section rhythm/feel overrides (decision D keeps these out of the Song).

---

## 8. Open implementation questions (non-blocking, decide at plan time)

1. **Default `InitialKey`** when `key` is omitted — propose C major.
2. **`RealizedSong` namespace** — `Rendering/` (it's a render-feed) vs a new
   `Domain/Song/` (it's pure data). Leaning `Domain/` since it holds no alphaTex.
3. **Section-aware renderer entry point** signature — `Render(RealizedSong, …)`
   vs iterate `RealizedSection` from a Features-layer orchestrator.

---

## Design conversation

Origin and the four locked forks (A/C/D + transforms-as-separate-thread) are in
`loom/domain/song/chats/song-chat-001.md`.

Related: [[chordflow-domain-model-reference]], [[chordflow-architecture-reference]].