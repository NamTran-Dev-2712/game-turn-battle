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
- Schema change ⇒ bump config_version per ADR-005 and update the schema + validator expectations.
- Balance values needing tuning ⇒ mark per docs/mvp/10-open-questions.md (EC), don't guess final numbers.

DONE
- config validates (schema + referential integrity). CI validate-config green.
- Docs synced (.claude/workflows/documentation-sync.md: config-schema row).
```
