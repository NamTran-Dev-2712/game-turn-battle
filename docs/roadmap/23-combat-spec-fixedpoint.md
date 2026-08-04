# 23 — Combat spec & fixed-point math + golden vector format

> Mục đích: Chốt **đặc tả combat dùng chung** (một ruleset, hai hiện thực) + thư viện **fixed-point/integer math** + **định dạng golden test vector** — nền cho combat deterministic (ADR-011).

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 5 Deterministic Combat Core | P2 | S6 | F04 (nền) |

# Mục tiêu

Viết đặc tả combat chi tiết đủ để hiện thực **giống hệt** ở .NET và GDScript (thứ tự lượt, target/aggro, energy/ultimate, damage formula bằng integer/fixed-point, seeded RNG), + thư viện fixed-point dùng chung về mặt quy tắc, + định dạng golden vector (input: config version, team snapshot, stage, seed → output: log + kết quả).

# Lý do

ADR-011: combat là **rủi ro kỹ thuật cao nhất**, phải deterministic-by-seed và giống nhau hai phía. Chốt spec + math + vector format **trước** khi code sim để hai hiện thực không lệch, và có "hợp đồng kiểm" (golden vector) ngay từ đầu.

# Phụ thuộc

- **Trước:** 22 (đọc hero/skill config), 06 (schema hero/skill), ADR-011 (Accepted).
- **Sau:** 24 (sim server), 25 (sim client), 26 (golden vectors).

# Phạm vi

- Đặc tả combat: vòng lượt, thứ tự hành động (ổn định, không phụ thuộc iteration-order), target/aggro theo vị trí, energy/cooldown/ultimate, công thức damage/crit/miss **bằng integer/fixed-point**, điều kiện thắng/thua/hoà.
- Thư viện fixed-point/integer math: phép toán xác định, làm tròn cố định, không `float`.
- Seeded PRNG: thuật toán cụ thể (cùng công thức hai phía), seed truyền vào (không global RNG).
- Định dạng golden vector (JSON): input đầy đủ + output kỳ vọng (log sự kiện + kết quả).

# Không thuộc phạm vi

- Hiện thực sim (phase 24–25).
- Bộ vector đầy đủ (phase 26 — ở đây chỉ định dạng + 1–2 vector mẫu).
- Hiển thị/animation client (phase 30).

# Deliverables

- Tài liệu spec combat (mở rộng [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md)) đủ để hai người hiện thực ra kết quả trùng.
- Đặc tả thuật toán PRNG + fixed-point (pseudo-code ngôn ngữ-trung lập).
- Schema/định dạng golden vector + 1–2 vector mẫu.
- Danh sách "quy tắc xác định" (no float, thứ tự ổn định, RNG seeded) đưa vào `../conventions/code-style.md`.

# Công việc cần thực hiện

- [ ] Viết spec vòng lượt & thứ tự hành động **xác định** (tiêu chí sắp xếp rõ, không dựa thứ tự dictionary).
- [ ] Đặc tả target/aggro theo vị trí (CB3), energy/ultimate (CB4), crit/miss (CB5) — số liệu để config, spec chỉ mô tả cơ chế; link [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md).
- [ ] Đặc tả công thức damage bằng integer/fixed-point (điểm làm tròn cố định).
- [ ] Chọn & đặc tả thuật toán PRNG seeded (vd PCG/xorshift) — pseudo-code, cùng hai phía.
- [ ] Định nghĩa định dạng golden vector JSON (config_version, team snapshot, stage, seed → event log + result).
- [ ] Tạo 1–2 vector mẫu tay/tham chiếu để 24–25 kiểm sơ bộ.
- [ ] Ghi "quy tắc xác định" vào `../conventions/code-style.md` + [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md).
- [ ] Cập nhật các open-question combat (CB1–CB6) đã chốt/để lại trong `../mvp/10`.

# Tiêu chí hoàn thành

- Spec đủ chi tiết: hai lập trình viên độc lập hiện thực → cùng output cho vector mẫu (kiểm ý niệm).
- Không `float` trong bất kỳ phép toán combat nào (spec cấm rõ).
- PRNG seeded xác định, công thức mô tả đủ để tái lập.
- Định dạng golden vector rõ + vector mẫu hợp lệ.

# Cách kiểm tra

- Review chéo spec bởi vai trò combat-determinism (charter `.agents/`).
- Tính tay/tham chiếu 1 vector mẫu → khớp mô tả spec.
- Rà spec: mọi giá trị combat là integer/fixed-point; RNG nhận seed.

# Rủi ro

- **Spec mơ hồ → hai hiện thực lệch** → viết pseudo-code + vector mẫu làm "chân lý"; review chặt.
- **Iteration-order không ổn định** → quy định tiêu chí sắp xếp tường minh (id ổn định).
- **Fixed-point làm tròn khác nhau** → đặc tả điểm & luật làm tròn duy nhất.

# Ghi chú

Đây là phase **đặc tả**, không code sim. Là "hợp đồng combat" mà phase 24–25 phải tuân. Số liệu balance để config (data-driven); spec chỉ mô tả cơ chế. Bám ADR-011 + [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md).

# Technical Debt Review

- **Maintainability:** một spec cho hai hiện thực giảm drift.
- **Scalability:** thêm skill/effect qua registry (ADR-004), không sửa lõi.
- **Testing:** golden vector format là xương sống kiểm.
- **Security:** determinism là nền chống cheat (server re-sim).
- **Nợ:** bộ vector đầy đủ & skill phức tạp (phase 26/28).

# Phase Review

Đóng khi spec + fixed-point + PRNG + golden format chốt, vector mẫu khớp, quy tắc xác định tài liệu hoá, review combat-determinism đạt.

---

## Liên kết
- [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md) · [`../gameplay/skill-framework.md`](../gameplay/skill-framework.md) · [`../conventions/code-style.md`](../conventions/code-style.md) · [`../mvp/10-open-questions.md`](../mvp/10-open-questions.md)
- ADR: [`../adr/ADR-011-combat-authority-and-determinism.md`](../adr/ADR-011-combat-authority-and-determinism.md)
- Roadmap: [`README.md`](README.md) → kế: [`24-combat-sim-server.md`](24-combat-sim-server.md)
