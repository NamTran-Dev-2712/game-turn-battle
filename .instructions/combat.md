# Instructions: Combat scope (deterministic sim)

Short execution hints. Canonical: **ADR-011** + `docs/gameplay/combat-framework.md` **§9–§20** (the Phase-23 spec).
Golden-vector format + samples: `shared/combat-vectors/`. Agent: `.claude/agents/combat-determinism`. Prompt: `.prompts/combat.md`.

- Integer/fixed-point only — **no floats** in the sim.
- Seeded PRNG passed in — **no** global/ambient RNG.
- **No** wall-clock time in sim logic.
- Server is authoritative; the client sim is prediction/replay that must match the server golden vector.
- Sim behavior change → update golden vectors **deliberately**, explain WHY in the PR.
- Business truth: `docs/mvp/02-core-game-loop.md`, `docs/mvp/03-core-gameplay.md`.

**Locked contract (Phase 23 — spec is canon; implement, don't re-decide):**
- **Fixed-point:** 64-bit × `FIXED_SCALE=1000`, **round-half-up** as the *single* rounding law (every `fixed_mul`/`fixed_div`/`from_fixed`). No floor/banker's; divide-by-zero is a guard, never NaN/float.
- **PRNG:** **PCG32** (`pcg_setseq_64_xsh_rr_32`) + **SplitMix64** seed expansion; one stream/battle; seed is a `uint64` **server-generated** input. Use **logical** shifts, **wrapping** 64-bit multiply. `pcg_bounded` unbiased; rolls in basis points [0,10000).
- **Action order:** stable speed-sort `(-spd, actor_id)` each round. Never hash/iteration/insertion/DB order.
- **RNG order:** `hit` roll then `crit` roll; **miss = 1 roll, hit = 2 rolls** (consume the crit roll even when `crit_rate_bp==0`).
- **Damage:** divisive DEF-ratio `atk*coeff*K/(K+def)`, crit **after** mitigation, final `from_fixed`, floor `MIN_DMG`.
- **Balance numbers = config** (`combat_int`) — never hardcode/invent. CB3/CB4 are `[ĐỀ XUẤT]` (pending product); never silently close a CB.
- **Scope:** sim code = phases 24/25; full vector suite + cross-impl CI gate = phase 26. Not here.

**After any combat-doc/spec change (completion workflow — same as CLAUDE.md §4.5/§4.6):** read the spec + ADR-011 first → don't invent beyond scope → keep `combat-framework.md` §9–§20 + `code-style.md` §4 + `shared/combat-vectors/*` + open-questions CB1–CB6 in sync → update golden vectors deliberately → run the combat-determinism self-review → tick the phase checklist `[x]` **only** after verification.
