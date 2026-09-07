# 0027 — Team & Formation standardized (Phase 29)

- **Trạng thái:** Đã chốt & verify cục bộ (2026-09-06). Đội hình **6 hero + vị trí** — lựa chọn tactical duy nhất
  trong combat full-auto (A02/A05/A12). Lưu **server-authoritative** (ADR-007); vị trí đi vào combat sim, ảnh hưởng
  target/aggro (ADR-011). Client chỉ gửi **intent** — server validate + quyết định. Nối collection ↔ combat, trước
  Battle flow (30).
- **Bối cảnh:** Sim đã dùng `slot` từ Phase 24/25 (`ResolveByRule` nhắm slot nhỏ nhất, `UnitSnapshot`/`CombatTeamMember`
  đã có `Slot`). Phase 29 KHÔNG phát minh aggro mới — chỉ persist đội + validate + nối đội→snapshot→sim + UI.

## Quyết định (user-approved)

- **Lưới = loại config `formation` mới** (không hardcode, không nhét vào economy): `formation.schema.json` +
  `config/formation/formation_default.json` (`rows`/`cols`); **team size = rows×cols**. `ConfigType.Formation` trong
  validator core lib điều khiển CẢ config-validator gate LẪN `ConfigBundleBuilder` (Enum.GetValues) — một nguồn.
- **Team = aggregate riêng** (mirror Phase-27 OwnedHero): `teams` + owned `team_slots`, unique `profile_id` (một
  đội/profile MVP) — KHÔNG nhồi vào `PlayerProfile` schema.

## Thành phần

- **Domain** `GameTeam.Domain/Teams/`: `Team : AggregateRoot<Guid>` (ProfileId unique, `TeamSlot`{SlotIndex,HeroId},
  `Create`/`Replace`/`Restore`, event `TeamSaved`). Guard **cấu trúc** (≥1 ô, không trùng ô/hero); ràng buộc
  **phụ thuộc config** (đúng số ô, range, ownership) ở Application.
- **Application** `GameTeam.Application/Features/Teams/`: `FormationConfig` POCO (`IConfigProvider.Get<FormationConfig>
  ("formation","formation_default")`, SlotCount=Rows*Cols), `SaveTeamCommand`(+Validator+Handler, `ITransactionalRequest`),
  `GetMyTeamQuery`(+Handler), `TeamErrors`, `TeamMapping`, **`TeamSnapshotFactory`** (Team→`IReadOnlyList<CombatTeamMember>`
  bất biến, slot→combat slot, actor `ally_{i}`). `ITeamRepository` (Abstractions). Owner suy từ token `sub`
  (`ICurrentUser`) — chống IDOR. Mã lỗi: `TEAM_INVALID_SIZE/_SLOT/_DUPLICATE_HERO/_HERO_NOT_OWNED` (→400),
  `PROFILE_NOT_FOUND`, `FORMATION_CONFIG_MISSING`, `UNAUTHENTICATED`.
- **Infrastructure**: `TeamConfiguration` (bảng `teams` + owned `team_slots` PK `(team_id,slot_index)`, unique
  `profile_id`, FK cascade; `slot_index` ValueGeneratedNever), `TeamRepository`, `DbSet<Team> Teams`, DI, migration `AddTeams`.
- **Contracts** `GameTeam.Contracts/Team/`: `TeamDto`/`TeamSlotDto`/`SaveTeamRequest` (SaveTeamCommand tái dùng
  `TeamSlotDto` làm input — không type trùng). Endpoint `GET`+`POST /api/v1/team` (protected). Codegen sinh 3 DTO
  GDScript (`RealSpecTests` file-list cập nhật = hợp đồng codegen). `NetworkResponseParser.parse_team`.
- **Config type mới**: `ConfigType.Formation` + `ConfigFileMapper["formation"]` + `formation.schema.json` +
  `common.schema.json#/$defs/formation_id` + fixtures + `config/formation/formation_default.json` (2×3). `ConfigBundleBuilderTests`
  "eight"→"all"; `ConfigFileMapperTests` +InlineData.
- **Client** `client/src/ui/formation/`: `FormationView` (BaseView, network-free — dựng lưới rows×cols từ config +
  roster owned) + `FormationPresenter` (bản nháp cục bộ: select_hero/place_slot/clear_slot; save→`post_json("/team")`→
  hiển thị lại theo đội SERVER; open→`get_json("/team")`). Hub: thêm nút "Đội hình" (`MainHubView.FEATURES` +
  `MainHubPresenter` route `FORMATION_PATH`). KHÔNG thêm event EventBus.

## Chống tự-vẽ (binding)

- **Server là authority (ADR-007/011):** client gửi `SaveTeamRequest` intent; server validate TẤT CẢ + trả đội chuẩn;
  client hiển thị theo đó — KHÔNG lưu cục bộ coi như đã nhận. Sim đọc **snapshot bất biến** (config@vN), không giữ tham
  chiếu profile/team mutable.
- **KHÔNG hardcode** team size/grid (đọc `FormationConfig`); KHÔNG nguồn team thứ hai; KHÔNG đọc owner từ client input;
  KHÔNG lưu stats ở `team_slots`; **KHÔNG fork thuật toán aggro** (slot-order có sẵn; CB3 vẫn `[OPEN]`). KHÔNG hand-edit
  `client/src/data/generated`/`openapi.json`.
- **Thêm loại config = hợp đồng Phase 06/07** (schema + config + enum + mapper + test + fixtures cùng một change).
- Ngoài scope: battle thật (30), nhiều đội/preset (Post-MVP), bonus vị trí (config/tuning), aggro CB3, gap `slot` ở
  `stage.schema.json` (phase stage sau).

## Verify

- `dotnet build` 0 error; `dotnet test server/GameTeam.sln` (Docker Desktop bật) **toàn bộ xanh**: Domain **107**
  (`BattleSimulatorFormationTests`: đổi slot → TargetSelected/event log khác; `TeamTests`), Application **68**
  (`SaveTeamCommandHandlerTests` 9 + `GetMyTeamQueryHandlerTests` + `TeamSnapshotFactoryTests`), Contracts **36**,
  Infrastructure **48** (Testcontainers pg16 `TeamPersistenceTests`: save/get + unique `profile_id` + upsert `Replace` +
  dispatch `TeamSaved`), Api.IntegrationTests **64** (Testcontainers `TeamEndpointTests`: login→POST/GET, 5-hero→400,
  cross-owner isolation, 401; + `OpenApiContractTests` team paths/schemas).
- config-validator **48** + `run.sh` exit 0 (9 file); codegen **41** + idempotent, no generated/openapi drift ngoài additive
  team; `has-pending-model-changes` sạch; migration `AddTeams` (teams + team_slots).
- Godot 4.7.1 `--headless --import` exit 0; gdUnit4 **121/121 pass, 0 orphan** (formation 7).
- **CI-pending:** các gate GitHub Actions (`ci-server.yml`/`ci-client.yml`/`validate-config.yml`/`codegen-check.yml`) — đã
  xanh đầy đủ ở local (gồm Testcontainers), chờ kết quả Actions.

## Liên kết

- CLAUDE.md §4.6 (Team & Formation block) · [[0025-hero-system-standardized]] (OwnedHero pattern) ·
  [[0017-profile-persistence-standardized]] (save root) · [[0019-config-service-standardized]] (IConfigProvider) ·
  [[0022-combat-sim-server-standardized]] (CombatInputResolver/slot) · [[0006-codegen-pipeline-standardized]] (codegen).
- Docs: `docs/gameplay/hero-system.md` §8, `docs/gameplay/combat-framework.md` §7/§14,
  `docs/gameplay/configuration-and-data.md`, `docs/roadmap/29-formation-team.md`.
