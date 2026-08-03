# Context Pack: Feature Map

> Where each gameplay feature is designed and where its business truth lives. Design docs describe
> **module boundaries only** — the business SSOT is `docs/mvp/`.

| Feature | Design (module boundary) | Business SSOT |
|---|---|---|
| Heroes | `docs/gameplay/hero-system.md` | `docs/mvp/03-core-gameplay.md`, `05-player-progression.md` |
| Combat | `docs/gameplay/combat-framework.md` | `docs/mvp/02-core-game-loop.md`, `03-core-gameplay.md` |
| Skills | `docs/gameplay/skill-framework.md` | `docs/mvp/03-core-gameplay.md` |
| Inventory & Equipment | `docs/gameplay/inventory-and-equipment.md` | `docs/mvp/05-player-progression.md` |
| Quests | `docs/gameplay/quest-system.md` | `docs/mvp/04-feature-analysis.md` |
| Progression & Economy | `docs/gameplay/progression-and-economy.md` | `docs/mvp/05-player-progression.md`, `06-game-economy.md` |
| Config & Data | `docs/gameplay/configuration-and-data.md` | `docs/mvp/08-technical-impact.md` |
| LiveOps (remote config, flags, mail, scheduling) | `docs/liveops/` | `docs/mvp/07-liveops-planning.md` |

Client feature folders live under `client/src/features/`; backend features under
`server/src/GameTeam.Application/Features/`. Cross-feature rules: EventBus (ADR-002), server-auth (ADR-011).
