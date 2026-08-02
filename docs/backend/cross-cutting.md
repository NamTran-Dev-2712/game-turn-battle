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

## 3. Monitoring & Observability
| Trụ cột | Công cụ/hướng |
|---|---|
| Metrics | Prometheus-style metrics (request rate, latency, error rate, sim time) |
| Tracing | OpenTelemetry (Post-bootstrap) cho luồng quan trọng |
| Logging | Tập trung (Seq/ELK/cloud) |
| Alerting | Ngưỡng lỗi/latency (`../deployment/release-operations.md`) |
| Business telemetry | Sự kiện game (retention/funnel/source-sink) — `../liveops/`, `../mvp/09` LO2 |

## 4. Health Checks
- `/health/live` (process sống), `/health/ready` (DB + Redis sẵn sàng).
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
