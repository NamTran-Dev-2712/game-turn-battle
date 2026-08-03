# Context Pack: Dependency Map

> The rules that govern what may depend on what. Canonical: `docs/architecture/dependency-graph.md`.

| Need | Source |
|---|---|
| The dependency rule (full) | `docs/architecture/dependency-graph.md` |
| Backend layering & solution structure | `docs/backend/solution-structure.md`, `docs/architecture/overview.md` |
| Client scene/service structure | `docs/godot/scene-architecture.md`, `state-and-signals.md` |
| Dependency management policy (NuGet/addons) | ADR-010, `server/Directory.Packages.props` |

## Quick reference (authoritative source is the doc above)
- **Backend inward-only:** `Api → {Application, Infrastructure, Contracts}` · `Infrastructure →
  Application` · `Application → {Domain, Contracts}` · `Contracts → Domain` · **Domain → nothing**.
  Enforced by NetArchTest in `GameTeam.Application.Tests` — do not weaken those tests.
- **Client:** features never import each other; communication via EventBus/signals (ADR-002); no God autoload.
- **Config:** Domain/Application read config only through a port (`IConfigProvider`), never files (ADR-005).
