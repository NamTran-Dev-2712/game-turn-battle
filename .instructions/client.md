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
- **Core autoloads are standardized (Phase 14 — closed & verified).** `EventBus`
  (`src/core/events/event_bus.gd`) and `SceneRouter` (`src/core/scene/scene_router.gd`) are the two independent core
  autoloads (registered in `client/project.godot`). **Reuse them — never re-declare a bus/router, never merge them into
  a God autoload.** Cross-feature communication goes through **`EventBus.emit/subscribe/unsubscribe`**; every event must
  be a **declared, documented catalogue signal** (add to `EVENTS` + a `signal <name>(payload)` + the table in
  `docs/godot/state-and-signals.md` §3.1) — no ad-hoc "God channel"/"event chui". Navigate **only** through
  `SceneRouter.goto_scene(path)`/`back()` (never scatter `get_tree().change_scene*` in features); old scenes are
  `queue_free`d (no stale ref). Autoload scripts **omit `class_name`** (collides with the singleton name) — access via
  the global (`EventBus.emit(...)`). Canonical: `docs/godot/state-and-signals.md` §3.1 + `docs/godot/scene-architecture.md`
  §4.1; decision log `.memory/0012-client-autoloads-standardized.md`.
- **NetworkClient is standardized (Phase 15 — closed & verified).** `NetworkClient` (`src/core/net/network_client.gd`,
  autoload) is the **single server-communication gateway**. **UI/feature MUST NOT call `HTTPRequest` / REST directly** —
  `HTTPRequest` lives **only** in `src/core/net/` (grep guard). Use **`NetworkClient.get_json(path, parser)` /
  `post_json(path, body, parser)`**; base URL from env `GAME_TEAM_API_BASE_URL` (default `http://localhost:8080`), paths
  under `/api/v1`. Responses parse into **generated models (Phase 08)** via `NetworkResponseParser` (generated DTOs are
  DO-NOT-EDIT / no `from_dict` — add a parse func in `core/net/response_parser.gd`, never hand-declare a DTO). Failures →
  one normalized **`NetResult`** + **`network_error`** event; **401 also emits `unauthorized`** (both catalogue signals,
  `docs/godot/state-and-signals.md` §3.1). Retry **only GET/idempotent-safe** on transient transport failure; **POST is
  never auto-retried**. JWT via **`TokenStore`** (in-memory stub; real login/refresh = phase 18/20) — **never log
  token/Authorization, never hardcode a token**. Network loss → report failure, **never fabricate a result/reward**
  (ADR-008/011). Reuse — never add a second HTTP client or bypass NetworkClient. Canonical:
  `docs/godot/state-and-signals.md` §4; decision log `.memory/0013-client-networkclient-standardized.md`.
