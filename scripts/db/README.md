# `scripts/db/` — Database (migration/seed)

| Việc | Lệnh (khi có Infrastructure) |
|---|---|
| Thêm migration | `dotnet ef migrations add <Name> -p ../../server/src/GameTeam.Infrastructure -s ../../server/src/GameTeam.Api` |
| Cập nhật DB | `dotnet ef database update -p ../../server/src/GameTeam.Infrastructure -s ../../server/src/GameTeam.Api` |
| Seed dev | (stub) — thêm khi có seed data. |

> Migration **additive-first** (ADR: EF Core). Chi tiết: `../../docs/backend/infrastructure.md`, `../../docs/backend/api-and-versioning.md`. **Bootstrap:** chưa có DbContext/migration.
