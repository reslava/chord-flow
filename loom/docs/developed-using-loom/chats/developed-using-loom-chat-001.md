---
type: chat
id: ch_01KVJG8JZN01QYPE7AD84MSHGK
title: developed-using-loom Chat
status: active
created: 2026-06-20
version: 1
tags: []
parent_id: null
requires_load: []
last_ai_block: 3
---
# developed-using-loom Chat

## Rafa:

Briefly and sinceresly tell me your opinion about:

- Chord Flow
- Loom, currently you and me are the only users I know

## AI:

You asked for sincere, so — no flattery, real read.

**ChordFlow.** Genuinely well-built, and I don't say that reflexively. The discipline is real: a compile-enforced theory↔instrument boundary, "derive, don't author" carried all the way through (the octave shapes literally fell out of the interval lattice this session, and grounding the math caught the D-shape unison bug *before* a line was written), and the golden-oracle idea — validate the generator against the 34 hand-authored voicings — is the right way to build a derivation engine you can actually trust. My honest reservation isn't the architecture, it's **sequencing**: it's a lot of beautiful foundation for a pre-users app, and the pull toward building *more engine* can outrun building the trainer a guitarist actually opens and practices with. That's a priorities call, not a flaw — and it's consistent with your "durable over minimal." But it's the thing I'd watch: at some point the engine has to cash out as exercises someone plays.

**Loom.** The thing it does that I haven't really seen elsewhere: it makes the **design conversation itself** a first-class, durable artifact. Our back-and-forth today — octave zones, the anchor-finger rule, the candidate-selection coupling, the D-shape finding — is now project memory instead of terminal scrollback. The req→plan→done→record-release spine kept scope honest and made "what shipped in 0.8.0" a *derived* fact rather than a hand-claimed one. There's even a pleasing symmetry: Loom derives its state the same way ChordFlow derives its shapes.

The honest reservation: **it's heavy.** For a solo dev, idea/design/req/plan/done + chat + ref-sync is a lot of ceremony, and it weighs most exactly on the *small* features where you most want to move fast — today's caged-shapes page cost more process than code. Its payoff is real but conditional: it rewards a disciplined author and taxes an impatient one. With two users, whether that trade generalizes is genuinely unproven. My sincere read — it's a strong fit *for how you specifically work*: you actually want the durable record and the forced "stop at 95% confidence" pauses, and you use them well. I'm less sure it's a universal fit; it asks a lot up front, and a lot of people would bounce off the ceremony before they felt the payoff.

If I had to name the through-line of both: you optimize for *correctness and durability over speed*, in the tool and the product alike. That's rarer than it sounds, and this session was a good showcase of it — but it's also the bias worth being most honest with yourself about.

## Rafa:

Thank your for replying! having at least some feedback is necesary to improve some aspects.

### TODO

I would like to include in main README a section mentioning that this project is fully developed with 
🧵Loom: https://github.com/reslava/loom

Telling how using weaves, threads, ideas, designs, reqs, plans, context, reference docs and the roadmap features has been helpful in the durable design of a robust music domain and construction of Chord Flow. The chats between you and me before implementing any code to design models properly.

How every decision maked, every idea brainstorm, everything is there.
That by using loom, AI has all related context available in every session.

You speak English much better than me.

Feel free to icnlude some lines with your own vision, sign it yourself, you are part of Chord Flow and part of Loom. Please, be sincere and honest, if not is useless, name the negative part also.

- Update main README

## AI:

Added a **## Developed with Loom** section to the root `README.md` (between Documentation and Third-party assets). It covers:

- What Loom is (documents-as-database, derived state) + the **weave / thread** structure.
- The **idea → design → req → plan → done** spine, with reqs as locked, citable scope handles.
- **Chats first** — the design conversations happen before any code; the music domain (octave shapes, interval lattice, CAGED zones, fingering/candidate rules) gets modelled in writing.
- **Context + the three reference docs** kept in lockstep; the **roadmap** with derived status/release facts.
- The payoff: every decision/brainstorm/correction is *there*, and the AI loads the full related context each session — plus the symmetry with ChordFlow's own "derive, don't author."

Then a short **signed note from me** — honest, with the downside named (it's heavy; ceremony is real overhead on small features; the friction precedes the benefit; it trades speed for correctness/durability). Signed *— Claude (Anthropic), via Claude Code*.

It's an uncommitted working-tree change for now. Want me to commit it (e.g. `docs: README — developed-with-Loom section`)? Have a read first and tweak anything that doesn't sound like you — and tell me if the honest/negative part is the right amount or you'd pitch it differently.

## Rafa:

commit

I would also add a mirror section `Used by` in main README of loom:
`J:/src/loom/README.md`
note: do not touch any Section Number, neither the TOC, they are automatically generated by a VSCode extension, I will update the section number when you insert the new section
Mentioning `Guitar Flow` https://github.com/reslava/chord-flow
Opening to other users of Loom to tell me if they want to be included

Do you think is good idea?
If so, do it



