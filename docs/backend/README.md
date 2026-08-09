# Backend Design (.NET 9 — Clean Architecture)

> Thiết kế backend là **nguồn sự thật server-authoritative** (ADR-007). Kiến trúc: Clean Architecture + CQRS/MediatR + EF Core (PostgreSQL) + Redis + JWT + SignalR (optional). Chi tiết quyết định: ADR-003.

## Danh mục
| File | Nội dung |
|---|---|
| [solution-structure.md](solution-structure.md) | Solution/projects/tầng/DI + ánh xạ Clean Architecture |
| [domain-and-application.md](domain-and-application.md) | Domain layer; Application (CQRS/MediatR), ports, validators, behaviors |
| [infrastructure.md](infrastructure.md) | EF Core, repositories, PostgreSQL, Redis, Configuration Service, migration |
| [cross-cutting.md](cross-cutting.md) | Auth/JWT, logging, monitoring, health checks, background jobs |
| [api-and-versioning.md](api-and-versioning.md) | API design, versioning, error contract, DB/schema migration |

## Nguyên tắc
- Hướng phụ thuộc vào trong; Domain thuần (ADR-003).
- CQRS tách read/write; cross-cutting qua pipeline behaviors.
- Data-driven qua Configuration Service (ADR-004/005).
- Server-authoritative + deterministic re-sim (ADR-011).
- Modular monolith, feature-folder → dễ tách service về sau.

## Liên kết
- Yêu cầu (SSOT): [`../mvp/03-core-gameplay.md`](../mvp/03-core-gameplay.md), [`../mvp/06-game-economy.md`](../mvp/06-game-economy.md) · Hàm ý kỹ thuật: [`../mvp/08-technical-impact.md`](../mvp/08-technical-impact.md).
- Quyết định: [`../adr/ADR-003-backend-architecture.md`](../adr/ADR-003-backend-architecture.md) · Conventions: [`../conventions/`](../conventions/) · Testing: [`../testing/`](../testing/)
