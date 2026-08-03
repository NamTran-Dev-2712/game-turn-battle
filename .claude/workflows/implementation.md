# Workflow: Implementation

> How to take a task from request to "Done" in this repo. Canonical rules: `docs/ai/coding-rules.md`,
> `docs/ai/context-strategy.md`, `docs/ai/review-and-dod.md`. This is the operational sequence.

## 0. Precondition
Run `.claude/checklists/startup.md` once per session. One task = one goal with clear acceptance.

## 1. Load context (in this order — `docs/ai/context-strategy.md` §2)
1. Task goal + acceptance criteria.
2. Business SSOT: the **specific** `docs/mvp/` file(s), not the whole folder.
3. Relevant ADR(s) (`docs/adr/`).
4. Module boundary + conventions (`docs/architecture/dependency-graph.md`, `docs/conventions/`,
   plus the module doc: `docs/backend/`, `docs/godot/`, or `docs/gameplay/`).
5. Existing module code — **reuse before writing new**.

If a required decision is missing or ambiguous → add to `docs/mvp/10-open-questions.md` and ask.
**Do not guess.**

## 2. Plan
- State the smallest change that satisfies acceptance. Identify the exact files/modules touched and
  the ones that must **not** be touched (boundaries).
- Pick a scaffold from `.templates/` if one fits; pick a prompt from `.prompts/` if delegating.
- For non-trivial work, delegate to the right agent: `.claude/agents/dotnet-backend`,
  `godot-client`, or `combat-determinism`.

## 3. Implement
- Small, incremental edits. Match surrounding code's idiom, naming, comment density.
- Obey the Golden Rules (CLAUDE.md §2) and Forbidden Patterns (`docs/ai/coding-rules.md` §3):
  no God objects, no hardcoded balance, no floats/global RNG in combat, no client authority,
  no Domain framework deps, no swallowed errors.
- Write the test alongside the code — especially for combat/economy/save logic.

## 4. Validate
- Backend: `dotnet test server/GameTeam.sln` green (includes NetArchTest dependency-rule gate).
- Client: gdUnit4 tests; golden-vector test if the sim changed.
- Config change: schema + referential validation.

## 5. Self-review & docs
- Run `.claude/checklists/self-review.md`.
- Run `.claude/workflows/documentation-sync.md` — update every doc the change impacts.

## 6. Done
Meet `docs/ai/review-and-dod.md` §4 before declaring done. Then `.claude/checklists/commit.md`.
