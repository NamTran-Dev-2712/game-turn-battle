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

- [ ] Thêm `AppDbContext`, đăng ký Npgsql, đọc connection string qua options.
- [ ] Cấu hình EF theo `IEntityTypeConfiguration` (một file/entity), quy ước bảng `snake_case`.
- [ ] Hiện thực `IRepository<T>` + `IUnitOfWork` (BeginTransaction/Commit/Rollback qua EF).
- [ ] Override SaveChanges để **dispatch domain events** thu từ aggregate (sau khi persist, cùng transaction).
- [ ] Hiện thực `IClock` (UTC server) + đăng ký DI.
- [ ] Tạo migration khởi tạo (`dotnet ef migrations add Initial`) + trường schema version.
- [ ] Cập nhật `AddInfrastructure` đăng ký đầy đủ; bỏ stub.
- [ ] Integration test Testcontainers: ghi/đọc entity mẫu, transaction rollback, domain event bắn.
- [ ] Cập nhật [`../backend/infrastructure.md`](../backend/infrastructure.md).

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

---

## Liên kết
- [`../backend/infrastructure.md`](../backend/infrastructure.md) · [`../testing/backend-testing.md`](../testing/backend-testing.md)
- ADR: [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md) · [`../adr/ADR-003-backend-architecture.md`](../adr/ADR-003-backend-architecture.md) · [`../adr/ADR-010-dependency-management.md`](../adr/ADR-010-dependency-management.md)
- Roadmap: [`README.md`](README.md) → kế: [`12-backend-redis-cache.md`](12-backend-redis-cache.md)
