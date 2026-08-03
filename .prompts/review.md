# Prompt: Review a Change

```
Review <PR / diff / files> for merge-readiness.

APPLY
- docs/ai/review-and-dod.md §1 (Review Checklist) and §4 (Definition of Done) verbatim.
- Forbidden Patterns scan: docs/ai/coding-rules.md §3.
- Dependency rule: docs/architecture/dependency-graph.md.
- Doc-sync: did it update the docs its change→doc-impact row requires?

OUTPUT
- Findings most-severe first, each with file:line and the rule violated.
- Verdict: DoD met, or the exact list of blockers.
- Prefer root-cause fixes over symptom patches; don't rewrite the feature yourself.
```

Agent: `.claude/agents/reviewer`. Workflow: `.claude/workflows/review.md`.
