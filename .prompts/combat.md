# Prompt: Combat Sim Change

For any change to the deterministic combat simulation, on either side. Delegate to
`.claude/agents/combat-determinism`.

```
Change combat: <what behavior changes and why>.

SSOT & DECISION
- Business: docs/mvp/03-core-gameplay.md (+ 02-core-game-loop.md)
- Design: docs/gameplay/combat-framework.md, skill-framework.md
- DECISION: ADR-011 (combat authority & determinism) — if the decision itself changes, draft an ADR update first.

NON-NEGOTIABLES
- Integer / fixed-point only — no floats in the sim.
- Seeded PRNG passed in — no global/ambient RNG.
- No wall-clock time in sim logic.
- Server is authoritative; client sim must match the server golden vector bit-for-bit.

DELIVERABLE
- Update golden vectors DELIBERATELY in the same change; explain WHY.
- Client and server produce identical results for shared fixtures. Tests green both sides.
```
