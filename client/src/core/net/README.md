# `core/net/` — NetworkClient (Phase 15 — đã chốt)

Kênh giao tiếp server **DUY NHẤT** của client (ADR-008, ADR-002). UI/feature **không** gọi `HTTPRequest` trực tiếp — luôn qua đây. **Không** chứa logic gameplay; client **không** tự quyết kết quả/phần thưởng (ADR-011).

| File | Vai trò |
|---|---|
| `network_client.gd` | Autoload `NetworkClient` (bỏ `class_name`). `get_json(path, parser)` / `post_json(path, body, parser)`, gắn JWT, chuẩn hoá lỗi → `NetResult`, phát `network_error`/`unauthorized` (EventBus), timeout + retry (chỉ GET). Base URL: env `GAME_TEAM_API_BASE_URL` (mặc định `http://localhost:8080`), path dưới `/api/v1`. |
| `http_transport.gd` | `HttpTransport` — hợp đồng seam vận chuyển (`send(req)`), cho test dùng transport giả. |
| `godot_http_transport.gd` | `GodotHttpTransport` — bọc `HTTPRequest` (nơi DUY NHẤT chạm `HTTPRequest`). |
| `token_store.gd` | `TokenStore` — kho JWT tối giản trong bộ nhớ (đăng nhập/refresh thật = phase 18/20). |
| `net_result.gd` | `NetResult` — kết quả chuẩn hoá (`ok`/`value`/`error`/`status_code`/`kind`). |
| `response_parser.gd` | `NetworkResponseParser` — JSON → model generated (phase 08). Thêm DTO = thêm một hàm parse. |

**Quy tắc:** không log token/Authorization; POST không tự retry; mất mạng → báo lỗi, không bịa kết quả. Chi tiết: `../../../../docs/godot/state-and-signals.md` §4 + §3.1, `../../../../docs/adr/ADR-008-networking.md`. Decision log: `../../../../.memory/0013-client-networkclient-standardized.md`.
