# Cross-Cutting Concerns (Backend)

> Auth/JWT, logging, monitoring, health checks, background jobs, security. Chuẩn hoá qua pipeline behaviors + middleware, không rải rác trong handler.

---

## 1. Authentication & Authorization

| Chủ đề | Thiết kế |
|---|---|
| Cơ chế | **JWT** bearer token (ADR-008) |
| Guest-first | Tạo tài khoản guest → token; link account (Google/Apple/email) Post-MVP (`../mvp/10` BE3) |
| Authorization | Policy-based; kiểm quyền ở Api + kiểm sở hữu tài nguyên ở handler (vd hero thuộc người chơi) |
| Token | Access token ngắn hạn + refresh; lưu phụ trợ ở Redis nếu cần thu hồi |
| Server-authoritative | Mọi hành động nhạy cảm kiểm quyền + kiểm nghiệp vụ server-side (ADR-007/011) |

---

## 2. Logging (structured)
- Serilog, log có cấu trúc (JSON), correlation id theo request.
- LoggingBehavior log command/query + thời gian xử lý.
- **Không** log dữ liệu nhạy cảm (token, PII).
- Mức log: Debug (dev), Information (nghiệp vụ chính), Warning/Error (bất thường).

---

## 2.5 Application pipeline behaviors (Phase 10 — đã đóng)

> Cross-cutting của tầng Application nằm ở **MediatR pipeline behaviors** (ADR-003), **không** rải trong
> handler. Mọi command/query đi qua `IMediator.Send` tự hưởng pipeline; handler chỉ điều phối nghiệp vụ
> (thin handler). Nguồn: `GameTeam.Application/Behaviors/`, đăng ký ở `AddApplication`.

### Thứ tự thực thi (cố định — có test chứng minh)

**`Logging → Validation → Transaction → Caching`** (đăng ký `AddOpenBehavior` theo đúng thứ tự; đầu tiên =
ngoài cùng). Lý do: Logging bao trọn thời gian (kể cả validation); Validation chặn **trước** khi mở
transaction; Transaction chỉ bao command ghi; Caching chỉ bao query đọc. Command và query dùng marker rời
nhau nên Transaction/Caching không bao giờ lồng nhau. `PipelineOrderTests` kiểm chứng chuỗi thực tế
(`[log:before, validate, tx:begin, handler, tx:commit, log:after]` cho command;
`[log:before, validate, cache:get, handler, cache:set, log:after]` cho query — query **không** vào transaction).

### 4 behaviors

| Behavior | Trách nhiệm | Ràng buộc / Marker |
|---|---|---|
| `LoggingBehavior` | Log tên request + elapsed (ms) + outcome (`Success`/`Failure(CODE)`/`Completed`). Chỉ log **tên kiểu request** — KHÔNG serialize body ⇒ không rò token/PII. | Áp cho **mọi** request (command & query). |
| `ValidationBehavior` | Gom `IValidator<TRequest>`, chạy **trước** handler; fail ⇒ short-circuit trả `Result` lỗi (code `VALIDATION_FAILED`, gộp `{Property}: {Message}`). Handler **không** chạy; **không** ném exception thô lên API. Không validator ⇒ đi thẳng. | `where TResponse : Result`. |
| `TransactionBehavior` | Bao command ghi trong `IUnitOfWork`: begin → handler → **commit** nếu `Result` thành công, **rollback** nếu `Result` lỗi hoặc ném exception (rồi rethrow). Atomic (ADR-007). | `where TRequest : ITransactionalRequest, TResponse : Result`. Query (không marker) **không** vào transaction. |
| `CachingBehavior` | Query có marker: đọc cache (hit ⇒ trả cache, **không** gọi handler), miss ⇒ chạy handler, **chỉ** ghi cache khi `Result` thành công, theo TTL. | `where TRequest : ICacheableQuery, TResponse : class`. |

### Marker interfaces (opt-in tường minh)

- `ITransactionalRequest` — command tự đánh dấu cần transaction. **Không** suy ra "command" từ tên; query
  không mang marker ⇒ không bao giờ vào transaction.
- `ICacheableQuery` — query khai báo `CacheKey` (phần tham số) + `CacheTtl`.

### Quy ước cache key

`"{RequestTypeName}:{ICacheableQuery.CacheKey}:cfg{IConfigProvider.CurrentVersion.Bundle}"`
= **tên query + tham số + config version**. Config version (bundle) khiến rollout cấu hình tự vô hiệu
cache cũ; tên + tham số chống va chạm.

> **Redis backend (Phase 12 — đã đóng):** `ICacheService` hiện thực trên **Redis** (`RedisCacheService`,
> StackExchange.Redis) ở Infrastructure — `CachingBehavior` chạy **thật** với Redis (query lần 2 = cache hit).
> `RedisCacheService` thêm tiền tố namespace `{env}:cache:` trước key trên (quy ước
> `{env}:{domain}:{name}:{configVersion?}`) và round-trip nguyên `Result<T>` (converter STJ). **Graceful
> degradation bắt buộc:** Redis down ⇒ Get miss / Set-Remove bỏ qua + log warning, request vẫn phục vụ —
> cache KHÔNG là điểm chết đơn. Port có thêm `RemoveAsync` (evict theo key, idempotent, degrade an toàn).
> Chi tiết: `infrastructure.md` §2.1.

### Ports (DIP — khai báo ở Application, hiện thực ở Infrastructure)

| Port | Vị trí | Hiện thực (phase) |
|---|---|---|
| `IUnitOfWork` | `Application/Abstractions/Persistence` | EF Core — **phase 11** |
| `IRepository<TEntity, TId>` | `Application/Abstractions/Persistence` (tối giản: `GetByIdAsync`+`AddAsync`, đặc tả feature 18+) | EF Core — **phase 11** |
| `ICacheService` | `Application/Abstractions/Caching` | Redis — **phase 12 (đã đóng)**: `RedisCacheService` (StackExchange.Redis) |
| `IConfigProvider` | `Application/Abstractions/Configuration` (Phase 10 chỉ dùng `CurrentVersion`) | Config Service — **phase 21** |
| `IClock` | `Domain/Common` (dùng lại Phase 09) | `SystemClock` (Infrastructure) — đã có adapter tối giản |

> **Ranh giới DIP:** `GameTeam.Application` **không** ref `GameTeam.Infrastructure`/`GameTeam.Api`
> (`ArchitectureTests.Application_should_not_depend_on_infrastructure_or_api`). `AddApplication` chỉ đăng ký
> MediatR + FluentValidation + 4 behaviors; **không** đăng ký hiện thực port (composition root = Api).

### Chưa thuộc phase này (defer)

`IdempotencyBehavior` (command nhạy cảm claim/summon), hiện thực repository/cache/config thật (11–12/21),
endpoint gọi MediatR (13). Mọi command/query tương lai **phải** đi qua pipeline — **không** nhét cross-cutting
vào handler.

## 3. Monitoring & Observability
| Trụ cột | Công cụ/hướng |
|---|---|
| Metrics | Prometheus-style metrics (request rate, latency, error rate, sim time) |
| Tracing | OpenTelemetry (Post-bootstrap) cho luồng quan trọng |
| Logging | Tập trung (Seq/ELK/cloud) |
| Alerting | Ngưỡng lỗi/latency (`../deployment/release-operations.md`) |
| Business telemetry | Sự kiện game (retention/funnel/source-sink) — `../liveops/`, `../mvp/09` LO2 |

## 4. Health Checks
- **Hiện tại (Phase 12):** `/health` minimal endpoint **ping Redis** (`PingAsync`, timeout ngắn) → `HealthResponse`
  `{"status":"ok"}` khi Redis truy cập được, `{"status":"degraded"}` khi không — **luôn HTTP 200** (liveness;
  Redis down không làm API sập). Chưa dùng framework `Microsoft.Extensions.Diagnostics.HealthChecks`.
- **Đích (phase 13+):** `/health/live` (process sống), `/health/ready` (DB + Redis sẵn sàng) qua health-checks đầy đủ.
- Dùng cho orchestrator/deploy (`../deployment/`).

## 5. Background Jobs (tham chiếu)
- Chi tiết ở `infrastructure.md` §6; cross-cutting: job có logging/monitoring/idempotency như request.

## 6. Security (nguyên tắc)
| Rủi ro | Biện pháp |
|---|---|
| Gian lận currency/gacha/AFK | Server-authoritative + validate + idempotency (ADR-007/011) |
| Injection | EF parameterized; validate input (FluentValidation) |
| Abuse/DoS nhẹ | Rate-limit qua Redis |
| Secrets lộ | Secret manager, không commit (`../deployment/`) |
| Authz sai | Kiểm sở hữu tài nguyên ở handler; test authz |

> Áp dụng tư duy `security-awareness` cho mọi endpoint nhận input người dùng.

## 7. Resilience
- Timeout + retry (có backoff) cho phụ thuộc ngoài; idempotency để retry an toàn.
- Circuit breaker cho phụ thuộc không ổn định (Post-bootstrap).

## 8. Liên kết
- Networking/API: `api-and-versioning.md`, ADR-008
- Infrastructure: `infrastructure.md`
- Deploy/monitoring: `../deployment/release-operations.md`
- Testing: `../testing/backend-testing.md`
