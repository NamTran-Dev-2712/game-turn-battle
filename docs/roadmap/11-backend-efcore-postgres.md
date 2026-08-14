# 11 — Infrastructure: EF Core + PostgreSQL + migrations base

> Mục đích: Dựng nền persistence: `DbContext`, cấu hình EF Core + Npgsql, `IUnitOfWork`/repository hiện thực, migration nền + versioning — hiện thực các port của phase 10.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 2 Backend Core Framework | P1 | S2 | F11 (nền) |

# Mục tiêu

`GameTeam.Infrastructure` có `AppDbContext` (Npgsql), cấu hình EF, hiện thực `IUnitOfWork`/`IRepository<T>`, dispatch domain event khi SaveChanges, migration khởi tạo + trường schema version, đăng ký trong `AddInfrastructure`.

# Lý do

ADR-007: save server-authoritative trên PostgreSQL, có schema versioning + migration. Nền persistence phải sẵn trước auth/profile (phase 19) và mọi state nghiệp vụ. Domain event dispatch nối tại SaveChanges (transaction outbox tối giản).

# Phụ thuộc

- **Trước:** 10 (port), 09 (domain event/base), 04 (Postgres dev).
- **Sau:** 19 (profile), 21 (config cache đọc DB nếu cần), mọi feature ghi DB.

# Phạm vi

- `AppDbContext` + entity configuration (Fluent) tách theo `IEntityTypeConfiguration`.
- Npgsql provider; connection string từ config (`ConnectionStrings__Postgres`).
- Hiện thực `IUnitOfWork` (transaction) + `IRepository<T>` generic.
- Dispatch domain events sau SaveChanges (trong cùng transaction).
- Migration khởi tạo; quy ước schema version field (ADR-007).
- `IClock` hiện thực (server UTC).

# Không thuộc phạm vi

- Redis cache (phase 12).
- Bảng nghiệp vụ cụ thể (profile ở phase 19; hero/currency ở phase feature).
- Backup/restore vận hành (phase 55 / deploy docs).

# Deliverables

- `AppDbContext` + cấu hình + repository/UoW hiện thực.
- Migration khởi tạo chạy được trên Postgres dev.
- Integration test (Testcontainers Postgres) cho UoW + repository + domain-event dispatch.
- `AddInfrastructure` đăng ký DbContext/UoW/clock; không còn stub.

# Công việc cần thực hiện

- [x] Thêm `AppDbContext`, đăng ký Npgsql, đọc connection string qua options. — `Persistence/AppDbContext.cs`; `AddInfrastructure` gọi `AddDbContext<AppDbContext>(UseNpgsql(GetConnectionString("Postgres")))`; guard ném nếu thiếu (không hardcode).
- [x] Cấu hình EF theo `IEntityTypeConfiguration` (một file/entity), quy ước bảng `snake_case`. — `Persistence/Configurations/SchemaMetadataConfiguration.cs` (`ToTable("schema_metadata")`, cột `id`/`version`); nạp qua `ApplyConfigurationsFromAssembly`.
- [x] Hiện thực `IRepository<T>` + `IUnitOfWork` (BeginTransaction/Commit/Rollback qua EF). — `Persistence/Repositories/EfRepository.cs` (GetById/Add, không rò `IQueryable`/`DbContext`), `Persistence/UnitOfWork.cs` (Commit tự SaveChanges rồi commit tx vì port không có SaveChanges; guard double-begin; `IAsyncDisposable`).
- [x] Override SaveChanges để **dispatch domain events** thu từ aggregate (sau khi persist, cùng transaction). — `AppDbContext.SaveChangesAsync` thu event từ `IHasDomainEvents` → `base.SaveChanges` → `DomainEventDispatcher` (MediatR `IPublisher` + wrapper `DomainEventNotification<T>`) → clear. Vì Commit gọi SaveChanges trong tx đang mở ⇒ dispatch sau persist, cùng transaction. Integration test xác nhận.
- [x] Hiện thực `IClock` (UTC server) + đăng ký DI. — `SystemClock` (Phase 09/10) tái dùng, `AddInfrastructure` giữ `AddSingleton<IClock, SystemClock>()`.
- [x] Tạo migration khởi tạo (`dotnet ef migrations add Initial`) + trường schema version. — `Persistence/Migrations/*_Initial.cs` tạo `schema_metadata` + seed `version=1` (HasData). `has-pending-model-changes` sạch. `database update` up/down verify trên Postgres dev.
- [x] Cập nhật `AddInfrastructure` đăng ký đầy đủ; bỏ stub. — DbContext + `IUnitOfWork`/`IRepository<,>` (scoped) + `DomainEventDispatcher` + `IClock`; TODO stub đã xoá.
- [x] Integration test Testcontainers: ghi/đọc entity mẫu, transaction rollback, domain event bắn. — `Persistence/PersistenceIntegrationTests.cs` (CRUD, rollback thực, dispatch) + `MigrationIntegrationTests.cs` (up tạo+seed, down revert). 7 test Infrastructure xanh (4 integration) trên `postgres:16-alpine`.
- [x] Cập nhật [`../backend/infrastructure.md`](../backend/infrastructure.md). — §1/§5 phản ánh hiện thực thật.

# Tiêu chí hoàn thành

- Migration chạy sạch trên Postgres dev (`up`/`down`).
- Integration test (Testcontainers) xanh: CRUD mẫu, rollback, domain-event dispatch.
- `AddInfrastructure` không còn stub; Infra chỉ hiện thực port (không rò EF lên Application/Domain).
- Connection string lấy từ config, không hardcode.

# Cách kiểm tra

- `scripts/dev/up` → `dotnet ef database update` → bảng tạo đúng.
- `dotnet test` (Infrastructure.Tests) với Testcontainers Postgres.
- NetArchTest: Application/Domain không ref EF; chỉ Infra dùng EF.
- Kiểm domain event: ghi aggregate raise event → handler nhận sau SaveChanges.

# Rủi ro

- **Testcontainers cần Docker trong CI** → đảm bảo runner có Docker; fallback skip có ghi chú.
- **Domain event dispatch ngoài transaction** → dispatch trong cùng SaveChanges/transaction (nhất quán).
- **Migration drift** → CI kiểm `dotnet ef migrations has-pending-model-changes` (nếu có).
- **Rò rỉ EF lên Application** → chỉ trả entity/domain, không `IQueryable` ra ngoài repo.

# Ghi chú

Idempotency cho claim/transaction (chống double-grant, ADR-007) đặt nền ở đây (bảng/khoá idempotency), dùng thực ở phase 31 (currency) & 37 (AFK). Bám [`../backend/infrastructure.md`](../backend/infrastructure.md).

# Technical Debt Review

- **Maintainability:** cấu hình EF tách file; repository/UoW chuẩn.
- **Scalability:** modular monolith; index/migration quản lý có version.
- **Testing:** integration Testcontainers là hợp đồng persistence.
- **Security:** connection string qua secret; không log query nhạy cảm.
- **Nợ:** bảng idempotency dùng đầy đủ ở phase 31/37; backup vận hành ở phase 55.

# Phase Review

Đóng khi DbContext+UoW+repo+migration+domain-event dispatch có integration test xanh, Infra không rò lên trên, connection từ config.

**Kết luận: ĐỦ ĐIỀU KIỆN ĐÓNG** (verify local, SDK 9.0.306, Windows + Docker Desktop 28.5.1, 2026-08-13):
- `dotnet build server/GameTeam.sln -c Release` — **0 warning / 0 error** (warnings-as-error).
- `dotnet test server/GameTeam.sln -c Release` — **128 pass / 0 fail** (Infrastructure.Tests **7**: 3 DI smoke + 4 integration; Application.Tests **26** gồm arch test EF-boundary mới).
- Testcontainers `postgres:16-alpine`: CRUD ghi/đọc, **rollback thực** (SaveChanges trong tx rồi rollback ⇒ hàng biến mất), **domain-event dispatch** (aggregate raise → SaveChanges → handler nhận đúng `SampleCreated`, event được clear), migration up (tạo `schema_metadata` + seed `version=1`) & down (revert) — tất cả xanh.
- Migration trên Postgres dev (compose, port override 5544): `dotnet ef database update` tạo bảng + seed `id=1,version=1`; `dotnet ef database update 0` revert sạch (chỉ còn `__EFMigrationsHistory`). `has-pending-model-changes` = không drift.
- Architecture gate: NetArchTest `Application_should_not_depend_on_efcore_or_npgsql` — negative (rò `DbContext` vào Application) ⇒ **đỏ** → revert ⇒ **xanh**. Domain purity gate (Phase 09) vẫn xanh (marker `IHasDomainEvents` BCL-only).
- Connection string từ config (`ConnectionStrings:Postgres` / env `ConnectionStrings__Postgres`) — không hardcode credential runtime; guard fail-fast khi thiếu. `openapi.json` không drift.
- Scope: KHÔNG Redis/auth/profile/hero/currency; bảng idempotency chỉ *ghi chú* nền (dùng ở 31/37); `IDomainEvent` vẫn marker thuần.

---

## Liên kết
- [`../backend/infrastructure.md`](../backend/infrastructure.md) · [`../testing/backend-testing.md`](../testing/backend-testing.md)
- ADR: [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md) · [`../adr/ADR-003-backend-architecture.md`](../adr/ADR-003-backend-architecture.md) · [`../adr/ADR-010-dependency-management.md`](../adr/ADR-010-dependency-management.md)
- Roadmap: [`README.md`](README.md) → kế: [`12-backend-redis-cache.md`](12-backend-redis-cache.md)
