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

## Established infrastructure (closed & verified — reuse, don't reinvent)
- **Core autoloads (Phase 14).** `EventBus` (`src/core/events/event_bus.gd`) + `SceneRouter`
  (`src/core/scene/scene_router.gd`) are the two independent core autoloads (registered in `client/project.godot`).
  Cross-feature events go through **`EventBus.emit/subscribe/unsubscribe`**; every event is a **declared, documented
  catalogue signal** (`EVENTS` + `signal <name>(payload)` + the table in `docs/godot/state-and-signals.md` §3.1) — no
  "God channel"/undocumented events. Navigate **only** via **`SceneRouter.goto_scene(path)`/`back()`** (never scatter
  `get_tree().change_scene*`); old scenes are `queue_free`d. Autoload scripts omit `class_name` (singleton-name
  collision) — use the global (`EventBus.emit(...)`). Never merge the two into one manager. Canonical:
  `docs/godot/state-and-signals.md` §3.1 + `docs/godot/scene-architecture.md` §4.1; decision log
  `.memory/0012-client-autoloads-standardized.md`.

## Definition of Done
Per `docs/ai/review-and-dod.md`: gdUnit4 tests for new logic (golden-vector test if the sim changed), no Forbidden Patterns, docs updated per `.claude/workflows/documentation-sync.md`.
