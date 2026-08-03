# `client/addons/` — Plugin Godot

| Mục | Nội dung |
|---|---|
| **Purpose** | Plugin editor & runtime bên thứ ba (gdUnit4 test, editor tools). |
| **Responsibilities** | Chứa addon theo chuẩn Godot (`addons/<plugin>/plugin.cfg`). |
| **Allowed** | Mã plugin có license rõ ràng. |
| **Not allowed** | ❌ logic gameplay của game (đặt ở `src/`). |
| **Dependencies** | Bật/tắt trong `project.godot`. |
| **Owner** | Client tooling team. |
| **Future expansion** | Thêm addon qua PR + ghi license (ADR-010 tinh thần). |

Chi tiết: `../../docs/godot/tooling-and-testing.md`.
