# 0007 — Domain foundation standardized (Phase 09)

- Date: 2026-08-12
- Scope: workspace
- Status: Active

## Decision

`GameTeam.Domain` có nền **primitive tái dùng** ở **`Common/`** (một public type/file, **BCL-only**, csproj
**không có package reference** — NetArchTest canh): `Error(Code, Message)` (value bất biến, `SCREAMING_SNAKE_CASE`,
`Error.None`, không rò nội bộ), `Result`/`Result<T>` (lỗi nghiệp vụ mong đợi; `Result<T>.Value` ném
`InvalidOperationException` khi failure; bất biến success⇔`Error.None` — vi phạm là lỗi lập trình ⇒ ném),
`Entity<TId>` (equality theo định danh + kiểu runtime), `ValueObject` (equality theo `GetEqualityComponents()`),
`AggregateRoot<TId> : Entity<TId>` (sở hữu domain event: `RaiseDomainEvent`/`DomainEvents` read-only/`ClearDomainEvents`),
`IDomainEvent` (marker), `IClock` (`DateTimeOffset UtcNow`, ranh giới server-time), `Guard` (NotNull/Positive/InRange —
**ném** BCL argument exception, dùng `CallerArgumentExpression`). Domain **chỉ raise & collect** domain event; dispatch =
phase 10/11. Architecture test mới `Domain_should_not_depend_on_framework_packages` khoá tính thuần.

## Why

ADR-003: Domain là lõi thuần, các phase nghiệp vụ (auth/save/gacha/combat/hero/currency) cần base type nhất quán để
không mỗi feature tự phát minh. Đặt sớm (Phase 09, nhóm 2) trước Application (10)/EF (11). **Result vs Exception** là rủi
ro thật của phase → chốt ranh giới: `Result` cho lỗi nghiệp vụ *mong đợi*, exception/`Guard` cho lỗi lập trình/bất
biến/hạ tầng — **một** paradigm validate, Guard không trả Result (tránh xung đột). `IClock` đặt **trong Domain** (không
phải Application) vì Domain cần server-time cho invariant mà không được chạm wall-clock (Forbidden Pattern; cần cho test
tái lập & chống gian lận giờ); Application/Infrastructure tái dùng cùng interface. Verified (SDK 9.0.306, Windows):
build Release sạch (warnings-as-error, `src/` 0 warning), `dotnet test` **101 pass** (Domain.Tests **35**: Error/Result/
Entity/ValueObject/AggregateRoot add+clear+không-sửa-từ-ngoài/Guard/Clock), NetArchTest xanh, grep wall-clock trong
`server/src/GameTeam.Domain` = chỉ prose doc-comment, không call site.

## Not this

- **Guard trả `Result`**: biến guard-invariant thành luồng nghiệp vụ → hai paradigm validate song song, rườm rà cho
  invariant ở constructor. Chọn **ném** (invariant = lỗi lập trình). Người dùng xác nhận.
- **`DomainException` riêng**: thêm abstraction chưa có consumer (over-engineering phase cảnh báo). Dùng BCL
  `ArgumentNullException`/`ArgumentOutOfRangeException` — idiom .NET, BCL-only.
- **`IClock.UtcNow` = `DateTime`**: nhập nhằng `DateTimeKind`. Chọn **`DateTimeOffset`** (instant rõ ràng, hợp AFK/energy).
  Người dùng xác nhận.
- **`IClock` ở Application** (như liệt kê "Ports" cũ): Domain không ref được Application → không dùng được clock trong
  Domain. Đặt port ở tầng cần nó (Domain); đã đồng bộ `docs/backend/domain-and-application.md` §2.
- **Domain dispatch event** (MediatR/bus/notification): sai tầng — Domain chỉ raise/collect; dispatch phase 10/11.
- **Entity nghiệp vụ / persistence / audit field / EF annotation** trong nền: thuộc phase feature (18–19, 27, 31) & phase 11.

Liên quan: bám ADR-003 (Clean Architecture), ADR-007 (server-time/save), `docs/backend/domain-and-application.md`.
Khuôn architecture test tái dùng seed ở [[0003-shared-contracts-standardized]] (NetArchTest). Kế tiếp: Phase 10
(Application + MediatR pipeline behaviors) dùng `Result` trong behavior.
