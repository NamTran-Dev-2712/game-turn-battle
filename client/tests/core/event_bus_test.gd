# Test EventBus — Phase 14.
# Chứng minh pub/sub: emit → subscriber nhận ĐÚNG payload; unsubscribe ngắt nhận;
# nối trùng chỉ giao một lần; danh mục ĐÓNG (is_known). Kiểm qua autoload thật.
# Tất định — không network/animation/wall-clock (docs/testing/godot-testing.md).
extends GdUnitTestSuite

var _captured: Array = []


func before_test() -> void:
	_captured = []


func after_test() -> void:
	# Dọn kết nối để không ảnh hưởng test sau (EventBus là autoload dùng chung).
	EventBus.unsubscribe(&"scene_changed", _capture)


func _capture(payload) -> void:
	_captured.append(payload)


func test_emit_delivers_payload_to_subscriber() -> void:
	EventBus.subscribe(&"scene_changed", _capture)
	EventBus.emit(&"scene_changed", {"to": "res://x.tscn", "from": ""})
	assert_int(_captured.size()).is_equal(1)
	assert_dict(_captured[0]).contains_key_value("to", "res://x.tscn")


func test_unsubscribe_stops_delivery() -> void:
	EventBus.subscribe(&"scene_changed", _capture)
	EventBus.unsubscribe(&"scene_changed", _capture)
	EventBus.emit(&"scene_changed", {"to": "res://x.tscn", "from": ""})
	assert_int(_captured.size()).is_equal(0)


func test_double_subscribe_delivers_once() -> void:
	EventBus.subscribe(&"scene_changed", _capture)
	EventBus.subscribe(&"scene_changed", _capture)
	EventBus.emit(&"scene_changed", {"to": "res://x.tscn", "from": ""})
	assert_int(_captured.size()).is_equal(1)


func test_catalogue_is_closed() -> void:
	# Chỉ sự kiện đã đăng ký mới hợp lệ ⇒ chống "God channel"/"event chui".
	assert_bool(EventBus.is_known(&"scene_changed")).is_true()
	assert_bool(EventBus.is_known(&"totally_unknown_event")).is_false()


func test_autoloads_present() -> void:
	# EventBus & SceneRouter là hai autoload độc lập, đều có mặt.
	assert_object(get_node_or_null(^"/root/EventBus")).is_not_null()
	assert_object(get_node_or_null(^"/root/SceneRouter")).is_not_null()
