---
type: idea
id: id_01KVQ3RMZXEPH63QJQD2M3MVE2
title: Anacrusis rendering — emit PickupMeasure as a true \ac pickup bar
status: done
created: 2026-06-22
updated: 2026-06-22
version: 2
tags: []
parent_id: null
requires_load: []
---
# Anacrusis rendering — emit PickupMeasure as a true \ac pickup bar

## The idea

We already **model** a pickup correctly — `PickupMeasure(IReadOnlyList<RhythmEvent> Events, int LengthTicks)`
in `ChordFlow.Music.Rhythm` is a short leading measure with its own tick length (ctx IN11), and the
renderer emits it as a leading bar voiced with the first chord (comping track) / mirrored as a leading
rest bar (lead track). What we *don't* do is tell alphaTab that bar is an **anacrusis** — it's currently
emitted as an ordinary, merely-short bar (`:4 (…) |`).

This thread closes that one gap: **emit the pickup bar with alphaTex `\ac`** so it renders as a true
pickup, not a generic incomplete bar.

## Why it matters (correctness, not cosmetics)

Without `\ac`, alphaTab:
- counts the pickup as **bar 1** — every real bar number is then off by one;
- treats it as a generic incomplete bar handled by its own default padding rules, not as a pick-up;
- produces no compensating short final bar (the classic anacrusis convention).

It's also squarely **dogfood-useful** — pickups are common in real content.

## What this thread adds

- Emit `\ac` on the comping pickup bar (`AlphaTexRenderer.BuildCompingBars`) and on the mirrored lead
  pickup bar (`BuildLeadBars`).
- Document `\ac` in `loom/refs/alphatex-syntax-reference.md` (it is **not** currently in the ref) — same
  unit of work, per the ref-sync contract.
- Update the two pickup tests (`Render_Pickup_EmitsLeadingMeasureBeforeBars`,
  `Render_WithLeadAndPickup_MirrorsPickupAsRestsOnLeadTrack`) whose `EndsWith`/`Contains` assertions
  encode the current pickup line.

## Confirmed facts (settled in `multi-bar-chat-001`)

- Our bundled `alphaTab.min.js` **supports anacrusis** (`isAnacrusis`) — `\ac` is available to us.
- alphaTex docs: *"Anacrusis (aka. pickup bars) do not follow [strict timing]. The length of those bars
  is defined by the actual beats/notes in the bar."* → a pickup is **not** padded to a full bar, so our
  current short-bar emission shape is **already correct** — we are *only* adding the `\ac` marker, no
  fill/pad logic needed.

## Scope

- **Pure rendering change.** The `PickupMeasure` domain type does not change at all; this lives entirely
  in `Rendering/AlphaTexRenderer` + the alphaTex ref. (Homed in the `domain` weave as a rhythm-timing
  concept sibling to `rhythm`/`multi-bar`, per the chat decision — weaves are workstreams, not namespaces.)

## Out of scope

- **Pickup-into-section** — how a pickup interacts with multi-bar section alignment / section-anchored
  fills. That is owned by [[multi-bar-idea]] and can `depends_on` this thread once it starts.
- Any change to how pickups are authored or parsed (the DSL/type side is unchanged).

## Open question to resolve at design time

- **Exact `\ac` placement / syntax** against alphaTab — bar-level vs beat-effect token, and where it sits
  relative to the chord group. A quick spike against the engine before locking the emitted string.

## Validation

- Full solution builds; all tests green; the two updated pickup tests assert the `\ac` token.
- `loom/refs/alphatex-syntax-reference.md` documents `\ac`.
- **Verify visually** — run the app and confirm alphaTab renders the bar as a real pickup (correct bar
  numbering / pickup display); a string assertion alone does not prove this.

Related: [[multi-bar-idea]], [[design-philosophy-durable-over-minimal]], the `rhythm` thread (which
delivered `PickupMeasure`), `chordflow-dsl-reference`.