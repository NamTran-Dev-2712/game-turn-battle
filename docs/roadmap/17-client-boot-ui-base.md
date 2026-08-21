# 17 — Client boot flow + main scene + UI architecture base

> Mục đích: Dựng luồng khởi động client (boot → health check → nhận config → main scene) và **nền kiến trúc UI** (view tách logic, presenter/view-model) theo ADR-002.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 3 Client Core Framework | P1 | S3 | nền client |

# Mục tiêu

Scene khởi động: boot splash → gọi `/health` + nhận config bundle (ConfigProvider) → điều hướng (SceneRouter) tới main hub tối giản. Nền UI: base view/presenter, UI **không** gọi network trực tiếp, chỉ qua feature/EventBus.

# Lý do

Đóng nhóm 3 bằng một lát cắt "boot chạy được": chứng minh 4 autoload phối hợp (EventBus/NetworkClient/ConfigProvider/StateCache) và thiết lập khuôn UI để feature sau (nhóm 6+) gắn view nhất quán.

# Phụ thuộc

- **Trước:** 14, 15, 16.
- **Sau:** 20 (auth vào boot), 22 (config e2e), mọi feature UI.

# Phạm vi

- Boot scene: splash → health check → nhận config → route tới main hub.
- Main hub scene tối giản (placeholder các nút feature, chưa nghiệp vụ).
- UI base: lớp `BaseView`/presenter (view-model), quy ước UI chỉ nhận dữ liệu + phát intent, không gọi net.
- Xử lý lỗi boot (mất mạng/health fail) → màn báo lỗi + retry.

# Không thuộc phạm vi

- Đăng nhập thật (phase 20) — boot dùng health + config trước.
- Feature nghiệp vụ (hero/battle…).
- Art/animation hoàn chỉnh (ADR-009 tối ưu ở phase 52).

# Deliverables

- Boot scene + main hub scene tối giản chạy được.
- UI base (BaseView/presenter) + quy ước tài liệu hoá.
- Màn lỗi boot + retry.
- Test gdUnit4: boot happy-path (mock health+config) → tới hub; boot fail → màn lỗi.

# Công việc cần thực hiện

- [x] Tạo boot scene: splash → `NetworkClient.get(/health)` → `ConfigProvider` nhận/nạp bundle → `SceneRouter.goto(main_hub)`. → `src/ui/app_root.tscn` (main scene) route qua SceneRouter tới `src/ui/boot/boot.tscn` (`BootController` = presenter): `/health` (cổng bắt buộc) → `ConfigProvider.check_for_update()` (best-effort, Config Service = phase 21) → `SceneRouter.goto(main_hub)` + `clear_history`.
- [x] Tạo main hub scene tối giản (nút placeholder cho feature, dùng SceneRouter). → `src/ui/main_hub/` (`MainHubView` + `MainHubPresenter`), 4 nút placeholder phát intent; điều hướng qua SceneRouter (boot→hub đã chạy).
- [x] UI base: `BaseView` + presenter/view-model; quy ước dữ liệu vào, intent ra (EventBus), **không** gọi NetworkClient trong view. → `src/ui/base/base_view.gd`: `set_data`→`_render` (vào), `emit_intent`→signal `intent` (ra). Presenter (BootController/MainHubPresenter) dịch intent → SceneRouter/feature; **không thêm event EventBus mới** (danh mục đóng — §3.1). View network-free (grep guard xanh).
- [x] Màn lỗi boot (health fail/mất mạng) + nút retry. → `src/ui/boot/boot_error_view.gd` (thông báo AN TOÀN, không lộ nội bộ) + nút "Thử lại" phát intent `retry` (nối MỘT LẦN); `BootController.retry()` chạy lại flow sạch (khoá `_running` chống tái nhập/trùng listener).
- [x] Static typing, node PascalCase theo vai trò, tab indent. → toàn bộ script typed, tab indent; node `AppRoot`/`Boot`/`MainHub` PascalCase; Godot import 0 error/0 warning (script Phase 17 không phát warning).
- [x] Test gdUnit4: happy-path boot→hub; fail→error screen; retry. → `tests/ui/boot/boot_controller_test.gd` (happy→hub, config best-effort→hub, health fail→error + không điều hướng, retry→hub 1 lần) + `tests/ui/base/base_view_test.gd` + parse_health (net suite). **Toàn bộ 48/48 pass, 0 orphan** (Godot 4.7.1 local).
- [x] Cập nhật [`../godot/ui-architecture.md`](../godot/ui-architecture.md) + [`../godot/scene-architecture.md`](../godot/scene-architecture.md). → thêm hợp đồng BaseView/presenter (ui) + §4.2 boot/app-shell (scene) + note §5 autoload (AudioManager hoãn). Đồng bộ `state-and-signals.md §4` (parse_health), `resources-and-assets.md`, `setup-and-run.md`, `.instructions/client.md`, agent godot-client, `.memory/0015`.

# Tiêu chí hoàn thành

- Chạy client → boot gọi health + nhận config → vào main hub.
- Mất mạng/health fail → màn lỗi + retry hoạt động.
- UI base tách logic: không view nào gọi NetworkClient trực tiếp.
- Test gdUnit4 xanh headless; Godot import sạch.

# Cách kiểm tra

- Chạy server local → mở client → thấy boot → hub.
- Tắt server → client hiện màn lỗi boot + retry.
- gdUnit4: happy-path & fail-path.
- Grep: view không import `core/net`.

# Rủi ro

- **Boot chặn UI khi mạng chậm** → tải nền, splash không block; timeout → màn lỗi.
- **UI dính logic/network** → review guard + BaseView chuẩn.
- **Scene rò tài nguyên** → giải phóng khi chuyển (ADR-009, hoàn thiện phase 52).

# Ghi chú

Auth sẽ chèn vào boot ở phase 20 (guest login trước khi vào hub). Đây là "hello-world chơi được": boot + kết nối + config. Bám [`../godot/ui-architecture.md`](../godot/ui-architecture.md) + ADR-002.

# Technical Debt Review

- **Maintainability:** UI base chuẩn cho mọi feature; boot rõ ràng.
- **Scalability:** hub + router cho phép thêm feature dễ.
- **Testing:** boot path có test; nền test UI.
- **Security:** không client-authority; lỗi boot không lộ chi tiết.
- **Nợ:** auth vào boot (20); tối ưu asset/anim (52).

# Phase Review

Đóng khi boot→config→hub chạy, màn lỗi+retry hoạt động, UI base tách logic, test gdUnit4 xanh. **Kết thúc nhóm 3 — client core sẵn sàng.**

### Kết quả (2026-08-20 — local PASS, đủ điều kiện đóng)

- **Boot flow (app-shell):** `run/main_scene = src/ui/app_root.tscn` (Control rỗng) → `_ready` route qua `SceneRouter.goto(boot)` ⇒ SceneRouter làm CHỦ SỞ HỮU mọi screen từ frame đầu (boot→hub, tráo/`queue_free` gọn, không chồng lớp). `BootController` (presenter, root của `boot.tscn`): `NetworkClient.get_json("/health", parse_health)` = **cổng kết nối bắt buộc** (mất mạng/non-2xx → màn lỗi); `ConfigProvider.check_for_update()` = **best-effort** (Config Service thật = phase 21; endpoint vắng ⇒ giữ cache, KHÔNG chặn boot); rồi `SceneRouter.goto(main_hub)` + `clear_history`. Emit `boot_succeeded`/`boot_failed`.
- **UI base (data-in → view → intent-out):** `BaseView` (`set_data`→`_render`; `emit_intent`→signal `intent`; hook `bind`/`unbind` theo vòng đời cây). View THUẦN hiển thị, **network-free**; presenter (BootController/MainHubPresenter) dịch intent → SceneRouter/feature. **User chọn:** intent = signal cục bộ + presenter (KHÔNG thêm event EventBus mỗi nút — tôn trọng danh mục đóng §3.1); app-shell AppRoot (KHÔNG để boot làm main scene tự-free). **Không thêm event EventBus mới.**
- **Màn lỗi + retry:** `BootErrorView` thông báo AN TOÀN (không lộ stack) + nút retry (nối MỘT LẦN → không trùng listener); `retry()` chạy lại flow sạch, khoá `_running` chống tái nhập/điều hướng trùng.
- **Main hub:** `MainHubView` + `MainHubPresenter` (đọc `ConfigProvider`/`StateCache` hiển thị nhãn config/offline — KHÔNG network); 4 nút placeholder phát intent (feature thật = phase sau).
- **Net:** thêm `NetworkResponseParser.parse_health` (tái dùng model generated `HealthResponse`; thiếu `status` ⇒ null). Không đổi contract ⇒ không drift `client/src/data/generated`.
- **Verify (Godot 4.7.1-stable local):** `--headless --import` exit 0 (0 error/0 warning; đăng ký `BaseView`/`BootController`/`BootErrorView`/`BootView`/`MainHubView`/`MainHubPresenter`); gdUnit4 **toàn bộ 48/48 pass, 0 error/0 failure/0 orphan** (ui/base 3 + ui/boot 5 + net +2 parse_health + regression 14/15/16); smoke headless main scene thật (server tắt → boot→health-fail→màn lỗi, exit 0, không crash) + main_hub scene (exit 0). **Grep guard xanh:** `HTTPRequest`/`core/net` KHÔNG có trong `src/ui` (chỉ comment); usage `NetworkClient` chỉ ở `boot_controller.gd` (presenter — cổng hợp lệ); 4 file view network-free.
- **CI-pending:** `ci-client.yml` trên GitHub Actions (import + gdUnit4 headless dưới xvfb) — verify khi Actions chạy.
- **Nợ có chủ đích:** `AudioManager` (không thuộc checklist phase 17 — hoãn, chưa có audio content); transition nâng cao/async asset load (ADR-009) = phase 52; auth vào boot = phase 20; siết cổng config khi Config Service (phase 21/22) sẵn sàng.

**Đủ điều kiện đóng — kết thúc nhóm 3 (Client Core Framework).**

---

## Liên kết
- [`../godot/ui-architecture.md`](../godot/ui-architecture.md) · [`../godot/scene-architecture.md`](../godot/scene-architecture.md) · [`../godot/resources-and-assets.md`](../godot/resources-and-assets.md)
- ADR: [`../adr/ADR-002-godot-architecture.md`](../adr/ADR-002-godot-architecture.md) · [`../adr/ADR-009-asset-loading.md`](../adr/ADR-009-asset-loading.md)
- Roadmap: [`README.md`](README.md) → kế: [`18-auth-jwt-guest.md`](18-auth-jwt-guest.md)
