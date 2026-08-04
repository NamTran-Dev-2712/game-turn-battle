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

- [ ] `ValidationBehavior`: gom FluentValidation validators, fail → trả `Result` lỗi (không throw thô lên API).
- [ ] `LoggingBehavior`: log request name + elapsed + kết quả (không log dữ liệu nhạy cảm).
- [ ] `TransactionBehavior`: chỉ bao command ghi (marker interface `ITransactionalRequest`), commit/rollback qua `IUnitOfWork`.
- [ ] `CachingBehavior`: query có `ICacheableQuery` → đọc/ghi cache theo key + TTL.
- [ ] Định nghĩa port: `IUnitOfWork`, `IRepository<T>`, `ICacheService`, `IConfigProvider` (khai báo).
- [ ] Đăng ký behaviors đúng thứ tự (Logging → Validation → Transaction → Caching hoặc theo `../backend/cross-cutting.md`).
- [ ] Command mẫu (`PingCommand`) + query mẫu (`GetServerTimeQuery` dùng IClock) + validators.
- [ ] Test: validation fail dừng trước handler; transaction rollback khi lỗi; cache hit không gọi handler.
- [ ] Cập nhật `../backend/cross-cutting.md`.

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

Đóng khi pipeline 4 behaviors + port + command/query mẫu có test, thứ tự đúng, Application thuần (không ref Infra).

---

## Liên kết
- [`../backend/cross-cutting.md`](../backend/cross-cutting.md) · [`../backend/domain-and-application.md`](../backend/domain-and-application.md) · [`../architecture/dependency-graph.md`](../architecture/dependency-graph.md)
- ADR: [`../adr/ADR-003-backend-architecture.md`](../adr/ADR-003-backend-architecture.md)
- Roadmap: [`README.md`](README.md) → kế: [`11-backend-efcore-postgres.md`](11-backend-efcore-postgres.md)
