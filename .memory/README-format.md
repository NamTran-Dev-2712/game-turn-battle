# Decision Log — Entry Format

> Project decision log for choices **below ADR level** (workflow, tooling, workspace conventions).
> Architectural decisions go to `docs/adr/`, business decisions to `docs/mvp/`, unresolved questions
> to `docs/mvp/10-open-questions.md`. This log captures the "why" behind smaller, durable choices so
> future sessions don't re-litigate them.

One file per decision: `NNNN-short-slug.md`. Template:

```
# NNNN — <decision title>

- Date: <YYYY-MM-DD>
- Scope: <workflow | tooling | workspace | process>
- Status: Active | Superseded by <NNNN>

## Decision
<what was decided, one paragraph>

## Why
<forces / rationale — the part that's expensive to re-derive>

## Not this
<the main alternative and why it lost>
```

Keep entries short. If a decision grows architectural, promote it to an ADR and mark this Superseded.
