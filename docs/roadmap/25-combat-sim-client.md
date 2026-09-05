# 25 — Deterministic Combat Sim — client (GDScript)

> Mục đích: Hiện thực bộ combat sim phía client (GDScript) **giống hệt quy tắc** server (phase 24), dùng để **hiển thị/replay** theo seed — không phải chân lý (ADR-011).

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 5 Deterministic Combat Core | P2 | S6 | F04 |

# Mục tiêu

Sim GDScript thuần trong `client/src/combat/`, hiện thực đúng spec phase 23 (fixed-point, seeded PRNG, thứ tự lượt), đọc chỉ số từ ConfigProvider, cho **cùng seed+input → cùng output** như server; dùng để replay trận cho người chơi xem.

# Lý do

ADR-011: client sim chỉ để hiển thị/dự đoán; server là chân lý. Nhưng client sim phải **khớp bit** quy tắc với server để replay theo seed đúng với kết quả server trả về (phase 30).

# Phụ thuộc

- **Trước:** 24 (spec đã hiện thực server = đáp án), 16/22 (config client), 23 (spec).
- **Sau:** 26 (golden cross-check), 30 (replay trong battle flow).

# Phạm vi

- Sim GDScript thuần trong `combat/` (không gọi network/UI trong lõi sim).
- Fixed-point/integer math GDScript (khớp spec) — không `float` trong tính combat.
- Seeded PRNG GDScript (cùng thuật toán server).
- Đọc chỉ số từ ConfigProvider (data-driven).
- Output event log để UI replay (phase 30 vẽ).

# Không thuộc phạm vi

- Vẽ/animation trận (phase 30).
- Quyết định kết quả/thưởng (server — client không tự quyết).
- Golden cross-impl gate (phase 26).

# Deliverables

- Sim client GDScript thuần khớp quy tắc server.
- Test gdUnit4 determinism: cùng seed+input → cùng output; khớp vector mẫu (đáp án từ server 24).
- Cập nhật [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md) (phần client) + [`../godot/state-and-signals.md`](../godot/state-and-signals.md).

# Công việc cần thực hiện

- [x] Dựng sim GDScript theo spec 23 (state, vòng lượt, thứ tự xác định). — `client/src/combat/battle_simulator.gd`
- [x] Fixed-point/integer math GDScript khớp lib server (cùng điểm làm tròn); cấm `float` trong tính combat. — `client/src/shared/fixed_point.gd`
- [x] Seeded PRNG GDScript cùng thuật toán server (cùng seed → cùng chuỗi số). — `client/src/combat/rng/pcg32.gd` (seed 12345→7329/4605)
- [x] Đọc chỉ số từ ConfigProvider (phase 16/22) — cùng config version với server. — `client/src/combat/combat_input_resolver.gd`
- [x] Registry effect skill khớp server (cùng effect-data → cùng kết quả). — `client/src/combat/effects/*` + registry
- [x] Sinh event log đúng golden format để UI replay. — `client/src/combat/events/combat_events.gd` (13 loại, seq theo vị trí)
- [x] Test gdUnit4: determinism (N lần trùng) + khớp vector mẫu (đáp án server). — `client/tests/combat/*` (24–25 test xanh, 0 orphan)
- [x] Static typing, tab indent; lõi sim không phụ thuộc scene/UI. — grep sạch (không import `core/net`/UI; không `float`)
- [x] Cập nhật `../gameplay/combat-framework.md` (client). — §21.6 "Hiện thực client (GDScript) — Phase 25"

# Tiêu chí hoàn thành

- Cùng (config_version, team, stage, seed) → client sim ra **cùng** kết quả + log như server (khớp vector mẫu).
- Không `float` trong tính combat client.
- Sim lõi thuần (không network/UI bên trong).
- Test gdUnit4 xanh headless.

# Cách kiểm tra

- gdUnit4: determinism N lần; so output với vector mẫu (đáp án từ phase 24).
- So thủ công 1 vector: client log ≡ server log.
- Rà: lõi sim không import `core/net`/UI; không `float` combat.

# Rủi ro

- **Lệch fixed-point GDScript vs .NET** → dùng đúng điểm làm tròn spec; test biên nhiều case.
- **PRNG khác chuỗi** → cùng thuật toán + seed; test chuỗi số trùng.
- **Config version lệch server** → luôn dùng version server trả trong replay (phase 30).

# Ghi chú

Client sim **không** quyết kết quả; khi vào battle flow (phase 30), client replay bằng seed server trả và kết quả phải khớp. Bám ADR-011.

# Technical Debt Review

- **Maintainability:** sim client song ánh spec; đổi spec → đổi hai phía có kiểm (golden).
- **Scalability:** effect registry mở rộng cùng server.
- **Testing:** determinism + so vector mẫu.
- **Security:** client không authority; chỉ hiển thị.
- **Nợ:** golden cross-impl tự động (26); vẽ trận (30).

# Phase Review

Đóng khi sim client khớp quy tắc server (cùng seed→cùng output, khớp vector mẫu), thuần, không float, test gdUnit4 xanh.

## Kết quả (ĐÓNG — verify local 2026-09-04)

- **PASS.** Sim client GDScript (`client/src/combat/*` + `client/src/shared/fixed_point.gd`) song ánh bit-for-bit sim
  server (Phase 24): `BattleSimulator.simulate`, `FixedPoint` (round-half-up, **không float**), `Pcg32` (SplitMix64→PCG32,
  logical shift), 13 event type, `DamageEffectHandler`, effect registry, `CombatInputResolver`.
- **Test gdUnit4 xanh (Godot 4.7.1 local):** `client/tests/combat/*` — golden (khớp baseline server), determinism,
  outcome (DEFEAT/DRAW/miss/turn-order + tie-break), fixed_point, pcg32, resolver. **24–25 test, 0 failure, 0 orphan.**
- **Sửa nợ (Phase 26 phát hiện):** `battle_simulator_outcome_test.gd` có helper `_input(...)` trùng virtual
  `Node._input(InputEvent)` ⇒ Godot 4.7.1 parse-error chặn cả suite; đổi tên `_input`→`_make_input` (chỉ đổi tên helper test,
  không đổi hành vi sim).
- **Authority/độ thuần:** client chỉ replay/dự đoán, **không** quyết kết quả (ADR-011); grep sạch (lõi sim không import
  `core/net`/UI; không `float` trong tính combat).
- **Doc-sync:** `docs/gameplay/combat-framework.md` §21.6 (client) + §22 (golden Phase 26). Đủ điều kiện đóng.
- **CI-pending:** `ci-client.yml` chạy trên Actions (bộ combat + golden nằm trong `res://tests`).

---

## Liên kết
- [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md) · [`../godot/state-and-signals.md`](../godot/state-and-signals.md)
- ADR: [`../adr/ADR-011-combat-authority-and-determinism.md`](../adr/ADR-011-combat-authority-and-determinism.md) · [`../adr/ADR-002-godot-architecture.md`](../adr/ADR-002-godot-architecture.md)
- Roadmap: [`README.md`](README.md) → kế: [`26-combat-golden-vectors.md`](26-combat-golden-vectors.md)
