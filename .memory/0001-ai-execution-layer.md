# 0001 — AI execution layer separate from docs SSOT

- Date: 2026-08-03
- Scope: workspace
- Status: Active

## Decision
Introduce a thin **AI execution layer** — root `CLAUDE.md` + `.claude/` (settings, agents,
workflows, checklists) + additive `.prompts/`, `.templates/`, `.context/` — plus thin pointer
layers (`.rules/`, `.instructions/`, `.memory/`, `.tasks/`, `.agents/`). This layer **references**
`docs/` and never copies its content.

## Why
`docs/` is a disciplined SSOT built on "index, don't repeat." But its guidance lived in
`AI_GUIDE.md` + `docs/ai/`, which Claude Code does **not** auto-load — so the AI started each session
uninformed. Wiring an execution layer into the files the tool *does* auto-load (`CLAUDE.md`,
`.claude/`) makes every session start with the golden rules, context-load order, and doc-sync policy,
without duplicating the SSOT (which would create drift).

## Not this
Fully populating all nine dotfolders with standalone content was rejected: it would duplicate
`docs/ai` + conventions + ADRs, create a second source of truth, and demand sync tooling to stay
honest — contradicting the repo's own governing principle.
