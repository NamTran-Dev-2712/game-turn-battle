# `.claude/` — Cấu hình Claude Code (nếu dùng)

| Mục | Nội dung |
|---|---|
| **Purpose** | Nơi đặt cấu hình project cho Claude Code: agents, skills, settings dùng chung team. |
| **Responsibilities** | Chuẩn hoá cách agent AI làm việc trong repo này. |
| **Allowed** | `agents/`, `skills/`, `settings.json` (dùng chung, không secret). |
| **Not allowed** | ❌ secret/token; ❌ ghi đè luật chuẩn ở `docs/ai/`. |
| **Dependencies** | Trỏ về [`../docs/ai/`](../docs/ai/) làm nguồn chuẩn. |
| **Owner** | Platform/AI-enablement team. |
| **Future expansion** | Thêm agent/skill chuyên biệt cho dự án. |

## Nội dung (execution layer — tiếng Anh)
- [`../CLAUDE.md`](../CLAUDE.md) — điểm vào tự nạp mỗi phiên (golden rules, thứ tự nạp context, doc-sync).
- [`settings.json`](settings.json) — permissions dùng chung, **không secret**.
- [`agents/`](agents/) — agent chuyên biệt cho repo (backend, client, combat, reviewer, docs-sync); charter ở [`../.agents/ROLES.md`](../.agents/ROLES.md).
- [`workflows/`](workflows/) — implementation, review, documentation-sync.
- [`checklists/`](checklists/) — startup, pre-response, self-review, commit, post-task.

> Nguồn chuẩn về quy tắc AI vẫn là [`docs/ai/`](../docs/ai/) + [`AI_GUIDE.md`](../AI_GUIDE.md); layer này **trỏ tới**, không thay thế.
