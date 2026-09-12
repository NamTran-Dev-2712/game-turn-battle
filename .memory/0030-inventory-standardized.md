# 0030 — Inventory (asset ledger) standardized (Phase 32)

- **Trạng thái:** Đã chốt & verify cục bộ (2026-09-11, Docker Desktop + Godot 4.7.1). Kho đồ **server-authoritative** — "sổ tài
  sản" thứ hai bên cạnh ví (Phase 31): vật phẩm (item) + mảnh (fragment) dạng chồng `(item_type,item_id)→quantity` (bất biến
  không âm), gắn 1-1 profile; thêm/bớt qua **giao dịch atomic nhiều-item + idempotent + concurrency-safe** (ADR-007). Nền tái
  dùng cho gacha (33)/equipment (38)/shop (40)/mail (42).
- **Bối cảnh:** Phase 31 chuẩn hoá cơ chế ví/ledger/idempotency; roadmap 32 yêu cầu inventory tái dùng đúng mẫu đó (không cơ chế
  thứ hai). Hero owned đã là aggregate `OwnedHero` (Phase 27) — inventory KHÔNG nhân bản.

## Quyết định (thiết kế, đã justify — user-approved)

- **Chỉ command nội bộ + `GET /inventory` (KHÔNG endpoint thêm/bớt công khai).** Theo đúng nguyên tắc bảo mật Phase 31 (endpoint
  "cấp item" cho client là lỗ hổng kinh tế). Prompt §13 nêu "AddItems/RemoveItems API" nhưng SSOT (roadmap) + Completed-Phase-
  Preservation (Phase 31) thắng ⇒ `AddItemsCommand`/`RemoveItemsCommand` là `ITransactionalRequest` nội bộ (caller: gacha/shop/mail/
  equipment); atomic/idempotent chứng minh qua Infrastructure persistence tests (gọi service trực tiếp — như currency).
- **Item data-driven = loại config MỚI `item`** (hợp đồng Phase 06/07, như formation Phase 29): `item.schema.json` + `common`
  `item_id`(`^item_`)/`item_type` + `config/items/*` + `ConfigType.Item`/`ConfigFileMapper["items"]` + fixtures + mapper test.
  Stack = `{item_type, item_id, quantity}`: `item`→catalog `item`; **`fragment`→hero** (`item_id` là `hero_id` — khớp reward
  polymorphism + inventory doc "fragment = id hero"). KHÔNG catalog fragment riêng. Reward ref nay kiểm tồn tại: `item`→item index,
  `fragment`→hero index (`ReferenceValidator.Reward` — đóng "intentional limitation" cũ).
- **Ledger nhiều-item.** `InventoryTransaction` = một command = một dòng, chứa nhiều `InventoryChange{item_type,item_id,delta,
  quantity_after}` (JSON `changes`), **unique `idempotency_key`**. Khác currency (một delta/dòng) vì inventory thao tác nhiều-item
  atomic; cùng philosophy (idempotency-key + row-lock + one-transaction audit).
- **`InventoryService` (Application, scoped, song sinh `CurrencyWalletService`).** idempotency check → `GetByProfileIdForUpdateAsync`
  (`SELECT … FOR UPDATE`) → kiểm config MỌI item → **consume: kiểm đủ MỌI item TRƯỚC khi trừ bất kỳ** (nhiều-item atomic, §17) →
  áp dụng + ghi MỘT ledger. Gộp trùng `(type,id)`. KHÔNG tự mở transaction (chạy trong command top-level).
- **Seed starter inventory (tạm).** `CreateGuestAccountCommandHandler` cấp vài item catalog + mảnh mỗi hero (data-driven, cùng
  transaction) để màn kho có dữ liệu — TẠM đến khi gacha/shop (mirror seed hero tạm Phase 27). Config trống ⇒ không cấp (graceful).

## Thành phần

- **Config/schema:** `shared/config-schema/item.schema.json` + `common.schema.json` (`item_id`/`item_type` $defs) + fixtures
  (`item.valid|invalid.json`) + `config/items/{item_potion_small,item_upgrade_ore}.json` + `tools/config-validator/…/{ConfigType,
  ConfigFileMapper,ReferenceValidator}.cs` (+ `ConfigFileMapperTests`). `ConfigType.Item`→bundle key `"item"` (ConfigBundleBuilder Enum.GetValues).
- **Domain** `GameTeam.Domain/Inventory/`: `Inventory : AggregateRoot<Guid>` (1-1 profile, `Add`/`Remove`/`QuantityOf`, bất biến
  không âm) + `ItemStack` (value, `internal Add/Subtract`) + `InventoryTransaction : AggregateRoot<Guid>` (append-only, `Record`/
  `Restore`) + `InventoryChange` (value).
- **Application** `GameTeam.Application/Features/Inventory/`: `InventoryService` (`GrantAsync`/`ConsumeAsync`), `InventoryItemTypes`
  (item/fragment + config type keys), `ItemChange`, `InventoryErrors` (`UNAUTHENTICATED`/`PROFILE_NOT_FOUND`/`INVENTORY_UNKNOWN_ITEM`/
  `INVENTORY_INSUFFICIENT_CONFLICT`), `InventoryTransactionResult`+`InventoryItemResult`, `InventoryMapping`, `Commands/{Add,Remove}
  ItemsCommand`(+Handler+Validator, `ITransactionalRequest`), `Queries/GetInventoryQuery`(+Handler+Validator). Ports:
  `IInventoryRepository`(+`GetByProfileIdForUpdateAsync`) + `IInventoryTransactionRepository`(+`GetByIdempotencyKeyAsync`). DI:
  `InventoryService` scoped.
- **Infrastructure**: `InventoryConfiguration` (`inventories`, unique `profile_id`, JSON `stacks`, FK cascade) +
  `InventoryTransactionConfiguration` (`inventory_transactions`, unique `idempotency_key`, index `profile_id`, JSON `changes`, FK
  cascade) + `InventoryRepository`(`FOR UPDATE`)/`InventoryTransactionRepository` + DbSets + DI + migration `AddInventory`.
- **API**: `GET /api/v1/inventory` (version set, protected) → `GetInventoryQuery(itemType?, page, pageSize)`.
- **Contract**: `Contracts/Inventory/{InventoryDto{items,ownedHeroes}, ItemStackDto{itemType,itemId,quantity}}` → regenerate
  `openapi.json` → codegen `inventory_dto.gd`+`item_stack_dto.gd` (`RealSpecTests` +2). Reuse `OwnedHeroDto` (Phase 27).
- **Client**: `NetworkResponseParser.parse_inventory`; `StateCache.apply_inventory`+`get_inventory` (server-sourced, display-only,
  offline-view); `client/src/ui/inventory/{inventory_view,inventory_presenter,inventory.tscn}` (fetch→cache→display, lọc tab, fallback
  KHÔNG im lặng, tên từ `ConfigProvider`); route hub "Kho đồ". KHÔNG thêm event EventBus (tái dùng `state_refreshed`).

## Chống tự-vẽ (binding — Rules A–H)

- **A Asset Ledger / F Reuse:** dùng `InventoryService`/`Inventory`/`InventoryTransaction` + mẫu Phase 31 — KHÔNG inventory/ledger/
  idempotency/save-root thứ hai; KHÔNG nhân bản `OwnedHero`. gacha/shop/mail/equipment là **caller**.
- **B No Client Authority (ADR-007/011):** client CHỈ hiển thị số lượng; KHÔNG mutator chân lý (`add_item`…); KHÔNG endpoint thêm/bớt
  công khai; owner suy từ token (IDOR).
- **C Atomic Multi-Item:** một item thiếu/lạ ⇒ toàn bộ command fail, KHÔNG mutate một phần (§17). **D Idempotency:** unique key ⇒
  retry/replay không double. Concurrency: `FOR UPDATE` ⇒ không âm.
- **E Data-Driven:** item/fragment theo config/schema (`item` type + `hero` cho fragment); KHÔNG hardcode item content. Item mới dùng
  type sẵn = config-only; item_type mới = additive enum + validator + service branch cùng change.
- Ngoài scope (nợ): gacha cấp hero/fragment thật (33), equipment lắp/tháo (38), ascension tiêu fragment (39), shop/mail cấp item
  (40/42); seed starter = tạm; query trên JSON stacks per-profile (bounded) — tách bảng dòng nếu về sau cực lớn.

## Verify (2026-09-11)

- `dotnet build -c Release` 0/0; `dotnet test` — Domain **130** (Inventory 13) / Application **112** (Inventory 21: service+command+query)
  / Contracts **36** / Infrastructure **70** (Testcontainers pg16 `InventoryPersistenceTests` 10: atomic/idempotent/over-remove/
  **concurrency**/rollback/**§17 multi-item partial-fail không mutate**/multi-type) / Api **86** (`InventoryEndpointTests` 6: auth 401/seed
  item+fragment+hero/lọc/phân trang/isolation).
- config-validator **51** + `run.sh` exit 0 (17 file); codegen **41** + drift sạch; `has-pending-model-changes` = "No changes".
- Godot 4.7.1 `--headless --import` exit 0; gdUnit4 **143/143, 0 orphan** (inventory presenter 6 + parse_inventory 2 + apply_inventory 2).
- `openapi.json` additive (`/inventory` + InventoryDto/ItemStackDto); không generated drift ngoài 2 file mới.
- **CI-pending:** GitHub Actions (`ci-server`/`ci-client`/`codegen-check`/`validate-config`) — xanh đầy đủ ở local (Testcontainers + Godot
  headless), chờ Actions.

## Liên kết

- CLAUDE.md §4.6 (Inventory block) · [[0029-currencies-transactions-standardized]] (mẫu ledger/idempotency/row-lock tái dùng) ·
  [[0025-hero-system-standardized]] (OwnedHero — chiếu, không nhân bản; seed tạm) · [[0027-team-formation-standardized]] (add-a-config-type) ·
  [[0017-profile-persistence-standardized]] (save root) · [[0011-api-layer-standardized]] (error mapping) ·
  [[0006-codegen-pipeline-standardized]] (codegen).
- Docs: `docs/gameplay/inventory-and-equipment.md` §1, `docs/backend/infrastructure.md` §1.6, `docs/backend/domain-and-application.md`
  (Inventory feature), `docs/backend/api-and-versioning.md`, `docs/gameplay/configuration-and-data.md` §2b,
  `docs/godot/state-and-signals.md` §1.1, `docs/roadmap/32-inventory.md`.
