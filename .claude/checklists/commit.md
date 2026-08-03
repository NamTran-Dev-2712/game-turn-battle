# Checklist: Commit

Only commit/push when the user asks. When you do:

- [ ] On a **feature branch** (never commit directly to `main`).
- [ ] No **secrets** staged — `.env`, `*.pem`, `*.key`, `*.keystore`, `*.p12` are rejected by
      `.githooks/pre-commit`; don't work around it.
- [ ] Self-review passed (`.claude/checklists/self-review.md`) and CI-relevant tests are green locally.
- [ ] Docs synced (`.claude/workflows/documentation-sync.md`).
- [ ] **Conventional commit** message per `docs/conventions/git-conventions.md`; body explains **WHY**
      and links the ADR / `docs/mvp/*`.
- [ ] PR (when opened) fills the DoD template in `.github/pull_request_template.md`.
- [ ] Commit message ends with the required co-author trailer.
