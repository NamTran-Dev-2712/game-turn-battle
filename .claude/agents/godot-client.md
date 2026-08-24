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
- **NetworkClient (Phase 15).** `NetworkClient` (`src/core/net/network_client.gd`, autoload) is the **single
  server-communication gateway**. **UI/feature never call `HTTPRequest`/REST directly** — `HTTPRequest` lives **only** in
  `src/core/net/` (grep guard). Call **`get_json(path, parser)` / `post_json(path, body, parser)`** (base URL env
  `GAME_TEAM_API_BASE_URL`, default `http://localhost:8080`; paths under `/api/v1`). Parse responses into **generated
  models (Phase 08)** via `NetworkResponseParser` (add a parse func in `core/net/response_parser.gd`; never hand-declare a
  DTO). Failures → normalized **`NetResult`** + **`network_error`** event; **401 also emits `unauthorized`**. Retry **only
  GET** on transient transport failure; **POST never auto-retried**. JWT via **`TokenStore`** (in-memory stub; real
  login/refresh = phase 18/20) — **never log token/Authorization; never hardcode a token**. Network loss → report failure,
  **never fabricate a result/reward** (ADR-008/011). Reuse — never add a second HTTP client. Canonical:
  `docs/godot/state-and-signals.md` §4; decision log `.memory/0013-client-networkclient-standardized.md`.
- **ConfigProvider + StateCache (Phase 16).** Two independent autoloads (after `NetworkClient`, omit `class_name`).
  **`ConfigProvider`** (`src/core/config/config_provider.gd`) is the **single config read gate**: `apply_bundle(bundle)`
  caches a versioned envelope **immutably** to disk (`user://config_cache/config@vN.json`, **write-once**; ADR-005), loads
  on boot (offline-view), serves **data-driven** `get_entry(type,id)`/`get_hero(id)`/`current_version()` (deep copies, **no
  hardcoded numbers**); `check_for_update()` pulls a newer version via `NetworkClient` (placeholder `/api/v1/config/...`;
  Config Service = phase 21, e2e = phase 22) and emits **`config_updated`** on a version change. **`StateCache`**
  (`src/core/state/state_cache.gd`) is a **read-only/display-only** state cache (`IS_DISPLAY_ONLY = true`): only
  `apply_snapshot(snapshot)` writes (from a server response; **no authoritative mutator**), reads return deep copies,
  `source()`/`is_offline()` label cached-vs-server, persists last snapshot for offline-view, emits **`state_refreshed`**.
  **Reuse both** — never load raw config in a feature, hardcode gameplay numbers, add a second cache, or treat `StateCache`
  as truth. Authoritative mutation path: `Feature/UI → NetworkClient → Server → response → StateCache.apply_snapshot`; the
  client **never** computes currency/reward/battle-result. Canonical: `docs/godot/resources-and-assets.md` §1.1 +
  `docs/godot/state-and-signals.md` §1.1/§3.1; decision log `.memory/0014-client-configprovider-statecache-standardized.md`.
- **Boot flow + UI base (Phase 17 — closes group 3).** All under `client/src/ui/` (a **scene**, not an autoload).
  **App-shell:** `run/main_scene = res://src/ui/app_root.tscn` (empty `Control`) → `_ready` routes to boot via `SceneRouter`
  ⇒ **SceneRouter owns every visible screen from frame one** (boot → hub, swap + `queue_free`). **Boot**
  (`src/ui/boot/boot_controller.gd` = presenter): `NetworkClient.get_json("/health", parse_health)` = **hard reachability
  gate** (fail → error + retry); `ConfigProvider.check_for_update()` = **best-effort** (Config Service = phase 21; missing
  endpoint ⇒ keep cache, never blocks boot); then `SceneRouter.goto(main_hub)` + `clear_history()`. **UI base**
  (`src/ui/base/base_view.gd`, `class_name BaseView extends Control`): **data-in** (`set_data`→`_render`) → **intent-out**
  (`emit_intent`→signal `intent`) + `bind`/`unbind`. **A view MUST NOT reference `NetworkClient`/`HTTPRequest`/`core/net`**
  (grep guard over `src/ui/**`); the **presenter** (BootController/`MainHubPresenter`) is the only touchpoint — reads
  `StateCache`/`ConfigProvider` (display-only), calls `NetworkClient` via the gateway, navigates via `SceneRouter`, emits
  EventBus **only** for genuine global events. **Intent = local signal + presenter** (no per-button EventBus event — the
  catalogue stays CLOSED; **no new EventBus event in Phase 17**). Error screen shows a **safe** message (no stack/internal
  leak) + retry (connected once → no duplicate listeners; `_running` guard → no duplicate navigation/requests).
  **Reuse `BaseView`/boot — never let a view call the network, never make boot a self-freeing main scene, never add a
  per-UI-action EventBus event.** `AudioManager` stays **deferred** (not in the Phase 17 contract). Canonical:
  `docs/godot/ui-architecture.md` §2.1/§4.1 + `docs/godot/scene-architecture.md` §4.2/§5; decision log
  `.memory/0015-client-boot-ui-standardized.md`. Setup/run: root `setup-and-run.md`.
- **Auth + Profile integration (Phase 20).** Client auth/save loop: **guest login → JWT → GET /profile → StateCache →
  hub**. **Auth lifecycle is CENTRALIZED in boot + `AuthProfileFlow`** (`src/ui/boot/auth_profile_flow.gd`, RefCounted,
  **not** an autoload); `NetworkClient` only attaches the token + emits `unauthorized`; **UI/views never hold auth logic**.
  **`TokenStore`** (`src/core/net/token_store.gd`) persists access+refresh+expiry **encrypted**
  (`FileAccess.open_encrypted_with_pass` → `user://auth/token.dat`, device-bound key) — **never plaintext, never log the
  token/passphrase, never commit**. Boot (`State.AUTHENTICATING`): health → `AuthProfileFlow.run()` (reuse valid token
  else `POST /api/v1/auth/guest`; then `GET /api/v1/profile` → `StateCache.apply_snapshot`) → config → hub. **401/expiry →
  bounded re-login** (`MAX_RELOGIN=1`) — **no infinite loop**. **Offline** (fail + cached profile) ⇒ hub offline mode
  (`[offline]` label), **never fabricate**. New parsers → existing generated `AuthGuestResponse`/`ProfileDto` (no
  contract change). Hub shows **name·level** (currency placeholder → phase 31). **No new EventBus event** — reuse
  `unauthorized` + `state_refreshed`. **Reuse `AuthProfileFlow`/`TokenStore`/`NetworkClient`/`StateCache`/`ProfileDto` —
  never add a second auth/token/HTTP/profile abstraction, never put auth in a view, never bypass StateCache, never add a
  refresh-token architecture beyond scope.** Canonical: `docs/godot/state-and-signals.md` §4.1/§3.1 +
  `docs/godot/ui-architecture.md` §4.1; decision log `.memory/0018-client-auth-profile-standardized.md`.

## Definition of Done
Per `docs/ai/review-and-dod.md`: gdUnit4 tests for new logic (golden-vector test if the sim changed), no Forbidden Patterns, docs updated per `.claude/workflows/documentation-sync.md`.
