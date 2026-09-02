# 0022 — Deterministic combat sim server (.NET) standardized (Phase 24)

- **Trạng thái:** Đã chốt & verify cục bộ (2026-09-02). **Hiện thực** spec Phase 23 (§9–§20) thành sim server .NET —
  **nguồn chân lý** kết quả trận (ADR-011). KHÔNG đổi spec; đây là chi tiết hiện thực. Sim = phase 24 (server) này;
  client = phase 25; bộ vector đầy đủ + CI gate cross-impl = phase 26; endpoint battle = phase 30; skill nội dung = phase 28.
- **Bối cảnh:** Phase 23 chốt hợp đồng combat + golden format nhưng KHÔNG code sim. Phase 24 hiện thực đúng hợp đồng để
  server có "đáp án đúng" tất định cho golden vector.

## Quyết định (user-approved)

- **Phân tầng:** lõi sim **thuần** ở `GameTeam.Domain/Combat/` (package-free, nhận `BattleInput` **tự chứa**, KHÔNG
  `IConfigProvider`/`IClock`) + tầng **data-driven** `GameTeam.Application/Combat/CombatInputResolver` đọc config qua
  `IConfigProvider`. Khớp cách golden vector tự chứa; guard Domain purity phủ lõi.
- **Nguồn `combat_rules`:** đọc từ **stage config** (chưa hình thức hoá vào JSON Schema Phase 06/07 — **follow-up/nợ**);
  test data-driven dùng `IConfigProvider` in-memory nên không cần file `config/**` / đổi schema ở phase này.

## Thành phần

- **Lõi `GameTeam.Domain/Combat/`:** `Numerics/FixedPoint` (long, `FixedScale=1000`, `RoundHalfUp=(num+den/2)/den`,
  **một luật round-half-up**, guard chia-0/âm — **cấm float/double**); `Rng/Pcg32` (PCG32 `pcg_setseq_64_xsh_rr_32` +
  SplitMix64, seed `ulong` tường minh, **một stream/trận**, `unchecked` wrap mod 2^64, dịch phải logical — **không RNG
  global**); `Model/*` (`BattleInput`/`UnitSnapshot`/`CombatRules`/`SkillDef`/`EffectDef`…); `State/UnitState`; `Events/*`
  (13 loại, mỗi loại `WriteBody`); `Effects/*` (`EffectRegistry` `effect_type`→`IEffectHandler`; mẫu `DamageEffectHandler`
  §17 + `HealEffectHandler`; unknown ⇒ ném; **không `switch(skillId)`**); `Serialization/CombatEventSerializer` (JSON compact
  tất định, `seq` theo vị trí, `final_hp` ally→enemy); `BattleSimulator.Simulate(BattleInput)→BattleOutput` (§12–§19,
  thứ tự lượt `(-spd, actor_id)` stable, chọn mục tiêu `(slot, actor_id)`, end-check sau mỗi action/round).
- **Data-driven `GameTeam.Application/Combat/`:** config POCO (`HeroCombatConfig`/`SkillCombatConfig`/`StageCombatConfig`
  + `CombatRulesConfig`/`EnergyConfig`/`StageEnemyConfig`) snake_case; `CombatInputResolver.Resolve(BattleRequest)→Result<BattleInput>`
  (thiếu config ⇒ `CombatErrors.*`). Không endpoint (phase 30), không DI wiring (phase 30).
- **Năng lượng/ultimate (§15, CB4 `[ĐỀ XUẤT]`):** `EnergyRules`/`UnitState.Energy` nối sẵn nhưng **chưa kích hoạt** gain/
  ultimate (đề xuất, chưa canon; vector mẫu tắt) — **không tự đóng CB3/CB4**.

## Verify (local, KHÔNG cần Docker)

- `dotnet build server/GameTeam.sln -c Release` 0 error (warnings-as-error compiler); `dotnet test`:
  **Domain 77 / Application 45 / Contracts 36** pass.
- Golden `vector_01_basic_hit` (59 ev → VICTORY) + `vector_02_crit_ko` (30 ev → VICTORY) khớp **từng sự kiện + result**
  (structural compare). Determinism **N=200 byte-identical** (2 vector). PRNG khớp roll golden (12345→7329/4605; 999→8003/8884).
- Data-driven: đổi `hero.atk` config 200→400 ⇒ first damage 158→316, ít vòng hơn, **không sửa code combat**.
- Purity: `CombatPuritySourceScanTests` (cấm float/double/DateTime/Stopwatch/RNG global/Guid.NewGuid) + NetArchTest
  `Combat_domain_sim_should_not_depend_on_framework_or_persistence`; **negative-test** inject `double` ⇒ guard đỏ → revert xanh.
- Không drift `shared/contracts/openapi.json` / `client/src/data/generated` (không chạm Contracts).
- **CI-pending:** Infra/Api integration test (Testcontainers) cần Docker — không liên quan Phase 24 (chỉ chạm Domain/Application).

## Nợ / follow-up

- Hình thức hoá `combat_rules` vào JSON Schema stage (Phase 06/07) + validator; typed gameplay POCO đầy đủ = phase 27+.
- Năng lượng/ultimate/cooldown (§15) + target/aggro nâng cao (§14) chờ product chốt CB3/CB4.
- Tối ưu perf sim = phase 52; re-resolve target giữa action (đa-effect) khi có AoE.

Liên quan: [[0021-combat-spec-fixedpoint-standardized]] (spec), [[0019-config-service-standardized]] (`IConfigProvider`),
[[0007-domain-foundation-standardized]] (`Result`/`Guard`). ADR-011 (authority/determinism), ADR-004 (data-driven).

> Quyết định kiến trúc **luôn** đi vào `docs/adr/`, không chỉ ở đây.
