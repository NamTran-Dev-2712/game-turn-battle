# `shared/config-schema/` — JSON Schema cho config

| Mục | Nội dung |
|---|---|
| **Purpose** | JSON Schema validate **mọi** file trong `../../config/` (ADR-005) — chặn config sai khi live. |
| **Responsibilities** | Định nghĩa schema cho heroes/skills/stages/gacha/shop/rewards/economy/quests/liveops + versioning. |
| **Allowed** | `*.schema.json`. |
| **Not allowed** | ❌ dữ liệu config thật (thuộc `../../config/`). |
| **Dependencies** | `tools/config-validator` dùng để validate ở CI (`validate-config.yml`). |
| **Owner** | Platform/content-tools team. |
| **Future expansion** | `schema_version` + migration khi đổi cấu trúc (ADR-005). |

> **Bootstrap:** kèm 1 schema mẫu `config-bundle.schema.json` (khung tối thiểu). Schema đầy đủ thêm ở phase Core Framework (S2).
