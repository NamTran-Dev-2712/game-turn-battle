# Prompt: Write Tests

```
Add tests for <target>.

PRIORITY (docs/ai/review-and-dod.md §3 + docs/testing/)
- Risk-first: combat, gacha, AFK, currency, save MUST be covered.
- Test behavior, not fragile internals. Keep tests deterministic (inject clock/RNG seeded).

STACK
- Backend: xUnit + FluentAssertions + NSubstitute (+ NetArchTest for boundaries). docs/testing/backend-testing.md
- Client: gdUnit4. docs/testing/godot-testing.md
- Combat: golden-vector fixtures shared client↔server — treat as a living spec.

DONE
- New/changed risk logic covered; coverage not reduced in risk areas.
- dotnet test / gdUnit4 green; CI green.
```
