# `client/src/` — Mã nguồn client (feature-based)

> Tổ chức **theo feature, không theo loại file**. Mỗi feature tự chứa scene + script + resource để dễ nạp ngữ cảnh (AI/dev) và giảm coupling.

| Mục | Nội dung |
|---|---|
| **Purpose** | Chứa toàn bộ GDScript/scene của client. |
| **Responsibilities** | Chia mã theo tầng `core` (nền) và `features` (nghiệp vụ hiển thị), tách `combat`/`ui`/`data`/`shared`. |
| **Allowed** | `.gd`, `.tscn`, `.tres`. |
| **Not allowed** | ❌ import chéo giữa các feature (giao tiếp qua Event Bus/signals — ADR-002); ❌ God autoload. |
| **Dependencies** | `core` được feature dùng; feature KHÔNG được import feature khác. |
| **Owner** | Client team. |
| **Future expansion** | Thêm thư mục con trong `features/`. |

Xem chi tiết ranh giới: `../../docs/godot/scene-architecture.md` · `../../docs/architecture/dependency-graph.md`.
