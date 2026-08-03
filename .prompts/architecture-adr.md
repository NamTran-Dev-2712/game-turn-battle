# Prompt: Propose an Architecture Decision (ADR)

Use when a decision is needed that isn't already in `docs/adr/`. Do not decide silently in code.

```
Draft ADR-<next number>: <title>.

CONTEXT
- Problem / forces: <what makes this a decision, not an obvious default>
- Business drivers: docs/mvp/<file(s)>
- Related ADRs: <…>   Related modules: <…>

REQUIRED (follow docs/adr/README.md template exactly)
- ## Context  ## Decision  ## Alternatives  ## Trade-offs  ## Consequences
- Status: Proposed (until accepted).

AFTER DRAFTING
- Index it in DECISIONS.md and the ADR catalog (docs/adr/README.md).
- If it supersedes an existing ADR, mark that one Superseded and fix cross-refs.
- Do NOT implement against it until it is Accepted.
```

Agent: the `architect-agent` (design only, no code).
