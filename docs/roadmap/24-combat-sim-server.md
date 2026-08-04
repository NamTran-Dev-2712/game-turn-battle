# 24 — Deterministic Combat Sim — server (.NET)

> Mục đích: Hiện thực bộ **combat sim thuần, deterministic** phía server (.NET) theo spec phase 23 — đây là **nguồn chân lý** kết quả trận (ADR-011).

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 5 Deterministic Combat Core | P2 | S6 | F04 |

# Mục tiêu

Sim .NET thuần (không I/O, integer/fixed-point, seeded RNG) trong Domain/Application: nhận (config version, team snapshot, stage, seed) → chạy → trả kết quả + event log. Đọc chỉ số từ config qua provider (data-driven). Là "authority" mà API battle sẽ gọi (phase 30).

# Lý do

ADR-011: server quyết kết quả & cấp thưởng; sim server là chân lý. Làm server trước (rồi client phase 25) để khoá "đáp án đúng" cho golden vector.

# Phụ thuộc

- **Trước:** 23 (spec/format), 21 (config provider), 09 (domain/base/IClock).
- **Sau:** 25 (client khớp), 26 (golden), 30 (battle flow).

# Phạm vi

- Sim thuần trong `GameTeam.Domain`/`Application` (không EF/HTTP/wall-clock).
- Đọc chỉ số hero/skill/stage từ `IConfigProvider` (không hardcode).
- Fixed-point math + seeded PRNG (theo spec 23).
- Output: kết quả (thắng/thua/hoà) + event log tái lập được (khớp golden format).
- Skill qua registry effect-data (ADR-004) — nền cho phase 28.

# Không thuộc phạm vi

- Hiện thực client (phase 25).
- Endpoint battle/cấp thưởng (phase 30).
- Skill nội dung đầy đủ (phase 28) — ở đây đủ cơ chế + vài skill mẫu.

# Deliverables

- Sim server .NET thuần + registry effect.
- Unit test: cùng seed+input → cùng output (lặp lại nhiều lần ổn định).
- Khớp vector mẫu phase 23.
- Cập nhật [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md) (chi tiết hiện thực server).

# Công việc cần thực hiện

- [ ] Dựng cấu trúc sim: state trận, đơn vị, vòng lượt theo spec 23 (thứ tự xác định).
- [ ] Fixed-point math lib .NET (integer, làm tròn cố định) — cấm `float`.
- [ ] Seeded PRNG .NET theo thuật toán spec (nhận seed, không global).
- [ ] Đọc chỉ số từ `IConfigProvider` (hero/skill/stage) — data-driven, không hardcode.
- [ ] Registry handler skill (effect-data → handler), thêm vài skill mẫu.
- [ ] Sinh event log + kết quả đúng golden format.
- [ ] Unit test determinism: chạy 1 input × N lần → output byte-đồng nhất; khớp vector mẫu.
- [ ] NetArchTest/guard: sim không ref EF/HTTP; không `DateTime.Now`; không `float`.
- [ ] Cập nhật `../gameplay/combat-framework.md`.

# Tiêu chí hoàn thành

- Cùng (config_version, team, stage, seed) → **cùng** kết quả + log qua N lần chạy.
- Khớp 1–2 vector mẫu phase 23.
- Sim thuần: không I/O, không wall-clock, không float (guard/test xác nhận).
- Chỉ số đọc từ config (đổi config → kết quả đổi tương ứng, không cần đổi code).

# Cách kiểm tra

- `dotnet test`: determinism (N lần trùng) + khớp vector mẫu.
- Grep/analyzer: không `float`/`double` trong sim; không `DateTime.Now`; không `Random` global.
- Đổi giá trị config hero → kết quả sim đổi (data-driven) trong test.

# Rủi ro

- **Lệch fixed-point/rounding** → dùng đúng lib & điểm làm tròn của spec; test biên.
- **Thứ tự lượt không ổn định** → sort theo tiêu chí tường minh (id/tốc độ ổn định).
- **RNG global lọt vào** → chỉ PRNG nhận seed; guard review.

# Ghi chú

Sim server là "đáp án" cho golden vector (phase 26). Sweep/quick-battle (phase 43) tái dùng sim này server-side. Bám ADR-011 (không float, seeded RNG, server-authoritative).

# Technical Debt Review

- **Maintainability:** sim thuần dễ test/đọc; skill qua registry.
- **Scalability:** thêm skill/effect không sửa lõi.
- **Testing:** determinism test là hợp đồng.
- **Security:** server-authoritative — chống cheat gốc.
- **Nợ:** skill nội dung đầy đủ (28); tối ưu perf sim (52).

# Phase Review

Đóng khi sim server deterministic (N lần trùng), thuần (no I/O/float/wall-clock), data-driven, khớp vector mẫu, test xanh.

---

## Liên kết
- [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md) · [`../gameplay/skill-framework.md`](../gameplay/skill-framework.md) · [`../backend/domain-and-application.md`](../backend/domain-and-application.md)
- ADR: [`../adr/ADR-011-combat-authority-and-determinism.md`](../adr/ADR-011-combat-authority-and-determinism.md) · [`../adr/ADR-004-data-driven-design.md`](../adr/ADR-004-data-driven-design.md)
- Roadmap: [`README.md`](README.md) → kế: [`25-combat-sim-client.md`](25-combat-sim-client.md)
