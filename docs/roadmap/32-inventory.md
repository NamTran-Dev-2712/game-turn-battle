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

- [ ] Domain: `Inventory` (gắn profile) chứa hero owned + item stacks; bất biến số lượng ≥ 0.
- [ ] Application: `AddItemsCommand`/`RemoveItemsCommand` (transactional + idempotency), `GetInventoryQuery` (lọc/phân trang).
- [ ] Item định nghĩa theo config/schema (fragment, vật phẩm) — data-driven.
- [ ] Client feature `inventory/`: màn kho, lọc theo loại, hiển thị số lượng.
- [ ] Contract DTO inventory + codegen.
- [ ] Integration test: add/remove atomic+idempotent; over-remove chặn; query đúng.
- [ ] Cập nhật `../gameplay/inventory-and-equipment.md`.

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

Đóng khi inventory add/remove atomic+idempotent + query + client hiển thị, test xanh.

---

## Liên kết
- [`../gameplay/inventory-and-equipment.md`](../gameplay/inventory-and-equipment.md) · [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md)
- ADR: [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md) · [`../adr/ADR-004-data-driven-design.md`](../adr/ADR-004-data-driven-design.md)
- Roadmap: [`README.md`](README.md) → kế: [`33-summon-gacha.md`](33-summon-gacha.md)
