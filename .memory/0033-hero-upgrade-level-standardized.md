# 0033 — Hero upgrade (Level) standardized (Phase 35)

- **Trạng thái:** Đã chốt & verify cục bộ (2026-09-14, Docker Desktop + Godot 4.7.1). Trục nâng cấp **Must** đầu tiên
  (F06): hero tăng **Level** bằng tiêu **Gold** ⇒ chỉ số combat + **Power Rating** tăng. **Server-authoritative +
  data-driven + tất định integer + atomic** (ADR-004/007/011). Xây bằng **composing** cơ chế sẵn có (currency 31, config
  21, save-root 19/27, battle 30/34) — KHÔNG fork transaction/sim/config.
- **Bối cảnh:** Phase 27 (OwnedHero), 31 (currency spend), 34 (nguồn gold), 21 (config). Combat KHÔNG dùng level trước
  Phase 35; Power Rating KHÔNG tồn tại (chỉ có trong docs) — phải xây mới.

## Quyết định (thiết kế, user-approved)

- **Gold-per-cấp, một cấp/lệnh** (user chọn thay vì EXP-item / batch). `economy.schema.json` đã có `cost_curves.level_up`
  ⇒ đúng ý đồ. KHÔNG thêm cột `Exp`, KHÔNG tiêu inventory item; **số cấp tối đa = 1 + độ dài `level_up`**.
- **Idempotency lần tiêu = khoá mã hoá cấp đích** `hero-levelup:{profileId}:{ownedHeroId}:{targetLevel}` + **khoá dòng
  hero** (`GetByProfileAndHeroForUpdateAsync` FOR UPDATE). Dưới khoá, cấp đích luôn nhất quán trạng thái dòng ⇒ không lên
  cấp "chùa"; **không cần requestId / bảng record mới** (khác summon/battle vốn cần record).
- **Power Rating tính-khi-đọc** (không lưu ⇒ không stale). Công thức = tổng có trọng số integer (`power_weights` config).
  Chưa nối power-gate campaign (mở khoá tuần tự — chưa tồn tại gate).
- **Level scaling ở tầng resolver (Application), KHÔNG ở sim (Domain).** Golden vectors (Phase 26) dùng chỉ số **tường
  minh** và **bypass resolver** ⇒ thêm scaling KHÔNG đụng sim/golden (14 vector byte-identical). Enemy = nền (cấp 1).

## Cái ĐÃ xây (reuse trước, đừng phát minh)

- **Config (additive, KHÔNG bump `schema_version`):** `economy.schema.json` +`level_stat_growth_bp` +`power_weights`;
  `config/economy/economy_default.json`; fixtures cập nhật. `EconomyConfig` POCO (`Features/Economy/`).
- **Domain:** `OwnedHero.LevelUp()` + event `OwnedHeroLeveledUp` (không thêm cột ⇒ **không migration**).
- **Công thức chung `HeroStatCalculator`** (`Features/Heroes/`): `ScaleStat` (round-half-up qua `FixedPoint`, cấp 1 ⇒ nền)
  + `Power` + `MaxLevel`/`TryGetLevelUpCost`. Mirror client `client/src/shared/hero_stats.gd` (bit-parity).
- **Application:** `LevelUpHeroCommand(heroId) : ITransactionalRequest` + handler (owner từ token → FOR UPDATE → kiểm trần
  cấp → `CurrencyWalletService.SpendAsync` → `LevelUp()` → chỉ số/Power) → `LevelUpHeroResponse`. Repo
  `IOwnedHeroRepository.GetByProfileAndHeroForUpdateAsync`. Errors: `OWNED_HERO_NOT_FOUND`(404),
  `HERO_MAX_LEVEL_CONFLICT`(409), `ECONOMY_CONFIG_NOT_FOUND`(404) — map qua suffix (không sửa `ErrorHttpMapping`).
- **Combat:** `CombatTeamMember.Level` + `TeamSnapshotFactory.Create(team, levelByHeroId)` + `BattleExecutionService` nạp
  cấp owned → `CombatInputResolver` nhân chỉ số ally. Mirror client `combat_input_resolver.gd` + `battle_presenter._replay`
  (ghép cấp từ `StateCache.get_heroes()`).
- **API:** `POST /api/v1/heroes/{heroId}/level-up` (protected). Contract `Contracts/Hero/LevelUpHeroResponse` → regenerate
  `openapi.json` → codegen (`RealSpecTests` +1, `OpenApiContractTests` +path+schema).
- **Client:** `hero_detail_{presenter,view}.gd` (chỉ số theo cấp + Power + cost + gold + nút Nâng cấp → POST → refresh
  `StateCache.apply_heroes`/`apply_wallet`); `StateCache.apply_heroes` (mới); `parse_level_up_hero_response`.

## Ràng buộc cho agent sau

- **MUST reuse** `HeroStatCalculator`/`EconomyConfig`/`LevelUpHeroCommand`/`OwnedHero.LevelUp`/`GetByProfileAndHeroForUpdateAsync`
  + client `hero_stats.gd`/`StateCache.apply_heroes` trước khi thêm plumbing nâng cấp. **MUST NOT** lưu chỉ số ở
  `owned_heroes`, hardcode đường cong/tăng-trưởng/power (config), đọc owner từ client (IDOR), để client tăng cấp/chỉ
  số/Power/trừ gold, tạo transaction/idempotency thứ 2, hay thêm endpoint cấp cấp/tiền công khai.
- **Data-driven binding:** đổi `economy` config ⇒ chi phí/chỉ số/Power đổi KHÔNG sửa code (test A/B chứng minh).
- **Combat binding:** scaling ở resolver; **không** sửa golden vectors để "vá"; luôn chạy golden gate hai phía sau khi
  đụng combat. Sao/Ascension (39), Equipment (38), Skill level (Post-MVP), EXP item / batch / power-gate = **ngoài scope**.
- **Doc-sync:** giữ đồng bộ `economy.schema.json`+`config/economy/*`+fixtures + `HeroStatCalculator`/`EconomyConfig` +
  `Features/Heroes/Commands/*` + `Combat/*`(resolver/snapshot/member) + `BattleExecutionService` + `Contracts/Hero/*` +
  regenerated `openapi.json`+`client/src/data/generated/**` + client `hero_stats.gd`/`hero_detail/*`/`response_parser.gd`/
  `state_cache.gd`/`combat_input_resolver.gd`/`battle_presenter.gd` + tests + docs (`hero-system.md` §9,
  `progression-and-economy.md` §1, `configuration-and-data.md`, `api-and-versioning.md`, `infrastructure.md` §1.3,
  `domain-and-application.md`, `state-and-signals.md` §1.1, `ui-architecture.md` §4.3) + CLAUDE.md §4.6 + `.instructions/*`.

## Verify (2026-09-14, local)

- Build Release 0/0; `dotnet test` — Domain **137** / Application **146** / Contracts **36** / Infrastructure **77** /
  Api.IntegrationTests **112** (`LevelUpHeroEndpointTests` 6: success atomic / thiếu gold 409 no-mutation / max cấp 409 /
  không sở hữu 404 / 401 / combat mạnh hơn sau nâng) / Codegen **41**.
- `CombatLevelScalingTests` + `HeroStatCalculatorTests` (Application); config-validator **26 file** OK; golden
  `tools/combat-baseline` **14 vector** khớp; `has-pending-model-changes` **sạch** (không migration); no openapi/generated
  drift ngoài additive.
- Godot import exit 0 + gdUnit4 **167/167, 0 orphan** (hero_detail +3, `hero_stats_test` 5, state_cache +1,
  combat_input_resolver +2).
