# 0014 — Client ConfigProvider + StateCache standardized (Phase 16)

- Date: 2026-08-20
- Scope: workspace
- Status: Active

## Decision

Hai autoload lõi ở `client/src/core/` tách "đọc dữ liệu" khỏi feature, độc lập, đơn trách nhiệm (KHÔNG God
autoload). Cả hai **bỏ `class_name`** (trùng tên singleton) — truy cập qua global. Đây là **hợp đồng đọc
config/state** của client: `Server → NetworkClient → ConfigProvider/StateCache → Feature/UI`, và mọi thay đổi
chân lý: `Feature/UI → NetworkClient → Server → response → StateCache.apply_snapshot`. Client CHỈ đọc/cache —
chân lý luôn ở server (ADR-005/007/011).

- **`config/config_provider.gd`** (autoload `ConfigProvider`) — **cửa đọc config DUY NHẤT**:
  - `apply_bundle(bundle: Dictionary) -> bool`: nhận envelope `config-bundle.schema.json` (`config_version`
    "config@vN" + `schema_version` + `data` per-type), dựng chỉ mục `{type:{id:entry}}`, cache đĩa **BẤT BIẾN**
    `user://config_cache/config@vN.json` **ghi-một-lần** (không ghi đè version cũ) + con trỏ `active.json`, kích
    hoạt, phát **`config_updated`** `{version, config_version}` **chỉ khi version đổi**. Áp lại đúng version đang
    dùng = **no-op** (config@vN immutable). Bundle sai (thiếu/không đúng dạng `config_version`) → `push_warning` +
    `false`, không crash.
  - Truy vấn (data-driven, trả **BẢN SAO sâu**): `get_entry(type,id)`, `get_all(type)`, `has_entry(type,id)`,
    `get_hero(id)`, `current_version()`, `config_label()`, `has_config()`. Đọc theo schema phase 06, **KHÔNG nhúng
    số gameplay**.
  - Boot `_ready()`: nạp con trỏ + bundle version từ đĩa (offline-view); thiếu/hỏng → trạng thái rỗng + warn.
  - `check_for_update()` (coroutine): hỏi server version qua `NetworkClient.get_json` (parser
    `NetworkResponseParser.parse_config_bundle`), version mới hơn → tải bundle → `apply_bundle`. Endpoint
    `/api/v1/config/version|bundle` là **placeholder wire** (Config Service = phase 21, e2e = phase 22). Mất mạng →
    giữ cache, không bịa.
- **`state/state_cache.gd`** (autoload `StateCache`) — **read-cache CHỈ HIỂN THỊ** (`const IS_DISPLAY_ONLY = true`):
  - Đường ghi **DUY NHẤT** `apply_snapshot(snapshot: Dictionary)`: thay **toàn bộ** cache bằng snapshot từ **server
    response** (`profile`/`currencies`/`heroes`/`progress`), nguồn = `"server"`, lưu đĩa
    `user://state_cache/snapshot.json`, phát **`state_refreshed`** `{source}`. **KHÔNG** có mutator chân lý (không
    `add_currency`/`spend_currency`/`set_progress`/…).
  - Đọc (trả **BẢN SAO**): `get_currency(code)`, `get_currencies()`, `get_heroes()`, `get_hero(id)`,
    `get_progress(key)`, `get_all_progress()`, `get_profile()`, `source()` (`empty｜server｜cache`), `is_offline()`,
    `is_display_only()`.
  - Boot nạp snapshot đĩa với nguồn = `"cache"` (offline/cũ, UI gắn nhãn "offline") tới khi server refresh lật về
    `"server"` (ưu tiên server khi online). Chỉ dữ liệu hiển thị — không bí mật.
- **Đăng ký:** `[autoload]` trong `client/project.godot` (`ConfigProvider` + `StateCache` **sau** `NetworkClient`).
  Hai event mới vào `EventBus.EVENTS` + `signal`: `config_updated`, `state_refreshed` (danh mục §3.1, quy trình 4
  bước). Thêm `parse_config_bundle(data)→ConfigBundleDto` vào `response_parser.gd` (không sửa file generated, không
  đổi contract ⇒ không drift codegen).

**Quyết định thiết kế (giải theo ADR + phase spec, không hỏi thừa):** (1) bundle client-side = envelope phase 06 +
mục `data` per-type bổ sung (schema envelope `additionalProperties: true` cho phép ⇒ **không** tạo envelope thứ
hai). (2) Cache đĩa **ghi-một-lần** để đảm bảo `config@vN` bất biến (ADR-005). (3) StateCache **persist snapshot +
nhãn `cache`** để thoả tiêu chí "offline-view hiển thị dữ liệu cũ có nhãn" (ADR-007 "read-cache cho display/
offline-view"). (4) Truy vấn config trả **Dictionary** (chưa có HeroData DTO — data-driven) thay vì typed. (5) Đọc
trả **bản sao** để chặn mutation cache (guard read-only bằng kiến trúc + test).

Verified (Godot 4.7.1-stable, Windows, cục bộ): `--headless --import --path client` **exit 0**, 0 lỗi/0 warning,
autoload `ConfigProvider`/`StateCache` tạo OK. gdUnit4 headless (`runtest.cmd -a res://tests`) **toàn bộ 38/38 pass,
0 error/0 failure/0 orphan** — `config/config_provider_test.gd` (11: apply v1→query id; đọc trả bản sao; version
bump v1→v2 active đổi + `config_updated` payload version=2 + query v2 KHÔNG rebuild; áp lại cùng version = no-op;
cache đĩa persist→instance mới boot nạp lại; version cũ file bất biến không ghi đè; thiếu cache→rỗng; cache hỏng→
không crash; bundle sai→từ chối; `check_for_update` qua NetworkClient+FakeHttpTransport tải v2; autoload hiện diện)
+ `state/state_cache_test.gd` (6: apply_snapshot→đọc currency/hero/progress + `state_refreshed` + source=server; đọc
trả bản sao; KHÔNG có mutator chân lý (`has_method` false cho add_currency/spend_currency/set_currency/add_hero/
set_progress/grant_reward/apply_reward/mutate); cache đĩa→instance mới boot source=cache→refresh lật server; rỗng
khi chưa cache; autoload hiện diện) + Phase-14/15 regression (21) giữ xanh. Rà authority: không `currency +=`/reward/
inventory/progress mutation ở `client/src` (chỉ prose + README placeholder); `HTTPRequest` chỉ ở `core/net/`; không
EventBus thứ hai. Không drift `client/src/data/generated`.

## Why

ADR-005: client cache config theo version bất biến (`config@vN`), chỉ tải khi có version mới, **không rebuild** khi
đổi config → tách "đọc config" vào `ConfigProvider` (data-driven, ADR-004: code phụ thuộc schema không phụ thuộc
giá trị). ADR-007: client giữ **read-cache** cho display/offline-view, chân lý ở server; mọi thay đổi state qua
command server → `StateCache` read-only, đường ghi duy nhất = snapshot server. ADR-009: tách dữ liệu nhẹ (config)
khỏi asset nặng, cache đĩa + nạp nền. Hai autoload này là nền cho phase 22 (bundle e2e), 27+ (feature đọc config/
state), 18/20 (auth/profile refresh StateCache).

## Not this

- **`class_name` trên script autoload:** trùng singleton ⇒ Godot ẩn autoload. Bỏ `class_name`; truy cập qua global.
- **Feature tự tải/nạp raw config bundle:** phá data-driven + phân tán. `ConfigProvider` là cửa đọc DUY NHẤT.
- **Ghi đè `config@vN` khi đổi version:** phá bất biến (ADR-005). Cache đĩa **ghi-một-lần**; version mới = file mới.
- **Hardcode số gameplay vào provider/feature:** cấm (ADR-004). Số đọc từ bundle theo schema.
- **StateCache như database/source of truth; mutator chân lý (add/spend currency, set progress, grant reward):** cấm
  (ADR-007/011). Chỉ `apply_snapshot` từ server ghi; không setter tuỳ ý. Client **không** tự tính currency/reward/
  kết quả battle/progress/inventory/stats.
- **Trả tham chiếu cache trực tiếp:** cho phép caller sửa cache ngầm. Đọc trả **bản sao sâu**.
- **Envelope config thứ hai:** dùng lại `config-bundle.schema.json` (thêm `data` là additive hợp lệ), không tự vẽ.
- **EventBus thứ hai / event chui:** dùng lại một EventBus; `config_updated`/`state_refreshed` theo danh mục §3.1.
- **Kéo scope phase 22 (bundle e2e, signed bundle, LiveOps) / Config Service server (phase 21):** ngoài phạm vi.
  Phase 16 chỉ autoload client + cache đọc; endpoint config là placeholder wire.

Liên quan: ADR-005 (configuration strategy, `config@vN` immutable, client cache theo version), ADR-007 (save
strategy, client read-cache không authority), ADR-009 (asset/config loading nhẹ trước + cache). Dùng lại
[[0013-client-networkclient-standardized]] (NetworkClient `get_json`/parser/`NetResult`) +
[[0012-client-autoloads-standardized]] (EventBus danh mục + convention autoload) +
[[0006-codegen-pipeline-standardized]] (model generated `ConfigBundleDto`/`ConfigVersion`) +
[[0004-config-schema-standardized]] (envelope `config-bundle.schema.json`). Canonical:
`docs/godot/resources-and-assets.md` §1.1 + `docs/godot/state-and-signals.md` §1.1/§3.1 +
`docs/gameplay/configuration-and-data.md` §4. Kế tiếp: Phase 17 (client boot + UI base).
