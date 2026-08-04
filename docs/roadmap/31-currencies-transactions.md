# 31 — Currencies + atomic transaction + idempotency

> Mục đích: Hiện thực **3 tiền tệ nền** (Gold/Gem/Ticket) với mọi thay đổi qua **giao dịch server atomic + idempotency** — chống double-grant/double-spend (ADR-007).

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 7 Collection Core | P3 | S8 | F09 |

# Mục tiêu

Model currency gắn profile (server-authoritative); mọi cộng/trừ đi qua command atomic có idempotency key; số dư đọc qua query; client chỉ hiển thị. Nền cho gacha (33), thưởng (30/37), shop (40).

# Lý do

Currency là hệ **nhạy cảm nhất** (🔴 mvp/08). Phải atomic + idempotent trước khi bất kỳ nguồn/sink nào (gacha, reward, shop) đụng tới, tránh lỗi kinh tế không thể sửa.

# Phụ thuộc

- **Trước:** 19 (profile), 11 (transaction/idempotency nền), 30 (battle reward tối giản).
- **Sau:** 33 (gacha tiêu ticket/gem), 37 (AFK cấp gold), 38–40 (sink), 32 (inventory).

# Phạm vi

- Currency (Gold soft, Gem premium, Summon Ticket) gắn profile; loại tiền theo enum (phase 05).
- Command `GrantCurrency`/`SpendCurrency` atomic + idempotency key (không double).
- Query số dư; lịch sử giao dịch tối giản (audit).
- Ràng buộc: không âm; spend đủ số dư mới thực hiện.

# Không thuộc phạm vi

- Hero Fragment (semi-currency, Should — phase 33/39).
- Energy (phase 36).
- Số liệu giá/tỉ lệ (config).

# Deliverables

- Currency model + command grant/spend atomic + idempotency.
- Query số dư + audit log giao dịch.
- Integration test: grant/spend atomic; idempotent (retry không double); spend thiếu tiền bị chặn; concurrency an toàn.
- Cập nhật [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md).

# Công việc cần thực hiện

- [ ] Domain: `Wallet`/currency trên profile; loại tiền enum (05); bất biến không âm.
- [ ] Application: `GrantCurrencyCommand`/`SpendCurrencyCommand` (`ITransactionalRequest`) + idempotency key.
- [ ] Infrastructure: bảng idempotency (dùng nền phase 11) — key đã xử lý → trả kết quả cũ, không thực hiện lại.
- [ ] Query `GetBalance` + ghi audit giao dịch (nguồn/sink, thời điểm server).
- [ ] Ràng buộc concurrency: cập nhật số dư an toàn (optimistic/row lock).
- [ ] Integration test: atomic, idempotent (retry), spend thiếu→chặn, concurrency (2 spend song song không âm).
- [ ] Client: hiển thị số dư từ StateCache (không tự tính).
- [ ] Cập nhật `../gameplay/progression-and-economy.md`.

# Tiêu chí hoàn thành

- Grant/spend atomic; retry cùng idempotency key → **không** double.
- Spend vượt số dư bị từ chối (số dư không âm).
- 2 thao tác song song không gây số dư sai (concurrency test).
- Client hiển thị số dư từ server; không client-authority.

# Cách kiểm tra

- `dotnet test`: atomic, idempotent, over-spend, concurrency.
- Local: battle reward (30) cộng gold → số dư đúng; gọi lại không double.
- Rà: không có phép cộng/trừ currency ở client.

# Rủi ro

- **Double-grant khi mạng chập/retry** → idempotency key bắt buộc mọi giao dịch.
- **Race condition số dư âm** → transaction + khoá/optimistic concurrency.
- **Client tự cộng tiền** → server-authoritative tuyệt đối (ADR-007).

# Ghi chú

Idempotency ở đây là mẫu tái dùng cho AFK claim (37), gacha (33), mail claim (42). Số liệu kinh tế là data-driven (config), không nằm ở code. Bám [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md) + ADR-007.

# Technical Debt Review

- **Maintainability:** một cơ chế giao dịch tái dùng cho mọi nguồn/sink.
- **Scalability:** concurrency-safe; audit cho vận hành.
- **Testing:** atomic/idempotent/concurrency là hợp đồng.
- **Security:** hệ nhạy cảm nhất — server-authoritative + idempotent.
- **Nợ:** fragment (33/39); cân bằng số (tuning).

# Phase Review

Đóng khi currency grant/spend atomic + idempotent + concurrency-safe, client chỉ hiển thị, test xanh.

---

## Liên kết
- [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md) · [`../backend/domain-and-application.md`](../backend/domain-and-application.md)
- ADR: [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md) · [`../adr/ADR-003-backend-architecture.md`](../adr/ADR-003-backend-architecture.md)
- Roadmap: [`README.md`](README.md) → kế: [`32-inventory.md`](32-inventory.md)
