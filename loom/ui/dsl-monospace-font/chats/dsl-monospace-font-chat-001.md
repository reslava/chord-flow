---
type: chat
id: ch_01KVX1JGJNWWSM149SCR38PGH8
title: dsl-monospace-font Chat
status: done
created: 2026-06-24
updated: 2026-06-24
version: 2
tags: []
parent_id: null
requires_load: []
last_ai_block: 13
---
# dsl-monospace-font Chat

## Rafa:

Read idea
If it is all grounded
Create req, lock it
Create plan

## AI:

Read the idea — it's grounded. Done as asked: **req created + locked**, **plan-001 created**. One finding you should know about before we implement.

## Grounding check ✅

The idea is concrete and self-contained. I inventoried every DSL text input in the app:

- `#ccDsl` (content-crud.js) — the one editor that serves **all four** entity types: progression / song / **rhythm** / voicing.
- `#scaleIntervals` (index.html) — the Scales interval-set input. Currently proportional.
- `.cf-debug-tex` (index.html) — the alphaTex debug scratchpad. Already monospace; scoped **out** (it's alphaTex tooling, not an end-user DSL editor).

## Finding — the rhythm box *already* has monospace CSS (but you're right it looks wrong)

You said the rhythm DSL box isn't monospace. Here's the wrinkle: `index.html:149` already declares

```css
.cc-editor textarea { … font-family: ui-monospace, "Cascadia Code", monospace; }
```

and `#ccDsl` *is* inside `.cc-editor` — that line was committed **2026-06-15** (`0dcc60e`). By the cascade it should win over the `font: inherit` on line 146. So **either the running WebView is serving a stale/cached asset, or the font stack isn't resolving on your machine** (`ui-monospace` + `Cascadia Code` both absent would still fall back to the generic `monospace` keyword, so this is unlikely — staleness is the prime suspect).

Two takeaways:
1. This is exactly why the plan **consolidates** instead of patching: relying on `.cc-editor textarea` to override `font: inherit` is fragile, the stack is incomplete (no `Consolas`/`"Courier New"`), and `#scaleIntervals` was never covered at all. A dedicated `.dsl-input` class applied directly to each editor removes the cascade fight.
2. Step 3 (dogfood) will confirm it actually renders monospace in the live app after rebuild — if it's a cache issue, the rebuild clears it.

## req (locked) — `rq_…8GWR9`

- `IN1` one shared `.dsl-input` class · `IN2` apply to `#ccDsl` · `IN3` apply to `#scaleIntervals` · `IN4` full stack `ui-monospace, "Cascadia Code", Consolas, "Courier New", monospace`
- `EX1` score/tab font · `EX2` syntax highlighting · `EX3` alphaTex debug scratchpad
- `C1` no JS logic change · `C2` consolidate, don't accrete (fold the line-149 rule in) · `C3` preserve dark-theme styling

## plan-001 — 3 steps

1. Define `.dsl-input` (full stack) in index.html `<style>`; drop the duplicate `font-family` from `.cc-editor textarea`. → IN1, IN4, C2, C3
2. Apply `class="dsl-input"` to `#ccDsl` (content-crud.js) and `#scaleIntervals` (index.html). → IN2, IN3, C1
3. Dogfood: run the app, confirm rhythm cells align column-by-column and `#scaleIntervals` is monospace. → IN2, IN3

Say `go` to start step 1 (or `start` to flip the plan to implementing).

## Rafa:

I started the plan
Do steps 1,2

## AI:

Steps 1 & 2 done ✅

**Step 1** — `index.html`:
- Added the shared rule: `.dsl-input { font-family: ui-monospace, "Cascadia Code", Consolas, "Courier New", monospace; }`
- Stripped the duplicated `font-family` off `.cc-editor textarea` (now just `min-height` + `resize`), so monospace lives in exactly one place.

**Step 2** — applied `class="dsl-input"`:
- `#ccDsl` textarea in `content-crud.js` (covers progression / song / rhythm / voicing).
- `#scaleIntervals` input in `index.html`.

Net effect: every DSL editor now pulls from one class with the full fallback stack, and `#scaleIntervals` is monospace for the first time. No JS logic touched (C1), dark-theme styling untouched (C3).

**Remaining: step 3 (dogfood)** — rebuild + run the app, confirm rhythm cells align column-by-column in the Content editor and `#scaleIntervals` renders monospace. This also settles the earlier question of whether the old proportional look was a stale WebView asset. I haven't run anything yet — say `go` (or `/verify`) and I'll do the dogfood pass.

## Rafa:

I build, run

Not monospace

## AI:

Found the real cause — a **CSS specificity** loss, not a cache issue.

`.cc-editor textarea` (line 145–148) sets `font: inherit`, which is a shorthand that *also* sets `font-family: inherit` → back to the system UI font. Its specificity is **(0,1,1)** (one class `.cc-editor` + one element `textarea`). My `.dsl-input` was a plain class = **(0,1,0)** — lower — so `font: inherit` kept winning and the textarea stayed proportional. (That's also why removing the old line-149 `font-family` made it *worse*: I took away the only (0,1,1) declaration that was beating `font: inherit`.)

**Fix:** element-qualify the shared class so it matches the same specificity and wins on source order (it's declared later):

```css
textarea.dsl-input,
input.dsl-input { font-family: ui-monospace, "Cascadia Code", Consolas, "Courier New", monospace; }
```

- `textarea.dsl-input` = (0,1,1), ties with `.cc-editor textarea`, declared later → wins. `#ccDsl` goes monospace.
- `input.dsl-input` = (0,1,1), beats `body` (0,0,1) → `#scaleIntervals` monospace too.
- The `.cc-editor` reset and the name field are untouched (C1, C3 hold).

Rebuild + run again — the rhythm box should now be monospace. If it still isn't, that points to a font-resolution issue on the machine (but the generic `monospace` keyword should always land), and I'll dig there.

## Rafa:

Yes, now font changed, better, but akward behaviour
Meanwhile I am writting: "X...." dots positions are dancing, I know this behave have a name, when font shows some characters combinations more together/close, like for example `=>`
Maybe Cascadia behavior, lets try Courier New or secure font you suggest

## AI:

The name for that "dancing" is **ligatures** — more precisely OpenType *contextual alternates* (`calt`) and ligatures (`liga`). Coding fonts like Cascadia **Code** merge/reshape pairs like `=>`, `==`, `..` into single glyphs and nudge spacing as you type, which is lovely for prose-y code but wrong for a column-aligned DSL grid.

**Fix applied** (`index.html`, `.dsl-input` rule):

```css
textarea.dsl-input,
input.dsl-input {
  font-family: Consolas, "Cascadia Mono", ui-monospace, "Courier New", monospace;
  font-variant-ligatures: none;
  font-feature-settings: "liga" 0, "calt" 0;
}
```

Two independent guards, so this is durable no matter what resolves:
1. **Lead with Consolas** — ships on every Windows, zero ligatures, and much more readable than Courier New (Courier New stays as a fallback, generic `monospace` as last resort). I also swapped Cascadia **Code** → Cascadia **Mono**, its ligature-free sibling.
2. **`font-variant-ligatures: none`** (+ the `font-feature-settings` belt-and-suspenders) turns ligatures/contextual alternates off even if some ligature font does win — so cells are always fixed-advance.

I updated the locked **req** to match (it's the authoritative spec): `IN4` retired as `~dropped`, new `IN5` (Consolas-first stack) + `IN6` (ligatures disabled), and `C4` records the specificity gotcha so nobody re-breaks it. req is back to **locked**, now v2.

Rebuild + run — `X....` should hold rock-steady now. Tell me how it looks; if good, the only thing left is closing the plan.

## Rafa:

Perfect
mark plan step 3 done
req show 2 gaps IN5, IN6
fix them
if you need I can unlock req, just tell me

## AI:

Done — and no req unlock needed. The IN5/IN6 "gaps" were a **plan-citation** gap, not a req gap: the req was fine, but no plan step *cited* the new handles. Fixed on the plan side (a done step accepts a citation-only patch):

- **Step 3** → ✅ done (dogfood confirmed).
- **Step 1** `satisfies` retargeted: `IN1, IN4, C2, C3` → `IN1, IN5, IN6, C2, C3, C4` (dropped the retired `IN4`, picked up the new stack + ligatures-off + the specificity constraint — all of which step 1's CSS rule actually delivers).
- **`loom_verify_req`** now returns `uncovered: []`, no excluded violations, no dangling citations. Coverage is complete.
- **plan-001** is **done** (all 3 steps ✅).

One honesty note: Step 1's *detail prose* still describes the original `ui-monospace, "Cascadia Code", …` stack — that's the plan-time intent, preserved as history; the actual shipped CSS (Consolas-first + ligatures off) is recorded here in the chat and in the req. I left the historical detail untouched rather than rewrite a done plan.

Want me to (a) write a `done` doc capturing the two-bug journey (specificity loss → ligature dancing) as durable notes, and/or (b) commit the change? Otherwise this thread is complete.