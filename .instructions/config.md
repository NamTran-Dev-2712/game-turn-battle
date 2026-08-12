# Instructions: Config / data scope (`config/`, `shared/config-schema/`)

Short execution hints. Canonical: `docs/gameplay/configuration-and-data.md`, `docs/liveops/remote-config.md`.
Prompt: `.prompts/config-change.md`.

- Data-driven: all gameplay balance lives in config, never in code (ADR-004).
- Domain/Application read config only through `IConfigProvider` — never read files directly (ADR-005).
- Values needing tuning → mark in `docs/mvp/10-open-questions.md` (EC), don't invent final numbers.
- CI `validate-config` must pass (schema + referential integrity + `schema_version`) — GATE is live (phase 07).
  Run it after any `config/**` or `shared/config-schema/**` change: `bash tools/config-validator/run.sh config shared/config-schema`.

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
- **After changing schema OR config:** run `bash tools/config-validator/run.sh config shared/config-schema`
  (schema + referential integrity + version) — exit 0 required. Update the validator tests when you change
  validator behavior; never edit config to silence a real violation or bypass the gate to make CI green.
- **Cross-references:** schema validates only the *format* of an id ref (prefix/pattern). Existence of a
  referenced id across files (hero→skill, stage→reward…) is **referential integrity = the phase-07 validator**
  (`tools/config-validator`, codes `REF001`/`REF002`) — JSON Schema alone does NOT validate cross-file id
  existence. Do not claim otherwise.

## Config validator (phase 07 — closed & verified)

- **One tool:** `tools/config-validator` (.NET 9, `JsonSchema.Net`). Core lib `GameTeam.ConfigValidator` is the
  reusable validation boundary; the CLI is thin. Reuse it — do NOT reinvent a second validation mechanism.
- **A new config type MUST** ship: its schema (phase-06 rules) + validator support (`ConfigFileMapper` mapping +
  `ReferenceValidator` refs) + a test — in the same change. Do NOT invent config relationships or `schema_version`
  values not backed by the schemas + gameplay docs + ADRs.
- **Error codes** (`JSON001`/`MAP001`/`SCH001`/`VER001`/`VER002`/`REF001`/`REF002`) + report format
  `file:jsonpath:CODE message`: see `tools/config-validator/README.md`.
- **Boundaries:** validator tool + CI gate = phase 07 (done); runtime Configuration Service = phase 21 (reuses the
  validator core via `ConfigValidationRunner.Run(...)`); real balance = feature/tuning phases.
