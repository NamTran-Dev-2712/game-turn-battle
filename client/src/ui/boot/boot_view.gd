# BootView — splash boot (Phase 17). View thuần: hiển thị tiêu đề + trạng thái kết nối.
# Nhận dữ liệu qua `set_data({ "status": String })`; KHÔNG gọi network, KHÔNG chứa logic boot
# (logic ở BootController = presenter). Chi tiết: docs/godot/ui-architecture.md.
class_name BootView
extends BaseView

var _title: Label = null
var _status: Label = null


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
	_title = Label.new()
	_title.text = "game team"
	_title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	box.add_child(_title)
	_status = Label.new()
	_status.text = "Đang kết nối máy chủ..."
	_status.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	box.add_child(_status)


# Cập nhật dòng trạng thái (vd "Đang tải cấu hình..."). An toàn nếu gọi trước khi UI dựng xong.
func _render(data: Dictionary) -> void:
	if _status != null and data.has("status"):
		_status.text = str(data["status"])
