# 37 — AFK/Idle rewards (server-side on claim) — chốt MVP loop

> Mục đích: Hiện thực **thưởng AFK/Idle** — cơ chế định danh thể loại: tích luỹ offline theo thời gian **server**, có trần, cấp khi **claim** (atomic, idempotent). Đây là mắt xích **đóng vòng lặp cốt lõi MVP**.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 8 Đóng vòng Core Loop | P3 | S9 | F07 |

# Mục tiêu

Tài nguyên AFK tích theo (now_server − lastClaim) × rate(AFK-stage) đến trần (cap); người chơi **claim** → server tính + cấp (currency/item) atomic idempotent; client hiển thị ước lượng. Kết thúc phase này = **MVP Must loop chạy được**.

# Lý do

AFK là cơ chế genre-defining (F07, north star Idle Heroes). Phải server-side khi claim (ADR-007, chống cheat thời gian). Đóng vòng: summon→team→đánh→thưởng→nâng cấp→đẩy xa→**AFK**→lặp (mvp/02).

# Phụ thuộc

- **Trước:** 34 (AFK-stage rate), 36 (server-time pattern), 31–32 (cấp currency/item), 35 (sink nâng cấp).
- **Sau:** 41–42 (quest/mail bổ trợ), 47 (daily login), 43 (sweep dùng determinism).

# Phạm vi

- AFK state (lastClaim server time) gắn profile; rate theo AFK-stage (tiến độ campaign 34) + config.
- Tính **lazy** khi xem/claim theo `IClock` server; trần cap (offline lâu không tích vô hạn).
- Command `ClaimAfkCommand` atomic + idempotency (claim đúp không double).
- Client: màn AFK hiển thị ước lượng tích luỹ + nút claim.

# Không thuộc phạm vi

- Số liệu rate/cap (config — EC2).
- Fast reward/mua thời gian AFK (Post-MVP).
- Mail/quest (phase 41–42).

# Deliverables

- AFK accrual server-side + claim atomic idempotent.
- Client màn AFK (ước lượng + claim).
- Integration test: tích theo thời gian server (mock clock), trần cap, claim atomic, idempotent (không double), rate theo stage.
- Cập nhật [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md) + [`../mvp/02-core-game-loop.md`](../mvp/02-core-game-loop.md).
- **Ghi mốc "MVP loop khép kín" vào [`../audit/bootstrap-audit.md`](../audit/bootstrap-audit.md)/roadmap.**

# Công việc cần thực hiện

- [ ] Domain: `AfkState` (lastClaim server time); rate = f(AFK-stage 34) + config; cap từ config.
- [ ] Tính lazy khi query/claim: reward = min(cap, (now_server − lastClaim) × rate).
- [ ] Application: `ClaimAfkCommand` (idempotent) → cấp currency/item atomic (31/32) → reset lastClaim; `GetAfkQuery` (ước lượng hiện tại).
- [ ] Client feature (trong campaign/hub): hiển thị ước lượng tích luỹ (đồng hồ) + nút claim.
- [ ] Integration test: mock clock tiến X giờ → tích đúng; vượt X → capped; claim atomic; claim đúp idempotent (không double); rate theo stage cao hơn → nhiều hơn.
- [ ] Chạy **kịch bản loop khép kín** end-to-end (summon→team→battle→reward→upgrade→push→AFK claim) như smoke thủ công.
- [ ] Cập nhật `../gameplay/progression-and-economy.md`, `../mvp/02-core-game-loop.md`; đánh dấu mốc.

# Tiêu chí hoàn thành

- AFK tích theo **server time** (mock clock), có **trần cap**; rate theo AFK-stage.
- Claim cấp thưởng atomic; claim đúp **không** double (idempotent).
- Không phụ thuộc client time (chống cheat).
- **Vòng lặp cốt lõi chạy end-to-end** (kịch bản smoke thủ công xanh).

# Cách kiểm tra

- `dotnet test`: mock clock → tích/capped/rate-by-stage; claim atomic + idempotent.
- Local end-to-end: summon → lập đội → đánh campaign → nhận thưởng → nâng cấp → đẩy stage → (mock/chờ) → claim AFK. Loop chạy trơn.
- Claim đúp (retry) → không double.

# Rủi ro

- **Clock-cheat** → tính theo `IClock` server khi claim; client chỉ ước lượng.
- **Double-claim** → idempotency (mẫu 31).
- **Offline lâu tích vô hạn** → cap bắt buộc.
- **AFK cap thấp gây bực (bottleneck)** → số ở config, tune (EC2).

# Ghi chú

**Đây là cột mốc quan trọng nhất (P3 / M3): MVP Must loop khép kín.** Số liệu AFK là tuning (config, EC2). Sau phase này, các phase còn lại thêm chiều sâu/QoL/vận hành, không phá loop. Bám [`../mvp/02-core-game-loop.md`](../mvp/02-core-game-loop.md) + ADR-006/007.

# Technical Debt Review

- **Maintainability:** tính lazy đơn giản; rate/cap config.
- **Scalability:** không cron; O(1) khi claim.
- **Testing:** mock clock + idempotent + loop smoke.
- **Security:** server-time + idempotent (chống cheat/double).
- **Nợ:** fast reward (Post-MVP); tune số (content).

# Phase Review

Đóng khi AFK accrual server-time + claim atomic idempotent + cap + loop end-to-end chạy. **🎯 Chốt MVP Must loop — cột mốc P3/M3.**

---

## Liên kết
- [`../mvp/02-core-game-loop.md`](../mvp/02-core-game-loop.md) · [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md) · [`../mvp/06-game-economy.md`](../mvp/06-game-economy.md)
- ADR: [`../adr/ADR-006-liveops.md`](../adr/ADR-006-liveops.md) · [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md)
- Roadmap: [`README.md`](README.md) → kế: [`38-equipment.md`](38-equipment.md)
