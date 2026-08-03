# `.context/` — Gói ngữ cảnh tổng hợp

| Mục | Nội dung |
|---|---|
| **Purpose** | Tập hợp "context package" gọn để nạp nhanh cho một mảng công việc (trỏ tới file `docs/` liên quan). |
| **Responsibilities** | Giúp AI nạp **đúng & đủ**, không đổ hết `docs/`. |
| **Allowed** | File `.md` liệt kê nguồn ngữ cảnh theo chủ đề. |
| **Not allowed** | ❌ sao chép nội dung docs (dễ lệch) — chỉ **liên kết**. |
| **Dependencies** | [`../docs/ai/context-strategy.md`](../docs/ai/context-strategy.md). |
| **Owner** | AI-enablement. |
| **Future expansion** | Context pack theo feature/phase. |

## Nội dung (context pack — chỉ liên kết docs, không sao chép)
- [`project-overview`](project-overview.md), [`current-milestone`](current-milestone.md),
  [`feature-map`](feature-map.md), [`dependency-map`](dependency-map.md),
  [`active-decisions`](active-decisions.md).

> Mỗi file chỉ **trỏ** tới nguồn chuẩn trong `docs/`; cập nhật khi ranh giới/quyết định đổi.
