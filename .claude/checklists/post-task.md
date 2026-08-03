# Checklist: Post-Task (the update policy gate)

Run at the end of every task that changed the repo. This is where the **mandatory update policy**
is enforced — a task is not Done until this passes.

- [ ] **Definition of Done** met — `docs/ai/review-and-dod.md` §4 (all 7 points).
- [ ] **Doc-sync matrix run** — every row triggered by this change updated
      (`.claude/workflows/documentation-sync.md`). Canonical updated, derived docs still just link it.
- [ ] **No dead links** introduced in any doc you edited.
- [ ] **New decision?** → captured as an ADR, not left in prose or in your head.
- [ ] **New ambiguity?** → in `docs/mvp/10-open-questions.md`.
- [ ] **Status moved?** → reflected in `docs/audit/bootstrap-audit.md` / `docs/roadmap/`.
- [ ] **Hand-off:** if work is unfinished, leave a note in `.tasks/` for the next session.
- [ ] **Workspace improved?** If you hit friction a future session would also hit, improve the
      relevant `.claude/` / `.prompts/` / `.instructions/` file now (the self-maintaining goal).
