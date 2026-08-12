# Instructions: Client scope (`client/`)

Short execution hints. Canonical design: `docs/godot/`. Agent: `.claude/agents/godot-client`.

- Godot 4.7, GDScript, static typing everywhere.
- Features never import each other — EventBus/signals only (ADR-002); no God autoload.
- No client-side authority (economy/result/reward) — call the server (ADR-007/011).
- Naming: `snake_case` members, `PascalCase` `class_name`, `CONSTANT_CASE` consts, `##` docs; **TAB** indent (`.editorconfig`).
- Feature layout: `.templates/godot-feature/`.
- Combat sim: pure/node-decoupled, integer/fixed-point, seeded RNG; match server golden vector (ADR-011).
- Tests: gdUnit4 under `client/tests/`.
- **Contract models are generated (Phase 08 — closed & verified).** DTO/enum read-models live in
  `client/src/data/generated/` (`AUTO-GENERATED — DO NOT EDIT`), produced by `shared/codegen` from
  `shared/contracts/openapi.json`. **Never hand-edit them; never re-declare a client DTO by hand.** To change one:
  edit `server/GameTeam.Contracts` → rebuild (regenerate `openapi.json`) → `bash shared/codegen/run.sh` → commit the
  generated diff (CI `codegen-check.yml` fails on drift). Enums keep C# numeric values; wire is string. Config-driven
  `.tres` Resource models (from `shared/config-schema`) are a **separate** family — see `docs/godot/resources-and-assets.md`.
