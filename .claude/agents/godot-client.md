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
- **Combat sim is pure:** decoupled from nodes, integer/fixed-point, seeded RNG. Must reproduce the server golden vector (ADR-011). **Skill framework (Phase 28):** `client/src/combat/effects/*` mirrors the server `EffectRegistry` (damage/heal/apply_buff/apply_debuff + `stat_modifier_core.gd`) bit-for-bit — `heal` emits `Healed`, buff/debuff = flat stat modifier + `duration` (refresh-on-reapply), energy-ultimate (§15) config-gated default-OFF. Extend via a handler mirroring server + config; never fork a second sim, use `float`/global RNG, or `switch(effect_type)`. Canon: `docs/gameplay/skill-framework.md`.
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
  hardcoded numbers**); `check_for_update()` (e2e **phase 22**) does `GET /api/v1/config/current` → compare →
  `GET /api/v1/config/bundle?bundleVersion=N` → `apply_bundle`, returns a **status dict**
  `{updated, used_fallback, error_code, has_config}`, emits **`config_updated`** on a version change. **Real contract:**
  param **`bundleVersion`** (never `version`), endpoints **public**; server `data` is a **map by id**
  (`data.{type}.{id}=entry`) — `_build_index` handles map + array. **Fallback NOT silent** (Rule E): failed download keeps
  old cache + `is_stale()`/`last_error_code()` + `push_warning`; no cache ⇒ feature empty + Retry. Sample
  `HeroListView`/`HeroListPresenter` (`src/ui/hero_list/`) reads `get_all(&"hero")` — config change → new version, **no
  client rebuild**. **`StateCache`**
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
- **Hero System (Phase 27, closed — opens Group 6):** Hero List (`src/ui/hero_list/`) joins **owned** heroes
  (`StateCache.get_heroes()`, server-authoritative) + **definition** (`ConfigProvider.get_hero(id)`, data-driven — config
  change re-renders WITHOUT rebuild); tap → intent `open_hero {id}`. **Hero Detail** (`src/ui/hero_detail/`, NEW) is a
  separate routed scene; hero id passed via **`SceneRouter.goto_scene(path, context)`** + read with
  **`SceneRouter.route_context()`** (additive Phase-14 extension). **Hero art lazy-loads** via autoload **`AssetLoader`**
  (`src/core/assets/asset_loader.gd`: `load_texture` async + `placeholder` + `release`) — art path **from config** (`art`
  field), placeholder on missing/error, **the list loads NO art** (never blocks — ADR-009). Boot `AuthProfileFlow` fetches
  `/api/v1/heroes` (`parse_my_heroes`) → one combined `{profile, heroes}` snapshot (StateCache replaces the whole
  snapshot). **No new EventBus event** (reuse `config_updated`/`state_refreshed`; catalogue stays CLOSED). Hero DTOs are
  **generated** (`data/generated/` — DO-NOT-EDIT); consume a new endpoint by adding a parse func in `response_parser.gd`.
  **Reuse `AssetLoader`/`ConfigProvider`/`StateCache`/`NetworkClient` — never a second asset/config/state/HTTP abstraction;
  never hardcode hero stats in a view/scene; never let a view call the network; never fabricate ownership (server is the
  authority).** Out of scope: skill (28)/formation (29)/battle (30)/summon (33)/upgrade (35/39)/real art + atlas-pool (52).
  Canonical: `docs/gameplay/hero-system.md` §7 + `docs/godot/resources-and-assets.md` §2.1 +
  `docs/godot/scene-architecture.md` §4.1; decision log `.memory/0025-hero-system-standardized.md`.
- **Team & Formation (Phase 29, closed):** feature `src/ui/formation/` (`FormationView` **network-free** + `FormationPresenter`).
  Presenter reads grid rows×cols from `ConfigProvider.get_entry(&"formation","formation_default")` (data-driven — never a
  hardcoded grid) + roster from `StateCache.get_heroes()`; keeps a **local draft** (select_hero → place_slot → change
  position → clear_slot). On save it POSTs the **intent** via `NetworkClient.post_json("/team", …)` then re-renders from the
  **server-returned team** — never a local save assumed accepted (ADR-007/011); opening the screen loads the saved team via
  `get_json("/team")`. Parser `response_parser.parse_team` → generated `TeamDto`/`TeamSlotDto` (DO-NOT-EDIT). Hub button
  "Đội hình" (`MainHubView.FEATURES` + `MainHubPresenter` `FORMATION_PATH`). **No new EventBus event** (reuse
  `state_refreshed`). **Reuse `NetworkClient`/`ConfigProvider`/`StateCache` — never a view calling the net, a second
  HTTP/config/state path, or client-side validation of ownership/count/duplicates (server is the authority).** Out of scope:
  battle (30)/preset (Post-MVP)/positional bonus. Canonical: `docs/gameplay/hero-system.md` §8; decision log
  `.memory/0027-team-formation-standardized.md`.
- **Battle flow (Phase 30, closed — CLOSES P2, first playable slice):** feature `src/ui/battle/` (`BattleView`
  **network-free** + `BattlePresenter`). Reached from hub button "Chiến đấu" (`MainHubPresenter` `BATTLE_PATH` with
  `{stage_id}`; stage-select = phase 34). Presenter: `get_json("/team")` → `post_json("/battles", {teamId, stageId,
  attemptId})` → receives `BattleResultDto` → **replays** by the returned seed (client `CombatInputResolver` +
  `BattleSimulator` + `ConfigProvider`) to render the sequence. **Server is authority (ADR-011/007):** `outcome`/`rewards`
  displayed **from the server**; on replay mismatch show the server outcome + `push_warning` (**never fabricate**); the client
  never chooses the seed nor grants reward; `attemptId` (idempotency key) is client-generated per fight (re-fight = new
  battle; server prevents double-grant). Parser `parse_battle_result` → generated `BattleResultDto`/`RewardDto`; `parse_team`
  reads `id` (client sends teamId). **Client resolver `src/combat/combat_input_resolver.gd` reads the REAL gameplay config**
  (base_stats+skills[]; skill target/trigger/effects[].params.coeff_fixed; enemy actor `enemy_{i}`) — **identical to the
  server** (bit-for-bit replay); never fork a second resolver/sim. **No new EventBus event.** Hub global wallet/currency =
  phase 31 (Phase 30 shows only this battle's rewards from the response). Canonical: `docs/gameplay/combat-framework.md` §24 +
  `docs/godot/ui-architecture.md` §4.2; decision log `.memory/0028-battle-flow-standardized.md`.
- **Currency display (Phase 31, closed):** wallet balances (Gold/Gem/Ticket) are **server-authoritative** — the client only
  DISPLAYS, never adds/subtracts (ADR-007). `NetworkResponseParser.parse_wallet` (wraps `{balances:[{currency,amount}]}`;
  `currency` wire is a STRING enum → mapped by `NetworkResponseParser.currency_code` to code `gold`/`gem`/`ticket`) → generated
  `WalletDto`/`CurrencyBalanceDto` (DO-NOT-EDIT). `AuthProfileFlow._fetch_balances` (`GET /api/v1/wallet`, best-effort) folds
  `currencies` into the single boot snapshot with profile+heroes. `StateCache.apply_wallet(balances)` = a server-sourced
  balance-only refresh (keeps profile/hero/progress; emits `state_refreshed`) — NOT a truth mutator (still no
  `add_currency`/`spend_currency`/`set_currency`; the forbidden-mutator guard test stays green). `BattlePresenter._refresh_wallet`
  refreshes the balance after a battle (server already granted — the client never adds it). Hub shows real balances. **No new
  EventBus event** (reuse `state_refreshed`). **Reuse `StateCache`/`NetworkClient`/generated DTO — no second wallet/parser/cache,
  no client currency math, no grant/spend call (read-only `/wallet`).** Out of scope: Fragment/Material/Energy, a dedicated wallet
  screen. Canonical: `docs/godot/state-and-signals.md` §1.1; decision log `.memory/0029-currencies-transactions-standardized.md`.
- **Inventory feature (Phase 32, closed):** the inventory screen `src/ui/inventory/` (`InventoryView` **network-free** +
  `InventoryPresenter`), entered from the hub "Kho đồ" button (`MainHubPresenter` `INVENTORY_PATH`). Presenter:
  `get_json("/api/v1/inventory", parse_inventory)` → `StateCache.apply_inventory(items, owned_heroes)` → display (single source =
  cache, `state_refreshed`). Filter tabs [All][Heroes][Fragments][Items] **client-side** over fetched data (the endpoint also supports
  server filter/pagination). Item names joined from `ConfigProvider.get_entry(&"item", id)` (data-driven); fragment = "Mảnh <hero>".
  `parse_inventory` (wraps `{items:[{itemType,itemId,quantity}], ownedHeroes:[…]}`) → generated `InventoryDto`/`ItemStackDto`
  (DO-NOT-EDIT). `StateCache.apply_inventory` + `get_inventory()` = server-sourced, display-only (NOT a truth mutator — still no
  `add_item`…; the forbidden-mutator guard test stays green). Non-silent fallback (Rule E): fetch error ⇒ keep cache + error label +
  Retry; empty ⇒ empty state. **Server-authoritative:** the client only DISPLAYS quantities, never mutates; no add/remove endpoint
  (grant/consume are internal server commands). **No new EventBus event** (reuse `state_refreshed`). **Reuse
  `StateCache`/`NetworkClient`/`ConfigProvider`/generated DTO — no second cache/parser.** Out of scope: gacha (33), equipment (38),
  shop/mail (40/42). Canonical: `docs/godot/state-and-signals.md` §1.1 + `docs/gameplay/inventory-and-equipment.md` §1; decision log
  `.memory/0030-inventory-standardized.md`.

## Definition of Done
Per `docs/ai/review-and-dod.md`: gdUnit4 tests for new logic (golden-vector test if the sim changed), no Forbidden Patterns, docs updated per `.claude/workflows/documentation-sync.md`.
