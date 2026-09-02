# Agent Roles — Charter

> Human-readable charter for the specialized AI roles used in this repo. The **executable**
> counterparts (for Claude Code) live in `.claude/agents/*.md`; keep the two in sync (edit both when a
> role's scope changes — `.claude/workflows/documentation-sync.md`, "AI dotfolder" row).

## Shared boundaries (all roles)
- The **SSOT wins**: no role edits `docs/mvp/` or an Accepted ADR to match code. Contradictions →
  fix the code or propose a new ADR.
- No role introduces Forbidden Patterns (`docs/ai/coding-rules.md` §3) or violates the dependency rule.
- Ambiguity → `docs/mvp/10-open-questions.md`, never a silent guess.
- **Anti-self-invention:** search the repo first; prefer an existing decision → ADR → canonical gameplay doc.
  Never invent a gameplay/balance rule or silently resolve an open question — mark it `[ĐỀ XUẤT]`/`[OPEN]`/`TBD` and
  never promote a proposal to canon without approval.
- **Completion workflow (every task — canon in CLAUDE.md §4.5/§4.6):** read the phase requirement + its
  source-of-truth → implement/document in scope → check consistency with ADRs and across docs → run the fitting
  validation/self-review → tick the checklist `[x]` **only after** verification passes (leave `[ ]` with a written
  reason if blocked/out-of-scope) → sync every affected downstream/upstream doc (doc-sync matrix) → audit the whole
  checklist before declaring done. "Memory through documentation" — do not rely on model memory.

## Roles

| Role | Purpose | Executable agent |
|---|---|---|
| **Architect** | Turn ideas into architecture-safe blueprints; propose ADRs. Design only, no code. | harness `architect-agent`; prompt `.prompts/architecture-adr.md` |
| **Planner** | Break work into staged, testable steps with explicit dependencies. | harness `planner-agent` |
| **Backend coder** | Implement .NET CQRS features within the dependency rule. | `.claude/agents/dotnet-backend.md` |
| **Client coder** | Implement Godot features with EventBus isolation, no client authority. | `.claude/agents/godot-client.md` |
| **Combat guardian** | Protect deterministic, server-authoritative combat + golden vectors. | `.claude/agents/combat-determinism.md` |
| **Reviewer** | Enforce Review Checklist + DoD + Forbidden-Pattern scan before merge. | `.claude/agents/reviewer.md` |
| **Docs-sync** | Keep docs in lockstep with implementation (the update policy). | `.claude/agents/docs-sync.md` |
| **Debugger** | Reproduce → isolate → root-cause fix with a regression test. | harness `debug-agent`; prompt `.prompts/bugfix.md` |

Each executable agent file states its own "read first" context load and hard rules.
