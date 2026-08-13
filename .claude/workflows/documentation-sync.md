# Workflow: Documentation Sync (Mandatory Update Policy)

> **Rule:** documentation must never lag implementation. Any change that alters architecture,
> dependencies, config schema, public API/contract, a module boundary, or public behavior **must**
> update the affected docs **in the same change**. A change that skips this is **not Done**
> (`docs/ai/review-and-dod.md` §4.5).
>
> **Principle:** *index, don't repeat.* Update the **canonical** doc; make derived docs *link* it,
> never copy. `docs/` is the SSOT — this layer keeps it honest, it does not replace it.

## How to use
1. Run `git diff` and classify the change against the matrix below.
2. For every matching row, open the listed docs and update them before declaring Done.
3. If a change would contradict `docs/mvp/` or an Accepted ADR: **stop** — that's a code defect or a
   new ADR is required. Do not edit the SSOT to match the code.

## Change → Doc-impact matrix

| If the change… | Update (canonical first) |
|---|---|
| Makes a new architectural decision | New ADR (`docs/adr/` template) → index in `DECISIONS.md` + ADR catalog (`docs/adr/README.md`) → foundational-decisions table if applicable |
| Reverses/supersedes a decision | Mark old ADR `Superseded`, add new ADR, fix cross-refs |
| Adds/removes a dependency (NuGet/addon) | ADR-010 rationale (in PR) + `server/Directory.Packages.props` (or Godot addon note in `docs/godot/tooling-and-testing.md`) |
| Changes the config schema | `shared/config-schema/*.schema.json` (per-type + `common.schema.json` + `config-bundle.schema.json`) + matching `fixtures/*.{valid,invalid}.json` + `docs/liveops/remote-config.md` + `docs/gameplay/configuration-and-data.md` (schema-mapping table) + `docs/conventions/data-and-docs-conventions.md` (ID-prefix table) + `config/*/README.md` if a domain is affected. **Breaking change** (remove/rename field, tighten type, narrow enum, change meaning) → bump `schema_version` + add migration under `shared/config-schema/_versions/` (see its README). Never hardcode balance in a schema. Cross-file referential integrity is validated by `tools/config-validator` (phase-07 gate), not JSON Schema alone — **run it** (`bash tools/config-validator/run.sh config shared/config-schema`) and update its tests when a new config type/ref is added. |
| Changes the config validator (behavior / error codes / refs) | `tools/config-validator` (core lib + xUnit tests — the behavior contract) + `tools/config-validator/README.md` (error-code table `JSON001…REF002`) + `.github/workflows/validate-config.yml` (mandatory gate) + `docs/gameplay/configuration-and-data.md` + `docs/deployment/ci-cd-pipeline.md` + `.instructions/config.md`. A new config type MUST add schema + validator mapping + refs + test together. **Never** bypass/weaken the gate to make CI green; fix the correct layer. Config Service (phase 21) reuses the core — do not fork a second validator. |
| Changes a client↔server contract/DTO | `server/GameTeam.Contracts` (one public type/file, additive-only) → **rebuild** to regenerate `shared/contracts/openapi.json` (never hand-edit; CI drift-guard enforces) + `docs/backend/api-and-versioning.md`; **regenerate client models** `bash shared/codegen/run.sh` → **commit** the diff in `client/src/data/generated/` (never hand-edit; `codegen-check.yml` drift gate enforces). **Enum change:** keep numeric values stable (add-only, deprecate not reuse), update `EnumStabilityTests` deliberately (`server/tests/GameTeam.Contracts.Tests`); enums flow to GDScript via `x-enum-varnames`/`x-enum-values` (`ContractEnumsDocumentTransformer`) |
| Changes an Application pipeline behavior / port | `server/src/GameTeam.Application/Behaviors/*` + ports (`Abstractions/*`) + markers + `AddApplication` order + `GameTeam.Application.Tests` (behavior tests + `PipelineOrderTests` + `ArchitectureTests` — the contract) + `docs/backend/cross-cutting.md` §2.5 + `docs/backend/domain-and-application.md` §2 + `.instructions/backend.md` + `.claude/agents/dotnet-backend.md`. Fixed order **Logging → Validation → Transaction → Caching**; cross-cutting lives in behaviors (never handlers); transaction only on `ITransactionalRequest`, cache only on `ICacheableQuery`; `GameTeam.Application` must not reference Infrastructure/Api. |
| Changes combat sim behavior | ADR-011 (if the decision changes) + `docs/gameplay/combat-framework.md` + update golden vectors deliberately |
| Adds/changes a module boundary | `docs/architecture/dependency-graph.md` + the module doc (`docs/backend/`, `docs/godot/`, `docs/gameplay/`) + that folder's `README.md` |
| Changes public behavior of a feature | The module doc + module `README.md`; note in `CHANGELOG.md` |
| Advances a roadmap phase / milestone | `docs/roadmap/README.md` + `docs/audit/bootstrap-audit.md` (status) + root `ROADMAP.md` if the stage list changed |
| Adds/changes a CI stage, deploy, or env | `docs/deployment/ci-cd-pipeline.md` / `release-operations.md` + `.github/workflows/*` + root `DEPLOYMENT.md` if the summary changed |
| Changes a convention (naming/style/git) | `docs/conventions/*` (canonical) — root `STYLE_GUIDE.md` only links it |
| Changes the AI workflow/rules | `docs/ai/*` (canonical) → then reflect in this `.claude/` layer + `.rules/rules.md` |
| Discovers an unresolved question | Append to `docs/mvp/10-open-questions.md` — do not resolve silently |
| Populates/changes an AI dotfolder | That folder's `README.md` Contents index + `AI_GUIDE.md` map if roles changed |

## Derived-doc pointers (keep these as links, not copies)
- Root `README.md`, `ARCHITECTURE.md`, `DECISIONS.md`, `ROADMAP.md`, `TESTING.md`, `DEPLOYMENT.md`,
  `STYLE_GUIDE.md`, `CONTRIBUTING.md`, `SECURITY.md` are **navigational** — they link `docs/`.
- The foundational-decisions table appears in a few places; change the canonical one
  (`docs/README.md`) and ensure the others link rather than restate.

## Definition of Done for a doc-sync pass
Every matrix row the change triggers is updated, the canonical/derived link direction is preserved,
and no dead links were introduced.
