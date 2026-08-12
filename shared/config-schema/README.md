# `shared/config-schema/` — JSON Schema cho config

| Mục | Nội dung |
|---|---|
| **Purpose** | JSON Schema validate **mọi** file trong `../../config/` (ADR-005) — chặn config sai khi live. |
| **Responsibilities** | Định nghĩa schema cho heroes/skills/stages/gacha/shop/rewards/economy/quests/liveops + versioning. |
| **Allowed** | `*.schema.json`, fixture test (`fixtures/`), ghi chú migration (`_versions/`). |
| **Not allowed** | ❌ dữ liệu config thật (thuộc `../../config/`); ❌ giá trị balance khoá cứng trong schema. |
| **Dependencies** | `tools/config-validator` dùng để validate ở CI (`validate-config.yml`) — phase 07. |
| **Owner** | Platform/content-tools team. |
| **Future expansion** | `schema_version` + migration khi đổi cấu trúc (ADR-005). |

## Nội dung (phase 06 — closed)

- `config-bundle.schema.json` — envelope metadata bundle (`schema_version` + `config_version`, tương thích `config@vN`).
- `common.schema.json` — `$defs` dùng lại (id prefix, `combat_int`, enum class/element/role/currency, rarity, faction, cost). Enum khớp `GameTeam.Contracts` (phase 05).
- Schema per-type: `hero`, `skill`, `stage`, `reward`, `gacha`, `shop`, `economy`, `quest` (`.schema.json`).
- `fixtures/` — mỗi type có `*.valid.json` (pass) + `*.invalid.json` (fail đúng quy tắc). Không phải balance thật.
- `_versions/` — quy tắc versioning & migration schema (xem `_versions/README.md`).

## Quy ước

- JSON Schema **draft 2020-12**; key `snake_case`; giá trị combat **integer** (ADR-011); mọi file có `schema_version`.
- ID prefix theo type: `hero_`, `skill_`, `stage_`, `gacha_`, `shop_`, `reward_`, `economy_`, `quest_`.
- Schema định nghĩa **cấu trúc/kiểu**, KHÔNG chứa giá trị balance (giá trị thật là tuning — `../../docs/gameplay/configuration-and-data.md`).
- Tham chiếu ID chéo (hero→skill, stage→reward…) biểu diễn ở cấp cấu trúc; **kiểm tồn tại id** là phase 07 (validator), không phải JSON Schema đơn.

> **Ranh giới:** phase 06 = schema + fixture + nền migration (tự validate cục bộ). Validator tool + referential integrity + CI gate = phase 07. Config Service runtime = phase 21.
