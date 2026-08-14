# 0009 — Persistence standardized (Phase 11)

- Date: 2026-08-13
- Scope: workspace
- Status: Active

## Decision

`GameTeam.Infrastructure` có nền **persistence EF Core + PostgreSQL** ở **`Persistence/`** (EF/Npgsql **CHỈ** ở
Infrastructure — NetArchTest `Application_should_not_depend_on_efcore_or_npgsql` + Domain-purity Phase 09 gác cổng),
hiện thực các port Phase 10:

- **`AppDbContext`** (`DbContextOptions<AppDbContext>` + `DomainEventDispatcher`): `OnModelCreating` =
  `ApplyConfigurationsFromAssembly`; **override `SaveChangesAsync`** — thu event từ mọi aggregate track
  (`IHasDomainEvents`) → `base.SaveChangesAsync` (persist) → dispatch → `ClearDomainEvents`. Ctor `protected` không-generic
  cho context dẫn xuất (test).
- **`EfRepository<TEntity,TId>`** (port `IRepository`, tối giản `GetByIdAsync`+`AddAsync`, `Set<TEntity>()`): **không** rò
  `IQueryable`/`DbSet`/`DbContext`.
- **`UnitOfWork`** (port `IUnitOfWork` `Begin/Commit/Rollback`): port **không** có SaveChanges ⇒ **`CommitAsync` tự gọi
  `SaveChangesAsync`** (persist+dispatch) rồi commit tx; `RollbackAsync` rollback thật; scoped; `IAsyncDisposable`; guard
  double-begin.
- **Domain-event dispatch** (`DomainEventDispatcher` + `DomainEventNotification<TDomainEvent> : INotification`): bọc mỗi
  `IDomainEvent` (marker thuần — KHÔNG là INotification) rồi MediatR `IPublisher.Publish` (đóng generic theo kiểu runtime,
  cache factory). Dispatch tại SaveChanges ⇒ **sau persist, trong cùng transaction** (ADR-007). Handler nghiệp vụ (19+) đăng
  ký `INotificationHandler<DomainEventNotification<TEvent>>`.
- **Schema version** (`SchemaMetadata` → bảng `schema_metadata`, `HasData` seed `version=1`) là neo ADR-007; profile per-row
  version = phase 19.
- **Migration** `Initial` (`Persistence/Migrations`) + design-time `AppDbContextFactory` (đọc env `ConnectionStrings__Postgres`,
  fallback dev-compose). **Connection string từ config** `ConnectionStrings:Postgres` — không hardcode; `AddInfrastructure`
  fail-fast khi thiếu, đăng ký DbContext(Npgsql)+`IUnitOfWork`/`IRepository<,>`(scoped)+`DomainEventDispatcher`+`IClock`
  (bỏ stub TODO).

**Mở rộng Phase 09 (được phép):** thêm marker BCL-only **`IHasDomainEvents`** vào `GameTeam.Domain/Common/`,
`AggregateRoot<TId>` hiện thực — để Infrastructure phát hiện event không cần biết `TId`; Domain vẫn package-free.

Verified (SDK 9.0.306, Windows + Docker Desktop 28.5.1): build Release **0 warning/0 error**; `dotnet test` **128 pass**
(Infrastructure.Tests **7** = 3 DI smoke + 4 integration Testcontainers `postgres:16-alpine`: CRUD, rollback thực,
domain-event dispatch, migration up/down; Application.Tests **26** gồm arch test EF-boundary). `dotnet ef database update`
up (tạo `schema_metadata`+seed) & `update 0` down (revert) trên Postgres dev; `has-pending-model-changes` sạch; negative
arch (rò `DbContext` vào Application) ⇒ đỏ → revert ⇒ xanh; `openapi.json` không drift.

## Why

ADR-007: save server-authoritative trên PostgreSQL, schema versioning + migration, giao dịch atomic + idempotency. Nền
persistence phải sẵn trước auth/profile (19) & mọi state nghiệp vụ. Đặt ở Infrastructure (ADR-003 DIP) — Application/Domain
không biết EF. **Dispatch tại SaveChanges** (checklist Phase 11) + **cùng transaction** (rủi ro phase: dispatch ngoài
transaction ⇒ mất nhất quán) ⇒ Commit gọi SaveChanges trong tx. **Port không có SaveChanges** (contract Phase 10) ⇒ Commit
gánh SaveChanges (không thêm method vào port — reuse contract). `IDomainEvent` giữ marker thuần (Domain package-free) ⇒ cầu
nối MediatR bằng wrapper generic (handler subscribe đúng kiểu event, không rework phase sau).

## Not this

- **Thêm `SaveChangesAsync` vào `IUnitOfWork`** hoặc tạo port persistence song song: sai — dùng đúng contract Phase 10
  (Begin/Commit/Rollback), Commit tự SaveChanges.
- **`IDomainEvent : INotification`**: kéo MediatR (package) vào Domain ⇒ vỡ purity. Giữ marker thuần + wrapper
  `DomainEventNotification<T>` ở Infrastructure.
- **Bảng entity mẫu trong schema production** để test: chọn `TestDbContext : AppDbContext` (assembly test, EnsureCreated) —
  giữ schema prod sạch; migration test riêng dùng `AppDbContext` thật. (Người dùng chọn "Clean prod schema".)
- **Skip integration test khi thiếu Docker** (SkippableFact): không thêm dependency — integration là gate thật, Docker
  bắt buộc (CI ubuntu có sẵn). (Người dùng chọn "Require Docker".)
- **Outbox bền vững / handler tái nhập / bảng idempotency dùng thật**: defer (idempotency nền chỉ ghi chú, dùng ở 31/37);
  Redis (12), Config Service (21), bảng nghiệp vụ (19+). Không mở rộng ngoài scope.
- **EFCore.NamingConventions** cho snake_case tự động: không thêm package — snake_case tường minh trong config (ADR-010,
  giảm bề mặt dependency).

Liên quan: ADR-003 (Clean Architecture/DIP), ADR-007 (save/transaction/schema version), ADR-010 (CPM — thêm
`Microsoft.EntityFrameworkCore.Design`/`.Relational` pin 9.0.10 + `Testcontainers.PostgreSql`). Dùng lại nền Phase 09
[[0007-domain-foundation-standardized]] (`AggregateRoot`/`IDomainEvent`/`IClock`) + Phase 10
[[0008-application-pipeline-standardized]] (`IUnitOfWork`/`IRepository`/`TransactionBehavior`). Canonical:
`docs/backend/infrastructure.md` §1.1/§5.1. Kế tiếp: Phase 12 (Redis cache + `ICacheService`).
