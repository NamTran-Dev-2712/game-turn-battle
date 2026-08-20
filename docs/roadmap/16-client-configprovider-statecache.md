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

- [x] `core/config/config_provider.gd`: nhận bundle (từ NetworkClient), lưu cache đĩa theo `config@vN`, load khi boot. — autoload `ConfigProvider` (bỏ `class_name`), `apply_bundle` cache `user://config_cache/config@vN.json` **ghi-một-lần** + `active.json`, `_ready` nạp lại từ đĩa (offline-view; thiếu/hỏng → rỗng).
- [x] API truy vấn config theo id/type (đọc dữ liệu theo schema phase 06, không nhúng số). — `get_entry(type,id)`/`get_all(type)`/`get_hero(id)`/`current_version()`/`config_label()`/`has_config()`, trả **bản sao**, không hardcode số (test đọc `rarity`/`base_stats` từ bundle).
- [x] So version: nếu server báo version mới → tải bundle mới, emit `config_updated`. — `check_for_update()` qua `NetworkClient.get_json` (parser `parse_config_bundle`) → `apply_bundle`; `config_updated` `{version,config_version}` phát khi active version đổi (test: FakeHttpTransport v2).
- [x] `core/state/state_cache.gd`: giữ read-cache (currency/hero/progress); cập nhật khi server trả; cờ "display-only". — autoload `StateCache` (`IS_DISPLAY_ONLY=true`), `apply_snapshot` từ server response, đọc trả bản sao, `source()`/`is_offline()`, persist offline-view + phát `state_refreshed`.
- [x] Đảm bảo không có đường ghi chân lý ở client (review guard); mọi mutation gọi server. — StateCache **không** có mutator chân lý (test `has_method` false cho add/spend currency…); grep `client/src` sạch (không `currency +=`/reward/inventory); `HTTPRequest` chỉ ở `core/net/`; mutation qua `Feature/UI → NetworkClient → Server → response → StateCache`.
- [x] Test gdUnit4: bundle mẫu→query; version bump→reload; state set từ response→hiển thị. — `tests/core/config/config_provider_test.gd` (11) + `tests/core/state/state_cache_test.gd` (6); toàn bộ suite **38/38 pass, 0 error/0 failure/0 orphan** (Godot 4.7.1 local).
- [x] Cập nhật [`../godot/resources-and-assets.md`](../godot/resources-and-assets.md) + [`../godot/state-and-signals.md`](../godot/state-and-signals.md). — `resources-and-assets.md` §1.1 (ConfigProvider), `state-and-signals.md` §1.1 (StateCache) + §3.1 (event `config_updated`/`state_refreshed`) + §4 note; `configuration-and-data.md` §4; Vibe Code: CLAUDE.md §4.6, `.instructions/client.md`, `.claude/agents/godot-client.md`, `.claude/workflows/documentation-sync.md`, `.memory/0014`.

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

**Kết quả (2026-08-20 — local PASS, đủ điều kiện đóng):** Hai autoload `ConfigProvider` + `StateCache` ở
`client/src/core/{config,state}/` (đăng ký sau `NetworkClient`, bỏ `class_name`). ConfigProvider: `apply_bundle` cache
envelope `config@vN` **BẤT BIẾN** (ghi-một-lần, không ghi đè version cũ — ADR-005) + con trỏ `active.json`, boot nạp lại
(offline-view), truy vấn data-driven `get_entry`/`get_hero`/`current_version` (bản sao, không hardcode số), `check_for_update`
qua NetworkClient phát `config_updated` khi đổi version (endpoint `/api/v1/config/...` placeholder — Config Service phase 21,
e2e phase 22). StateCache: read-cache **CHỈ HIỂN THỊ** (`IS_DISPLAY_ONLY`), chỉ `apply_snapshot` (server response) ghi —
**không** mutator chân lý, đọc trả bản sao, `source()`/`is_offline()` nhãn cache-vs-server, persist offline-view, phát
`state_refreshed`. Verify (Godot 4.7.1-stable, Windows, local): `--headless --import` exit 0 (0 warning, autoload tạo OK);
gdUnit4 **toàn bộ 38/38 pass, 0 error/0 failure/0 orphan** (config 11 + state 6 + Phase-14/15 regression 21). Client-authority
audit **PASS** (grep `client/src` sạch; `HTTPRequest` chỉ `core/net/`; StateCache không expose mutator chân lý; không EventBus
thứ hai). Không drift `client/src/data/generated`. Docs + Vibe Code đồng bộ (§ Deliverables). **Tiêu chí hoàn thành: đạt đủ.**
CI-pending: `ci-client.yml` trên Actions (import + gdUnit4 headless dưới xvfb).

---

## Liên kết
- [`../godot/resources-and-assets.md`](../godot/resources-and-assets.md) · [`../godot/state-and-signals.md`](../godot/state-and-signals.md) · [`../gameplay/configuration-and-data.md`](../gameplay/configuration-and-data.md)
- ADR: [`../adr/ADR-005-configuration-strategy.md`](../adr/ADR-005-configuration-strategy.md) · [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md) · [`../adr/ADR-009-asset-loading.md`](../adr/ADR-009-asset-loading.md)
- Roadmap: [`README.md`](README.md) → kế: [`17-client-boot-ui-base.md`](17-client-boot-ui-base.md)
