# `config/` — Dữ liệu data-driven (author-time)

> Nội dung gameplay **data-driven**: hero, skill, stage, gacha, shop, reward, economy, quest, liveops. Tách khỏi code để tune không cần build (ADR-004/005).

| Mục | Nội dung |
|---|---|
| **Purpose** | Nguồn **author-time** của mọi số liệu/cân bằng gameplay. |
| **Responsibilities** | Lưu config dạng JSON theo `../shared/config-schema/`; version qua `_versions/`. |
| **Allowed** | `.json` config theo schema. |
| **Not allowed** | ❌ hardcode số cân bằng trong code; ❌ file không hợp schema. |
| **Dependencies** | Validate bởi `tools/config-validator` (CI `validate-config.yml`); publish lên Configuration Service (backend) — **runtime nguồn sự thật là backend**, client cache. |
| **Owner** | Game design + content team. |
| **Future expansion** | Thêm loại content; pipeline import từ bảng tính (`tools/content-importer`). |

## Cấu trúc (SSOT: `../docs/architecture/project-structure.md` §6)
```text
config/
├── heroes/    skills/    stages/    gacha/
├── shop/      rewards/   economy/   quests/
├── liveops/   (event/season/flag — Post-MVP)
└── _versions/ (metadata phiên bản bundle — ADR-005)
```

> **Bootstrap:** các thư mục hiện chỉ có README (chưa author giá trị gameplay). Điền ở phase Gameplay Systems, trỏ nguồn `docs/mvp/*`.
