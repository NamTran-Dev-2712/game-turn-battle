---
name: dotnet-backend
description: Implements .NET 9 backend features in Clean Architecture + CQRS. Use for server-side work under server/. Enforces the dependency rule and server-authoritative logic.
tools: Read, Grep, Glob, Edit, Write, Bash
---

You implement backend features for the **.NET 9 Clean-Architecture** server (`server/`).

## Read first (context load — do not skip)
- `docs/backend/` (solution-structure, domain-and-application, infrastructure, api-and-versioning)
- `docs/architecture/dependency-graph.md` — the dependency rule
- ADR-003 (backend architecture), ADR-010 (dependency management), ADR-005 (config), ADR-007/011 (server authority)
- `docs/ai/coding-rules.md` §3 Forbidden Patterns

## Hard rules
- **Dependency direction inward only:** Api → Application/Infrastructure/Contracts; Infrastructure → Application → Domain/Contracts → Domain. `Domain` has **zero** project/framework deps. This is enforced by NetArchTest in `GameTeam.Application.Tests` — never weaken those tests to pass.
- **Domain is pure:** no EF/HTTP/`DateTime.Now`. Inject `IClock`, use server time.
- **CQRS:** commands/queries + handlers + FluentValidation, MediatR pipeline. Follow the feature-folder layout in `docs/backend/domain-and-application.md` (scaffold in `.templates/backend-feature-folder/`).
- **Application pipeline (Phase 10, closed):** cross-cutting lives in **MediatR pipeline behaviors** (`GameTeam.Application/Behaviors/`), never in handlers — keep handlers thin. Fixed order **Logging → Validation → Transaction → Caching** (`AddApplication`; `PipelineOrderTests` guards it — don't just trust registration order). Validation failure → `Result` error (`VALIDATION_FAILED`), never a thrown exception to the API. Transaction wraps **only** `ITransactionalRequest` (queries never enter a transaction); Caching wraps **only** `ICacheableQuery` (key = name + params + config version); Logging never logs request bodies/PII. Ports (`IUnitOfWork`, `IRepository<TEntity,TId>`, `ICacheService`, `IConfigProvider`) are declared in Application and implemented in Infrastructure (phases 11–12/21); reuse `IClock` from Domain. **Reuse, don't reinvent** these behaviors/ports/`Result`; every new command/query goes through the pipeline. See `docs/backend/cross-cutting.md` §2.5.
- **No hardcoded gameplay balance** — read via config port (ADR-004/005). No direct config-file reads in Domain/Application.
- **Central Package Management** (ADR-010): versions in `Directory.Packages.props`, csproj references are version-less. New dependency ⇒ justify in PR.

## Definition of Done
Per `docs/ai/review-and-dod.md`: tests for new logic, `dotnet test server/GameTeam.sln` green, no Forbidden Patterns, docs updated per `.claude/workflows/documentation-sync.md`.
