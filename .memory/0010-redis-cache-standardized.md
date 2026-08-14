# 0010 — Redis cache standardized (Phase 12)

- Date: 2026-08-14
- Scope: workspace
- Status: Active

## Decision

`GameTeam.Infrastructure` có nền **cache phân tán Redis** ở **`Caching/`** + **`Serialization/`** (StackExchange.Redis
**CHỈ** ở Infrastructure — consumer phụ thuộc port `ICacheService`, KHÔNG phụ thuộc StackExchange.Redis, không rò kiểu
Redis ra ngoài). Hiện thực port Phase 10 `ICacheService`:

- **`RedisCacheService`** (`Caching/RedisCacheService.cs`) hiện thực `ICacheService` (`GetAsync`/`SetAsync`/**`RemoveAsync`**):
  serialize JSON, TTL = **absolute expiry**, key namespaced qua `RedisCacheKey`. **Graceful degradation:** lỗi Redis
  (`RedisException`) hoặc entry hỏng (`JsonException`) ⇒ **log warning + degrade** (Get→miss/null ⇒ caller chạy nguồn thật;
  Set/Remove→bỏ qua), KHÔNG ném lên caller; lỗi lập trình (`ArgumentNullException`…) **vẫn ném** (không `catch (Exception)` mù).
- **`RedisCacheKey`** (`Caching/RedisCacheKey.cs`) chuẩn hoá key **tập trung** theo `{env}:{domain}:{name}:{configVersion?}`;
  cache query domain = `cache` ⇒ key đầy đủ `{env}:cache:{rawKey}` (rawKey do CachingBehavior dựng, đã gấp `cfg{version}`).
- **`ResultJsonConverterFactory`** + **`CacheSerialization`** (`Serialization/`): converter STJ cho `Result`/`Result<T>`
  (Phase 09 bất biến, ctor không public ⇒ STJ mặc định KHÔNG deserialize được). CachingBehavior cache nguyên `Result<T>`
  ⇒ bắt buộc. `JsonSerializerOptions` dùng chung, `MakeReadOnly(populateMissingResolver:true)` ⇒ deterministic. Giữ Domain
  sạch (không attribute JSON trong Domain).
- **DI** (`DependencyInjection.cs`): `AddInfrastructure` đăng ký **`IConnectionMultiplexer` singleton**
  (`AbortOnConnectFail=false` ⇒ boot không chặn/ném khi Redis down) + `ICacheService → RedisCacheService`. **Connection từ
  config** `ConnectionStrings:Redis` (env `ConnectionStrings__Redis`) — không hardcode host/port/password; thiếu ⇒ fail-fast.
- **Healthcheck** (`GameTeam.Api/Program.cs`): `/health` mở rộng ping Redis (`PingAsync`, timeout ngắn, catch-all) ⇒
  `HealthResponse` `{"status":"ok"}` khi truy cập được, `{"status":"degraded"}` khi không — **luôn HTTP 200** (liveness;
  full health checks = phase 13+). `appsettings.json` thêm `ConnectionStrings:Redis=localhost:6379`.

**Mở rộng port Phase 10 (được phép):** thêm `RemoveAsync(key, ct)` vào `ICacheService` (additive) — phase yêu cầu remove;
cập nhật test stub `RecordingCacheService`. `CachingBehavior` không dùng Remove (invalidation qua config-version key).

Verified (SDK 9.0.306, Windows + Docker Desktop 28.5.1): build Release **0 warning/0 error**; `dotnet test` **144 pass**
(Infrastructure.Tests **22** = smoke DI + Testcontainers `redis:7-alpine` [set/get incl. `Result<T>`, TTL hết hạn poll,
remove, down→degrade + log warning] + **CachingBehavior chạy thật với Redis** [query lần 2 cache hit, handler chạy đúng 1
lần] + serialization unit; Api.IntegrationTests **25** gồm `/health` degraded-when-Redis-down 200); architecture gate xanh
(Application ⊥ Infra); `openapi.json` không drift (thêm `RemoveAsync` vào port + Redis nội bộ không đổi HTTP contract).

## Why

ADR-005: backend cache đọc để giảm tải DB; bundle config bất biến `config@vN` ⇒ cache dài an toàn (key gắn config version).
ADR-003 (DIP): port ở Application, hiện thực ở Infrastructure ⇒ testable, đổi backend cache dễ. Redis **không** là điểm chết
đơn ⇒ graceful degradation bắt buộc (rủi ro phase: cache lỗi làm sập request). Serialize `Result<T>` cần converter vì
CachingBehavior cache nguyên response (`Result<T>` bất biến, STJ mặc định không dựng lại được) — đặt ở Infrastructure giữ
Domain sạch. Multiplexer singleton + `AbortOnConnectFail=false` ⇒ thread-safe reuse + boot không phụ thuộc Redis sống.

## Not this

- **Tạo abstraction cache thứ hai / bypass `ICacheService`**: sai — reuse port Phase 10; chỉ **mở rộng** thêm `RemoveAsync`.
- **Nhét `[JsonConstructor]`/attribute JSON vào Domain** để deserialize `Result`: vỡ Domain purity — dùng converter ở
  Infrastructure (`Serialization/`).
- **`catch (Exception)` mù trong cache**: che lỗi lập trình — chỉ catch `RedisException` (degrade) + `JsonException` (entry
  hỏng ⇒ miss); còn lại propagate.
- **Adopt `Microsoft.Extensions.Diagnostics.HealthChecks` framework** ngay: đổi hình dạng `/health` (vỡ contract
  `{"status":"ok"}` + test), chồng scope phase 13+. Chọn mở rộng minimal endpoint (ping → ok/degraded, giữ 200).
  (Người dùng chọn "Extend minimal /health endpoint".)
- **Skip integration test khi thiếu Docker**: Testcontainers Redis là gate thật (CI ubuntu có sẵn; local cần Docker).
- **Config Service / bundle publish / pub-sub / cache warming / advanced invalidation / leaderboard**: defer (phase 21/45,
  Post-MVP). Không mở rộng ngoài scope.

Liên quan: ADR-003 (Clean Architecture/DIP), ADR-005 (config versioned cache, immutable `config@vN`), ADR-010 (CPM — thêm
`Testcontainers.Redis` 4.0.0; `StackExchange.Redis` 2.8.24 đã có sẵn). Dùng lại nền Phase 09
[[0007-domain-foundation-standardized]] (`Result`/`Result<T>`/`Error`) + Phase 10 [[0008-application-pipeline-standardized]]
(`ICacheService`/`CachingBehavior`/`ICacheableQuery`/`IConfigProvider`) + Phase 11 [[0009-persistence-standardized]]
(`AddInfrastructure`, Testcontainers pattern). Canonical: `docs/backend/infrastructure.md` §2.1. Kế tiếp: Phase 13 (API layer
+ real handlers).
