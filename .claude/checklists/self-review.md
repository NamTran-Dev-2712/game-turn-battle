# Checklist: Self-Review (before commit / declaring Done)

Apply the full checklist in `docs/ai/review-and-dod.md` §1. Quick pass:

- [ ] **Acceptance** met — and nothing beyond scope crept in.
- [ ] **Architecture:** dependency rule holds; no God object; no cross-feature client import; Domain pure.
- [ ] **Server authority + data-driven:** no client-side sensitive decision; no hardcoded balance.
- [ ] **Combat (if touched):** integer/fixed-point, seeded RNG, golden vector updated deliberately.
- [ ] **Naming:** per `docs/conventions/naming.md` + glossary; no magic numbers; small functions.
- [ ] **Errors/edges** handled; no swallowed exceptions; no secrets in code.
- [ ] **Tests** for new logic; `dotnet test` / gdUnit4 green; config validated if schema changed; CI green.
- [ ] **Docs synced** per `.claude/workflows/documentation-sync.md`.
- [ ] **Traceability:** links the ADR/`docs/mvp/*`; open-questions recorded.

If any box fails, fix it before finishing — don't defer silently.
