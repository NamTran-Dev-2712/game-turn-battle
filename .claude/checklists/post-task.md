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
- [ ] **Roadmap phase? Strict Phase Gate** (`docs/roadmap/README.md` §5, `CLAUDE.md` §4.5): every
      `# Công việc cần thực hiện` item is `[x]` **with run evidence**, negative/failure tests done and
      **reverted**, no future-phase scope leaked in, no open `TODO`. An unchecked item ⇒ not Done.
- [ ] **CI-only gate? Do not self-certify.** When an item can only be exercised on a CI runner
      (e.g. Godot headless import / gdUnit4 / a Docker build with the daemon off / a `v*`-tag draft
      release) and you cannot run it locally, **leave it `[ ]`** annotated "GitHub-verification pending"
      until you have the actual Actions run result. Separate **locally verified** from **CI-pending**
      in your report; never flip a box on "the YAML/command looks correct". (Lesson: Phase 03.)
- [ ] **Hand-off:** if work is unfinished, leave a note in `.tasks/` for the next session.
- [ ] **Workspace improved?** If you hit friction a future session would also hit, improve the
      relevant `.claude/` / `.prompts/` / `.instructions/` file now (the self-maintaining goal).
