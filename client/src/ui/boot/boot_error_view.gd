# BootErrorView — màn lỗi boot + nút retry (Phase 17). View thuần: hiển thị THÔNG BÁO AN TOÀN
# (không lộ stack/chi tiết nội bộ) và phát ý định `retry` khi bấm nút. KHÔNG tự chạy lại boot —
# BootController (presenter) nghe `intent` và quyết định. Chi tiết: docs/godot/ui-architecture.md.
class_name BootErrorView
extends BaseView

## Ý định phát khi người dùng bấm "Thử lại".
const INTENT_RETRY: StringName = &"retry"

var _message: Label = null
var _retry_button: Button = null


func _ready() -> void:
	_build()


func _build() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	var center := CenterContainer.new()
	center.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(center)
	var box := VBoxContainer.new()
	box.alignment = BoxContainer.ALIGNMENT_CENTER
	center.add_child(box)
	_message = Label.new()
	_message.text = "Đã xảy ra lỗi khi khởi động."
	_message.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	box.add_child(_message)
	_retry_button = Button.new()
	_retry_button.text = "Thử lại"
	box.add_child(_retry_button)
	# Nối MỘT LẦN ở đây (không nối lại mỗi lần lỗi ⇒ không trùng listener).
	_retry_button.pressed.connect(_on_retry_pressed)


func _render(data: Dictionary) -> void:
	if _message != null and data.has("message"):
		_message.text = str(data["message"])


func _on_retry_pressed() -> void:
	emit_intent(INTENT_RETRY)
