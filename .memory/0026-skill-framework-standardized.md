# 0026 — Skill Framework (effect-data + handler registry) standardized (Phase 28)

- **Trạng thái:** Đã chốt & verify cục bộ (2026-09-05). Khung skill data-driven (ADR-004) hiện thực trên
  **cả hai** sim (server .NET §21, client GDScript §21.6) — skill = effect-data; dispatch qua **registry**
  theo `effect_type` (KHÔNG `switch`). Không đổi spec §9–§20; **promote** cơ chế §15/§23. Golden mở rộng
  `vector_10..14` ⇒ **server ≡ client ≡ baseline** (gate Phase 26).
- **Bối cảnh:** Trước Phase 28, registry đã có (Phase 24/25) nhưng chỉ `damage` chạy đủ; `heal` đăng ký mà
  không phát event; buff/debuff/energy/ultimate/cooldown **chưa có / no-op**. Sim dùng 1 `BasicSkill` chung,
  `TargetRule` bị bỏ qua, `UnitState.Atk/Def/Spd` bất biến. Phase 28 lấp đầy các seam đó.

## Quyết định (user-approved)

- **Energy/Ultimate (CB4):** hiện thực cơ chế §15 hai phía, **config-gated, mặc định TẮT** (gain=0 ⇒ 9 vector
  Phase 26 byte-identical); `vector_13` bật bằng test config. **Số liệu balance production vẫn `[OPEN]`** (CB4).
- **Buff/debuff:** modifier chỉ số phẳng (atk/def/spd) + `duration` vòng; giảm tại `RoundStarted`, hết ở 0.
  Khoá `(source_skill_id, stat)` ⇒ áp lại **refresh** (thay amount+duration, **không chồng**). Event `BuffApplied`/`BuffExpired`.
- **Shield + điều kiện tổng quát: HOÃN (nợ).** Giữ `shield` trong enum schema (chưa handler); `trigger`
  (energy/cooldown) là cơ chế điều kiện hiện tại — không dựng hệ biểu thức tổng quát.

## Thành phần

- **Schema** `shared/config-schema/skill.schema.json`: `params` typed tuỳ chọn (`coeff_fixed`/`amount_fixed`/
  `atk`/`def`/`spd`/`duration`) + skill-level `cooldown`. **Additive** ⇒ KHÔNG bump `schema_version`. Seed
  `config/skills/*` cập nhật params + thêm `skill_aqua_guard` (buff) + `skill_ignis_ultimate` (energy) ref từ hero.
- **Server Domain** `GameTeam.Domain/Combat/`: `Effects/{ApplyBuffEffectHandler,ApplyDebuffEffectHandler,StatModifierCore}`
  (buff/debuff dùng chung core, khác dấu), `HealEffectHandler` phát `Healed`, `DamageEffectHandler` nạp on_hit;
  `EffectRegistry.CreateDefault` = 4 handler. `Model/{SkillDef(+EnergyCost/CooldownRounds),EffectDef(+Target),UnitSkillSet,StatKind}`;
  `State/{UnitState(modifier/energy/cooldown + effective atk/def/spd),StatModifier}`; `Events/{Healed,BuffApplied,BuffExpired}`.
  `BattleSimulator`: chọn basic/ultimate (§15), per-effect target resolve (single_enemy/single_ally/self),
  attack↔non-attack roll, energy accrual/spend + `EnergyChanged`, buff-tick + cooldown-tick tại `RoundStarted`.
- **Server Application** `GameTeam.Application/Combat/`: `SkillCombatConfig` (effects cấu trúc {effect_type,target?,params}
  + energy_cost/cooldown_rounds) + `SkillEffectConfig`; `HeroCombatConfig` (+`BasicSkillId`/`UltimateSkillId`);
  `CombatInputResolver` map params→`EffectDef.Params` + dựng per-unit `UnitSkillSet`.
- **Client** `client/src/combat/`: song ánh bit-for-bit (`effects/*` 4 handler + `stat_modifier_core.gd`,
  `model/{unit_skill_set,skill_def(+energy_cost/cooldown_rounds),effect_def(+target/has_param)}`, `state/combat_unit_state.gd`,
  `events/combat_events.gd`, `battle_simulator.gd`, `combat_input_resolver.gd`).
- **Golden**: `shared/combat-vectors/vector_10..14` + định dạng skill riêng của unit (`config_excerpt.skills` +
  `team_snapshot[].skills`) trong **3 loader** (`GoldenVectorLoader.cs`, `VectorInputParser.cs`, `combat_vector_loader.gd`) — sửa đồng bộ.

## Chống tự-vẽ (binding)

- Dispatch **chỉ qua registry** — KHÔNG `switch(effect_type/skill)`. So sánh `effect_type == damage` duy nhất =
  phân loại attack↔non-attack (business logic quyết roll RNG), KHÔNG phải dispatch handler.
- Thêm skill dùng effect có sẵn = **chỉ config** (test hai phía chứng minh). Loại effect mới = 1 handler/phía +
  đăng ký `create_default()`/`CreateDefault()` + config + test (+ vector nếu vào golden).
- Xác định (ADR-011): integer/fixed-point (không `float`), RNG 1 stream/trận, thứ tự effect/target/lượt tất định.
  Baseline sinh từ **server**; client replay khớp — KHÔNG sửa vector cho CI xanh (regenerate có chủ đích + WHY).
- 9 vector Phase 26 **bất biến**: mọi tính năng mới gate trên data mới (per-unit skills / gain≠0 / có modifier).

## Verify (local)

- config-validator `run.sh` exit 0 (8 file: 3 hero + 5 skill, hero→skill integrity).
- `dotnet build -c Release` 0 error; `dotnet test` Domain **97** / Application **52** / Contracts **36** (gồm
  registry 4-handler, buff/debuff, heal-event, refresh-not-stack, config-only-new-skill, purity/architecture).
- `tools/combat-baseline/run.sh check` exit 0 (**14/14**); tool xUnit 4.
- Godot 4.7.1 `--headless --import` exit 0; gdUnit4 full **114/114, 0 orphan** (golden auto-discover 14 +
  `test_new_skill_via_config_only_runs`).
- **Negative (hai phía):** `+1` vào buff magnitude (`StatModifierCore`) ⇒ drift `vector_11/12/14` + golden đỏ; revert ⇒ xanh.
- grep sạch (không dispatch switch; không `float`/wall-clock/RNG global trong combat); không drift `openapi.json`/`client/src/data/generated`.
- **CI-pending:** gate `golden-vector` (`ci-server.yml` + `ci-client.yml`) + `validate-config.yml` trên Actions.

## Liên kết

- CLAUDE.md §4.6 (Skill framework block) · [[0021-combat-spec-fixedpoint-standardized]] (spec §9–§20) ·
  [[0022-combat-sim-server-standardized]] (sim server + registry) · [[0023-combat-sim-client-standardized]] (sim client) ·
  [[0024-combat-golden-vectors-standardized]] (golden + baseline tool) · [[0025-hero-system-standardized]] (hero refs skill).
- Docs: `docs/gameplay/skill-framework.md` (rewrite), `docs/gameplay/combat-framework.md` §23,
  `docs/mvp/10-open-questions.md` (CB4), `shared/combat-vectors/README.md`, `tools/combat-baseline/README.md`.
