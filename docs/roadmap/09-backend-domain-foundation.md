# 09 — Backend Domain foundation

> Mục đích: Dựng nền **Domain thuần** (không phụ thuộc framework): base types, `Result`, value object, `IClock`, domain event — bộ khối xây cho mọi nghiệp vụ backend.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 2 Backend Core Framework | P1 | S2 | F11 (nền) |

# Mục tiêu

Bổ sung `GameTeam.Domain` các primitive tái dùng: `Result`/`Result<T>` (error rõ ràng thay vì throw bừa), base `Entity`/`AggregateRoot`, `ValueObject`, `IClock` (chống `DateTime.Now`), cơ chế domain event nội bộ — tất cả **không ref** EF/HTTP/MediatR.

# Lý do

Clean Architecture (ADR-003): Domain là lõi thuần. Các phase nghiệp vụ (auth/save/gacha/combat…) cần sẵn base types nhất quán để không mỗi feature tự phát minh. `IClock` là điều kiện để không dùng wall-clock (Forbidden Pattern, cần cho server-time & determinism).

# Phụ thuộc

- **Trước:** 05 (enum/contracts), 02 (CI+architecture test).
- **Sau:** 10 (Application dùng Result/behaviors), 18–19 (auth/profile entity), 24/27/31 (combat/hero/currency dùng value object & clock).

# Phạm vi

- `Result`/`Result<T>` + danh mục lỗi domain (error code chuẩn).
- Base `Entity<TId>`, `AggregateRoot`, `ValueObject` (equality theo giá trị).
- `IClock` (port) + quy ước dùng server time.
- Domain event abstraction (raise & thu thập trong aggregate; dispatch do Application/Infra lo).
- Guard/invariant helpers (validate bất biến trong domain).

# Không thuộc phạm vi

- Entity nghiệp vụ cụ thể (Hero/Profile/Currency) — thuộc phase feature.
- Persistence/EF (phase 11).
- Pipeline/MediatR (phase 10).

# Deliverables

- Base types trong `GameTeam.Domain` (một public type/file).
- Test đơn vị cho `Result`, `ValueObject` equality, guard, domain-event collection.
- Ghi chú convention trong [`../backend/domain-and-application.md`](../backend/domain-and-application.md).

# Công việc cần thực hiện

- [ ] `Result`/`Result<T>`: success/failure, error code + message, không dùng cho luồng ngoại lệ thật sự.
- [ ] `Error` value (code, message) — mã lỗi ổn định để map API (phase 13).
- [ ] `Entity<TId>` (định danh), `AggregateRoot` (chứa domain events), `ValueObject` (equality by components).
- [ ] `IClock` với `UtcNow`; ghi rõ mọi thời gian nghiệp vụ dùng clock này (server-time).
- [ ] Domain event: interface + collection trong aggregate + `ClearDomainEvents`.
- [ ] Guard helpers (NotNull, Positive, InRange) ném/return lỗi domain nhất quán.
- [ ] Test đầy đủ; xác nhận Domain **không** ref package ngoài (thuần).
- [ ] Cập nhật `../backend/domain-and-application.md`.

# Tiêu chí hoàn thành

- `GameTeam.Domain` không có package reference (thuần) — CI architecture test xác nhận.
- Test: `Result` success/failure, `ValueObject` equality, guard, domain-event add/clear — pass.
- Build Release sạch (warnings-as-error).
- Không dùng `DateTime.Now` bất kỳ đâu trong Domain.

# Cách kiểm tra

- `dotnet build -c Release` + `dotnet test` (Domain.Tests).
- NetArchTest: Domain không ref Application/Infrastructure/framework.
- Grep xác nhận không có `DateTime.Now`/`DateTime.UtcNow` trực tiếp (chỉ qua `IClock`).

# Rủi ro

- **Result vs Exception dùng lẫn lộn** → quy ước: Result cho lỗi nghiệp vụ mong đợi, exception cho lỗi lập trình/hạ tầng.
- **Domain event dispatch sai chỗ** → domain chỉ raise; dispatch ở Application/Infra (phase 10/11).
- **Over-engineering base types** → chỉ thêm cái có người dùng ngay.

# Ghi chú

Bám [`../backend/domain-and-application.md`](../backend/domain-and-application.md) + ADR-003. `IClock` sẽ được Infrastructure hiện thực (server time) và inject — nền cho AFK/Energy/determinism.

# Technical Debt Review

- **Maintainability:** base types nhất quán giảm trùng lặp.
- **Scalability:** aggregate/domain-event chuẩn DDD cho phép mở rộng nghiệp vụ.
- **Testing:** primitive được test kỹ, giảm bug lan.
- **Security:** error code không lộ nội bộ; clock chống time-cheat.
- **Nợ:** dispatch domain event nối ở phase 10/11.

# Phase Review

Đóng khi base types + IClock + domain event có test, Domain thuần (architecture test xanh), không wall-clock.

---

## Liên kết
- [`../backend/domain-and-application.md`](../backend/domain-and-application.md) · [`../architecture/dependency-graph.md`](../architecture/dependency-graph.md)
- ADR: [`../adr/ADR-003-backend-architecture.md`](../adr/ADR-003-backend-architecture.md) · [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md)
- Roadmap: [`README.md`](README.md) → kế: [`10-backend-application-mediatr.md`](10-backend-application-mediatr.md)
