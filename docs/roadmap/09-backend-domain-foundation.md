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

- [x] `Result`/`Result<T>`: success/failure, error code + message, không dùng cho luồng ngoại lệ thật sự. — `GameTeam.Domain/Common/Result.cs`, `ResultOfT.cs` (invariant success⇔`Error.None`; `Value` ném `InvalidOperationException` khi failure).
- [x] `Error` value (code, message) — mã lỗi ổn định để map API (phase 13). — `Common/Error.cs` (`sealed record`, `Error.None`).
- [x] `Entity<TId>` (định danh), `AggregateRoot` (chứa domain events), `ValueObject` (equality by components). — `Common/Entity.cs`, `AggregateRoot.cs` (`AggregateRoot<TId> : Entity<TId>`), `ValueObject.cs`.
- [x] `IClock` với `UtcNow`; ghi rõ mọi thời gian nghiệp vụ dùng clock này (server-time). — `Common/IClock.cs` (`DateTimeOffset UtcNow`).
- [x] Domain event: interface + collection trong aggregate + `ClearDomainEvents`. — `Common/IDomainEvent.cs` + `AggregateRoot` (`DomainEvents` read-only, `RaiseDomainEvent`, `ClearDomainEvents`).
- [x] Guard helpers (NotNull, Positive, InRange) ném/return lỗi domain nhất quán. — `Common/Guard.cs` (ném BCL argument exception; Result dành cho lỗi nghiệp vụ).
- [x] Test đầy đủ; xác nhận Domain **không** ref package ngoài (thuần). — `GameTeam.Domain.Tests` (35 pass) + NetArchTest `Domain_should_not_depend_on_framework_packages`; csproj không có PackageReference.
- [x] Cập nhật `../backend/domain-and-application.md`. — thêm mục "Foundation primitives (Phase 09)" + ranh giới Result/Exception, domain-event dispatch, server-time; ghi rõ `IClock` thuộc Domain.

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

**Kết luận: đủ điều kiện đóng (local PASS 2026-08-12).**

- **Đã hiện thực:** `GameTeam.Domain/Common/` — `Error`, `Result`/`Result<T>`, `Entity<TId>`, `ValueObject`,
  `AggregateRoot<TId>`, `IDomainEvent`, `IClock` (`DateTimeOffset UtcNow`), `Guard` (một public type/file, BCL-only).
- **Test:** `dotnet test -c Release` xanh — `GameTeam.Domain.Tests` **35 pass** (Error/Result/Entity/ValueObject/
  AggregateRoot(add/clear/không sửa được từ ngoài)/Guard/Clock); toàn solution **101 pass / 0 fail**.
- **Architecture:** NetArchTest `Domain_should_not_depend_on_framework_packages` (mới) + `Domain_should_not_depend_on_outer_layers`
  xanh; `GameTeam.Domain.csproj` **không có** PackageReference/ProjectReference.
- **Build Release** sạch, warnings-as-error (compiler) — 0 error, `src/` 0 warning.
- **Wall-clock:** grep `DateTime.Now/UtcNow`, `DateTimeOffset.Now/UtcNow` trong `server/src/GameTeam.Domain` → chỉ có
  **prose trong doc-comment** `IClock.cs` (cảnh báo), không có call site. `IClock.UtcNow` là ranh giới duy nhất.
- **Quyết định:** Guard **ném** BCL argument exception (không trả Result — tránh hai paradigm); `IClock.UtcNow` = `DateTimeOffset`.
  Chi tiết: `.memory/0007-domain-foundation-standardized.md`.
- **Ranh giới phase:** domain event chỉ raise/collect (dispatch = phase 10/11); không entity nghiệp vụ / persistence / MediatR.

---

## Liên kết
- [`../backend/domain-and-application.md`](../backend/domain-and-application.md) · [`../architecture/dependency-graph.md`](../architecture/dependency-graph.md)
- ADR: [`../adr/ADR-003-backend-architecture.md`](../adr/ADR-003-backend-architecture.md) · [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md)
- Roadmap: [`README.md`](README.md) → kế: [`10-backend-application-mediatr.md`](10-backend-application-mediatr.md)
