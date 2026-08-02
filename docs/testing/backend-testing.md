# Backend Testing (.NET)

> Unit, integration, architecture test cho backend. Nền: `README.md`. Áp dụng cho Clean Architecture (ADR-003).

---

## 1. Công cụ (đề xuất)
| Mục | Công cụ |
|---|---|
| Test framework | xUnit |
| Assertion | FluentAssertions |
| Mock | NSubstitute / Moq |
| Integration DB | Testcontainers (PostgreSQL/Redis) hoặc DB test riêng |
| Architecture test | NetArchTest |

## 2. Theo tầng

| Tầng | Loại test | Trọng tâm |
|---|---|---|
| Domain | Unit thuần | Invariant, business rule, value object, domain service (vd pity, AFK policy) |
| Application | Unit (mock ports) | Handler đúng luồng; behaviors (validation/idempotency/transaction) |
| Infrastructure | Integration | Repository + EF + PostgreSQL; cache Redis; config provider |
| Api | Integration | Endpoint + auth + serialization (WebApplicationFactory) |

## 3. Test trọng yếu

| Test | Vì sao |
|---|---|
| **Golden combat vector** (server sim) | Khớp client & đặc tả (ADR-011) |
| Gacha rate/pity | Server-authoritative, đúng phân phối & pity |
| AFK claim | Đúng theo server time + cap; **idempotent** (không double) |
| Currency transaction | Atomic; không âm; concurrency an toàn |
| Save migration | Nâng version profile không mất dữ liệu (ADR-007) |
| Authz | Người chơi không thao tác tài nguyên người khác |
| Architecture | Domain không ref Infrastructure; hướng phụ thuộc đúng (`../architecture/dependency-graph.md`) |

## 4. Nguyên tắc
- Domain test **không** chạm DB (thuần, nhanh).
- Integration dùng container/DB thật cho hành vi SQL đúng.
- Idempotency & concurrency có test riêng (chạy song song mô phỏng double-claim).
- Deterministic: inject `IClock`, `IRandomProvider` (seeded) để test tái lập.

## 5. Liên kết
- Strategy: `README.md` · Combat: `../gameplay/combat-framework.md`, ADR-011
- Backend design: `../backend/`
