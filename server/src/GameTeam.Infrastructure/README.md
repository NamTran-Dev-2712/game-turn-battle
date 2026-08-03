# `GameTeam.Infrastructure` — Infrastructure layer

EF Core/PostgreSQL, repository impl, Redis, JWT, Configuration Service, background jobs, adapters. Hiện thực **port** của Application (DIP). **Owner:** Backend. Thiết kế: `../../../docs/backend/infrastructure.md`, `../../../docs/backend/cross-cutting.md`. **Bootstrap:** `AddInfrastructure()` là stub rỗng (gói EF/Redis/JWT đã tham chiếu sẵn qua CPM).
