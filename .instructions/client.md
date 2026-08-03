# Instructions: Client scope (`client/`)

Short execution hints. Canonical design: `docs/godot/`. Agent: `.claude/agents/godot-client`.

- Godot 4.7, GDScript, static typing everywhere.
- Features never import each other — EventBus/signals only (ADR-002); no God autoload.
- No client-side authority (economy/result/reward) — call the server (ADR-007/011).
- Naming: `snake_case` members, `PascalCase` `class_name`, `CONSTANT_CASE` consts, `##` docs; **TAB** indent (`.editorconfig`).
- Feature layout: `.templates/godot-feature/`.
- Combat sim: pure/node-decoupled, integer/fixed-point, seeded RNG; match server golden vector (ADR-011).
- Tests: gdUnit4 under `client/tests/`.
