# 36 — Energy system (server-time)

> Mục đích: Hiện thực **Energy** — cổng nhịp chơi chủ động (farm), hồi theo **thời gian server** (chống clock-cheat), tiêu khi đánh stage active.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 8 Đóng vòng Core Loop | P3 | S9 | F08 |

# Mục tiêu

Energy gắn profile: hồi theo thời gian server (rate/cap config), tiêu khi đánh stage active; tính hồi khi đọc (lazy, dựa `IClock` server); server-authoritative, không phụ thuộc client time.

# Lý do

Energy điều tiết farm chủ động, bổ trợ AFK (A07: AFK nền, Energy tăng tốc). Phải server-time (ADR-006/007) chống gian lận chỉnh đồng hồ. Cần trước/song song AFK (37) để loop có nhịp.

# Phụ thuộc

- **Trước:** 34 (stage active tiêu energy), 31 (nếu mua energy bằng gem), 09/11 (IClock server).
- **Sau:** 37 (AFK cùng dùng server-time), 41 (quest có thể thưởng energy).

# Phạm vi

- Energy state (current, cap) gắn profile; rate hồi + cap theo config (EC3).
- Tính hồi **lazy** theo server time (`IClock`) khi đọc/tiêu (không cron mỗi giây).
- Tiêu energy khi đánh stage active (campaign 34).
- (Tuỳ chọn) mua energy bằng gem — spend atomic (31).

# Không thuộc phạm vi

- AFK accrual (phase 37) — khác cơ chế (idle reward).
- Số liệu rate/cap/giá (config — EC3).
- VIP/loyalty tăng cap (Post-MVP).

# Deliverables

- Energy model + tính hồi lazy theo server-time + tiêu.
- Integration test: hồi đúng theo thời gian server (mock IClock); tiêu chặn khi thiếu; cap không vượt.
- Client: hiển thị energy + đếm hồi (chỉ hiển thị, chân lý server).
- Cập nhật [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md).

# Công việc cần thực hiện

- [ ] Domain: `Energy` (current, cap, lastUpdate server time); rate/cap từ config.
- [ ] Tính hồi lazy: khi đọc/tiêu → cộng theo (now_server − lastUpdate) × rate, giới hạn cap.
- [ ] Application: `SpendEnergyCommand` (đánh stage active), `GetEnergyQuery`; dùng `IClock` server.
- [ ] (Tuỳ chọn) `BuyEnergyCommand` bằng gem (spend atomic 31).
- [ ] Client: hiển thị energy + đồng hồ hồi (ước lượng hiển thị; chân lý server khi refresh).
- [ ] Integration test: hồi theo thời gian (mock clock tiến X phút), tiêu thiếu→chặn, cap trần, không dùng client time.
- [ ] Cập nhật `../gameplay/progression-and-economy.md`.

# Tiêu chí hoàn thành

- Energy hồi đúng theo **server time** (mock IClock tiến thời gian → cộng đúng, không vượt cap).
- Tiêu energy khi đánh active; thiếu → chặn.
- Không phụ thuộc client time (chỉnh đồng hồ client không ảnh hưởng).
- Server-authoritative; client chỉ hiển thị.

# Cách kiểm tra

- `dotnet test`: mock IClock tiến thời gian → energy hồi đúng; cap; tiêu thiếu chặn.
- Local: đánh active tốn energy; chờ (hoặc mock) → hồi.
- Thử đổi giờ thiết bị client → energy không đổi (chân lý server).

# Rủi ro

- **Clock-cheat (client)** → mọi tính dựa `IClock` server; client time chỉ hiển thị.
- **Cron nặng mỗi giây** → tính lazy khi đọc/tiêu.
- **Vượt cap do tính sai** → giới hạn cap trong công thức.

# Ghi chú

Energy = accelerator; AFK = base source (A07). Số liệu rate/cap/giá là tuning (config, EC3). Bám ADR-006/007 (server-time) + [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md).

# Technical Debt Review

- **Maintainability:** tính lazy đơn giản; rate/cap config.
- **Scalability:** không cron; tính O(1) khi đọc.
- **Testing:** mock clock cho thời gian.
- **Security:** server-time chống clock-cheat.
- **Nợ:** VIP cap, mua energy nâng cao (Post-MVP).

# Phase Review

Đóng khi energy hồi theo server-time (mock clock) + tiêu + cap + không phụ thuộc client time, test xanh.

---

## Liên kết
- [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md) · [`../mvp/06-game-economy.md`](../mvp/06-game-economy.md)
- ADR: [`../adr/ADR-006-liveops.md`](../adr/ADR-006-liveops.md) · [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md)
- Roadmap: [`README.md`](README.md) → kế: [`37-afk-idle-rewards.md`](37-afk-idle-rewards.md)
