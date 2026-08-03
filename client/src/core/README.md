# `client/src/core/` — Dịch vụ nền (Autoload services)

> Các autoload **tối giản, mỗi cái một việc** (SRP) — KHÔNG God autoload. Nền tảng mọi feature dựa vào.

| Mục | Nội dung |
|---|---|
| **Purpose** | Cung cấp dịch vụ nền: mạng, cache config, event bus, quản lý scene, cache state. |
| **Responsibilities** | `net/` gọi backend; `config/` cache config versioned; `events/` Event Bus toàn cục; `state/` cache trạng thái đọc-chỉ để hiển thị; `scene/` điều hướng scene. |
| **Allowed** | Script autoload single-responsibility + resource cấu hình. |
| **Not allowed** | ❌ logic gameplay; ❌ gộp nhiều trách nhiệm vào 1 autoload; ❌ quyết định nhạy cảm client-side. |
| **Dependencies** | `shared/contracts` (hợp đồng). Được mọi feature dùng; core KHÔNG phụ thuộc feature. |
| **Owner** | Client core team. |
| **Future expansion** | Thêm dịch vụ nền mới thành autoload riêng, có kiểm soát. |

Chi tiết: `../../../docs/godot/state-and-signals.md`.

## Thư mục con
- `net/` — NetworkClient: REST/JSON tới backend, xử lý auth token, retry/idempotency.
- `config/` — ConfigProvider: nhận & cache config bundle versioned từ backend (ADR-005).
- `events/` — EventBus: signal toàn cục để feature giao tiếp lỏng lẻo.
- `state/` — StateCache: cache đọc-chỉ phục vụ hiển thị (nguồn sự thật ở server).
- `scene/` — SceneRouter: chuyển cảnh, transition.
