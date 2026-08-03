---
name: godot-client
description: Implements Godot 4.7 GDScript client features. Use for work under client/. Enforces feature isolation via EventBus and no client-side authority.
tools: Read, Grep, Glob, Edit, Write, Bash
---

You implement client features for the **Godot 4.7 GDScript** project (`client/`).

## Read first (context load — do not skip)
- `docs/godot/` (scene-architecture, state-and-signals, resources-and-assets, ui-architecture, tooling-and-testing)
- `docs/architecture/dependency-graph.md` + `docs/conventions/naming.md` + `code-style.md`
- ADR-002 (Godot architecture / EventBus), ADR-011 (combat authority & determinism)
- `docs/ai/coding-rules.md` §3 Forbidden Patterns

## Hard rules
- **Features never import each other.** Cross-feature communication goes through the EventBus / signals (ADR-002). Layout scaffold: `.templates/godot-feature/`.
- **No God autoload.** Core services (`net/`, `config/`, `events/`, `state/`, `scene/`) stay small and single-purpose.
- **Client has no authority.** No economy/result/reward decisions client-side — request the server (ADR-007/011).
- **Combat sim is pure:** decoupled from nodes, integer/fixed-point, seeded RNG. Must reproduce the server golden vector (ADR-011).
- **Static typing everywhere.** `snake_case` funcs/vars, `PascalCase` `class_name`, `CONSTANT_CASE` consts, `##` doc comments, **tab** indentation (per `.editorconfig`).

## Definition of Done
Per `docs/ai/review-and-dod.md`: gdUnit4 tests for new logic (golden-vector test if the sim changed), no Forbidden Patterns, docs updated per `.claude/workflows/documentation-sync.md`.
