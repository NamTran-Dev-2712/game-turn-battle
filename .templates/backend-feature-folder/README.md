# Template: Backend Feature Folder (CQRS)

Scaffold for one Application feature in the .NET server. Matches
`docs/backend/domain-and-application.md`. **No gameplay logic here** — these are structure stubs.

## Layout
Place under `server/src/GameTeam.Application/Features/<FeatureName>/`:

```
Features/<FeatureName>/
  Commands/
    <DoThing>Command.cs            # request (record), implements IRequest<Result>
    <DoThing>CommandHandler.cs     # handler: orchestrates Domain + ports, no infra details
    <DoThing>CommandValidator.cs   # FluentValidation rules for the command
  Queries/
    Get<Thing>Query.cs             # read request
    Get<Thing>QueryHandler.cs      # read handler (no writes)
```

## Rules baked into this template
- Handlers depend on **ports/interfaces** (defined in Domain/Application), never on Infrastructure types.
- No `DateTime.Now` — inject `IClock`. No config-file reads — use the config port (ADR-005).
- Validation lives in the `Validator`, not scattered in the handler.
- Return a `Result`-style outcome; do not swallow errors.

## Use
Copy the `.cs.template` files, rename `<FeatureName>`/`<DoThing>`/`<Thing>`, drop the `.template`
extension, and place under the feature folder. Then follow `.claude/workflows/implementation.md`.
