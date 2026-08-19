# 15 — Client autoload: NetworkClient (HTTP+JWT) + models

> Mục đích: Dựng autoload **NetworkClient** gọi REST `/api/v1` (JWT), parse vào model generated (phase 08), xử lý lỗi/timeout — kênh giao tiếp server duy nhất của client.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 3 Client Core Framework | P1 | S3 | nền client |

# Mục tiêu

`NetworkClient` autoload: gọi HTTP (HTTPS/JSON), gắn JWT, deserialize vào model generated, chuẩn hoá lỗi (map `ErrorResponse`), timeout/retry cơ bản; UI/feature **không** gọi HTTP trực tiếp (chỉ qua đây).

# Lý do

ADR-008: client giao tiếp server qua REST versioned + JWT, contract single-source. Tập trung mạng vào một autoload để bảo mật token, xử lý lỗi/timeout nhất quán, và giữ UI tách khỏi network (ADR-002).

# Phụ thuộc

- **Trước:** 14 (EventBus), 08 (model generated), 05 (contract), 13 (API layer đích).
- **Sau:** 16 (ConfigProvider dùng network), 20 (auth/profile), mọi feature gọi API.

# Phạm vi

- `NetworkClient` autoload: GET/POST JSON, header JWT, base URL từ cấu hình client.
- Deserialize response → model generated; map `ErrorResponse` → lỗi client + emit event lỗi qua EventBus.
- Timeout, retry idempotent-safe (chỉ GET), báo mất mạng (không tự quyết kết quả — ADR-008).
- Quản lý token (lưu an toàn, refresh — nối phase 18/20).

# Không thuộc phạm vi

- Đăng nhập/lấy token thật (phase 18/20) — ở đây chỉ hạ tầng gắn token.
- Config bundle caching (phase 16).
- SignalR realtime (Post-MVP).

# Deliverables

- `network_client.gd` autoload + đăng ký.
- Parse model generated + xử lý `ErrorResponse` nhất quán.
- Sự kiện `network_error`/`unauthorized` qua EventBus.
- Test gdUnit4 (mock HTTP) cho parse thành công/lỗi/timeout.

# Công việc cần thực hiện

- [x] Tạo `core/net/network_client.gd`: wrapper `HTTPRequest` (qua seam `HttpTransport`/`GodotHttpTransport`), GET/POST JSON, base URL cấu hình (env `GAME_TEAM_API_BASE_URL`, mặc định `http://localhost:8080`). Autoload đăng ký trong `client/project.godot`. ✅ import exit 0.
- [x] Gắn header `Authorization: Bearer <jwt>` (token từ `TokenStore` — kho tối giản; đăng nhập/refresh thật nối phase 18/20). Chỉ gắn khi có token; **không log**. ✅
- [x] Deserialize JSON → model generated (phase 08) qua `NetworkResponseParser` (hàm parser tường minh); lỗi parse → `PARSE_ERROR`, JSON không hợp lệ → `INVALID_JSON` (không bịa thành công). ✅ test 200 parse `ServerTimeResponse`.
- [x] Map `ErrorResponse` (code/message/traceId) → `NetResult` chuẩn hoá; emit `network_error`; 401 → emit **thêm** `unauthorized`. ✅ test 4xx/401 (đã thêm 2 event vào danh mục `EventBus.EVENTS`).
- [x] Timeout (`request_timeout_seconds` mặc định 10s) + retry chỉ GET/idempotent (lỗi vận chuyển tạm thời, tối đa `MAX_GET_RETRIES=2`); POST không tự retry; mất mạng → báo, **không** tự tạo kết quả. ✅ test timeout/GET-retry/POST-no-retry.
- [x] Static typing, tab indent; `HTTPRequest` chỉ ở `core/net/` (grep guard xanh — UI/feature không gọi trực tiếp). ✅
- [x] Test gdUnit4 với HTTP stub (`FakeHttpTransport`): 200 parse đúng; 4xx/5xx/401→error event; JSON không hợp lệ; parse lỗi; timeout; GET retry; POST no-retry; autoload hiện diện. ✅ **21/21 pass, 0 orphan**.
- [x] Cập nhật [`../godot/state-and-signals.md`](../godot/state-and-signals.md) §3.1 (2 event mạng) + §4 (NetworkClient) + [`../godot/ui-architecture.md`](../godot/ui-architecture.md) §1 (UI không gọi net). ✅

# Tiêu chí hoàn thành

- Gọi `/api/v1/server-time` (phase 13) → parse model đúng.
- Lỗi 4xx/5xx → map `ErrorResponse`, emit event; 401 → `unauthorized`.
- Timeout/mất mạng → báo lỗi, không bịa kết quả.
- UI/feature không có lời gọi HTTP trực tiếp (chỉ qua NetworkClient).

# Cách kiểm tra

- Chạy server local (phase 13) → client gọi `/server-time` hiển thị kết quả.
- gdUnit4: mock 200/4xx/timeout → hành vi đúng.
- Grep client: không `HTTPRequest` ngoài `core/net`.

# Rủi ro

- **Token rò rỉ/log** → không log Authorization; lưu token an toàn (nối phase 20).
- **Retry gây double-effect** → chỉ retry GET; POST nhạy cảm dùng idempotency key (server phase 31).
- **Parse model lệch contract** → dựa model generated (phase 08) + drift check.

# Ghi chú

Client **không bao giờ** tự quyết kết quả/phần thưởng (ADR-008/011) — chỉ gửi intent, nhận kết quả. Token refresh/link account nối ở phase 18/20.

# Technical Debt Review

- **Maintainability:** mạng tập trung, đổi transport dễ.
- **Scalability:** nền cho mọi feature gọi API.
- **Testing:** mock HTTP cho test độc lập.
- **Security:** bảo vệ token, không log nhạy cảm, không client-authority.
- **Nợ:** refresh token, offline queue nâng cao (phase 20/48).

# Phase Review

**Đủ điều kiện đóng (2026-08-19, cục bộ).** `NetworkClient` là kênh giao tiếp server DUY NHẤT: autoload đăng ký,
`get_json`/`post_json` qua seam `HttpTransport`, base URL env, JWT qua `TokenStore`, parse model generated qua
`NetworkResponseParser`, chuẩn hoá lỗi `NetResult`, phát `network_error`/`unauthorized`, timeout + retry chỉ GET.

- **Tiêu chí hoàn thành:** (1) Server thật (.NET cục bộ `:5080`, Redis tắt→graceful) — `NetworkClient` (transport
  HTTPRequest thật) GET `/api/v1/server-time` → parse `ServerTimeResponse.utc_now` đúng (test tạm, đã xoá).
  (2) 4xx/5xx→map `ErrorResponse` + `network_error`; 401→`unauthorized`. (3) timeout/mất mạng→lỗi rõ, không bịa.
  (4) `HTTPRequest` chỉ ở `core/net/` (grep guard). Tất cả ✅.
- **Verify:** Godot 4.7.1-stable `--headless --import` exit 0 (0 warning); gdUnit4 `runtest.cmd -a res://tests`
  **21/21 pass, 0 error/failure/orphan** (`network_client_test` 10 + Phase-14 11). Không drift
  `client/src/data/generated`. Không log token/Authorization; không hardcode credential.
- **CI-verification pending:** `.github/workflows/ci-client.yml` (Godot headless import + gdUnit trên Actions) — cập
  nhật `[x]` khi có kết quả Actions xanh.
- **Nợ có chủ đích (không kéo vào Phase 15):** refresh/lưu token thật (18/20), offline queue nâng cao (20/48),
  `Idempotency-Key` POST (server 31), SignalR (Post-MVP), retry backoff.

Decision log: `.memory/0013-client-networkclient-standardized.md`. Kế tiếp: Phase 16 (ConfigProvider + StateCache).

---

## Liên kết
- [`../godot/ui-architecture.md`](../godot/ui-architecture.md) · [`../godot/state-and-signals.md`](../godot/state-and-signals.md) · [`../backend/api-and-versioning.md`](../backend/api-and-versioning.md)
- ADR: [`../adr/ADR-008-networking.md`](../adr/ADR-008-networking.md) · [`../adr/ADR-002-godot-architecture.md`](../adr/ADR-002-godot-architecture.md)
- Roadmap: [`README.md`](README.md) → kế: [`16-client-configprovider-statecache.md`](16-client-configprovider-statecache.md)
