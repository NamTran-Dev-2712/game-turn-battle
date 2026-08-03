# Workflow: Review

> How to review a change before merge. The authoritative checklist is `docs/ai/review-and-dod.md`
> — this file is the operating procedure around it. For an agent-driven review use `.claude/agents/reviewer`.

## 1. Frame the change
- What is the acceptance criterion (task/phase in `docs/roadmap/`)? Does the diff meet it and nothing more?
- Which ADR(s) / `docs/mvp/` sections does it touch? Is the PR linked to them?

## 2. Apply the Review Checklist (`docs/ai/review-and-dod.md` §1)
- **Architecture & boundaries:** dependency rule holds; no reverse deps; no God object; no
  cross-feature client imports (EventBus only); Domain pure (no EF/HTTP/`DateTime.Now`).
- **Data-driven & server authority:** no hardcoded gameplay config (ADR-004); sensitive decisions
  server-side (ADR-007/011); combat integer/fixed-point + seeded RNG if the sim was touched.
- **Quality:** naming per `docs/conventions/naming.md` + glossary; small functions; no magic numbers;
  edge/error handling; no secrets in code.
- **Test & CI:** new logic tested (combat/economy especially); golden vector updated if sim changed;
  config validated if schema changed; CI fully green.
- **Traceability:** PR links ADR/`docs/mvp/*`; open-questions recorded.

## 3. Check for Forbidden Patterns
Scan against `docs/ai/coding-rules.md` §3. Any hit is a blocker.

## 4. Check doc-sync
Did the change update the docs its change→doc-impact row requires
(`.claude/workflows/documentation-sync.md`)? Missing doc updates block "Done".

## 5. Verdict
List findings most-severe first (file:line + rule). Either confirm DoD met (§4) or list exactly
what blocks merge. Prefer root-cause fixes over symptom patches.
