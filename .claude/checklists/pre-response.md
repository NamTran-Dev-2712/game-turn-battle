# Checklist: Pre-Response (before proposing code or a design)

Fast gate to run before you output an implementation or architectural answer.

- [ ] Am I answering from the **SSOT**, not from assumption? (Cited the specific `docs/mvp/` / ADR?)
- [ ] Does this respect the **dependency rule** and module boundaries?
- [ ] Any **Forbidden Pattern** creeping in? (God object, hardcoded balance, float/global-RNG in
      combat, client authority, Domain framework dep, swallowed error — `docs/ai/coding-rules.md` §3)
- [ ] Am I **reusing** existing code/patterns instead of inventing new ones?
- [ ] Is the change **small and testable**, with the test identified?
- [ ] Any ambiguity I'm about to paper over? → record in `docs/mvp/10-open-questions.md` and ask instead.
