# Quick Rules (generated summary)

> **Canonical source: [`docs/ai/coding-rules.md`](../docs/ai/coding-rules.md).** This is a fast-load
> checklist of headings only — it defines **no** new rules. If this and the canonical doc ever
> disagree, the canonical doc wins and this file is stale. Keep it in sync via
> `.claude/workflows/documentation-sync.md` (the "AI workflow/rules" row).

## Coding rules ([full](../docs/ai/coding-rules.md) §2)
- Obey the dependency rule ([dependency-graph](../docs/architecture/dependency-graph.md)).
- Data-driven — no hardcoded balance (ADR-004).
- SRP + small functions; composition over inheritance.
- Server-authoritative — no sensitive decisions on client (ADR-007/011).
- Deterministic combat — integer/fixed-point + seeded RNG (ADR-011).
- Reuse before writing new; name per [glossary](../docs/mvp/12-glossary.md).
- Test alongside code, especially combat/economy.
- Ambiguity → [open-questions](../docs/mvp/10-open-questions.md), don't guess.

## Forbidden Patterns ([full table](../docs/ai/coding-rules.md) §3)
God object · switch/if sprawl to extend gameplay · hardcoded gameplay config · float in combat sim ·
global RNG in sim · client decides sensitive results · Domain depends on EF/HTTP/framework ·
cross-feature client import · `DateTime.Now` in logic · swallowed errors · ad-hoc dependencies ·
config-file reads inside Domain/Application.

## Definition of Done ([full](../docs/ai/review-and-dod.md) §4)
Acceptance met · review checklist passed · tests + CI green · no Forbidden Patterns · docs synced ·
PR links SSOT/ADR · no unrecorded open question.
