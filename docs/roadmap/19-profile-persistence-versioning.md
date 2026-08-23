# 19 — Profile persistence & schema versioning

> Mục đích: Lưu **profile người chơi server-authoritative** (chân lý ở PostgreSQL) với schema versioning + migration (ADR-007) — nền save cho toàn bộ state game.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 4 Auth, Save & Config Service | P1 | S4 | F11 |

# Mục tiêu

Tạo aggregate `PlayerProfile` (gắn Account) với trường schema version, lưu/đọc qua repository/UoW; endpoint `GET/PUT profile` (thay đổi qua command server, atomic); migration hỗ trợ tiến hoá schema profile.

# Lý do

ADR-007: chân lý state (hero, currency, progression, inventory) ở backend; client chỉ cache đọc. Profile là "gốc" chứa/tham chiếu mọi state; phải có versioning + migration để tiến hoá không mất dữ liệu người chơi.

# Phụ thuộc

- **Trước:** 18 (account/auth), 11 (persistence), 10 (MediatR).
- **Sau:** 20 (client đọc profile), và mọi feature state (currency 31, hero 27/35, inventory 32, progress 34) mở rộng profile.

# Phạm vi

- Aggregate `PlayerProfile` (id, accountId, schema_version, timestamps, khung chứa/tham chiếu state con).
- Command đọc/khởi tạo profile khi guest login lần đầu; đọc profile của chính mình (authz theo sub).
- Schema versioning: trường version + cơ chế migration dữ liệu profile khi schema đổi.
- Idempotency nền cho thao tác khởi tạo (không tạo trùng).

# Không thuộc phạm vi

- Nội dung state cụ thể (currency/hero/inventory) — thêm ở phase feature (mở rộng profile).
- Config (phase 21).
- Client integration (phase 20).

# Deliverables

- `PlayerProfile` + migration + repository.
- Endpoint `GET /api/v1/profile` (của mình) + khởi tạo khi cần.
- Cơ chế + tài liệu schema-version migration profile.
- Integration test: tạo profile khi guest login; đọc profile; authz chặn đọc profile người khác.

# Công việc cần thực hiện

- [x] Domain: `PlayerProfile` aggregate (accountId, schema_version, created/updated, khung state con). ✅ `GameTeam.Domain/Profiles/PlayerProfile.cs` (+`PlayerProfileCreated`); Domain.Tests **43 pass**.
- [x] Application: `GetOrCreateProfileCommand`/`GetMyProfileQuery` + handler; map DTO (contract phase 05). ✅ `Features/Profile/*`; **không validator** (không có input client — theo hướng dẫn không tạo validator vô nghĩa); Application.Tests **38 pass** (gồm arch facts App⊥Infra/EF/JWT).
- [x] Infrastructure: cấu hình EF cho profile, migration; repository. ✅ `PlayerProfileConfiguration` (bảng `player_profiles`, unique `account_id` + FK→`accounts`) + `PlayerProfileRepository` + migration `AddPlayerProfiles`; build Release 0/0; `has-pending-model-changes` **sạch**.
- [x] Khởi tạo profile idempotent khi guest login lần đầu (không tạo trùng nếu retry). ✅ eager cùng transaction (`CreateGuestAccountCommandHandler`, unit test asserts profile staged) + **unique index `account_id`** trong migration (bảo đảm DB-level, đọc được ở `_AddPlayerProfiles.cs`). Bằng chứng runtime unique-constraint/retry→1-hàng ở Testcontainers (`PlayerProfilePersistenceTests`/`ProfileEndpointTests`) — **Docker/CI-verification pending** (Docker daemon chưa khởi động cục bộ; xác nhận trên CI ubuntu).
- [x] Authz: chỉ đọc/ghi profile của `sub` trong token. ✅ port `ICurrentUser` (adapter `Api/Auth/CurrentUser.cs`); owner **chỉ** từ `sub` — unit-verified (`GetOrCreateProfileCommandHandlerTests`/`GetMyProfileQueryHandlerTests`, Application.Tests pass). HTTP cross-owner cô lập ở `ProfileEndpointTests` — **Docker/CI-verification pending**.
- [x] Schema versioning: trường `schema_version`, quy ước migration (map version cũ→mới) + test migration mẫu. ✅ `CurrentSchemaVersion=1` + `PlayerProfile.Upgrade()` (`v0→v1` giữ `Level`); **Domain.Tests migration-preservation pass** (bằng chứng non-Docker). Preservation ở tầng persistence (`PlayerProfilePersistenceTests`) — Docker/CI-verification pending.
- [ ] Integration test: login→profile tạo; đọc profile; đọc profile người khác→403. **Tests đã viết + compile** (`Api.IntegrationTests/ProfileEndpointTests` + `Infrastructure.Tests/PlayerProfilePersistenceTests`, Testcontainers): login→`GET /profile` 200 đúng chủ; retry→1 hàng; cross-owner cô lập (endpoint self-only ⇒ không thể đọc profile người khác — §12, thay 403 literal bằng cô lập theo cấu trúc, chống IDOR mạnh hơn); no-token→401; unique-constraint; migrate-v0→current giữ dữ liệu. **Docker/CI-verification pending** — Docker daemon chưa khởi động cục bộ (giữ `[ ]` đến khi có kết quả run: local Docker hoặc CI ubuntu, theo Strict Phase Gate §4.5).
- [x] Cập nhật [`../backend/domain-and-application.md`](../backend/domain-and-application.md) + [`../backend/infrastructure.md`](../backend/infrastructure.md). ✅ + ADR-007 (Implementation) + CLAUDE.md §4.6 + `.instructions/backend.md` + `.claude/agents/dotnet-backend.md` + `.memory/0017` + doc-sync matrix.

# Tiêu chí hoàn thành

- Guest login lần đầu → profile được tạo (idempotent khi retry).
- `GET /profile` trả profile của chính mình; chặn người khác (403).
- `schema_version` tồn tại; có test migration version cũ→mới không mất dữ liệu.
- Mọi thay đổi profile qua command server (không client-authority).

# Cách kiểm tra

- `dotnet test` (integration): tạo/đọc profile, authz 403, migration.
- Local: guest login → `GET /profile` → dữ liệu đúng chủ.
- Thử tạo lại (retry) → không sinh profile trùng.

# Rủi ro

- **Migration làm hỏng dữ liệu người chơi** → migration có test + backup (vận hành phase 55); expand-then-contract.
- **Tạo profile trùng khi retry** → idempotency (khoá theo accountId).
- **Rò profile người khác** → authz theo `sub` bắt buộc + test.

# Ghi chú

Profile là root cho state; feature sau **mở rộng** profile (thêm bảng/tham chiếu), luôn tăng `schema_version` khi đổi cấu trúc + doc-sync. Bám ADR-007 + [`../backend/domain-and-application.md`](../backend/domain-and-application.md).

# Technical Debt Review

- **Maintainability:** save tập trung, versioning rõ.
- **Scalability:** profile tham chiếu state con, mở rộng không phá.
- **Testing:** integration + migration test bảo vệ dữ liệu.
- **Security:** server-authoritative + authz theo chủ sở hữu.
- **Nợ:** backup/restore vận hành (55); state con thêm dần.

# Phase Review

Đóng khi profile tạo/đọc server-authoritative + versioning + migration test + authz chủ sở hữu xanh.

## Review (2026-08-23 — local)

**Đã hiện thực & verify (non-Docker):**
- Domain `PlayerProfile` (1-1 Account, `SchemaVersion`, `Upgrade()` migrate `v0→v1` giữ `Level`) + `PlayerProfileCreated`.
- Application `GetOrCreateProfileCommand` (backing `GET /api/v1/profile`, get-or-create + read-repair, atomic) + `GetMyProfileQuery`; ownership từ token `sub` qua port `ICurrentUser` (adapter Api). Không validator (không input client).
- Infrastructure: bảng `player_profiles` (unique `account_id` + FK→`accounts`), `PlayerProfileRepository`, migration `AddPlayerProfiles`. Eager profile trong guest-login transaction.
- Api: `/profile` chuyển vào **version set** (protected mặc định).
- **Verify:** `dotnet build -c Release` **0/0**; `dotnet test` Domain **43** + Application **38** pass (gồm arch facts App⊥Infra/EF/JWT); `has-pending-model-changes` **sạch**; `openapi.json` chỉ đổi thứ tự path (ProfileDto không đổi) — **no client generated drift**.
- Doc-sync đầy đủ: `domain-and-application.md`, `infrastructure.md` §1.2, ADR-007 (Implementation), CLAUDE.md §4.6, `.instructions/backend.md`, `.claude/agents/dotnet-backend.md`, `.memory/0017`, doc-sync matrix.

**Còn pending (Docker/CI):** các test Testcontainers (`PlayerProfilePersistenceTests` — persist/read, unique-constraint, dispatch `PlayerProfileCreated`, migrate-preservation; `ProfileEndpointTests` — login→`GET /profile` owner, retry→1 hàng, cross-owner cô lập, 401) **đã viết + compile** nhưng chưa chạy được cục bộ (Docker daemon không khởi động trên máy này). Theo Strict Phase Gate §4.5, mục "Integration test" giữ `[ ]` đến khi có kết quả run (local Docker hoặc CI ubuntu-latest — nơi Testcontainers luôn chạy được, như Phase 11/12/18).

**Kết luận:** implementation + non-Docker verification **đủ điều kiện**; **chưa đủ điều kiện đóng hoàn toàn** cho tới khi Testcontainers/integration xanh trên Docker/CI (một mục `[ ]` CI-pending còn lại).

---

## Liên kết
- [`../backend/domain-and-application.md`](../backend/domain-and-application.md) · [`../backend/infrastructure.md`](../backend/infrastructure.md)
- ADR: [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md) · [`../adr/ADR-003-backend-architecture.md`](../adr/ADR-003-backend-architecture.md)
- Roadmap: [`README.md`](README.md) → kế: [`20-client-auth-profile-integration.md`](20-client-auth-profile-integration.md)
