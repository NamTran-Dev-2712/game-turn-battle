# 0017 — Profile persistence & schema versioning standardized (Phase 19)

- Date: 2026-08-23
- Scope: workspace
- Status: Active

## Decision

Nền **save server-authoritative** (ADR-007) có MỘT gốc: aggregate **`PlayerProfile`**. Phase feature sau **mở rộng**
profile (không dựng root song song); mọi thay đổi qua **command server** (client không authority).

- **Domain** (`GameTeam.Domain/Profiles/`): **`PlayerProfile : AggregateRoot<Guid>`** — `Id` riêng + `AccountId`
  (**1-1 với `Account`**, unique ở DB), `DisplayName`/`Level` (field contract Phase 05 — đặt mặc định, **không** phát
  minh state nghiệp vụ), `SchemaVersion`, `CreatedAt`/`UpdatedAt` (từ `IClock`). Factory **`CreateForAccount`** raise
  **`PlayerProfileCreated`**; **`Restore`** dựng lại từ trạng thái đã lưu (không event, cho migration/test).
  **Versioning:** `const CurrentSchemaVersion = 1`; **`Upgrade(nowUtc)`** migrate **dữ liệu** `v(N)→v(N+1)`
  (read-repair, deterministic; bước mẫu `MigrateV0ToV1` back-fill `DisplayName`, **giữ `Level`**). Đây là migrate
  **dữ liệu profile** — KHÁC EF Core DDL migration.
- **Application** (`Features/Profile/`): **`GetOrCreateProfileCommand`** (`ITransactionalRequest`) backing
  `GET /api/v1/profile` — get-or-create + read-repair `Upgrade`, atomic; **`GetMyProfileQuery`** đọc thuần
  (→`PROFILE_NOT_FOUND`). Chủ sở hữu **chỉ** từ token `sub` qua port **`ICurrentUser`** (`Abstractions/Security/`).
  Repo port **`IPlayerProfileRepository`** (`GetByAccountIdAsync`, không rò `IQueryable`). Không validator (không có
  input client). **`CreateGuestAccountCommandHandler`** tạo `PlayerProfile` **cùng transaction** với `Account` (eager).
- **Infrastructure:** `PlayerProfileConfiguration` (bảng **`player_profiles`** snake_case, **unique index
  `account_id`** + FK→`accounts` cascade), `PlayerProfileRepository`, `DbSet` trên `AppDbContext`, migration
  **`AddPlayerProfiles`**. **Idempotency ở tầng DB** = unique index (KHÔNG check-then-insert).
- **Api:** adapter **`GameTeam.Api/Auth/CurrentUser.cs`** (`ICurrentUser` trên `IHttpContextAccessor`, đọc `sub`/
  `NameIdentifier`), đăng ký `AddHttpContextAccessor` + `ICurrentUser` ở `AddApi`. `/profile` **chuyển** từ stub 501
  (literal `/api/v1`, Phase 05) vào **version set** (`.MapToApiVersion(1)`, protected mặc định — KHÔNG `.AllowAnonymous`).

## Quyết định người dùng (đã chốt)

1. **Định danh:** `Id` riêng + unique index `account_id` (+ `IPlayerProfileRepository.GetByAccountIdAsync`) — KHÔNG dùng
   `Id == AccountId`. Khớp phase text ("id, accountId" + "unique index on AccountId") + id profile ổn định cho state con.
2. **Khởi tạo:** eager trong guest-login handler (cùng transaction) — KHÔNG lazy-on-GET, KHÔNG dùng handler của
   `AccountCreated` (dispatcher không chạy SaveChanges lần hai ⇒ insert staged trong event handler sẽ không persist).
3. **403:** endpoint self-only, owner từ `sub`; KHÔNG route nhận id người khác ⇒ cross-account read bất khả theo cấu
   trúc (chống IDOR mạnh nhất). Test chứng minh B luôn nhận profile của B + `accountId` inject bị bỏ qua.

## Ràng buộc cho phase sau

- **Reuse, đừng phát minh:** dùng `PlayerProfile`/`IPlayerProfileRepository`/`ICurrentUser`/`GetOrCreateProfileCommand`
  trước khi thêm persistence state người chơi; KHÔNG root save thứ hai, KHÔNG current-user/profile mechanism thứ hai.
- **Profile là root:** feature state (currency 31, hero 27/35, inventory 32, progress 34) **mở rộng** profile.
- **Đổi cấu trúc profile ⇒** bump `SchemaVersion` + bước `MigrateV{n}ToV{n+1}` + **test preservation** + EF migration +
  doc-sync, cùng một change (không lưu int version trần không migration).
- **Ownership** luôn từ token `sub`; **KHÔNG** tin id client. Mutation qua command server (server đặt
  `schema_version`/`account_id`/owner/timestamp).
- **Ngoài phạm vi:** PUT/arbitrary update, provider linking, refresh-token rotation, backup/restore (55).

## Verify

- `dotnet build` 0/0; Domain.Tests + Application.Tests xanh (gồm architecture facts Application ⊥ Infra/EF/JWT).
- Testcontainers Postgres: persist/read theo `account_id`, **unique constraint chặn profile thứ hai**, dispatch
  `PlayerProfileCreated`, **migrate v0→current giữ dữ liệu**.
- Api.IntegrationTests (Testcontainers): login→`GET /profile` 200 đúng chủ, retry→1 hàng, cross-owner cô lập, no-token 401.
- `has-pending-model-changes` sạch; openapi.json chỉ đổi thứ tự path (ProfileDto không đổi) — no client generated drift.

## Liên kết

- Phase: `docs/roadmap/19-profile-persistence-versioning.md`
- Docs: `docs/backend/domain-and-application.md`, `docs/backend/infrastructure.md` §1.2, `docs/adr/ADR-007-save-strategy.md`
- CLAUDE.md §4.6 (Phase 19 block) · `.instructions/backend.md` · `.claude/agents/dotnet-backend.md` · doc-sync matrix row
