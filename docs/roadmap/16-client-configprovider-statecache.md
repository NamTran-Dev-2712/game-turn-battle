# 16 — Client autoload: ConfigProvider + StateCache

> Mục đích: Dựng **ConfigProvider** (nhận & cache config bundle theo version) và **StateCache** (read-cache trạng thái người chơi cho hiển thị/offline-view) — client đọc dữ liệu qua đây, không tự tính chân lý.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 3 Client Core Framework | P1 | S3 | nền client |

# Mục tiêu

`ConfigProvider` autoload: tải config bundle versioned từ backend (qua NetworkClient), cache theo `config@vN`, cung cấp truy vấn config cho feature (id→data). `StateCache` autoload: giữ **read-cache** profile/trạng thái (không authoritative — ADR-007), refresh từ server.

# Lý do

ADR-005: client cache config theo version, không rebuild khi đổi config. ADR-007: client chỉ giữ cache đọc, chân lý ở server. Hai autoload này tách "đọc dữ liệu" khỏi feature, đảm bảo data-driven & không client-authority.

# Phụ thuộc

- **Trước:** 15 (NetworkClient), 14 (EventBus), 08 (model).
- **Sau:** 22 (config bundle e2e), 27+ (feature đọc config/state).

# Phạm vi

- `ConfigProvider`: tải bundle theo version, cache local (đĩa), API truy vấn `get_hero(id)`… (đọc theo schema, không hardcode).
- `StateCache`: lưu snapshot đọc (currency/hero/progress) để hiển thị; invalidation khi server trả cập nhật; đánh dấu "chỉ hiển thị".
- Emit event khi config version đổi / state refresh.
- Không ghi chân lý; mọi thay đổi state qua server (NetworkClient → command server).

# Không thuộc phạm vi

- Configuration Service phía server (phase 21).
- Luồng bundle e2e đầy đủ (phase 22).
- Logic nghiệp vụ tính toán chân lý (thuộc server).

# Deliverables

- `config_provider.gd`, `state_cache.gd` autoload + đăng ký.
- Cache bundle theo version (đĩa) + invalidation khi đổi version.
- Test gdUnit4: nạp bundle mẫu → query id trả data; đổi version → cache mới; state refresh.

# Công việc cần thực hiện

- [ ] `core/config/config_provider.gd`: nhận bundle (từ NetworkClient), lưu cache đĩa theo `config@vN`, load khi boot.
- [ ] API truy vấn config theo id/type (đọc dữ liệu theo schema phase 06, không nhúng số).
- [ ] So version: nếu server báo version mới → tải bundle mới, emit `config_updated`.
- [ ] `core/state/state_cache.gd`: giữ read-cache (currency/hero/progress); cập nhật khi server trả; cờ "display-only".
- [ ] Đảm bảo không có đường ghi chân lý ở client (review guard); mọi mutation gọi server.
- [ ] Test gdUnit4: bundle mẫu→query; version bump→reload; state set từ response→hiển thị.
- [ ] Cập nhật [`../godot/resources-and-assets.md`](../godot/resources-and-assets.md) + [`../godot/state-and-signals.md`](../godot/state-and-signals.md).

# Tiêu chí hoàn thành

- Client nhận bundle version X, query id trả đúng data; đổi sang X+1 không cần rebuild client.
- StateCache chỉ đọc; không có code client tự cộng currency/kết quả.
- Cache đĩa hoạt động (offline-view hiển thị dữ liệu cũ có nhãn).
- Test gdUnit4 xanh.

# Cách kiểm tra

- Với bundle mẫu 2 version → client chuyển version, dữ liệu hiển thị đổi mà không build lại.
- gdUnit4: query config, reload version, state refresh.
- Rà: không có phép tính chân lý (reward/currency) ở client.

# Rủi ro

- **Client tự tính chân lý (drift/cheat)** → chặn bằng review + đặt mọi mutation ở server; StateCache read-only.
- **Cache version cũ hiển thị sai** → gắn nhãn "offline/cached", ưu tiên server khi online.
- **Bundle lớn tải chậm** → cache đĩa + tải nền (ADR-009), tải phần nhẹ trước.

# Ghi chú

`config@vN` là **immutable** ⇒ cache dài an toàn. StateCache phục vụ hiển thị/offline-view; chân lý luôn ở server (ADR-007). Bám ADR-005/007.

# Technical Debt Review

- **Maintainability:** đọc dữ liệu tách khỏi feature; đổi config không đụng code.
- **Scalability:** cache version giảm tải mạng; hỗ trợ nội dung lớn dần.
- **Testing:** provider/cache có test độc lập.
- **Security:** không client-authority; cache đọc không chứa bí mật.
- **Nợ:** bundle e2e & signed bundle (phase 22/LiveOps).

# Phase Review

Đóng khi ConfigProvider cache theo version + StateCache read-only chạy, đổi version không rebuild, test xanh, không client-authority.

---

## Liên kết
- [`../godot/resources-and-assets.md`](../godot/resources-and-assets.md) · [`../godot/state-and-signals.md`](../godot/state-and-signals.md) · [`../gameplay/configuration-and-data.md`](../gameplay/configuration-and-data.md)
- ADR: [`../adr/ADR-005-configuration-strategy.md`](../adr/ADR-005-configuration-strategy.md) · [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md) · [`../adr/ADR-009-asset-loading.md`](../adr/ADR-009-asset-loading.md)
- Roadmap: [`README.md`](README.md) → kế: [`17-client-boot-ui-base.md`](17-client-boot-ui-base.md)
