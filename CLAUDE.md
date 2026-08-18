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
