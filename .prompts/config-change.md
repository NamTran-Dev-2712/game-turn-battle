# Prompt: Config / Data Change (data-driven)

For changes to game config data or its schema (data-driven design, ADR-004/005).

```
Change config: <which domain: economy | gacha | heroes | quests | rewards | shop | skills | stages | liveops>.

DESIGN & DECISION
- ADR-004 (data-driven), ADR-005 (configuration strategy / versioning).
- Schema: shared/config-schema/config-bundle.schema.json
- Docs: docs/gameplay/configuration-and-data.md, docs/liveops/remote-config.md

RULES
- Gameplay values live in config, never hardcoded. Domain/App read via IConfigProvider only.
- Schema change ⇒ bump config_version per ADR-005 and update the schema + validator (tools/config-validator:
  ConfigFileMapper/ReferenceValidator) + its tests. A new config type needs schema + validator support + a test.
- Never edit config to silence a real violation or bypass the gate to make CI green — fix the correct layer.
- Balance values needing tuning ⇒ mark per docs/mvp/10-open-questions.md (EC), don't guess final numbers.

DONE
- Ran `bash tools/config-validator/run.sh config shared/config-schema` → exit 0 (schema + referential
  integrity + schema_version). CI validate-config green. Error codes: tools/config-validator/README.md.
- Docs synced (.claude/workflows/documentation-sync.md: config-schema + config-validator rows).
```
