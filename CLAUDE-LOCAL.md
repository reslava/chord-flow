# Project-Local AI Rules — Chord Flow

<!-- These are Chord Flow's OWN rules. The root CLAUDE.md imports this file AFTER the Loom contract -->
<!-- (@.loom/CLAUDE.md), so they augment/override it. Loom never overwrites this file. -->
<!-- Extracted from CLAUDE-before-loom-install.md: the residue left after removing the Loom boilerplate -->
<!-- (which now lives, current, in .loom/CLAUDE.md). Copy this to J:/src/chord-flow/CLAUDE-LOCAL.md. -->

## Reference-doc sync (required)

Three `loom/refs/` docs mirror the live system. Keeping them current is **mandatory and bidirectional** — a code change that lands without its ref update is incomplete:

| Area changed | Ref to UPDATE in the same change | Ref to LOAD before reasoning about it |
|--------------|----------------------------------|----------------------------------------|
| A **core DSL** (Progression / Song / Rhythm grammar, glyphs, tokens) | `chordflow-dsl-reference.md` | `chordflow-dsl-reference.md` |
| The **domain/kernel** (`Domain/` or `Rendering/` types, the music model) | `chordflow-domain-model-reference.md` | `chordflow-domain-model-reference.md` |
| **App architecture** (project structure, boundaries, seams, dependency direction) | `chordflow-architecture-reference.md` | `chordflow-architecture-reference.md` |
| The **Voicings Engine** derivation rules (operators, families, order, golden oracles, catalog coverage) | `voicings-engine-rules-reference.md` | `voicings-engine-rules-reference.md` |

- **Always UPDATE**: when you change the code in one of those areas, edit the matching ref in the *same* unit of work — never "later."
- **Always LOAD**: before designing or reasoning about one of those areas, read the matching ref first (it is the authoritative map; the source files are the detail).
- These are versioned Loom docs but `loom/refs/*.md` is gate-excluded — edit them with `loom_patch_doc` / `loom_update_doc` to keep frontmatter consistent.

## Deferral tracking (required)

**Whenever we decide to defer something, it must land on the roadmap the same turn — never only in prose.** A deferral that lives only inside a chat or design body is a deferral that gets forgotten.

- **Always create at least a `thread`** (`loom_create_thread`) for the deferred work, so it surfaces in the derived roadmap. Give it a `depends_on` edge to the thread it was deferred *from* when there is a real ordering.
- **Optionally add an `idea`** to that thread:
  - **Create the idea now** when we already have *enough foundation* — the shape is understood, constraints are known — so the reasoning isn't lost.
  - **Defer the idea** (thread-only) when it's a *very early* notion — capture the pointer, flesh it out when it's picked up.
- This applies to every kind of deferral — a phase-2 feature, an extracted seam, a small nice-to-have glyph — not just the big ones.
