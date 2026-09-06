# 28 — Skill framework (effect-data + handler registry)

> Mục đích: Hiện thực **khung skill data-driven**: skill = effect-data + handler registry (ADR-004), tích hợp vào combat sim hai phía — mở rộng skill bằng data, không `switch/if`.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 6 Gameplay Vertical Slice | P2 | S7 | F03 |

# Mục tiêu

Khung skill: định nghĩa skill trong config (effect-data: loại effect, target, hệ số, điều kiện energy/cooldown), registry handler cho từng loại effect (damage/heal/buff/debuff…) ở **cả** sim server (24) và client (25), khớp golden vector.

# Lý do

ADR-004 cấm `switch/if` để mở rộng gameplay; skill phải là data + registry. Skill là cơ chế cốt lõi của combat auto; cần trước Battle (30) và để combat có chiều sâu.

# Phụ thuộc

- **Trước:** 24 (sim server), 25 (sim client), 27 (hero refs skill), 06 (skill schema).
- **Sau:** 30 (battle dùng skill), 44 (faction advantage), nội dung skill mở rộng.

# Phạm vi

- Schema skill (mở rộng phase 06): effect-data (type, target, magnitude từ config, energy/cooldown, điều kiện).
- Registry handler effect ở server & client (cùng quy tắc, khớp golden).
- Vài loại effect nền (damage, heal, buff/debuff, ultimate theo energy).
- Cập nhật golden vector (phase 26) phủ skill.

# Không thuộc phạm vi

- Toàn bộ nội dung skill của mọi hero (content, thêm dần qua config).
- Skill preview animation nâng cao (Could — phase UI sau).
- Số liệu balance skill (config/tuning).

# Deliverables

- Schema skill hoàn chỉnh + registry effect hai phía.
- Vài effect nền hoạt động trong sim, khớp golden.
- Golden vector mở rộng phủ skill (đồng bộ phase 26).
- Cập nhật [`../gameplay/skill-framework.md`](../gameplay/skill-framework.md).

# Công việc cần thực hiện

- [x] Hoàn thiện `skill.schema.json` (effect-data: type/target/magnitude/energy/cooldown/điều kiện). — `params` typed (coeff_fixed/amount_fixed/atk/def/spd/duration) + `cooldown`, additive (không bump `schema_version`); validator exit 0 (8 file).
- [x] Server: registry effect handler (`IEffectHandler` theo type) — thêm không `switch` mở rộng. — `EffectRegistry.CreateDefault` = damage/heal/apply_buff/apply_debuff; grep sạch (không dispatch switch).
- [x] Client: registry effect handler GDScript tương ứng (khớp quy tắc server). — `effect_registry.gd` 4 handler + `stat_modifier_core.gd`; song ánh server; golden khớp.
- [x] Hiện thực effect nền: damage, heal, buff/debuff, ultimate (energy-triggered). — Healed/BuffApplied/BuffExpired/EnergyChanged; §15 energy/ultimate config-gated (mặc định tắt).
- [x] Tích hợp registry vào sim (24/25): sim gọi handler theo effect-data. — `BattleSimulator` chọn basic/ultimate + per-effect target + energy/cooldown/buff-tick; 9 vector cũ byte-identical.
- [x] Mở rộng golden vector (26) phủ các skill → gate xanh hai phía. — `vector_10..14`; `run.sh check` exit 0 (14/14); client golden xanh; **server ≡ client ≡ baseline**. *(CI Actions pending)*
- [x] Test: skill từ config chạy đúng; thêm skill mới bằng config không sửa lõi. — `New_skill_via_config_only_runs_without_core_change` (server) + `test_new_skill_via_config_only_runs` (client); negative `+1` buff ⇒ đỏ hai phía ⇒ revert xanh.
- [x] Cập nhật `../gameplay/skill-framework.md`. — viết lại từ stub thành spec cụ thể + `combat-framework.md` §23.

# Tiêu chí hoàn thành

- Skill định nghĩa hoàn toàn bằng config (effect-data); không `switch/if` để mở rộng loại.
- Effect chạy đúng trong sim server & client, **khớp golden vector** (phase 26 mở rộng).
- Thêm 1 skill mới qua config (không sửa code lõi) → hoạt động.
- Test hai phía xanh; golden gate xanh.

# Cách kiểm tra

- `dotnet test` + gdUnit4: skill effect đúng; golden vector phủ skill khớp.
- Thêm skill config mới trong test → chạy không sửa lõi.
- Grep: không `switch/if` mở rộng loại skill (dùng registry).

# Rủi ro

- **Registry lệch giữa hai phía** → golden vector phủ skill bắt drift; cùng spec (23).
- **Effect phức tạp phá determinism** → mọi effect tuân fixed-point + thứ tự xác định.
- **Cám dỗ hardcode skill đặc biệt** → bắt buộc qua effect-data; review ADR-004.

# Ghi chú

Skill mới = thêm effect-data (config) + (nếu loại effect mới) thêm handler đăng ký registry. Bám [`../gameplay/skill-framework.md`](../gameplay/skill-framework.md) + ADR-004/011.

# Technical Debt Review

- **Maintainability:** skill là data + registry; mở rộng an toàn.
- **Scalability:** thêm hero/skill không phình lõi.
- **Testing:** golden phủ skill; test effect.
- **Security:** determinism giữ server-authority.
- **Nợ:** nội dung skill đầy đủ (content); animation preview (UI sau).

# Phase Review

**Trạng thái: ĐÓNG (local PASS 2026-09-05).** Skill = effect-data + handler registry hai phía (server `GameTeam.Domain/Combat/Effects/*`
+ client `client/src/combat/effects/*`), dispatch theo `effect_type` (KHÔNG `switch`). Effect nền: damage/heal (Healed)/apply_buff/
apply_debuff (modifier chỉ số, refresh-on-reapply) + ultimate theo năng lượng (§15, config-gated, mặc định TẮT ⇒ 9 vector Phase 26
byte-identical). Golden mở rộng `vector_10..14` (heal/buff/debuff/ultimate-energy/multi-effect), baseline sinh từ server ⇒ **server ≡
client ≡ baseline**. Thêm skill dùng effect có sẵn = chỉ config (test chứng minh hai phía). Doc: `skill-framework.md` (rewrite) +
`combat-framework.md` §23 + `shared/combat-vectors/README.md` + `.memory/0026`.

**Bằng chứng verify (local):** config-validator exit 0 (8 file); server `dotnet test` Domain 97 / Application 52 / Contracts 36 (gồm
registry/buff/debuff/heal-event/refresh + config-only-new-skill + purity/architecture); `tools/combat-baseline/run.sh check` exit 0
(14/14); Godot 4.7.1 `--import` exit 0; gdUnit4 full **114/114, 0 orphan** (golden auto-discover 14 + config-only resolver); negative
`+1` buff magnitude ⇒ drift vector_11/12/14 + golden đỏ hai phía ⇒ revert xanh; grep sạch (không switch dispatch, không float/wall-clock/
RNG global trong combat); không drift `openapi.json`/`client/src/data/generated`.

**Nợ (out-of-scope, ghi rõ):** `shield` handler + hệ điều kiện tổng quát (Post-MVP; `trigger` là cơ chế điều kiện hiện tại); số liệu
balance ultimate/energy (CB4 `[OPEN]`); battle endpoint + wiring `config/skills` thật theo `hero.skills[]` + nội dung skill đầy đủ = phase 30+.

**CI-verification pending:** gate `golden-vector` (`ci-server.yml` + `ci-client.yml`) chạy trên GitHub Actions — chờ kết quả Actions.

---

## Liên kết
- [`../gameplay/skill-framework.md`](../gameplay/skill-framework.md) · [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md)
- ADR: [`../adr/ADR-004-data-driven-design.md`](../adr/ADR-004-data-driven-design.md) · [`../adr/ADR-011-combat-authority-and-determinism.md`](../adr/ADR-011-combat-authority-and-determinism.md)
- Roadmap: [`README.md`](README.md) → kế: [`29-formation-team.md`](29-formation-team.md)
