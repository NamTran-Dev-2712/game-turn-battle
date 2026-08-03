# Prompt: Refactor (behavior-preserving)

```
Refactor <target> to <goal: reduce coupling / extract X / remove dead code / simplify>.

INVARIANT
- Behavior must NOT change. Name the tests that prove it (add characterization tests first if missing).

RULES (docs/ai/review-and-dod.md §2)
- Separate from feature work — this is its own change/PR.
- Improve along boundaries; never "refactor" across a boundary the dependency rule forbids.
- Delete dead code and dead flags.
- Small and incremental — no big-bang rewrite.

DONE
- Same tests green before and after (regression proof). No public-behavior change.
- Docs updated only if a boundary or public contract moved.
```
