# `client/src/ui/` — UI layer dùng chung

| Mục | Nội dung |
|---|---|
| **Purpose** | Theme, widget tái sử dụng, layout **landscape** (`mvp/00`). |
| **Responsibilities** | Thành phần UI chung cho mọi feature; không chứa logic nghiệp vụ. |
| **Allowed** | Scene/script widget, theme resource, style. |
| **Not allowed** | ❌ logic gameplay/nghiệp vụ; ❌ gọi backend trực tiếp (feature làm việc đó). |
| **Dependencies** | `src/shared`. Được feature dùng lại. |
| **Owner** | Client UI team. |
| **Future expansion** | Thêm widget/theme; hỗ trợ đa độ phân giải. |

Chi tiết: `../../../docs/godot/ui-architecture.md`.
