# 0004 — Config JSON schemas standardized (Phase 06)

- Date: 2026-08-11
- Scope: config / data-driven
- Status: Active

## Decision
The data-driven config contract is standardized and **closed** in roadmap Phase 06
(`docs/roadmap/06-config-json-schema.md`). Source of truth is **JSON Schema (draft 2020-12) in
`shared/config-schema/`**: **8 per-type schemas** (`hero`/`skill`/`stage`/`gacha`/`shop`/`reward`/
`economy`/`quest`.schema.json) + **`common.schema.json`** (shared `$defs`: id-prefix patterns,
`combat_int` = integer ≥ 0, enums `class`/`element`/`role`/`currency`/`rarity` matching
`GameTeam.Contracts` (Phase 05), `faction` as an open string, `cost`) + **`config-bundle.schema.json`**
(the single envelope — `schema_version` + `config_version` `^config@v[0-9]+$`, `config@vN` compatible).

Rules baked into the schemas: keys `snake_case`; combat/gameplay values **integer** (ADR-011, no float);
every file carries `schema_version` (integer ≥ 1); per-type ID prefixes (`hero_`, `skill_`, `stage_`,
`gacha_`, `shop_`, `reward_`, `economy_`, `quest_`) validated by pattern; `additionalProperties:false` +
`required` where tight. Schemas define **structure/type only — never balance values** (rates, pity, stats,
curves stay 0/open). Cross-file id references (hero→skill, stage→reward, gacha→hero, shop→reward) are
represented structurally but their **existence is not** checked by a single schema.

Minimal **fixtures** live in `shared/config-schema/fixtures/` — one `*.valid.json` (passes) and one
`*.invalid.json` (fails for a meaningful reason: missing `schema_version`, float-in-combat, wrong id
prefix, unknown enum, `additionalProperties` violation, missing required, below-minimum) per type.
Migration rules are in `shared/config-schema/_versions/README.md`.

Future agents **reuse and only extend** this — inspect the 8 schemas + `common.schema.json` before adding
any config field, enum value, effect type, quest/condition type, currency, or reward type; **never** invent
gameplay not backed by `docs/gameplay/*` + ADRs; **never** create a second config envelope. Evolution is
**additive-only** by default (new optional field / new enum value = no bump); a **breaking** change
(remove/rename field, tighten type, narrow enum, change meaning) requires **`schema_version` bump +
migration under `_versions/` + doc-sync** (`docs/gameplay/configuration-and-data.md`,
`docs/liveops/remote-config.md`, `.instructions/config.md`).

## Why
Data-driven is the foundation of all gameplay + LiveOps (ADR-004/005): the schema (contract) must exist
before authors fill balance and before the Configuration Service (Phase 21) loads/validates bundles.
Fixing the shape once — without freezing balance — avoids large refactors when content is added later.
Verified by real runs (jsonschema 4.23.0, Python, Draft 2020-12): 10/10 schemas self-validate, 8/8 valid
fixtures pass, 8/8 invalid fixtures fail for the intended reason (incl. missing `schema_version` and
float-in-combat). Enums reuse Phase-05 `GameTeam.Contracts` — no second enum source.

## Not this
Locking a `faction` enum was rejected — GP2 is unresolved in `docs/mvp/10-open-questions.md`, so `faction`
is an open non-empty string until the list is decided (mirrors `.memory/0003` shipping `Faction=None`
only). A Node/AJV toolchain was **not** added (repo is .NET + Python; validation used Python `jsonschema`
for evidence only). The **validator tool + cross-file referential-integrity + mandatory CI gate** are
explicitly **Phase 07** (`docs/roadmap/07-config-validator-tool.md`); the runtime **Configuration Service**
is **Phase 21**; **real balance values** are feature/tuning work — none were implemented here.
