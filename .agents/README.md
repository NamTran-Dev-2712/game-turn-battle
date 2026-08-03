# `.agents/` — Định nghĩa agent chuyên biệt

| Mục | Nội dung |
|---|---|
| **Purpose** | Mô tả vai trò/agent AI chuyên biệt (architect, coder, reviewer, debugger…) và ranh giới của họ. |
| **Responsibilities** | Chuẩn hoá phạm vi & công cụ mỗi agent; tránh vượt ranh giới kiến trúc. |
| **Allowed** | File định nghĩa agent (`.md`/cấu hình). |
| **Not allowed** | ❌ agent được phép đổi SSOT/ADR tự ý. |
| **Dependencies** | [`../docs/ai/`](../docs/ai/). |
| **Owner** | AI-enablement. |
| **Future expansion** | Đồng bộ với `.claude/agents/` nếu dùng Claude Code. |

## Nội dung
- [`ROLES.md`](ROLES.md) — charter các vai trò (architect, planner, coder backend/client, combat, reviewer, docs-sync, debugger).

> Bản thực thi cho Claude Code ở [`../.claude/agents/`](../.claude/agents/); giữ hai bên đồng bộ.
