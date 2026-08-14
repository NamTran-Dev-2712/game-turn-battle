# 12 — Infrastructure: Redis cache + provider abstractions

> Mục đích: Hiện thực `ICacheService` bằng Redis (StackExchange.Redis) cho CachingBehavior và phân phối config/versioned data, tách qua abstraction để test được.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 2 Backend Core Framework | P1 | S2 | F11 (nền) |

# Mục tiêu

`GameTeam.Infrastructure` hiện thực `ICacheService` (get/set/remove theo key+TTL) trên Redis, connection từ config (`ConnectionStrings__Redis`), phục vụ CachingBehavior (phase 10) và cache bundle config (phase 21).

# Lý do

ADR-005: client cache config theo version; backend cache đọc để giảm tải DB. Redis là hạ tầng cache/phân tán. Đặt nền cache trước Config Service (phase 21) và trước các query đọc nặng (leaderboard phase 45).

# Phụ thuộc

- **Trước:** 10 (ICacheService port + CachingBehavior), 04 (Redis dev), 11 (DbContext).
- **Sau:** 21 (config bundle cache), 45 (leaderboard), query đọc nhiều.

# Phạm vi

- Hiện thực `ICacheService` trên StackExchange.Redis (serialize JSON, TTL, key namespace).
- Quy ước key (prefix theo domain + version), invalidation cơ bản.
- Fallback graceful khi Redis lỗi (degrade sang DB, không sập).
- Đăng ký DI + healthcheck Redis.

# Không thuộc phạm vi

- Config Service publish bundle (phase 21).
- Pub/Sub realtime (SignalR — Post-MVP).
- Cache warming nâng cao.

# Deliverables

- `RedisCacheService : ICacheService` + đăng ký DI.
- Quy ước key + TTL tài liệu hoá.
- Integration test (Testcontainers Redis): set/get/expire/remove; fallback khi Redis down.
- Healthcheck Redis gắn `/health` (mở rộng).

# Công việc cần thực hiện

- [x] Hiện thực `RedisCacheService` (get/set/remove, serialize JSON, TTL, key namespace). — `GameTeam.Infrastructure/Caching/RedisCacheService.cs` (`ICacheService` mở rộng thêm `RemoveAsync`); serialize `CacheSerialization.Options` (STJ + converter `Result`/`Result<T>`); TTL absolute; key qua `RedisCacheKey`. Verify: Testcontainers set/get/remove xanh.
- [x] Quy ước key: `{env}:{domain}:{name}:{configVersion?}`. — `RedisCacheKey.Compose/ForCacheEntry` tập trung; cache query = `{env}:cache:{rawKey}` (rawKey đã gấp `cfg{version}`). Ví dụ `dev:cache:GetServerTimeQuery:server-time:cfg0`.
- [x] Xử lý Redis lỗi: log + degrade (cache miss → nguồn thật), không ném lên người dùng. — catch `RedisException` (Get→null, Set/Remove→bỏ qua) + `JsonException` (entry hỏng→miss) + log warning; lỗi lập trình vẫn ném. Verify: `RedisCacheServiceDegradeTests` (3) + `/health` degraded-when-down xanh.
- [x] Đăng ký DI trong `AddInfrastructure`; connection từ config. — `IConnectionMultiplexer` singleton (`AbortOnConnectFail=false`) + `ICacheService → RedisCacheService`; `ConnectionStrings:Redis` (env `ConnectionStrings__Redis`), fail-fast khi thiếu. Verify: `SmokeTests` (đăng ký + fail-fast Redis).
- [x] Mở rộng healthcheck: ping Redis. — `/health` (Program.cs) ping `PingAsync` timeout ngắn ⇒ `ok`/`degraded`, luôn 200. Verify: `Health_reports_degraded_when_redis_unreachable`.
- [x] Integration test Testcontainers Redis: set/get, TTL hết hạn, remove, down→degrade. — `GameTeam.Infrastructure.Tests/Caching/` (`redis:7-alpine`): set/get (incl. `Result<T>`), TTL poll expiry, remove, degrade; + `CachingBehaviorRedisIntegrationTests` (query lần 2 cache hit). Verify: 22 Infra tests xanh trên Docker Desktop 28.5.1.
- [x] Cập nhật [`../backend/infrastructure.md`](../backend/infrastructure.md) + [`../backend/cross-cutting.md`](../backend/cross-cutting.md). — infrastructure.md §2.1 (nền cache đã chốt) + cross-cutting.md §2.5 (Redis backend + `RemoveAsync`) / §4 (healthcheck ping Redis). Doc-sync thêm: CLAUDE.md §4.6, `.instructions/backend.md`, `.claude/agents/dotnet-backend.md`, `.memory/0010`.

# Tiêu chí hoàn thành

- CachingBehavior (phase 10) chạy thật với Redis: query lần 2 cache hit.
- Integration test Redis xanh (set/get/expire/remove/degrade).
- Redis down → API vẫn phục vụ (degrade), có log cảnh báo.
- Healthcheck báo trạng thái Redis.

# Cách kiểm tra

- `scripts/dev/up` (Redis) → chạy query cacheable 2 lần, lần 2 không vào handler/DB.
- `dotnet test` (Infrastructure.Tests) Testcontainers Redis.
- Tắt Redis → gọi API → vẫn 200 (degrade), log cảnh báo.

# Rủi ro

- **Redis là điểm chết đơn** → degrade graceful; không để cache lỗi làm sập request.
- **Serialize không nhất quán** → cố định serializer + version key theo config version (tránh dữ liệu cũ).
- **Key va chạm/không invalidation** → namespace + gắn config version vào key.

# Ghi chú

Cache bundle config sẽ dùng service này (phase 21) với key theo `config@vN` (immutable → an toàn cache dài). Bám ADR-005 + [`../backend/cross-cutting.md`](../backend/cross-cutting.md).

# Technical Debt Review

- **Maintainability:** cache sau abstraction, đổi backend cache dễ.
- **Scalability:** Redis chịu tải đọc; giảm áp lực Postgres.
- **Testing:** Testcontainers Redis kiểm hành vi thật.
- **Security:** không cache dữ liệu nhạy cảm không mã hoá; TTL hợp lý.
- **Nợ:** invalidation nâng cao & pub/sub để LiveOps/Post-MVP.

# Phase Review

Đóng khi Redis cache hiện thực + CachingBehavior chạy thật + degrade graceful + integration test xanh + healthcheck.

**Kết luận (2026-08-14): đủ điều kiện đóng.** Toàn bộ `# Công việc cần thực hiện` `[x]` với evidence từ run thật;
`# Tiêu chí hoàn thành` đạt:
- **CachingBehavior chạy thật với Redis, query lần 2 cache hit** — `CachingBehaviorRedisIntegrationTests` (handler chạy đúng 1 lần qua 2 query giống nhau, Redis Testcontainers).
- **Integration test Redis xanh (set/get/expire/remove/degrade)** — `RedisCacheServiceTests` (6) + `RedisCacheServiceDegradeTests` (3) + serialization unit (4).
- **Redis down → API vẫn phục vụ (degrade) + log cảnh báo** — degrade tests (log warning) + `Health_reports_degraded_when_redis_unreachable` (HTTP 200 + `degraded`).
- **Healthcheck báo trạng thái Redis** — `/health` ping Redis (`ok`/`degraded`).

Verified: build Release **0 warning/0 error**; `dotnet test server/GameTeam.sln` **144 pass** (Domain 35, Contracts 36,
Application 26, Infrastructure 22, Api.IntegrationTests 25); architecture gate (Application ⊥ Infra) xanh; `openapi.json`
không drift. Không TODO/blocker; không code ngoài scope (Config Service phase 21, pub/sub, cache warming, leaderboard đều
để nguyên placeholder). Decision log `.memory/0010-redis-cache-standardized.md`. Kế tiếp: Phase 13 (API layer).

---

## Liên kết
- [`../backend/infrastructure.md`](../backend/infrastructure.md) · [`../backend/cross-cutting.md`](../backend/cross-cutting.md)
- ADR: [`../adr/ADR-005-configuration-strategy.md`](../adr/ADR-005-configuration-strategy.md) · [`../adr/ADR-003-backend-architecture.md`](../adr/ADR-003-backend-architecture.md)
- Roadmap: [`README.md`](README.md) → kế: [`13-backend-api-layer.md`](13-backend-api-layer.md)
