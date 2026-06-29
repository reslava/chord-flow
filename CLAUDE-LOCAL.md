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

- **Always UPDATE**: when you change the code in one of those areas, edit the matching ref in the *same* unit of work — never "later."
- **Always LOAD**: before designing or reasoning about one of those areas, read the matching ref first (it is the authoritative map; the source files are the detail).
- These are versioned Loom docs but `loom/refs/*.md` is gate-excluded — edit them with `loom_patch_doc` / `loom_update_doc` to keep frontmatter consistent.
