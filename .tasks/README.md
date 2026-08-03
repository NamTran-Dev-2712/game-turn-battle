# `.tasks/` — Ghi chú task & hand-off giữa phiên

| Mục | Nội dung |
|---|---|
| **Purpose** | Lưu ngữ cảnh task đang làm để trao giữa phiên (người ↔ AI, AI ↔ AI). |
| **Responsibilities** | Ghi mục tiêu, quyết định tạm, việc còn lại — chống mất ngữ cảnh. |
| **Allowed** | File `.md` mô tả task/tiến độ. |
| **Not allowed** | ❌ thay thế issue tracker chính thức; ❌ quyết định kiến trúc (đó là ADR). |
| **Dependencies** | [`../docs/ai/context-strategy.md`](../docs/ai/context-strategy.md) §3, [`../docs/roadmap/`](../docs/roadmap/). |
| **Owner** | Người/agent thực hiện task. |
| **Future expansion** | Tích hợp với task tracker. |

## Nội dung
- [`TEMPLATE.md`](TEMPLATE.md) — khung ghi chú hand-off giữa phiên (copy khi bắt đầu task).

> Không thay thế issue tracker chính thức — luôn liên kết issue/PR.
