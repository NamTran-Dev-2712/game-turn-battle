# AppRoot — main scene tối giản (Phase 17). Vai trò DUY NHẤT: mở đầu điều hướng bằng cách route
# tới boot qua SceneRouter, để SceneRouter làm CHỦ SỞ HỮU mọi screen hiển thị ngay từ frame đầu
# (boot → hub, tráo/giải phóng gọn — không chồng lớp). AppRoot không có UI riêng (Control rỗng).
# Chi tiết: docs/godot/scene-architecture.md §4.2.
extends Control

## Scene boot đầu tiên (router host).
const BOOT_PATH: String = "res://src/ui/boot/boot.tscn"


func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	# Autoload đã sẵn sàng trước main scene; điều hướng tập trung qua SceneRouter (ADR-002).
	var router := get_node_or_null(^"/root/SceneRouter")
	if router != null:
		router.goto_scene(BOOT_PATH)
	else:
		push_error("AppRoot: thiếu autoload SceneRouter — không thể khởi động boot.")
