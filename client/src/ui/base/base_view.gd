# BaseView — lớp nền cho MỌI view UI (Phase 17). Hợp đồng một chiều rõ ràng (ADR-002):
#   DỮ LIỆU VÀO  → `set_data(data)` → `_render(data)` (subclass vẽ UI).
#   Ý ĐỊNH RA    → `emit_intent(name, payload)` → signal `intent` (presenter/view-model dịch).
# View CHỈ hiển thị + phát ý định. View **KHÔNG** gọi network (NetworkClient/HTTPRequest/core/net),
# **KHÔNG** chứa logic nghiệp vụ, **KHÔNG** tự quyết trạng thái ứng dụng. Việc gọi server / điều hướng
# / phát sự kiện toàn cục là của presenter (nghe signal `intent`). Grep guard: không `core/net` trong ui.
# Chi tiết: docs/godot/ui-architecture.md.
class_name BaseView
extends Control

## Ý định người dùng phát ra khỏi view (nút bấm, thao tác). Presenter lắng nghe và dịch
## thành điều hướng (SceneRouter) / gọi feature / phát EventBus khi là sự kiện toàn cục.
## `intent_name` = StringName, `payload` = Dictionary (mặc định {}).
signal intent(intent_name, payload)


## Nạp dữ liệu hiển thị vào view → gọi `_render`. Chỉ dữ liệu vào, không side-effect ra ngoài.
func set_data(data: Dictionary) -> void:
	_render(data)


## Ghi đè ở subclass để render `data` lên UI. Mặc định no-op (giữ BaseView tối giản).
func _render(_data: Dictionary) -> void:
	pass


## Phát một ý định (tên + payload) tới presenter. Side-effect DUY NHẤT = signal `intent`.
func emit_intent(intent_name: StringName, payload: Dictionary = {}) -> void:
	intent.emit(intent_name, payload)


## Hook vòng đời: gắn subscription (vd EventBus) khi view vào cây. Ghi đè nếu cần; mặc định no-op.
func bind() -> void:
	pass


## Hook vòng đời: huỷ subscription khi view rời cây — tránh rò rỉ. Ghi đè nếu cần; mặc định no-op.
func unbind() -> void:
	pass


# Gọi bind/unbind ĐỐI XỨNG theo vòng đời cây (không đụng _ready ⇒ subclass tự do override _ready).
func _enter_tree() -> void:
	bind()


func _exit_tree() -> void:
	unbind()
