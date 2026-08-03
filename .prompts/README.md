# `.prompts/` — Prompt mẫu tái sử dụng

| Mục | Nội dung |
|---|---|
| **Purpose** | Lưu prompt chuẩn cho task hay lặp (tạo feature, viết test, review). |
| **Responsibilities** | Kèm **context package** đúng chuẩn ([context-strategy](../docs/ai/context-strategy.md) §1). |
| **Allowed** | File `.md` prompt template. |
| **Not allowed** | ❌ prompt khuyến khích vi phạm Forbidden Patterns. |
| **Dependencies** | `docs/ai/`, `docs/mvp/`, `docs/adr/`. |
| **Owner** | AI-enablement. |
| **Future expansion** | Thư viện prompt theo phase. |

## Nội dung (thư viện prompt — tiếng Anh)
Chung: [`feature`](feature.md), [`bugfix`](bugfix.md), [`refactor`](refactor.md),
[`review`](review.md), [`documentation`](documentation.md), [`testing`](testing.md),
[`architecture-adr`](architecture-adr.md).
Chuyên biệt: [`backend-feature`](backend-feature.md), [`godot-feature`](godot-feature.md),
[`combat`](combat.md), [`config-change`](config-change.md).

> Mỗi prompt kèm sẵn khung context package theo [context-strategy](../docs/ai/context-strategy.md) §1.
