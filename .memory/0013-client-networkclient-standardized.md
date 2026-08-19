# 0013 — Client NetworkClient standardized (Phase 15)

- Date: 2026-08-19
- Scope: workspace
- Status: Active

## Decision

`NetworkClient` ở `client/src/core/net/` là **kênh giao tiếp server DUY NHẤT** của client (ADR-008, ADR-002),
nền cho mọi feature gọi API về sau. Convention dưới đây là **hợp đồng client↔server**: UI/feature → `NetworkClient`
→ REST `/api/v1` → server; **không bao giờ** UI/feature → `HTTPRequest` → server. Client **không** tự quyết kết
quả/phần thưởng — mất mạng → báo lỗi, không bịa (ADR-008/011).

- **`network_client.gd`** (autoload node `NetworkClient`, **bỏ `class_name`** — trùng singleton): coroutine
  **`get_json(path, parser)`** / **`post_json(path, body, parser)`**. Base URL = env **`GAME_TEAM_API_BASE_URL`**
  (mặc định `http://localhost:8080`), path dưới `/api/v1`; header **`Authorization: Bearer <jwt>`** chỉ khi
  `TokenStore` có token. `request_timeout_seconds` (mặc định 10s). Retry **chỉ GET/idempotent** trên lỗi vận chuyển
  tạm thời (timeout/mất kết nối), tối đa **`MAX_GET_RETRIES=2`**; **POST không bao giờ tự retry**. Mọi thất bại phát
  **`network_error`** (một kênh nhất quán); **401 phát thêm `unauthorized`**. **Không log token/Authorization/body**
  (chỉ `push_warning` code+status).
- **`http_transport.gd`** (`HttpTransport`) + **`godot_http_transport.gd`** (`GodotHttpTransport`): seam vận chuyển.
  `HttpTransport.send(req)` trả kết quả thô dạng tín hiệu `request_completed` `{result, response_code, headers, body}`.
  `GodotHttpTransport` bọc **`HTTPRequest`** — nơi **DUY NHẤT** chạm HTTPRequest (grep guard: chỉ `core/net/`).
- **`token_store.gd`** (`TokenStore`): kho JWT tối giản trong bộ nhớ (seam). Đăng nhập/lấy token/refresh/lưu bền
  thật = **phase 18/20** (thay/nâng cấp store). Không persist, không log.
- **`net_result.gd`** (`NetResult`): kết quả chuẩn hoá `ok`/`value`/`error: ErrorResponse`/`status_code`/`kind`
  (`enum Kind { SUCCESS, HTTP_4XX, HTTP_5XX, UNAUTHORIZED, INVALID_JSON, PARSE_ERROR, TIMEOUT, NETWORK_ERROR }`).
- **`response_parser.gd`** (`NetworkResponseParser`): JSON (camelCase) → **model generated phase 08** (snake_case),
  hàm tĩnh/DTO. Phase 15 dùng `parse_server_time` (utcNow→utc_now) + `parse_error_envelope`
  (`{error:{code,message,traceId}}`→ErrorResponse). Model generated là **DO-NOT-EDIT** (không `from_dict`) ⇒ parser
  sống ở đây; thêm DTO = thêm một hàm parse (không sửa file generated, không hand-declare DTO).
- **Đăng ký:** `[autoload]` trong `client/project.godot` (`NetworkClient` sau `EventBus`). Hai event mới thêm vào
  danh mục `EventBus.EVENTS` + `signal`: `network_error`, `unauthorized`.

**Quyết định user (2 nhánh):** (1) Base URL = **env var + hằng mặc định** (tối giản, hợp CI/test; phase 16
ConfigProvider có thể ghi đè `base_url`) — không dùng project setting / `.tres` (tránh chồng scope phase 16).
(2) JSON→DTO = **hàm parser tường minh** (một hàm/DTO đang dùng) — không dùng generic reflection mapper (tránh
"mini-framework" + map sai ngầm). Deviation nhỏ: `REQUEST_TIMEOUT_SECONDS` (const kế hoạch) đổi thành **`var
request_timeout_seconds`** (cấu hình được theo môi trường, mặc định giữ 10s) — cần cho verify server thật khi Redis
tắt (~11s/call).

Verified (Godot 4.7.1-stable, Windows, cục bộ): `--headless --import --path client` **exit 0**, 0 lỗi/0 warning,
autoload `NetworkClient` tạo OK. gdUnit4 headless (`runtest.cmd -a res://tests`) **21/21 pass, 0 error/0 failure/0
orphan** — `network_client_test.gd` (10: 200 parse ServerTimeResponse; 4xx map + `network_error`; 401 +`unauthorized`;
5xx; JSON không hợp lệ→INVALID_JSON; thiếu field→PARSE_ERROR; timeout; GET retry transient→success (call_count=2);
POST không retry (call_count=1); autoload hiện diện) + Phase-14 suites (11) giữ xanh. **Server thật** (API .NET cục
bộ `:5080`, `dotnet run`, Redis tắt→graceful): `NetworkClient` (GodotHttpTransport thật) GET `/api/v1/server-time`
→ parse `ServerTimeResponse.utc_now="2026-08-19T04:37:36.8977477+00:00"` (test tạm, đã xoá). Grep guard: `HTTPRequest`
chỉ ở `core/net/`; không log token/Authorization; không hardcode token. Không drift `client/src/data/generated`.

## Why

ADR-008: client giao tiếp server qua REST versioned `/api/v1` + JWT, contract single-source (codegen phase 08). Tập
trung mạng vào một autoload ⇒ bảo mật token (không rải rác/không log), xử lý lỗi/timeout nhất quán, tách UI khỏi
network (ADR-002 §6: UI không gọi network trực tiếp). Chuẩn hoá `ErrorResponse` → một cấu trúc lỗi + một kênh
`network_error` ⇒ mọi feature phản ứng đồng nhất. Retry chỉ GET ⇒ tránh double-effect POST (Forbidden: client tự
quyết kết quả — `docs/ai/coding-rules.md` §3; idempotency key POST = server phase 31). Nền cho phase 16
(ConfigProvider dùng network), 20 (auth/profile), mọi feature gọi API.

## Not this

- **`class_name NetworkClient` trên script autoload:** trùng singleton ⇒ Godot ẩn autoload. Bỏ `class_name`; truy
  cập qua global. Các lớp phụ trợ (không autoload) **có** `class_name` (`HttpTransport`/`NetResult`/…).
- **UI/feature tự `HTTPRequest`:** vi phạm ADR-002 (UI không gọi net) + phân tán token/lỗi. `HTTPRequest` **chỉ** ở
  `core/net/godot_http_transport.gd` (grep guard trong `# Cách kiểm tra`).
- **Generic reflection mapper (camelCase→snake_case tự động cho mọi DTO):** là "mini-framework" — xử lý nested
  Resource/enum/field lạ dễ map sai ngầm; phase cảnh báo không tự vẽ framework mạng. Chọn **hàm parser tường minh**.
- **Project setting `network/base_url` / `.tres` config:** chồng scope phase 16 (ConfigProvider). Chọn **env var +
  hằng mặc định** (tối giản, ghi đè được).
- **Retry POST / retry vô hạn / backoff framework:** double-effect + phức tạp thừa. Chỉ GET, bounded, không backoff
  (nợ nhẹ). Idempotency-Key POST = server phase 31.
- **Đăng nhập/refresh/lưu token thật, offline queue nâng cao, SignalR, config caching:** ngoài scope (phase 18/20,
  20/48, Post-MVP, 16). Phase 15 chỉ hạ tầng gắn token + seam.
- **Bịa kết quả khi mất mạng (fake success/reward):** cấm tuyệt đối (ADR-008/011). Mất mạng/timeout → `NetResult`
  lỗi + `network_error`, không giá trị giả.
- **Blanket `catch`/nuốt lỗi:** JSON không hợp lệ / parse lệch model → lỗi rõ (INVALID_JSON/PARSE_ERROR), không
  fake. `JSON.parse()` (không đẩy lỗi console) thay `JSON.parse_string` để log sạch.

Liên quan: ADR-008 (networking REST/JWT/server-time/không client-authority), ADR-002 (kiến trúc client, UI tách
network). Dùng lại [[0012-client-autoloads-standardized]] (EventBus/SceneRouter, convention autoload) +
[[0006-codegen-pipeline-standardized]] (model generated phase 08) + [[0011-api-layer-standardized]] (endpoint
`/api/v1/server-time`, `ErrorEnvelope`). Canonical: `docs/godot/state-and-signals.md` §4 + §3.1,
`docs/godot/ui-architecture.md` §1. Kế tiếp: Phase 16 (client ConfigProvider + StateCache).
