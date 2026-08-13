# 10 — Application layer + MediatR pipeline behaviors

> Mục đích: Hoàn thiện lớp Application (CQRS qua MediatR) với **pipeline behaviors** (validation, logging, transaction, caching) làm khung xử lý cho mọi command/query nghiệp vụ.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 2 Backend Core Framework | P1 | S2 | F11 (nền) |

# Mục tiêu

Thêm vào `GameTeam.Application`: pipeline behaviors MediatR (ValidationBehavior dùng FluentValidation, LoggingBehavior, TransactionBehavior, CachingBehavior), port interfaces nền (repository, unit-of-work, cache, clock), và một command+query **mẫu** end-to-end qua pipeline.

# Lý do

ADR-003 quy định cross-cutting nằm ở pipeline behaviors thay vì rải trong handler. Đặt khung này trước khi feature đầu tiên (auth/save) viết handler, để mọi handler sau tự hưởng validation/logging/transaction nhất quán.

# Phụ thuộc

- **Trước:** 09 (Result/IClock/base), 05 (contracts).
- **Sau:** 11–12 (Infra hiện thực port), 13 (API gọi MediatR), mọi feature command/query.

# Phạm vi

- Behaviors: Validation (chặn trước handler), Logging (request/response + thời gian), Transaction (bao command ghi), Caching (query đọc cache theo key).
- Port interfaces trong Application: `IRepository`/`IUnitOfWork`, `ICacheService`, `IClock` (dùng lại từ Domain), `IConfigProvider` (khai báo, hiện thực ở phase 21).
- Command/query mẫu qua MediatR để chứng minh pipeline.
- Đăng ký behaviors trong `AddApplication` (thứ tự đúng).

# Không thuộc phạm vi

- Hiện thực repository/cache thật (phase 11–12).
- Endpoint (phase 13).
- Feature nghiệp vụ.

# Deliverables

- 4 behaviors + đăng ký thứ tự đúng trong `DependencyInjection`.
- Port interfaces nền trong Application.
- Command+query mẫu + test đi qua pipeline (validation fail, logging, transaction scope, cache hit/miss).
- Cập nhật [`../backend/cross-cutting.md`](../backend/cross-cutting.md).

# Công việc cần thực hiện

- [x] `ValidationBehavior`: gom FluentValidation validators, fail → trả `Result` lỗi (không throw thô lên API). — `Behaviors/ValidationBehavior.cs` + `ValidationErrors` (code `VALIDATION_FAILED`); `ValidationBehaviorTests` (fail→failure, handler không chạy, không ném) xanh.
- [x] `LoggingBehavior`: log request name + elapsed + kết quả (không log dữ liệu nhạy cảm). — `Behaviors/LoggingBehavior.cs` (chỉ log tên kiểu, không serialize body); `LoggingBehaviorTests` (name+elapsed+outcome; secret không rò) xanh.
- [x] `TransactionBehavior`: chỉ bao command ghi (marker interface `ITransactionalRequest`), commit/rollback qua `IUnitOfWork`. — `Behaviors/TransactionBehavior.cs`; `TransactionBehaviorTests` (commit/rollback-lỗi/rollback-exception+rethrow/query-không-begin) xanh.
- [x] `CachingBehavior`: query có `ICacheableQuery` → đọc/ghi cache theo key + TTL. — `Behaviors/CachingBehavior.cs`; `CachingBehaviorTests` (miss chạy+set TTL+key có cfg version; hit không gọi handler; fail không cache; non-cacheable bypass) xanh.
- [x] Định nghĩa port: `IUnitOfWork`, `IRepository<T>`, `ICacheService`, `IConfigProvider` (khai báo). — `Abstractions/{Persistence,Caching,Configuration}`; `IRepository<TEntity, TId>` (id typed); `IClock` dùng lại Domain.
- [x] Đăng ký behaviors đúng thứ tự (Logging → Validation → Transaction → Caching). — `AddApplication` `AddOpenBehavior` đúng thứ tự; `PipelineOrderTests` chứng minh chuỗi thực tế (negative: đảo Transaction↔Validation ⇒ test đỏ, đã revert).
- [x] Command mẫu (`PingCommand`) + query mẫu (`GetServerTimeQuery` dùng IClock) + validators. — `Features/Diagnostics/{Commands,Queries}`; `PingCommandTests`/`GetServerTimeQueryTests` end-to-end qua MediatR xanh.
- [x] Test: validation fail dừng trước handler; transaction rollback khi lỗi; cache hit không gọi handler. — có đủ (xem trên); tổng **Application.Tests 25 pass**, solution **121 pass**.
- [x] Cập nhật `../backend/cross-cutting.md`. — thêm §2.5 (4 behaviors, thứ tự, marker, cache key, ports, DIP); đồng bộ `domain-and-application.md`, `CLAUDE.md` §4.6, `.instructions/backend.md`, agent, `.memory/0008`.

# Tiêu chí hoàn thành

- 4 behaviors hoạt động, đăng ký đúng thứ tự (test chứng minh thứ tự).
- Command mẫu qua pipeline: validate → (transaction) → handler; query mẫu qua cache.
- Application **không** ref Infrastructure (architecture test xanh).
- Validation fail trả về Result lỗi chuẩn, không exception rò lên.

# Cách kiểm tra

- `dotnet test` (Application.Tests): case validation-fail, transaction-rollback, cache-hit.
- NetArchTest: Application không ref Infra.
- Chạy command mẫu qua MediatR trong test, kiểm log/elapsed xuất hiện.

# Rủi ro

- **Thứ tự behavior sai** (transaction bao cả validation) → cố định thứ tự + test.
- **Transaction bao cả query đọc** → chỉ bao `ITransactionalRequest`.
- **Cache key va chạm** → quy ước key gồm tên query + tham số + config version.

# Ghi chú

Port ở Application, hiện thực ở Infrastructure (DIP). `IConfigProvider` khai báo ở đây, phase 21 hiện thực (Config Service). Bám [`../backend/cross-cutting.md`](../backend/cross-cutting.md) + ADR-003.

# Technical Debt Review

- **Maintainability:** cross-cutting tập trung; handler mỏng, dễ đọc.
- **Scalability:** thêm feature = thêm handler, hưởng pipeline sẵn.
- **Testing:** behavior test một lần, feature khỏi lặp.
- **Security:** logging loại dữ liệu nhạy cảm; validation chặn input xấu.
- **Nợ:** hiện thực port (11–12); caching phân tán (Redis) ở phase 12.

# Phase Review

**Đóng (local PASS 2026-08-13).** Pipeline 4 behaviors ở `GameTeam.Application/Behaviors/` (`Logging → Validation
→ Transaction → Caching`, đăng ký `AddOpenBehavior` đúng thứ tự trong `AddApplication`) + ports nền
(`IUnitOfWork`/`IRepository<TEntity,TId>`/`ICacheService`/`IConfigProvider`; `IClock` dùng lại Domain) +
marker `ITransactionalRequest`/`ICacheableQuery` + command/query mẫu (`PingCommand`, `GetServerTimeQuery`).
Validation fail → `Result` lỗi chuẩn (`VALIDATION_FAILED`), không ném thô; Transaction chỉ bao
`ITransactionalRequest` (query không vào transaction); Caching chỉ bao `ICacheableQuery`, key = tên+tham số+config
version; Logging không log body. **`PipelineOrderTests`** chứng minh chuỗi thực tế (không chỉ tin thứ tự đăng ký);
negative check (đảo Transaction↔Validation ⇒ đỏ) đã revert. Architecture test
`Application_should_not_depend_on_infrastructure_or_api` xanh; `GameTeam.Application` chỉ ref Domain+Contracts.
Verify: `dotnet build -c Release` sạch (0 warning), `dotnet test` **121 pass** (Application.Tests **25**), không
drift `shared/contracts/openapi.json`.

**Ghi chú deviation (nhỏ):** thêm `SystemClock : IClock` (Infrastructure) + đăng ký trong `AddInfrastructure` —
adapter server-time tối giản cần để composition root (Api) resolve/validate handler mẫu `GetServerTimeQuery`
(nếu thiếu, app không boot ở Development/ValidateOnBuild). Đây là ranh giới server-time đã được `IClock` doc dự
liệu ("Infrastructure hiện thực & inject"); repository/cache/config **thật** vẫn defer (phase 11–12/21). Deferred:
`IdempotencyBehavior`, hiện thực repository/cache/Config Service, endpoint gọi MediatR (phase 13).

---

## Liên kết
- [`../backend/cross-cutting.md`](../backend/cross-cutting.md) · [`../backend/domain-and-application.md`](../backend/domain-and-application.md) · [`../architecture/dependency-graph.md`](../architecture/dependency-graph.md)
- ADR: [`../adr/ADR-003-backend-architecture.md`](../adr/ADR-003-backend-architecture.md)
- Roadmap: [`README.md`](README.md) → kế: [`11-backend-efcore-postgres.md`](11-backend-efcore-postgres.md)
