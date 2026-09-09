# 0028 — Battle flow end-to-end standardized (Phase 30)

- **Trạng thái:** Đã chốt & verify cục bộ (2026-09-08, Docker Desktop 28.5.1 + Godot 4.7.1). **Cột mốc P2 — lát cắt dọc
  chơi được đầu tiên.** Nối trọn luồng trận (ADR-011/007 §3): client gửi ý định → server sinh seed + **re-sim** quyết
  outcome + cấp thưởng (transaction, idempotent) → trả `BattleResult{seed}` → client **replay** bằng seed để hiển thị.
- **Bối cảnh:** sim server (24) + client (25) + team snapshot (29) + config (21) + transaction (11) đã có. Phase 30 là
  tầng **orchestration + persistence + endpoint** quanh sim đã đóng — KHÔNG đổi spec combat §9–§23.

## Quyết định (user-approved)

- **Full real-config wiring:** reconcile combat-config readers ↔ gameplay schema (hero `base_stats`+`skills[]`; skill
  `target`/`trigger`/`effects[].params.coeff_fixed`; stage `combat_rules`/`max_rounds`/`basic_skill_id`+enemy `slot`) —
  sim đọc **config thật**, chạy trong app thật (không chỉ FakeConfigProvider). `stage.schema.json` mở rộng **additive**
  (không bump version). Trước Phase 30 combat readers chỉ chạy với stub; đây là "wiring `config/skills` thật" mà Phase 24 hoãn.
- **Minimal wallet credit:** ví per-profile mới (`Wallet`, credit-only) credit trong transaction đánh trận — "thưởng vào
  profile" thật + chống double-credit. Currency/inventory đầy đủ = Phase 31–33.
- **Idempotency key in request body:** `StartBattleRequest.attemptId` do client sinh; unique `(profile_id, attempt_id)`;
  retry cùng khoá → trả kết quả đã lưu, KHÔNG cấp thưởng lần hai.

## Thành phần

- **Contracts** `GameTeam.Contracts/Battle/`: `StartBattleRequest{teamId,stageId,attemptId}`, `BattleResultDto{seed
  (Int64 không âm),outcome,rounds,rewards[],log(JSON chuỗi)}`, `RewardDto{rewardType,refId,amount}`. `TeamDto` **+`Id`**
  (additive) để client gửi teamId. Codegen sinh 3 DTO GDScript; `RealSpecTests` file-list cập nhật (hợp đồng codegen).
- **Domain** `GameTeam.Domain/Battles/`: `BattleRecord : AggregateRoot<Guid>` (idempotency + kết quả; `BattleReward`
  value; event `BattleResolved`). `GameTeam.Domain/Economy/`: `Wallet : AggregateRoot<Guid>` (1-1 profile, `WalletBalance`,
  `Credit` credit-only).
- **Application** `GameTeam.Application/Features/Battles/`: `StartBattleCommand`(`ITransactionalRequest`)+`Handler` (owner
  từ token→profile→idempotency check→snapshot đội (29 `TeamSnapshotFactory`, validate teamId)→seed (`IBattleSeedSource`)→
  `CombatInputResolver`→`BattleSimulator`→thưởng chỉ VICTORY chỉ `currency`→credit ví→ghi record, atomic), `RewardConfig`,
  `BattleErrors` (`UNAUTHENTICATED`/`PROFILE_NOT_FOUND`/`BATTLE_TEAM_NOT_FOUND`/`BATTLE_TEAM_EMPTY`), `BattleMapping`.
  Ports: `IBattleRecordRepository`/`IWalletRepository` (Persistence), `IBattleSeedSource` (Abstractions/Combat). DI:
  `BattleSimulator` singleton + `CombatInputResolver` scoped (AddApplication).
- **Reconcile combat readers** (Application/Combat): `HeroCombatConfig`(base_stats+skills[]), `SkillCombatConfig`(target/
  trigger/cooldown/effects), `StageEnemyConfig`(hero_id+slot?), `StageCombatConfig`(+rewards), `CombatInputResolver` map
  skills[]→basic(energy==0)/ultimate(energy>0) + nâng coeff effect damage lên `SkillDef.CoeffFixed` + suy enemy actor
  `enemy_{i}`. **Client mirror** `client/src/combat/combat_input_resolver.gd` giống hệt (replay bit-for-bit).
- **Infrastructure**: `BattleRecordConfiguration` (`battle_records`, rewards JSON `OwnsMany().ToJson()`, unique
  `(profile_id,attempt_id)`, FK cascade), `WalletConfiguration` (`wallets`, balances JSON, unique `profile_id`),
  `BattleRecordRepository`/`WalletRepository`, `CryptoBattleSeedSource` (RNG mật mã, Int64 không âm), DI, `DbSet`s,
  migration `AddBattlesAndWallets`.
- **API**: `POST /api/v1/battles` (version set, protected) → `StartBattleCommand`.
- **Client** `client/src/ui/battle/`: `BattleView` (BaseView network-free) + `BattlePresenter` (GET /team → POST /battles →
  replay bằng seed qua `CombatInputResolver`+`BattleSimulator` client + `ConfigProvider`; outcome/rewards theo server).
  `NetworkResponseParser.parse_battle_result` + `parse_team` đọc `id`. Hub: nút "Chiến đấu" → `BATTLE_PATH` với
  `{stage_id:"stage_demo_01"}`. KHÔNG thêm event EventBus.
- **Config seed**: `config/stages/stage_demo_01.json` + `config/rewards/reward_stage_demo.json` (gold 100) + 4 hero
  (`hero_terra/gale/umbra/lux`) đủ đội 6.

## Chống tự-vẽ (binding)

- **Server là authority (ADR-011/007):** seed + outcome + thưởng do **server** quyết/cấp; client replay **để hiển thị**,
  KHÔNG tự quyết outcome / tự cấp thưởng / bịa khi lệch (hiện theo server + `push_warning`). Client KHÔNG chọn seed.
- **Idempotency bắt buộc:** unique `(profile_id, attempt_id)` là bảo đảm DB (không check-then-insert đơn thuần). Ghi kết
  quả + credit ví **atomic** (một transaction) — không partial state.
- **Data-driven:** stats/rules/rewards từ config (không hardcode balance). Reconcile combat↔gameplay config, KHÔNG nguồn
  thứ hai; combat readers giờ đọc gameplay schema thật (KHÔNG quay lại flat shape).
- **Reuse:** `BattleSimulator`/`CombatInputResolver`/`TeamSnapshotFactory`/`CombatEventSerializer`/`IUnitOfWork`/codegen —
  KHÔNG fork sim/resolver thứ hai, KHÔNG bypass MediatR/transaction, KHÔNG hand-edit generated/openapi.
- Ngoài scope (nợ): currency/inventory đầy đủ + ví UI (31–33); loại thưởng hero/fragment/item; nhiều stage (34); sweep (43);
  refresh token; race double-submit đồng thời chỉ chặn bằng unique index (nền, đủ 31/37).

## Verify (2026-09-08)

- `dotnet build -c Release` 0 warning/0 error; `dotnet test` — Domain **107** / Application **76** (`StartBattleCommandHandlerTests`
  8: re-sim/reward/idempotency/IDOR/defeat/seed-replay) / Contracts **36** / Infrastructure **52** (Testcontainers pg16
  `BattlePersistenceTests` 4: JSON round-trip, unique index, credit, dispatch `BattleResolved`) / Api **73** (Testcontainers
  `BattleEndpointTests` 6: re-sim, seed→log parity, tx rollback, idempotent no-double-grant, auth; + `OpenApiContractTests`
  `/battles`) / Codegen **41** / Config-validator **48** / Combat-baseline **4**.
- Godot 4.7.1 `--headless --import` exit 0; gdUnit4 **127/127, 0 orphan** (`battle_presenter_test` 6 + `combat_input_resolver_test`
  cập nhật gameplay shape). Golden gate `run.sh check` **14 vector khớp** (client sim không đổi, resolver bypass vector).
- config-validator `run.sh` exit 0 (**15 file**, +stage/reward/4 hero demo, referential integrity); `has-pending-model-changes`
  sạch; không drift `openapi.json`/generated ngoài additive battle.
- **CI-pending:** kết quả GitHub Actions (`ci-server`/`ci-client`/`validate-config`/`codegen-check`/`golden-vector`) — đã xanh
  đầy đủ ở local (gồm Testcontainers + Godot headless), chờ Actions.

## Liên kết

- CLAUDE.md §4.6 (Battle flow block) · [[0027-team-formation-standardized]] (team snapshot) · [[0022-combat-sim-server-standardized]]
  (BattleSimulator/CombatInputResolver) · [[0023-combat-sim-client-standardized]] (client replay) ·
  [[0024-combat-golden-vectors-standardized]] (golden gate) · [[0009-persistence-standardized]] (transaction/UoW) ·
  [[0017-profile-persistence-standardized]] (save root) · [[0006-codegen-pipeline-standardized]] (codegen).
- Docs: `docs/gameplay/combat-framework.md` §24, `docs/architecture/overview.md` §8, `docs/backend/domain-and-application.md`
  (§3 Start Battle), `docs/backend/infrastructure.md` §1.4, `docs/backend/api-and-versioning.md`,
  `docs/gameplay/configuration-and-data.md`, `docs/godot/ui-architecture.md` §4.2, `docs/roadmap/30-battle-flow-e2e.md`.
