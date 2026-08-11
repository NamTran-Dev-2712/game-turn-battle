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
| Changes the config schema | `shared/config-schema/*.schema.json` + `docs/liveops/remote-config.md` + `docs/gameplay/configuration-and-data.md` + `config/*/README.md` if a domain is affected |
| Changes a client↔server contract/DTO | `server/GameTeam.Contracts` (one public type/file, additive-only) → **rebuild** to regenerate `shared/contracts/openapi.json` (never hand-edit; CI drift-guard enforces) + `docs/backend/api-and-versioning.md`; regenerate `shared/codegen` output. **Enum change:** keep numeric values stable (add-only, deprecate not reuse), update `EnumStabilityTests` deliberately (`server/tests/GameTeam.Contracts.Tests`) |
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
