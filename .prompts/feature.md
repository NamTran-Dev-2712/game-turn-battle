# Prompt: Implement a Feature

Copy, fill the `<…>` slots, and paste. Every slot maps to a Context Package element
(`docs/ai/context-strategy.md` §1). Leave no slot as a guess — if you can't fill one, that's an
open question.

```
Implement <feature name> for the <backend | Godot client> under <path>.

GOAL & ACCEPTANCE
- Goal: <one sentence>
- Acceptance: <testable criteria; what "works" means>
- Out of scope / do NOT touch: <boundaries>

BUSINESS SSOT (do not invent requirements)
- docs/mvp/<file(s)>  ·  glossary terms: <docs/mvp/12-glossary.md entries>

ARCHITECTURE
- Relevant ADR(s): <docs/adr/ADR-xxx>
- Module boundary: docs/architecture/dependency-graph.md + <module doc: docs/backend|godot|gameplay/...>
- Conventions: docs/conventions/

CONSTRAINTS (this repo's non-negotiables)
- Server-authoritative, data-driven, deterministic combat where relevant.
- No Forbidden Patterns (docs/ai/coding-rules.md §3).
- Reuse existing code/patterns before writing new.

DELIVERABLE
- Smallest change that meets acceptance, with tests.
- Follow .claude/workflows/implementation.md; finish with .claude/checklists/post-task.md.
```

Agent to delegate to: `.claude/agents/dotnet-backend` or `godot-client`. Scaffold: `.templates/`.
