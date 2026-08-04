# 33 — Summon/Gacha (server-authoritative rate + pity)

> Mục đích: Hiện thực **triệu hồi/gacha server-authoritative** (rate + pity, RNG server, dupes→fragments) — hệ nhạy cảm bậc nhất, phải công bằng và không cheat được (ADR-011/007).

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 7 Collection Core | P3 | S8 | F02 |

# Mục tiêu

≥1 banner: single + 10-pull; RNG **trên server** theo rate + pity từ config; kết quả cấp hero vào inventory + trùng→fragment; tiêu ticket/gem atomic idempotent; client chỉ gửi intent + hiển thị kết quả.

# Lý do

Gacha là trái tim monetization/collection (F02) và 🔴 nhạy cảm (mvp/08). Rate/pity phải server-side + data-driven (ADR-004/011) để công bằng, tune được, chống cheat. Cần sau currency/inventory.

# Phụ thuộc

- **Trước:** 31 (currency spend), 32 (inventory add), 27 (hero), 21 (config gacha).
- **Sau:** 34+ (loop dùng hero từ gacha), 39 (fragment→ascension).

# Phạm vi

- Banner định nghĩa trong config (rate theo rarity, pity counter, pool hero) — **số liệu ở config**.
- Command `SummonCommand` (single/10x): tiêu currency atomic → RNG server (seeded, audit) → xác định hero → cấp vào inventory / trùng→fragment; toàn bộ trong transaction + idempotency.
- Pity: đếm server-side gắn profile/banner; đảm bảo đúng ngưỡng.
- Client: UI banner + kết quả (hiển thị, không quyết).

# Không thuộc phạm vi

- Banner rotation/limited (Post-MVP/LiveOps).
- Số liệu rate cụ thể (config/tuning — EC1).
- Ascension tiêu fragment (phase 39).

# Deliverables

- Gacha server: rate+pity+RNG server + cấp hero/fragment atomic idempotent.
- Client: UI summon single/10x + màn kết quả.
- Integration test: phân phối rate (thống kê nhiều lần), pity kích hoạt đúng ngưỡng, tiêu tiền atomic, idempotent, dupes→fragment.
- Cập nhật [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md) (hoặc hero-system).

# Công việc cần thực hiện

- [ ] Schema banner (mở rộng gacha.schema phase 06): rate theo rarity, pity ngưỡng, pool — không nhúng số vào code.
- [ ] Application `SummonCommand`: validate đủ tiền → spend (31, idempotent) → RNG server (seeded, log seed để audit) → chọn hero theo rate/pity → cấp inventory (32) / trùng→fragment; tất cả trong 1 transaction.
- [ ] Pity counter server-side theo profile+banner; reset đúng khi trúng; đảm bảo ngưỡng.
- [ ] 10-pull: xử lý gộp atomic (tất cả hoặc không).
- [ ] Client feature `summon/`: chọn banner, single/10x, gửi intent, hiển thị kết quả (không tự random).
- [ ] Integration test: chạy N summon kiểm phân phối gần rate config; pity kích hoạt tại ngưỡng; tiêu tiền atomic; idempotent (retry không double hero/không mất tiền hai lần); dupes→fragment.
- [ ] Cập nhật `../gameplay/progression-and-economy.md`.

# Tiêu chí hoàn thành

- RNG + rate + pity **hoàn toàn server-side**; client không quyết kết quả.
- Thống kê N lần summon khớp rate config (trong dung sai); pity kích hoạt đúng ngưỡng.
- Tiêu currency + cấp hero/fragment **atomic**; retry idempotent (không double).
- Rate/pity đọc từ config (đổi config → đổi hành vi, không sửa code).

# Cách kiểm tra

- `dotnet test`: phân phối rate (nhiều lần), pity ngưỡng, atomic, idempotent, dupes→fragment.
- Đổi rate/pity trong config → hành vi đổi (test data-driven).
- Local: summon 10x → hero vào kho, tiền trừ đúng; retry không double.
- Rà: client không có random quyết hero.

# Rủi ro

- **Client tự random/đoán kết quả (cheat)** → RNG server-only; client chỉ hiển thị.
- **Double summon/mất tiền hai lần** → idempotency + transaction (mẫu 31).
- **Rate/pity sai lệch cảm nhận (bad feel)** → test phân phối + pity; tune qua config (EC1).
- **10-pull nửa chừng** → xử lý atomic toàn bộ.

# Ghi chú

Đây là **P3** một phần: hoàn tất "collection core". Rate/pity là tuning (config, EC1) — roadmap không chốt số. Bám [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md) + ADR-004/007/011.

# Technical Debt Review

- **Maintainability:** banner/rate/pity là data; thêm banner không sửa code.
- **Scalability:** nền cho banner rotation (LiveOps).
- **Testing:** phân phối rate + pity + idempotent là hợp đồng.
- **Security:** hệ nhạy cảm — server-authoritative, RNG server, audit seed.
- **Nợ:** banner limited/rotation (Post-MVP); tune số (content).

# Phase Review

Đóng khi gacha rate+pity server-side + atomic idempotent + dupes→fragment + data-driven, test phân phối/pity xanh. **Hoàn tất Collection Core.**

---

## Liên kết
- [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md) · [`../gameplay/hero-system.md`](../gameplay/hero-system.md) · [`../mvp/06-game-economy.md`](../mvp/06-game-economy.md)
- ADR: [`../adr/ADR-011-combat-authority-and-determinism.md`](../adr/ADR-011-combat-authority-and-determinism.md) · [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md) · [`../adr/ADR-004-data-driven-design.md`](../adr/ADR-004-data-driven-design.md)
- Roadmap: [`README.md`](README.md) → kế: [`34-campaign-pve.md`](34-campaign-pve.md)
