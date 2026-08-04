# 43 — Sweep/Quick-battle + tua 2x

> Mục đích: Hiện thực **sweep/quick-battle** (bỏ qua xem trận, nhận thưởng) và **tua 2x** — QoL cày, **tái dùng tính tất định** server-side (ADR-011).

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 9 Kinh tế & QoL | P4 | S11 | F22, F23 |

# Mục tiêu

Sweep stage đã clear: server tính kết quả bằng sim (không cần client xem), cấp thưởng atomic; tua 2x: client tăng tốc replay (không đổi kết quả). Cả hai giữ server-authoritative.

# Lý do

QoL cày (F22/F23) giảm nhàm cho idle game. Sweep **tái dùng determinism** (server sim không cần render) — đúng ADR-011 (sweep dùng lại tính tất định). 2x chỉ ảnh hưởng hiển thị.

# Phụ thuộc

- **Trước:** 30 (battle flow), 24 (sim server), 34 (stage đã clear), 31–32 (thưởng).
- **Sau:** tuning; 48 (hardening).

# Phạm vi

- Sweep: `SweepCommand` cho stage đã clear → server sim (24) không render → cấp thưởng atomic idempotent; giới hạn (energy/số lần) theo config.
- 2x speed: client tăng tốc replay (chỉ hiển thị); toggle trong battle.
- Đảm bảo sweep dùng determinism (kết quả nhất quán như đánh thường).

# Không thuộc phạm vi

- 4x/skip nâng cao (Could — có thể thêm sau).
- Auto-repeat/farm bot (Post-MVP).
- Số liệu giới hạn (config).

# Deliverables

- Sweep server-side + 2x client.
- Integration test: sweep cấp thưởng đúng (dựa sim), atomic idempotent, giới hạn, chỉ stage đã clear; 2x không đổi kết quả.
- Client: nút sweep + toggle 2x.
- Cập nhật [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md) + [`../mvp/04-feature-analysis.md`](../mvp/04-feature-analysis.md).

# Công việc cần thực hiện

- [ ] Application `SweepCommand`: chỉ stage đã clear; server sim (24) tính kết quả không render; cấp thưởng atomic idempotent; tiêu energy/giới hạn theo config.
- [ ] Đảm bảo sweep dùng cùng sim/config version → kết quả nhất quán (determinism).
- [ ] Client: nút sweep (single/multi) + hiển thị tổng thưởng.
- [ ] Client 2x: tăng tốc replay (time scale) không đổi seed/kết quả; toggle.
- [ ] Integration test: sweep thưởng đúng, atomic/idempotent, giới hạn, stage chưa clear chặn.
- [ ] gdUnit4: 2x không đổi outcome (cùng seed → cùng kết quả, chỉ nhanh hơn).
- [ ] Cập nhật `../gameplay/combat-framework.md`.

# Tiêu chí hoàn thành

- Sweep server-side cấp thưởng đúng (dựa determinism), atomic idempotent, chỉ stage đã clear, tôn trọng giới hạn.
- 2x chỉ đổi tốc độ hiển thị, **không** đổi kết quả (test đối chứng).
- Server-authoritative; client không tự tính thưởng sweep.

# Cách kiểm tra

- `dotnet test`: sweep thưởng/atomic/idempotent/giới hạn/stage-chưa-clear.
- gdUnit4: 2x cùng seed → cùng outcome, nhanh hơn.
- Local: sweep stage đã clear → thưởng vào ví/kho; retry không double.

# Rủi ro

- **Client tự tính sweep (cheat)** → server sim + cấp thưởng; client chỉ yêu cầu.
- **Double sweep reward** → idempotency.
- **2x đổi kết quả do time-step** → sim theo lượt/tick logic, không theo delta-time thực.

# Ghi chú

Sweep là minh hoạ đẹp cho ADR-011 (tái dùng determinism không render). 2x là hiển thị thuần. Số liệu giới hạn là config. Bám [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md).

# Technical Debt Review

- **Maintainability:** tái dùng sim; ít code mới.
- **Scalability:** sweep giảm tải render; server tính nhanh.
- **Testing:** sweep + 2x-không-đổi-kết-quả có test.
- **Security:** server-authoritative.
- **Nợ:** 4x/auto-repeat (Post-MVP).

# Phase Review

Đóng khi sweep server-side (dựa determinism) atomic idempotent + 2x không đổi kết quả, test xanh.

---

## Liên kết
- [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md) · [`../mvp/04-feature-analysis.md`](../mvp/04-feature-analysis.md)
- ADR: [`../adr/ADR-011-combat-authority-and-determinism.md`](../adr/ADR-011-combat-authority-and-determinism.md)
- Roadmap: [`README.md`](README.md) → kế: [`44-faction-advantage.md`](44-faction-advantage.md)
