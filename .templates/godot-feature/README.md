# Template: Godot Feature

Scaffold for one client feature. Matches `docs/godot/scene-architecture.md` and ADR-002. **No
gameplay logic here** — structure stubs only.

## Layout
Place under `client/src/features/<feature>/`:

```
features/<feature>/
  <feature>.tscn                 # feature root scene
  <feature>_controller.gd        # wires scene to services; listens/emits via EventBus
  <feature>_view.gd              # presentation only (no business decisions)
  data/                          # feature-local Resources (.tres) — presentation data, not balance
  <feature>_test.gd              # gdUnit4 test (in client/tests/, mirroring this path)
```

## Rules baked in
- **No cross-feature imports.** Talk to other features only through the core EventBus/signals (ADR-002).
- **No authority on the client.** Economy/result/reward decisions come from the server (ADR-007/011).
- Core services (`net/`, `config/`, `events/`, `state/`, `scene/`) are accessed, never duplicated.
- Static typing; `snake_case` members, `PascalCase` `class_name`, `CONSTANT_CASE` consts, `##` docs, **TAB** indent.

## Use
Copy `feature_controller.gd.template`, rename, drop `.template`, place under the feature folder.
Follow `.claude/prompts` → `.prompts/godot-feature.md`.
