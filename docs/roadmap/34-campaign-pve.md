# 34 — Campaign PvE (stage chain, progression)

> Mục đích: Hiện thực **chiến dịch PvE** (chuỗi stage), nguồn tài nguyên chính và trục tiến độ; quyết định "AFK stage" hiện hành — server-authoritative.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 8 Đóng vòng Core Loop | P3 | S9 | F05 |

# Mục tiêu

Chuỗi stage campaign (config-driven) với tiến độ người chơi (stage cao nhất đã qua); đánh stage dùng battle flow (30); thắng → mở stage kế + thưởng; tiến độ đặt "AFK stage" cho phase 37.

# Lý do

Campaign là trục tiến độ chính (F05) và nguồn thưởng/AFK. Cần sau battle (30) + collection (31–33) để có "đích để đẩy". Là tiền đề cho AFK (37) đóng vòng.

# Phụ thuộc

- **Trước:** 30 (battle), 31–33 (currency/inventory/gacha), 21 (stage config).
- **Sau:** 36 (energy gate), 37 (AFK theo stage), 35 (upgrade để vượt wall).

# Phạm vi

- Stage/chapter định nghĩa config (địch, thưởng, điều kiện mở khoá) — số liệu ở config.
- Tiến độ người chơi server-authoritative (stage cao nhất clear); mở khoá tuần tự.
- Đánh stage qua battle flow (30); thắng → cập nhật tiến độ + thưởng (atomic).
- Lưu "current AFK stage" = stage tiến độ (dùng phase 37).

# Không thuộc phạm vi

- AFK accrual (phase 37).
- Energy (phase 36).
- Tower/game mode khác (Could/Post-MVP).

# Deliverables

- Campaign model + tiến độ + mở khoá.
- Client: màn campaign (chọn stage, đánh, tiến độ).
- Integration test: clear stage → mở kế + thưởng; không skip stage khoá; tiến độ server-authoritative.
- Cập nhật [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md) + [`../mvp/02-core-game-loop.md`](../mvp/02-core-game-loop.md).

# Công việc cần thực hiện

- [ ] Schema stage/chapter (mở rộng 06): địch (team địch), thưởng, điều kiện mở khoá.
- [ ] Domain: `CampaignProgress` (gắn profile: stage cao nhất clear).
- [ ] Application: `StartCampaignBattleCommand` (dùng battle flow 30) → thắng cập nhật tiến độ + thưởng atomic; `GetCampaignProgressQuery`.
- [ ] Ràng buộc: chỉ đánh stage đã mở (tuần tự); chống skip.
- [ ] Đặt "current AFK stage" từ tiến độ (cho phase 37).
- [ ] Client feature `campaign/`: danh sách stage, trạng thái mở/khoá, đánh, hiển thị tiến độ.
- [ ] Integration test: clear→mở kế+thưởng; đánh stage khoá bị chặn; tiến độ đúng.
- [ ] Cập nhật `../gameplay/progression-and-economy.md`.

# Tiêu chí hoàn thành

- Đánh stage (battle flow) → thắng cập nhật tiến độ + thưởng (atomic, server).
- Không thể đánh stage chưa mở (chống skip).
- Tiến độ server-authoritative; client hiển thị.
- "Current AFK stage" phản ánh tiến độ (sẵn cho 37).

# Cách kiểm tra

- `dotnet test`: clear→mở+thưởng; stage khoá chặn; tiến độ.
- Local: đẩy vài stage → tiến độ tăng, thưởng vào kho/ví.
- gdUnit4: màn campaign trạng thái mở/khoá đúng.

# Rủi ro

- **Skip stage/cheat tiến độ** → server validate tuần tự + battle server-authoritative.
- **Thưởng nửa chừng** → atomic với cập nhật tiến độ.
- **Khó/wall quá sớm** → số liệu ở config, tune được (không sửa code).

# Ghi chú

Campaign đặt AFK-stage rate (nguồn AFK phase 37). Số liệu (địch/thưởng/độ khó) là config/tuning. Bám [`../mvp/02-core-game-loop.md`](../mvp/02-core-game-loop.md) + [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md).

# Technical Debt Review

- **Maintainability:** stage là data; thêm chapter không sửa code.
- **Scalability:** hỗ trợ nhiều chapter/stage.
- **Testing:** tiến độ/mở khoá/thưởng có test.
- **Security:** tiến độ + thưởng server-authoritative.
- **Nợ:** AFK (37); energy (36); tower (Post-MVP).

# Phase Review

Đóng khi campaign chain + tiến độ + thưởng server-authoritative, chống skip, sẵn AFK-stage, test xanh.

---

## Liên kết
- [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md) · [`../mvp/02-core-game-loop.md`](../mvp/02-core-game-loop.md) · [`../mvp/05-player-progression.md`](../mvp/05-player-progression.md)
- ADR: [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md) · [`../adr/ADR-011-combat-authority-and-determinism.md`](../adr/ADR-011-combat-authority-and-determinism.md)
- Roadmap: [`README.md`](README.md) → kế: [`35-hero-upgrade-level.md`](35-hero-upgrade-level.md)
