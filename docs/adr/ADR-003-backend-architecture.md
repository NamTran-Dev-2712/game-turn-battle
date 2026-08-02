# ADR-003: Backend Architecture (Kiến trúc backend)
- Status: Accepted
- Date: 2026-08-02
- Deciders: Lead Technical Architect
- Related: ADR-007, ADR-008, ADR-011, `../backend/`

## Context
Backend .NET 9 là **nguồn sự thật** (server-authoritative — `../mvp/14` R1). Cần scalable/testable/maintainable cho live-service, nhiều dev/AI. Ràng buộc: Clean Architecture, CQRS, MediatR, EF Core, PostgreSQL, Redis (đề bài).

## Decision
Áp dụng **Clean Architecture** 4 tầng + **CQRS qua MediatR**:
- **Domain**: entity/aggregate/value object/domain service/domain event, business rule thuần, **không phụ thuộc framework**.
- **Application**: Command/Query + handler (MediatR), port/interface, validator, **pipeline behaviors** (validation, logging, transaction, caching).
- **Infrastructure**: EF Core (PostgreSQL), repository impl, Redis, JWT, Configuration Service, background jobs — **implements ports**.
- **Api (Presentation)**: controllers/minimal API, SignalR hub (optional), DTO mapping, auth.
- **DI** ở composition root (Api). Hướng phụ thuộc vào trong (DIP).

## Alternatives
| Phương án | Vì sao loại |
|---|---|
| N-layer truyền thống (không đảo phụ thuộc) | Domain dính DB/framework, khó test |
| Vertical slice thuần (không tầng) | Tốt cho nhỏ, nhưng cần kỷ luật tầng cho 5+ năm; ta kết hợp feature-folder trong Application |
| Microservices ngay | Quá sớm, phức tạp vận hành cho MVP; modular monolith trước, tách sau |

## Trade-offs
- **Được:** ranh giới rõ, testable, thay hạ tầng dễ, hợp AI (mỗi tầng ngữ cảnh gọn).
- **Mất:** boilerplate (command/handler/DTO), đường cong học; cần quy ước rõ (`../backend/`).

## Consequences
- Solution `GameTeam.{Domain,Application,Infrastructure,Api,Contracts}` (`../architecture/project-structure.md`).
- CQRS tách read/write; behaviors chuẩn hoá cross-cutting.
- Modular monolith, chia theo feature-folder trong Application để dễ tách service sau.
- Test kiến trúc chặn phụ thuộc sai (`../testing/backend-testing.md`).
