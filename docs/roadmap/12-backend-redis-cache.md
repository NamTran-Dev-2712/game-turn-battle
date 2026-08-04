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

- [ ] Hiện thực `RedisCacheService` (get/set/remove, serialize JSON, TTL, key namespace).
- [ ] Quy ước key: `{env}:{domain}:{name}:{configVersion?}`.
- [ ] Xử lý Redis lỗi: log + degrade (cache miss → nguồn thật), không ném lên người dùng.
- [ ] Đăng ký DI trong `AddInfrastructure`; connection từ config.
- [ ] Mở rộng healthcheck: ping Redis.
- [ ] Integration test Testcontainers Redis: set/get, TTL hết hạn, remove, down→degrade.
- [ ] Cập nhật [`../backend/infrastructure.md`](../backend/infrastructure.md) + [`../backend/cross-cutting.md`](../backend/cross-cutting.md).

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

---

## Liên kết
- [`../backend/infrastructure.md`](../backend/infrastructure.md) · [`../backend/cross-cutting.md`](../backend/cross-cutting.md)
- ADR: [`../adr/ADR-005-configuration-strategy.md`](../adr/ADR-005-configuration-strategy.md) · [`../adr/ADR-003-backend-architecture.md`](../adr/ADR-003-backend-architecture.md)
- Roadmap: [`README.md`](README.md) → kế: [`13-backend-api-layer.md`](13-backend-api-layer.md)
