# 0015 — Client boot flow + UI base standardized (Phase 17)

- **Trạng thái:** Đã chốt & verify cục bộ (2026-08-20, Godot 4.7.1-stable). Đóng **nhóm 3 — Client Core Framework**.
- **Bối cảnh:** Phase 14–16 dựng 5 autoload (EventBus/SceneRouter/NetworkClient/ConfigProvider/StateCache) nhưng client
  chưa có scene nào (`project.godot` không có `run/main_scene`, `src/ui`/`src/features` chỉ README). Phase 17 dựng lát
  cắt chạy được đầu tiên + nền UI cho mọi feature.

## Quyết định

- **App-shell:** `run/main_scene = res://src/ui/app_root.tscn` (`AppRoot` = Control RỖNG). `AppRoot._ready` →
  `SceneRouter.goto(boot)` ⇒ **SceneRouter làm CHỦ SỞ HỮU mọi screen từ frame đầu** (boot → hub, tráo tại chỗ +
  `queue_free` scene cũ, không chồng lớp). **User chọn** app-shell thay vì để boot làm main scene tự-`queue_free`
  (footgun); mọi screen đi qua router đồng nhất.
- **Boot** (`src/ui/boot/boot_controller.gd` = presenter, root của `boot.tscn`, tạo `BootView`+`BootErrorView` bằng code):
  1. **Health = cổng kết nối BẮT BUỘC:** `NetworkClient.get_json("/health", NetworkResponseParser.parse_health)`. Mất
     mạng/non-2xx → `BootErrorView` + retry.
  2. **Config = BEST-EFFORT:** `ConfigProvider.check_for_update()`. Config Service thật = phase 21 ⇒ endpoint vắng/lỗi hôm
     nay là bình thường (giữ cache, KHÔNG chặn boot). Siết cổng config khi phase 21/22 sẵn sàng.
  3. `SceneRouter.goto(main_hub)` + `clear_history()`; emit `boot_succeeded`/`boot_failed`.
  Deps (`network_client`/`config_provider`/`scene_router`) inject được (mặc định autoload) + `auto_start` — để test.
- **UI base** (`src/ui/base/base_view.gd`, `class_name BaseView extends Control`): hợp đồng một chiều
  **data-in** (`set_data`→`_render`) → **intent-out** (`emit_intent`→signal `intent(name, payload)`); hook `bind`/`unbind`
  gọi ở `_enter_tree`/`_exit_tree`. **View network-free** (grep guard: không `NetworkClient`/`HTTPRequest`/`core/net`
  trong file view). Presenter (BootController/`MainHubPresenter`) là điểm chạm DUY NHẤT: đọc `StateCache`/`ConfigProvider`
  (hiển thị), gọi `NetworkClient` qua cổng, điều hướng `SceneRouter`, phát EventBus **chỉ** cho sự kiện toàn cục thật.
- **Intent = signal cục bộ + presenter** (**User chọn** thay vì mỗi nút một event EventBus) ⇒ danh mục EventBus giữ ĐÓNG
  (§3.1) — **KHÔNG thêm event mới ở Phase 17**.
- **Màn lỗi + retry:** `BootErrorView` thông báo AN TOÀN (không lộ stack/nội bộ) + nút retry nối MỘT LẦN (không trùng
  listener); `retry()` chạy lại flow sạch với khoá `_running` (chống tái nhập / điều hướng-request trùng).
- **Main hub:** `MainHubView` + `MainHubPresenter` (đọc `ConfigProvider.config_label()`/`StateCache.is_offline()` hiển thị,
  KHÔNG network); 4 nút placeholder phát intent — shell điều hướng/bố cục, chưa nghiệp vụ.
- **Net:** thêm `NetworkResponseParser.parse_health` (tái dùng model generated `HealthResponse`; thiếu `status` ⇒ null).
  Không đổi contract ⇒ không drift `client/src/data/generated`.
- **`AudioManager` = HOÃN:** không thuộc checklist Phase 17 (chưa có audio content) — thêm khi cần; đã sửa note stale ở
  `scene-architecture.md §5`.

## Verify

- `--headless --import` exit 0 (0 error/0 warning; đăng ký `BaseView`/`BootController`/`BootErrorView`/`BootView`/
  `MainHubView`/`MainHubPresenter`).
- gdUnit4 **toàn bộ 48/48 pass, 0 error/0 failure/0 orphan** (`tests/ui/base` 3 + `tests/ui/boot` 5 + parse_health 2 +
  regression 14/15/16). Boot test: NetworkClient thật + FakeHttpTransport + SceneRouter stub + ConfigProvider cache tạm.
- Smoke headless scene thật: main scene (server tắt → boot→health-fail→màn lỗi, exit 0, không crash) + `main_hub.tscn`
  (exit 0). **Grep guard xanh:** `HTTPRequest`/`core/net` không có trong `src/ui` (chỉ comment); `NetworkClient` chỉ ở
  `boot_controller.gd` (presenter — cổng hợp lệ).
- **CI-pending:** `ci-client.yml` (import + gdUnit4 headless dưới xvfb) trên GitHub Actions.

## Ràng buộc cho agent sau

- **Tái dùng `BaseView`/boot flow** — **không** để view gọi network, **không** biến boot thành main scene tự-free,
  **không** thêm event EventBus cho mỗi thao tác UI (danh mục đóng). View mới = `extends BaseView`, dữ liệu vào/intent ra;
  logic/mạng/điều hướng ở presenter.
- Endpoint mới cho boot/feature = thêm parse func ở `core/net/response_parser.gd` + gọi qua NetworkClient ở presenter.
- Đồng bộ: `client/src/ui/*` + `client/tests/ui/*` + `client/project.godot` (`run/main_scene`) +
  `docs/godot/ui-architecture.md` §2.1/§4.1 + `docs/godot/scene-architecture.md` §4.2/§5 + `docs/godot/state-and-signals.md`
  §4 + `.instructions/client.md` + `.claude/agents/godot-client.md` + root `setup-and-run.md`.

> Quyết định kiến trúc gốc: ADR-002 (Godot architecture), ADR-009 (asset loading).
