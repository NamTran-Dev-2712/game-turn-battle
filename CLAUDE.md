# CLAUDE.md — AI Execution Entry Point

> Auto-loaded every session by Claude Code. This file is the **execution layer**: it does
> **not** restate project knowledge — it points to the Source of Truth (`docs/`) and to the
> workflows you must follow. If anything here conflicts with `docs/adr/` or `docs/mvp/`,
> **the docs win** and this file is wrong — flag it.

## 1. What this repo is (1-paragraph orientation)

A long-term, data-driven mobile turn-battle RPG. **Godot 4.7 client** (`client/`) + **.NET 9
Clean-Architecture backend** (`server/`) + shared contracts (`shared/`). Combat is
**server-authoritative and deterministic**. The project is documentation-first: `docs/` is the
Single Source of Truth (SSOT), and code is written to match it — never the other way around.
Current phase: **Bootstrap → Core Framework** (see `docs/audit/bootstrap-audit.md`).

## 2. Golden Rules (non-negotiable)

1. **SSOT supremacy.** Business truth lives in `docs/mvp/`; architecture truth in `docs/adr/`
   + `docs/architecture/`. Do not invent requirements or reverse a decision — read the source.
2. **ADR wins on conflict.** On any architectural disagreement, the relevant ADR is final.
   Need a new decision? Propose an ADR (`docs/adr/README.md`), don't decide silently.
3. **Server-authoritative.** No sensitive/economy/result decision on the client — ADR-007, ADR-011.
4. **Data-driven.** No hardcoded gameplay balance numbers — config + Configuration Service, ADR-004/005.
5. **Deterministic combat.** Integer/fixed-point + seeded RNG in the sim — ADR-011.
6. **When unsure, don't guess.** Ambiguity → record in `docs/mvp/10-open-questions.md` and ask.

Full rules + the **Forbidden Patterns** table: **`docs/ai/coding-rules.md`** (read §3 before writing code).

## 3. Before writing code — mandatory context load

Follow the order in **`docs/ai/context-strategy.md`** §2:

1. Task goal + acceptance criteria →
2. Relevant business SSOT (specific `docs/mvp/` file, not all) →
3. Relevant ADR(s) →
4. Module boundary + conventions (`docs/architecture/dependency-graph.md`, `docs/conventions/`) →
5. Existing module code (reuse before writing new) →
6. Start **small, with a test**.

## 4. Execution layer (this repo's `.claude/` + dotfolders)

| Need | Go to |
|---|---|
| How to implement / review / keep docs synced | `.claude/workflows/` |
| Startup / pre-response / commit / self-review gates | `.claude/checklists/` |
| Reusable prompt for a feature/bugfix/refactor/etc. | `.prompts/` |
| Code/doc scaffolds (no gameplay logic) | `.templates/` |
| Pre-assembled context package for a task area | `.context/` |
| Quick rule reminder (canonical stays in docs) | `.rules/rules.md` |
| Scope-specific hints (backend/client/config/combat) | `.instructions/` |
| Project decision log (below ADR level) | `.memory/` |
| Cross-session task hand-off | `.tasks/` |
| Repo-tuned specialist agents | `.claude/agents/` (charters in `.agents/`) |

## 4.5 Phase-Execution Protocol (roadmap work is a contract)

When the task is a **roadmap phase** (`docs/roadmap/NN-*.md`), the Strict Phase Gate in
**`docs/roadmap/README.md` §4–§5** is binding. Do not restate it — apply it:

1. **One phase per session.** Execute the lowest un-closed phase whose prerequisites are Closed.
   Do not skip, reorder, or bundle phases.
2. **Read the whole phase file first**, plus every doc it links (ADR / testing / deployment /
   architecture). Inspect the existing implementation before changing it.
3. **The phase's `# Công việc cần thực hiện` list is the execution contract.** Implement it item by
   item; never invent requirements outside the documented scope.
4. **`- [ ]` → `- [x]` only after the item is implemented AND verified** — with evidence from a run
   (build/test/CI output, a negative test, an artifact). Never check an item on intent, or because
   code merely exists. For a gate that **only runs on CI** (Godot headless, Docker build with no local
   daemon, a `v*`-tag release), the evidence is the **Actions result** — until you have it, keep the
   item `[ ]` ("CI-verification pending") and don't self-certify.
5. **Run the phase's negative / failure-path tests** where specified (e.g. inject a dependency-rule
   leak, confirm the gate fails), then **fully revert** the temporary change and re-verify green.
6. **Never implement future-phase scope.** Future gates stay explicit placeholders pointing at their
   owning phase.
7. **Before declaring the phase done:** re-read the phase file, run the completeness audit, confirm
   every checklist item is `[x]` with evidence and no `TODO`/blocker remains (Gate condition set).
   An unchecked item means the phase is **not** complete.

## 5. Definition of Done & the update policy

- A change is **Done** only per `docs/ai/review-and-dod.md` §4 (acceptance met, review checklist
  passed, tests + CI green, no Forbidden Patterns, docs updated, PR links SSOT/ADR).
- **Mandatory doc-sync:** any change that alters architecture, dependencies, config schema, or
  public behavior **must** update the affected docs in the same change. Run the change→doc-impact
  matrix in **`.claude/workflows/documentation-sync.md`** before declaring done.

## 6. Language

Repository docs and folder READMEs are **Vietnamese** (per `docs/conventions/`). The AI execution
layer (`CLAUDE.md`, `.claude/`, `.prompts/`, `.templates/`, `.context/`, `.rules/`,
`.instructions/`) is written in **English**. Keep each side in its language when editing.
