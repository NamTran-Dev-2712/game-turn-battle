# 29 — Formation & Team-of-6

> Mục đích: Hiện thực **đội hình 6 hero + vị trí (formation)** — quyết định chiến thuật duy nhất trong combat auto; lưu server-authoritative, ảnh hưởng target/aggro của sim.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 6 Gameplay Vertical Slice | P2 | S7 | F03 |

# Mục tiêu

Người chơi chọn 6 hero vào lưới formation (vị trí ảnh hưởng aggro/target theo spec combat 23); đội hình lưu ở server (gắn profile); client dựng UI kéo-thả/chọn ô; sim đọc formation làm input.

# Lý do

Formation là "lựa chọn tactical" duy nhất (A02/A05/A12) trong combat full-auto — mắt xích giữa collection và combat. Cần trước Battle flow (30).

# Phụ thuộc

- **Trước:** 27 (hero), 24/25 (sim đọc vị trí), 19 (profile lưu team).
- **Sau:** 30 (battle dùng team+formation), 43 (sweep dùng team).

# Phạm vi

- Server: model Team (6 slot) + Formation (lưới vị trí, ~2×3 theo A12 — cấu hình được), lưu profile, validate (đúng 6, không trùng hero — GP6).
- Client: UI chọn hero + đặt vị trí; hiển thị formation; lưu qua server command.
- Snapshot team để đưa vào sim (server tạo snapshot khi battle).

# Không thuộc phạm vi

- Battle thực (phase 30).
- Nhiều đội/preset (Post-MVP nếu có).
- Số liệu bonus vị trí (config/tuning).

# Deliverables

- Server: Team/Formation model + command lưu + validate.
- Client: UI formation (chọn hero, đặt vị trí, lưu).
- Test: lưu/đọc team; validate 6 hero; vị trí ảnh hưởng sim (input khác → kết quả khác).
- Cập nhật [`../gameplay/hero-system.md`](../gameplay/hero-system.md) (formation) / combat-framework.

# Công việc cần thực hiện

- [x] Server Domain: `Team` (6 slot, ref OwnedHero) + `Formation` (lưới vị trí cấu hình từ config). ✅ `Team`/`TeamSlot`/`TeamSaved` (`GameTeam.Domain/Teams/`); lưới = loại config `formation` (`formation.schema.json` + `config/formation/formation_default.json`, rows×cols).
- [x] Application: `SaveTeamCommand` (validate đúng 6, hero thuộc sở hữu, không trùng) + `GetMyTeamQuery`. ✅ handler server-authoritative (owner từ token, `TEAM_INVALID_SIZE/_SLOT/_DUPLICATE_HERO/_HERO_NOT_OWNED`); endpoint `GET`+`POST /api/v1/team` (protected).
- [x] Tạo team snapshot (chỉ số tại thời điểm) để feed sim (phase 30). ✅ `TeamSnapshotFactory` → `IReadOnlyList<CombatTeamMember>` bất biến (slot → combat slot); test bản-sao-giá-trị.
- [x] Client feature `formation/`: UI chọn hero + đặt vị trí lưới; lưu qua command; hiển thị. ✅ `FormationView`/`FormationPresenter` + `formation.tscn`; POST intent → hiển thị lại theo server; hub wiring.
- [x] Contract DTO team/formation + codegen. ✅ `Contracts/Team/*` → `openapi.json` (additive) → GDScript generated (`team_dto`/`team_slot_dto`/`save_team_request`); `parse_team`.
- [x] Test: lưu/đọc team; validate lỗi (thiếu hero/trùng/không sở hữu); đổi vị trí → sim input đổi. ✅ Domain `BattleSimulatorFormationTests` (đổi slot → target/log khác), handler validate tests, snapshot tests, gdUnit4 formation flow; Testcontainers save/get + Api e2e (chạy local với Docker — xanh).
- [x] Cập nhật `../gameplay/hero-system.md`. ✅ §8 "Formation & Team (Phase 29)" + `combat-framework.md` §7/§14 + `configuration-and-data.md` (loại `formation`).

# Tiêu chí hoàn thành

- Lưu đội 6 hero + vị trí; validate chặn sai (≠6, trùng, không sở hữu).
- Team lưu server-authoritative; client chỉ gửi intent.
- Vị trí formation ảnh hưởng sim (target/aggro) — test chứng minh input khác→kết quả khác.
- Test server + client xanh.

# Cách kiểm tra

- `dotnet test`: save/get team, validate, snapshot; đổi formation → sim khác.
- gdUnit4: UI đặt hero/vị trí, lưu, hiển thị lại đúng.
- Thử lưu team sai (5 hero) → bị chặn.

# Rủi ro

- **Client tự ý sửa team không qua server** → mọi lưu qua command + validate server.
- **Vị trí không tác động sim** → spec 23 định nghĩa aggro theo vị trí; test đối chứng.
- **Trùng hero/không sở hữu** → validate server bắt buộc.

# Ghi chú

Grid formation kích thước theo config (A12 ~2×3, chưa khoá cứng). Một-hero-một-slot (GP6) theo assumption; validate theo config. Bám [`../gameplay/hero-system.md`](../gameplay/hero-system.md).

# Technical Debt Review

- **Maintainability:** team/formation tách rõ; grid cấu hình.
- **Scalability:** hỗ trợ preset/nhiều đội sau (Post-MVP).
- **Testing:** validate + tác động sim có test.
- **Security:** team server-authoritative, validate sở hữu.
- **Nợ:** bonus vị trí (config); preset (Post-MVP).

# Phase Review

**Trạng thái: ĐÓNG (local PASS 2026-09-06).** Đội hình 6 hero + vị trí lưu **server-authoritative** (ADR-007):
`Team` aggregate (`teams` + owned `team_slots`, unique `profile_id`) mở rộng save-root `PlayerProfile`;
`SaveTeamCommand` validate server (đúng số ô theo config, slot trong lưới không trùng, không trùng hero, **mọi hero
thuộc sở hữu** — owner suy từ token, chống IDOR) → upsert; `GetMyTeamQuery` trả đội (rỗng nếu chưa lưu). Lưới
**data-driven** qua loại config mới `formation` (rows×cols → team size, KHÔNG hardcode). **Vị trí ảnh hưởng sim** qua
cơ chế slot có sẵn (combat-framework §14: nhắm slot nhỏ nhất) — `TeamSnapshotFactory` nối đội-persisted → snapshot bất
biến `CombatTeamMember{slot}` (feed phase 30), KHÔNG đổi thuật toán. Client `formation/` (view/presenter, network-free
view) gửi **intent** POST /team → hiển thị lại theo đội server (KHÔNG lưu cục bộ). Contract→codegen đồng bộ; doc + vibe-code sync.

**Bằng chứng verify (local, Docker Desktop bật):** `dotnet build -c Release` 0 error; `dotnet test server/GameTeam.sln`
**toàn bộ xanh** — **Domain 107** (gồm `BattleSimulatorFormationTests`: đổi slot → `TargetSelected`/event log khác;
`TeamTests`), **Application 68** (`SaveTeamCommandHandlerTests` 9: valid/replace/size/dup/not-owned/bad-slot/unauth/no-profile/no-config;
`GetMyTeamQueryHandlerTests`; `TeamSnapshotFactoryTests` bản-sao-giá-trị), **Contracts 36**, **Infrastructure 48**
(Testcontainers `postgres:16-alpine` — `TeamPersistenceTests`: save/get + unique `profile_id` + upsert `Replace` + dispatch
`TeamSaved`), **Api.IntegrationTests 64** (Testcontainers — `TeamEndpointTests`: login→POST/GET /team, sai-số-hero→400,
cross-owner isolation, 401; + `OpenApiContractTests` team paths/schemas). `tools/config-validator` **48** +
`run.sh config shared/config-schema` exit 0 (9 file, gồm formation). `shared/codegen` **41** + `run.sh` regenerate idempotent
(team DTOs) — no `client/src/data/generated` drift; `openapi.json` additive (+team paths/schemas). EF
`has-pending-model-changes` sạch; migration `AddTeams` (teams + team_slots). Godot 4.7.1 `--headless --import` exit 0; gdUnit4
**toàn bộ 121/121 pass, 0 orphan** (formation 7: grid từ config, place/đổi vị trí, save→server, load, save-fail giữ nháp,
back). Grep authority: view formation không chạm `NetworkClient`/`core/net`; client không tự tính chân lý.

**CI-verification pending:** các gate trên GitHub Actions (`ci-server.yml` gồm golden-vector + Testcontainers; `ci-client.yml`
gdUnit4 + import; `validate-config.yml`; `codegen-check.yml`) — chờ kết quả Actions cho lần chạy CI (đã xanh đầy đủ ở local).

**Nợ (out-of-scope, ghi rõ):** battle thật (30), nhiều đội/preset (Post-MVP), bonus vị trí theo hàng/cột (config/tuning),
aggro nâng cao CB3 (`../mvp/10` — vẫn `[OPEN]`), gap `stage.schema.json` thiếu `slot` cho enemy (thuộc phase stage sau).

---

## Liên kết
- [`../gameplay/hero-system.md`](../gameplay/hero-system.md) · [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md) · [`../godot/ui-architecture.md`](../godot/ui-architecture.md)
- ADR: [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md) · [`../adr/ADR-011-combat-authority-and-determinism.md`](../adr/ADR-011-combat-authority-and-determinism.md)
- Roadmap: [`README.md`](README.md) → kế: [`30-battle-flow-e2e.md`](30-battle-flow-e2e.md)
