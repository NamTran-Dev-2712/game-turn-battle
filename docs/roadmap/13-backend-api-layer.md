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

- [ ] Cấu hình `Asp.Versioning` (URL segment `/api/v{version:apiVersion}`), default v1.
- [ ] Middleware/exception handler: map `Result` fail → status phù hợp (400/404/409…) + `ErrorResponse`; unhandled → 500 chuẩn ProblemDetails, log traceId.
- [ ] Bật Swagger (dev), cấu hình để OpenAPI khớp `shared/contracts` (phase 05).
- [ ] Endpoint mẫu `/api/v1/ping` (command) + `/api/v1/server-time` (query, IClock) qua MediatR.
- [ ] Hoàn thiện `AddApi` + composition root DI (Application+Infrastructure+Api) trong `Program.cs`.
- [ ] Chừa middleware authentication/authorization (no-op tới phase 18), ghi TODO.
- [ ] Integration test (WebApplicationFactory): route versioned, error envelope, `/health`, swagger json 200.
- [ ] Cập nhật `../backend/api-and-versioning.md`.

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

Đóng khi versioning + error envelope + swagger + endpoint mẫu chạy, integration test xanh, DI composition root hoàn chỉnh.

---

## Liên kết
- [`../backend/api-and-versioning.md`](../backend/api-and-versioning.md) · [`../backend/solution-structure.md`](../backend/solution-structure.md) · [`../architecture/overview.md`](../architecture/overview.md)
- ADR: [`../adr/ADR-008-networking.md`](../adr/ADR-008-networking.md) · [`../adr/ADR-003-backend-architecture.md`](../adr/ADR-003-backend-architecture.md)
- Roadmap: [`README.md`](README.md) → kế: [`14-client-eventbus-scenerouter.md`](14-client-eventbus-scenerouter.md)
