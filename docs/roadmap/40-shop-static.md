# 40 — Shop (static)

> Mục đích: Hiện thực **cửa hàng tĩnh** — đổi currency lấy item/hero fragment theo bảng giá config; sink currency, server-authoritative.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 9 Kinh tế & QoL | P4 | S10 | F18 |

# Mục tiêu

Shop tĩnh (danh mục từ config: giá, loại tiền, item); mua qua command server atomic (spend currency + cấp item idempotent); giới hạn mua (nếu có) theo config; client hiển thị & mua.

# Lý do

Shop là sink currency chính (F18) + kênh lấy item định hướng. Tĩnh (không rotation) đủ cho MVP; rotation là Post-MVP (nền LiveOps để sau).

# Phụ thuộc

- **Trước:** 31 (currency), 32 (inventory), 21 (config shop).
- **Sau:** 49 (shop rotation LiveOps — Post-MVP nền), tuning.

# Phạm vi

- Danh mục shop config (item, giá, loại tiền, giới hạn mua).
- Command `PurchaseCommand` atomic (spend + cấp item idempotent) + kiểm giới hạn.
- Query danh mục + trạng thái đã mua/giới hạn.
- Client: UI shop.

# Không thuộc phạm vi

- Shop rotation/limited theo thời gian (Post-MVP/LiveOps — chỉ chừa schema time-based ở phase 49).
- IAP tiền thật (Post-MVP — F34).
- Số liệu giá (config).

# Deliverables

- Shop tĩnh + mua atomic idempotent + giới hạn.
- Integration test: mua atomic, thiếu tiền chặn, giới hạn mua, idempotent.
- Client UI shop.
- Cập nhật [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md).

# Công việc cần thực hiện

- [ ] Schema shop (mở rộng 06): item, giá, loại tiền, giới hạn mua/kỳ.
- [ ] Application: `PurchaseCommand` (spend currency atomic 31 → cấp item 32, idempotent) + kiểm giới hạn mua.
- [ ] Query danh mục + trạng thái giới hạn của người chơi.
- [ ] Client feature `shop/`: danh mục, mua, phản hồi.
- [ ] Integration test: mua atomic, thiếu tiền chặn, vượt giới hạn chặn, idempotent (retry không double).
- [ ] Cập nhật `../gameplay/progression-and-economy.md`.

# Tiêu chí hoàn thành

- Mua atomic (spend + cấp); thiếu tiền/vượt giới hạn → chặn.
- Idempotent (retry không double item/không mất tiền hai lần).
- Data-driven (đổi giá/danh mục config → đổi).
- Server-authoritative; client hiển thị/gửi intent.

# Cách kiểm tra

- `dotnet test`: mua atomic, thiếu tiền, giới hạn, idempotent.
- Local: mua item → tiền trừ, item vào kho; retry không double.
- Đổi giá config → đổi (test).

# Rủi ro

- **Double purchase** → idempotency (mẫu 31).
- **Client tự đổi giá** → giá server từ config; client chỉ hiển thị.
- **Giới hạn mua bị bypass** → kiểm server.

# Ghi chú

Shop tĩnh MVP; nền time-based cho rotation đặt ở phase 49 (ADR-006). Giá là tuning (config). Bám [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md).

# Technical Debt Review

- **Maintainability:** danh mục/giá là data.
- **Scalability:** nền rotation (LiveOps) không phá.
- **Testing:** mua/giới hạn/idempotent có test.
- **Security:** giá + giới hạn server-authoritative.
- **Nợ:** rotation/IAP (Post-MVP).

# Phase Review

Đóng khi shop mua atomic idempotent + giới hạn + data-driven, test xanh.

---

## Liên kết
- [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md) · [`../mvp/06-game-economy.md`](../mvp/06-game-economy.md)
- ADR: [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md) · [`../adr/ADR-006-liveops.md`](../adr/ADR-006-liveops.md)
- Roadmap: [`README.md`](README.md) → kế: [`41-daily-quest.md`](41-daily-quest.md)
