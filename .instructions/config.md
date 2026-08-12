# Instructions: Config / data scope (`config/`, `shared/config-schema/`)

Short execution hints. Canonical: `docs/gameplay/configuration-and-data.md`, `docs/liveops/remote-config.md`.
Prompt: `.prompts/config-change.md`.

- Data-driven: all gameplay balance lives in config, never in code (ADR-004).
- Domain/Application read config only through `IConfigProvider` — never read files directly (ADR-005).
- Values needing tuning → mark in `docs/mvp/10-open-questions.md` (EC), don't invent final numbers.
- CI `validate-config` must pass (schema + referential integrity when tooling lands — phase 07).

## Config schemas (phase 06 — closed & verified)

The contract lives in `shared/config-schema/` (JSON Schema draft 2020-12). Reuse it — do NOT reinvent:

- **8 per-type schemas** (`hero/skill/stage/gacha/shop/reward/economy/quest.schema.json`) + `common.schema.json`
  (`$defs`: id prefixes, `combat_int`, enums, `cost`) + `config-bundle.schema.json` (envelope, `config@vN`).
- **Schema-first:** before adding/changing a config field, read the relevant `docs/gameplay/*` doc + ADR +
  the schema. Never invent config fields, enum values, effect types, quest/condition types, currencies, or
  reward types not backed by the gameplay docs or `GameTeam.Contracts` enums (phase 05).
- **No balance in schema:** schemas constrain type/structure only (`snake_case`, integer for combat — ADR-011).
  Never hardcode real rates/pity/stats/curves into a schema. Fixtures use zeroed placeholders.
- **Versioning:** breaking change (remove/rename field, tighten type, narrow enum, change meaning) ⇒ bump
  `schema_version` + add a migration under `shared/config-schema/_versions/` + doc-sync. Additive change
  (new optional field, new enum value) does NOT bump. See `_versions/README.md`.
- **After changing schema:** run schema self-validation + fixture validation (valid pass / invalid fail) and
  a documentation-consistency check. Draft 2020-12 tool: Python `jsonschema` or AJV.
- **Cross-references:** schema validates only the *format* of an id ref (prefix/pattern). Existence of a
  referenced id across files (hero→skill, stage→reward…) is **referential integrity = phase 07 validator** —
  JSON Schema alone does NOT validate cross-file id existence. Do not claim otherwise.
- **Boundaries:** validator tool + CI gate = phase 07; runtime Configuration Service = phase 21; real
  balance = feature/tuning phases.
