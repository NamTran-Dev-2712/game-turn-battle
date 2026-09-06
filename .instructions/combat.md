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
- **Balance numbers = config** (`combat_int`) — never hardcode/invent. **CB3** (aggro) is `[ĐỀ XUẤT]`; **CB4** (energy/ultimate) MECHANISM is now implemented (Phase 28, config-gated) but its NUMBERS stay `[OPEN]` — never silently close a CB.
- **Scope:** server sim = phase 24 (DONE); client sim = phase 25 (DONE); vector suite + cross-impl CI gate = phase 26 (DONE); **skill framework = phase 28 (DONE — 14 vectors, registry effects damage/heal/buff/debuff + energy-ultimate)**. See `combat-framework.md` §22/§23 + `docs/gameplay/skill-framework.md` + `tools/combat-baseline/README.md`.

**Realized (Phase 24 — server .NET sim; REUSE, don't reinvent):**
- **Pure engine:** `GameTeam.Domain/Combat/` — `Numerics/FixedPoint`, `Rng/Pcg32`, `Model/*` (`BattleInput`…), `State/UnitState`, `Events/*`, `Effects/*` (registry), `Serialization/CombatEventSerializer`, `BattleSimulator` (entry: `Simulate(BattleInput) → BattleOutput`). Package-free, no `IClock`/wall-clock/`float`/global RNG. This is the **authority** — client (phase 25) must match it bit-for-bit; never define a divergent client result.
- **Data-driven layer:** `GameTeam.Application/Combat/CombatInputResolver` reads hero/skill/stage via `IConfigProvider` → builds `BattleInput`. Combat config POCOs live here (`combat_rules` sourced from **stage config** — schema formalization is a follow-up). No battle endpoint (phase 30).
- **Effects:** extend via `IEffectHandler` + `EffectRegistry` (`effect_type` → handler) + config; unknown type throws. Never `switch(skillId)` in the core. `CreateDefault` = damage/heal/apply_buff/apply_debuff.
- **Determinism guards:** `GameTeam.Domain.Tests/Combat` (golden vectors + N=200 byte-identical) + `GameTeam.Application.Tests/Combat` (data-driven + `CombatPuritySourceScanTests` banning float/double/DateTime/RNG-global) + NetArchTest. Tests are the behavior contract — update them with any sim change; never edit a golden vector to make CI green.

**Realized (Phase 28 — skill framework; REUSE, don't reinvent):**
- **Skill = effect-data + registry** on both sides (server `GameTeam.Domain/Combat/Effects/*`, client `client/src/combat/effects/*`). Base effects: `damage`, `heal` (emits `Healed`), `apply_buff`/`apply_debuff` (flat `atk/def/spd` modifier + `duration`, keyed `(source_skill_id,stat)` ⇒ **refresh-on-reapply, never stacks**; `BuffApplied`/`BuffExpired`), + **energy-ultimate** (§15 `on_attack`/`on_hit`/`energy_cost`/`cooldown_rounds`/`EnergyChanged`). Buff/debuff share `StatModifierCore` (opposite sign).
- **Energy/ultimate is config-gated, default-OFF** (gains 0 ⇒ no `EnergyChanged` ⇒ the 9 Phase-26 vectors stay **byte-identical**). Per-unit `UnitSkillSet {basic, ultimate?}` (absent ⇒ shared basic skill); per-effect target = minimal deterministic subset (`single_enemy`(default)/`single_ally`/`self`); attack (has `damage`) rolls hit/crit, pure heal/buff rolls nothing. `vector_10..14` cover it via `config_excerpt.skills` + `team_snapshot[].skills` (extend all **3** loaders in lockstep).
- **Adding a skill from existing effect types = CONFIG-ONLY** (no core change). New effect type = 1 handler/side in `create_default()`/`CreateDefault()` + additive enum in `skill.schema.json` + config + test. The only `effect_type ==` allowed is the attack-vs-non-attack check, never handler dispatch. Canon: `docs/gameplay/skill-framework.md` + `combat-framework.md` §23; `.memory/0026`.

**After any combat-doc/spec change (completion workflow — same as CLAUDE.md §4.5/§4.6):** read the spec + ADR-011 first → don't invent beyond scope → keep `combat-framework.md` §9–§20/§22 + `code-style.md` §4 + `shared/combat-vectors/*` (baseline via `tools/combat-baseline`) + the `golden-vector` gate + open-questions CB1–CB6 in sync → regenerate baseline deliberately (server-generated, review diff) → run the combat-determinism self-review + the golden gate on **both** sides → tick the phase checklist `[x]` **only** after verification.
