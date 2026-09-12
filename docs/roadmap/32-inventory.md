# 32 — Inventory

> Mục đích: Hiện thực **kho đồ (inventory)** server-authoritative chứa hero/vật phẩm/mảnh (fragment) — nơi tổng hợp tài sản người chơi, nền cho gacha/equipment/shop.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 7 Collection Core | P3 | S8 | F10 |

# Mục tiêu

Inventory gắn profile: chứa OwnedHero (từ phase 27), vật phẩm, hero fragment; thao tác thêm/bớt qua command atomic; query kho; client hiển thị. Tích hợp với currency-style transaction (idempotency).

# Lý do

Inventory là "nơi chứa" mọi phần thưởng (F10). Cần trước gacha (33 cấp hero/fragment vào kho) và equipment (38). Server-authoritative để tài sản không bị cheat.

# Phụ thuộc

- **Trước:** 27 (hero), 31 (transaction pattern), 19 (profile).
- **Sau:** 33 (gacha thêm hero/fragment), 38 (equipment item), 40/42 (shop/mail cấp item).

# Phạm vi

- Model Inventory: OwnedHero, item stack (fragment, vật phẩm), số lượng.
- Command thêm/bớt item atomic + idempotency; query kho (phân trang/lọc).
- Ràng buộc: không âm số lượng; item type theo schema (06).
- Client: màn kho hiển thị (list/lọc).

# Không thuộc phạm vi

- Logic equipment lắp/tháo (phase 38).
- Gacha (phase 33).
- Sink tiêu item nghiệp vụ cụ thể (ascension 39, shop 40).

# Deliverables

- Inventory model + command add/remove + query.
- Integration test: add/remove atomic + idempotent; query kho; không âm.
- Client màn kho.
- Cập nhật [`../gameplay/inventory-and-equipment.md`](../gameplay/inventory-and-equipment.md).

# Công việc cần thực hiện

- [x] Domain: `Inventory` (gắn profile) chứa hero owned + item stacks; bất biến số lượng ≥ 0.
  → `GameTeam.Domain/Inventory/` (`Inventory` aggregate 1-1 profile + `ItemStack` value object, bất biến `quantity ≥ 0`;
  `InventoryTransaction` ledger append-only nhiều-item + `InventoryChange`). Hero owned KHÔNG nhân bản — là aggregate
  riêng `OwnedHero` (Phase 27), inventory **chiếu** khi query. Test: `Domain.Tests/Inventory` (13) — add/remove/không âm/multi-stack.
- [x] Application: `AddItemsCommand`/`RemoveItemsCommand` (transactional + idempotency), `GetInventoryQuery` (lọc/phân trang).
  → `GameTeam.Application/Features/Inventory/` — `InventoryService` (idempotency-key + `FOR UPDATE` row-lock + kiểm config +
  nhiều-item atomic + ledger, tái dùng mẫu Phase 31); `AddItems/RemoveItemsCommand` (`ITransactionalRequest`, **KHÔNG endpoint
  công khai**); `GetInventoryQuery` (owner từ token, lọc `itemType` + phân trang + thứ tự tất định + chiếu hero). Test: 21.
- [x] Item định nghĩa theo config/schema (fragment, vật phẩm) — data-driven.
  → Loại config mới `item` (mẫu Phase 06/07): `shared/config-schema/item.schema.json` + `common.schema.json` (`item_id`/`item_type`)
  + `config/items/*` + `ConfigType.Item`/`ConfigFileMapper` + reference (reward `item`→item, `fragment`→hero). `run.sh` exit 0 (17 file);
  validator 51 test. Fragment = mảnh hero (`item_id` là `hero_id`); item = catalog. Service kiểm id theo `IConfigProvider`.
- [x] Client feature `inventory/`: màn kho, lọc theo loại, hiển thị số lượng.
  → `client/src/ui/inventory/` (`InventoryView` network-free + `InventoryPresenter` tải server → `StateCache.apply_inventory` → hiển thị,
  lọc tab [Tất cả][Anh hùng][Mảnh][Vật phẩm], fallback KHÔNG im lặng). Route từ hub. Test gdUnit4: 6.
- [x] Contract DTO inventory + codegen.
  → `GameTeam.Contracts/Inventory/` (`InventoryDto`{items, ownedHeroes} + `ItemStackDto`) → regen `openapi.json` → `shared/codegen/run.sh`
  → `client/src/data/generated/{inventory_dto,item_stack_dto}.gd`. Drift gate sạch; `RealSpecTests` + codegen 41 test.
- [x] Integration test: add/remove atomic+idempotent; over-remove chặn; query đúng.
  → `Infrastructure.Tests/Persistence/InventoryPersistenceTests` (Testcontainers pg16, 10: atomic/idempotent/over-remove/**concurrency**/rollback/
  **§17 multi-item partial-fail không mutate**); `Api.IntegrationTests/InventoryEndpointTests` (6: auth 401/seed/lọc/phân trang/isolation).
- [x] Cập nhật `../gameplay/inventory-and-equipment.md`. → §1 cập nhật theo implementation thật (ledger/atomic/idempotency/query/authority).

# Tiêu chí hoàn thành

- Thêm/bớt item atomic + idempotent (retry không double).
- Không thể bớt quá số lượng (không âm).
- Query kho đúng, hỗ trợ lọc/phân trang.
- Client hiển thị kho từ server; không client-authority.

# Cách kiểm tra

- `dotnet test`: add/remove atomic/idempotent, over-remove, query.
- Local: battle/gacha cấp item → kho cập nhật; retry không double.
- gdUnit4: màn kho hiển thị đúng.

# Rủi ro

- **Double-add/remove** → idempotency (mẫu phase 31).
- **Số lượng âm/không nhất quán** → transaction + ràng buộc domain.
- **Kho lớn tải chậm** → phân trang + query hiệu quả.

# Ghi chú

Inventory + currency là hai "sổ tài sản" server-authoritative; gacha/shop/mail đều ghi vào đây qua transaction idempotent. Bám [`../gameplay/inventory-and-equipment.md`](../gameplay/inventory-and-equipment.md) + ADR-007.

# Technical Debt Review

- **Maintainability:** kho tập trung; item data-driven.
- **Scalability:** phân trang; index truy vấn.
- **Testing:** atomic/idempotent/query có test.
- **Security:** tài sản server-authoritative.
- **Nợ:** equipment/ascension tiêu item (38/39).

# Phase Review

**ĐÓNG (đã xác minh 2026-09-11).** Inventory add/remove **atomic nhiều-item + idempotent** (retry/replay không double; một
item thiếu ⇒ toàn bộ fail không mutate một phần — §17), **concurrency-safe** (`FOR UPDATE`, hai consume song song không âm),
query kho **lọc + phân trang + thứ tự tất định** + chiếu hero owned; client hiển thị **server-authoritative** (không client-authority,
fallback KHÔNG im lặng). Item **data-driven** (loại config `item` mới; fragment→hero). Tái dùng đúng mẫu Phase 31 (ledger +
idempotency-key + row-lock) — KHÔNG cơ chế thứ hai. **KHÔNG endpoint thêm/bớt công khai** (chỉ `GET /api/v1/inventory`; cấp/tiêu là
command nội bộ — bảo mật kinh tế như Phase 31).

**Bằng chứng chạy:** build Release 0/0; `dotnet test` — Domain 130 / Application 112 / Contracts 36 / Infrastructure 70
(Testcontainers) / Api 86 (Testcontainers); config-validator 51 + `run.sh` exit 0; codegen 41 + drift sạch; `has-pending-model-changes`
= "No changes"; Godot 4.7.1 import exit 0 + gdUnit4 **143/143, 0 orphan**. Không openapi/generated drift ngoài additive inventory.

**Nợ (chuyển phase sau, có chủ đích):** gacha cấp hero/fragment thật (33); equipment lắp/tháo (38); ascension tiêu fragment (39);
shop/mail cấp item (40/42) — đều là **caller** của `AddItems/RemoveItemsCommand`, KHÔNG dựng cơ chế mới. Seed starter inventory
trong `CreateGuestAccountCommandHandler` là **tạm** (đến khi 33/40). Query truy vấn trên JSON stacks per-profile (bounded); nếu kho
cực lớn về sau cân nhắc tách bảng dòng — chưa cần MVP.

---

## Liên kết
- [`../gameplay/inventory-and-equipment.md`](../gameplay/inventory-and-equipment.md) · [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md)
- ADR: [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md) · [`../adr/ADR-004-data-driven-design.md`](../adr/ADR-004-data-driven-design.md)
- Roadmap: [`README.md`](README.md) → kế: [`33-summon-gacha.md`](33-summon-gacha.md)
