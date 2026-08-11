# `.memory/` — Nhật ký quyết định phạm vi dự án

| Mục | Nội dung |
|---|---|
| **Purpose** | Ghi lại quyết định/ngữ cảnh lâu dài **của dự án** để agent/dev nhớ giữa phiên (khác bộ nhớ cá nhân của công cụ AI). |
| **Responsibilities** | Lưu "vì sao" cho quyết định không thuộc tầm ADR nhưng vẫn cần nhớ. |
| **Allowed** | File `.md` ghi chú, mỗi mục một chủ đề. |
| **Not allowed** | ❌ thay thế ADR (quyết định kiến trúc → `docs/adr/`); ❌ secret. |
| **Dependencies** | [`../docs/adr/`](../docs/adr/), [`../docs/mvp/`](../docs/mvp/). |
| **Owner** | Cả team. |
| **Future expansion** | Chuẩn hoá format; liên kết chéo. |

## Nội dung
- [`README-format.md`](README-format.md) — format một mục nhật ký quyết định.
- [`0001-ai-execution-layer.md`](0001-ai-execution-layer.md) — quyết định tách execution layer khỏi docs SSOT.
- [`0002-dev-environment-standardized.md`](0002-dev-environment-standardized.md) — dev env một lệnh (Phase 04): compose Postgres16/Redis7, network `game-team-dev`, profile `api`, `.env`, script up/down đa nền tảng.

> Quyết định kiến trúc **luôn** đi vào `docs/adr/`, không chỉ ở đây.
