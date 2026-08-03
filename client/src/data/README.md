# `client/src/data/` — Resource models (data-driven)

| Mục | Nội dung |
|---|---|
| **Purpose** | Định nghĩa Resource (`.gd` class + `.tres`) làm "khuôn" cho config data-driven (ADR-004). |
| **Responsibilities** | Map cấu trúc config schema (`../../../shared/config-schema`) sang Resource cho client dùng. |
| **Allowed** | Lớp `Resource`, `.tres` mẫu. |
| **Not allowed** | ❌ nhúng số cân bằng cố định; giá trị đến từ config runtime. |
| **Dependencies** | `shared/config-schema` (đồng bộ cấu trúc). |
| **Owner** | Client core/data team. |
| **Future expansion** | Thêm model khi thêm loại config. |

Chi tiết: `../../../docs/godot/resources-and-assets.md`, `../../../docs/adr/ADR-004-data-driven-design.md`.
