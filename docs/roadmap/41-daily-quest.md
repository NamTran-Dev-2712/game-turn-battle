# 41 — Daily Quest

> Mục đích: Hiện thực **nhiệm vụ hằng ngày** — mục tiêu ngắn hạn tạo lý do quay lại; tiến độ theo hành động, reset theo **thời gian server**, thưởng khi hoàn tất (atomic).

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 9 Kinh tế & QoL | P4 | S10 | F16 |

# Mục tiêu

Bộ quest daily (config: điều kiện, tiến độ, thưởng); theo dõi tiến độ theo sự kiện (đánh trận, summon, claim AFK…); reset theo server-time; claim thưởng atomic idempotent.

# Lý do

Daily quest là công cụ retention (F16) — biến "chơi được" thành "quay lại". Cần cơ chế theo dõi tiến độ + reset server-time (ADR-006). Sau khi loop + kinh tế nền có.

# Phụ thuộc

- **Trước:** 30/33/37 (sự kiện tạo tiến độ), 31–32 (thưởng), 36 (server-time pattern), 21 (config quest).
- **Sau:** 47 (daily login), 51 (telemetry quest funnel).

# Phạm vi

- Quest định nghĩa config (loại điều kiện, mục tiêu, thưởng).
- Theo dõi tiến độ qua domain event/sự kiện (đánh trận, summon…); server-authoritative.
- Reset daily theo **server-time** (không client time).
- Claim thưởng atomic idempotent.
- Client: UI quest (tiến độ, claim).

# Không thuộc phạm vi

- Weekly quest (F26 — Post-MVP).
- Số liệu mục tiêu/thưởng (config).
- Achievement dài hạn (Post-MVP).

# Deliverables

- Quest daily + theo dõi tiến độ + reset server-time + claim atomic.
- Integration test: tiến độ theo sự kiện; reset đúng theo server-time; claim atomic idempotent; không claim khi chưa đạt.
- Client UI quest.
- Cập nhật [`../gameplay/quest-system.md`](../gameplay/quest-system.md).

# Công việc cần thực hiện

- [ ] Schema quest (mở rộng 06): loại điều kiện, mục tiêu, thưởng.
- [ ] Domain/Application: theo dõi tiến độ qua sự kiện (subscribe domain event: battle won, summon, afk claim…); server-authoritative.
- [ ] Reset daily theo `IClock` server (mốc reset cấu hình); tính lazy khi đọc.
- [ ] `ClaimQuestRewardCommand` atomic idempotent; chặn claim khi chưa đạt/đã claim.
- [ ] Client feature `quest/`: danh sách, tiến độ, claim.
- [ ] Integration test: tiến độ cộng theo sự kiện; reset theo server-time (mock clock); claim atomic/idempotent; chưa đạt→chặn.
- [ ] Cập nhật `../gameplay/quest-system.md`.

# Tiêu chí hoàn thành

- Tiến độ cập nhật đúng theo hành động; server-authoritative.
- Reset daily theo **server-time** (mock clock qua mốc → reset).
- Claim atomic idempotent; chưa đạt/đã claim → chặn.
- Data-driven (đổi mục tiêu/thưởng config → đổi).

# Cách kiểm tra

- `dotnet test`: tiến độ theo sự kiện, reset server-time, claim atomic/idempotent, chưa đạt chặn.
- Local: đánh trận/summon → quest tiến; claim → thưởng vào ví/kho.
- Mock clock qua mốc reset → quest reset.

# Rủi ro

- **Reset theo client time (cheat)** → server-time bắt buộc.
- **Double claim** → idempotency.
- **Tiến độ sai do bỏ sót sự kiện** → subscribe domain event nhất quán; test.

# Ghi chú

Quest theo dõi qua domain event (nối phase 09/10). Reset server-time (ADR-006). Số liệu là tuning. Bám [`../gameplay/quest-system.md`](../gameplay/quest-system.md).

# Technical Debt Review

- **Maintainability:** quest là data + event-driven tiến độ.
- **Scalability:** thêm loại quest qua config/handler.
- **Testing:** tiến độ/reset/claim có test.
- **Security:** server-time + server-authoritative.
- **Nợ:** weekly/achievement (Post-MVP).

# Phase Review

Đóng khi quest daily tiến độ event-driven + reset server-time + claim atomic idempotent + data-driven, test xanh.

---

## Liên kết
- [`../gameplay/quest-system.md`](../gameplay/quest-system.md) · [`../mvp/07-liveops-planning.md`](../mvp/07-liveops-planning.md)
- ADR: [`../adr/ADR-006-liveops.md`](../adr/ADR-006-liveops.md) · [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md)
- Roadmap: [`README.md`](README.md) → kế: [`42-mail-system.md`](42-mail-system.md)
