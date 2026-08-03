# `server/` — Backend .NET 9 (Clean Architecture)

> Solution `.NET 9` theo Clean Architecture + CQRS/MediatR. Nguồn sự thật thiết kế: `../docs/backend/`, `../docs/architecture/project-structure.md` §4.

| Mục | Nội dung |
|---|---|
| **Purpose** | API server-authoritative: xử lý combat re-sim, kinh tế, save, config service (ADR-003/007/011). |
| **Responsibilities** | Domain rule, CQRS handler, EF/Redis/JWT, endpoint versioned. |
| **Allowed** | Mã C# theo tầng; test theo tầng. |
| **Not allowed** | ❌ Domain phụ thuộc EF/HTTP; ❌ Application → Infrastructure; ❌ secret trong code. |
| **Dependencies** | `shared/contracts` (hợp đồng), PostgreSQL, Redis. |
| **Owner** | Backend team. |
| **Future expansion** | Thêm feature-folder trong Application; tách microservice sau (`mvp/09` SC1). |

## Cấu trúc
```text
server/
├── GameTeam.sln
├── Directory.Build.props        # nullable, analyzers, warnings-as-error, net9.0
├── Directory.Packages.props     # Central Package Management (ADR-010)
├── Dockerfile                   # image cho Api
├── src/
│   ├── GameTeam.Domain/         # Domain thuần (không phụ thuộc)
│   ├── GameTeam.Application/    # CQRS/MediatR, ports, validators, behaviors
│   ├── GameTeam.Infrastructure/ # EF Core, Redis, JWT, config service, jobs
│   ├── GameTeam.Api/            # Presentation + composition root (DI)
│   └── GameTeam.Contracts/      # DTO versioned (nguồn codegen client)
└── tests/
    ├── GameTeam.Domain.Tests/
    ├── GameTeam.Application.Tests/       # + architecture test (NetArchTest)
    ├── GameTeam.Infrastructure.Tests/
    └── GameTeam.Api.IntegrationTests/
```

## Chạy
- Build: `dotnet build GameTeam.sln`
- Test: `dotnet test GameTeam.sln`
- Run API: `dotnet run --project src/GameTeam.Api` (mặc định `/health`).

> **Bootstrap:** đây là **skeleton compile được, KHÔNG logic nghiệp vụ**. DI layer là stub; endpoint duy nhất là `/health` (infra health, không phải API game). Hiện thực ở phase Core Framework trở đi.

## Quy tắc phụ thuộc (kiểm bằng NetArchTest)
Api → Application/Infrastructure(chỉ DI); Application → Domain/Contracts; Infrastructure → Application/Domain; **Domain → không gì**. Chi tiết: `../docs/backend/solution-structure.md` §4, `../docs/architecture/dependency-graph.md`.
