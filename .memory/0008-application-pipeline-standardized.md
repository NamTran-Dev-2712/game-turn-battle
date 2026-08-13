# 0008 — Application pipeline standardized (Phase 10)

- Date: 2026-08-13
- Scope: workspace
- Status: Active

## Decision

`GameTeam.Application` có **pipeline MediatR** chuẩn hoá cross-cutting ở **`Behaviors/`** (handler mỏng, ADR-003):
4 behaviors `IPipelineBehavior<,>` đăng ký trong `AddApplication` theo thứ tự cố định (ngoài→trong)
**`Logging → Validation → Transaction → Caching`** (`AddOpenBehavior`, đầu tiên = ngoài cùng).

- **LoggingBehavior** (mọi request): log tên kiểu request + elapsed ms + outcome (`Success`/`Failure(CODE)`/
  `Completed`), **chỉ tên** — không serialize body ⇒ không rò token/PII. `ILogger<T>` + `Stopwatch`.
- **ValidationBehavior** (`where TResponse : Result`): gom `IValidator<TRequest>`, chạy **trước** handler; fail ⇒
  short-circuit trả `Result` lỗi (code **`VALIDATION_FAILED`**, `ValidationErrors.ToError` gộp `{Property}: {Message}`)
  qua reflection `Result.Failure`/`Failure<T>`. Handler **không** chạy, **không** ném exception lên API. Không
  validator ⇒ passthrough.
- **TransactionBehavior** (`where TRequest : ITransactionalRequest, TResponse : Result`): begin `IUnitOfWork` →
  handler → **commit** nếu `IsSuccess`, **rollback** nếu `Result` lỗi **hoặc** ném (rồi rethrow). Query (không
  marker) **không** vào transaction (đảm bảo bởi ràng buộc generic, không suy từ tên).
- **CachingBehavior** (`where TRequest : ICacheableQuery, TResponse : class`): key =
  `"{RequestTypeName}:{CacheKey}:cfg{IConfigProvider.CurrentVersion.Bundle}"` (tên+tham số+config version); hit ⇒
  trả cache **không** gọi handler; miss ⇒ handler, **chỉ** cache khi `Result` thành công, theo `CacheTtl`.

Ports (DIP, khai báo ở Application, Infrastructure hiện thực): `IUnitOfWork`, `IRepository<TEntity, TId>` (tối
giản `GetByIdAsync`+`AddAsync`, feature 18+ đặc tả), `ICacheService`, `IConfigProvider` (Phase 10 chỉ
`CurrentVersion`; Config Service = phase 21). `IClock` **dùng lại Domain** (Phase 09), không khai báo trùng.
Marker: `ITransactionalRequest`, `ICacheableQuery`. Command/query mẫu: `PingCommand` (transactional),
`GetServerTimeQuery` (cacheable, dùng `IClock`).

Verified (SDK 9, Windows): build Release sạch 0 warning; `dotnet test` **121 pass** (Application.Tests **25**);
`PipelineOrderTests` chứng minh **thứ tự thực tế** bằng recorder (`[log:before, validate, tx:begin, handler,
tx:commit, log:after]` command; `[log:before, validate, cache:get, handler, cache:set, log:after]` query — query
không có `tx:begin`); negative (đảo Transaction↔Validation ⇒ đỏ) đã revert; NetArchTest
`Application_should_not_depend_on_infrastructure_or_api` xanh; không drift `openapi.json`.

## Why

ADR-003: cross-cutting ở behaviors thay vì rải trong handler ⇒ feature sau (auth/save/gacha/combat) tự hưởng
validation/logging/transaction/caching nhất quán, handler mỏng dễ đọc/test. Đặt khung **trước** feature đầu (nhóm 2,
Phase 10) trước EF (11)/endpoint (13). **Thứ tự** là rủi ro thật của phase → cố định `Logging → Validation →
Transaction → Caching` (Logging bao trọn elapsed kể cả validation; Validation chặn trước khi mở transaction;
Transaction chỉ bao command ghi; Caching chỉ bao query) và **test hành vi** thứ tự (không chỉ tin thứ tự đăng ký).
Dùng lại **Result/Error/IClock** Phase 09 — không tạo abstraction thứ hai. Marker **tường minh**
(`ITransactionalRequest`/`ICacheableQuery`) thay vì suy từ tên ⇒ query không lỡ vào transaction (rủi ro phase).

## Not this

- **Suy "command" từ tên** để bật transaction: dễ sai (query lỡ vào transaction). Chọn **marker** tường minh +
  ràng buộc generic (`where TRequest : ITransactionalRequest`) ⇒ behavior chỉ đóng cho request có marker.
- **Result/Error/validation-error mới**: dùng lại Phase 09; ValidationBehavior build `Result` lỗi qua reflection
  `Result.Failure<T>` — không paradigm thứ hai (đúng scope prompt Phase 10).
- **Đăng ký hiện thực port trong `AddApplication`**: sai tầng (composition root = Api). Application chỉ đăng ký
  MediatR + FluentValidation + 4 behaviors; Infrastructure hiện thực port (11–12/21). Architecture test khoá
  Application không ref Infra/Api.
- **Hiện thực repository/cache/Config Service thật, endpoint, `IdempotencyBehavior`**: defer (phase 11–12/13/21/sau).
- **Deviation (nhỏ, cần thiết):** thêm `SystemClock : IClock` (Infrastructure) + đăng ký `AddInfrastructure` —
  adapter server-time tối giản để composition root resolve/validate handler mẫu `GetServerTimeQuery` (thiếu ⇒ Api
  không boot ở Development/ValidateOnBuild). Đúng ranh giới `IClock` doc đã dự liệu; **không** phải hiện thực
  repository/cache/EF/Redis.

Liên quan: ADR-003 (Clean Architecture + CQRS/MediatR), ADR-007 (transaction/atomic). Dùng lại nền Phase 09
[[0007-domain-foundation-standardized]] (`Result`/`IClock`) + khuôn architecture test
[[0003-shared-contracts-standardized]]. Canonical: `docs/backend/cross-cutting.md` §2.5. Kế tiếp: Phase 11 (EF
Core/PostgreSQL) hiện thực `IUnitOfWork`/`IRepository`.
