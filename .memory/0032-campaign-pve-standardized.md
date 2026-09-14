# 0032 — Campaign PvE standardized (Phase 34)

- **Trạng thái:** Đã chốt & verify cục bộ (2026-09-13, Docker Desktop + Godot 4.7.1). Campaign là **trục tiến độ
  chính** (F05) + **nguồn AFK-stage** cho phase 37 — **config-driven + server-authoritative + atomic** (ADR-004/007/011).
  Xây bằng **composing** cơ chế sẵn có (battle 30, economy 31, config 21, save-root 19) — KHÔNG fork sim/reward/transaction.
- **Bối cảnh:** Phase 30 (battle flow) + 31–33 (economy/inventory/gacha) + 21 (stage config). Roadmap 34 yêu cầu reuse
  đúng battle flow + reward infra; tiến độ + thưởng phải server-authoritative + atomic + chống skip.

## Quyết định (thiết kế, user-approved)

- **Reward = first-clear only** (user chọn). Thưởng + tiến độ chỉ áp dụng lần clear ĐẦU của một stage; clear lại ⇒ trận
  vẫn chạy nhưng không cấp lại/không lùi tiến độ (repeatable farm là việc AFK phase 37). Chống exploit kinh tế; khớp
  mvp/02 §8 "first-clear bonus".
- **Chuỗi campaign = loại config mới `chapter`** (user chọn thay vì chỉ dùng `requirements.prerequisite_stage_id`).
  Hợp đồng Phase 06/07 add-a-type: `chapter.schema.json` (`order`+`stages[]`) + `common.schema.json#chapter_id` +
  `ConfigType.Chapter` + `ConfigFileMapper["chapters"]` + `ReferenceValidator.Chapter` (`stages[]→stage`) + fixtures.
  Chuỗi = chapter theo `order` (tie-break id) rồi `stages` theo thứ tự. Ordering là **nguồn duy nhất** cho mở-khoá +
  AFK-stage (không dùng song song `prerequisite_stage_id` để tránh hai nguồn sự thật).
- **Tách cơ chế battle dùng chung (user chọn "extract shared service").** `BattleExecutionService` (idempotency
  `attemptId` + snapshot đội 29 + seed server + re-sim 24 + ghi `BattleRecord`; thưởng do **caller** cấp qua callback
  ⇒ campaign cấp first-clear trên khoá riêng) + `StageRewardService` (cấp thưởng stage config qua
  `CurrencyWalletService`). `StartBattleCommandHandler` (30) refactor để ủy thác — hành vi KHÔNG đổi (test Phase 30
  xanh; test unit dựng handler từ cùng mock). MỘT sim/reward path cho cả battle thường lẫn campaign.
- **Anti-skip TRƯỚC mọi mutation.** `CampaignChain.IsUnlocked` (stage đầu, hoặc stage liền trước đã clear); khoá ⇒
  `CAMPAIGN_STAGE_LOCKED` (403 — thêm explicit vào `ErrorHttpMapping.KnownCodes`) trả về trước khi sim/thưởng/tiến độ.
- **Khoá idempotency thưởng campaign scope theo PROFILE** (`campaign:{profileId}:{stageId}`). Ledger idempotency_key là
  unique TOÀN CỤC ⇒ khoá chỉ theo stage sẽ va chạm giữa các profile (bug đã bắt được ở integration test: profile khác
  đã ghi key → grant của profile này bị dedup → ví 0). Per-profile key sửa triệt để; `progress.IsStageCleared` là lớp
  bảo vệ first-clear thứ hai (cùng profile).
- **CurrentAfkStageId = `CampaignChain.NextAfkStageId`** (stage clear xa nhất theo thứ tự chuỗi) — Domain lưu, Application
  tính (thứ tự phụ thuộc config ⇒ Domain không tự suy, cùng pattern Team). Phase 34 CHỈ persist/phơi giá trị này;
  **KHÔNG** AFK accrual/timer/claim (phase 37).
- **Reuse `BattleResultDto`** cho response campaign (không DTO battle mới); client refresh tiến độ qua
  `GET /campaign/progress`. Client đánh stage **reuse màn battle** (route context `campaign_stage_id` ⇒ BattlePresenter
  POST `/campaign/battles`); back → scene re-instantiate ⇒ tự fetch lại tiến độ. **KHÔNG** thêm EventBus event (reuse
  `state_refreshed`/`config_updated`).

## Thành phần

- **Config/schema:** `shared/config-schema/{chapter.schema.json, common.schema.json(#chapter_id), fixtures/chapter.*.json}`
  + `config/chapters/chapter_01.json` + `config/stages/stage_ch01_0{1,2,3}.json` + `config/rewards/reward_ch01_0{1,2,3}.json`.
  Validator: `tools/config-validator/.../{ConfigType,ConfigFileMapper,ReferenceValidator}.cs` (+ `ConfigFileMapperTests`,
  `ReferenceValidatorTests`).
- **Domain:** `GameTeam.Domain/Campaign/{CampaignProgress, ClearedStage, CampaignStageCleared, CampaignProgressCreated}.cs`.
- **Application:** `GameTeam.Application/Features/Campaign/{ChapterConfig, CampaignChain, CampaignMapping, CampaignErrors,
  StartCampaignBattleCommand(+Handler), GetCampaignProgressQuery(+Handler)}.cs` + `Abstractions/Persistence/ICampaignProgressRepository.cs`
  + `Features/Battles/{BattleExecutionService, StageRewardService}.cs` (+ `StartBattleCommandHandler` refactor) + DI.
- **Infrastructure:** `Persistence/Configurations/CampaignProgressConfiguration.cs` + `Repositories/CampaignProgressRepository.cs`
  + `AppDbContext.CampaignProgresses` + migration `AddCampaignProgress` (tables `campaign_progress` unique `profile_id` +
  `campaign_cleared_stages`) + DI.
- **Contracts/codegen:** `GameTeam.Contracts/Campaign/{StartCampaignBattleRequest, CampaignStageDto, CampaignProgressDto}.cs`
  → `openapi.json` → `client/src/data/generated/{start_campaign_battle_request,campaign_stage_dto,campaign_progress_dto}.gd`
  (+ `RealSpecTests`).
- **API:** `Program.cs` `POST /api/v1/campaign/battles` + `GET /api/v1/campaign/progress` (version set, protected);
  `ErrorHttpMapping` `CAMPAIGN_STAGE_LOCKED→403`.
- **Client:** `client/src/ui/campaign/{campaign.tscn, campaign_view.gd, campaign_presenter.gd}` + `battle_presenter.gd`
  (campaign context) + `response_parser.gd` (`parse_campaign_progress`) + `state_cache.gd` (slice `campaign`) + hub wiring
  (`main_hub_{view,presenter}.gd`).
- **Tests:** Domain `CampaignProgressTests`; Application `CampaignChainTests` + `StartCampaignBattleCommandHandlerTests`;
  Infrastructure `CampaignProgressPersistenceTests` (Testcontainers); Api `CampaignEndpointTests` (Testcontainers, 5 kịch
  bản); client `campaign_presenter_test.gd`.

## Verify (2026-09-13)

- Build Release 0 error. `dotnet test`: Domain 135 / Application 132 / Contracts 36 / Infrastructure 77 / Api 104.
- codegen 41 (+3 file, `git diff` generated sạch); config-validator 56 (real config 25 file, exit 0).
- gdUnit4 156 (+6), 0 orphan; Godot import exit 0. Migration `has-pending-model-changes` sạch.
- Kịch bản Api (Testcontainers pg16): clear→unlock+reward(gold)+AFK stage; locked→403 no-mutation; anti-skip future
  stage→403; loss→no advance/reward; retry cùng attemptId + replay stage đã clear (attempt mới)→ không thưởng/tiến độ
  lần hai.

## Nợ / ngoài phạm vi (đúng)

- AFK accrual/claim (phase 37 — chỉ consume `CurrentAfkStageId`); energy gate (36); hero upgrade/level (35); tower/mode
  khác (Post-MVP). Concurrency: campaign battle đồng thời cùng (profile,stage) dựa unique index chống hỏng dữ liệu
  (không row-lock riêng cho progress như ví) — chấp nhận cho MVP.
