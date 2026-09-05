# CLAUDE.md — AI Execution Entry Point

> Auto-loaded every session by Claude Code. This file is the **execution layer**: it does
> **not** restate project knowledge — it points to the Source of Truth (`docs/`) and to the
> workflows you must follow. If anything here conflicts with `docs/adr/` or `docs/mvp/`,
> **the docs win** and this file is wrong — flag it.

## 1. What this repo is (1-paragraph orientation)

A long-term, data-driven mobile turn-battle RPG. **Godot 4.7 client** (`client/`) + **.NET 9
Clean-Architecture backend** (`server/`) + shared contracts (`shared/`). Combat is
**server-authoritative and deterministic**. The project is documentation-first: `docs/` is the
Single Source of Truth (SSOT), and code is written to match it — never the other way around.
Current phase: **Bootstrap → Core Framework** (see `docs/audit/bootstrap-audit.md`).

## 2. Golden Rules (non-negotiable)

1. **SSOT supremacy.** Business truth lives in `docs/mvp/`; architecture truth in `docs/adr/`
   + `docs/architecture/`. Do not invent requirements or reverse a decision — read the source.
2. **ADR wins on conflict.** On any architectural disagreement, the relevant ADR is final.
   Need a new decision? Propose an ADR (`docs/adr/README.md`), don't decide silently.
3. **Server-authoritative.** No sensitive/economy/result decision on the client — ADR-007, ADR-011.
4. **Data-driven.** No hardcoded gameplay balance numbers — config + Configuration Service, ADR-004/005.
5. **Deterministic combat.** Integer/fixed-point + seeded RNG in the sim — ADR-011.
6. **When unsure, don't guess.** Ambiguity → record in `docs/mvp/10-open-questions.md` and ask.

Full rules + the **Forbidden Patterns** table: **`docs/ai/coding-rules.md`** (read §3 before writing code).

## 3. Before writing code — mandatory context load

Follow the order in **`docs/ai/context-strategy.md`** §2:

1. Task goal + acceptance criteria →
2. Relevant business SSOT (specific `docs/mvp/` file, not all) →
3. Relevant ADR(s) →
4. Module boundary + conventions (`docs/architecture/dependency-graph.md`, `docs/conventions/`) →
5. Existing module code (reuse before writing new) →
6. Start **small, with a test**.

## 4. Execution layer (this repo's `.claude/` + dotfolders)

| Need | Go to |
|---|---|
| How to implement / review / keep docs synced | `.claude/workflows/` |
| Startup / pre-response / commit / self-review gates | `.claude/checklists/` |
| Reusable prompt for a feature/bugfix/refactor/etc. | `.prompts/` |
| Code/doc scaffolds (no gameplay logic) | `.templates/` |
| Pre-assembled context package for a task area | `.context/` |
| Quick rule reminder (canonical stays in docs) | `.rules/rules.md` |
| Scope-specific hints (backend/client/config/combat) | `.instructions/` |
| Project decision log (below ADR level) | `.memory/` |
| Cross-session task hand-off | `.tasks/` |
| Repo-tuned specialist agents | `.claude/agents/` (charters in `.agents/`) |

## 4.5 Phase-Execution Protocol (roadmap work is a contract)

When the task is a **roadmap phase** (`docs/roadmap/NN-*.md`), the Strict Phase Gate in
**`docs/roadmap/README.md` §4–§5** is binding. Do not restate it — apply it:

1. **One phase per session.** Execute the lowest un-closed phase whose prerequisites are Closed.
   Do not skip, reorder, or bundle phases.
2. **Read the whole phase file first**, plus every doc it links (ADR / testing / deployment /
   architecture). Inspect the existing implementation before changing it.
3. **The phase's `# Công việc cần thực hiện` list is the execution contract.** Implement it item by
   item; never invent requirements outside the documented scope.
4. **`- [ ]` → `- [x]` only after the item is implemented AND verified** — with evidence from a run
   (build/test/CI output, a negative test, an artifact). Never check an item on intent, or because
   code merely exists. For a gate that **only runs on CI** (Godot headless, Docker build with no local
   daemon, a `v*`-tag release), the evidence is the **Actions result** — until you have it, keep the
   item `[ ]` ("CI-verification pending") and don't self-certify.
5. **Run the phase's negative / failure-path tests** where specified (e.g. inject a dependency-rule
   leak, confirm the gate fails), then **fully revert** the temporary change and re-verify green.
6. **Never implement future-phase scope.** Future gates stay explicit placeholders pointing at their
   owning phase.
7. **Before declaring the phase done:** re-read the phase file, run the completeness audit, confirm
   every checklist item is `[x]` with evidence and no `TODO`/blocker remains (Gate condition set).
   An unchecked item means the phase is **not** complete.

## 4.6 Completed-phase constraints (established infrastructure — reuse, don't reinvent)

**Completed Phase Preservation Rule.** A closed, verified phase is an **established project constraint**.
Inspect its existing implementation before modifying or replacing it. You may **extend** it when a later
phase requires it, but never silently swap a completed convention for a new architecture. When a phase
changes repository behavior, its roadmap checklist/`# Phase Review` **and** the relevant AI/vibe-code docs
must be updated in the same change so the decision stays visible to future agents.

**Dev environment is standardized (Phase 04 — closed & verified).** One-command local dev via
**Docker Compose** (`deploy/compose/docker-compose.yml`): **`postgres:16-alpine`** + **`redis:7-alpine`**
(always-on, real healthchecks) on the **`game-team-dev`** network, with the **`api` profile** building from
`server/Dockerfile` and injecting `ConnectionStrings__Postgres/__Redis`; config from **`.env`** (local-only,
git-ignored) templated by **`.env.example`**; cross-platform **`scripts/dev/up.{ps1,sh}`** (`-Api`/`--api`,
healthcheck-driven readiness) and **`down.{ps1,sh}`** (`-Volumes`/`-v`; default **preserves** the pgdata
volume); API liveness at **`GET /health`** → `{"status":"ok"}`. Canonical how-to + troubleshooting:
`docs/deployment/README.md` → **Local development**.

- Future agents **MUST inspect and reuse** this dev environment/scripts before creating any new local
  infra, compose file, ports, env-var conventions, or startup scripts.
- Future agents **MUST NOT reinvent** the Phase 04 dev environment unless a later phase explicitly requires
  an extension (e.g. Testcontainers, a new service).
- When modifying it, keep **`.env.example` + compose + scripts + docs + this AI guidance in sync** (doc-sync
  matrix, §5).

**Shared contracts are standardized (Phase 05 — closed & verified).** The client↔server contract spine has
one source of truth: **C# `GameTeam.Contracts`** (one public type/file, references Domain only) — six shared
enums (`Faction`/`Class`/`Element`/`Role`/`Currency`/`Rarity`; `None=0` sentinel, explicit numeric values)
and foundation DTOs (`AuthGuest{Request,Response}`, `ProfileDto`, `ConfigBundleDto`/`ConfigVersion`,
`ErrorResponse` + `ErrorEnvelope` wrapper, `HealthResponse`, `ApiVersions`). **OpenAPI is generated from
code** (.NET 9 `Microsoft.AspNetCore.OpenApi` + `Microsoft.Extensions.ApiDescription.Server`, build-time) to
**`shared/contracts/openapi.json`** — the single source for client codegen (Phase 08). API versioning is
`/api/v1`; base routes are **501 metadata stubs** (real handlers = Phase 13). Canonical rules:
`docs/backend/api-and-versioning.md` §4; decision log: `.memory/0003-shared-contracts-standardized.md`.

- Future agents **MUST inspect and reuse** `GameTeam.Contracts` + `shared/contracts/openapi.json` before
  adding any enum/DTO/contract; **MUST NOT** create a second contract source or hand-edit the generated spec.
- Contract evolution is **additive-only** (new endpoints, optional fields, new enum values); **never**
  renumber/reuse an enum value or make a breaking change inside a major — that requires a new `/api/vN`.
- When changing a contract, keep **`GameTeam.Contracts` + regenerated `openapi.json` + `EnumStabilityTests`
  + `docs/backend/api-and-versioning.md` in sync** (doc-sync matrix, §5); the CI OpenAPI drift-guard enforces
  the regenerate step.

**Config schemas are standardized (Phase 06 — closed & verified).** The data-driven config contract has one
source of truth: **JSON Schema (draft 2020-12) in `shared/config-schema/`** — **8 per-type schemas**
(`hero`/`skill`/`stage`/`gacha`/`shop`/`reward`/`economy`/`quest`) + **`common.schema.json`** (shared `$defs`:
id prefixes, `combat_int`, enums matching `GameTeam.Contracts`, `cost`) + **`config-bundle.schema.json`**
(the single envelope, `config@vN` compatible). Keys are `snake_case`; combat values are **integer** (ADR-011);
every file carries `schema_version`; IDs use per-type prefixes (`hero_`, `skill_`, `stage_`, …). Minimal
pass/fail **fixtures** live in `fixtures/`; migration rules in `_versions/`. Schemas define **structure/type
only — never balance values**. Canonical rules: `docs/gameplay/configuration-and-data.md` (schema-mapping),
`docs/conventions/data-and-docs-conventions.md`; decision log: `.memory/0004-config-schema-standardized.md`.

- Future agents **MUST inspect and reuse** these 8 schemas + `common.schema.json` before adding any config
  field, enum value, effect type, quest/condition type, currency, or reward type; **MUST NOT** invent
  gameplay not backed by `docs/gameplay/*` + ADRs, and **MUST NOT** create a second config envelope.
- Schema evolution is **additive-only** by default (new optional field, new enum value = no version bump). A
  **breaking** change (remove/rename field, tighten type, narrow enum, change meaning) requires **`schema_version`
  bump + migration under `_versions/` + doc-sync**.
- Schemas hold **no balance numbers** (rates/pity/stats/curves are tuning). Cross-file **referential integrity**
  (hero→skill, stage→reward…) is the **phase-07 validator**, not JSON Schema alone — never claim a single
  schema validates cross-file id existence. Runtime Configuration Service is **phase 21**.
- When changing a schema, keep **schemas + `fixtures/` + `_versions/` + `docs/gameplay/configuration-and-data.md`
  + `docs/liveops/remote-config.md` + `.instructions/config.md` in sync** (doc-sync matrix, §5).

**Config validator is standardized (Phase 07 — closed & verified).** Config correctness is enforced by ONE tool:
**`tools/config-validator`** — a **.NET 9 console** (`JsonSchema.Net`, CPM per ADR-010) split into a reusable
**core lib** (`GameTeam.ConfigValidator`: `SchemaSet`/`ConfigFileMapper`/`ConfigLoader`/`IdIndex`/`SchemaValidator`/
`ReferenceValidator`/`VersionValidator`/`ConfigValidationRunner`) + a **thin CLI** + xUnit tests. It validates every
`config/**` file for **(1)** JSON Schema (draft 2020-12), **(2)** cross-file **referential integrity**
(hero→skill; stage→hero/reward/stage; gacha→hero; shop→reward; quest→reward; `reward.entries[].ref_id` polymorphic
by `reward_type`), **(3)** `schema_version` (supported = `1`). Errors aggregate (never stop at first) and print as
**`file:jsonpath:CODE message`** with stable codes **`JSON001`/`MAP001`/`SCH001`/`VER001`/`VER002`/`REF001`/`REF002`**
(exit `0` ok / `1` invalid / `2` tool error). It is a **mandatory CI gate**: `.github/workflows/validate-config.yml`
runs `tools/config-validator/run.sh config shared/config-schema` (setup-dotnet via `global.json`; `run.sh` is exec
`100755`). Canonical how-to + error-code table: `tools/config-validator/README.md`; decision log:
`.memory/0005-config-validator-standardized.md`.

- Future agents **MUST run the validator** (`bash tools/config-validator/run.sh config shared/config-schema`) after
  any change to `config/**` or `shared/config-schema/**`, and **MUST NOT bypass or weaken it to make CI green**
  (no editing config to silence a real contract violation; fix the correct layer — validator, schema, or config).
- **Referential integrity must never be bypassed.** A new config type MUST ship its schema (Phase 06 rules) **and**
  validator support (`ConfigFileMapper` + `ReferenceValidator` + a test) in the same change. **Do NOT invent**
  config relationships or `schema_version` values not backed by the schemas + `docs/gameplay/*` + ADRs.
- **Reuse, don't reinvent:** Config Service (Phase 21) **MUST project-reference the core lib** and call
  `ConfigValidationRunner.Run(...)` before publishing bundles — never a second validation implementation. Phase 07
  does **not** implement Config Service / bundle publishing / runtime loading / migration execution.
- When changing validator behavior/error codes, keep **`tools/config-validator` (core + tests) + its README +
  `.github/workflows/validate-config.yml` + `docs/gameplay/configuration-and-data.md` + `docs/deployment/ci-cd-pipeline.md`
  + `.instructions/config.md` in sync** (doc-sync matrix, §5); validator tests are the behavior contract — update them.

**Client codegen is standardized (Phase 08 — closed & verified).** Client read-model DTOs/enums are **generated,
never hand-written**, from the single contract source. ONE tool: **`shared/codegen`** — a **.NET 9 console**
(zero external NuGet; only `System.Text.Json`; own solution/CPM split like Phase 07: reusable core lib
`GameTeam.Codegen` = `OpenApiReader`/`GdTypeMapper`/`GdEmitter`/`CodegenRunner` + thin CLI `codegen` + xUnit tests) that
reads **`shared/contracts/openapi.json`** and emits **GDScript** into **`client/src/data/generated/`** (one `*.gd` per
schema, snake_case): 6 shared enums (`class_name <Name>` + unnamed `enum {…}`, **preserving the C# numeric values** incl.
`Rarity` gaps `0,3,4,5`) and 8 foundation DTOs (`class_name <Name> extends Resource`, typed vars; `## wire: <jsonKey>`
docs for Phase-15 parsing). Enums carry their numbers because Phase 05's `ContractEnumsDocumentTransformer` now emits
**`x-enum-varnames` + `x-enum-values`** into the spec (single-source). Every generated file has an
**`AUTO-GENERATED — DO NOT EDIT`** header + source path. Deterministic + idempotent (fixed order, LF, no timestamp).
It is a **mandatory CI gate**: `.github/workflows/codegen-check.yml` runs `shared/codegen/run.sh` then
`git diff --exit-code -- client/src/data/generated` (stale generated ⇒ FAIL); Godot headless `--import` of the models is
covered by `ci-client.yml`. Canonical how-to + type-map/limitations: `shared/codegen/README.md`; decision log:
`.memory/0006-codegen-pipeline-standardized.md`.

- Future agents **MUST regenerate** (`bash shared/codegen/run.sh`) after ANY change to `shared/contracts/openapi.json`
  (i.e. any `GameTeam.Contracts` change) and **MUST commit the generated diff**; **MUST NOT hand-edit** files under
  `client/src/data/generated/`, **MUST NOT** hand-define duplicate client DTOs, and **MUST NOT** bypass/weaken the drift
  gate to make CI green.
- **Contract/DTO change workflow (binding):** edit `GameTeam.Contracts` → rebuild (regenerate `openapi.json`) →
  `bash shared/codegen/run.sh` → verify generated diff → Godot import → drift check → doc-sync → phase/task checklist.
  Generated models are the client's **read-model** — client never re-declares them.
- **Reuse, don't reinvent:** the generator is **schema-driven** (no hardcoded DTO list) — new contract DTOs/enums appear
  automatically on regenerate. Unsupported OpenAPI constructs (`oneOf`/`allOf`/map/array-without-items/dangling `$ref`)
  **fail clearly** (`schema:property:reason`) — extend `GdTypeMapper` + a test rather than emitting wrong models. Network
  parse/round-trip is **Phase 15**, not here (models are data-only).
- When changing codegen behavior, keep **`shared/codegen` (core + tests) + its README + `.github/workflows/codegen-check.yml`
  + the generated `client/src/data/generated/**` + `ContractEnumsDocumentTransformer` (+ `OpenApiContractTests`) +
  `docs/backend/api-and-versioning.md` + `docs/godot/resources-and-assets.md` + `docs/deployment/ci-cd-pipeline.md` +
  `.instructions/client.md` in sync** (doc-sync matrix, §5); codegen tests are the behavior contract — update them.

**Domain foundation is standardized (Phase 09 — closed & verified).** The backend Domain core has ONE home for reusable
primitives: **`GameTeam.Domain/Common/`** — one public type/file, **BCL-only** (`GameTeam.Domain` has **zero package
references**, enforced by NetArchTest). Types: **`Result`/`Result<T>`** (expected business failures; `Result<T>.Value`
throws `InvalidOperationException` on failure), **`Error(Code, Message)`** (immutable value; stable `SCREAMING_SNAKE_CASE`
code for Phase-13 API mapping; `Error.None`; no stack/DB/infra leakage), **`Entity<TId>`** (identity equality),
**`ValueObject`** (component/value equality), **`AggregateRoot<TId> : Entity<TId>`** (owns domain events —
`RaiseDomainEvent`/read-only `DomainEvents`/`ClearDomainEvents`), **`IDomainEvent`** (marker), **`IClock`**
(`DateTimeOffset UtcNow` — the server-time boundary), **`Guard`** (`NotNull`/`Positive`/`InRange`, **throw** BCL argument
exceptions). Verified: `dotnet build -c Release` clean (warnings-as-error), `dotnet test` **101 pass** (Domain.Tests 35),
NetArchTest `Domain_should_not_depend_on_framework_packages` green, no wall-clock call site in Domain. Canonical rules:
`docs/backend/domain-and-application.md` → "Foundation primitives (Phase 09)"; decision log:
`.memory/0007-domain-foundation-standardized.md`.

- Future agents **MUST reuse** these `Common/` primitives before adding any base type/abstraction; **MUST NOT** re-invent
  a second `Result`/`Entity`/`ValueObject`/event base, and **MUST NOT** add EF/ASP.NET/HTTP/MediatR or any package to
  `GameTeam.Domain` (keep it pure — the NetArchTest gate fails otherwise).
- **Result vs Exception (binding):** **`Result`** for expected business failures (handler returns, caller handles);
  **exceptions/`Guard`** for programming errors, invariant violations, and infrastructure failures. Do **not** create a
  second validation paradigm (Guard never returns `Result`). Do **not** turn every exception into `Result`.
- **Domain events:** Domain only **raises and collects** events; **dispatch** (MediatR/notification/bus) belongs to
  Application/Infrastructure (**Phase 10/11**) — never dispatch from Domain. Feature entities (Hero/Profile/Currency),
  persistence, and MediatR are **out of scope** for Phase 09.
- **Server-time:** all business time comes through **`IClock.UtcNow`**; **never** use `DateTime.Now/UtcNow` or
  `DateTimeOffset.Now/UtcNow` directly in Domain (Forbidden Pattern). `IClock` lives in **Domain** (not Application);
  Infrastructure implements it later.
- When changing a Domain primitive, keep **`GameTeam.Domain/Common/*` + `GameTeam.Domain.Tests` (behavior contract) +
  the NetArchTest purity facts (`GameTeam.Application.Tests/ArchitectureTests.cs`) + `docs/backend/domain-and-application.md`
  + `.instructions/backend.md` in sync** (doc-sync matrix, §5).

**Application pipeline is standardized (Phase 10 — closed & verified).** The Application layer owns CQRS/MediatR
orchestration; **cross-cutting concerns live in MediatR pipeline behaviors, never in handlers** (ADR-003) — handlers
stay **thin**. Home: **`GameTeam.Application/Behaviors/`** = four `IPipelineBehavior<,>` in fixed execution order
**`Logging → Validation → Transaction → Caching`** (registered via `AddOpenBehavior` in `AddApplication`; outermost
first). **LoggingBehavior** logs request type name + elapsed ms + outcome only — never serializes the body (no
token/PII leak). **ValidationBehavior** (`where TResponse : Result`) runs FluentValidation before the handler; failure
short-circuits to a failed `Result` (stable code **`VALIDATION_FAILED`**, `ValidationErrors.ToError`) — handler not
invoked, **no exception leaks** to the API. **TransactionBehavior** (`where TRequest : ITransactionalRequest`) wraps
only opted-in write commands in `IUnitOfWork` (commit on success `Result`, rollback on failed `Result` **or**
exception+rethrow) — **queries never enter a transaction**. **CachingBehavior** (`where TRequest : ICacheableQuery`)
reads/writes cache by key = **`{RequestTypeName}:{CacheKey}:cfg{IConfigProvider.CurrentVersion.Bundle}`** (name +
params + config version), TTL from the query, caches successes only. Ports (DIP) declared in Application:
**`IUnitOfWork`**, **`IRepository<TEntity, TId>`**, **`ICacheService`**, **`IConfigProvider`** (Phase 10 uses only
`CurrentVersion`); **`IClock`** reused from Domain (Phase 09). `AddApplication` registers MediatR + FluentValidation +
the 4 behaviors ONLY — **no port implementation** (composition root = Api). Verified: `dotnet build -c Release` clean
(0 warning), `dotnet test` **121 pass** (Application.Tests **25**), pipeline order proven behaviorally by
`PipelineOrderTests` (+ negative swap ⇒ red ⇒ reverted), NetArchTest
`Application_should_not_depend_on_infrastructure_or_api` green, no `openapi.json` drift. Canonical rules:
`docs/backend/cross-cutting.md` §2.5; decision log: `.memory/0008-application-pipeline-standardized.md`.

- Future agents **MUST reuse** these behaviors/ports/markers before adding cross-cutting; **MUST NOT** re-invent a
  second behavior/port/`Result`/validation-error paradigm, **MUST NOT** put cross-cutting logic inside feature handlers,
  and **MUST NOT** bypass MediatR. Every future command/query goes through the pipeline and inherits it.
- **Marker semantics are binding:** transactionality/caching are **explicit opt-in** via `ITransactionalRequest` /
  `ICacheableQuery` — **never** inferred from a "Command"/"Query" name. Queries must **not** be marked transactional.
- **DIP boundary:** ports live in Application, implementations in Infrastructure (EF Core **phase 11**, Redis **phase
  12**, Config Service **phase 21**). `GameTeam.Application` **must not** reference `GameTeam.Infrastructure`/`Api`
  (architecture test gates it). Phase 10 does **not** implement repositories/cache/Config Service/endpoints, nor
  `IdempotencyBehavior` (deferred). A minimal `SystemClock : IClock` adapter (Infrastructure) is registered so composed
  handlers resolve a clock — real infra impls still deferred.
- When changing pipeline behavior, keep **`GameTeam.Application/Behaviors/*` + ports (`Abstractions/*`) + markers +
  `AddApplication` order + `GameTeam.Application.Tests` (behavior + `PipelineOrderTests` + architecture test) +
  `docs/backend/cross-cutting.md` §2.5 + `docs/backend/domain-and-application.md` §2 + `.instructions/backend.md` +
  `.claude/agents/dotnet-backend.md` in sync** (doc-sync matrix, §5); the behavior tests are the contract — update them.

**Persistence is standardized (Phase 11 — closed & verified).** The backend persistence foundation has ONE home:
**`GameTeam.Infrastructure/Persistence/`** (EF Core 9 + Npgsql) — EF/Npgsql live **only** in Infrastructure
(NetArchTest `Application_should_not_depend_on_efcore_or_npgsql` + the Phase-09 Domain-purity fact gate it).
**`AppDbContext`** (`OnModelCreating` = `ApplyConfigurationsFromAssembly`; **overrides `SaveChangesAsync`** to
**dispatch domain events** collected from tracked aggregates — after persist, inside the open transaction, via MediatR
`DomainEventNotification<TEvent>` + `IPublisher`, then `ClearDomainEvents`). **`EfRepository<TEntity,TId>`** implements
the Phase-10 port (`GetByIdAsync`+`AddAsync` only; **never leaks `IQueryable`/`DbSet`/`DbContext`**). **`UnitOfWork`**
implements the port (`Begin/Commit/Rollback`); the port has **no** `SaveChanges`, so **`CommitAsync` itself calls
`SaveChangesAsync`** (persist+dispatch) then commits the DB transaction ⇒ events dispatch same-transaction (ADR-007).
EF mapping = one **`IEntityTypeConfiguration<T>`** per entity with **explicit `snake_case`** table/column names.
Connection string comes from config key **`ConnectionStrings:Postgres`** (env `ConnectionStrings__Postgres`) — **never
hardcoded**; `AddInfrastructure` fail-fasts if missing. Schema version anchor = **`schema_metadata`** table seeded
`version=1` by migration `Initial` (ADR-007; per-row profile versioning = phase 19). Aggregates implement the BCL-only
marker **`IHasDomainEvents`** (added to `GameTeam.Domain/Common/` this phase — a permitted Phase-09 extension; Domain
stays package-free). Canonical: `docs/backend/infrastructure.md` §1.1/§5.1; decision log:
`.memory/0009-persistence-standardized.md`.

- Future agents **MUST reuse** `AppDbContext`/`EfRepository`/`UnitOfWork`/`DomainEventDispatcher` before adding any
  persistence; **MUST NOT** create a second DbContext/UoW/repository/dispatcher, add EF/Npgsql to Application/Domain, or
  leak `IQueryable`/`DbContext` out of Infrastructure (the architecture gates fail otherwise).
- **Schema changes go through EF migrations** (`dotnet ef migrations add … --project GameTeam.Infrastructure
  --output-dir Persistence/Migrations`; seed via `HasData`; `has-pending-model-changes` must be clean) — migrations are
  **source-controlled artifacts**; **never** hand-edit the DB or a migration to mask a model change. New feature entities
  ship their `IEntityTypeConfiguration<T>` (snake_case) + a migration in the same change.
- **Domain-event dispatch** happens **only** at `AppDbContext.SaveChangesAsync` (after persist, same transaction) — do
  **not** dispatch from Domain, nor add a persisted outbox / re-entrant-handler framework (deferred, noted debt). The
  **idempotency** table (ADR-007) is only *noted* here; it is built/used at phases 31/37. Redis = phase 12, Config
  Service = phase 21, business tables = phase 19+.
- **Persistence tests use Testcontainers PostgreSQL** (`postgres:16-alpine`; require Docker — CI `ubuntu-latest` has it,
  local via `scripts/dev/up`); **never mock `DbContext`** to substitute for them. Cover CRUD, **real rollback**,
  **event dispatch**, migration up/down. The sample entity lives in the **test** assembly (`TestDbContext : AppDbContext`)
  to keep the production schema clean.
- When changing persistence, keep **`GameTeam.Infrastructure/Persistence/*` + `AppDbContext`/`EfRepository`/`UnitOfWork`/
  `DomainEventDispatcher` + `GameTeam.Infrastructure.Tests` (integration contract) + the EF-boundary architecture fact +
  `docs/backend/infrastructure.md` + `docs/testing/backend-testing.md` + `docs/backend/domain-and-application.md` +
  `.instructions/backend.md` + `.claude/agents/dotnet-backend.md` in sync** (doc-sync matrix, §5); the integration tests
  are the behavior contract — update them.

**Redis cache is standardized (Phase 12 — closed & verified).** The distributed-cache foundation has ONE home:
**`GameTeam.Infrastructure/Caching/`** + **`Serialization/`** (StackExchange.Redis lives **only** in Infrastructure —
consumers depend on the Phase-10 port `ICacheService`, never on StackExchange.Redis). **`RedisCacheService`** implements
`ICacheService` (`GetAsync`/`SetAsync`/**`RemoveAsync`** — the port was additively extended this phase): JSON serialize,
**TTL = absolute expiry**, namespaced keys. **Graceful degradation is mandatory** — a Redis failure (`RedisException`)
or corrupt entry (`JsonException`) is **logged (warning) + degraded** (Get→miss/null so the caller runs the real source;
Set/Remove→skipped), **never** thrown to the API; Redis is **not** a single point of failure; programming errors
(`ArgumentNullException`…) still throw (no blanket `catch (Exception)`). **`RedisCacheKey`** centralizes the convention
**`{env}:{domain}:{name}:{configVersion?}`** (cache-query domain = `cache`; `CachingBehavior` already folds
`cfg{IConfigProvider.CurrentVersion.Bundle}` into the name ⇒ a config rollout invalidates stale reads — ADR-005 immutable
`config@vN` = safe to cache long). `Result`/`Result<T>` round-trip via **`ResultJsonConverterFactory`** (STJ; these
Phase-09 immutables have no public ctor so default STJ can't deserialize them) with the shared read-only
**`CacheSerialization.Options`** — Domain stays JSON-attribute-free. DI: `AddInfrastructure` registers
**`IConnectionMultiplexer` singleton** (`AbortOnConnectFail=false` ⇒ boot never blocks when Redis is down) +
`ICacheService → RedisCacheService`; connection from config key **`ConnectionStrings:Redis`** (env
`ConnectionStrings__Redis`) — **never hardcoded**; fail-fasts if missing. `/health` pings Redis (`ok`/`degraded`, always
HTTP 200; full health checks = phase 13+). Verified (Docker Desktop 28.5.1): build Release **0 warning/0 error**,
`dotnet test` **144 pass** (Infrastructure.Tests **22** incl. Testcontainers `redis:7-alpine`: set/get, TTL expiry,
remove, down→degrade, **CachingBehavior real cache-hit**; Api.IntegrationTests **25** incl. `/health` degraded-when-down),
architecture gate green, no `openapi.json` drift. Canonical: `docs/backend/infrastructure.md` §2.1; decision log:
`.memory/0010-redis-cache-standardized.md`.

- Future agents **MUST reuse** `ICacheService`/`RedisCacheService`/`RedisCacheKey`/`CacheSerialization` before adding any
  cache; **MUST NOT** create a second cache abstraction, depend on StackExchange.Redis outside Infrastructure, leak Redis
  types out, or bypass `ICacheService` at a consumer. **MUST NOT** weaken graceful degradation (Redis errors degrade +
  log — never crash a request) or blanket-swallow non-Redis exceptions.
- **Cache versioning follows ADR-005:** key on the immutable config bundle version; **never** cache mutable data under a
  version-less key. TTL is caller-declared (`ICacheableQuery.CacheTtl`) and must reach Redis (absolute expiry) — never
  downgrade it to app-only metadata. Config Service bundle publish is **phase 21** (reuses this service) — **not** here;
  no SignalR pub/sub, no advanced invalidation/warming, no leaderboard (phase 45).
- **Redis integration tests use Testcontainers Redis** (`redis:7-alpine`; require Docker — CI `ubuntu-latest` has it,
  local via `scripts/dev/up`); **never mock Redis** for the acceptance tests. Keys are per-test GUID-isolated.
- When changing caching, keep **`GameTeam.Infrastructure/Caching/*` + `Serialization/*` + `ICacheService` (port) +
  `GameTeam.Infrastructure.Tests/Caching` (behavior contract) + `AddInfrastructure` + `/health` + `docs/backend/infrastructure.md`
  §2.1 + `docs/backend/cross-cutting.md` §2.5/§4 + `.instructions/backend.md` + `.claude/agents/dotnet-backend.md` in sync**
  (doc-sync matrix, §5); the integration tests are the behavior contract — update them.

**API layer is standardized (Phase 13 — closed & verified).** `GameTeam.Api` is the HTTP gateway + composition root.
**API versioning** = `Asp.Versioning.Http` + `Asp.Versioning.Mvc.ApiExplorer` (8.1.1): `AddApiVersioning` (default
**v1**, `AssumeDefaultVersionWhenUnspecified`, `UrlSegmentApiVersionReader`, `ReportApiVersions`) + `AddApiExplorer`
(`GroupNameFormat="'v'VVV"`, `SubstituteApiVersionInUrl=true` ⇒ OpenAPI renders resolved `/api/v1/...`). New endpoints
map into the **version set**: `app.NewApiVersionSet().HasApiVersion(new ApiVersion(1))…Build()` +
`app.MapGroup("/api/v{version:apiVersion}").WithApiVersionSet(set)` + `.MapToApiVersion(1)`; `GameTeam.Contracts.Common.ApiVersions`
stays the version constant. **Error handling is centralized** in `GameTeam.Api/Http/`: **`ErrorHttpMapping`** (the ONE
`Error.Code`→HTTP-status table — `VALIDATION_FAILED`→400 + suffix convention `_NOT_FOUND`→404/`_CONFLICT`→409/
`_UNAUTHORIZED`|`UNAUTHENTICATED`→401/`_FORBIDDEN`→403, default 400), **`ApiResults`** (`Result`/`Result<T>`→HTTP; success→200,
failure→**`ErrorEnvelope`**; `traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier`), **`GlobalExceptionHandler`**
(`IExceptionHandler` + `app.UseExceptionHandler()`; unhandled→**500 `ErrorEnvelope`** code `INTERNAL_ERROR`, logs full
exception server-side only, **no stack/internal leak**). **500 uses `ErrorEnvelope`, NOT ProblemDetails** — ONE error
contract for every error (Phase-05 §3; `AddProblemDetails()` is only a framework fallback). Sample endpoints prove the
flow: `GET /api/v1/ping` (`PingCommand`) + `GET /api/v1/server-time` (`GetServerTimeQuery` + `IClock`), both HTTP →
`ISender` → Application → `Result` → `ApiResults`. **Swagger UI** = `Swashbuckle.AspNetCore.SwaggerUI` **UI-only, dev-only**
over the first-party `/openapi/v1.json` — **no SwaggerGen** (first-party OpenAPI stays the single source →
`shared/contracts/openapi.json` + drift guard + codegen). `/health` unchanged (not versioned). Verified: `dotnet build -c
Release` 0 warning/0 error, `dotnet test` **150 pass** (Api.IntegrationTests **31**: ping ±validation, server-time
deterministic, error-envelope shape, exception→500 no-leak, swagger json, + health/openapi-contract), runtime
`/api/v1/ping|server-time`, `/health`, `/openapi/v1.json`, `/swagger` OK, `api-supported-versions: 1`. Canonical:
`docs/backend/api-and-versioning.md` §3.1/§4.5; decision log: `.memory/0011-api-layer-standardized.md`.

- Future agents **MUST** map every new feature endpoint into the **versioned `/api/v1`** group (version set) and route it
  through **Application/MediatR** with a thin endpoint; **MUST reuse** `ErrorHttpMapping`/`ApiResults`/`GlobalExceptionHandler`
  + `ErrorEnvelope` + `Result`/`Error`. **MUST NOT** invent a second versioning convention, a second error shape/envelope,
  return raw exceptions or leak stack traces, scatter error mapping in endpoints, put business logic in endpoints, or add
  SwaggerGen / hand-edit `openapi.json`.
- **Contract/endpoint change workflow (binding):** edit `GameTeam.Contracts`/endpoint → rebuild (regenerate
  `openapi.json`) → `bash shared/codegen/run.sh` → commit generated diff → **add/extend an `Api.IntegrationTests` HTTP
  test** (status + contract + error envelope + versioned route) → doc-sync → phase checklist. Swagger/OpenAPI must stay in
  sync with the endpoint on every change.
- **Authentication is Phase 18.** Phase 13 only leaves a **hook** (TODO in the `Program.cs` pipeline + `AddApi`) — **MUST
  NOT** implement real JWT/login/refresh/authorization before Phase 18, fake a user, or register a real auth scheme.
- **`IConfigProvider`** has a minimal placeholder `DefaultConfigProvider` (Infrastructure, `config@v1`) so `CachingBehavior`
  works; **Phase 21** (Config Service) replaces it — do not build config-reading logic onto it.
- When changing the API layer, keep **`GameTeam.Api/Http/*` + `Program.cs` + `AddApi` + `Api.IntegrationTests` (HTTP
  contract) + `DefaultConfigProvider`/`AddInfrastructure` + regenerated `openapi.json` + `client/src/data/generated/**` +
  `docs/backend/api-and-versioning.md` §3.1/§4.5 + `.instructions/backend.md` + `.claude/agents/dotnet-backend.md` in sync**
  (doc-sync matrix, §5); the integration tests are the behavior contract — update them.

**Client core autoloads are standardized (Phase 14 — closed & verified).** The client's loose-coupling backbone has ONE
home: **`client/src/core/`** — two **independent, single-responsibility autoloads** (registered in `client/project.godot`
`[autoload]`; **no** God autoload, never merged into a `GameManager`/`CoreManager`). **`EventBus`**
(`core/events/event_bus.gd`, node `EventBus`): global pub/sub — `emit(event, payload)` / `subscribe(event, callback)` /
`unsubscribe` / `is_known`. The event catalogue is **closed**: a const **`EVENTS: Array[StringName]`** + one declared
**`signal <name>(payload)`** per event; `emit`/`subscribe` `assert` the event ∈ `EVENTS` ⇒ an unregistered event fails
fast (**anti-"God channel"/"event chui"**). Backing the catalogue with real Godot signals gives automatic disconnect when
a subscriber node frees (leak-safety) plus explicit `unsubscribe`. Convention: every event carries **one** `payload`
(Dictionary). Phase-14 base catalogue = **one** event **`scene_changed`** (`{to, from}`); feature/network events are added
by their owning phases. **`SceneRouter`** (`core/scene/scene_router.gd`, node `SceneRouter`): centralized navigation —
`goto_scene(path)` (push) / `back()` (pop) / `stack_depth()` / `clear_history()` + `current_path`/`current_scene`. It uses
a **manual scene-host** (holds the current screen as a child, swaps in place, **`queue_free`s the old scene** ⇒ no leak /
no stale reference — ADR-009); **transition is instant** ("tối giản", advanced/animated transitions deferred to the UI
phase); a bad path ⇒ `false` + `push_error` (never throws, never swallows). After a swap it publishes `scene_changed`
via `EventBus` so features react **without importing** `SceneRouter`. Autoload scripts **omit `class_name`** (it collides
with the singleton name — Godot "hides an autoload singleton") and are accessed via the global (`EventBus.emit(...)`).
Verified (Godot 4.7.1-stable, Windows, local): `--headless --import` exit 0 (0 error/0 warning, autoloads created);
gdUnit4 headless **11/11 pass, 0 orphan** (`client/tests/core/event_bus_test.gd` 5 + `scene_router_test.gd` 4 + smoke 2).
Canonical: `docs/godot/state-and-signals.md` §3.1 (event catalogue) + `docs/godot/scene-architecture.md` §4.1 (router);
decision log: `.memory/0012-client-autoloads-standardized.md`.

- Future agents **MUST reuse** `EventBus`/`SceneRouter` before adding any cross-feature communication or navigation;
  **MUST NOT** re-declare a second bus/router, merge them into a God autoload, let a feature import another feature, or
  scatter `get_tree().change_scene*` in features (navigate only via `SceneRouter`).
- **The event catalogue is a binding contract** (ADR-002): every event used in code **must** appear in `EVENTS` + a
  declared `signal` + the `docs/godot/state-and-signals.md` §3.1 table — **no undocumented/"chui" events, no "God
  channel"**. Event names are `snake_case` **past-tense** (`../conventions/naming.md` §6). If an event is only used
  inside one feature, use a **plain signal**, not the EventBus. Only seed base events the phase's own code emits — never
  future-phase feature events.
- **SceneRouter frees old scenes** (`queue_free`) and never retains them; advanced/animated transitions and
  `replace`/async-load/deep-link are **future (UI phase)** — do not build a transition framework here.
- When changing a client core autoload, keep **`client/src/core/{events,scene}/*` + `client/tests/core/*` (behavior
  contract) + `client/project.godot` (`[autoload]`) + `docs/godot/state-and-signals.md` §3.1 + `docs/godot/scene-architecture.md`
  §4.1 + `.instructions/client.md` + `.claude/agents/godot-client.md` in sync** (doc-sync matrix, §5); the gdUnit4 tests
  are the behavior contract — update them.

**Client NetworkClient is standardized (Phase 15 — closed & verified).** The client's server-communication has ONE
gateway: **`client/src/core/net/`** — the autoload **`NetworkClient`** (`network_client.gd`, registered in
`client/project.godot` `[autoload]`; **omits `class_name`** — singleton-name collision — accessed via the global). It is
the **single channel** client → server: **UI/feature MUST NOT call `HTTPRequest` / REST directly** — `HTTPRequest` lives
**only** in `core/net/` (grep guard; ADR-002 "UI không gọi network"). Public coroutines **`get_json(path, parser)`** /
**`post_json(path, body, parser)`**; base URL from env **`GAME_TEAM_API_BASE_URL`** (default `http://localhost:8080`),
paths under **`/api/v1`** (ADR-008); header **`Authorization: Bearer <jwt>`** attached only when the token store holds one
— **never logged**. Supporting types (non-autoload, with `class_name`): **`HttpTransport`**/**`GodotHttpTransport`** (the
transport seam — the ONE place `HTTPRequest` is touched; lets tests inject a fake), **`TokenStore`** (in-memory JWT stub —
real login/refresh = phase 18/20), **`NetResult`** (normalized result: `ok`/`value`/`error`/`status_code`/`kind`),
**`NetworkResponseParser`** (JSON→**generated model** phase 08; generated DTOs are DO-NOT-EDIT / no `from_dict` ⇒ add a
parse func here — never hand-declare a DTO). Failures normalize `ErrorResponse` (`{code, message, traceId}`) and emit
**`network_error`** (one consistent channel); **401 also emits `unauthorized`** (both added to `EventBus.EVENTS` + signals
+ §3.1 catalogue). Timeout per request (`request_timeout_seconds`, default 10s); retry **only GET/idempotent-safe** on
transient transport failure (max `MAX_GET_RETRIES=2`); **POST is never auto-retried** (double-effect; `Idempotency-Key` =
server phase 31). **User decisions:** base URL = env-var-with-default (not project setting / `.tres` — that overlaps phase
16); JSON→DTO = explicit parser funcs (not a generic reflection mapper). Verified (Godot 4.7.1-stable, Windows, local):
`--headless --import` exit 0 (0 warning, `NetworkClient` created); gdUnit4 headless **21/21 pass, 0 orphan**
(`client/tests/core/net/network_client_test.gd` 10 + Phase-14 11); real-server end-to-end (local .NET API on `:5080`,
Redis down→graceful) GET `/api/v1/server-time` → parsed `ServerTimeResponse.utc_now` (temp test, removed); grep guard
green (`HTTPRequest` only in `core/net/`; no token/Authorization logging); no `client/src/data/generated` drift.
Canonical: `docs/godot/state-and-signals.md` §4 + §3.1; decision log: `.memory/0013-client-networkclient-standardized.md`.

- Future agents **MUST reuse** `NetworkClient` for every server call before adding any HTTP; **MUST NOT** create a second
  HTTP client, call `HTTPRequest`/REST from UI/features, bypass `NetworkClient`, hand-declare a client DTO, or hand-edit
  `client/src/data/generated`. New endpoint consumption = add a parse func in `core/net/response_parser.gd` + call
  `get_json`/`post_json` from a feature (never from UI directly).
- **Security is binding:** **never log the token/Authorization header or a sensitive body**; **never hardcode a token**;
  the JWT comes from `TokenStore` (real acquisition/refresh = phase 18/20 — do not implement real login/refresh here).
- **Authority is binding (ADR-008/011):** the client **never** fabricates a result/reward on network loss/timeout —
  report a failed `NetResult` + `network_error`. **Retry GET only**; never auto-retry POST/side-effecting requests.
- **Out of scope (do not pull in):** real login/refresh + token persistence (18/20), advanced offline queue (20/48),
  `Idempotency-Key` POST (server 31), SignalR (Post-MVP), config bundle caching (16), retry backoff.
- When changing the network transport, keep **`client/src/core/net/*` + `client/tests/core/net/*` (behavior contract) +
  `client/project.godot` + any new EventBus event (`EVENTS`+signal+§3.1) + `docs/godot/state-and-signals.md` §4 +
  `docs/godot/ui-architecture.md` §1 + `client/src/core/net/README.md` + `.instructions/client.md` +
  `.claude/agents/godot-client.md` in sync** (doc-sync matrix, §5); the gdUnit4 tests are the behavior contract — update them.

**Client ConfigProvider + StateCache are standardized (Phase 16 — closed & verified).** The client's data-reading
backbone has two independent single-responsibility autoloads under **`client/src/core/`** (registered in
`client/project.godot` `[autoload]` after `NetworkClient`; both **omit `class_name`** — singleton-name collision —
accessed via the global). **`ConfigProvider`** (`core/config/config_provider.gd`): the **single config read gate** —
`apply_bundle(bundle)` receives a versioned config-bundle envelope (`config-bundle.schema.json`: `config_version`
"config@vN" + `schema_version` + `data` per-type), caches it **immutably** to disk (`user://config_cache/config@vN.json`,
**write-once — never overwrites an old version**) with an `active.json` pointer, loads the active bundle on `_ready`
(offline-view; missing/corrupt ⇒ empty, no crash), and serves **data-driven** queries `get_entry(type,id)`/`get_all(type)`/
`get_hero(id)`/`current_version()`/`config_label()`/`has_config()` (read by schema phase 06, **no hardcoded gameplay
numbers**; reads return **deep copies**). `check_for_update()` (coroutine) asks the server version via `NetworkClient` and,
if newer, downloads+applies the bundle — placeholder endpoints `/api/v1/config/...` (Config Service = phase 21, e2e bundle
= phase 22); offline ⇒ keep cache, never fabricate. Emits **`config_updated`** (`{version, config_version}`) only when the
active version **changes** (re-applying the active version = no-op ⇒ `config@vN` immutability). **`StateCache`**
(`core/state/state_cache.gd`): a **read-only display cache** of player state (`const IS_DISPLAY_ONLY = true`) — the **only**
write path is `apply_snapshot(snapshot)` reflecting a **server response** (whole-snapshot replace; **no authoritative
mutator** — no `add_currency`/`spend_currency`/`set_progress`…); reads `get_currency`/`get_currencies`/`get_heroes`/
`get_hero`/`get_progress`/`get_all_progress`/`get_profile` return **deep copies**; `source()`=`"empty"｜"server"｜"cache"`
+ `is_offline()` label; persists the last snapshot to `user://state_cache/snapshot.json` and boots it as `"cache"`
(offline-view) until a server refresh flips it to `"server"`; emits **`state_refreshed`** (`{source}`). Both new events are
in `EventBus.EVENTS` + declared signals + `docs/godot/state-and-signals.md` §3.1. Verified (Godot 4.7.1-stable, Windows,
local): `--headless --import` exit 0 (0 warning, autoloads created); gdUnit4 headless **full suite 38/38 pass, 0 error/0
failure/0 orphan** (`tests/core/config/config_provider_test.gd` 11 + `tests/core/state/state_cache_test.gd` 6 + Phase-14/15
regression 21); authority sweep clean (no currency/reward math, `HTTPRequest` only in `core/net/`, no duplicate EventBus);
no `client/src/data/generated` drift (no contract change). Canonical: `docs/godot/resources-and-assets.md` §1.1
(ConfigProvider) + `docs/godot/state-and-signals.md` §1.1 (StateCache) + §3.1; decision log:
`.memory/0014-client-configprovider-statecache-standardized.md`.

- Future agents **MUST reuse** `ConfigProvider` for all config reads and `StateCache` for all cached player-state reads
  before adding any config/state plumbing; **MUST NOT** load a raw config bundle in a feature, hand-hardcode gameplay
  numbers, create a second config/state cache, or use `StateCache` as a database/source of truth.
- **`config@vN` is immutable (ADR-005):** never overwrite an existing version's on-disk cache; a new config version = a new
  cache file + `config_updated`. Config changes must **not** require a client rebuild.
- **Client is not authority (ADR-007/011):** every authoritative mutation goes `Feature/UI → NetworkClient → Server →
  response → StateCache.apply_snapshot`. The client **never** computes currency/reward/battle-result/authoritative
  progress/inventory/stats. Cached/offline data is **display-only** — never the basis for an authoritative action.
- **Reuse the one EventBus:** `config_updated`/`state_refreshed` follow the §3.1 4-step process (name + signal + `EVENTS`
  + catalogue row). No second event bus. Do **not** pull in phase-22 e2e bundle, signed bundles, or LiveOps here.
- When changing these autoloads, keep **`client/src/core/{config,state}/*` + `client/tests/core/{config,state}/*` (behavior
  contract) + `client/project.godot` `[autoload]` + `EventBus.EVENTS`/signals + `docs/godot/state-and-signals.md` §1.1/§3.1
  + `docs/godot/resources-and-assets.md` §1.1 + `docs/gameplay/configuration-and-data.md` §4 + `.instructions/client.md` +
  `.claude/agents/godot-client.md` in sync** (doc-sync matrix, §5); the gdUnit4 tests are the behavior contract — update them.

**Client boot flow + UI base are standardized (Phase 17 — closed & verified). CLOSES Group 3 (Client Core Framework).**
The client's first runnable slice + the UI foundation for every feature live under **`client/src/ui/`** (boot is a
**scene**, NOT an autoload — no new autoload was added). **App-shell:** `run/main_scene = res://src/ui/app_root.tscn`
(`AppRoot`, an empty `Control`) → `_ready` routes to boot via `SceneRouter` ⇒ **SceneRouter owns every visible screen from
frame one** (boot → hub, swap-in-place + `queue_free` the old scene; no overlap, no stale ref — ADR-009). **Boot**
(`src/ui/boot/boot_controller.gd` = the **presenter**, root of `boot.tscn`, builds `BootView` + `BootErrorView` in code):
**(1)** `NetworkClient.get_json("/health", NetworkResponseParser.parse_health)` = the **hard reachability gate** (transport
failure / non-2xx → `BootErrorView` + retry); **(2)** `ConfigProvider.check_for_update()` = **best-effort** — the real
Config Service is **phase 21**, so a missing/failing config endpoint today is expected (keep cache, **never blocks boot**;
tighten the config gate when phase 21/22 lands); **(3)** `SceneRouter.goto(main_hub)` + `clear_history()`, emit
`boot_succeeded`/`boot_failed`. **UI base** (`src/ui/base/base_view.gd`, `class_name BaseView extends Control`): a one-way
contract — **data-in** (`set_data`→virtual `_render`) → **intent-out** (`emit_intent`→signal `intent(name, payload)`) +
`bind`/`unbind` lifecycle hooks (called at `_enter_tree`/`_exit_tree`). **A VIEW is network-free** — it MUST NOT reference
`NetworkClient`/`HTTPRequest`/`core/net` (grep guard over `src/ui/**`); the **presenter** (`BootController` /
`MainHubPresenter`) is the ONLY touchpoint: it reads `StateCache`/`ConfigProvider` (display-only), calls `NetworkClient`
via the gateway (never raw `HTTPRequest`), navigates via `SceneRouter`, and emits EventBus **only** for genuine global
events. **User decisions:** intent = a **local `intent` signal translated by the presenter** (NOT a per-button EventBus
event ⇒ the EventBus catalogue stays CLOSED — **no new EventBus event added in Phase 17**, §3.1); app-shell `AppRoot`
(NOT boot-as-main-scene self-freeing). **Main hub** = a navigation/composition **shell** (`MainHubView` + `MainHubPresenter`
reading config label / offline flag; 4 placeholder buttons emit intents) — **no feature business logic** (real features =
later phases). **Net:** added `NetworkResponseParser.parse_health` (reuses the generated `HealthResponse`; missing `status`
⇒ null) — no contract change ⇒ **no `client/src/data/generated` drift**. Verified (Godot 4.7.1-stable local): `--headless
--import` exit 0 (0 error/0 warning; `BaseView`/`BootController`/`BootErrorView`/`BootView`/`MainHubView`/`MainHubPresenter`
registered); gdUnit4 **full suite 48/48 pass, 0 orphan** (`tests/ui/base` 3 + `tests/ui/boot` 5 + `parse_health` 2 +
14/15/16 regression); headless smoke of the real main scene (server down → boot→health-fail→error, exit 0, no crash) +
`main_hub.tscn` (exit 0); grep guard green (`HTTPRequest`/`core/net` absent from `src/ui` code; `NetworkClient` used only in
the presenter `boot_controller.gd`). Canonical: `docs/godot/ui-architecture.md` §2.1/§4.1 +
`docs/godot/scene-architecture.md` §4.2/§5; setup/run: root `setup-and-run.md`; decision log:
`.memory/0015-client-boot-ui-standardized.md`.

- Future agents **MUST reuse** `BaseView` + the boot flow for any new screen; **MUST NOT** let a **view** call the network
  (`NetworkClient`/`HTTPRequest`/`core/net`) — data-in / intent-out only, the **presenter** does network/navigation; **MUST
  NOT** make boot a self-freeing main scene (use the `AppRoot` → SceneRouter shell), **MUST NOT** add a per-UI-action
  EventBus event (the catalogue is CLOSED), and **MUST NOT** put feature business logic in the hub (placeholder intents
  only until the owning feature phase).
- **Boot gating is binding:** `/health` is the **hard** reachability gate (fail → safe error + retry, **no stack/internal
  leak** — client is not authority); config load is **best-effort** until the Config Service (phase 21) exists — never make
  a missing config endpoint block boot, and never fabricate config/state on failure (ADR-005/007/011). Auth (guest login)
  is injected into boot at **phase 20**, not here. `AudioManager` stays **deferred** (not in the Phase 17 contract).
- New endpoint consumption = **add a parse func in `core/net/response_parser.gd`** + call `get_json`/`post_json` from a
  **presenter** (never from a view); never hand-declare a client DTO or hand-edit `client/src/data/generated`.
- When changing the boot/UI base, keep **`client/src/ui/*` + `client/tests/ui/*` (behavior contract) + `client/project.godot`
  (`run/main_scene`) + `docs/godot/ui-architecture.md` §2.1/§4.1 + `docs/godot/scene-architecture.md` §4.2/§5 +
  `docs/godot/state-and-signals.md` §4 + `.instructions/client.md` + `.claude/agents/godot-client.md` + root `setup-and-run.md`
  in sync** (doc-sync matrix, §5); the gdUnit4 tests are the behavior contract — update them.

**Auth (guest JWT) is standardized (Phase 18 — closed & verified).** Guest authentication + **default authorization** is the
security gate for every business API (ADR-008). Identity home: **`GameTeam.Domain/Accounts/`** — **`Account`**
(`AggregateRoot<Guid>`, `AccountType` = `None=0`/`Guest=1` — a **Domain** enum, not a wire contract; `CreatedAt` from `IClock`;
factory `CreateGuest` raises `AccountCreated`) is the **identity boundary** for server-authoritative state; provider linking
(Google/Apple/email) is **Post-MVP** (a future `account_providers` table — never add provider columns now). **Guest login** =
`POST /api/v1/auth/guest` (mapped into the **version set**, `.AllowAnonymous`) → **`CreateGuestAccountCommand`** (thin handler,
`ITransactionalRequest`) → JWT via the Application port **`ITokenService`** (`TokenBundle`). **JWT lives ONLY in Infrastructure**
(`GameTeam.Infrastructure/Auth/`: **`JwtTokenService`** HS256 — claims `sub`=account id/`type`=guest/`jti`/`iat`/`nbf`/`exp`/`iss`/
`aud`, time from `IClock`; **`JwtOptions`** Options-pattern) — Application depends only on `ITokenService`, **never** a JWT
framework (NetArchTest `Application_should_not_depend_on_jwt_or_authentication_frameworks` gates it). `Account` persists via the
Phase-11 stack (`AccountConfiguration` → table **`accounts`** snake_case, migration **`AddAccounts`**; `AccountCreated` dispatch at
`SaveChanges`). **API:** `AddApi` registers `AddAuthentication(JwtBearer).AddJwtBearer(...)` (validate signature+issuer+audience+
lifetime from `IOptions<JwtOptions>`) + `AddAuthorization` with **`FallbackPolicy = RequireAuthenticatedUser`** ⇒ **business
endpoints are authenticated by default**; `Program.cs` turns on `UseAuthentication()/UseAuthorization()`. **Public endpoints are
explicitly whitelisted** with `.AllowAnonymous()`: `/health`, `POST /api/v1/auth/guest`, `/openapi/*`, `/swagger` (`/ping`+
`/server-time` are now protected). Auth failures return the standard **`ErrorEnvelope`** (401 `UNAUTHENTICATED` / 403 `FORBIDDEN`
via `AuthProblem` + `ErrorHttpMapping`). **Signing key** comes from secret/env **`Jwt__SigningKey`** (fail-fast; appsettings holds
only issuer/audience/expiry) — **never hardcode/commit/log** it. Verified: build Release 0/0, `dotnet test` **167 pass**
(Api.Integration 36 incl. `AuthGuestEndpointTests` A–D; Infrastructure 26 incl. Testcontainers `AccountPersistenceTests`), migration
up/down, real-runtime guest-login→JWT (`sub`==DB row)→protected 200 / no-token 401 / tampered 401, negative-authz test red→revert,
secret scan clean. Canonical: `docs/backend/api-and-versioning.md` §4.5 + `docs/backend/infrastructure.md` §2.5 +
`docs/backend/cross-cutting.md` §1; decision log: `.memory/0016-auth-jwt-guest-standardized.md`.

- Future agents **MUST reuse** this auth infra: new business endpoints inherit the default-auth policy (add `.AllowAnonymous()`
  only for genuinely public endpoints); guest login / `Account` / `ITokenService` / `JwtTokenService` / `JwtOptions` are the ONE
  auth mechanism. **MUST NOT** build a second auth/token mechanism, add a JWT/authentication framework to Application/Domain, fake
  a user, bypass authorization for convenience, or implement real provider login before its Post-MVP phase.
- **Secret handling is binding:** the JWT signing key is read from `Jwt__SigningKey` (secret/env) via `IOptions<JwtOptions>` and is
  **never** hardcoded, committed (no key in `appsettings*.json`), or logged. Test/dev keys are obvious non-production placeholders.
- **Authority is binding (ADR-007/011):** `Account`/`sub` is the identity for server-authoritative state; resource-ownership checks
  live in feature handlers (phase 19+). Provider linking, refresh-token rotation/revocation, and rate-limiting are **Post-MVP** —
  the refresh token issued today is an opaque **foundation** value only.
- When changing auth, keep **`GameTeam.Domain/Accounts/*` + `ITokenService` + `Features/Auth/*` + `GameTeam.Infrastructure/Auth/*`
  + `AccountConfiguration`/migration + `AddApi`/`Program.cs`/`AuthProblem` + the auth tests (behavior contract, incl. the arch fact)
  + `docs/backend/api-and-versioning.md` §4.5 + `docs/backend/infrastructure.md` §2.5 + `docs/backend/cross-cutting.md` §1 +
  `docs/mvp/10-open-questions.md` (BE3) + `.instructions/backend.md` + `.claude/agents/dotnet-backend.md` in sync** (doc-sync
  matrix, §5); the auth tests are the behavior contract — update them.

**Profile persistence is standardized (Phase 19 — closed & verified).** The **server-authoritative save root** (ADR-007) has ONE
home: **`GameTeam.Domain/Profiles/`** — **`PlayerProfile : AggregateRoot<Guid>`** (own `Id` + `AccountId` **1-1 with `Account`**,
`DisplayName`/`Level` = the Phase-05 `ProfileDto` contract fields defaulted, `SchemaVersion`, `CreatedAt`/`UpdatedAt` from `IClock`).
It is the **root every future game-state feature extends** (currency 31, hero 27/35, inventory 32, progress 34 — add tables/refs +
**bump `SchemaVersion`**); Phase 19 adds **no business state**. Factory **`CreateForAccount`** raises **`PlayerProfileCreated`**;
**`Restore`** rehydrates any stored version without an event. **Schema versioning (ADR-007):** `const CurrentSchemaVersion = 1`;
**`Upgrade(nowUtc)`** migrates persisted **data** `v(N)→v(N+1)` (read-repair, deterministic, sample `MigrateV0ToV1` back-fills
`DisplayName` and **preserves `Level`**) — this is the profile **data** migration, distinct from EF Core DDL migrations. **Application**
(`GameTeam.Application/Features/Profile/`): **`GetOrCreateProfileCommand`** (`ITransactionalRequest`, backs `GET /api/v1/profile` —
get-or-create + read-repair `Upgrade`, atomic) + **`GetMyProfileQuery`** (pure read → `PROFILE_NOT_FOUND`); owner resolved ONLY from
the token `sub` via the new port **`ICurrentUser`** (`Abstractions/Security/`; adapter **`GameTeam.Api/Auth/CurrentUser.cs`** over
`IHttpContextAccessor`, registered in `AddApi` + `AddHttpContextAccessor`). **Repository** port **`IPlayerProfileRepository`**
(`GetByAccountIdAsync`, no `IQueryable` leak) → EF **`PlayerProfileRepository`**. **Infrastructure:** table **`player_profiles`**
(`snake_case`; **unique index `account_id`** + FK→`accounts` cascade = DB-level idempotency), migration **`AddPlayerProfiles`**,
`DbSet` on `AppDbContext`. **Eager, atomic creation:** `CreateGuestAccountCommandHandler` now creates the `PlayerProfile` in the SAME
transaction as the `Account` ⇒ guest login → profile. **Endpoint moved** from the Phase-05 literal-`/api/v1` 501 stub into the `apiV1`
**version set** (`.MapToApiVersion(1)`, protected by the default policy — NO `.AllowAnonymous()`). Verified: `dotnet build` 0/0,
Domain.Tests + Application.Tests (incl. architecture facts Application ⊥ Infra/EF/JWT) green; Testcontainers Postgres persist/read +
**unique-constraint** + event dispatch + **migrate-v0→current preserving data**; Api.IntegrationTests (Testcontainers) login→`GET /profile`
200 owner-correct + retry→1 row + cross-owner isolation + no-token 401; migration `has-pending-model-changes` clean; no `openapi.json`
shape drift (path reorder only) / no `client/src/data/generated` drift. Canonical: `docs/backend/domain-and-application.md` (profile
aggregate + versioning + ownership) + `docs/backend/infrastructure.md` §1.2; ADR-007 → Implementation; decision log:
`.memory/0017-profile-persistence-standardized.md`.

- Future agents **MUST reuse** `PlayerProfile`/`IPlayerProfileRepository`/`ICurrentUser`/`GetOrCreateProfileCommand` before adding any
  player-state persistence; **MUST NOT** create a second save root, read the owner from a client-supplied id (body/route/query — IDOR),
  build a client-authoritative profile, or add a second current-user/profile mechanism.
- **Profile is the state root:** future feature state **extends** `PlayerProfile` (new table/ref) — never a parallel root. Any change to
  the profile schema **MUST** bump `SchemaVersion` + add a `MigrateV{n}ToV{n+1}` step + a **data-preservation** migration test + an EF
  migration + doc-sync, in the same change (never a bare version int without a migration).
- **Idempotency is DB-level:** one profile per account is guaranteed by the **unique `account_id` index**, not a check-then-insert. All
  profile mutation goes through a **server command** (server-authoritative — `schema_version`/`account_id`/owner/timestamps are
  server-controlled). Provider linking, PUT/arbitrary update, and refresh-token rotation stay out of scope (Post-MVP / their phases).
- When changing profile, keep **`GameTeam.Domain/Profiles/*` + `Features/Profile/*` + `ICurrentUser`/`IPlayerProfileRepository` +
  `GameTeam.Api/Auth/CurrentUser.cs` + `PlayerProfileConfiguration`/migration + `AppDbContext` + `CreateGuestAccountCommandHandler` +
  `Program.cs` (`/profile` in the version set) + the profile tests (Domain/Application/Infrastructure/Api — behavior contract) +
  `docs/backend/domain-and-application.md` + `docs/backend/infrastructure.md` §1.2 + ADR-007 (Implementation) + `.instructions/backend.md`
  + `.claude/agents/dotnet-backend.md` in sync** (doc-sync matrix, §5); the profile tests are the behavior contract — update them.

**Client auth+profile integration is standardized (Phase 20 — closed & verified). CLOSES the client side of the
auth/save loop.** The client identity+profile flow has ONE home: **`client/src/ui/boot/auth_profile_flow.gd`**
(`AuthProfileFlow`, RefCounted — **NOT** an autoload, no God manager) orchestrating **guest login → JWT → GET
/profile → StateCache → hub** (ADR-007/008/011). The **auth lifecycle is CENTRALIZED in boot + AuthProfileFlow**;
`NetworkClient` only attaches the token + emits `unauthorized`; **UI/views never contain auth logic**. Token
persistence lives in the extended **`client/src/core/net/token_store.gd`** (`TokenStore`): stores access+refresh
token + expiry, persisted **encrypted** via `FileAccess.open_encrypted_with_pass` to **`user://auth/token.dat`**
(key = app salt + `OS.get_unique_id()`, device-bound) — **never plaintext, never logged, never committed**
(`user://` is git-ignored); `NetworkClient._ready()` calls `token_store.load()`. Not OS-keychain-grade (vanilla
Godot has none — needs a native plugin, out of scope). **Boot flow** (`boot_controller.gd`, new `State.AUTHENTICATING`):
`health` → `AuthProfileFlow.run()` (reuse token if present+not-expired, else `POST /api/v1/auth/guest`; then
`GET /api/v1/profile` → `StateCache.apply_snapshot`) → config (best-effort) → hub. **401/expiry → bounded re-login**
(`MAX_RELOGIN=1`, inspects `NetResult.kind == UNAUTHORIZED` inline) — **no infinite loop**; the global `unauthorized`
EventBus event still fires. **Offline** = health/auth fail **but** a cached profile exists (`StateCache` boots
`source="cache"`) ⇒ boot enters the hub in **offline mode** (`[offline]` label), **never fabricating data**; error
screen only when no usable cache. Two new parsers (`parse_auth_guest_response`/`parse_profile`) map to the existing
generated `AuthGuestResponse`/`ProfileDto` (**no contract change → no `client/src/data/generated` drift**). Hub
(`main_hub_presenter.gd`/`main_hub_view.gd`) displays server profile **name·level** (currency = **placeholder** —
`ProfileDto` carries no currency until phase 31) + offline label; refreshes on `state_refreshed`. **No new EventBus
event** — the catalogue stays CLOSED (5), reusing `unauthorized` + `state_refreshed`. Verified (Godot 4.7.1-stable
local): `--headless --import` exit 0 (0 error/0 warning); gdUnit4 **65/65 pass, 0 orphan** (new: `token_store_test`
4 + `auth_profile_flow_test` 6 + `main_hub_presenter_test` 3 + boot +5; 14–17 regression green); grep guard clean
(`HTTPRequest` only in `core/net/`, no token/passphrase logging, no authority math). Canonical:
`docs/godot/state-and-signals.md` §4.1/§3.1 + `docs/godot/ui-architecture.md` §4.1; decision log:
`.memory/0018-client-auth-profile-standardized.md`.

- Future agents **MUST reuse** `AuthProfileFlow` (boot auth orchestration), `TokenStore` (secure token store),
  `NetworkClient` (token attach + `unauthorized`), `StateCache` (read-cache), and the generated `ProfileDto`/
  `AuthGuestResponse` before adding any auth/profile plumbing. **MUST NOT** create a second AuthManager/ProfileManager/
  token store/HTTP client/profile DTO, put auth logic in UI/views, call an auth endpoint from a view, bypass
  `StateCache`, add a refresh-token architecture beyond scope, or add a per-action EventBus event.
- **Security is binding:** token persisted **encrypted only** (never plaintext), **never logged** (no token/
  Authorization/passphrase in `print`/`push_*`), **never hardcoded/committed**; tests use `fake-*` values only.
- **Authority is binding (ADR-007/011):** client is display-only; profile/currency/state come from the server via
  `StateCache.apply_snapshot`; offline shows **cached data with an explicit label** — never fabricated as fresh.
  **401 re-login is bounded** (no infinite loop); on unrecoverable failure report an error, don't fake a profile.
- **Out of scope (do not pull in):** refresh-token rotation endpoint (refresh token stored but not yet exchanged),
  account/provider linking (Post-MVP), currency/wallet (phase 31), config bundle e2e (phase 22), OS keychain.
- When changing client auth/profile, keep **`client/src/core/net/token_store.gd` + `client/src/ui/boot/auth_profile_flow.gd`
  + `boot_controller.gd` + `main_hub_{presenter,view}.gd` + `response_parser.gd` + the gdUnit4 tests (behavior contract) +
  `docs/godot/state-and-signals.md` §4.1/§3.1 + `docs/godot/ui-architecture.md` §4.1 + `.instructions/client.md` +
  `.claude/agents/godot-client.md` in sync** (doc-sync matrix, §5); the gdUnit4 tests are the behavior contract — update them.

**Configuration Service is standardized (Phase 21 — closed & verified). CLOSES Core Framework (P1).** The backend runtime
**SSOT for config** lives in **`GameTeam.Infrastructure/Configuration/`** (ADR-004/005). Pipeline:
`config/ → validate → build immutable bundle (config@vN) → persist (DB) + cache (Redis) → flip "current" atomically →
serve via IConfigProvider`. Domain/Application read config **ONLY** through the `IConfigProvider` port — **never the
filesystem** (grep guard: no `File.`/`Directory.`/`Path.` in Domain/Application). Components: **`RuntimeConfigProvider`**
(replaces the Phase-13 `DefaultConfigProvider`; holds an immutable in-memory `ConfigSnapshot` swapped atomically —
`CurrentVersion` + `Get<T>(type,id)` + `GetIds(type)`, synchronous/no-I/O); **`ConfigBundleBuilder`** (groups config into
`data{type:{id}}`, **deterministic SHA-256 checksum** over canonical `{schema_version,data}` — order-independent, excludes
`generated_at`); **`ConfigBundlePublisher`** (validate via the **reused Phase-07 `ConfigValidationRunner`/`ConfigLoader`**
core lib — one validation source, ProjectReference; fail ⇒ NO publish, current unchanged; **checksum dedup** ⇒ identical
config never bumps; new version = current+1); **`ConfigBundleStore`** (DB `config_bundles` + singleton pointer
`config_current`; `SaveAndPublishAsync` inserts bundle + flips pointer in **one transaction**, warms Redis after commit;
`GetByVersionAsync` Redis→DB fallback; reuses the Phase-12 `ICacheService`, key `config-bundle:config@vN`, long TTL);
**`ConfigPublishHostedService`** (`IHostedService` = **deploy-time publish MVP**, runs once at boot, **best-effort/graceful
degradation** — never crashes the host). Persistence: tables **`config_bundles`** (immutable rows: `version` PK,
`config_version` unique, `schema_version`, `checksum`, `generated_at`, `payload` = verbatim envelope) + **`config_current`**
(singleton pointer, no seed), migration `AddConfigBundles`. **Endpoints** (version set, `.AllowAnonymous` — bundle is
non-sensitive shared content): `GET /api/v1/config/current` (→ `ConfigBundleDto`) + `GET /api/v1/config/bundle?bundleVersion=N`
(verbatim payload; missing ⇒ current; unknown ⇒ 404 `ErrorEnvelope` `CONFIG_BUNDLE_NOT_FOUND`; query param is
`bundleVersion` — NOT `version` — to avoid the `{version:apiVersion}` token collision). Verified (Docker Desktop 28.5.1):
build Release 0/0, `dotnet test` **203 pass** (Infrastructure.Tests 41 incl. 5 Testcontainers Config Service integration +
6 checksum unit; Api.IntegrationTests 45 incl. 4 config endpoint), `has-pending-model-changes` clean, migration up/down
green, config-validator exit 0, no `openapi.json` shape drift beyond the new paths, no generated-client drift. Canonical:
`docs/backend/infrastructure.md` §3.1 + `docs/backend/api-and-versioning.md` §4.5 + `docs/liveops/remote-config.md` §4.1 +
`docs/gameplay/configuration-and-data.md` §5; decision log: `.memory/0019-config-service-standardized.md`.

- Future agents **MUST reuse** `IConfigProvider`/`RuntimeConfigProvider`/`ConfigBundlePublisher`/`ConfigBundleStore`/
  `ConfigBundleBuilder` before adding any config read/publish; **MUST NOT** create a second config provider/publisher/bundle
  store, read config from the filesystem in Domain/Application, bypass the port, or **fork a second validator** (the Config
  Service reuses the Phase-07 core lib — validate is one source of truth).
- **Immutability & atomicity are binding (ADR-005):** a bundle version is **never mutated** (a config change publishes a NEW
  version); the "current" pointer flips **last**, inside the persist transaction, so it only ever names a fully-built bundle.
  Each `config@vN` is cached under its **own immutable Redis key** (never overwritten). Keep old versions (rollback foundation).
- **Validator-fail MUST block publish** (current unchanged; invalid bundle never persisted/cached/served). **Config change ⇒
  new version served with NO client rebuild.** Dedup by checksum (unchanged config = no version bump; idempotent redeploy).
- **Out of scope (leave as placeholders):** client bundle e2e/caching = **phase 22** (client `ConfigProvider` already exists,
  phase 16); typed gameplay config POCOs (hero/skill) = **phases 27+** (provider stays generic `Get<T>`); live bundle swap
  without deploy = **Post-MVP**; feature flags / A-B = **phase 49**; admin publish/authz, signed bundles, delta download,
  rollback workflow = **Post-MVP**.
- When changing the Config Service, keep **`GameTeam.Infrastructure/Configuration/*` + `Persistence/ConfigBundleRecord`/
  `ConfigCurrentPointer` + configs + migration + `IConfigProvider` (port) + `AddInfrastructure` + `Program.cs` (config
  endpoints in the version set) + the tests (`ConfigBundleBuilderTests`, `ConfigServiceIntegrationTests`, `ConfigEndpointTests`
  — behavior contract) + regenerated `openapi.json` + `docs/backend/infrastructure.md` §3.1 + `docs/backend/api-and-versioning.md`
  + `docs/liveops/remote-config.md` §4.1 + `docs/gameplay/configuration-and-data.md` §5 + `.instructions/backend.md` +
  `.instructions/config.md` + `.claude/agents/dotnet-backend.md` in sync** (doc-sync matrix, §5); the integration tests are the
  behavior contract — update them.

**Client config bundle e2e is standardized (Phase 22 — closed & verified). CLOSES P1 (data-driven end-to-end).** The
data-driven loop now runs from the Phase-21 Configuration Service to config-driven client UI. Home: the existing Phase-16
autoload **`ConfigProvider`** (`client/src/core/config/config_provider.gd`) — extended, not replaced — plus a sample screen
**`client/src/ui/hero_list/`**. **Boot config check (`check_for_update()`):** `GET /api/v1/config/current` (ConfigBundleDto
→ server version N) → compare with the immutable disk cache → if newer, `GET /api/v1/config/bundle?bundleVersion=N` →
`apply_bundle` (validate envelope + write-once `config@vN` disk cache + emit `config_updated`) → features query via
`ConfigProvider.get_all(&"hero")`. It returns a **status dict** `{updated, used_fallback, error_code, has_config}`.
**Two integration fixes were required (the real Phase-22 work):** (1) the server serves bundle `data` as a **map by id**
(`data.{type}.{id}=entry`), not an array — `_build_index` now accepts **both** map (server) and array (old fixtures),
indexing by `entry.id`; (2) the real endpoints/param are `/config/current` + `?bundleVersion=N` (param is **`bundleVersion`**,
never `version`; endpoints are **public/`.AllowAnonymous`**). **Fallback is NOT silent (Rule E):** a failed bundle download
keeps the old cache + sets `is_stale()`/`last_error_code()` + `push_warning` (boot logs "using stale cache"); the sample
screen shows a **stale banner + Retry**; **no cache** ⇒ **empty state + Retry** (retry re-runs `check_for_update()` via
`NetworkClient` — no infinite loop). **Sample UI** `HeroListView` (BaseView, **network-free**) + `HeroListPresenter`
(reads `ConfigProvider`, navigated from the hub `heroes` intent via `SceneRouter`) proves the loop: **server config change →
new version → client displays it with NO client rebuild**. **No new EventBus event** (catalogue stays CLOSED — consistent
with Phases 17 & 20; reuse `config_updated`). Minimal placeholder config seeded (`config/heroes/hero_sample.json` +
`config/skills/skill_sample_basic.json`, **zero stats — no balance**) so a real server serves a non-empty hero map for demo.
**User decisions:** dedicated hero screen (not a hub section); no new EventBus event; seed real config. Verified (Godot
4.7.1-stable local): config-validator exit 0 (2 files, hero→skill integrity); `--headless --import` exit 0 (0/0); gdUnit4
**76/76 pass, 0 orphan** (config_provider +7: map-shape/real-endpoints/fallback-stale/no-cache; hero_list_presenter 5:
receive→query→display/version-bump/error→fallback/no-cache→retry/back); grep guard clean; no generated/openapi drift.
Canonical: `docs/gameplay/configuration-and-data.md` §4.1–4.3 + `docs/godot/resources-and-assets.md` §1.1; decision log
`.memory/0020-client-config-bundle-e2e-standardized.md`.

- Future agents **MUST reuse** `ConfigProvider` (single config read gate) + the real endpoints/param + both data shapes +
  the sample presenter pattern before adding any config-read UI; **MUST NOT** create a second config provider, read a raw
  bundle/cache/file in a feature, hardcode gameplay/config values in a scene, hand-decide a config version (always ask
  `/config/current`), or use `version` instead of `bundleVersion`.
- **Fallback MUST stay non-silent (Rule E):** using an old cache because a new bundle failed must **log + expose** (`is_stale()`
  + a visible label) + keep **Retry**; **never** fabricate config/state on failure (ADR-005/007/011). Boot stays best-effort
  on config (never blocks); the feature screen is where stale/retry is surfaced to the user.
- **Config change ⇒ new version served with NO client rebuild** (ADR-004/005): change a value in `config/**` → server
  republishes a new `config@vN` → client `check_for_update` pulls & displays it. Never edit client code/scene to change a
  config value. `config@vN` disk cache is immutable/write-once.
- **Out of scope (leave as debt):** signed/secure bundle + cryptographic verify + advanced LiveOps + live swap = **Post-MVP**;
  real Hero System/combat/skill logic = phase 27+/group 5 (the hero screen is a config-read **sample** only).
- When changing this loop, keep **`client/src/core/config/config_provider.gd` + `client/src/ui/hero_list/*` +
  `client/src/ui/boot/boot_controller.gd` + `client/src/ui/main_hub/main_hub_presenter.gd` + the gdUnit4 tests
  (`tests/core/config/config_provider_test.gd`, `tests/ui/hero_list/hero_list_presenter_test.gd` — behavior contract) +
  seeded `config/heroes|skills/*` (validator-passing) + `docs/gameplay/configuration-and-data.md` §4.1–4.3 +
  `docs/godot/resources-and-assets.md` §1.1 + `.instructions/client.md` + `.claude/agents/godot-client.md` in sync**
  (doc-sync matrix, §5); the gdUnit4 tests are the behavior contract — update them.

**Combat spec + fixed-point math + golden-vector format are standardized (Phase 23 — closed & verified). OPENS Group 5
(Deterministic Combat Core).** Phase 23 is a **specification phase, not sim code** — it turns the architecture-only combat
docs into a precise **combat contract** so the .NET server sim (phase 24) and GDScript client sim (phase 25) implement the
**same ruleset** and produce **bit-identical `event_log` + `result`** for the same `(config version, team snapshot, stage,
seed)` (ADR-011). Home of the canon: **`docs/gameplay/combat-framework.md` §9–§20** (detailed spec) + the golden-vector
format & samples in **`shared/combat-vectors/`** + the determinism summary in **`docs/conventions/code-style.md` §4**.
**Balance numbers stay in config** (`combat_int` ≥ 0, ADR-004/005) — the spec fixes only *mechanism, formula shape, math,
RNG, serialization*; **no `float` in any combat arithmetic**. **Constitutional decisions (mechanism; user-approved):**
(1) **turn order** = stable speed-sort per round, key `(-spd, actor_id)` (tie-break ends in the unique `actor_id`, byte
compare — never hash/iteration/DB order); (2) **fixed-point** = 64-bit × **`FIXED_SCALE=1000`**, single rounding law
**round-half-up** at every `fixed_mul`/`fixed_div`/`from_fixed` (no floor/banker's; divide-by-zero guards, never
NaN/float); (3) **PRNG** = **PCG32** (`pcg_setseq_64_xsh_rr_32`) + **SplitMix64** seed expansion, one stream/battle, seed
a `uint64` server-generated input, logical shifts + wrapping 64-bit multiply, unbiased `pcg_bounded`, rolls in basis
points; (4) **RNG consumption order** fixed = `hit` roll then `crit` roll, **miss = 1 roll, hit = 2 rolls** (consume the
crit roll even when `crit_rate_bp==0`); (5) **damage** = divisive DEF-ratio `atk*coeff*K/(K+def)` in fixed-point, crit
**after** mitigation, final `from_fixed`, floor `MIN_DMG`; (6) **event log** = a `seq`-ordered stream (same sequence +
fields, not just final HP); (7) **win/lose/draw** from the `ally` perspective, evaluated after each action/round,
`max_rounds` ⇒ DRAW. **CB status:** CB1/CB2 **decided** by ADR-011 (server-authoritative; seeded); **CB5** mechanism
**decided** (thresholds config; disable randomness via `accuracy_bp=10000`/`crit_rate_bp=0`); **CB6** mechanism decided
(target seconds/`max_rounds` value stay open); **CB3/CB4** are **`[ĐỀ XUẤT]`** (deterministic target/aggro + energy-bar
mechanism proposed, numbers to config, **pending product** — not promoted to canon). Verified (2026-09-01): two
reference-generated golden vectors (`vector_01_basic_hit` 59 events → VICTORY; `vector_02_crit_ko` 30 events → VICTORY)
computed by a spec-faithful reference calculator and **hand-checked** (e.g. `fixed_div(300000,380000)=789`,
`fixed_mul(200000,789)=157800`, `from_fixed=158`); both files are valid JSON; no-float audit + contradiction audit clean;
no code/openapi drift (spec-only change). Canonical: `docs/gameplay/combat-framework.md` §9–§20 + `shared/combat-vectors/README.md`
+ `docs/conventions/code-style.md` §4; decision log `.memory/0021-combat-spec-fixedpoint-standardized.md`.

- Future agents **MUST reuse** this spec (`combat-framework.md` §9–§20) + the fixed-point/PRNG/damage/turn-order rules +
  the golden-vector format before writing any sim code; **MUST NOT** use `float`/`double` in combat arithmetic, introduce
  a second fixed-point scale/rounding law or a second PRNG, depend on hash/iteration/DB/insertion order, use global RNG or
  wall-clock time in the sim, or hand-decide balance numbers (they are `combat_int` from config).
- **Golden vectors are a living contract** (`shared/combat-vectors/`): a deliberate sim/spec change updates the vectors in
  the same change **and explains WHY**; **never** edit a vector to make CI green. Two implementations must match `event_log`
  + `result` **bit-for-bit** (phase 26 adds the full suite + cross-impl CI gate).
- **Anti-self-invention is binding:** search repo → prefer ADR/spec → mark `[ĐỀ XUẤT]`/`[OPEN]` for anything product has
  not decided; **never** silently close a CB open question. CB3/CB4 stay proposals until product confirms.
- **Out of scope (do not pull in):** sim implementation = **phases 24/25**; full golden-vector suite + cross-impl CI gate =
  **phase 26**; complex skill/effect impl = **phase 28**; battle UI/animation = **phase 30**; fast-forward/skip (CB7),
  signed vectors = Post-MVP.
- When changing the combat contract, keep **`docs/gameplay/combat-framework.md` §9–§20 + `shared/combat-vectors/*` (README
  format + sample vectors, updated deliberately) + `docs/conventions/code-style.md` §4 + `docs/mvp/10-open-questions.md`
  (CB1–CB6) + ADR-011 (only if the decision changes) + `.instructions/combat.md` + `.claude/agents/combat-determinism.md`
  + `.agents/ROLES.md` in sync** (doc-sync matrix, §5); the golden vectors are the behavior contract — update them.

**Deterministic combat sim server (.NET) is standardized (Phase 24 — closed & verified).** Phase 24 **implements** the
frozen Phase-23 spec (§9–§20) as the **pure, deterministic** server simulator — the server-authoritative source of truth
for battle results (ADR-011). It does **not** change the spec. Two layers: **(1) pure engine in `GameTeam.Domain/Combat/`**
(package-free — guarded by `Domain_should_not_depend_on_framework_packages` +
`Combat_domain_sim_should_not_depend_on_framework_or_persistence`): `BattleSimulator.Simulate(BattleInput) → BattleOutput`
over `Numerics/FixedPoint` (`long`, `FixedScale=1000`, **single** round-half-up law, div-by-zero/negative guards — **no
`float`/`double`**), `Rng/Pcg32` (PCG32 `pcg_setseq_64_xsh_rr_32` + SplitMix64, explicit `ulong` seed, **one stream/battle**,
`unchecked` wrapping 64-bit, logical shifts — **no global RNG**), `Model/*` (self-contained `BattleInput`), `State/UnitState`,
`Events/*` (13 types, `seq` by list position, golden field names), `Effects/*` (`EffectRegistry` `effect_type`→`IEffectHandler`;
sample `DamageEffectHandler` §17 + `HealEffectHandler`; unknown type throws; **never `switch(skillId)`** in the core),
`Serialization/CombatEventSerializer` (deterministic compact JSON). The engine takes **no `IClock`/time** at all (stronger than
"inject the clock") and does **no I/O**. **(2) Data-driven layer in `GameTeam.Application/Combat/`**: `CombatInputResolver`
reads hero/skill/stage via the Phase-10/21 **`IConfigProvider`** port → builds `BattleInput`; combat config POCOs live here
(`combat_rules` sourced from **stage config** — formalizing it into the stage JSON Schema is a documented follow-up). **No
battle endpoint** (phase 30) and **no DI wiring** yet. Verified (2026-09-02, local, no Docker): `dotnet build -c Release` 0
error, `dotnet test` **Domain 77 / Application 45 / Contracts 36** pass — golden `vector_01_basic_hit` (59 ev) +
`vector_02_crit_ko` (30 ev) match **event-by-event + result**; determinism **N=200 byte-identical**; PRNG matches golden rolls
(12345→7329/4605, 999→8003/8884); data-driven (`hero.atk` 200→400 ⇒ damage 158→316, fewer rounds, no code change); purity
guard (`CombatPuritySourceScanTests` + NetArchTest) red on injected `double` → reverted green; no `openapi.json`/generated
drift. Canonical: `docs/gameplay/combat-framework.md` §21; decision log: `.memory/0022-combat-sim-server-standardized.md`.

- Future agents **MUST reuse** `BattleSimulator`/`FixedPoint`/`Pcg32`/`EffectRegistry`/`CombatEventSerializer` +
  `CombatInputResolver` before adding any combat logic; **MUST NOT** create a second simulator/fixed-point/PRNG/registry, use
  `float`/`double`/`DateTime`/wall-clock/global `Random` in the sim path, depend on hash/iteration/DB order, read config from
  the filesystem in the sim, or hand-decide balance numbers (they are `combat_int` from config — ADR-004).
- **The server sim is the authority (ADR-011):** the phase-25 client sim replays/predicts and **must match it bit-for-bit**;
  never let the client define a divergent result. **Golden vectors are the contract** — a deliberate sim change updates the
  vectors in the same change and explains WHY; **never** edit a vector to make CI green (full suite + cross-impl CI gate = phase 26).
- **Extend via the registry, not the core:** a new effect = a new `IEffectHandler` + config, never a `switch`. **Energy/ultimate**
  (§15, CB4 `[ĐỀ XUẤT]`) is wired but **inactive** — do not activate or close CB3/CB4 without product; do not implement future-phase
  scope (client 25, endpoint 30, full skill content 28).
- When changing the combat sim, keep **`GameTeam.Domain/Combat/*` + `GameTeam.Application/Combat/*` + the combat tests
  (`GameTeam.Domain.Tests/Combat` + `GameTeam.Application.Tests/Combat` + `ArchitectureTests` — behavior contract) +
  `docs/gameplay/combat-framework.md` §21 + `.instructions/combat.md` + `.instructions/backend.md` +
  `.claude/agents/combat-determinism.md` + `.claude/agents/dotnet-backend.md` in sync** (doc-sync matrix, §5); the golden
  vectors + determinism/purity tests are the behavior contract — update them.

**Deterministic combat sim client (GDScript) is standardized (Phase 25 — closed & verified).** The client combat sim
**mirrors the server (Phase 24) bit-for-bit** and is **display/replay only, NOT authority** (ADR-011). Home:
**`client/src/combat/`** + **`client/src/shared/fixed_point.gd`** (pure, scene/UI-decoupled): `BattleSimulator.simulate(BattleInput)
→ {event_log, result}`; `FixedPoint` (`FIXED_SCALE=1000`, round-half-up — **no float**); `rng/pcg32.gd` (SplitMix64→PCG32,
**logical** shift since GDScript `>>` is arithmetic, constants match `Pcg32.cs`); `events/combat_events.gd` (13 types,
snake_case factories, `seq` by position); `effects/damage_effect_handler.gd` (divisive DEF-ratio) + registry; `model/*`
(mirror server) + `combat_input_resolver.gd`. Ordering matches server: action `(-spd, actor_id)`, target `(slot, actor_id)`,
hit-then-crit rolls. Verified (Godot 4.7.1 local): gdUnit4 `client/tests/combat/*` (golden/determinism/outcome/fixed_point/
pcg32/resolver) **24–25 pass, 0 orphan**; grep clean (sim imports no `core/net`/UI; no `float`). Fixed a latent test bug:
`battle_simulator_outcome_test.gd` helper `_input(...)` collided with the `Node._input(InputEvent)` virtual (Godot 4.7.1
parse-error blocked the whole suite) → renamed `_input`→`_make_input` (test-helper name only, no sim change). Canonical:
`docs/gameplay/combat-framework.md` §21.6; decision log: `.memory/0023-combat-sim-client-standardized.md`.

- Future agents **MUST reuse** the client `BattleSimulator`/`FixedPoint`/`Pcg32`/effect registry — **MUST NOT** fork a
  second client sim, use `float`/wall-clock/global RNG in combat math, let the sim import `core/net`/UI, or let the client
  **decide** a battle result (it replays the server's seed; server is authority — ADR-011).
- **Never name a test/helper function after a Godot virtual** (`_input`/`_ready`/`_process`/`_notification`…) — it silently
  collides and can break parse/discovery of the whole suite.
- When changing the client sim, keep **`client/src/combat/*` + `client/src/shared/fixed_point.gd` + `client/tests/combat/*`
  (behavior contract) + `docs/gameplay/combat-framework.md` §21.6 + `.instructions/client.md` + `.claude/agents/godot-client.md`
  in sync**, and it **must still match the server golden vectors** (Phase 26 gate) — never diverge silently.

**Golden vector suite + cross-impl CI gate is standardized (Phase 26 — closed & verified). CLOSES Group 5 (Deterministic
Combat Core).** The combat sim is now safety-locked: a shared multi-scenario golden-vector set whose **baseline is generated
from the server sim** (the authority, ADR-011), run by both implementations in CI against the **same** committed baseline ⇒
**server ≡ client ≡ baseline**; a **blocking** `golden-vector` gate fails merge on any drift. **9 vectors** in
**`shared/combat-vectors/`** (`vector_01`..`vector_09`: basic / crit / **miss** / **DEFEAT** / **DRAW** / multi-unit
turn-order+tie-break+retarget / mixed-crit / boundary damage==HP / boundary damage<HP). Baseline generator =
**`tools/combat-baseline/`** (.NET console, mirrors `tools/config-validator`; **ProjectReference `GameTeam.Domain`** ⇒ one
`BattleSimulator`+`CombatEventSerializer`, **no forked sim**): `run.sh generate` writes each vector's `expected` from the
server sim (canonical 2-space/LF, idempotent); `run.sh check` regenerates in-memory and **byte-compares** vs committed
(exit 1 on drift — the drift guard). The tool's `input` parser mirrors the test-side `GoldenVectorLoader` and is
cross-checked automatically (a divergence turns `GoldenVectorTests` red). Both suites **auto-discover** vectors — server
`GoldenVectorTests` (`[Theory]`+`[MemberData]`), client `golden_vector_test.gd` (`CombatVectorLoader.list_vector_files()`)
— so adding a vector needs **no test-code change**. Gate: `.github/workflows/ci-server.yml` job `golden-vector` (real:
`run.sh check` + `dotnet test --filter GoldenVector`) + `ci-client.yml` (gdUnit4, now triggered by `shared/combat-vectors/**`),
**BLOCKING** (no `continue-on-error`/`|| true`). Verified (local 2026-09-04): server 9/9 golden + `run.sh check` exit 0 + tool
xUnit 4/4; client gdUnit4 golden auto-discovers 9, 24 pass/0 orphan. **Negative proven on BOTH sides:** `+1` in the server
`DamageEffectHandler.ComputeDamage` ⇒ `run.sh check` exit 1 + 9/9 golden red; `+1` in client `damage_effect_handler.gd` ⇒
client golden red; revert ⇒ both green. Canonical: `docs/gameplay/combat-framework.md` §22 + `shared/combat-vectors/README.md`
+ `tools/combat-baseline/README.md`; decision log: `.memory/0024-combat-golden-vectors-standardized.md`.

- Future agents **MUST reuse** the 9 vectors + `tools/combat-baseline` + the auto-discovery tests before touching combat;
  **MUST NOT** fork a second baseline generator/sim, hand-write or hand-edit a vector's `expected`, edit a vector to make CI
  green, weaken the comparison, or make the `golden-vector` gate non-blocking. The baseline is **server-generated** — the
  client replays and must match it, never the reverse.
- **Baseline changes are deliberate (doc-sync row "Combat sim change"):** an intentional sim-formula change → run golden
  (red) → confirm the diff is intentional (not a bug) → `run.sh generate` → **review the diff** → write WHY in the PR →
  doc-sync → `combat-determinism`/`reviewer` review. **Never** regenerate to silence an unexplained mismatch. After ANY
  combat-sim change, run the gate on **both** sides.
- **Out of scope (leave as debt):** ultimate/energy vectors (CB4 `[ĐỀ XUẤT]`, inactive) + new skill/effect content = phase 28;
  signed/compressed/delta vectors = Post-MVP. Do not activate energy/ultimate or add future-phase content here.
- When changing the golden system, keep **`shared/combat-vectors/*` + `tools/combat-baseline/*` (core+tests+README) +
  `server/tests/GameTeam.Domain.Tests/Combat/GoldenVectorTests.cs` + `client/tests/combat/golden_vector_test.gd` +
  `client/tests/combat/support/combat_vector_loader.gd` + `.github/workflows/ci-server.yml` + `.github/workflows/ci-client.yml`
  + `docs/gameplay/combat-framework.md` §22 + `docs/testing/backend-testing.md` §4.2 + `docs/testing/godot-testing.md` §3 +
  `docs/deployment/ci-cd-pipeline.md` §4 + `.claude/agents/combat-determinism.md` + `.claude/agents/reviewer.md` +
  `.instructions/combat.md`/`backend.md`/`client.md` in sync** (doc-sync matrix, §5); the golden vectors + gate are the
  behavior contract — update them deliberately.

**Hero System is standardized (Phase 27 — closed & verified). OPENS Group 6 (Gameplay Vertical Slice).** The Hero
foundation is **data-driven** (ADR-004) + **server-authoritative** (ADR-007). Two homes: **server**
`GameTeam.Domain/Heroes/` + `GameTeam.Application/Features/Heroes/`; **client** `client/src/ui/hero_list/` +
`client/src/ui/hero_detail/` + `client/src/core/assets/`. **HeroDefinition = config, never hardcode:** read via the
Phase-10/21 port **`IConfigProvider.Get<HeroConfig>("hero", id)`** (POCO `HeroConfig`, snake_case↔PascalCase; bám hero
schema Phase 06 — added optional additive field **`art`**). **`OwnedHero : AggregateRoot<Guid>`** (ProfileId FK,
`HeroId` config ref, `Level`/`Stars` base; factory `Grant` raises `OwnedHeroGranted`; table **`owned_heroes`** — index
`profile_id` + **unique `(profile_id, hero_id)`** + FK→`player_profiles` cascade; migration `AddOwnedHeroes`) is the
server-authoritative ownership record extending the Phase-19 save root — **stats are NOT stored, always read from
config**. Queries: **`GetMyHeroesQuery`** (owner ONLY from `ICurrentUser` token `sub` — IDOR-safe; returns
**`MyHeroesResponse`** wrapping lean **`OwnedHeroDto{heroId,level,stars}`** — client joins definition from
ConfigProvider) → `GET /api/v1/heroes` (protected); **`GetHeroDefinitionQuery`** (definition from config →
`HeroDefinitionDto`; `HERO_DEFINITION_NOT_FOUND`) → `GET /api/v1/heroes/{heroId}/definition` (public catalog).
**Temporary seed:** `CreateGuestAccountCommandHandler` grants all config heroes (`GetIds("hero")`) in the SAME
transaction as account+profile — **temporary until Phase 33 (summon)**, NOT real acquisition. **Client:** Hero List
joins **owned** (`StateCache.get_heroes()`) + **definition** (`ConfigProvider.get_hero(id)`); Hero Detail is a separate
routed scene (hero id via the additively-extended **`SceneRouter.goto_scene(path, context)`** + **`route_context()`**);
**hero art lazy-loads** via the new autoload **`AssetLoader`** (`load_texture` async + placeholder + `release`, art path
from config `art` — ADR-009; list loads NO art). **Contract→codegen** carried the hero DTOs; fixed the generator to
**escape GDScript reserved words** (wire `class` → var `class_`, `## wire: class` kept) in `shared/codegen` `GdNaming.ToFieldName`.
Verified (local 2026-09-05, Docker Desktop 28.5.1): build Release 0/0, `dotnet test` Domain 88 / Application 51 /
Contracts 36 / Infrastructure 44 / Api 56; codegen 41; config-validator 45 + `run.sh` exit 0; Godot 4.7.1 import exit 0;
gdUnit4 **113/113 pass, 0 orphan**; `has-pending-model-changes` clean; no openapi/generated drift beyond additive hero.
Canonical: `docs/gameplay/hero-system.md` §7 + `docs/backend/domain-and-application.md` (Hero feature) +
`docs/backend/infrastructure.md` §1.3 + `docs/backend/api-and-versioning.md` + `docs/godot/resources-and-assets.md` §2.1
+ `docs/godot/scene-architecture.md` §4.1 + `docs/gameplay/configuration-and-data.md` §2b; decision log:
`.memory/0025-hero-system-standardized.md`.

- Future agents **MUST reuse** `HeroConfig`/`IConfigProvider` (definition), `OwnedHero`/`IOwnedHeroRepository`
  (ownership), `GetMyHeroes`/`GetHeroDefinition`, client `ConfigProvider`+`StateCache` join, and `AssetLoader` (art)
  before adding any hero plumbing; **MUST NOT** hardcode gameplay stats in code, create a second hero definition source,
  store hero stats in `owned_heroes`, read owner from client input (body/route/query — IDOR), fabricate ownership on the
  client, create a second asset loader / config / state cache, or hand-edit `client/src/data/generated`.
- **Contract/DTO change workflow (binding):** edit `GameTeam.Contracts/Hero/*` → rebuild (regenerate `openapi.json`) →
  `bash shared/codegen/run.sh` → commit generated diff → extend `Api.IntegrationTests`/`OpenApiContractTests` → doc-sync.
  A new contract field that is a GDScript reserved word is escaped by the generator (extend `GdNaming` + a test, never
  hand-edit generated files).
- **Art is config-driven + lazy (ADR-004/009):** art path comes from the config `art` field (never scattered hardcoded
  paths); the list must not block on art; free art on scene exit. Real art content + atlas/pool tuning = later (phase 52).
- **Out of scope (leave as debt / future phases):** skill logic (28), formation (29), battle (30), summon / real hero
  acquisition (33), level/star upgrade (35/39). Do NOT implement future-phase scope.
- When changing the Hero System, keep **`GameTeam.Domain/Heroes/*` + `GameTeam.Application/Features/Heroes/*` +
  `IOwnedHeroRepository`/`OwnedHeroRepository`/`OwnedHeroConfiguration`/migration + `Contracts/Hero/*` + regenerated
  `openapi.json` + `client/src/data/generated/**` + `CreateGuestAccountCommandHandler` (seed) + `client/src/ui/hero_list/*`
  + `client/src/ui/hero_detail/*` + `client/src/core/assets/asset_loader.gd` + `SceneRouter` (context) +
  `client/src/ui/boot/auth_profile_flow.gd` (hero fetch) + `response_parser.gd` + `config/heroes|skills/*` +
  `shared/config-schema/hero.schema.json` (`art`) + all the tests (behavior contract) + the docs listed above +
  `.instructions/backend.md`/`client.md`/`config.md` + `.claude/agents/dotnet-backend.md`/`godot-client.md` in sync**
  (doc-sync matrix, §5); the tests are the behavior contract — update them.

**Execution rule (applies to every task).** After completing any implementation task, the agent **MUST** update the
relevant roadmap/phase checklist and mark each completed item `[x]` (✅), **verify** it against the phase acceptance
criteria with real run evidence, and **synchronize all affected Vibe Code/agent docs** (this file §4.6, `.instructions/*`,
`.claude/workflows/*`, `.memory/*`) **before** declaring the task complete. Never claim a phase/task complete without
verification; never leave a finished checklist item unchecked; never silently skip a phase requirement or invent a
missing one. If a requirement is blocked by a missing dependency, **report it explicitly and leave it unchecked** —
do not mark it done. CI-only gates stay `[ ]` ("CI-verification pending") until the Actions result exists (§4.5).

## 5. Definition of Done & the update policy

- A change is **Done** only per `docs/ai/review-and-dod.md` §4 (acceptance met, review checklist
  passed, tests + CI green, no Forbidden Patterns, docs updated, PR links SSOT/ADR).
- **Mandatory doc-sync:** any change that alters architecture, dependencies, config schema, or
  public behavior **must** update the affected docs in the same change. Run the change→doc-impact
  matrix in **`.claude/workflows/documentation-sync.md`** before declaring done.

## 6. Language

Repository docs and folder READMEs are **Vietnamese** (per `docs/conventions/`). The AI execution
layer (`CLAUDE.md`, `.claude/`, `.prompts/`, `.templates/`, `.context/`, `.rules/`,
`.instructions/`) is written in **English**. Keep each side in its language when editing.
