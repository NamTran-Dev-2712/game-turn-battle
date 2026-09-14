# 35 — Hero upgrade (Level/EXP)

> Mục đích: Hiện thực **nâng cấp hero bằng Level/EXP** (trục nâng cấp Must đầu tiên) — tiêu tài nguyên để tăng sức mạnh, giúp vượt "wall" campaign.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 8 Đóng vòng Core Loop | P3 | S9 | F06 |

# Mục tiêu

Hero có level (EXP), nâng cấp tiêu tài nguyên (gold/EXP item) qua command atomic; chỉ số hero tính theo level + config (data-driven); Power Rating cập nhật; server-authoritative.

# Lý do

Nâng cấp Level là trục Must (F06) — mắt xích "thưởng → nâng cấp → đẩy xa" của loop. Cần sau campaign (34, nguồn tài nguyên) và trước AFK (37) để có sink cho AFK reward.

# Phụ thuộc

- **Trước:** 27 (hero), 31 (currency spend), 34 (nguồn tài nguyên), 21 (config đường cong level).
- **Sau:** 37 (AFK cấp tài nguyên nâng cấp), 39 (ascension trục kế).

# Phạm vi

- Level/EXP trên OwnedHero; đường cong cost/stat theo config (EC4) — số liệu ở config.
- Command `LevelUpHero` (tiêu gold/EXP item atomic) → tăng level → chỉ số tính lại.
- Chỉ số hero = base(config) × hàm(level) — deterministic, integer.
- Cập nhật Power Rating (dùng gate độ khó campaign).

# Không thuộc phạm vi

- Star/Ascension (phase 39).
- Equipment (phase 38).
- Skill level (Could/Post-MVP).

# Deliverables

- Level/EXP + command nâng cấp atomic.
- Chỉ số theo level (data-driven) phản ánh vào combat sim.
- Integration test: nâng cấp tiêu tài nguyên atomic; chỉ số tăng đúng công thức; thiếu tài nguyên chặn.
- Cập nhật [`../gameplay/hero-system.md`](../gameplay/hero-system.md) + [`../mvp/05-player-progression.md`](../mvp/05-player-progression.md).

# Công việc cần thực hiện

- [x] Schema: đường cong level (cost gold, stat theo level) trong config — không nhúng số vào code. ✅ `economy.schema.json` mở rộng **additive** (`level_stat_growth_bp`, `power_weights`; `cost_curves.level_up` = chi phí gold/cấp); dữ liệu ở `config/economy/economy_default.json`; fixtures cập nhật; config-validator 26 file OK. (MVP dùng **gold-per-cấp**; không dùng EXP item riêng — đường cong `cost_curves.level_up` đúng ý đồ schema.)
- [x] Domain: level trên OwnedHero; hàm tính stat theo level (integer, deterministic). ✅ `OwnedHero.LevelUp()` + event `OwnedHeroLeveledUp`; `HeroStatCalculator.ScaleStat` (round-half-up qua `FixedPoint`, cấp 1 ⇒ nền) + `Power` (tổng có trọng số). Domain.Tests 137 xanh.
- [x] Application: `LevelUpHeroCommand` (spend gold atomic 31) → tăng cấp → cập nhật stat/Power. ✅ handler `ITransactionalRequest`: owner từ token (chống IDOR) → `GetByProfileAndHeroForUpdateAsync` (FOR UPDATE) → kiểm trần cấp → `CurrencyWalletService.SpendAsync` (khoá `hero-levelup:{profile}:{hero}:{targetLevel}`) → `LevelUp()` → trả `LevelUpHeroResponse{level,stats,power,goldSpent,goldBalanceAfter}`. Endpoint `POST /api/v1/heroes/{heroId}/level-up`. Application.Tests 146 xanh.
- [x] Đảm bảo sim (24/25) dùng chỉ số theo level (snapshot khi battle). ✅ `CombatTeamMember.Level` + `TeamSnapshotFactory` nhận map cấp; `BattleExecutionService` nạp cấp owned → `CombatInputResolver` nhân chỉ số ally theo cấp (địch = nền); mirror client `combat_input_resolver.gd` + `hero_stats.gd`. Golden 14 vector **byte-identical** (scaling ở resolver, không đụng sim/golden). Integration `LevelUp_makes_the_hero_hit_harder_in_combat` xanh.
- [x] Client: UI nâng cấp hero (hiển thị cost, kết quả). ✅ `hero_detail_presenter.gd`/`hero_detail_view.gd`: chỉ số theo cấp + Power + chi phí + gold + nút "Nâng cấp" (intent → POST) → refresh hero + ví AUTHORITATIVE (`StateCache.apply_heroes`/`apply_wallet`); parser `parse_level_up_hero_response`. gdUnit4 167/167 xanh, 0 orphan.
- [x] Integration test: nâng cấp atomic; stat tăng đúng công thức config; thiếu tài nguyên chặn; đổi config đường cong→đổi. ✅ Api.IntegrationTests `LevelUpHeroEndpointTests` (Testcontainers pg16): success (gold−cost, cấp+1, chỉ số/Power, persist), thiếu gold ⇒ 409 không mutate, max cấp ⇒ 409, không sở hữu ⇒ 404, 401, combat mạnh hơn sau nâng; `CombatLevelScalingTests` (đổi growth config ⇒ chỉ số đổi, không sửa code); `HeroStatCalculatorTests`.
- [x] Cập nhật `../gameplay/hero-system.md`. ✅ §10 (Phase 35).

# Tiêu chí hoàn thành

- [x] Nâng cấp tiêu tài nguyên atomic; thiếu → chặn. ✅ spend gold trong 1 transaction (TransactionBehavior + `CurrencyWalletService` + FOR UPDATE); thiếu gold ⇒ 409, không trừ tiền/không tăng cấp (test `LevelUp_insufficient_gold_returns_409_and_mutates_nothing`).
- [x] Chỉ số hero tăng theo công thức config (đổi config → đổi, không sửa code). ✅ `HeroStatCalculator` đọc `economy` config; test đổi `level_stat_growth_bp` A/B ⇒ chỉ số đổi không sửa code.
- [x] Level ảnh hưởng combat (team snapshot dùng stat mới). ✅ `BattleExecutionService` nạp cấp owned → resolver nhân chỉ số ally; test `LevelUp_makes_the_hero_hit_harder_in_combat`.
- [x] Server-authoritative; client chỉ hiển thị/gửi intent. ✅ cấp/chi phí/chỉ số/Power do server tính; client POST intent rỗng rồi refresh state AUTHORITATIVE; không có mutator client.

# Cách kiểm tra

- `dotnet test`: level-up atomic, stat đúng, thiếu tài nguyên chặn, data-driven.
- Local: nâng cấp hero → chỉ số tăng → trận mạnh hơn.
- Đổi đường cong config → hành vi đổi (test).

# Rủi ro

- **Client tự tăng level/stat** → server-authoritative + spend atomic.
- **Công thức stat dùng float** → integer/fixed-point (nhất quán combat).
- **Gold sink lệch (lạm phát/thiếu)** → số ở config, tune (EC4).

# Ghi chú

Level là 1 trong 3 trục nâng cấp MVP (A06: Level + Star + Gear). Số liệu đường cong là tuning (config). Bám [`../gameplay/hero-system.md`](../gameplay/hero-system.md) + [`../mvp/05-player-progression.md`](../mvp/05-player-progression.md).

# Technical Debt Review

- **Maintainability:** đường cong là data; tune không sửa code.
- **Scalability:** thêm trục (star/gear) tái dùng pattern.
- **Testing:** atomic + công thức có test.
- **Security:** nâng cấp server-authoritative.
- **Nợ:** star/gear (39/38); balance số (tuning).

# Phase Review

**ĐÓNG (2026-09-14).** Level-up atomic (gold-per-cấp, `CurrencyWalletService` + FOR UPDATE + `TransactionBehavior`) +
stat/Power data-driven từ `economy` config (`HeroStatCalculator`, integer/round-half-up, cấp 1 ⇒ nền) + ảnh hưởng combat
(cấp owned → `BattleExecutionService`/`TeamSnapshotFactory` → `CombatInputResolver` nhân chỉ số ally; mirror client
`hero_stats.gd`) + server-authoritative (client chỉ POST intent rồi refresh state). **Không cần migration** (dùng cột
`level` sẵn có; `has-pending-model-changes` sạch). Golden 14 vector **byte-identical** (scaling ở tầng resolver, không
đụng sim). Quyết định: **gold-per-cấp, một cấp/lệnh, không EXP item riêng**; idempotency lần tiêu = khoá mã hoá cấp đích +
khoá dòng hero (không cần requestId/bảng record mới); Power Rating **tính-khi-đọc** (không lưu → không stale), chưa nối
power-gate campaign (chưa tồn tại — mở khoá tuần tự).

Verify (2026-09-14, Docker Desktop + Godot 4.7.1): build Release 0/0; `dotnet test` — Domain **137** / Application **146** /
Contracts **36** / Infrastructure **77** / Api.IntegrationTests **112** (gồm `LevelUpHeroEndpointTests` 6) / Codegen **41**;
config-validator **26 file** OK; golden `tools/combat-baseline` **14 vector** khớp; `has-pending-model-changes` sạch; no
openapi/generated drift ngoài additive `LevelUpHeroResponse` + path level-up; Godot import exit 0 + gdUnit4 **167/167, 0 orphan**.

**Nợ / ngoài phạm vi:** EXP item (dùng gold), batch level-up, power-gate campaign, bảng idempotency riêng — không làm.
Sao/Ascension (39), Equipment (38), Skill level (Post-MVP) giữ nguyên.

---

## Liên kết
- [`../gameplay/hero-system.md`](../gameplay/hero-system.md) · [`../mvp/05-player-progression.md`](../mvp/05-player-progression.md) · [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md)
- ADR: [`../adr/ADR-004-data-driven-design.md`](../adr/ADR-004-data-driven-design.md) · [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md)
- Roadmap: [`README.md`](README.md) → kế: [`36-energy-system.md`](36-energy-system.md)
