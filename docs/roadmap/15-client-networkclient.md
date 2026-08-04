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

- [ ] Tạo `core/net/network_client.gd`: wrapper `HTTPRequest`, GET/POST JSON, base URL cấu hình.
- [ ] Gắn header `Authorization: Bearer <jwt>` (token từ store; store nối phase 20).
- [ ] Deserialize JSON → model generated (phase 08); lỗi parse → lỗi rõ.
- [ ] Map `ErrorResponse` (code/message/traceId) → cấu trúc lỗi client; emit `network_error`; 401 → emit `unauthorized`.
- [ ] Timeout + retry chỉ cho request idempotent (GET); mất mạng → báo, **không** tự tạo kết quả.
- [ ] Static typing, tab indent; không để UI gọi trực tiếp (review guard).
- [ ] Test gdUnit4 với HTTP mock/stub: 200 parse đúng; 4xx→error event; timeout.
- [ ] Cập nhật [`../godot/state-and-signals.md`](../godot/state-and-signals.md) (event mạng) + [`../godot/ui-architecture.md`](../godot/ui-architecture.md) (UI không gọi net).

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

Đóng khi NetworkClient gọi API thật + parse model + xử lý lỗi/timeout + UI không gọi net trực tiếp, test gdUnit4 xanh.

---

## Liên kết
- [`../godot/ui-architecture.md`](../godot/ui-architecture.md) · [`../godot/state-and-signals.md`](../godot/state-and-signals.md) · [`../backend/api-and-versioning.md`](../backend/api-and-versioning.md)
- ADR: [`../adr/ADR-008-networking.md`](../adr/ADR-008-networking.md) · [`../adr/ADR-002-godot-architecture.md`](../adr/ADR-002-godot-architecture.md)
- Roadmap: [`README.md`](README.md) → kế: [`16-client-configprovider-statecache.md`](16-client-configprovider-statecache.md)
