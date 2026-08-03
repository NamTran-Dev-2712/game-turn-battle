# Instructions: Combat scope (deterministic sim)

Short execution hints. Canonical: **ADR-011** + `docs/gameplay/combat-framework.md`.
Agent: `.claude/agents/combat-determinism`. Prompt: `.prompts/combat.md`.

- Integer/fixed-point only — **no floats** in the sim.
- Seeded PRNG passed in — **no** global/ambient RNG.
- **No** wall-clock time in sim logic.
- Server is authoritative; the client sim is prediction/replay that must match the server golden vector.
- Sim behavior change → update golden vectors **deliberately**, explain WHY in the PR.
- Business truth: `docs/mvp/02-core-game-loop.md`, `docs/mvp/03-core-gameplay.md`.
