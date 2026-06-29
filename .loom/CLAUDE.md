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
| **Plan** | An implementation plan doc (`*-plan-*.md`) with a steps table. Lives in `{thread}/plans/`. |
| **Design** | A design doc (`*-design.md`). Contains the design conversation log. |

Thread layout: `loom/{weave-id}/{thread-id}/{thread-id}-idea.md`, `{thread-id}-design.md`, `plans/`, `done/`.

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
      "command": "loom",
      "args": ["mcp"],
      "env": {
        "LOOM_ROOT": "${workspaceFolder}"
      }
    }
  }
}
```

Project-scoped MCP servers need a one-time approval per project — run `claude`
interactively in the project root and approve the `loom` server, or use
`claude /mcp` to manage. Verify with `claude mcp list`.

<!-- rule:primary-entry-points -->
### Primary entry points

| Entry point | When to use |
|-------------|-------------|
| `loom://catalog` resource | Grouped index of every `loom_*` tool (name + one-line purpose). **Read it before searching for a tool**, then `ToolSearch select:<exact name>` — it removes the discovery search, not the one-time schema fetch |
| `loom://context/{docId}` resource (or `loom://context/thread/{weaveId}/{threadId}`) | Load the assembled context bundle (global/weave/thread ctx + parent chain + requires_load) for a doc or thread before working on it |
| `do-next-step` prompt | Get the next incomplete step with full context pre-loaded |
| `continue-thread` prompt | Review thread state and get a next-action suggestion |
| `validate-state` prompt | Review diagnostics and identify issues to fix |

<!-- rule:mcp-rules -->
### Rules

- **`loom://catalog` is loaded at session start (step 2) — consult it, never keyword-flail.** MCP tool schemas are deferred, so you only see tool *names* until you fetch them. The catalog (loaded up front) is the grouped name index; find the exact tool in it, then `ToolSearch select:<exact name>` (one targeted fetch). If the catalog is not yet in context when you need a `loom_*` tool, read `loom://catalog` **before** the first `ToolSearch` — a blind `ToolSearch` for a `loom_*` tool (keyword guessing without the catalog) is a rule violation.
- **All writes to `loom/**/*.md` go through MCP tools** — frontmatter, body, state mutations, and prose edits alike (see the "AI session rules" hard rule below for the full breakdown and the gate hook that enforces it).
- Use `loom://context/{docId}` (or `loom://context/thread/{weaveId}/{threadId}`) before starting any thread work. The Unified Context Pipeline bundles global/weave/thread ctx + parent chain + requires_load in a single read.
- `do-next-step` prompt is the primary workflow driver: call it with the active planId to get context + step instruction.
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
- **Chat docs are the conversation surface (always reply inside).** Whenever a chat doc (any file matching `*-chat.md` or `*-chat-NNN.md`, i.e. `type: chat` in frontmatter) is the active context of the session — the user asked you to read it, opened it in the IDE while discussing it, references a line/section inside it, or the previous turn was already written into it — every reply goes inside that doc, appended at the bottom under `## AI:`. This is not optional and does not require the user to repeat "reply inside" each turn. Once a chat doc is active, keep replying inside it for all follow-ups until the user explicitly says `close` or switches to a different chat doc. The terminal response should be a brief one-liner pointing at the appended reply, not a duplicate of the content.
- **Why this matters:** Chats are Loom's User↔AI collaboration medium and the durable context database. Replies that live only in the terminal disappear; replies inside the chat doc persist as part of the project's shared memory.
- **MCP tools for ALL writes to `loom/**/*.md` (hard rule):** Every write to a Loom doc — frontmatter or body, new doc or existing, state mutation or prose edit — goes through a `loom_*` MCP tool. No exceptions for "small" edits, typo fixes, or appending a single line. If a `loom-mcp-gate` PreToolUse hook is installed in this workspace, direct `Edit`/`Write`/`MultiEdit` to `loom/**/*.md` is **physically blocked**; if you see a deny from the gate, switch to the right MCP tool — don't route around it.
  - Chats → `loom_append_to_chat`
  - New idea/design/plan/done → `loom_create_*` (or `loom_generate_*` if sampling is available)
  - Step progress → `loom_complete_step` / `loom_append_done`
  - Existing doc body or frontmatter → `loom_update_doc`
  - Surgical body-prose edits → `loom_patch_doc` (one-line/section find-and-replace — preferred over re-supplying the whole body via `loom_update_doc`; refuses the generated plan `## Steps` table)
  - Plan step edits → `loom_update_step` (amend a pending step's description/files/blockedBy/satisfies) / `loom_add_step` (insert a step append/before/after) / `loom_remove_step` (delete a pending step; strips blockedBy refs to it) / `loom_reorder_steps` (reorder pending steps); done steps are immutable history
  - Renames/archives → `loom_rename` / `loom_archive`
  - Excluded from the gate: `loom/refs/*.md`, `loom/.archive/**/*.md`, repo-root `CLAUDE.md`, anything outside `loom/`. Edits to those use normal `Edit`/`Write`.
  - If MCP is genuinely down, output `⚠️ MCP unavailable — editing file directly`, ask the user to disable the gate hook via `/hooks`, and proceed only with explicit go.
- **Treat MCP tool failures as findings, not friction.** If a `loom_*` tool returns the wrong shape, a malformed doc (missing Steps table, double type-suffix, broken frontmatter), or times out — stop, report what happened in the active chat, and let the user decide how to proceed. Routing around a buggy MCP tool by editing the file directly hides the bug.

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

- **First reply for this thread in the current conversation** — read the thread context (idea + design + active plan + any `requires_load` docs) before responding. Emit one visibility line per doc:
  ```
  📡 MCP: loom://context/{chat-id}?mode=chat
  📄 {thread}-idea.md — loaded for context
  📄 {thread}-design.md — loaded for context
  📄 {plan-id}.md — loaded for context  (only if an active plan exists)
  ```
  (The Unified Context Pipeline assembles global/weave/thread ctx + the chat's parent chain + requires_load; the chat itself is the target.)
- **Same thread, no `refine` / `generate` since last reply** — context is already in the conversation transcript. Do NOT re-read. Emit only the tool-call visibility line:
  ```
  🔧 MCP: loom_append_to_chat(id="{chat-id}")
  ```
- **Same thread, but a `refine` or `generate` ran since last reply** — re-read the context (it may have changed) and re-emit the doc-loaded visibility lines.

To load the chat's own new turns cheaply on first touch, call `loom_read_chat_tail` — it returns only the turns since the last `## AI:` reply (via the chat's `last_ai_block` cursor) instead of re-reading the whole chat.

For a chat at weave root (loose fiber, no thread), load the parent doc(s) the chat refers to and emit `📄 {doc}.md — loaded for context` for each.

The "is this thread already in transcript?" decision lives **in the AI**, not in the MCP server — the server is stateless across calls and cannot see the LLM transcript.

---

<!-- rule:session-start -->
## Session start protocol

**Order of operations at session start (mandatory, including after conversation compaction):**

1. **Load global ctx** — read `loom/ctx.md`. Emit:
   ```
   📘 loom-ctx loaded — global context ready
   ```
   (or `⚠️ loom-ctx not loaded — proceeding without global context` on failure).
2. **Load the tool catalog** — read the `loom://catalog` resource so the grouped `loom_*` tool index is in context *before* any tool is needed. Emit `📡 MCP: loom://catalog` then `🗂️ loom-catalog loaded — tool index ready`. Mandatory and unconditional: it removes the "first `ToolSearch` runs blind" moment that causes the index to be skipped. Once loaded, never `ToolSearch` for a `loom_*` tool without first consulting this index — go straight from catalog → `ToolSearch select:<exact name>`.
3. **Read active work from MCP** — `loom://state?status=active,implementing`. Emit `🧵 Active: <thread IDs>`. MCP is the only source of truth — do not maintain a hand-written active-work pointer.
4. **Call `do-next-step` prompt** with the active planId. Bundles thread context (idea, design, current plan, requires_load docs), the next incomplete step, and a pre-filled `loom_complete_step` call.

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

<!-- rule:collaboration-style -->
## Collaboration style

- Discuss design before implementing — think out loud and reach better solutions through dialogue.
- When a design question is open, present trade-offs and ask — don't just pick one.
- Do not make any changes until you have 95% confidence. Ask follow-up questions until you reach that confidence.
- Always choose the cleanest, most correct approach. If it requires more work, say so.
