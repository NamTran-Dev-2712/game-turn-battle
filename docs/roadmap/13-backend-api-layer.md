# 13 — API layer (versioning, error handling, Swagger)

> Mục đích: Hoàn thiện `GameTeam.Api` thành cổng HTTP chuẩn: API versioning `/api/v1`, error handling nhất quán (map Result→HTTP), Swagger, endpoint convention — hiện thực contract phase 05.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 2 Backend Core Framework | P1 | S2 | F11 (nền) |

# Mục tiêu

API layer: versioning `/api/v{major}`, middleware error handling map `Result`/exception → `ErrorResponse` (contract phase 05), Swagger/OpenAPI, endpoint mẫu gọi MediatR, chuẩn xác thực (chừa chỗ JWT phase 18). Composition root DI hoàn chỉnh.

# Lý do

ADR-008: API versioned, contract-first. Cần cổng HTTP chuẩn (error envelope, versioning, swagger) trước khi feature endpoint (auth phase 18) xuất hiện, để mọi endpoint sau đồng nhất.

# Phụ thuộc

- **Trước:** 05 (contract/error envelope), 10 (MediatR), 11–12 (Infra).
- **Sau:** 18 (auth endpoint), 21 (config endpoint), mọi feature endpoint.

# Phạm vi

- API versioning (`Asp.Versioning`) `/api/v1`.
- Middleware xử lý lỗi: `Result` fail → HTTP status + `ErrorResponse` (code/message/traceId); exception chưa bắt → 500 chuẩn (không lộ stack).
- Swagger UI (dev only) + xuất OpenAPI (đồng bộ phase 05).
- Endpoint mẫu (`/api/v1/ping`, `/api/v1/server-time`) qua MediatR.
- Chuẩn hoá `AddApi`: versioning, controllers/minimal API, problem details, chừa authentication/authorization (bật phase 18).

# Không thuộc phạm vi

- JWT auth thật (phase 18).
- Feature endpoint nghiệp vụ.
- Rate limiting nâng cao / anti-cheat (phase 53).

# Deliverables

- API versioning + error middleware + Swagger hoạt động.
- Endpoint mẫu qua MediatR trả contract chuẩn.
- Integration test: versioning route, error envelope, swagger doc sinh ra.
- Cập nhật [`../backend/api-and-versioning.md`](../backend/api-and-versioning.md).

# Công việc cần thực hiện

- [x] Cấu hình `Asp.Versioning` (URL segment `/api/v{version:apiVersion}`), default v1. — `AddApiVersioning` + `AddApiExplorer(SubstituteApiVersionInUrl)` trong `AddApi`; version set + `MapGroup("/api/v{version:apiVersion}")` trong `Program.cs`; OpenAPI render `/api/v1/...`, header `api-supported-versions: 1` (verify runtime).
- [x] Middleware/exception handler: map `Result` fail → status phù hợp (400/404/409…) + `ErrorResponse`; unhandled → 500, log traceId. — `Http/ErrorHttpMapping` (bảng code→status tập trung) + `Http/ApiResults` (Result→HTTP + traceId) + `Http/GlobalExceptionHandler` (`IExceptionHandler`). **500 dùng `ErrorEnvelope`** (KHÔNG ProblemDetails) để giữ MỘT error contract — xem Deviation ở Phase Review.
- [x] Bật Swagger (dev), cấu hình để OpenAPI khớp `shared/contracts` (phase 05). — Swagger UI (`Swashbuckle.AspNetCore.SwaggerUI`, dev-only) render OpenAPI first-party `/openapi/v1.json`; KHÔNG SwaggerGen (giữ single-source). `SwaggerJsonTests` xanh; Swagger UI 200 (verify runtime).
- [x] Endpoint mẫu `/api/v1/ping` (command) + `/api/v1/server-time` (query, IClock) qua MediatR. — `GET /api/v1/ping` → `PingCommand`; `GET /api/v1/server-time` → `GetServerTimeQuery` + `IClock`. `PingEndpointTests`/`ServerTimeEndpointTests` xanh; runtime 200/400.
- [x] Hoàn thiện `AddApi` + composition root DI (Application+Infrastructure+Api) trong `Program.cs`. — `AddApplication().AddInfrastructure().AddApi()`; bổ sung `DefaultConfigProvider : IConfigProvider` (Infra) để composition root ĐỦ cho `CachingBehavior` (server-time là ICacheableQuery đầu tiên chạy qua HTTP) — Phase 21 thay bằng Config Service thật.
- [x] Chừa middleware authentication/authorization (no-op tới phase 18), ghi TODO. — Block TODO Phase 18 trong `Program.cs` (đúng vị trí pipeline) + `AddApi`; KHÔNG đăng ký scheme thật, KHÔNG fake user.
- [x] Integration test (WebApplicationFactory): route versioned, error envelope, `/health`, swagger json 200. — `Api.IntegrationTests` 31 test xanh (Ping/ServerTime/ErrorEnvelope/ExceptionHandling/SwaggerJson + Health/OpenApiContract cũ). `ApiTestFactory` swap ports (no-op UoW/cache, FixedClock) để chạy không cần Postgres/Redis thật.
- [x] Cập nhật `../backend/api-and-versioning.md`. — Thêm §3.1 (error→HTTP mapping, traceId, GlobalExceptionHandler) + §4.5 (Asp.Versioning, AddApi, endpoint mẫu, Swagger UI, auth hook Phase 18).

# Tiêu chí hoàn thành

- `/api/v1/ping` & `/server-time` trả contract chuẩn; sai input → `ErrorResponse` đúng status.
- Unhandled exception → 500 ProblemDetails, **không** lộ stack; có traceId.
- Swagger JSON sinh ra, khớp contract nền.
- Integration test xanh; `Program` vẫn expose `partial` cho test.

# Cách kiểm tra

- `dotnet test` (Api.IntegrationTests): ping/server-time/health/error/swagger.
- Chạy API local → mở Swagger UI (dev) → thử endpoint.
- Gửi request lỗi validation → nhận `ErrorResponse` đúng format + status.

# Rủi ro

- **Error mapping không nhất quán** → bảng ánh xạ error-code→HTTP status tập trung một chỗ.
- **Swagger drift với contract** → sinh từ cùng nguồn; CI kiểm (nối phase 05).
- **Lộ thông tin lỗi** → ProblemDetails chuẩn, ẩn chi tiết nội bộ ở môi trường non-dev.

# Ghi chú

Authentication/authorization bật ở phase 18 (JWT). Versioning & error envelope là hợp đồng cho mọi feature endpoint sau. Bám [`../backend/api-and-versioning.md`](../backend/api-and-versioning.md) + ADR-008.

# Technical Debt Review

- **Maintainability:** error handling & versioning tập trung; endpoint mỏng.
- **Scalability:** versioned API cho tiến hoá không phá client.
- **Testing:** integration test cổng HTTP là hợp đồng.
- **Security:** không lộ stack; nền cho authz phase 18/53.
- **Nợ:** auth, rate limit, anti-cheat ở phase 18/53.

# Phase Review

**Kết luận: ĐỦ ĐIỀU KIỆN ĐÓNG (local PASS 2026-08-14).** Versioning + error envelope + Swagger + endpoint mẫu chạy; integration test xanh; DI composition root hoàn chỉnh.

**Bảng audit:**

| Requirement | Implementation | Test/Verification | Status |
|---|---|---|---|
| API v1 versioning | `Asp.Versioning.Http` + `Mvc.ApiExplorer` 8.1.1; `AddApiVersioning`(default v1, `UrlSegmentApiVersionReader`)+`AddApiExplorer`(`SubstituteApiVersionInUrl`); version set + `MapGroup("/api/v{version:apiVersion}")` | OpenAPI render `/api/v1/ping`,`/api/v1/server-time`; runtime header `api-supported-versions: 1`; `PingEndpointTests` | PASS |
| Error envelope | `Http/ApiResults`→`ErrorEnvelope{error:{code,message,traceId}}`; reuse `Contracts.Common.ErrorResponse/ErrorEnvelope` (không chế mới) | `ErrorEnvelopeTests` (top-level chỉ `error`; inner đúng 3 field); runtime 400 | PASS |
| Error→HTTP mapping tập trung | `Http/ErrorHttpMapping` (dict `VALIDATION_FAILED`→400 + suffix convention `_NOT_FOUND`/`_CONFLICT`/`_UNAUTHORIZED`/`_FORBIDDEN`; default 400) | `PingEndpointTests` 400; unit-covered qua endpoint | PASS |
| Exception → 500 | `Http/GlobalExceptionHandler : IExceptionHandler` + `app.UseExceptionHandler()`; `ErrorEnvelope` code `INTERNAL_ERROR` | `ExceptionHandlingTests` (throwing IClock → 500, không lộ stack/message nội bộ) | PASS |
| TraceId | `Activity.Current?.Id ?? HttpContext.TraceIdentifier` trong `ApiResults`/handler | Tests assert `traceId` non-empty; runtime W3C id | PASS |
| Swagger | Swagger UI dev-only over first-party `/openapi/v1.json`; không SwaggerGen | `SwaggerJsonTests` 200 + paths; runtime `/swagger` 200 | PASS |
| Ping (MediatR) | `GET /api/v1/ping` → `PingCommand` qua `ISender` | `PingEndpointTests` 200/400; runtime | PASS |
| Server-time (MediatR+IClock) | `GET /api/v1/server-time` → `GetServerTimeQuery` + `IClock` | `ServerTimeEndpointTests` (FixedClock deterministic); runtime | PASS |
| Health | `/health` giữ nguyên (không versioned, không đổi) | `HealthEndpointTests` (ok + degraded) | PASS |
| DI composition root | `AddApplication().AddInfrastructure().AddApi()`; + `DefaultConfigProvider` (Infra) để đủ `CachingBehavior` | Build xanh; server-time 200 runtime | PASS |
| Auth hook | TODO Phase 18 (pipeline + AddApi); KHÔNG scheme thật/fake user | Code review; không có `AddAuthentication` thật | PASS |
| Integration tests | `Api.IntegrationTests` + `ApiTestFactory` (swap ports) | 31 test xanh; tổng solution 150 xanh | PASS |
| Documentation | `api-and-versioning.md` §3.1/§4.5 | Doc review | PASS |
| Vibe Code instructions | `CLAUDE.md` §4.6 (Phase 13 block), `.instructions/backend.md`, `.claude/agents/dotnet-backend.md`, `.memory/0011` | Doc review | PASS |

**Deviations có chủ đích:**
1. **500 dùng `ErrorEnvelope` thay vì ProblemDetails.** Lý do: `api-and-versioning.md` §3 (contract Phase 05) yêu cầu MỌI error dùng `ErrorEnvelope`, và `OpenApiContractTests` đã khoá điều đó; dùng ProblemDetails sẽ tạo error shape thứ hai. Giữ MỘT contract nhất quán, vẫn không lộ stack + có traceId (đủ tinh thần "500 chuẩn"). `AddProblemDetails()` vẫn đăng ký làm fallback framework.
2. **Stub Phase 05 (`auth/guest`,`profile`,`config/{version}`) giữ trên group literal `/api/v1`, KHÔNG vào version set.** Lý do kỹ thuật: `/config/{version}` có route param `version` TRÙNG với `{version:apiVersion}` của prefix version set ⇒ `RoutePatternFactory` vỡ khi sinh OpenAPI. Đổi tên param sẽ phá path contract `/api/v1/config/{version}`. Endpoint mới (ping/server-time) dùng version set; các phase sở hữu (18/21) sẽ chuyển stub vào version set khi reimplement.
3. **Thêm `DefaultConfigProvider : IConfigProvider` (Infra, config@v1 cố định).** Không có implementation `IConfigProvider` nào tồn tại (deferred Phase 21) nhưng `CachingBehavior` cần nó — server-time là ICacheableQuery đầu tiên chạy qua HTTP. Placeholder tối thiểu để composition root đủ; Phase 21 thay bằng Config Service thật (không đọc balance ở đây).

**Verify:** `dotnet build -c Release` 0 warning/0 error; `dotnet test` 150 xanh (Contracts 36, Domain 35, Application 26, Api.Integration 31, Infrastructure 22 — Testcontainers Postgres16/Redis7); runtime `/health`,`/api/v1/ping`(±validation),`/api/v1/server-time`,`/openapi/v1.json`,`/swagger` OK; `openapi.json` + `client/src/data/generated/server_time_response.gd` regenerate (additive, drift guard). Gate CI (codegen-check, validate-config, OpenAPI drift) = **CI-pending** cho tới khi Actions xanh.

---

## Liên kết
- [`../backend/api-and-versioning.md`](../backend/api-and-versioning.md) · [`../backend/solution-structure.md`](../backend/solution-structure.md) · [`../architecture/overview.md`](../architecture/overview.md)
- ADR: [`../adr/ADR-008-networking.md`](../adr/ADR-008-networking.md) · [`../adr/ADR-003-backend-architecture.md`](../adr/ADR-003-backend-architecture.md)
- Roadmap: [`README.md`](README.md) → kế: [`14-client-eventbus-scenerouter.md`](14-client-eventbus-scenerouter.md)
