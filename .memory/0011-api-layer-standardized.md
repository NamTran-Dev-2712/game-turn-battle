# 0011 — API layer standardized (Phase 13)

- Date: 2026-08-14
- Scope: workspace
- Status: Active

## Decision

`GameTeam.Api` là **cổng HTTP chuẩn + composition root** (versioning, xử lý lỗi tập trung, Swagger, endpoint mẫu qua
MediatR). Convention dưới đây là **hợp đồng cho mọi feature endpoint** về sau.

- **API versioning:** `Asp.Versioning.Http` + `Asp.Versioning.Mvc.ApiExplorer` (**8.1.1** — nhánh 9.x không tồn tại,
  8.1.1 target net8.0 roll-forward net9). `AddApi` gọi `AddApiVersioning` (default **v1**, `AssumeDefaultVersionWhenUnspecified`,
  `UrlSegmentApiVersionReader`, `ReportApiVersions`) + `AddApiExplorer` (`GroupNameFormat="'v'VVV"`,
  `SubstituteApiVersionInUrl=true` ⇒ OpenAPI render path đã resolve `/api/v1/...`). Endpoint mới map vào **version set**:
  `app.NewApiVersionSet().HasApiVersion(new ApiVersion(1))...Build()` + `MapGroup("/api/v{version:apiVersion}").WithApiVersionSet(set)`
  + `.MapToApiVersion(1)`. `GameTeam.Contracts.Common.ApiVersions` vẫn là hằng số version.
- **Xử lý lỗi tập trung** (`GameTeam.Api/Http/`): endpoint KHÔNG tự map lỗi.
  - `ErrorHttpMapping` — MỘT bảng `Error.Code`→HTTP status: `VALIDATION_FAILED`→400 (reuse `Application.Common.ValidationErrors.Code`)
    + **quy ước hậu tố** (không chế code cụ thể) `*_NOT_FOUND`→404, `*_CONFLICT`→409, `UNAUTHENTICATED`/`*_UNAUTHORIZED`→401,
    `*_FORBIDDEN`→403, default 400.
  - `ApiResults` — `Result`/`Result<T>` (handler MediatR) → HTTP; success→200 (rỗng cho `Result`, body cho `Result<T>`),
    failure→**`ErrorEnvelope`** (status từ mapping); `traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier`.
  - `GlobalExceptionHandler : IExceptionHandler` + `app.UseExceptionHandler()` — unhandled → **500 `ErrorEnvelope`** code
    `INTERNAL_ERROR`, message an toàn, traceId; exception đầy đủ **chỉ log server-side** (cùng traceId), **KHÔNG lộ**
    stack/message/DB/nội bộ ra client. `AddProblemDetails()` = fallback framework.
- **Endpoint mẫu (qua MediatR):** `GET /api/v1/ping` (`PingCommand`; `?message=` rỗng ⇒ `VALIDATION_FAILED` ⇒ 400 —
  validation chạy trước TransactionBehavior nên không chạm DB) + `GET /api/v1/server-time` (`GetServerTimeQuery` + `IClock`).
  Flow: HTTP → `ISender.Send` → Application `Result` → `ApiResults` → HTTP; endpoint **mỏng**.
- **Swagger:** UI (`Swashbuckle.AspNetCore.SwaggerUI` **10.1.0**, dev-only) **chỉ render** OpenAPI first-party
  `/openapi/v1.json` — **KHÔNG SwaggerGen** (giữ single-source `shared/contracts/openapi.json` + drift guard + codegen Phase 08).
- **Composition root** (`Program.cs`): `AddApplication().AddInfrastructure(config).AddApi()`; `UseExceptionHandler()` sớm;
  `MapOpenApi()`; Swagger UI dev-only; `/health` giữ nguyên (không versioned); `public partial class Program` cho `WebApplicationFactory`.
- **Auth hook:** Phase 13 **chỉ chừa chỗ** (TODO Phase 18 ở pipeline + `AddApi`) — KHÔNG JWT thật/fake user/scheme thật.

**Bổ sung để đủ composition root:** thêm `DefaultConfigProvider : IConfigProvider` (`GameTeam.Infrastructure/Configuration/`,
config@v1 cố định) đăng ký trong `AddInfrastructure` — vì `CachingBehavior` cần `IConfigProvider` và server-time là
`ICacheableQuery` đầu tiên chạy qua HTTP (chưa endpoint nào exercise caching trước Phase 13). Placeholder tối thiểu, **Phase 21**
(Config Service) thay thế.

Verified (SDK 9.0.306, Windows + Docker Desktop): build Release **0 warning/0 error**; `dotnet test` **150 pass**
(Contracts 36, Domain 35, Application 26, **Api.IntegrationTests 31**, Infrastructure 22 [Testcontainers postgres16/redis7]).
Api.IntegrationTests mới: Ping (200 + empty→400 `VALIDATION_FAILED`), ServerTime (FixedClock deterministic), ErrorEnvelope
(top-level chỉ `error`; inner đúng `code`/`message`/`traceId`), ExceptionHandling (ThrowingClock→500 không lộ nội bộ),
SwaggerJson (200 + paths) — dùng `ApiTestFactory` swap port (no-op UoW/cache, FixedClock) chạy không cần infra thật; giữ
HealthEndpointTests + OpenApiContractTests xanh. Runtime (Postgres/Redis dev up): `/health` 200, `/api/v1/ping` 200 & 400,
`/api/v1/server-time` 200, `/openapi/v1.json` 200, `/swagger` 200, header `api-supported-versions: 1`. `openapi.json` +
`client/src/data/generated/server_time_response.gd` regenerate (additive, +64 dòng spec, drift guard).

## Why

ADR-008: API versioned, contract-first — cần cổng HTTP chuẩn (error envelope + versioning + swagger) trước feature endpoint
(auth Phase 18) để mọi endpoint sau đồng nhất. ADR-003 (Clean/DIP): endpoint mỏng, nghiệp vụ qua Application/MediatR; error
mapping tập trung một chỗ (rủi ro phase: mapping rải rác không nhất quán). Error contract Phase 05 §3 = MỌI lỗi dùng
`ErrorEnvelope` + an toàn sản xuất (không lộ nội bộ) — nên 500 cũng dùng `ErrorEnvelope`.

## Not this

- **500 dùng ProblemDetails** (như chữ phase ghi): tạo error shape THỨ HAI, lệch contract Phase 05 §3 + `OpenApiContractTests`
  (đã khoá `ErrorEnvelope`). Chọn **`ErrorEnvelope` cho mọi lỗi** (MỘT contract). (Người dùng chọn "ErrorEnvelope (unified)".)
- **Chuyển stub Phase 05 vào version set:** `/config/{version}` có route param `version` TRÙNG `{version:apiVersion}` của
  prefix ⇒ `RoutePatternFactory` vỡ khi sinh OpenAPI; đổi tên param sẽ phá path contract `/api/v1/config/{version}`. Giữ 3
  stub trên group **literal** `/api/v1`; phase sở hữu (18/21) chuyển vào version set khi reimplement.
- **Thay first-party OpenAPI bằng Swashbuckle SwaggerGen:** vỡ single-source `shared/contracts/openapi.json` + drift guard +
  codegen. Chỉ thêm **SwaggerUI** (UI) trỏ vào doc first-party. (Người dùng chọn "Swashbuckle SwaggerUI".)
- **Triển khai JWT/login/authorization thật:** thuộc **Phase 18** — Phase 13 chỉ chừa hook.
- **Đổi `/health` thành `/api/v1/health` / adopt HealthChecks framework:** ngoài scope; giữ nguyên convention Phase 12.
- **Rate limiting / anti-cheat / feature endpoint nghiệp vụ:** defer (phase 18/21/53).

Liên quan: ADR-008 (networking/versioning/error contract), ADR-003 (Clean/DIP), ADR-010 (CPM — thêm `Asp.Versioning.Http` +
`Asp.Versioning.Mvc.ApiExplorer` 8.1.1, `Swashbuckle.AspNetCore.SwaggerUI` 10.1.0). Dùng lại
[[0003-shared-contracts-standardized]] (`ErrorResponse`/`ErrorEnvelope`/`ApiVersions`, OpenAPI first-party),
[[0006-codegen-pipeline-standardized]] (regenerate client), [[0007-domain-foundation-standardized]] (`Result`/`Error`/`IClock`),
[[0008-application-pipeline-standardized]] (MediatR pipeline, `PingCommand`/`GetServerTimeQuery`, ports), 
[[0009-persistence-standardized]] + [[0010-redis-cache-standardized]] (`AddInfrastructure`). Canonical:
`docs/backend/api-and-versioning.md` §3.1/§4.5. Kế tiếp: Phase 14 (client EventBus/SceneRouter).
