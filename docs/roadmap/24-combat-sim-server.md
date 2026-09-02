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

- [x] Dựng cấu trúc sim: state trận, đơn vị, vòng lượt theo spec 23 (thứ tự xác định). — `GameTeam.Domain/Combat/` (`BattleSimulator` §12–§19, `State/UnitState`, thứ tự lượt `(-spd, actor_id)` ổn định; outcome test DEFEAT/DRAW/turn-order xanh).
- [x] Fixed-point math lib .NET (integer, làm tròn cố định) — cấm `float`. — `Numerics/FixedPoint` (long, `FixedScale=1000`, round-half-up một luật, guard chia-0/âm); `FixedPointTests` (biên + worked example 158/334) xanh; guard cấm float/double.
- [x] Seeded PRNG .NET theo thuật toán spec (nhận seed, không global). — `Rng/Pcg32` (PCG32+SplitMix64, seed `ulong`, `unchecked` wrap, một stream/trận); `Pcg32Tests` khớp roll golden (12345→7329/4605, 999→8003/8884), same-seed same-sequence xanh.
- [x] Đọc chỉ số từ `IConfigProvider` (hero/skill/stage) — data-driven, không hardcode. — `GameTeam.Application/Combat/CombatInputResolver` + config POCO; `CombatDataDrivenTests`: đổi atk config 200→400 ⇒ damage 158→316, ít vòng hơn, KHÔNG sửa code; thiếu config ⇒ `Result` lỗi.
- [x] Registry handler skill (effect-data → handler), thêm vài skill mẫu. — `Effects/EffectRegistry` + `IEffectHandler`; mẫu `DamageEffectHandler`/`HealEffectHandler`; unknown effect ⇒ ném; `EffectRegistryTests` xanh; lõi không `switch(skillId)`.
- [x] Sinh event log + kết quả đúng golden format. — `Events/*` (13 loại) + `Serialization/CombatEventSerializer` (seq theo vị trí); `GoldenVectorTests` khớp **từng sự kiện + result** cho `vector_01`/`vector_02`.
- [x] Unit test determinism: chạy 1 input × N lần → output byte-đồng nhất; khớp vector mẫu. — `BattleSimulatorDeterminismTests` N=200 byte-identical (2 vector) + seed khác ⇒ output khác; golden khớp bit-for-bit.
- [x] NetArchTest/guard: sim không ref EF/HTTP; không `DateTime.Now`; không `float`. — `CombatPuritySourceScanTests` (quét mã: cấm float/double/DateTime/Stopwatch/RNG global/Guid.NewGuid) + NetArchTest `Combat_domain_sim_should_not_depend_on_framework_or_persistence`; negative-test (inject `double`) đỏ → revert xanh.
- [x] Cập nhật `../gameplay/combat-framework.md`. — thêm **§21 Hiện thực server (.NET)** (kiến trúc/phân tầng, fixed-point/PRNG, registry, event log, test); doc-sync `.instructions/{combat,backend}.md` + `.claude/agents/{combat-determinism,dotnet-backend}.md` + CLAUDE.md §4.6 + `.memory/0022`.

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

## Kết quả (2026-09-02 — local PASS)

**Hoàn tất Phase 24 — sim combat server .NET là nguồn chân lý (ADR-011); sẵn sàng cho client (25) khớp bit-for-bit + golden suite (26).**

- **Lõi thuần** `GameTeam.Domain/Combat/` (package-free): `Numerics/FixedPoint`, `Rng/Pcg32`, `Model/*` (`BattleInput` tự chứa),
  `State/UnitState`, `Events/*` (13 loại), `Effects/*` (registry + `IEffectHandler` + `DamageEffectHandler`/`HealEffectHandler`),
  `Serialization/CombatEventSerializer`, `BattleSimulator.Simulate(BattleInput)→BattleOutput`. Không I/O / wall-clock (không cần
  `IClock`) / `float`/`double` / RNG global.
- **Data-driven** `GameTeam.Application/Combat/`: `CombatInputResolver` đọc hero/skill/stage qua `IConfigProvider` → `BattleInput`
  (config POCO; `combat_rules` nguồn stage config — hình thức hoá schema là follow-up). Không endpoint (phase 30).
- **Verify (local, không cần Docker):** `dotnet build server/GameTeam.sln -c Release` 0 error; `dotnet test` **Domain 77 / Application 45 /
  Contracts 36** pass. Golden `vector_01_basic_hit`(59 ev) + `vector_02_crit_ko`(30 ev) khớp **từng sự kiện + result**; determinism
  **N=200 byte-identical**; data-driven (atk 200→400 ⇒ dmg 158→316); purity guard đỏ→revert xanh; không drift `openapi.json`/generated.
- **Ngoài scope (giữ nguyên):** client sim (25), battle endpoint (30), skill nội dung đầy đủ (28). Năng lượng/ultimate (§15, CB4
  `[ĐỀ XUẤT]`) nối sẵn nhưng **chưa kích hoạt** — không tự đóng CB3/CB4. Bộ vector đầy đủ + CI gate cross-impl = phase 26.
- **CI-pending:** Infra/Api integration test (Testcontainers) cần Docker — không liên quan Phase 24 (chỉ chạm Domain/Application).
- Doc-sync: `combat-framework.md` §21 + `.instructions/{combat,backend}.md` + `.claude/agents/{combat-determinism,dotnet-backend}.md` +
  CLAUDE.md §4.6 + `.memory/0022-combat-sim-server-standardized.md` (+README).

---

## Liên kết
- [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md) · [`../gameplay/skill-framework.md`](../gameplay/skill-framework.md) · [`../backend/domain-and-application.md`](../backend/domain-and-application.md)
- ADR: [`../adr/ADR-011-combat-authority-and-determinism.md`](../adr/ADR-011-combat-authority-and-determinism.md) · [`../adr/ADR-004-data-driven-design.md`](../adr/ADR-004-data-driven-design.md)
- Roadmap: [`README.md`](README.md) → kế: [`25-combat-sim-client.md`](25-combat-sim-client.md)
