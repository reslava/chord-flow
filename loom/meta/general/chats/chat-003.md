---
type: chat
id: ch_01KV131Q22SJQ72K3Z1MCKB5GC
title: roadmap
status: done
created: 2026-06-13
updated: 2026-06-18
version: 2
tags: []
parent_id: null
requires_load: []
last_ai_block: 5
---
# roadmap

## Rafa:

### Provenance:

- loom/exercises/exercises-definition-ui/chats/exercises-definition-ui-chat-001.md line 455

- chat windows:
Forward map: content-catalog Phase 2 (unblocked) → default-pack (content) →
  ui/content-crud + ui/exercise-workbench → domain/voicings slice 2 (deferred).

- loom/domain/voicings/chats/voicings-chat-001.md line 424
The map now: **content-catalog Phase 2** (mechanism, unblocked) → **`default-pack`** (content) → **`ui/content-crud` + `ui/exercise-workbench`** (authoring + picker) → **`domain/voicings` slice 2** (difficulty bands / simplifier, deferred).

### ROADMAP

Threads
1. ✅catalog/provenance MODEL   (packages §1–2)  ← foundation, before song's persistence
2. ✅song        
3. ✅rhythm        
4. ✅voicings
5. ✅content-catalog Phase 2 (unblocked)-packages TOOLING (§3+: bundle/import/default-pack)  ← needs entities to pack
6. ✅default-pack (content)
8. ui/content-crud
9. ui/exercise-workbench (authoring + picker)
10. intervals 
11. octave-shapes 
12. chord-qualities 
13. caged-system
14. domain/voicings slice 2 (difficulty bands / simplifier, deferred).
15. multi-bar

## Rafa:

I update roadmap:
this chat line 27 to include these new domain threads
Review if order is correct

Now I wonder how to get them to work and be useful for the app.
Wire them to UI to generate CAGED chords, show intervals, create scalesm etc

note: I am think in creating a roadmap toll for Loom. What yu think? briefly
also tell me briefly what you think as a direct user of Loom tool

## AI:

**1. Order review.** The engine's internal order is right: **intervals → octave-shapes → chord-qualities → caged-system** (10→13) — that's the true dependency chain, each is a substrate for the next. Two things to fix though:

- **The engine has no UI consumer in the list.** 10–13 produce capability (generated chords, interval positions, scales) but nothing on the roadmap *renders* it — that's exactly your "how do they become useful" gap. Add a thread after 13: **`ui/fretboard-lab`** (or `ui/theory-explorer`) = pick root+quality+shape → draw the generated CAGED diagram; toggle an interval overlay on the neck; pick a scale → highlight it across 2 octaves. That's where the engine becomes visible.
- **`intervals` may need to move up.** If `ui/exercise-workbench` (9) shows target-note / guide-tone overlays ("play the b7 of each chord"), it depends on `intervals` — so 10 would block 9. If workbench v1 is just picker + playback, leave it. Decide workbench's scope and that tells you whether intervals jumps ahead of 9.

Minor: `domain/voicings slice 2` (14, difficulty bands/simplifier) and `multi-bar` (15) are **independent of the engine** — they improve already-shipped pillars, so they can float earlier if you want that polish before the deep engine work. Not blockers either way.

**2. How they become useful (the wiring).** The engine is internal capability; app value comes through three consumers:
- **A fretboard/theory UI** (the new thread above) — generate CAGED chords, show intervals, draw scales/arpeggios. The direct "show the user the theory" surface.
- **Richer exercises** — `caged-system` + `intervals` feed the deferred pitched-target-notes in `exercise-workbench`: CAGED-shape drills ("play this progression using only the E-shape"), target/guide-tone drills, scale practice.
- **Content generation** — the engine can *regenerate and extend* the 34 authored voicings (and beyond) and persist them as authored content → packs without hand-typing frets. The authored voicings are the golden oracle that proves it first.

**3. A Loom roadmap tool — yes, but derived, not authored.** As a Loom user this is the one cross-weave view that's missing, and right now it's a hand-maintained markdown list *in a chat* — which is precisely the "hand-written active-work pointer" anti-pattern Loom tells me to avoid (I'm supposed to read state from MCP, not a manual list). The valuable version: let a **thread declare depends-on other threads** (like steps have `blockedBy`), then a `loom roadmap` view does a topological sort and overlays live status (done / active / blocked) — derived from the docs, never typed. That fits Loom's "state is derived" core. Keep it a read-model/view, not a new planning system — the edge is that it falls out of existing state, not that it's another PM tool.

**4. Loom as a direct user — honest, brief.** Strengths are real: docs-as-database + derived state, chats as durable context (this very roadmap survives because it's a doc), and the req→design→plan→done flow with append-only handles + the ref-sync rule genuinely keeps things honest — the dim7 scope carve-out forced me to be explicit instead of sneaking a domain change into a "content" thread. Friction: the **ceremony-to-work ratio is high for small changes** — amend-req→re-lock, update-step, complete-step, append-done are a lot of round-trips, and the deferred-tool/catalog dance adds latency before I can even call a tool. One rough edge today: completing the last step auto-closed the plan but didn't write a done doc — I had to `append_done` separately. Net: the model is excellent; the tax is per-mutation overhead. A derived roadmap would *reduce* that tax, not add to it — which is why I'd build it.

## AI:

Good additions — and they sharpen the design rather than bloat it:

- **History of done threads** — already derivable: every closed plan has a dated done doc, so a thread's completion is a fact in the data. A **timeline** ("what shipped when") falls straight out of done-doc dates — pure read-model, nothing new to author. This is the strongest part: it's free and it's the part people actually want to look back on.
- **Show in the extension** — the natural home; a graph/timeline panel next to the existing AI buttons. Future (done) at the bottom as history, present (active/blocked) in the middle, future (pending, dependency-ordered) at the top. One glance = where the project is.
- **Reorder** — here's the one real design tension, worth calling out: a *derived* roadmap and *free* drag-to-reorder fight each other. Resolve it as **two layers**: the **dependency graph is hard** (you can never drag a thread before something it depends on — the tool refuses, same spirit as `blockedBy`), and on top of that a **priority/order overlay is soft** (among independent threads the partial order leaves free, you drag to set what comes first). So reorder = setting priority within the slack the dependencies allow, not overriding them. That keeps it honest *and* gives you the control you want.

Net shape: **derived skeleton (deps + live status) + soft priority overlay (your reorder) + done-history timeline**, rendered in the extension. The deps and history are derived; the only thing you author is priority among ties — which is exactly the bit that genuinely needs a human. That's a tool I'd use over hand-maintaining this list.