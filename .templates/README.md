# `.templates/` — Template code & tài liệu

| Mục | Nội dung |
|---|---|
| **Purpose** | Khuôn mẫu tạo nhanh: feature client, feature-folder Application (command/query/handler/validator), ADR, test. |
| **Responsibilities** | Đảm bảo cấu trúc mới đúng convention ngay từ đầu. |
| **Allowed** | File template + hướng dẫn dùng. |
| **Not allowed** | ❌ template chứa logic gameplay thật; ❌ vi phạm ranh giới. |
| **Dependencies** | [`../docs/conventions/`](../docs/conventions/), [`../docs/backend/`](../docs/backend/), [`../docs/godot/`](../docs/godot/). |
| **Owner** | Platform. |
| **Future expansion** | Thêm template theo nhu cầu; nối với codegen. |

## Nội dung (scaffold — tiếng Anh, không chứa logic gameplay)
- [`backend-feature-folder/`](backend-feature-folder/) — feature CQRS (command/query + handler + validator).
- [`godot-feature/`](godot-feature/) — layout feature client + controller mẫu (EventBus, không authority).
- [`test-backend.md`](test-backend.md), [`test-godot.md`](test-godot.md) — khung test.

> ADR template có sẵn ở [`../docs/adr/README.md`](../docs/adr/README.md) — dùng bản đó, không trùng lặp.
