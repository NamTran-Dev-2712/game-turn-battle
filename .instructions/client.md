# Instructions: Client scope (`client/`)

Short execution hints. Canonical design: `docs/godot/`. Agent: `.claude/agents/godot-client`.

- Godot 4.7, GDScript, static typing everywhere.
- Features never import each other — EventBus/signals only (ADR-002); no God autoload.
- No client-side authority (economy/result/reward) — call the server (ADR-007/011).
- Naming: `snake_case` members, `PascalCase` `class_name`, `CONSTANT_CASE` consts, `##` docs; **TAB** indent (`.editorconfig`).
- Feature layout: `.templates/godot-feature/`.
- Combat sim: pure/node-decoupled, integer/fixed-point, seeded RNG; match server golden vector (ADR-011). **Golden gate (Phase 26):** `client/tests/combat/golden_vector_test.gd` **auto-discovers** every `shared/combat-vectors/*.json` (`CombatVectorLoader.list_vector_files()`) and matches each against the **server-generated** baseline; client replays, never redefines the result. Baseline change = server-side `tools/combat-baseline` only (never hand-edit a vector). `ci-client.yml` triggers on `shared/combat-vectors/**`.
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
- **ConfigProvider + StateCache are standardized (Phase 16 — closed & verified).** Two independent core autoloads
  (registered after `NetworkClient`, both **omit `class_name`**). **`ConfigProvider`** (`src/core/config/config_provider.gd`)
  is the **single config read gate** — `apply_bundle(bundle)` caches a versioned envelope **immutably** to disk
  (`user://config_cache/config@vN.json`, **write-once — never overwrite an old version**; ADR-005), loads on boot
  (offline-view), and serves **data-driven** queries `get_entry(type,id)`/`get_hero(id)`/`current_version()` (reads by
  schema, **no hardcoded numbers**, return deep copies). `check_for_update()` (e2e **phase 22**) does
  `GET /api/v1/config/current` → compare → `GET /api/v1/config/bundle?bundleVersion=N` → `apply_bundle`, returning a
  **status dict** `{updated, used_fallback, error_code, has_config}` and emitting **`config_updated`** on a version change
  (re-applying the active version = no-op). **Real server contract:** param is **`bundleVersion`** (never `version`),
  endpoints are **public** (`.AllowAnonymous`); the server serves `data` as a **map by id** (`data.{type}.{id}=entry`) —
  `_build_index` handles both map (server) and array (old fixtures). **Fallback is NOT silent** (Rule E): a failed bundle
  download keeps the old cache + sets `is_stale()`/`last_error_code()` + `push_warning`; no cache ⇒ the feature screen shows
  empty + Retry. The sample **`HeroListView`/`HeroListPresenter`** (`src/ui/hero_list/`) reads `get_all(&"hero")` to prove
  the loop (server config change → new version → client displays it **without a rebuild**). **`StateCache`** (`src/core/state/state_cache.gd`) is a **read-only,
  display-only** player-state cache (`IS_DISPLAY_ONLY = true`) — the **only** write path is `apply_snapshot(snapshot)` from a
  **server response** (no authoritative mutator: no `add_currency`/`spend_currency`/`set_progress`); reads return deep
  copies; `source()`/`is_offline()` label cached-vs-server; persists last snapshot for offline-view; emits
  **`state_refreshed`**. **Reuse both** — never load raw config in a feature, hardcode gameplay numbers, create a second
  config/state cache, or treat `StateCache` as a source of truth. Every authoritative mutation goes
  `Feature/UI → NetworkClient → Server → response → StateCache.apply_snapshot`; the client **never** computes
  currency/reward/battle-result. Both new events follow the §3.1 catalogue process. Canonical:
  `docs/godot/resources-and-assets.md` §1.1 + `docs/godot/state-and-signals.md` §1.1/§3.1; decision log
  `.memory/0014-client-configprovider-statecache-standardized.md`.
- **Boot flow + UI base are standardized (Phase 17 — closed & verified).** First runnable slice + the UI foundation
  (all under `client/src/ui/`, **not** an autoload — boot is a scene). **App-shell:** `run/main_scene =
  res://src/ui/app_root.tscn` (empty `Control`) → `_ready` routes to boot via `SceneRouter` ⇒ **SceneRouter owns every
  visible screen from frame one** (boot → hub, swap + `queue_free`). **Boot** (`src/ui/boot/boot_controller.gd` = presenter):
  `NetworkClient.get_json("/health", NetworkResponseParser.parse_health)` = **hard reachability gate** (fail → error +
  retry); `ConfigProvider.check_for_update()` = **best-effort** (Config Service = phase 21; missing endpoint ⇒ keep cache,
  never blocks boot); then `SceneRouter.goto(main_hub)` + `clear_history()`. **UI base** (`src/ui/base/base_view.gd`,
  `class_name BaseView extends Control`): **data-in** (`set_data`→`_render`) → **intent-out** (`emit_intent`→signal
  `intent`) + `bind`/`unbind` lifecycle. **Views are network-free** — a **view** MUST NOT reference
  `NetworkClient`/`HTTPRequest`/`core/net` (grep guard); the **presenter** (BootController/`MainHubPresenter`) is the only
  touchpoint: it reads `StateCache`/`ConfigProvider` (display-only), calls `NetworkClient` via the gateway, navigates via
  `SceneRouter`, and emits EventBus **only** for genuine global events. **Intent = local signal + presenter** (chosen over
  a per-button EventBus event — the catalogue stays CLOSED; **no new EventBus event added in Phase 17**). Error screen
  (`boot_error_view.gd`) shows a **safe** message (no stack/internal leak) + retry (connected once → no duplicate
  listeners; `_running` guard → no duplicate navigation/requests). **Reuse `BaseView`/boot flow — never let a view call
  the network, never make boot a self-freeing main scene, never add a per-UI-action EventBus event.** `AudioManager`
  stays **deferred** (not in the Phase 17 contract). Canonical: `docs/godot/ui-architecture.md` §2.1/§4.1 +
  `docs/godot/scene-architecture.md` §4.2/§5; decision log `.memory/0015-client-boot-ui-standardized.md`. Setup/run:
  root `setup-and-run.md`.
- **Auth + Profile integration is standardized (Phase 20 — closed & verified).** Closes the client auth/save loop:
  **guest login → JWT → GET /profile → StateCache → hub** (ADR-007/008). **Auth lifecycle is CENTRALIZED in boot +
  `AuthProfileFlow`** (`src/ui/boot/auth_profile_flow.gd`, RefCounted — **not** an autoload); `NetworkClient` only
  attaches the token + emits `unauthorized`; **UI/views never contain auth logic**. **`TokenStore`**
  (`src/core/net/token_store.gd`, extended) persists access+refresh+expiry **encrypted** (`FileAccess.open_encrypted_with_pass`
  → `user://auth/token.dat`, device-bound key) — **never plaintext, never log the token/passphrase, never commit**;
  `NetworkClient._ready()` calls `token_store.load()`. **Boot** (`boot_controller.gd`, `State.AUTHENTICATING`):
  health → `AuthProfileFlow.run()` (reuse token if present+not-expired else `POST /api/v1/auth/guest`; then
  `GET /api/v1/profile` → `StateCache.apply_snapshot`) → config → hub. **401/expiry → bounded re-login** (`MAX_RELOGIN=1`,
  reads `NetResult.kind==UNAUTHORIZED`) — **no infinite loop**. **Offline** (health/auth fail + cached profile) ⇒ hub in
  **offline mode** (`[offline]` label), **never fabricate**; error screen only when no cache. New parsers
  `parse_auth_guest_response`/`parse_profile` → existing generated `AuthGuestResponse`/`ProfileDto` (**no contract change,
  no generated drift**). Hub shows server **name·level** (currency = **placeholder** until phase 31) + offline label,
  refreshing on `state_refreshed`. **No new EventBus event** — reuse `unauthorized` + `state_refreshed` (catalogue stays
  CLOSED at 5). **Reuse `AuthProfileFlow`/`TokenStore`/`NetworkClient`/`StateCache`/`ProfileDto` — never add a second
  auth/token/HTTP/profile abstraction, never put auth in a view, never bypass StateCache, never add refresh-token
  architecture beyond scope.** Out of scope: provider linking (Post-MVP), refresh endpoint, currency (phase 31), config
  bundle (phase 22). Canonical: `docs/godot/state-and-signals.md` §4.1/§3.1 + `docs/godot/ui-architecture.md` §4.1;
  decision log `.memory/0018-client-auth-profile-standardized.md`.
- **Hero System (Phase 27, đã chốt — mở Nhóm 6):** Hero List (`src/ui/hero_list/`) GHÉP hero **owned**
  (`StateCache.get_heroes()`, server-authoritative) + **definition** (`ConfigProvider.get_hero(id)`, data-driven) — đổi
  config → định nghĩa đổi KHÔNG rebuild; bấm hero → intent `open_hero {id}`. **Hero Detail** (`src/ui/hero_detail/`, MỚI)
  là scene riêng; hero id truyền qua **`SceneRouter.goto_scene(path, context)`** + đọc bằng **`SceneRouter.route_context()`**
  (mở rộng additive Phase 14). **Art tải LAZY** qua autoload **`AssetLoader`** (`src/core/assets/asset_loader.gd`:
  `load_texture` async + `placeholder` + `release`) — đường dẫn art **từ config** (field `art`), placeholder khi thiếu/lỗi,
  **list KHÔNG tải art** (không chặn — ADR-009). Boot `AuthProfileFlow` fetch `/api/v1/heroes` (`parse_my_heroes`) → gộp
  vào **một** snapshot `{profile, heroes}` (StateCache thay nguyên snapshot ⇒ không ghi đè mất nhau). **KHÔNG thêm event
  EventBus** (tái dùng `config_updated`/`state_refreshed`, catalogue vẫn ĐÓNG). DTO hero là **generated** (`data/generated/`
  — DO-NOT-EDIT); consume endpoint mới = thêm parse func ở `response_parser.gd`. **Reuse `AssetLoader`/`ConfigProvider`/
  `StateCache`/`NetworkClient` — KHÔNG bộ nạp asset / config / state / HTTP thứ 2; KHÔNG hardcode chỉ số hero ở view/scene;
  KHÔNG để view gọi network; KHÔNG bịa ownership (chân lý ở server).** Ngoài scope: skill (28)/formation (29)/battle (30)/
  summon (33)/upgrade (35/39)/art thật + atlas-pool (52). Canonical: `docs/gameplay/hero-system.md` §7 +
  `docs/godot/resources-and-assets.md` §2.1 + `docs/godot/scene-architecture.md` §4.1; decision log `.memory/0025`.
