# Hướng dẫn phát triển với AI (AI Guide)

> Repo này được thiết kế cho **AI-assisted development**. Điểm vào đầy đủ: [`docs/ai/`](docs/ai/). Tài liệu này là bản rút gọn + bản đồ thư mục AI ở gốc.
>
> **Claude Code:** điểm vào **tự nạp mỗi phiên** là [`CLAUDE.md`](CLAUDE.md) (execution layer, tiếng Anh) — nó trỏ về `docs/ai/` chứ không thay thế.

## Nạp ngữ cảnh trước khi code (bắt buộc)
Theo [docs/ai/context-strategy.md](docs/ai/context-strategy.md):
1. **Mục tiêu + acceptance** của task.
2. **SSOT nghiệp vụ** liên quan ([docs/mvp/](docs/mvp/)) — file cụ thể, không đổ hết.
3. **ADR** liên quan ([docs/adr/](docs/adr/)).
4. **Ranh giới module** + conventions ([dependency-graph](docs/architecture/dependency-graph.md), [docs/conventions](docs/conventions/)).
5. **Code hiện có** của module (tái sử dụng).
6. Bắt đầu **nhỏ, có test**.

## Luật vàng
- Tuân [docs/ai/coding-rules.md](docs/ai/coding-rules.md) & **Forbidden Patterns** §3.
- Không đổi SSOT/ADR; mơ hồ → ghi [open-questions](docs/mvp/10-open-questions.md), **không đoán**.
- Server-authoritative + data-driven + determinism combat.
- "Done" theo [docs/ai/review-and-dod.md](docs/ai/review-and-dod.md).

## Thư mục AI ở gốc (scaffolding)
| Thư mục | Vai trò |
|---|---|
| [`.claude/`](.claude/) | Cấu hình/agent/skill cho Claude Code (nếu dùng) |
| [`.instructions/`](.instructions/) | Chỉ dẫn theo phạm vi/loại task |
| [`.prompts/`](.prompts/) | Prompt mẫu tái sử dụng |
| [`.agents/`](.agents/) | Định nghĩa agent chuyên biệt |
| [`.context/`](.context/) | Gói ngữ cảnh tổng hợp (trỏ docs/) |
| [`.rules/`](.rules/) | Luật rút gọn (mirror docs/ai + conventions) |
| [`.templates/`](.templates/) | Template code/tài liệu |
| [`.tasks/`](.tasks/) | Ghi chú task/hand-off giữa phiên |
| [`.memory/`](.memory/) | Nhật ký quyết định phạm vi dự án |

> Các thư mục này **bổ sung** cho `docs/ai/` (nguồn chuẩn), **không** thay thế. Xem [docs/audit/bootstrap-audit.md](docs/audit/bootstrap-audit.md).
>
> **Đã dựng execution layer (tiếng Anh):** [`CLAUDE.md`](CLAUDE.md) + [`.claude/`](.claude/) (settings, `agents/`, `workflows/`, `checklists/`); thư viện [`.prompts/`](.prompts/), [`.templates/`](.templates/), [`.context/`](.context/); lớp mỏng [`.rules/`](.rules/), [`.instructions/`](.instructions/), [`.memory/`](.memory/), [`.tasks/`](.tasks/), [`.agents/`](.agents/). Chính sách cập nhật tài liệu bắt buộc: [`.claude/workflows/documentation-sync.md`](.claude/workflows/documentation-sync.md).
