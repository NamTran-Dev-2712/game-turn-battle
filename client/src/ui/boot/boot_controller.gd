# BootController — presenter/điều phối boot (Phase 17). Root script của boot.tscn.
# Luồng: splash → NetworkClient.get(/health) [CỔNG kết nối bắt buộc] → ConfigProvider.check_for_update()
#   [BEST-EFFORT: Config Service = phase 21, endpoint vắng ⇒ giữ cache, KHÔNG chặn boot] →
#   SceneRouter.goto(main_hub) + clear_history().
# Thất bại health/mất mạng → BootErrorView (thông báo AN TOÀN, không lộ nội bộ) + nút retry → chạy lại.
# Là PRESENTER: chứa toàn bộ logic + gọi network; hai view (BootView/BootErrorView) thuần hiển thị.
# Deps inject được cho test (mặc định = autoload). Chi tiết: docs/godot/scene-architecture.md §4.2.
class_name BootController
extends Control

## Đường dẫn /health (KHÔNG dưới /api/v1 — health là endpoint hạ tầng, phase 12/13).
const HEALTH_PATH: String = "/health"
## Main hub đích sau khi boot xong.
const DEFAULT_MAIN_HUB_PATH: String = "res://src/ui/main_hub/main_hub.tscn"
## Thông báo lỗi AN TOÀN cho người dùng (không lộ stack/chi tiết nội bộ — ADR-002/Tiêu chí bảo mật).
const SAFE_ERROR_MESSAGE: String = "Không kết nối được máy chủ. Vui lòng kiểm tra kết nối và thử lại."

## Trạng thái boot (kiểm thử/gỡ lỗi).
enum State { IDLE, CHECKING_HEALTH, LOADING_CONFIG, DONE, FAILED }

## Kênh mạng (seam — mặc định autoload NetworkClient; inject giả/thật cho test).
var network_client: Node = null
## Cửa đọc config (mặc định autoload ConfigProvider; inject cho test).
var config_provider: Node = null
## Điều hướng scene (mặc định autoload SceneRouter; inject stub cho test).
var scene_router: Node = null
## Scene main hub đích.
var main_hub_path: String = DEFAULT_MAIN_HUB_PATH
## Tự chạy boot khi _ready (test đặt false để tự điều khiển).
var auto_start: bool = true

## Trạng thái hiện tại (State).
var state: int = State.IDLE

## Boot hoàn tất (đã vào hub).
signal boot_succeeded()
## Boot thất bại (đã hiện màn lỗi). `message` = thông báo an toàn đã hiển thị.
signal boot_failed(message)

var _boot_view: BootView = null
var _error_view: BootErrorView = null
# Khoá chống tái nhập (double-tap retry / gọi start lồng nhau).
var _running: bool = false


func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	if network_client == null:
		network_client = get_node_or_null(^"/root/NetworkClient")
	if config_provider == null:
		config_provider = get_node_or_null(^"/root/ConfigProvider")
	if scene_router == null:
		scene_router = get_node_or_null(^"/root/SceneRouter")
	_build_views()
	if auto_start:
		start()


func _build_views() -> void:
	_boot_view = BootView.new()
	_boot_view.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(_boot_view)
	_error_view = BootErrorView.new()
	_error_view.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(_error_view)
	# Nối intent của màn lỗi MỘT LẦN (không nối lại mỗi lần retry ⇒ không trùng listener).
	_error_view.intent.connect(_on_error_view_intent)
	_error_view.visible = false


## Chạy boot flow. An toàn khi gọi lặp: khoá `_running` chặn tái nhập.
func start() -> void:
	if _running:
		return
	_running = true
	_show_boot()

	# 1) Health = cổng kết nối bắt buộc. Mất mạng/non-2xx ⇒ màn lỗi + retry.
	_set_state(State.CHECKING_HEALTH)
	if network_client == null:
		_fail()
		return
	var health: NetResult = await network_client.get_json(HEALTH_PATH, NetworkResponseParser.parse_health)
	if not health.ok:
		_fail()
		return

	# 2) Config = best-effort: server báo version mới thì tải; endpoint vắng/lỗi ⇒ giữ cache, KHÔNG chặn boot.
	#    (Config Service thật = phase 21; siết cổng config khi phase 21/22 hoàn thiện.)
	_set_state(State.LOADING_CONFIG)
	if _boot_view != null:
		_boot_view.set_data({"status": "Đang tải cấu hình..."})
	if config_provider != null:
		await config_provider.check_for_update()

	# 3) Vào main hub qua SceneRouter (router giải phóng scene boot). clear_history: không quay lại boot.
	_set_state(State.DONE)
	_running = false
	if scene_router != null:
		scene_router.goto_scene(main_hub_path)
		scene_router.clear_history()
	boot_succeeded.emit()


## Thử lại sau lỗi: chạy lại boot flow sạch (chờ hoàn tất — hỗ trợ test await).
func retry() -> void:
	await start()


# Nghe ý định từ màn lỗi: `retry` → chạy lại (fire-and-forget cho lượt bấm nút).
func _on_error_view_intent(intent_name: StringName, _payload: Dictionary) -> void:
	if intent_name == BootErrorView.INTENT_RETRY:
		retry()


func _fail() -> void:
	_set_state(State.FAILED)
	_running = false
	_show_error(SAFE_ERROR_MESSAGE)
	boot_failed.emit(SAFE_ERROR_MESSAGE)


func _show_boot() -> void:
	if _error_view != null:
		_error_view.visible = false
	if _boot_view != null:
		_boot_view.set_data({"status": "Đang kết nối máy chủ..."})
		_boot_view.visible = true


func _show_error(message: String) -> void:
	if _boot_view != null:
		_boot_view.visible = false
	if _error_view != null:
		_error_view.set_data({"message": message})
		_error_view.visible = true


func _set_state(next: int) -> void:
	state = next
