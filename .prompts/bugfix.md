# Prompt: Fix a Bug

```
Fix: <symptom / observed vs expected>.

REPRO
- Steps / input: <…>
- Expected: <…>   Actual: <…>
- Scope suspected: <backend | client | config | combat sim>

CONTEXT
- Business truth (if behavior is in question): docs/mvp/<file>
- Relevant ADR(s): <…>   Module doc: <docs/backend|godot|gameplay/...>

RULES
- Root-cause fix, not a symptom patch. No swallowed errors.
- Keep the diff minimal and focused. No opportunistic refactor in the same change.
- Add a regression test that fails before and passes after.
- If it touches the combat sim: preserve determinism (ADR-011) and update golden vectors deliberately.

DONE
- Regression test + existing suite green (dotnet test / gdUnit4).
- Docs synced if behavior/boundary changed (.claude/workflows/documentation-sync.md).
```

Agent: `.claude/agents/dotnet-backend` / `godot-client`; for hard-to-repro use a systematic debug pass.
