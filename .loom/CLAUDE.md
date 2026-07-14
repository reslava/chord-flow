# Loom Session Contract

<!-- rule:what-loom-is -->
## What Loom is

**Loom** is a document-driven, event-sourced workflow system for AI-assisted development.
Markdown files are the database. State is derived. AI collaborates step-by-step with human approval.

---

## File ownership — where your rules go

**`.loom/CLAUDE.md` is Loom-owned and regenerated on every `loom install`.** Never put project-local rules here and never hand-edit it — the next `loom install` overwrites the file to deliver Loom contract updates, and your edits are lost.

**Project-local AI rules go in `CLAUDE-LOCAL.md` at the repo root** — a user-owned file Loom creates once and never overwrites (not even with `--force`). The root `CLAUDE.md` imports both:

```
@.loom/CLAUDE.md
@CLAUDE-LOCAL.md
```

Loom's contract loads first; your local rules load after and can augment or override it.

---

<!-- rule:key-terminology -->
## Key terminology

| Term | Meaning |
|------|---------|
| **Weave** | A project folder under `loom/`. The core domain entity. |
| **Thread** | A workstream subfolder inside a Weave (`loom/{weave}/{thread}/`). Contains idea, design, plans, done docs, chats. |
| **Plan** | An implementation plan doc (`plan-NNN.md`) with a steps table. Lives in `{thread}/plans/`. |
| **Design** | A design doc (`design.md`). Contains the design conversation log. |

Thread layout (flat canonical filenames — identity is the frontmatter ULID, so a folder rename rewrites no doc content): `loom/{weave}/{thread}/idea.md`, `design.md`, `req.md`, `thread.md`, `plans/plan-NNN.md`, `done/plan-NNN-done.md`, `chats/chat-NNN.md`.

---

<!-- rule:mcp-tools -->
## MCP tools

> **MCP host availability:** the Loom MCP server only runs inside hosts that
> implement the MCP client protocol — **Claude Code (CLI), the Claude desktop
> app**, and other MCP-capable agents. The **Claude VS Code extension does NOT
> support MCP today** — sessions running there cannot reach `loom://` resources
> or `loom_*` tools and must fall back to direct file edits (with the
> `⚠️ MCP unavailable — editing file directly` visibility prefix).

<!-- rule:claude-code-config -->
### Claude Code config

Create this as `.mcp.json` in the **project root** (NOT `.claude/settings.json` —
that file is for permissions/hooks/env and ignores `mcpServers`):

```json
{
  "mcpServers": {
    "loom": {
      "type": "stdio",
      "command": "npx",
      "args": ["-y", "@reslava/loom@<version>", "mcp"]
    }
  }
}
```

No `LOOM_ROOT` is set: the server resolves its own workspace root by walking up from
the launch directory to the nearest `.loom/`, so the config is committable and works
whether `claude` is launched from the project root or a subdirectory. (The old
`LOOM_ROOT: "${workspaceFolder}"` was a VS-Code-only editor variable a standalone
terminal `claude` cannot expand.)

Project-scoped MCP servers need a one-time approval per project — run `claude`
interactively in the project root and approve the `loom` server, or use
`claude /mcp` to manage. Verify with `claude mcp list`.

<!-- rule:primary-entry-points -->
### Primary entry points

| Entry point | When to use |
|-------------|-------------|
| `loom://catalog` resource | Grouped index of every `loom_*` tool (name + one-line purpose). **Read it before searching for a tool**, then `ToolSearch select:<exact name>` — it removes the discovery search, not the one-time schema fetch |
| `loom://context/{docUlid}` resource (or `loom://context/thread/{weaveSlug}/{threadSlug}`) | Load the assembled context bundle (global ctx + parent chain + requires_load) for a doc or thread before working on it |
| `do-next-step` prompt | Get the next incomplete step with full context pre-loaded |
| `continue-thread` prompt | Review thread state and get a next-action suggestion |
| `validate-state` prompt | Review diagnostics and identify issues to fix |

<!-- rule:mcp-rules -->
### Rules

- **`loom://catalog` is loaded at session start (step 2) — consult it, never keyword-flail.** MCP tool schemas are deferred, so you only see tool *names* until you fetch them. The catalog (loaded up front) is the grouped name index; find the exact tool in it, then `ToolSearch select:<exact name>` (one targeted fetch). If the catalog is not yet in context when you need a `loom_*` tool, read `loom://catalog` **before** the first `ToolSearch` — a blind `ToolSearch` for a `loom_*` tool (keyword guessing without the catalog) is a rule violation.
- **All writes to `loom/**/*.md` go through MCP tools** — frontmatter, body, state mutations, and prose edits alike (see the "AI session rules" hard rule below for the full breakdown and the gate hook that enforces it).
- Use `loom://context/{docUlid}` (or `loom://context/thread/{weaveSlug}/{threadSlug}`) before starting any thread work. The Unified Context Pipeline bundles global ctx + parent chain + requires_load in a single read.
- `do-next-step` prompt is the primary workflow driver: call it with the active planUlid to get context + step instruction.
- **Plans are structured, never hand-authored tables.** Create a plan with `loom_create_plan` by passing `goal` (prose) + a `steps` array of objects (`{ description, title?, files?, blockedBy?, satisfies?, detail? }`) — **never** a Markdown steps table. Loom owns the canonical `## Steps` table; steps live in YAML frontmatter (the source of truth) and the body table is a generated view. `blockedBy` references step `id`s (or plan ids). `loom_create_plan` does **not** accept a `content` body (idea/design/reference still do).
<!-- rule:single-ai -->
- **Single-AI (by design):** Loom requires **exactly one** AI provider, never two — run it with whatever AI path you have; only one is required. *Primary:* the Loom VS Code extension's AI buttons launch a **Claude Code CLI agent** with a task prompt that writes via content tools (`loom_update_doc` / `loom_create_*`) — no API key, no sampling. *Fallback:* when no Claude CLI is present, the `loom_generate_*` / `loom_refine_*` sampling tools run via a configured `reslava-loom.ai.apiKey`. A user configures one path or the other, never both.
- **`loom_generate_*` / `loom_refine_*` tools use MCP sampling (server→client)** — this is the **fallback** path. Two host behaviors:
  - **VS Code extension (fallback)**: when Claude CLI is absent, the extension advertises `{ sampling: {} }` and routes `sampling/createMessage` through its configured AI API key.
  - **Claude Code CLI sessions**: sampling is intentionally blocked — Claude Code is already the AI; recursive server→client inference returns `MethodNotFound`. **Create docs in a single call:** idea/design/reference take a `content` body; **a plan takes `goal` + a structured `steps` array** (objects, never a Markdown table — see the plan rule above). The doc is born at version 1 with real content. For an existing doc, do the edit yourself and write it via `loom_update_doc`. Never call `loom_refine_*` / `loom_generate_*` here — they'll `MethodNotFound`.
<!-- rule:context-ledger -->
- **Declare what you already hold — don't re-receive it (Context Dispatcher).** The MCP server is stateless and can't see your context window, so context injection dedupes against a ledger you declare. When advancing through several steps of one plan in a session, pass `context: "skip"` (coarse — you hold the whole thread) or `alreadyLoaded: [{ id, version }]` (precise, per-doc) on every `loom_do_step` after the first: the brief then injects only the delta (docs absent from your ledger, or whose version changed) and lists the assumed-present rest in `contextManifest`. The dedupe unit is `{id@version}` — a refine bumps the version, so a changed doc always re-injects (no silent under-load). After a context compaction, or whenever unsure, drop the flag and re-receive the full bundle. The `loom://context` resource takes the same ledger via `?loaded=id@version,…`.

---

<!-- rule:ai-session-rules -->
## AI session rules

> **#1 rule — reply INSIDE the active chat doc.** This is the single most-violated rule. If a chat doc is the active context and you answer only in the terminal, that is a **bug**, not a stylistic choice — the reply is lost the moment the terminal scrolls. Once a chat doc is active, every reply (including short follow-ups) goes inside it via `loom_append_to_chat` until the user says `close` or opens a different chat. See the full rule below.

- **Chat Mode (default):** Respond naturally. Never modify frontmatter or files without explicit approval.
- **Action Mode:** Only when the user explicitly asks. Respond with a JSON proposal per the handshake protocol.
- **Never propose state changes** (version bumps, status transitions) without being asked.
- **Chat docs are the conversation surface (always reply inside).** Whenever a chat doc (`chat-NNN.md`, i.e. `type: chat` in frontmatter) is the active context of the session — the user asked you to read it, opened it in the IDE while discussing it, references a line/section inside it, or the previous turn was already written into it — every reply goes inside that doc, appended at the bottom under `## AI:`. This is not optional and does not require the user to repeat "reply inside" each turn. Once a chat doc is active, keep replying inside it for all follow-ups until the user explicitly says `close` or switches to a different chat doc. The terminal response should be a brief one-liner pointing at the appended reply, not a duplicate of the content.
- **Why this matters:** Chats are Loom's User↔AI collaboration medium and the durable context database. Replies that live only in the terminal disappear; replies inside the chat doc persist as part of the project's shared memory.
- **MCP tools for ALL writes to `loom/**/*.md` (hard rule):** Every write to a Loom doc — frontmatter or body, new doc or existing, state mutation or prose edit — goes through a `loom_*` MCP tool. No exceptions for "small" edits, typo fixes, or appending a single line. If a `loom-mcp-gate` PreToolUse hook is installed in this workspace, direct `Edit`/`Write`/`MultiEdit` to `loom/**/*.md` is **physically blocked**; if you see a deny from the gate, switch to the right MCP tool — don't route around it.
  - Chats → `loom_append_to_chat`
  - New idea/design/plan/done → `loom_create_*` (or `loom_generate_*` if sampling is available)
  - Step progress → `loom_complete_step` / `loom_append_done`
  - Existing doc body or frontmatter → `loom_update_doc`
  - Surgical body-prose edits → `loom_patch_doc` (one-line/section find-and-replace — preferred over re-supplying the whole body via `loom_update_doc`; refuses the generated plan `## Steps` table)
  - Plan step edits → `loom_update_step` (amend a pending step's description/files/blockedBy/satisfies) / `loom_add_step` (insert a step append/before/after) / `loom_remove_step` (delete a pending step; strips blockedBy refs to it) / `loom_reorder_steps` (reorder pending steps); done steps are immutable history
  - Renames/archives → `loom_retitle` (doc title) / `loom_rename_reference_file` (reference filename) / `loom_archive`
  - Excluded from the gate: `loom/refs/*.md`, `loom/.archive/**/*.md`, repo-root `CLAUDE.md`, anything outside `loom/`. Edits to those use normal `Edit`/`Write`.
  - If MCP is genuinely down, output `⚠️ MCP unavailable — editing file directly`, ask the user to disable the gate hook via `/hooks`, and proceed only with explicit go.
- **Treat MCP tool failures as findings, not friction.** If a `loom_*` tool returns the wrong shape, a malformed doc (missing Steps table, double type-suffix, broken frontmatter), or times out — stop, report what happened in the active chat, and let the user decide how to proceed. Routing around a buggy MCP tool by editing the file directly hides the bug.

<!-- rule:human-pointer-context -->
### Human pointer → slug-path context resource (never derive a ULID)

**Whenever the user points you at a doc or thread by name or path — at session start _or_ mid-session** — resolve it through the **slug-path human-pointable context resource**: `loom://context/{weaveSlug}/{threadSlug}/{docSlug}` for a doc (`docSlug` = `idea` / `design` / `req` or a filename stem like `chat-001`; add `?mode=chat` for a chat), or `loom://context/thread/{weaveSlug}/{threadSlug}` for a thread. **Never obtain the ULID yourself** with `bash` / `grep` / `Read` on the file — the returned bundle's header carries `target=…` and `thread_ulid=…`, which you hand to any ULID-strict write tool or workflow prompt (`do-next-step`, `loom_do_step`). The slug-path resource *is* the slug→ULID resolver; deriving the ULID by hand bypasses MCP and is redundant. Once a thread is the **active thread**, point at a single doc with `?scope=doc` (the `read`/`reply` slang path) — it returns only that doc, not the bundle you already hold; the first pointed action of a session loads the full thread (`load`).

<!-- rule:mcp-visibility -->
### MCP visibility (required)

Before any MCP call, output one line:
```
🔧 MCP: loom_tool_name(key="value", ...)
📡 MCP: loom://resource-uri
```

If MCP is unavailable, output:
```
⚠️ MCP unavailable — editing file directly
```

<!-- rule:context-injection -->
### Chat-reply context injection (required)

When replying inside a chat doc that lives in a thread (`loom/{weave}/{thread}/chats/...`):

- **First reply for this thread in the current conversation** — read the thread context (idea + design + active plan + any `requires_load` docs) before responding. Load up front, before you start diagnosing — do not answer from code and backfill the read afterward (that is the "context loaded at the wrong time" failure). Because the user pointed you here by path, use the **slug-path** form (per the *Human pointer → slug-path* hard rule — don't derive the chat ULID by hand). Emit one visibility line per doc:
  ```
  📡 MCP: loom://context/{weaveSlug}/{threadSlug}/{chat-stem}?mode=chat
  📄 idea.md — loaded for context
  📄 design.md — loaded for context
  📄 plan-NNN.md — loaded for context  (only if an active plan exists)
  ```
  (The Unified Context Pipeline assembles global/weave/thread ctx + the chat's parent chain + requires_load; the chat itself is the target. The `loom://context/{chat-ulid}?mode=chat` form is equivalent when you already hold the chat's ULID mid-session.) This first-touch full-bundle read **is** the `load` slang — it sets the pointed thread as the active thread; every later `read`/`reply` on its docs is doc-only (`?scope=doc`). A `reply` naming a chat in a thread you have not loaded this session is refused until you `load` that thread.
- **Same thread, no `refine` / `generate` since last reply** — context is already in the conversation transcript. Do NOT re-read. Emit only the tool-call visibility line:
  ```
  🔧 MCP: loom_append_to_chat(id="{chat-id}")
  ```
- **Same thread, but a `refine` or `generate` ran since last reply** — re-read the context (it may have changed) and re-emit the doc-loaded visibility lines.

To load the chat's own new turns cheaply on first touch, call `loom_read_chat_tail` — it returns only the turns since the last `## AI:` reply (via the chat's `last_ai_block` cursor) instead of re-reading the whole chat.

For a chat at weave root (loose fiber, no thread), load the parent doc(s) the chat refers to and emit `📄 {doc}.md — loaded for context` for each.

The "is this thread already in transcript?" decision lives **in the AI**, not in the MCP server — the server is stateless across calls and cannot see the LLM transcript.

<!-- rule:loom-slang -->
### Loom slang — canonical User→AI verbs

A small set of **loom words** map deterministically to one action, so the user never spells out the tool and the AI never guesses from phrasing. Each fires **only in its trigger context** — outside it the word is ordinary English. Full mappings, triggers, chains, and rejections live in the workspace's `loom/refs/loom-slang-reference.md` if present; the essentials:

- `load {weaveSlug}/{threadSlug}` — load `loom://context/thread/{weaveSlug}/{threadSlug}` (the full thread bundle) and make it the **active thread** (AI-held — the MCP server is stateless). Emit `🧵 Active: {weaveSlug}/{threadSlug}`. The only verb that sets/switches the active thread; the first pointed action of a session implicitly loads.
- `read {docSlug}` *(active thread)* / `read {weaveSlug}/{threadSlug}/{docSlug}` — doc-only: `loom://context/{weaveSlug}/{threadSlug}/{docSlug}?scope=doc` returns **only that doc** (`?mode=chat` for a chat), not the whole thread you already hold. Bare `{docSlug}` resolves in the active thread. After the first touch `read` never auto-loads or switches — a cross-thread read is a pure peek. **If the target is a chat doc with a pending user turn (one after the last `## AI:`), `read` implies `reply`** — flow straight into the reply chain. Reading a non-chat doc, or a chat whose last turn is already `## AI:`, stays load-only.
- `reply` *(a chat doc is active)* — `loom_read_chat_tail` → compose → `loom_append_to_chat`. `reply {chatSlug}` resolves the chat in the active thread; `reply {weaveSlug}/{threadSlug}/{chatSlug}` is honored **only for the active thread** — a non-active thread is refused (`load` it first).
- `do quick` — `loom_quick_ship` (record already-done work; never writes code).
- `code quick` *(a source-code change was agreed in the active chat)* — implement it → build + test + verify → `loom_quick_ship`. Any source touch — or a **test-gated contract file** (a file your project's tests validate, e.g. an agent-contract file checked by a sync test) — is `code quick`, because the record owes a test run.
- `write quick` *(a docs/prose-only change was agreed)* — implement it → `loom_quick_ship`, **no build/test** (a read-through is the check).
- `do step {N}` *(implementing plan)* — resolve the ordinal N to its step id, `loom_do_step` → implement → `loom_append_done` → `loom_complete_step`, then **STOP** (stop-rule 1).
- `do steps {N,M}` / `do steps {N-Z}` / `do plan` *(implementing plan)* — that chain per step, run through without stopping between (the stop-rule 1 explicit-authorization exception; rules 2 & 3 still interrupt).
- `docs done` — `set-status done` on the thread's idea + design + chats; **never** plans (report any plan with pending steps); `req` stays `locked`.

Slang covers only ambiguous or multi-step verbs; a capability with a self-naming command you run by that name. No single-letter aliases.

---

<!-- rule:session-start -->
## Session start protocol

**Order of operations at session start (mandatory, including after conversation compaction):**

1. **Load global ctx** — read `loom/ctx.md`. Emit:
   ```
   📘 loom-ctx loaded — global context ready
   ```
   (or `⚠️ loom-ctx not loaded — proceeding without global context` on failure).
2. **Load the tool catalog** — read the `loom://catalog` resource so the grouped `loom_*` surface index (tools + resources + prompts) is in context *before* any tool is needed. Emit `📡 MCP: loom://catalog` then `🗂️ loom-catalog loaded — surface index ready`. Mandatory and unconditional: it removes the "first `ToolSearch` runs blind" moment that causes the index to be skipped. Once loaded, never `ToolSearch` for a `loom_*` tool without first consulting this index — go straight from catalog → `ToolSearch select:<exact name>`.
3. **Load the project map** — read `loom://state?shape=summary`: the cheap weave/thread skeleton + status (a few KB), **not** the full state graph (every plan's every step). Emit `📡 MCP: loom://state?shape=summary` then `🧵 Active: <active/implementing thread IDs>`. This always-loaded orientation read replaces both the old full-state read and any hand-written active-work pointer — never read the full `loom://state` at session start.
4. **Load only the pointed thread deeply.** When the user pointed you at a chat/doc/thread, that pointer is the active-thread signal — scope the deep load to it via the **slug-path human-pointable resource** (`loom://context/{weaveSlug}/{threadSlug}/{docSlug}`, or `loom://context/thread/{weaveSlug}/{threadSlug}` for the thread's primary doc). This deep load **is** the `load` slang — it sets the active thread; later `read`/`reply` on that thread's docs are doc-only (`?scope=doc`). **Never derive the thread/plan ULID by hand — the bundle header returns it** (see the *Human pointer → slug-path* hard rule). When the thread has an active plan, follow up with `do-next-step` using that returned `planUlid`; the bundle also carries the next incomplete step + a pre-filled `loom_complete_step` call. Do not load other threads' content; with no pointer, use the step-3 map to pick.

After the reads, output this block and **STOP**:

```
📋 Session start
> Active weave:  {weave-id}
> Active plan:   {plan title} — Step {N}  (or "no active plan")
- Next step: {step description}

STOP — waiting for go
```

---

<!-- rule:stop-rules -->
## Non-negotiable stop rules

1. **After each step**: mark ✅ in the plan · state the next step + files that will be touched · **STOP** — wait for `go`. **Exception — explicit multi-step authorization:** when the user explicitly asks for a range or all steps in advance (e.g. "do steps 2–4", "do all remaining steps", "do the whole plan"), continue through the authorized range without stopping between steps. Still mark ✅ as each completes. Rules 2 and 3 continue to interrupt the range — they always stop.
2. **Error loop**: after a 2nd consecutive failed fix — stop, write root-cause findings, wait for `go`.
3. **Design decision**: when a decision affects architecture, API shape, or test design — explain options and trade-offs, **STOP** and wait.
4. **User says "STOP"**: respond with `Stopped.` only — nothing else.

---

<!-- rule:commit-last -->
## Commit last

When a chat turn asks you to commit, the chat reply is part of the work, so it must land **before** the commit. Append your reply to the active chat first (`loom_append_to_chat`), then stage everything — including the chat doc — and commit as the **last** action of the turn. Never put the commit hash in the reply: that forces the reply after the commit and re-dirties the doc — describe what changed, not the commit object (the hash is in `git log`). This keeps a commit from leaving its own trigger chat modified. The next turn legitimately re-dirties the doc; that's expected.

---

<!-- rule:collaboration-style -->
## Collaboration style

- Discuss design before implementing — think out loud and reach better solutions through dialogue.
- When a design question is open, present trade-offs and ask — don't just pick one.
- Do not make any changes until you have 95% confidence. Ask follow-up questions until you reach that confidence.
- Always choose the cleanest, most correct approach. If it requires more work, say so.
