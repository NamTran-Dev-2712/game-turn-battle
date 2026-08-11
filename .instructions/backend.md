# Instructions: Backend scope (`server/`)

Short execution hints. Canonical design: `docs/backend/`. Agent: `.claude/agents/dotnet-backend`.

- Clean Architecture + CQRS/MediatR; dependency direction **inward only** (`docs/architecture/dependency-graph.md`).
- Domain is pure — no EF/HTTP/`DateTime.Now`. Inject `IClock`; use server time.
- Feature layout: `.templates/backend-feature-folder/` (command/query + handler + validator).
- **Contracts (Phase 05, đã chốt):** enum/DTO dùng chung ở `GameTeam.Contracts` (một public type/file), chỉ ref Domain. OpenAPI **sinh từ code** ra `shared/contracts/openapi.json` (build-time) — **không sửa tay**; feature chỉ **mở rộng** (additive), không breaking. Enum: giá trị số cố định, serialize chuỗi, chỉ thêm. Xem `docs/backend/api-and-versioning.md` §4.
- Config via port only (ADR-005); no hardcoded balance (ADR-004).
- New NuGet dep → `Directory.Packages.props` + PR justification (ADR-010).
- Tests: xUnit; `dotnet test server/GameTeam.sln` must stay green (incl. NetArchTest gate).
- Decisions to make: ADR-003, ADR-005, ADR-007/011, ADR-010.
