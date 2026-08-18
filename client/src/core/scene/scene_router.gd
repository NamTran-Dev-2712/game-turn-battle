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


## Chuyển tới scene tại `path`, đẩy scene hiện tại vào back stack.
## Trả về false (và push_error) nếu path không hợp lệ — không ném, không nuốt lỗi.
func goto_scene(path: String) -> bool:
	if not ResourceLoader.exists(path):
		push_error("SceneRouter: không tìm thấy scene '%s'." % path)
		return false
	var had_current: bool = current_scene != null
	var previous_path: String = current_path
	if not _swap_to(path):
		return false
	if had_current:
		_stack.push_back(previous_path)
	return true


## Quay lại scene trước trong back stack. Trả về false nếu stack rỗng.
func back() -> bool:
	if _stack.is_empty():
		return false
	var previous: String = _stack.pop_back()
	if not _swap_to(previous):
		# Tráo thất bại: khôi phục stack để không mất lịch sử điều hướng.
		_stack.push_back(previous)
		return false
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
