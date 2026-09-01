# 22 — Client config bundle end-to-end

> Mục đích: Hoàn tất luồng **config data-driven end-to-end**: client (ConfigProvider) nhận bundle versioned từ Configuration Service, cache theo version, dùng để dựng dữ liệu hiển thị — chứng minh đổi config không rebuild client.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 4 Auth, Save & Config Service | P1 | S5 | nền data-driven |

# Mục tiêu

Client boot: hỏi version hiện hành → nếu khác cache → tải bundle từ `GET /config/bundle` → ConfigProvider (phase 16) cache & phục vụ query cho feature; hiển thị một dữ liệu từ config (ví dụ danh sách hero mẫu) để xác nhận vòng.

# Lý do

Đóng mắt xích cuối Core Framework từ phía client: xác nhận contract config + Configuration Service + ConfigProvider client hoạt động cùng nhau, sẵn sàng cho combat/hero (nhóm 5–6) đọc config thật.

# Phụ thuộc

- **Trước:** 21 (Config Service), 16 (ConfigProvider client), 20 (auth để gọi API).
- **Sau:** 27 (hero từ config), 23–25 (combat đọc hero/skill config), mọi feature.

# Phạm vi

- Client so version (current vs cache) → tải bundle khi cần → cache đĩa theo `config@vN`.
- Query config qua ConfigProvider để dựng dữ liệu hiển thị mẫu (hero list placeholder).
- Xử lý bundle lỗi/thiếu → fallback cache cũ + báo lỗi.
- Chứng minh: đổi giá trị config phía server → client thấy đổi mà không build lại.

# Không thuộc phạm vi

- Feature nghiệp vụ hoàn chỉnh (hero system thật phase 27).
- Combat (nhóm 5).
- Signed/secure bundle nâng cao (LiveOps/Post-MVP).

# Deliverables

- Luồng config e2e chạy; client hiển thị dữ liệu từ bundle.
- Cache đĩa theo version + fallback.
- Test gdUnit4 (mock server): nhận bundle→query→hiển thị; version bump→reload; lỗi→fallback.
- Ghi chú "đổi config không rebuild" trong [`../gameplay/configuration-and-data.md`](../gameplay/configuration-and-data.md).

# Công việc cần thực hiện

- [x] Boot: gọi `GET /config/current` → so với cache; khác → `GET /config/bundle?bundleVersion=`.
  *(param thật = `bundleVersion`, KHÔNG `version` — trùng token `{version:apiVersion}` server. `config_provider.gd`
  `check_for_update()`; boot bắt status + log stale. Test `test_check_for_update_calls_real_config_endpoints`.)*
- [x] ConfigProvider (phase 16) lưu bundle đĩa theo version; load khi boot.
  *(Đã có Phase 16 — cache `config@vN` ghi-một-lần + boot reload. Phase 22 sửa `_build_index` nhận hình MAP server thật
  `data.{type}.{id}=entry` + mảng cũ. Test `test_apply_map_shaped_bundle_indexes_by_id`, `..._indexes_map_shaped_server_bundle`.)*
- [x] Dựng màn mẫu đọc từ config (danh sách hero placeholder từ `hero.schema`) để xác nhận query.
  *(`src/ui/hero_list/` — `HeroListView`(BaseView, network-free) + `HeroListPresenter` đọc `ConfigProvider.get_all(&"hero")`,
  điều hướng từ nút "Anh hùng" của hub. Test `test_receive_query_display_from_config`.)*
- [x] Fallback: bundle tải lỗi → dùng cache cũ + báo; không có cache → màn lỗi + retry.
  *(KHÔNG im lặng — `is_stale()`/`last_error_code()` + `push_warning` + banner stale + nút Thử lại; no-cache → empty + Retry.
  Test `test_..._bundle_download_fails_keeps_old_cache_and_marks_stale`, `test_bundle_failure_falls_back...`,
  `test_no_cache_shows_empty_then_retry_recovers_via_network`.)*
- [x] Kịch bản chứng minh: đổi giá trị config server → publish → client reload version mới không build lại.
  *(Chứng minh tự động: `test_version_bump_reflects_new_data_without_rebuild` (v1 rarity 3 → v2 rarity 5, cùng binary).
  Seed `config/heroes/hero_sample.json`+`config/skills/skill_sample_basic.json` (số 0, validator exit 0) cho demo server thật.
  Quy trình demo Docker ghi ở `../gameplay/configuration-and-data.md` §4.3.)*
- [x] Test gdUnit4 mock: nhận→query→hiển thị; version bump; lỗi→fallback.
  *(gdUnit4 headless **76/76 pass, 0 orphan**; mock ở boundary `FakeHttpTransport` — ConfigProvider/NetworkClient THẬT.)*
- [x] Cập nhật `../gameplay/configuration-and-data.md` (ghi rõ luồng e2e + chứng minh).
  *(§4.1 luồng e2e + hình MAP + endpoint/param thật; §4.2 fallback không im lặng; §4.3 chứng minh không rebuild.)*

# Tiêu chí hoàn thành

- Client nhận bundle version X, hiển thị dữ liệu từ config.
- Server đổi config → version X+1 → client hiển thị đổi **không rebuild** (chứng minh, có ảnh/log).
- Bundle lỗi → fallback cache cũ hoặc màn lỗi + retry.
- Test gdUnit4 xanh.

# Cách kiểm tra

- Chạy server local: đổi giá trị config → publish → mở lại client → thấy đổi.
- gdUnit4 mock: nhận/version-bump/lỗi.
- Rà: dữ liệu hiển thị lấy từ ConfigProvider, không hardcode trong scene.

# Rủi ro

- **Tải bundle lớn chậm** → tải nền (ADR-009), phần nhẹ trước; progress splash.
- **Version lệch client-server** → so version bắt buộc trước khi dùng; immutable per version.
- **Fallback che lỗi thật** → luôn log + báo khi dùng cache cũ.

# Ghi chú

Đây là chứng minh "data-driven & LiveOps-ready" từ đầu-đến-cuối. Sau phase này, mọi feature đọc số liệu từ config bundle, không hardcode. Bám ADR-004/005/009.

# Technical Debt Review

- **Maintainability:** feature đọc config thống nhất; đổi số không đụng client build.
- **Scalability:** cache version + tải nền cho nội dung lớn.
- **Testing:** e2e mock cover luồng chính.
- **Security:** bundle validate ở server; client chỉ đọc.
- **Nợ:** signed bundle & live swap (LiveOps/Post-MVP).

# Phase Review

Đóng khi luồng config e2e chạy, chứng minh đổi config không rebuild client, fallback hoạt động, test xanh. **Hoàn tất P1 — nền data-driven end-to-end sẵn sàng cho gameplay.**

**Kết quả (2026-08-28, Godot 4.7.1-stable Windows — local PASS):**
- **Vấn đề tích hợp đã sửa (công việc chính, KHÔNG greenfield):** (1) client `_build_index` chỉ nhận mảng nhưng server phát
  `data` **map theo id** ⇒ sửa nhận cả hai (index theo `entry.id`); (2) wire sai endpoint (`/config/version`+`?version=`) ⇒
  đổi sang thật `/config/current`+`?bundleVersion=`; (3) fallback im lặng ⇒ status dict + `is_stale()` + `push_warning` + banner/retry.
- **Reuse, KHÔNG reinvent:** mở rộng `ConfigProvider` (Phase 16) + `NetworkClient`/`BaseView`/`SceneRouter`; **không** tạo
  config provider/HTTP client/DTO thứ hai. **Không thêm event EventBus** (danh mục ĐÓNG — tái dùng `config_updated`).
- **Verify:** config-validator exit 0 (2 file, hero→skill OK); `--headless --import` exit 0 (0/0); gdUnit4 **76/76 pass,
  0 error/0 failure/0 orphan** (config_provider +7 test mới; hero_list_presenter 5 test); grep guard sạch (view network-free);
  không drift `client/src/data/generated`/`openapi.json`.
- **Scope discipline:** màn hero là **mẫu đọc config** (KHÔNG Hero System — phase 27). Nợ Post-MVP: signed/secure bundle,
  cryptographic verify, advanced LiveOps, live swap.
- **Demo server thật (Docker):** quy trình ở `../gameplay/configuration-and-data.md` §4.3; nếu môi trường không có Docker,
  gdUnit4 mock (`test_version_bump_reflects_new_data_without_rebuild`) là bằng chứng "đổi config không rebuild" tự động chuẩn.
- Doc-sync: `../gameplay/configuration-and-data.md` §4.1–4.3 · `../godot/resources-and-assets.md` §1.1 · `CLAUDE.md` §4.6 ·
  `.instructions/client.md` · `.claude/agents/godot-client.md` · `.memory/0020-client-config-bundle-e2e-standardized.md`.

---

## Liên kết
- [`../gameplay/configuration-and-data.md`](../gameplay/configuration-and-data.md) · [`../godot/resources-and-assets.md`](../godot/resources-and-assets.md)
- ADR: [`../adr/ADR-005-configuration-strategy.md`](../adr/ADR-005-configuration-strategy.md) · [`../adr/ADR-004-data-driven-design.md`](../adr/ADR-004-data-driven-design.md) · [`../adr/ADR-009-asset-loading.md`](../adr/ADR-009-asset-loading.md)
- Roadmap: [`README.md`](README.md) → kế: [`23-combat-spec-fixedpoint.md`](23-combat-spec-fixedpoint.md)
