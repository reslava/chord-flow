---
type: design
id: de_01KWZAEXXHM8K1PB5J71G3T800
title: Explicit per-chord voicing references in the DSL
status: done
created: 2026-07-07
updated: 2026-07-08
version: 6
idea_version: 1
tags: []
parent_id: id_01KVZYCRYEXGAAAH2QQ27ZCSYF
requires_load: []
---
# Explicit per-chord voicing references in the DSL

## Summary

One capability — **pin a specific voicing to specific chords in a Song** — expressed as **one voicing-spec grammar** used in **two placements**, resolved through a **most-specific-wins cascade** that sits on top of the automatic ranking fill. It is a purely **additive override path** riding the `CompingResolver` seam from [[engine-derived-as-app-source]] (decision D4 = (B)); nothing about the ranking fill changes.

The whole feature is **Song-arrangement** concern: progressions stay pure harmony.

## Decisions (from chat-001)

- **D1 — Grips are movable, normalized to C.** A literal grip is not absolute frets; it is a *shape*, canonicalized to a C anchor exactly like the Voicing DSL, and re-transposed to the sounding root at realization. Rationale: ChordFlow is a transposing trainer — absolute frets would silently break on any `key`/`mod` change. This also settles idea open-Q2 ("does it canonicalize?" → yes).
- **D2 — Resolution is a most-specific-wins cascade** (see *Resolution* below): per-chord `{}` › degree-scoped `voice` › quality-scoped `voice` › ranking fill.
- **D3 — Progressions stay pure harmony** (idea open-Q4). The `{}` token is *lexed* by `ProgressionParser`, but a standalone/stored progression **rejects** annotations (fail-loud); only an **inline** progression inside a Song honors them. The `voice` default is unambiguously Song-DSL.
- **D4 — `voice` keyword, not a `\` sigil.** The Song DSL is otherwise all space-keywords (`key`, `feel`, `tempo`, `mod`); `voice <selector> = <voicing-spec>` reads like the existing `NAME = …` definitions.
- **D5 — One voicing-spec grammar, uniform sugar.** A voicing-spec is either a **literal grip** (bare, or explicit `c:`) or a **source-qualified reference** (`u:` user · `a:` automatic/engine-derived · `<package>:` e.g. `swing:`). A reference always carries its `source:` word, so a bare grip is unambiguous anywhere. Same grammar in the brace and after `=`.
- **D6 — Missing/filtered reference → fail loud** (idea open-Q3), consistent with content-pack reference resolution.
- **D7 — Wildcard degree `*`** marks a quality-scoped selector (`*7`, `*m7`), disambiguating it from a bare degree token (a lone `7` already means degree-VII-major). `*` is unused elsewhere in the Song/Progression DSL.
- **D8 — Rootless voicings via a declared anchor.** A grip may carry an optional `root:<string>[@<fret>]` clause. The `@<fret>` form declares a **phantom root** on a *muted* string, making rootless voicings (jazz shells/drop-2s) first-class. It is required only when the root is unsounded or inference is ambiguous — a rootless `{3, b7}` shell is a tritone shared by two dom7 roots, so the engine can't always recover it; otherwise the engine infers.

## D9 — Annotation representation at the Music↔Instruments boundary

`InstrumentBoundaryTests` enforces (NetArchTest, IL-level) that **`ChordFlow.Music` never depends on `ChordFlow.Instruments`**, and `VoicingSpec` is `FretPosition`-based (Instruments). So a typed spec cannot live on the pure-Music `ChordSpan` / `Song`. Resolution:

- `ChordSpan` carries the annotation as an **opaque `string? VoicingAnnotation`** (the raw inner text of `{…}`), uninterpreted by Music.
- `Song` carries the `voice` map as **`VoiceSelector → raw-spec-string`**. The *selector* is pure-harmony vocab (degree/quality/`*`), so `SongParser` validates it into a small Music `VoiceSelector` type and does the duplicate check (`C6`); only the *spec value* stays opaque.
- `CompingResolver` (Features — legitimately sees both Music and Instruments) parses the opaque strings into a `VoicingSpec` via `VoicingDslParser.ParseSpec`.
- **Consequence:** a malformed grip inside `{…}` fails loud at **realization**, not at progression-parse time (Music can't call the Instruments parser) — consistent with reference resolution (`IN6`).

No change to the DSL surface, resolution semantics, or any req handle — only the internal representation.

## Grammar

### Voicing-spec (shared value)

```
voicing-spec := grip | reference
grip         := [ "c:" ] <s6> <s5> <s4> <s3> <s2> <s1> [ root-clause ]   # low-E → high-E; x=mute, 0=open, N=fret
reference    := <source> ":" <id>                                        # source ∈ { u | a | <package-id> }
root-clause  := "root:" <string 6..1> [ "@" <fret> ]                     # optional anchor; @fret = phantom (muted) root, e.g. root:6@8
```

- `c:` is the pseudo-source meaning "the payload is a literal grip." It is **accepted but optional** — a bare 6-token grip is identical.
- A `reference` is distinguished purely by carrying a non-`c` `source:` word before its id.

### Per-chord annotation (Progression-DSL lexeme)

A `{ voicing-spec }` token binds to the **immediately preceding chord**. A `{` can never begin a bar or chord, so the binding is unambiguous whether or not whitespace separates it (`17{…}` ≡ `17 {…}`). Internal spaces belong to the grip; the tokenizer reads to the matching `}`.

```
end = 17 {8 x 7 9 8 x} 6-7 {u: C6} 17 57
```

Timing-free, like the chord itself. A leading `{` with no preceding chord, or an annotation on a chord in a **stored/standalone** progression, is an error (D3).

### Song-level default (`voice` directive, Song-DSL)

Lives in the Song's **definitions** section (peer of `NAME =`, `key`, `feel`, `tempo`), position-independent, whole-song.

```
voice <selector> = <voicing-spec>

selector := quality-scoped | degree-scoped
quality-scoped := "*" <quality>            # *7  *m7  *maj7  *dim7  ( "*" alone = major triad )
degree-scoped  := [accidental] <degree> <quality>   # 17  #4dim7  2-7  (same shape as a chord token)
```

At most **one** `voice` per distinct selector (a duplicate selector is an error, like a duplicated part definition).

```
voice *7   = 3 3 2 3 1 x      # every dominant-7 chord, transposed per root
voice 17   = 8 x 7 9 8 x      # …but the I7 specifically uses this
voice #4dim7 = 8 x 7 8 7 x    # the chromatic passing dim7
```

## Resolution — the `CompingResolver` cascade

For each realized chord instance (degree D, quality Q, sounding root R), the resolver takes the **first** hit, highest priority first:

1. **Per-chord `{}`** on that exact chord token.
2. **Degree-scoped** `voice <D><Q>` (exact degree+quality match).
3. **Quality-scoped** `voice *<Q>`.
4. **Automatic ranking fill** (today's behavior — [[engine-derived-as-app-source]]).

"Most-specific wins" is exactly this order: an explicit per-chord pin beats a degree default beats a quality default beats the fill. All four tiers produce a `Voicing`; only tier 4 exists today, so tiers 1–3 are pure additions in front of it.

## Grip anchoring & movability

A grip must resolve to a movable shape (D1). Movability needs one **anchor** — a (string, fret) reference whose pitch lets the engine compute the transpose shift and the normalize-to-C form. The anchor is found three escalating ways:

1. **Harmony-fixed** (per-chord grip, degree-scoped `voice`). The degree + current key fix the root pitch class outright. Transposition is "shift the whole shape by the root's semitone motion" on any `key`/`mod` change — no inference. Canonical storage = the grip shifted so its root maps to C.
2. **Inferred** (quality-scoped `voice *Q`, voiced root). The selector fixes only the quality, so the engine **infers** the anchor by spelling the shape (the chord-spelling the derivation engine already performs), then normalizes-to-C. This is the one piece that rides the [[engine-derived-as-app-source]] / voicings-engine machinery.
3. **Declared** (`root:` clause — required for rootless or ambiguous shapes). Inference cannot recover a root that isn't sounded, and a **rootless shell is genuinely ambiguous**: `{3, b7}` is a tritone shared by two dom7 roots a tritone apart (the reason tritone subs exist). So a grip may declare its anchor:
   - `root:<string>` — voiced root on that string; the fret is read from the grip (identical to the Voicing DSL's `root:`).
   - `root:<string>@<fret>` — a **phantom root** on a *muted* string: `@<fret>` supplies the pitch the grip can't sound and doubles as the normalize-to-C reference. The phantom fret may sit anywhere, including below the played notes (an implied bass root). `root:6@8` sounds C on the low E → the canonical C anchor.

`root:` is **optional** — omit it and the engine uses (1)/(2); supply it only when the root is muted (rootless) or inference is ambiguous. A `reference` (`voice *7 = a: shell-7`) is the already-movable alternative that sidesteps grips and inference entirely.

## Parsing & domain model

- `ProgressionParser` gains an optional **annotation** field on the parsed chord (parsed from a trailing `{…}` lexeme). Purity guard: annotations are accepted only when the parser is invoked in **inline-Song** context; a stored/standalone parse rejects them.
- `SongParser` gains the `voice` directive → a map `selector → voicing-spec` on the parsed Song (alongside key/feel/tempo defaults).
- The `voicing-spec` grammar is one small shared parser used by both the brace lexeme and the `voice` RHS.
- `CompingResolver` consumes both (per-chord annotations + the song's `voice` map) and applies the cascade above before its ranking fill. This is the only behavioral change to realization.

## Round-trip & serialization

- A **literal grip** serializes back as authored (bare grip; `c:` normalized away or preserved — bare is canonical output).
- A **reference** serializes back as `{source: id}` / `voice sel = source: id`.
- `voice` directives serialize in the definitions block. Parse → serialize → parse is a fixed point (idea validation: "the annotated DSL round-trips unchanged").

## Validation & errors

- `{…}` on a chord in a **stored/standalone progression** → "voicing annotations are a Song-level concern."
- **Reference** whose `source:id` resolves nowhere, or whose source is filtered out → fail loud at realization (D6).
- **Grip** that isn't exactly 6 tokens, or a bad fret token → same errors as the Voicing DSL `frets:` field.
- **`root:` clause**: `@<fret>` is required when the named root string is muted (`x`); a rootless/ambiguous grip with no `root:` and no inferable root → "cannot determine the voicing's root — add `root:<string>[@<fret>]`."
- **Duplicate `voice` selector** → "defines voicing for … more than once."
- **Bare degree vs wildcard**: `voice 7` is degree-VII-major (degree-scoped); "all dom7" must be written `voice *7`.
- Leading/orphan `{` with no preceding chord → parse error.

## Dogfood

An annotated 12-bar blues (mix of per-chord `{}`, a `voice *7` default, and one `voice #4dim7`) renders the **pinned grips** on the now/next fret-boxes of the fretboard UI page — visual confirmation that each tier resolves to the intended shape (guitar-weave dogfood rule).

## Out of scope (this thread)

- The automatic ranking fill + main-source/fallback ([[engine-derived-as-app-source]]).
- Selectable ranking modes ([[voicing-ranking-strategies]]).
- A UI voicing-picker that *writes* these annotations (additive, later).
- Per-span (per-bar) annotations; barre/finger hints in grips; the `\` sigil syntax.
- Voicing annotations as *content* on stored Progressions/Rhythms (purity, D3).
