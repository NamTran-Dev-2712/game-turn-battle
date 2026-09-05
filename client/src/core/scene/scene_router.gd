# SceneRouter — autoload điều hướng scene tập trung (ADR-002, ADR-009).
# Trách nhiệm DUY NHẤT: chuyển scene + back stack. KHÔNG chứa logic feature.
# Mô hình "scene-host": router giữ scene hiện tại làm node CON và tráo tại chỗ, giải
# phóng scene cũ (queue_free) ⇒ không rò rỉ / không giữ tham chiếu scene cũ.
# Transition: tối giản (tráo tức thời) — hiệu ứng chuyển cảnh nâng cao để phase UI sau.
# Phát sự kiện `scene_changed` qua EventBus để feature phản ứng mà không import SceneRouter.
# Chi tiết: docs/godot/scene-architecture.md.
extends Node

# Tên sự kiện phát qua EventBus khi đổi scene (khớp danh mục EventBus.EVENTS).
const _EVENT_SCENE_CHANGED: StringName = &"scene_changed"

## Đường dẫn scene đang hiển thị ("" nếu chưa có).
var current_path: String = ""
## Node scene đang hiển thị (null nếu chưa có).
var current_scene: Node = null

var _stack: Array[String] = []
# Ngữ cảnh điều hướng của scene hiện tại (vd {"hero_id": "..."}). SceneRouter không truyền tham số qua
# constructor scene (mô hình scene-host tráo tại chỗ), nên presenter của scene đích đọc qua route_context().
# Đặt khi goto_scene (TRƯỚC khi instantiate ⇒ _ready của scene mới đọc được); back() reset về rỗng.
var _current_context: Dictionary = {}


## Chuyển tới scene tại `path`, đẩy scene hiện tại vào back stack. `context` (tuỳ chọn, additive — phase 27)
## là dữ liệu điều hướng cho presenter scene đích đọc qua route_context() (vd hero_id). Trả về false (và
## push_error) nếu path không hợp lệ — không ném, không nuốt lỗi.
func goto_scene(path: String, context: Dictionary = {}) -> bool:
	if not ResourceLoader.exists(path):
		push_error("SceneRouter: không tìm thấy scene '%s'." % path)
		return false
	var had_current: bool = current_scene != null
	var previous_path: String = current_path
	# Đặt context TRƯỚC khi tráo: _swap_to instantiate scene mới ⇒ _ready → presenter đọc route_context() ngay.
	_current_context = context.duplicate(true)
	if not _swap_to(path):
		_current_context = {}
		return false
	if had_current:
		_stack.push_back(previous_path)
	return true


## Ngữ cảnh điều hướng của scene hiện tại (bản sao). {} nếu không có. Presenter scene đích đọc tại đây
## (vd `SceneRouter.route_context().get("hero_id")`).
func route_context() -> Dictionary:
	return _current_context.duplicate(true)


## Quay lại scene trước trong back stack. Trả về false nếu stack rỗng.
func back() -> bool:
	if _stack.is_empty():
		return false
	var previous: String = _stack.pop_back()
	if not _swap_to(previous):
		# Tráo thất bại: khôi phục stack để không mất lịch sử điều hướng.
		_stack.push_back(previous)
		return false
	# Quay lại: xoá context của scene vừa rời (context chỉ dành cho lần đi tới).
	_current_context = {}
	return true


## Số phần tử back stack (hỗ trợ kiểm thử/gỡ lỗi).
func stack_depth() -> int:
	return _stack.size()


## Xoá toàn bộ back stack (không đổi scene hiện tại) — dùng khi reset điều hướng (vd boot/logout).
func clear_history() -> void:
	_stack.clear()


# Tráo scene hiện tại sang `path`: nạp + instantiate + gắn con, giải phóng scene cũ,
# cập nhật trạng thái, phát `scene_changed`. Nơi DUY NHẤT đụng tới cây scene.
func _swap_to(path: String) -> bool:
	var packed: PackedScene = load(path) as PackedScene
	if packed == null:
		push_error("SceneRouter: '%s' không phải PackedScene." % path)
		return false
	var next_scene: Node = packed.instantiate()
	var from_path: String = current_path
	if current_scene != null:
		current_scene.queue_free()
	add_child(next_scene)
	current_scene = next_scene
	current_path = path
	# EventBus là autoload luôn tồn tại; phát để feature phản ứng khi đổi scene.
	EventBus.emit(_EVENT_SCENE_CHANGED, {"to": path, "from": from_path})
	return true
