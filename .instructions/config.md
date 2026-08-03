# Instructions: Config / data scope (`config/`, `shared/config-schema/`)

Short execution hints. Canonical: `docs/gameplay/configuration-and-data.md`, `docs/liveops/remote-config.md`.
Prompt: `.prompts/config-change.md`.

- Data-driven: all gameplay balance lives in config, never in code (ADR-004).
- Domain/Application read config only through `IConfigProvider` — never read files directly (ADR-005).
- Schema is `shared/config-schema/config-bundle.schema.json`; schema change → bump `config_version` (ADR-005).
- Values needing tuning → mark in `docs/mvp/10-open-questions.md` (EC), don't invent final numbers.
- CI `validate-config` must pass (schema + referential integrity when tooling lands).
