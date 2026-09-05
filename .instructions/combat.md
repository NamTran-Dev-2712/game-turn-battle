# Instructions: Combat scope (deterministic sim)

Short execution hints. Canonical: **ADR-011** + `docs/gameplay/combat-framework.md` **§9–§20** (the Phase-23 spec).
Golden-vector format + samples: `shared/combat-vectors/`. Agent: `.claude/agents/combat-determinism`. Prompt: `.prompts/combat.md`.

- Integer/fixed-point only — **no floats** in the sim.
- Seeded PRNG passed in — **no** global/ambient RNG.
- **No** wall-clock time in sim logic.
- Server is authoritative; the client sim is prediction/replay that must match the server golden vector.
- Sim behavior change → regenerate golden baseline **from the server** via `tools/combat-baseline generate` (never hand-edit a vector's `expected`), review the diff, explain WHY in the PR.
- Business truth: `docs/mvp/02-core-game-loop.md`, `docs/mvp/03-core-gameplay.md`.

**Locked contract (Phase 23 — spec is canon; implement, don't re-decide):**
- **Fixed-point:** 64-bit × `FIXED_SCALE=1000`, **round-half-up** as the *single* rounding law (every `fixed_mul`/`fixed_div`/`from_fixed`). No floor/banker's; divide-by-zero is a guard, never NaN/float.
- **PRNG:** **PCG32** (`pcg_setseq_64_xsh_rr_32`) + **SplitMix64** seed expansion; one stream/battle; seed is a `uint64` **server-generated** input. Use **logical** shifts, **wrapping** 64-bit multiply. `pcg_bounded` unbiased; rolls in basis points [0,10000).
- **Action order:** stable speed-sort `(-spd, actor_id)` each round. Never hash/iteration/insertion/DB order.
- **RNG order:** `hit` roll then `crit` roll; **miss = 1 roll, hit = 2 rolls** (consume the crit roll even when `crit_rate_bp==0`).
- **Damage:** divisive DEF-ratio `atk*coeff*K/(K+def)`, crit **after** mitigation, final `from_fixed`, floor `MIN_DMG`.
- **Balance numbers = config** (`combat_int`) — never hardcode/invent. CB3/CB4 are `[ĐỀ XUẤT]` (pending product); never silently close a CB.
- **Scope:** server sim = phase 24 (DONE); client sim = phase 25 (DONE); full vector suite + cross-impl CI gate = phase 26 (DONE — 9 multi-scenario vectors, baseline via `tools/combat-baseline`, `golden-vector` gate blocking on both `ci-server.yml` + `ci-client.yml`; both test suites auto-discover vectors). See `combat-framework.md` §22 + `tools/combat-baseline/README.md`.

**Realized (Phase 24 — server .NET sim; REUSE, don't reinvent):**
- **Pure engine:** `GameTeam.Domain/Combat/` — `Numerics/FixedPoint`, `Rng/Pcg32`, `Model/*` (`BattleInput`…), `State/UnitState`, `Events/*`, `Effects/*` (registry), `Serialization/CombatEventSerializer`, `BattleSimulator` (entry: `Simulate(BattleInput) → BattleOutput`). Package-free, no `IClock`/wall-clock/`float`/global RNG. This is the **authority** — client (phase 25) must match it bit-for-bit; never define a divergent client result.
- **Data-driven layer:** `GameTeam.Application/Combat/CombatInputResolver` reads hero/skill/stage via `IConfigProvider` → builds `BattleInput`. Combat config POCOs live here (`combat_rules` sourced from **stage config** — schema formalization is a follow-up). No battle endpoint (phase 30).
- **Effects:** extend via `IEffectHandler` + `EffectRegistry` (`effect_type` → handler) + config; unknown type throws. Never `switch(skillId)` in the core. `DamageEffectHandler`/`HealEffectHandler` are the samples; full content = phase 28.
- **Determinism guards:** `GameTeam.Domain.Tests/Combat` (golden vectors + N=200 byte-identical) + `GameTeam.Application.Tests/Combat` (data-driven + `CombatPuritySourceScanTests` banning float/double/DateTime/RNG-global) + NetArchTest. Tests are the behavior contract — update them with any sim change; never edit a golden vector to make CI green.
- **Energy/ultimate (§15, CB4 `[ĐỀ XUẤT]`):** wired but inactive at phase 24 (proposal, not canon). Don't activate/close it without product.

**After any combat-doc/spec change (completion workflow — same as CLAUDE.md §4.5/§4.6):** read the spec + ADR-011 first → don't invent beyond scope → keep `combat-framework.md` §9–§20/§22 + `code-style.md` §4 + `shared/combat-vectors/*` (baseline via `tools/combat-baseline`) + the `golden-vector` gate + open-questions CB1–CB6 in sync → regenerate baseline deliberately (server-generated, review diff) → run the combat-determinism self-review + the golden gate on **both** sides → tick the phase checklist `[x]` **only** after verification.
