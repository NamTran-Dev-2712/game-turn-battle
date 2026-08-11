# 0003 — Shared contracts standardized (Phase 05)

- Date: 2026-08-11
- Scope: contracts / api
- Status: Active

## Decision
The client↔server contract "spine" is standardized and **closed** in roadmap Phase 05
(`docs/roadmap/05-shared-contracts-skeleton.md`). Source of truth is **C# `GameTeam.Contracts`**
(one public type/file, references Domain only): six shared enums
(`Faction`/`Class`/`Element`/`Role`/`Currency`/`Rarity`, each `None=0` sentinel, explicit numeric values)
and foundation DTOs (`AuthGuestRequest/Response`, `ProfileDto`, `ConfigBundleDto`/`ConfigVersion`,
`ErrorResponse` + `ErrorEnvelope` wrapper `{ "error": {…} }`, `HealthResponse`, `ApiVersions`).
OpenAPI is **generated from code** (.NET 9 `Microsoft.AspNetCore.OpenApi` +
`Microsoft.Extensions.ApiDescription.Server`, build-time) to **`shared/contracts/openapi.json`** — the
single source for client codegen (Phase 08); **never hand-edited**. A document transformer publishes the
shared enums as string components. API versioning is `/api/v1/...`; the base routes are declared as
**501 metadata stubs** (contract shape only — real handlers are Phase 13).

Guards: `EnumStabilityTests` + serialization round-trip + `OpenApiContractTests` (validates the doc via
`Microsoft.OpenApi.Readers`) in `GameTeam.Contracts.Tests`/`Api.IntegrationTests`; NetArchTest
(`Contracts` → Domain only) in `GameTeam.Application.Tests`; and a CI **OpenAPI drift guard**
(`git diff --exit-code` on the generated `openapi.json`) in `ci-server.yml`.

Future agents **reuse** this — inspect `GameTeam.Contracts` + `shared/contracts/openapi.json` before adding
any contract/enum/DTO, and only **extend** it (additive: new endpoints, optional fields, new enum values;
never renumber/reuse enum values, never hand-edit the generated spec, never create a second contract
source). Contract changes must run doc-sync (`docs/backend/api-and-versioning.md`) + rebuild to regenerate
the spec + (Phase 08) regenerate codegen.

## Why
Contract-first (ADR-008) lets backend (Phase 13) and client (Phase 08/15) proceed in parallel without
drift. Fixing the source-of-truth once (C# → OpenAPI single-source, enum stability policy, error envelope,
versioning) prevents divergent hand-written contracts and silent breaking changes. Verified by real runs
(SDK 9.0.306): `dotnet build -c Release` 0/0, `dotnet test` 63 pass, OpenAPI valid + deterministic,
negative tests (enum renumber, injected Contracts dependency) fail as expected then revert green.

## Not this
Swashbuckle / a second OpenAPI generator was rejected in favor of the .NET 9 first-party stack (no second
system). Hand-maintaining `openapi.json` was rejected (drift risk → generated + CI-guarded). Inventing
`Faction` members was rejected — GP2 is unresolved in `docs/mvp/10-open-questions.md`, so `Faction` ships
with only `None` until the list is decided. Endpoint behavior, feature DTOs (hero/gacha/battle), SignalR,
and client codegen are explicitly out of scope → Phases 13 / feature phases / Post-MVP / 08.
