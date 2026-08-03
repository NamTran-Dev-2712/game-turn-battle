# Prompt: Godot Client Feature

Specialization of `feature.md` for the Godot 4.7 client. Delegate to `.claude/agents/godot-client`.

```
Implement <feature> under client/src/features/<feature>/.

SSOT & DESIGN
- Business: docs/mvp/<file>   ·   Client design: docs/godot/<scene-architecture|state-and-signals|ui-architecture>
- Decisions: ADR-002 (architecture/EventBus), ADR-011 (combat authority) if relevant.

CLIENT RULES
- No cross-feature imports — communicate via EventBus/signals (ADR-002).
- No God autoload; keep core services single-purpose.
- No client-side authority (economy/result/reward) — call the server.
- Static typing; snake_case funcs/vars, PascalCase class_name, CONSTANT_CASE consts, ## docs, TAB indent.
- If combat sim: pure (node-decoupled), integer/fixed-point, seeded RNG; reproduce server golden vector.

DELIVERABLE
- Feature layout per .templates/godot-feature/ ; gdUnit4 tests; golden-vector test if sim touched.
- Finish with .claude/checklists/post-task.md.
```
