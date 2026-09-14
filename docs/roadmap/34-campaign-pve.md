# 34 — Campaign PvE (stage chain, progression)

> Mục đích: Hiện thực **chiến dịch PvE** (chuỗi stage), nguồn tài nguyên chính và trục tiến độ; quyết định "AFK stage" hiện hành — server-authoritative.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 8 Đóng vòng Core Loop | P3 | S9 | F05 |

# Mục tiêu

Chuỗi stage campaign (config-driven) với tiến độ người chơi (stage cao nhất đã qua); đánh stage dùng battle flow (30); thắng → mở stage kế + thưởng; tiến độ đặt "AFK stage" cho phase 37.

# Lý do

Campaign là trục tiến độ chính (F05) và nguồn thưởng/AFK. Cần sau battle (30) + collection (31–33) để có "đích để đẩy". Là tiền đề cho AFK (37) đóng vòng.

# Phụ thuộc

- **Trước:** 30 (battle), 31–33 (currency/inventory/gacha), 21 (stage config).
- **Sau:** 36 (energy gate), 37 (AFK theo stage), 35 (upgrade để vượt wall).

# Phạm vi

- Stage/chapter định nghĩa config (địch, thưởng, điều kiện mở khoá) — số liệu ở config.
- Tiến độ người chơi server-authoritative (stage cao nhất clear); mở khoá tuần tự.
- Đánh stage qua battle flow (30); thắng → cập nhật tiến độ + thưởng (atomic).
- Lưu "current AFK stage" = stage tiến độ (dùng phase 37).

# Không thuộc phạm vi

- AFK accrual (phase 37).
- Energy (phase 36).
- Tower/game mode khác (Could/Post-MVP).

# Deliverables

- Campaign model + tiến độ + mở khoá.
- Client: màn campaign (chọn stage, đánh, tiến độ).
- Integration test: clear stage → mở kế + thưởng; không skip stage khoá; tiến độ server-authoritative.
- Cập nhật [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md) + [`../mvp/02-core-game-loop.md`](../mvp/02-core-game-loop.md).

# Công việc cần thực hiện

- [x] Schema stage/chapter (mở rộng 06): địch (team địch), thưởng, điều kiện mở khoá.
  → Thêm loại config `chapter` (`shared/config-schema/chapter.schema.json` + `common.schema.json#chapter_id` + `ConfigType.Chapter` + `ConfigFileMapper["chapters"]` + `ReferenceValidator.Chapter` `stages[]→stage` + fixtures). Stage giữ `enemies`/`rewards`/`requirements` (Phase 06/30). Config-validator xanh (25 file, 56 test).
- [x] Domain: `CampaignProgress` (gắn profile: stage cao nhất clear).
  → `GameTeam.Domain/Campaign/` (`CampaignProgress` 1-1 profile, tập `ClearedStage` + `CurrentAfkStageId` + event; `MarkStageCleared` first-clear idempotent). Domain.Tests 135 (+5).
- [x] Application: `StartCampaignBattleCommand` (dùng battle flow 30) → thắng cập nhật tiến độ + thưởng atomic; `GetCampaignProgressQuery`.
  → Tách cơ chế dùng chung `BattleExecutionService`+`StageRewardService` (reuse battle flow 30, không fork sim); `StartCampaignBattleCommand`/`GetCampaignProgressQuery`+handlers; thưởng+tiến độ+AFK trong MỘT transaction (ITransactionalRequest). Application.Tests 132 (+11).
- [x] Ràng buộc: chỉ đánh stage đã mở (tuần tự); chống skip.
  → `CampaignChain.IsUnlocked` (chuỗi chapter theo order); locked → `CAMPAIGN_STAGE_LOCKED` (403) TRƯỚC mọi mutation. Test locked/anti-skip xanh (Application + Api).
- [x] Đặt "current AFK stage" từ tiến độ (cho phase 37).
  → `CampaignProgress.CurrentAfkStageId` = `CampaignChain.NextAfkStageId` (stage clear xa nhất); phơi qua `GET /campaign/progress`. KHÔNG hiện thực AFK accrual (phase 37).
- [x] Client feature `campaign/`: danh sách stage, trạng thái mở/khoá, đánh, hiển thị tiến độ.
  → `client/src/ui/campaign/` (view+presenter) đọc `/campaign/progress` → `StateCache.apply_campaign_progress` → render mở/khoá/đã-clear; đánh reuse màn battle (context `campaign_stage_id` → `POST /campaign/battles`); hub wiring. gdUnit4 156 (+6).
- [x] Integration test: clear→mở kế+thưởng; đánh stage khoá bị chặn; tiến độ đúng.
  → `CampaignEndpointTests` (Testcontainers pg16): clear→unlock+reward+AFK; locked 403 no-mutation; anti-skip; loss no-advance; retry+replay idempotent (first-clear only). Api 104, Infrastructure `CampaignProgressPersistenceTests` 77.
- [x] Cập nhật `../gameplay/progression-and-economy.md`.
  → Cập nhật + `../mvp/02-core-game-loop.md`/`05-player-progression.md` + backend/config docs + `.instructions/*` + `.claude/agents/*` + CLAUDE.md §4.6 + `.memory/0032`.

# Tiêu chí hoàn thành

- Đánh stage (battle flow) → thắng cập nhật tiến độ + thưởng (atomic, server).
- Không thể đánh stage chưa mở (chống skip).
- Tiến độ server-authoritative; client hiển thị.
- "Current AFK stage" phản ánh tiến độ (sẵn cho 37).

# Cách kiểm tra

- `dotnet test`: clear→mở+thưởng; stage khoá chặn; tiến độ.
- Local: đẩy vài stage → tiến độ tăng, thưởng vào kho/ví.
- gdUnit4: màn campaign trạng thái mở/khoá đúng.

# Rủi ro

- **Skip stage/cheat tiến độ** → server validate tuần tự + battle server-authoritative.
- **Thưởng nửa chừng** → atomic với cập nhật tiến độ.
- **Khó/wall quá sớm** → số liệu ở config, tune được (không sửa code).

# Ghi chú

Campaign đặt AFK-stage rate (nguồn AFK phase 37). Số liệu (địch/thưởng/độ khó) là config/tuning. Bám [`../mvp/02-core-game-loop.md`](../mvp/02-core-game-loop.md) + [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md).

# Technical Debt Review

- **Maintainability:** stage là data; thêm chapter không sửa code.
- **Scalability:** hỗ trợ nhiều chapter/stage.
- **Testing:** tiến độ/mở khoá/thưởng có test.
- **Security:** tiến độ + thưởng server-authoritative.
- **Nợ:** AFK (37); energy (36); tower (Post-MVP).

# Phase Review

Đóng khi campaign chain + tiến độ + thưởng server-authoritative, chống skip, sẵn AFK-stage, test xanh.

**Kết luận (2026-09-13): ĐỦ ĐIỀU KIỆN ĐÓNG.** Chuỗi campaign config-driven (`chapter` type + stage `enemies`/`rewards`);
tiến độ `CampaignProgress` server-authoritative (stage đã clear + `CurrentAfkStageId`, gắn 1-1 profile, unique DB);
`StartCampaignBattleCommand` reuse battle flow 30 (một sim/reward path qua `BattleExecutionService`/`StageRewardService`) →
VICTORY first-clear cập nhật tiến độ + thưởng + AFK stage ATOMIC; mở khoá tuần tự (`CampaignChain`), stage khoá → 403
(no mutation); thưởng **first-clear only** (clear lại/thua không cấp lại, idempotent per-profile). Client `campaign/`
hiển thị mở/khoá/đã-clear (server-authoritative), đánh reuse màn battle. Verify: Release build 0 error; `dotnet test`
Domain 135 / Application 132 / Contracts 36 / Infrastructure 77 / Api 104 (Testcontainers pg16, 5 kịch bản); codegen 41
(+3 file, no drift); config-validator 56 (+25 file); gdUnit4 156, 0 orphan; migration `AddCampaignProgress`
`has-pending-model-changes` sạch. Ngoài phạm vi (đúng): AFK accrual (37), energy (36), hero upgrade (35), tower (Post-MVP).

---

## Liên kết
- [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md) · [`../mvp/02-core-game-loop.md`](../mvp/02-core-game-loop.md) · [`../mvp/05-player-progression.md`](../mvp/05-player-progression.md)
- ADR: [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md) · [`../adr/ADR-011-combat-authority-and-determinism.md`](../adr/ADR-011-combat-authority-and-determinism.md)
- Roadmap: [`README.md`](README.md) → kế: [`35-hero-upgrade-level.md`](35-hero-upgrade-level.md)
