# `client/tests/` — Test Godot (gdUnit4)

| Mục | Nội dung |
|---|---|
| **Purpose** | Unit/integration test client, gồm **golden vector** combat. |
| **Responsibilities** | Kiểm chứng logic client (đặc biệt `src/combat`, `src/shared`), khớp sim với server. |
| **Allowed** | Test script gdUnit4, fixture, test vector. |
| **Not allowed** | ❌ phụ thuộc backend thật (mock/hằng); ❌ test không xác định. |
| **Dependencies** | `addons/` (gdUnit4), `src/*`. |
| **Owner** | Client team. |
| **Future expansion** | Tăng coverage vùng rủi ro (combat/kinh tế). |

Chi tiết: `../../docs/testing/godot-testing.md`.
