# Changelog

All notable changes to ChordFlow are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project
adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.14.0] — 2026-07-14

This release turns ChordFlow's songs and progressions into **printable chord sheets** — the classic
one-page-of-bars view guitarists actually read from. A new **Chord Sheets** page renders any song or
progression in two idioms — a flowing **leadsheet** (`| bars |`, four to a row, with boxed section tags) or
a **bar-grid** — in any key or the song's own, and exports it to **SVG, PNG, or PDF**. It leans on the whole
engine to *explain* the harmony, not just print it: chords shown as **letter names, Nashville numbers, or
Roman numerals** (with an optional second line showing another at once), an optional per-bar **chord-tone
strip** (note names ⇄ interval degrees) and **fret diagram**, repeated bars collapsed to a **`%` simile**,
and multi-chord bars split by beat. Light/dark on screen, always light on export.

### Added
- **Chord Sheets — print your songs as chord sheets.** A new **Chord Sheets** page turns any song or
  progression into a one-page chord sheet in two layouts — a flowing **leadsheet** (`| bars |` grouped four
  to a row, with boxed section tags) or a **bar-grid** (one box per bar, in bordered section blocks) — and
  renders it in any key or the song's own. Repeated bars collapse to a **`%` simile** and multi-chord bars
  split by beat, so the page reads the way a real chart does.
- **Read the chords your way.** Show each chord as a **letter name** (`Cmaj7`), a **Nashville number**
  (`1maj7`), or a **Roman numeral** (`Imaj7`), and optionally add a **small second line** underneath showing
  a different notation at the same time — the concrete chord and its function, side by side.
- **Optional per-bar detail — the engine explains the harmony.** Turn on a **chord-tone strip** under each
  bar (its spelled notes, toggling to interval degrees, colour-coded by function) and/or a **fret diagram**
  of the comped voicing — so a chord sheet doubles as a what-to-play reference, not just chord names.
- **Export to SVG, PNG, or PDF.** One click each: a crisp vector **SVG**, a **PNG** image, or a **PDF**
  (printed by the app) — always rendered on a clean **light** page regardless of the on-screen theme.
- **`capo <fret>` in the Song DSL.** A song can now record its **capo** (`capo 3`) in its DSL header; the
  chord sheet shows it in the heading ("play these shapes with a capo on the 3rd fret") without transposing
  the written harmony.

## [0.13.0] — 2026-07-09

This is the release where ChordFlow's **voicing engine — its core differentiator — becomes something you
can see, inspect, and steer.** The engine already *derives* every comping voicing from CAGED; now two new
pages open it up. The **faceted Voicings grid** lays out every realized voicing at once as a wall of
fretboard chord-boxes, filterable by root, source, family, and chord tones — the engine's visual oracle.
The **Voicings Engine inspector** goes one level deeper: it shows *how* each grip is derived, operator by
operator, with the full derivation trace beside the resulting shape. And you can now **steer** the engine
straight from the DSL — name the exact voicing a chord should use — plus a **light/dark** fretboard theme,
whole-song **`tempo`** / **`feel`** directives, and a score that owns **key / tempo / feel** live.

### Added
- **Faceted Voicings grid — see every voicing the engine derives.** A new **Voicings** page renders many
  realized voicings at once as a grid of fretboard chord-boxes, over a **faceted toggle-button filter
  stack** — Root, Source, Family, and 3rd / 5th / 7th chord-tone facets. It's the engine's **visual oracle
  and dogfood surface**: the whole derived voicing space, on screen, side by side. Each cell carries a
  title, a copy-to-clipboard id, and its own orientation toggle.
- **Voicings Engine inspector — see *how* each grip is derived.** The chord-derivation logic is reified from
  static classes into an introspectable **operator library** — CAGED, shell, and doubled-shell operators
  behind a registry, each emitting a step-by-step **derivation trace**. A new schema-driven **Voicings
  Engine** page shows the abstract voicing and its derivation steps beside the resulting grip, turning the
  engine from a black box into a glass one. The derived grips are byte-for-byte unchanged.
- **Explicit per-chord voicings in the DSL — steer the engine.** A new voicing-spec grammar names the exact
  voicing a chord should use — a **movable literal grip** (with an optional `root:<string>[@<fret>]` anchor;
  rootless voicings are first-class) or a **reference** (`u:` user, `a:` engine `automatic`, `<pkg>:` a named
  pack). It works in two places: as a per-chord **`{…}` annotation** on an inline-Song chord, and as a Song
  **`voice <selector> = …` default** (a `*<quality>` wildcard or a degree chord symbol). Resolution is a
  **most-specific-wins cascade** — per-chord `{…}` > degree voice > quality voice > the engine's ranked
  fill — and a per-chord pin is keyed per occurrence, so it never leaks to identical chords elsewhere.
  References resolve origin-strict (failing loud on a miss). Ships a default-pack demo song.
- **Song `tempo` and `feel` directives.** A Song can now carry a default **`tempo <bpm>`** and a whole-song
  default **`feel none|triplet8th|triplet16th`** (the peer of `key`) in its DSL header. Both seed play-time
  interpretation without being baked into content — the transport still overrides, and an absent `feel` (no
  swing seed) stays distinct from an explicit `feel none`.

### Changed
- **The score owns key / tempo / feel (seeded + live).** The score component is now the single owner of the
  three render/interpretation params. **Key** and **feel** re-emit a re-render (transpose / `\tf`); **tempo**
  stays a local playback-speed knob. Each seeds per content (a song's own `key`/`tempo`/`feel` defaults, else
  C / 80 / Straight) and, for a saved exercise, from its persisted params (the saved override wins). The Key
  picker moved off the Practice page onto the score, and a live key/feel override is now honored when loading
  a saved exercise. (Also fixes two feel bugs surfaced by the earlier stepping-stone wiring.)
- **Fretboard light/dark theme + display polish.** The shared fretboard component gains a **light/dark
  theme** — it now owns its render surface, so the toggle flips the whole background (light = white surface /
  dark contrast, dark = grey surface / light contrast), with larger fret-number text. Exposed as a per-cell
  and a grid-wide **Dark/Light** toggle, plus orientation toggles on the standalone shape pages.

### Fixed
- **Two-digit fret-position labels no longer clipped.** In the vertical chord-box, an end-anchored
  `10fr`/`12fr` position label was losing its leading digit at the viewBox edge (`10fr` rendered as `0fr`);
  the box's left margin now leaves room for it.

### Tests
- Full `ChordFlow.Core` xUnit suite green (883).

## [0.12.0] — 2026-06-27

The voicing engine becomes the app's living comping source. ChordFlow now derives **every** comping
voicing from its CAGED engine — and shows **every content source side by side** (the default pack, your
own edits, and a new computed `automatic` source) instead of hiding them. The engine also grows
**voicing families** — compact **shell** and **doubled-shell** grips, picked per chord — and **6th
chords** (maj6 / m6) across all five CAGED shapes. Notation gets more accurate too: the **Rhythm DSL**
now describes true notated durations (**dotted notes + ties**, with rhythm winning over harmony),
progressions accept **chromatic (#/b) degrees**, and a **tab / standard / both** staff-display switch
rides the score. Plus a jazz-blues song bundle, monospace DSL editors, and an OffScreen score-follow mode.

### Added
- **Multi-source content model.** Content lists now show every source side by side — the default
  **package**, your **user** edits, and the computed **automatic** source — each carrying a per-source
  badge and filterable by a transient source chip. Editing a package item **forks a user copy** (never a
  silent same-id shadow); the old BuiltIn tier is retired (the default pack is now an ordinary package).
- **Engine-derived automatic comping.** The CAGED derivation engine is now the app's `automatic` voicing
  source: a Features-layer **CompingResolver** builds a comping plan (user > package > automatic fallback,
  closest-ranked) and the renderer becomes a pure formatter consuming it, with a Practice **min/max
  fret-region** control. The default pack ships zero authored voicings — the engine fills them (the
  authored grips live on as a test-only golden oracle).
- **Voicing families — shell & doubled-shell.** Comping can pick a **shell** voicing (compact root + 3rd +
  7th/6th guide-tone grip, 5th omitted) or a **doubled-shell** (chord minus the 5th) instead of the full
  CAGED grip; a **Family** selector on the CAGED Chords page filters shape + quality per family.
- **CAGED 6th chords (maj6 / m6).** Major6 and Minor6 join the derived five-shape CAGED qualities, with an
  E-shape grip tweak (mute string 5 + behind-1 stretch) for the string-5-awkward qualities (m7b5, dim7, 6, m6).
- **Chromatic (#/b) progression degrees.** A Progression-DSL degree can take a single leading `#`/`b`
  (e.g. `#4dim7` → B°7 in F), resolved to the correct pitch and a **letter-pure** spelled chord symbol
  (the written degree spells the root, no enharmonic collapse).
- **Accurate-notation Rhythm DSL — dotted notes & ties.** The Rhythm DSL now notates real durations:
  `.` extends a sounding note, `-` is silence, `_` is a **tied** note (a leading `_` ties across the
  barline). Ties are held — **rhythm wins over harmony**, so a tie over a chord change keeps the previous
  chord — and the renderer emits dotted notes (`{d}`) and ties.
- **Staff-display mode (tab / standard / both).** A three-way switch on the shared score component shows
  tablature, standard notation, or both — a display-only choice over unchanged content, persisted as a
  global preference and available in Practice + Content preview.
- **Jazz-blues song bundle.** The default pack gains a **Standard Jazz Blues** progression and a
  **Jazz Blues in F** song.
- **OffScreen score follow + transport controls.** A new **OffScreen** scroll mode page-flips the score
  instead of creeping per-frame; a transport **Scroll** select (Off / OffScreen / Continuous) and a
  **Now / Next** toggle for the fretboards now sit on the score transport.

### Changed
- **Practice key seeds from the selected song.** Choosing a song seeds the Practice key picker from the
  song's own key (progressions still default to C); a saved exercise's key override still wins.
- **Monospace DSL editors.** Every DSL text input (the content-CRUD editor + the Scales interval input)
  now renders in a ligature-free monospace font so cells and columns line up.
- **Chord-tone function derives from the formula degree.** Chord-tone colouring + legend now read from the
  quality's formula degree, so semitone 9 reads **6** for a 6/m6 chord vs **bb7** for dim7.

### Fixed
- **Open-root comping no longer throws.** `AnchorFinger.Derive` now handles an open root (e.g. open D7),
  unblocking open-position comping.

### Tests
- Full `ChordFlow.Core` xUnit suite green (735).

## [0.11.0] — 2026-06-23

The score comes alive. ChordFlow gains **Now/Next chord fretboards** — two fret-boxes above the
Practice score that show the **current** and **upcoming** chord as real voicings and follow the beat
as it plays, the first slice of a live theory overlay on the staff. The engine emits a **chord
schedule** alongside the tab so the boards always show exactly what's being comped. Plus a **comping
picker** in the Content preview, opt-in **score auto-scroll**, and a fix that revives playback beat
tracking.

### Added
- **Now/Next chord fretboards (Practice).** Two fretboards pinned above the score show the chord
  playing now and the one coming next, as real fretboard voicings, synced to playback. The C# engine
  emits a **chord schedule** (one entry per chord change, each carrying a real-root fretboard diagram
  of the comped voicing) as a by-product of the render pass — so the boards can never drift from the
  tab — and a shared `ChordFlowNowNext` JS module drives them off the score's active-beat signal.
  Built reusable for the Progressions/Songs views and as the foundation for guide-tone / scale
  overlays to come.
- **Comping rhythm picker in the Content preview.** Progression and song previews now pick the
  comping pattern (from the rhythm catalog) instead of a hard-wired default, re-previewing on change.
- **Opt-in score auto-scroll (Practice).** The staff follows the playback cursor (alphaTab `Smooth`
  mode, scrolling its own bounded surface) so the played bar stays in view. *(Known rough edge: the
  glide is continuous and a row can tuck under at line-end — a polish item, not a blocker.)*

### Changed
- **Content preview defaults to the beat 1 + 3 comping pattern** (was quarters) — the app default.
- **Real-root voicing diagrams.** A new `RealizedVoicingDiagram` renders a concrete voicing at its
  actual root; the existing canonical-C `VoicingDiagram` is now its special case (one diagram path,
  no drift). Internal, but it's what lets the Now/Next boards show each chord at its real position.

### Fixed
- **Playback beat tracking was silently dead.** The shared score component read the wrong shape from
  alphaTab's `activeBeatsChanged` event (`.activeBeats.beats`, but `activeBeats` is a `Beat[]`), so the
  active-beat signal never fired. Reading it correctly revives the signal — and is what powers the new
  Now/Next sync.

### Tests
- Full `ChordFlow.Core` xUnit suite green (645).

## [0.10.0] — 2026-06-22

Swing arrives. ChordFlow gains a whole-song **triplet feel** — pick a swing in the score transport
and the notation *reads* and plays swung, delegated to alphaTab's native `\tf` (no more straight-looking
8ths that merely play swung). Plus an editable **alphaTex debug panel** on the shared score component,
true **pickup (anacrusis)** rendering, a **maj7** beginner voicing, and the first **progression
transform** (`@take`). Under the hood the music kernel was renamed `Domain → Music`.

### Added
- **Progression transforms (slice 1) — `@take(n)`.** A Song play-line can now rewrite its progression
  before it's realized: `head @take(4)` drills just the first 4 bars. The `IProgressionTransform` seam
  and the `@op` play-line grammar (composable, in either order with `x<n>`) are in place; `@take` is the
  first transform (`@repeat` stays reserved — `x<n>` already covers it).
- **Editable alphaTex debug panel** on the shared `ChordFlowScore` component — an opt-in, collapsed
  scratchpad showing the tex the staff last rendered, with *Render from alphaTex* (re-renders edits
  locally via `api.tex`, bypassing C#) and *Reload from engine*; dirty-state preserves edits against host
  re-renders. Available on every score page (Practice + Content preview); retires the standalone Debug view.
- **maj7 beginner shell voicing** — the beginner shell strategy now covers Major 7th alongside Dominant 7th
  / Minor 7th, so maj7 standards (e.g. a ii–V–Imaj7) render end-to-end instead of throwing.

### Changed
- **Whole-song triplet feel (swing) via alphaTab `\tf`.** The `Feel` model became **`TripletFeel`**
  (alphaTab's vocabulary — `None` / `Triplet8th` / `Triplet16th` wired, the dotted/Scottish feels
  reserved). Swing is now **delegated to alphaTab's native `\tf` directive** instead of a self-computed
  tick warp, so the score *reads* swung, not just plays swung. The feel control moved out of the Practice
  builder into the **score transport** (and the Content preview), where changing it is a cheap re-render
  — feel stays a play-time choice, never baked into content. (`FeelTransform` is retained, unused, for the
  future MIDI/GuitarPro export seam.)
- **Pickup bars render as a true anacrusis (`\ac`).** A leading `PickupMeasure` now emits `\ac` on both
  the comping and lead tracks, so alphaTab draws a real incomplete pickup instead of an illegal/padded
  full bar. (Known limit: alphaTab still numbers the pickup as bar 1 — alphaTex exposes no directive to
  renumber.)
- **`Domain` kernel renamed to `Music` and split into concept-named flat siblings.** The single
  `ChordFlow.Domain` namespace / `Domain/` folder became `ChordFlow.Music.{Harmony,Rhythm,Melody,
  Progressions,Songs}`, with the non-theory practice types moved to `ChordFlow.Exercises`
  (`Exercise`, `Difficulty`). Pure naming/structure — no behavioral change. The new boundaries are
  now locked in by `NetArchTest` layering tests: `Harmony`/`Rhythm` are sinks and the `Music.*`
  namespaces form an acyclic DAG (`Transposer` moved to `Progressions` so `Harmony` depends on
  nothing). The instrument-boundary test was retargeted from `ChordFlow.Domain` to `ChordFlow.Music`.

### Tests
- Full `ChordFlow.Core` xUnit suite green (635).

## [0.9.0] — 2026-06-21

The CAGED **chord-derivation engine** lands: ChordFlow now *computes* the actual chord grip for any
CAGED shape × quality × root — every combination, far beyond the authored pack — and lights it on
the neck. Plus a proper **end-user guide** bundled into the download, and an app icon.

### Added
- **CAGED Chords page** — pick a CAGED shape (C/A/G/E/D) × quality × root and ChordFlow *derives* the
  chord grip and lights it on the horizontal neck: each fret coloured by chord-tone function, the
  octave-zone band shaded, the anchor finger named. A generator over all 8 × 5 quality × shape
  combinations — it renders grips the authored pack never contained.
- **End-user guide** — a new [`docs/user-guide.md`](docs/user-guide.md) for downloaders: install &
  first run, build & play an exercise, make your own content, soundfonts, and known limits. Linked
  from the README and **bundled into the release zip** as `USERGUIDE.md` (with its screenshots), so
  it travels offline with the download.
- **App icon** — the desktop build now carries the ChordFlow icon on the window and taskbar.

### Changed
- **CAGED derivation engine** — every CAGED grip is now computed from the interval substrates + one
  global hand-reach table (zero authored fret tables): a bass-up greedy stacker with a max-width-4
  cap and a root-on-root-string preference, validated against all 36 authored voicings (frets +
  anchor-finger oracles, 36/36). The default voicing pack was revised to the engine's standard grips
  where it improved them, and the augmented quality gained its full CAGED set.
- **Chord qualities are defined by an authoritative interval formula** — `QualityFormulas` is now the
  single authored source per quality, with the semitone sets derived from it (existing output is
  byte-for-byte unchanged).

### Fixed
- **CAGED Chords placement** — auto-region no longer collapses a grip onto open strings with spurious
  muted notes (it picks the lowest octave whose whole skeleton fits the neck), and the drawn fret
  window always contains the octave-zone band instead of clipping it.

### Tests
- Full `ChordFlow.Core` xUnit suite green (590).

## [0.8.0] — 2026-06-20

The guitar **shape engine** takes shape. ChordFlow gains a fretboard **interval lattice** and the
five CAGED **octave shapes** derived from it — the groundwork for deriving chord/scale shapes from
intervals — surfaced through two new visual pages on a new **horizontal neck** view.

### Added
- **Scales page** — type an interval set (e.g. `1 b3 4 5 b7` minor pentatonic, `1 2 3 5 6` major
  pentatonic) and pick a root; every degree lights up across the whole neck. Each dot keeps your
  typed spelling (a typed `#4` stays `#4`). The dogfood page for the new interval lattice.
- **CAGED Shapes page** — pick a CAGED shape (C/A/G/E/D) + a root and see that shape's **root
  skeleton** (the root and its octaves on the neck), with the **octave zone** highlighted as a
  shaded band. The visual check for the octave-shape engine.
- **Horizontal neck view** — the shared fretboard component can now draw a left-to-right neck
  (frets across, many notes per string) in addition to the vertical chord box; both new pages use it.
- **Interval lattice + CAGED octave shapes (engine)** — `IntervalLattice` projects the interval
  vocabulary onto the fretboard (signed distances + pitch-class / octave labels), and `OctaveShape`
  derives the five CAGED root skeletons from it (anchors / octave zone / string-set boxes) — the
  foundation the interval-derived chord & scale engine is built on.

### Changed
- **Fretboard render component** — added a left-to-right **horizontal** orientation, a reusable
  **zone-band** highlight layer, a palette `*` fallback colour, and per-control visibility flags.
  All additive: existing chord & voicing diagrams render byte-identical.
- **Engine internals** — `Fretboard` now single-sources tuning via an octave-preserving absolute
  coordinate (the pitch-class lookups derive from it); `IntervalSpeller` gained a `Parse`
  (label → semitone) inverse for the Scales input.

### Tests
- Full `ChordFlow.Core` xUnit suite green (564).

## [0.7.0] — 2026-06-19

A foundation release that hardens the **theory ↔ instrument boundary**: the music kernel is
now provably instrument-agnostic, guitar specifics sit behind a `GuitarInstrument` facade, and
interval spelling is centralized in a single authority — groundwork for the interval-derived
shape engine. Plus `.sf3` soundfont support.

### Added
- **`.sf3` soundfont discovery** — the soundfont picker now lists `.sf3` files alongside `.sf2`
  (alphaTab loads the Ogg-compressed `.sf3` variant interchangeably). Drop either format into
  `wwwroot/soundfont/` and it is auto-discovered. The README documents the dual-format support
  and links the MuseScore soundfont list.

### Changed
- **Theory ↔ instrument boundary split.** Guitar-specific types (fretboard geometry,
  `Voicing` / `VoicingBook` / CAGED / strategy realization, the fretboard & voicing diagrams)
  moved out of `Domain/` into `Instruments/Guitar/` (namespace `ChordFlow.Instruments.Guitar`),
  leaving `Domain/` a pure, instrument-agnostic theory kernel. A new **`GuitarInstrument`**
  facade (`Realize` / `Diagram` / `ResolveLead`) is the public surface; `LeadTargets` is trimmed
  to pitch-class output (fret resolution moves to `GuitarInstrument.ResolveLead`). The boundary
  is enforced by a `NetArchTest.Rules` architecture test — `ChordFlow.Domain` must not depend on
  `ChordFlow.Instruments`.
- **`IntervalSpeller` — one interval-spelling authority.** A new `Domain/IntervalSpeller` (the
  interval peer of `NoteSpeller`) centralizes interval naming: `Name(semitone)` is the computed,
  unfolded flats **substrate vocabulary** (the 2nd octave yields `9/10/11/13…` for free);
  `Label(semitone, role)` is the **role-keyed** chord-context spelling with conventional tensions
  (`#9/#11/b13`). `VoicingDiagram` now delegates to it (its inline label logic removed) — diagram
  labels are byte-for-byte unchanged.

### Tests
- Full `ChordFlow.Core` xUnit suite green (454), including the new architecture-boundary test.

## [0.6.0] — 2026-06-18

A reusable **fretboard diagram** rendering layer. Chord/scale shapes are now drawn by a
single dumb SVG component over a Core-computed marker model — the spatial twin of the
shared `ChordFlowScore` notation component — replacing the old one-off chord-diagram view
and laying the groundwork for the interval-derived shape engine.

### Added
- **`ChordFlowFretboard` SVG component** (`fretboard-render-component.js`) — a dumb SVG view
  over a Core-computed `FretboardDiagram` marker model: a flat, many-per-string marker list
  where **color = interval** (default function palette or a per-interval override), **shape =
  layer**, with an owned label toggle + auto legend, open/muted/barre rendering, and an
  auto-fit fret window. The spatial counterpart to `ChordFlowScore`.
- **Core `FretboardDiagram` marker model** — new `Domain/Diagrams/FretboardDiagram`
  (`FretboardDiagram` / `FretboardMarker` / `MarkerShape`); `Function` is a string color-key
  (`root`…`tension`).
- **`fretboard-sandbox.html`** — a hand-fed harness for the new component.

### Changed
- **`VoicingDiagram.Build` recast onto `FretboardDiagram`** as its first producer; the old
  `DiagramModel` / `DiagramString` parallel path is removed and
  `EntityPreviewEnvelope.Diagram` retyped. The Content/Voicings preview is retrofitted onto
  the new component and the old `chord-diagram.js` is deleted (no drifting second path).

### Tests
- Full `ChordFlow.Core` xUnit suite green (399/399).

## [0.5.0] — 2026-06-17

ChordFlow grows from a hardcoded 12-bar-blues demo into a **content-driven trainer**:
progressions, songs, rhythms, and voicings are all authored in compact text DSLs,
distributed as importable **content packs**, and assembled into exercises through an
on-screen **workbench** — over a rebuilt rendering/UI layer. Plus a one-command,
tag-driven **release pipeline** that ships a downloadable Windows build.

### Added
- **Song arrangement layer** — a pure arrangement of progressions (repetition, modulation,
  section order) via `SongExpander`, a line-oriented **Song DSL**, and `Render(RealizedSong)`.
  Harmony stays in the progression; the song layer slots in above `Transposer`.
- **Rhythm DSL** — multi-bar rhythm patterns in an `X/./-` glyph DSL with `:n` subdivisions,
  pickup measures, and triplet rendering (`{tu N}`), plus rhythm-pattern persistence and
  DSL-derived (single-source-of-truth) seed patterns.
- **Authored voicing content pillar** — canonical-C, inherently-movable voicings in a
  **Voicing DSL** with CAGED-shape ranking, a stored-first `VoicingBook`, realize/transpose,
  and persistence; a curated **default pack of 34 pitch-verified CAGED voicings**
  (maj/min/dom7/maj7/m7 × full CAGED + m7b5/dim7/aug grips). New `Quality.Diminished7`.
- **Content packs (open-core)** — data-only bundles (`manifest.json` + per-kind `.dsl`
  folders) imported idempotently by a composite `(Id, Origin)` key with non-destructive
  shadowing (UserDefined > Pack > BuiltIn); the built-in starter content now ships as the
  default pack. Catalog metadata (genre/subgenre/tags) + `Origin` provenance.
- **Exercise workbench** — generate over the canonical `Exercise` via content references:
  harmony (song/progression) + comping + optional lead + params (key/tempo/difficulty/feel),
  resolved through a shared `ExerciseRefs` seam. Harmony/comping/lead pickers, difficulty/feel
  controls, and per-track volume sliders.
- **Shared score render component + content-CRUD surface** — one `ChordFlowScore` JS
  component owns the alphaTex → alphaTab render/transport (replacing two drifted instances);
  a generic DSL-entity CRUD editor (`entity*` bridge family) with voicing fret-box diagrams;
  a Practice ⇄ Content view toggle.
- **Chord-diagram display toggles** — independent chord names, diagrams-over-staff, and
  diagrams-on-top.
- **alphaTex inspector (Debug view)** — show/edit the engine's emitted alphaTex and
  render/play it through its own player.
- **User-selectable soundfont library** — pick the playback soundfont (auto-discovered from
  `wwwroot/soundfont`), a global persisted choice that switches the synth font live; backed
  by a new Core `AppSettings` key/value store.
- **Release pipeline** — a tag-driven GitHub Actions release (`guard → build-test → release`)
  that publishes a self-contained, single-file **`ChordFlow.exe` + `wwwroot`** zip and cuts a
  GitHub release with the changelog as notes; driven by a `/do-release` command and
  [`RELEASING.md`](RELEASING.md).

### Changed
- **One canonical `Exercise`** — merged `Exercise`/`SongExercise` into a single
  `Exercise(Song, Comping, Lead?, KeyOverride?, …)`; a bare progression is lifted via
  `Song.OfProgression` so everything rides one Song → render path. The renderer stays pure
  (Song expansion moved to a `Features` I/O seam). An optional lead renders as a second track
  of dead notes.
- **Shipped executable renamed to `ChordFlow.exe`** (`<AssemblyName>`); the default Sonivox
  GM soundfont is now **committed/bundled** instead of fetched at build time, so builds are
  hermetic.
- Two-track exercises render both staves; bars-per-row layout is controllable (4/row default
  + an Auto-layout toggle).

### Fixed
- The last partial system now stretches to full width in fixed 4-bar layout
  (`justifyLastSystem`); previously only natural in Auto layout.
- A render failure path and the saved-exercise load path (previously hard-wired to the seed
  blues) now resolve through the shared reference seam like the generate path.

### Tests
- Full `ChordFlow.Core` xUnit suite green (verified in CI on every tagged release).

## [0.4.0] — 2026-06-10

Harmonic rhythm + a clean engine/host split. Progressions gain multiple chords per
bar via a key-independent text DSL, and the single project is split into a
host-agnostic engine and a thin desktop host so the "engine stays UI-agnostic" rule
is enforced by the compiler.

### Added
- **Harmonic-rhythm layer — multi-chord-per-bar progressions.** `HarmonicBar` /
  `ChordSpan` let a single bar hold several chords, each with its own tick duration.
- **Progression DSL** — a Nashville-style, key-independent notation parsed by
  `ProgressionParser`: bars separated by spaces, chords within a bar by `_`, scale
  degrees `1`–`7` with quality suffixes (`-`/`m`, `7`, `-7`/`m7`, `maj7`/`^7`,
  `°`/`dim`, `ø`/`m7b5`, `+`/`aug`), and per-chord durations via even split or
  explicit `:slots`. End-user guide:
  [`chordflow-dsl-reference.md`](loom/refs/chordflow-dsl-reference.md).
- **Documentation** — a public [Progression DSL guide](loom/refs/chordflow-dsl-reference.md)
  (linked from the README) and an [architecture overview](loom/refs/chordflow-architecture-reference.md),
  both as `loom/refs/` reference docs.

### Changed
- **Project split — `ChordFlow.App` → `ChordFlow.Core` + `ChordFlow.Desktop`.** The
  host-agnostic engine (Domain, Rendering, Features, the `Bridge/` contract, and
  `Persistence/`) moved to `ChordFlow.Core` (`net10.0`, **zero UI references**); the
  WinForms + WebView2 host moved to `ChordFlow.Desktop` (`net10.0-windows`).
  Dependency direction is strictly Desktop → Core, so the UI-agnostic-engine rule is
  now a compile-time guarantee and a future web host is additive rather than a
  rewrite. The former `Infrastructure/` split into `Core/Bridge/` (the envelope
  contract + the host-agnostic `WebMessageRouter`), `Core/Persistence/` (SQLite + EF
  migrations), and `Desktop/WebHost/` (the `WebView2Bridge` transport). Pure
  structural refactor — no behavior change.
- Test project renamed `ChordFlow.Tests` → `ChordFlow.Core.Tests` and retargeted to
  plain `net10.0` (no longer Windows-bound).

### Tests
- 163 xUnit tests (was 106); all green.

## [0.3.0] — 2026-06-08

Phase 4 — music-theory-first domain. A focused rewrite of the `Domain/` kernel so
transposition, diatonic generation, voicings, rhythm, swing/shuffle, and lead
targets are all *derived*, never hand-authored. The sequential `Beat` rhythm model
is replaced by a positional 48-PPQ tick grid. No UI or persistence-schema changes.

### Added
- **Harmony** — `Quality` backed by interval sets (8 v1 qualities) via
  `QualityIntervals`; chord-relative `ChordTone`/`ChordTones` (the b7-of-G7 bridge);
  first-class `Scale` + `DiatonicChord.Build` (I maj7 … vii m7b5); `NoteSpeller`
  (per-key spelling, promoted out of the renderer); `ScaleDegree` distinct from
  `RomanDegree` (two degree frames).
- **Voicings** — optional diagram metadata on `Voicing` (`BarreFret`/`FirstFret`/
  muted strings); `IVoicingStrategy` + `BeginnerShellStrategy`; `VoicingBook` is now
  a strategy dispatcher; `Fretboard` geometry.
- **Rhythm (tick grid)** — `TickGrid` (PPQ 48), `TimeSignature`, `RhythmEvent`,
  `Stroke`/`Accent`, `PickupMeasure`, and a tick-based `RhythmPattern`. A
  `RhythmQuantizer` in the `Rendering/` seam compiles the grid to `:N` slots.
- **Overlays** — `Feel` + `FeelTransform` (playback-time long-short warp),
  `AccentPattern` (backbeat), `StrokeOverlay` — composable, never stored on a pattern.
- **Lead training** — `TargetZone`/`Importance`/`LeadTargets`; guide tones (3 & 7)
  derived from the interval sets and resolved to fretboard positions (domain only).
- **Reference doc** — `loom/refs/chordflow-domain-model-reference.md` mapping the kernel;
  linked from `loom/ctx.md`.

### Changed
- **Rhythm model migrated** from sequential `Beat(Duration, IsHit)` to the positional
  tick grid; `Beat`/`Duration` removed and the renderer's inline duration logic replaced
  by the quantizer. Existing alphaTex output is byte-identical for the MVP patterns.
- `Transposer` now consumes a `Scale`; the renderer derives spelling from `NoteSpeller`
  and the `\ts` header from the pattern's `TimeSignature`.
- `Exercise` gained a `Feel` field (defaults to Straight; applied at render time, not stored).

### Tests
- 106 xUnit tests (was 39); all green. Includes a Bb 12-bar-blues end-to-end render
  smoke check through the new path.

## [0.2.0] — 2026-06-08

Phase 3 — persistence & UI. ChordFlow becomes a usable trainer: build an exercise
on screen, save it, reload it, and track practice. The voicing engine now covers
all 12 keys.

### Added
- **SQLite persistence (EF Core)** — `ChordFlowDbContext` with `Exercises`
  (definition fields only — never alphaTex) and `PracticeRecords`; initial
  migration, applied on startup. Local file at `%LOCALAPPDATA%\ChordFlow\chordflow.db`
  (no server, no localhost port).
- **`ExerciseLibrary` slice** — save an exercise definition, list saved exercises,
  reload one (alphaTex **regenerated** from the definition on load, never stored).
- **`Progress` slice** — "mark practiced" records a practice event (no accuracy /
  scoring); an unsaved exercise is saved first so the record always has a target.
- **Builder UI** — key picker (12 keys), rhythm picker, tempo, Generate, Save,
  Mark-practiced, and a clickable saved-exercise list with a ✅ practiced marker.
  Each control posts a bridge envelope to its slice.
- **Bridge vocabulary** — added `generate` / `save` / `listExercises` /
  `loadExercise` / `markPracticed` (in) and `exerciseList` / `practiceRecorded` /
  `status` (out).

### Changed
- **`VoicingBook` generalized to a computed movable dom7 shell shape** covering all
  12 keys (`(s5:R, s4:R-1, s3:R)`, `R` in 1..12). Previously a 3-row hand-authored
  table that only rendered the Bb blues, so the key picker silently failed off Bb.
  Reproduces the original Bb7/Eb7/F7 frets exactly. (Design §2/§6 amended.)
- Transport (play/stop/tempo) now routes through the `PracticeSession` slice rather
  than driving alphaTab directly from JS.

### Fixed
- A render failure no longer silently drops the bridge message (which looked like a
  control "doing nothing") — the host now surfaces a `status` error, and an exercise
  that doesn't render can't be saved.

### Tests
- 39 xUnit tests (was 26); the `VoicingBook` suite now covers all 12 roots.

## [0.1.0] — 2026-06-08

First tagged release: the engine generates a 12-bar blues exercise, renders it
as tablature, and plays it back with a synchronized beat cursor.

### Added
- **Music engine (`Domain/`)** — pure, immutable kernel (Key, Chord, Progression,
  RhythmPattern, Voicing) with a `Transposer` and a `VoicingBook`. 12-bar blues
  transposable to all 12 keys; rhythm patterns beat-1, beats-1&3, quarters;
  beginner shell voicings.
- **`AlphaTexRenderer`** — turns an `Exercise` into an alphaTex string; the sole
  alphaTex-aware component (renderer seam for future MIDI/GuitarPro/MusicXML).
- **Desktop host** — WinForms + WebView2, serving the local `wwwroot` over an
  in-process `https://chordflow.local/` virtual host (no web server, no localhost
  port). Renders tablature via [alphaTab](https://www.alphatab.net/) and plays it
  with a bundled GM soundfont.
- **C#↔JS bridge** — narrow JSON-envelope protocol over `chrome.webview`
  (`loadScore`/`play`/`stop`/`setTempo` out; `ready`/`playbackFinished`/
  `beatChanged` in); payload is the alphaTex string.
- **Playback** — play / stop / tempo transport with a synchronized beat cursor,
  current-bar highlight, and active-note highlighting.
- **Tests** — 26 xUnit tests over the Domain kernel and `AlphaTexRenderer`.

### Changed
- **Desktop host migrated from Photino.NET to WinForms + the official
  `Microsoft.Web.WebView2` control.** Photino's WebView2 *composition* controller
  renders a black window on the .NET 10 + WebView2-149 stack; the WinForms
  *windowed* controller renders correctly. Only the host (`Infrastructure/`) and
  the `app.js` transport shim changed — the engine, renderer, feature slices, and
  the bridge envelope contract were untouched. Rationale:
  `loom/refs/photino-net-desktop-host-reference.md`.

### Known limitations
- **Windows-only** (WinForms host). The engine stays UI-agnostic, so a
  cross-platform / web front-end remains an additive future option.
- No persistence or on-screen pickers yet (SQLite save + key/rhythm/tempo UI are
  the next phase).
- No audio-input accuracy detection (out of scope for v1).

[0.14.0]: https://github.com/reslava/chord-flow/releases/tag/v0.14.0
[0.13.0]: https://github.com/reslava/chord-flow/releases/tag/v0.13.0
[0.12.0]: https://github.com/reslava/chord-flow/releases/tag/v0.12.0
[0.11.0]: https://github.com/reslava/chord-flow/releases/tag/v0.11.0
[0.10.0]: https://github.com/reslava/chord-flow/releases/tag/v0.10.0
[0.9.0]: https://github.com/reslava/chord-flow/releases/tag/v0.9.0
[0.8.0]: https://github.com/reslava/chord-flow/releases/tag/v0.8.0
[0.7.0]: https://github.com/reslava/chord-flow/releases/tag/v0.7.0
[0.6.0]: https://github.com/reslava/chord-flow/releases/tag/v0.6.0
[0.5.0]: https://github.com/reslava/chord-flow/releases/tag/v0.5.0
[0.4.0]: https://github.com/reslava/chord-flow/releases/tag/v0.4.0
[0.3.0]: https://github.com/reslava/chord-flow/releases/tag/v0.3.0
[0.2.0]: https://github.com/reslava/chord-flow/releases/tag/v0.2.0
[0.1.0]: https://github.com/reslava/chord-flow/releases/tag/v0.1.0
