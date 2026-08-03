# Prompt: Backend Feature (.NET / CQRS)

Specialization of `feature.md` for the server. Delegate to `.claude/agents/dotnet-backend`.

```
Implement <feature> in GameTeam.Application (+ Domain/Infrastructure/Api as needed).

SSOT & DESIGN
- Business: docs/mvp/<file>   ·   Backend design: docs/backend/<solution-structure|domain-and-application|infrastructure|api-and-versioning>
- Decisions: ADR-003 (backend arch), ADR-005 (config), ADR-007/011 (server authority), ADR-010 (deps).

BACKEND RULES
- Dependency direction inward only; Domain pure (no EF/HTTP/DateTime.Now — inject IClock).
- CQRS: command/query + handler + FluentValidation via MediatR pipeline (layout: .templates/backend-feature-folder/).
- Balance/config via config port (ADR-004/005) — no hardcoded gameplay numbers, no config-file reads in Domain/App.
- New NuGet dep ⇒ Directory.Packages.props + PR justification (ADR-010).

DELIVERABLE
- Tests (xUnit); dotnet test server/GameTeam.sln green including NetArchTest dependency gate.
- Finish with .claude/checklists/post-task.md.
```
